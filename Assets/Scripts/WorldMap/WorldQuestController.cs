using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class WorldQuestController : MonoBehaviour
{
    private enum ConfirmPopupMode
    {
        None = 0,
        AbandonQuest = 1,
        IgnoreUnclaimedRewardsAndClose = 2
    }

    [Header("References")]
    [SerializeField] private WorldRunManager runManager;
    [SerializeField] private WorldQuestListPanelUI questListPanelUI;
    [SerializeField] private WorldQuestPopupUI questPopupUI;
    [SerializeField] private WorldQuestAbandonConfirmPopupUI abandonConfirmPopupUI;
    [SerializeField] private SaveCoordinator saveCoordinator;

    [Header("Quest Definitions")]
    [SerializeField] private List<WorldQuestDefinition> questDefinitions = new List<WorldQuestDefinition>(4);

    [Header("Rules")]
    [SerializeField] private int maxActiveQuests = 5;
    [SerializeField] private float completionPopupDelay = 0.65f;
    [SerializeField] private float immediateRewardGrantDelay = 1f;

    [Header("Confirm Popup Texts")]
    [SerializeField] private string abandonMessage = "정말 이 퀘스트를 포기하시겠습니까?\n진행도는 초기화되며 다시 받을 수 없습니다.";
    [SerializeField] private string abandonConfirmLabel = "퀘스트 포기";
    [SerializeField] private string confirmCloseLabel = "닫기";

    [SerializeField] private string unclaimedRewardMessage = "수령하지 않은 보상이 있습니다.\n정말 무시하고 닫으시겠습니까?";
    [SerializeField] private string ignoreRewardConfirmLabel = "무시하고 닫기";

    private readonly Dictionary<int, WorldQuestState> generatedQuestByTileId = new Dictionary<int, WorldQuestState>();
    private readonly HashSet<int> blockedQuestTileIds = new HashSet<int>();
    private readonly List<WorldQuestState> activeAcceptedQuests = new List<WorldQuestState>();
    private readonly List<WorldQuestState> visibleQuestBuffer = new List<WorldQuestState>();
    private readonly HashSet<WorldQuestState> delayedImmediateRewardScheduled = new HashSet<WorldQuestState>();

    private WorldQuestState currentPopupQuest;
    private WorldQuestPopupMode currentPopupMode = WorldQuestPopupMode.None;

    private WorldQuestState pendingConfirmQuest;
    private ConfirmPopupMode pendingConfirmMode = ConfirmPopupMode.None;

    public IReadOnlyList<WorldQuestState> ActiveAcceptedQuests => activeAcceptedQuests;
    public bool IsPopupOpen => (questPopupUI != null && questPopupUI.IsOpen) || (abandonConfirmPopupUI != null && abandonConfirmPopupUI.IsOpen);

    private void Awake()
    {
        if (runManager == null)
            runManager = UnityEngine.Object.FindFirstObjectByType<WorldRunManager>();
    }

    private void Start()
    {
        if (questListPanelUI != null)
            questListPanelUI.Bind(this);

        if (questPopupUI != null)
            questPopupUI.Initialize(this);

        if (abandonConfirmPopupUI != null)
            abandonConfirmPopupUI.Initialize(this);

        RefreshQuestListUI();
    }

    private void RequestAutoSaveAll()
    {
        saveCoordinator?.SaveAll();
    }

    public IReadOnlyList<WorldQuestState> GetVisibleQuestList()
    {
        visibleQuestBuffer.Clear();

        for (int i = 0; i < activeAcceptedQuests.Count; i++)
        {
            WorldQuestState quest = activeAcceptedQuests[i];
            if (quest == null)
                continue;
            if (quest.isCancelled)
                continue;
            if (quest.isCompleted)
                continue;
            if (!quest.isAccepted)
                continue;

            visibleQuestBuffer.Add(quest);
        }

        return visibleQuestBuffer;
    }

    public bool HasReachedQuestLimit()
    {
        int count = 0;

        for (int i = 0; i < activeAcceptedQuests.Count; i++)
        {
            WorldQuestState q = activeAcceptedQuests[i];
            if (q == null)
                continue;
            if (q.isCancelled)
                continue;
            if (!q.isAccepted)
                continue;
            if (q.isCompleted)
                continue;

            count++;
        }

        return count >= maxActiveQuests;
    }

    public bool IsActiveCaptureTargetTile(int tileId)
    {
        if (tileId < 0)
            return false;

        for (int i = 0; i < activeAcceptedQuests.Count; i++)
        {
            WorldQuestState quest = activeAcceptedQuests[i];
            if (quest == null || !quest.isAccepted || quest.isCancelled || quest.isCompleted)
                continue;
            if (quest.definition == null)
                continue;

            if (quest.definition.questType == WorldQuestType.CaptureSpecificTile &&
                quest.assignedTargetTileId == tileId)
            {
                return true;
            }
        }

        return false;
    }

    public bool TryOpenQuestOfferFromTile(WorldTileData sourceTile)
    {
        if (sourceTile == null || runManager == null || runManager.MapData == null)
            return false;

        WorldQuestState quest = GetOrCreateQuestForTile(sourceTile, runManager.MapData);
        if (quest == null)
            return false;

        currentPopupQuest = quest;
        currentPopupMode = WorldQuestPopupMode.Offer;

        if (questPopupUI != null)
            questPopupUI.ShowOffer(quest, !HasReachedQuestLimit());

        return true;
    }

    public WorldQuestState GetOrCreateQuestForTile(WorldTileData sourceTile, WorldMapData mapData)
    {
        if (sourceTile == null || mapData == null)
            return null;

        if (blockedQuestTileIds.Contains(sourceTile.tileId))
            return null;

        if (generatedQuestByTileId.TryGetValue(sourceTile.tileId, out WorldQuestState existing) && existing != null)
            return existing;

        WorldQuestDefinition picked = PickQuestDefinition(sourceTile, mapData);
        if (picked == null)
            return null;

        WorldQuestState state = new WorldQuestState();
        state.Initialize(picked, sourceTile.tileId);

        if (picked.questType == WorldQuestType.CaptureSpecificTile)
            state.assignedTargetTileId = PickTargetCaptureTileId(mapData, sourceTile.tileId);

        generatedQuestByTileId[sourceTile.tileId] = state;
        return state;
    }

    public void AcceptCurrentPopupQuest()
    {
        if (currentPopupQuest == null || currentPopupMode != WorldQuestPopupMode.Offer)
            return;

        if (!TryAcceptQuest(currentPopupQuest))
        {
            if (questPopupUI != null)
                questPopupUI.RefreshCurrent();
            return;
        }

        TryInvokeRunManagerMethod("HandleQuestAcceptedFromPopup");
        HidePopupOnly();
        RefreshQuestListUI();
    }

    public void RejectCurrentPopupQuest()
    {
        if (currentPopupQuest == null || currentPopupMode != WorldQuestPopupMode.Offer)
            return;

        TryInvokeRunManagerMethod("HandleQuestRejectedFromPopup");
        HidePopupOnly();
    }

    public bool TryAcceptQuest(WorldQuestState quest)
    {
        if (quest == null || quest.isCancelled || quest.isAccepted || quest.isCompleted)
            return false;

        if (HasReachedQuestLimit())
            return false;

        quest.isAccepted = true;

        if (!activeAcceptedQuests.Contains(quest))
            activeAcceptedQuests.Add(quest);

        RefreshQuestListUI();
        return true;
    }

    public void OpenQuestFromList(WorldQuestState quest)
    {
        if (quest == null || quest.isCancelled)
            return;

        currentPopupQuest = quest;

        if (quest.isCompleted)
        {
            currentPopupMode = WorldQuestPopupMode.Completed;

            if (questPopupUI != null)
                questPopupUI.ShowCompleted(quest);

            ScheduleImmediateRewardsForQuest(quest);
        }
        else
        {
            currentPopupMode = WorldQuestPopupMode.Active;

            if (questPopupUI != null)
                questPopupUI.ShowActive(quest);
        }
    }

    public void RequestAbandonFromCurrentPopup()
    {
        if (currentPopupQuest == null)
            return;

        OpenConfirmPopup(
            currentPopupQuest,
            ConfirmPopupMode.AbandonQuest,
            abandonMessage,
            abandonConfirmLabel,
            confirmCloseLabel);
    }

    public void CancelQuestFromList(WorldQuestState quest)
    {
        if (quest == null || !quest.isAccepted || quest.isCompleted)
            return;

        OpenConfirmPopup(
            quest,
            ConfirmPopupMode.AbandonQuest,
            abandonMessage,
            abandonConfirmLabel,
            confirmCloseLabel);
    }

    public void ConfirmQuestAbandon(WorldQuestState quest)
    {
        if (quest == null || !quest.isAccepted || quest.isCompleted)
        {
            CloseQuestAbandonConfirmPopup();
            return;
        }

        quest.isAccepted = false;
        quest.isCancelled = true;
        quest.currentProgress = 0;

        blockedQuestTileIds.Add(quest.sourceTileId);
        activeAcceptedQuests.Remove(quest);
        delayedImmediateRewardScheduled.Remove(quest);

        if (currentPopupQuest == quest && currentPopupMode == WorldQuestPopupMode.Active)
            HidePopupOnly();

        CloseQuestAbandonConfirmPopup();
        RefreshQuestListUI();
    }

    public void CloseQuestAbandonConfirmPopup()
    {
        pendingConfirmQuest = null;
        pendingConfirmMode = ConfirmPopupMode.None;

        if (abandonConfirmPopupUI != null)
            abandonConfirmPopupUI.Hide();
    }

    public void CloseCurrentPopup()
    {
        if (currentPopupQuest != null && currentPopupMode == WorldQuestPopupMode.Completed)
        {
            if (currentPopupQuest.HasAnyUnclaimedItemRewards())
            {
                OpenConfirmPopup(
                    currentPopupQuest,
                    ConfirmPopupMode.IgnoreUnclaimedRewardsAndClose,
                    unclaimedRewardMessage,
                    ignoreRewardConfirmLabel,
                    confirmCloseLabel);
                return;
            }

            FinalizeCompletedQuestClose(currentPopupQuest);
            HidePopupOnly();
            RefreshQuestListUI();
            TryShowQueuedCompletionPopup();
            return;
        }

        HidePopupOnly();
        RefreshQuestListUI();
    }

    public void CloseCurrentPopupFromOutsideClick()
    {
        if (currentPopupMode != WorldQuestPopupMode.Active)
            return;

        HidePopupOnly();
    }

    public void NotifyEnemyKilled(int count = 1)
    {
        if (count <= 0)
            return;

        for (int i = 0; i < activeAcceptedQuests.Count; i++)
        {
            WorldQuestState quest = activeAcceptedQuests[i];
            if (quest == null || quest.isCompleted || quest.isCancelled || !quest.isAccepted)
                continue;

            if (quest.definition != null && quest.definition.questType == WorldQuestType.KillEnemies)
                quest.AddProgress(count);
        }

        PostProgressRefresh();
    }

    public void NotifyTileCaptured(WorldTileData tile)
    {
        if (tile == null)
            return;

        for (int i = 0; i < activeAcceptedQuests.Count; i++)
        {
            WorldQuestState quest = activeAcceptedQuests[i];
            if (quest == null || quest.isCompleted || quest.isCancelled || !quest.isAccepted)
                continue;
            if (quest.definition == null)
                continue;

            if (quest.definition.questType == WorldQuestType.CaptureSpecificTile &&
                quest.assignedTargetTileId == tile.tileId)
            {
                quest.MarkCompleted();
            }
        }

        PostProgressRefresh();
    }

    public void NotifyEliteBattleWon()
    {
        for (int i = 0; i < activeAcceptedQuests.Count; i++)
        {
            WorldQuestState quest = activeAcceptedQuests[i];
            if (quest == null || quest.isCompleted || quest.isCancelled || !quest.isAccepted)
                continue;

            if (quest.definition != null && quest.definition.questType == WorldQuestType.WinEliteBattle)
                quest.AddProgress(1);
        }

        PostProgressRefresh();
    }

    public void NotifyBossBattleWon()
    {
        for (int i = 0; i < activeAcceptedQuests.Count; i++)
        {
            WorldQuestState quest = activeAcceptedQuests[i];
            if (quest == null || quest.isCompleted || quest.isCancelled || !quest.isAccepted)
                continue;

            if (quest.definition != null && quest.definition.questType == WorldQuestType.WinBossBattle)
                quest.AddProgress(1);
        }

        PostProgressRefresh();
    }

    public void TryShowQueuedCompletionPopup()
    {
        if (questPopupUI != null && questPopupUI.IsOpen)
            return;

        if (abandonConfirmPopupUI != null && abandonConfirmPopupUI.IsOpen)
            return;

        for (int i = 0; i < activeAcceptedQuests.Count; i++)
        {
            WorldQuestState quest = activeAcceptedQuests[i];
            if (quest == null)
                continue;
            if (!quest.isCompleted || quest.completionPopupClosed || quest.completionPopupShown)
                continue;

            quest.completionPopupShown = true;
            currentPopupQuest = quest;
            currentPopupMode = WorldQuestPopupMode.Completed;

            if (questPopupUI != null)
                questPopupUI.ShowCompleted(quest);

            ScheduleImmediateRewardsForQuest(quest);
            RefreshQuestListUI();
            return;
        }
    }

    public void ClaimRewardAt(WorldQuestState quest, int rewardIndex)
    {
        if (quest == null || !quest.isCompleted)
            return;
        if (!quest.CanClaimItemAt(rewardIndex))
            return;
        if (quest.definition == null || quest.definition.itemRewards == null || rewardIndex < 0 || rewardIndex >= quest.definition.itemRewards.Count)
            return;

        WorldQuestRewardItemEntry reward = quest.definition.itemRewards[rewardIndex];
        if (reward == null || reward.item == null)
            return;

        if (TryGrantItemReward(reward.item, Mathf.Max(1, reward.amount)))
        {
            quest.MarkItemClaimed(rewardIndex);

            if (questPopupUI != null && quest == currentPopupQuest && currentPopupMode == WorldQuestPopupMode.Completed)
                questPopupUI.ShowCompleted(quest);
        }
    }

    public void ClaimAllRewardsForCurrentQuest()
    {
        if (currentPopupQuest == null || !currentPopupQuest.isCompleted)
            return;

        if (currentPopupQuest.definition != null && currentPopupQuest.definition.itemRewards != null)
        {
            for (int i = 0; i < currentPopupQuest.definition.itemRewards.Count; i++)
            {
                if (!currentPopupQuest.CanClaimItemAt(i))
                    continue;

                WorldQuestRewardItemEntry reward = currentPopupQuest.definition.itemRewards[i];
                if (reward == null || reward.item == null)
                    continue;

                if (TryGrantItemReward(reward.item, Mathf.Max(1, reward.amount)))
                    currentPopupQuest.MarkItemClaimed(i);
            }
        }

        FinalizeCompletedQuestClose(currentPopupQuest);
        HidePopupOnly();
        RefreshQuestListUI();
        TryShowQueuedCompletionPopup();
    }

    public void LoadFromSave(IReadOnlyList<WorldQuestSaveData> savedQuests)
    {
        generatedQuestByTileId.Clear();
        blockedQuestTileIds.Clear();
        activeAcceptedQuests.Clear();
        visibleQuestBuffer.Clear();
        delayedImmediateRewardScheduled.Clear();

        currentPopupQuest = null;
        currentPopupMode = WorldQuestPopupMode.None;
        pendingConfirmQuest = null;
        pendingConfirmMode = ConfirmPopupMode.None;

        if (questPopupUI != null)
            questPopupUI.Hide();

        if (abandonConfirmPopupUI != null)
            abandonConfirmPopupUI.Hide();

        if (savedQuests == null)
        {
            RefreshQuestListUI();
            return;
        }

        for (int i = 0; i < savedQuests.Count; i++)
        {
            WorldQuestSaveData save = savedQuests[i];
            if (save == null)
                continue;

            WorldQuestDefinition def = FindQuestDefinitionById(save.questId);
            if (def == null)
                continue;

            WorldQuestState quest = new WorldQuestState();
            quest.Initialize(def, save.sourceTileId);

            quest.assignedTargetTileId = save.assignedTargetTileId;
            quest.currentProgress = save.currentProgress;
            quest.targetProgress = Mathf.Max(1, save.targetProgress);
            quest.isAccepted = save.isAccepted;
            quest.isCancelled = save.isCancelled;
            quest.isCompleted = save.isCompleted;
            quest.completionPopupQueued = save.completionPopupQueued;
            quest.completionPopupShown = save.completionPopupShown;
            quest.completionPopupClosed = save.completionPopupClosed;
            quest.soulGranted = save.soulGranted;
            quest.experienceGranted = save.experienceGranted;

            quest.itemClaimed.Clear();
            if (save.itemClaimed != null)
                quest.itemClaimed.AddRange(save.itemClaimed);

            generatedQuestByTileId[quest.sourceTileId] = quest;

            if (quest.isCancelled)
            {
                blockedQuestTileIds.Add(quest.sourceTileId);
                continue;
            }

            if (quest.isAccepted || (quest.isCompleted && !quest.completionPopupClosed))
                activeAcceptedQuests.Add(quest);
        }

        RefreshQuestListUI();
    }

    private WorldQuestDefinition FindQuestDefinitionById(string questId)
    {
        if (string.IsNullOrWhiteSpace(questId))
            return null;

        for (int i = 0; i < questDefinitions.Count; i++)
        {
            WorldQuestDefinition def = questDefinitions[i];
            if (def != null && def.questId == questId)
                return def;
        }

        return null;
    }
    private void PostProgressRefresh()
    {
        RefreshQuestListUI();

        for (int i = 0; i < activeAcceptedQuests.Count; i++)
        {
            WorldQuestState quest = activeAcceptedQuests[i];
            if (quest == null || !quest.isCompleted || quest.completionPopupQueued || quest.completionPopupShown)
                continue;

            quest.completionPopupQueued = true;
            StartCoroutine(QueueCompletionAfterDelay(quest));
        }
    }

    private IEnumerator QueueCompletionAfterDelay(WorldQuestState quest)
    {
        yield return new WaitForSeconds(completionPopupDelay);

        if (quest == null || quest.isCancelled || !quest.isCompleted)
            yield break;

        quest.completionPopupQueued = false;
        quest.completionPopupShown = false;
    }

    private void RefreshQuestListUI()
    {
        if (questListPanelUI != null)
            questListPanelUI.Refresh();
    }

    private void OpenConfirmPopup(
        WorldQuestState quest,
        ConfirmPopupMode mode,
        string message,
        string confirmLabel,
        string closeLabel)
    {
        if (quest == null)
            return;

        pendingConfirmQuest = quest;
        pendingConfirmMode = mode;

        if (abandonConfirmPopupUI != null)
        {
            abandonConfirmPopupUI.Show(
                message,
                confirmLabel,
                closeLabel,
                HandleConfirmPopupConfirmed,
                HandleConfirmPopupCancelled);
        }
    }

    private void HandleConfirmPopupConfirmed()
    {
        WorldQuestState quest = pendingConfirmQuest;
        ConfirmPopupMode mode = pendingConfirmMode;

        pendingConfirmQuest = null;
        pendingConfirmMode = ConfirmPopupMode.None;

        switch (mode)
        {
            case ConfirmPopupMode.AbandonQuest:
                ConfirmQuestAbandon(quest);
                break;

            case ConfirmPopupMode.IgnoreUnclaimedRewardsAndClose:
                if (quest != null)
                {
                    FinalizeCompletedQuestClose(quest);
                    HidePopupOnly();
                    RefreshQuestListUI();
                    TryShowQueuedCompletionPopup();
                }
                break;
        }
    }

    private void HandleConfirmPopupCancelled()
    {
        pendingConfirmQuest = null;
        pendingConfirmMode = ConfirmPopupMode.None;
    }

    private void HidePopupOnly()
    {
        currentPopupQuest = null;
        currentPopupMode = WorldQuestPopupMode.None;

        if (questPopupUI != null)
            questPopupUI.Hide();
    }

    private void FinalizeCompletedQuestClose(WorldQuestState quest)
    {
        if (quest == null)
            return;

        quest.completionPopupClosed = true;
        activeAcceptedQuests.Remove(quest);
        delayedImmediateRewardScheduled.Remove(quest);
    }

    private void ScheduleImmediateRewardsForQuest(WorldQuestState quest)
    {
        if (quest == null || quest.definition == null)
            return;

        bool hasImmediateRewards =
            (!quest.soulGranted && quest.definition.soulReward > 0) ||
            (!quest.experienceGranted && quest.definition.experienceReward > 0);

        if (!hasImmediateRewards)
            return;

        if (delayedImmediateRewardScheduled.Contains(quest))
            return;

        delayedImmediateRewardScheduled.Add(quest);
        StartCoroutine(GrantImmediateRewardsAfterDelay(quest));
    }

    private IEnumerator GrantImmediateRewardsAfterDelay(WorldQuestState quest)
    {
        yield return new WaitForSeconds(immediateRewardGrantDelay);

        delayedImmediateRewardScheduled.Remove(quest);

        if (quest == null || quest.definition == null || quest.isCancelled)
            yield break;

        if (!quest.soulGranted)
        {
            int soul = Mathf.Max(0, quest.definition.soulReward);
            if (soul > 0)
                TryGrantSoulReward(soul);

            quest.soulGranted = true;
        }

        if (!quest.experienceGranted)
        {
            int exp = Mathf.Max(0, quest.definition.experienceReward);
            if (exp > 0)
                TryGrantPartyExperienceReward(exp);

            quest.experienceGranted = true;
        }

        if (questPopupUI != null && quest == currentPopupQuest && currentPopupMode == WorldQuestPopupMode.Completed)
            questPopupUI.ShowCompleted(quest);
    }

    private WorldQuestDefinition PickQuestDefinition(WorldTileData sourceTile, WorldMapData mapData)
    {
        List<WorldQuestDefinition> candidates = new List<WorldQuestDefinition>();

        for (int i = 0; i < questDefinitions.Count; i++)
        {
            WorldQuestDefinition def = questDefinitions[i];
            if (def == null || !def.enabled)
                continue;

            if (IsDefinitionValidForMap(def, mapData, sourceTile))
                candidates.Add(def);
        }

        if (candidates.Count == 0)
            return null;

        return candidates[Random.Range(0, candidates.Count)];
    }

    private bool IsDefinitionValidForMap(WorldQuestDefinition def, WorldMapData mapData, WorldTileData sourceTile)
    {
        if (def == null || mapData == null)
            return false;

        switch (def.questType)
        {
            case WorldQuestType.KillEnemies:
                return true;

            case WorldQuestType.CaptureSpecificTile:
                return PickTargetCaptureTileId(mapData, sourceTile != null ? sourceTile.tileId : -1) >= 0;

            case WorldQuestType.WinEliteBattle:
                return HasRemainingEventTile(mapData, WorldTileEventType.EliteBattle);

            case WorldQuestType.WinBossBattle:
                return HasRemainingEventTile(mapData, WorldTileEventType.Boss);

            default:
                return false;
        }
    }

    private bool HasRemainingEventTile(WorldMapData mapData, WorldTileEventType eventType)
    {
        IReadOnlyList<WorldTileData> tiles = mapData.Tiles;
        for (int i = 0; i < tiles.Count; i++)
        {
            WorldTileData tile = tiles[i];
            if (tile == null)
                continue;

            if (tile.eventType == eventType && tile.currentOwner != FactionType.Player)
                return true;
        }

        return false;
    }

    private int PickTargetCaptureTileId(WorldMapData mapData, int sourceTileId)
    {
        List<int> candidates = new List<int>();
        IReadOnlyList<WorldTileData> tiles = mapData.Tiles;

        for (int i = 0; i < tiles.Count; i++)
        {
            WorldTileData tile = tiles[i];
            if (tile == null)
                continue;
            if (tile.tileId == sourceTileId)
                continue;
            if (tile.isPlayerStart)
                continue;
            if (tile.currentOwner == FactionType.Player)
                continue;

            candidates.Add(tile.tileId);
        }

        if (candidates.Count == 0)
            return -1;

        return candidates[Random.Range(0, candidates.Count)];
    }

    private bool TryGrantSoulReward(int amount)
    {
        if (runManager == null || amount <= 0)
            return false;

        string[] methodNames =
        {
            "AddPersistentSoul",
            "AddSoul",
            "GainSoul",
            "GrantSoulReward"
        };

        for (int i = 0; i < methodNames.Length; i++)
        {
            MethodInfo method = runManager.GetType().GetMethod(methodNames[i], BindingFlags.Public | BindingFlags.Instance);
            if (method == null)
                continue;

            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length == 1 && parameters[0].ParameterType == typeof(int))
            {
                method.Invoke(runManager, new object[] { amount });
                return true;
            }
        }

        Debug.Log($"[WorldQuestController] Soul reward granted (fallback log only): {amount}");
        return false;
    }

    private bool TryGrantPartyExperienceReward(int amount)
    {
        if (runManager == null || amount <= 0)
            return false;

        string[] methodNames =
        {
            "AddPartyExperienceToAllMembers",
            "GrantPartyExperienceReward",
            "AddPartyExperienceReward",
            "GainPartyExperience",
            "AddPartyExp"
        };

        for (int i = 0; i < methodNames.Length; i++)
        {
            MethodInfo method = runManager.GetType().GetMethod(methodNames[i], BindingFlags.Public | BindingFlags.Instance);
            if (method == null)
                continue;

            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length == 1 && parameters[0].ParameterType == typeof(int))
            {
                method.Invoke(runManager, new object[] { amount });
                return true;
            }
        }

        Debug.LogWarning($"[WorldQuestController] Party EXP reward method not found on WorldRunManager. Reward amount={amount}");
        return false;
    }

    private bool TryGrantItemReward(ItemDefinition item, int amount)
    {
        if (runManager == null || item == null || amount <= 0)
            return false;

        string[] methodNames =
        {
            "TryAddItemToStorage",
            "AddItemToStorage",
            "GrantStorageItem",
            "AddStorageItem"
        };

        for (int i = 0; i < methodNames.Length; i++)
        {
            MethodInfo method = runManager.GetType().GetMethod(methodNames[i], BindingFlags.Public | BindingFlags.Instance);
            if (method == null)
                continue;

            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length == 2 &&
                parameters[0].ParameterType == typeof(ItemDefinition) &&
                parameters[1].ParameterType == typeof(int))
            {
                object result = method.Invoke(runManager, new object[] { item, amount });
                if (method.ReturnType == typeof(bool))
                    return (bool)result;

                return true;
            }
        }

        Debug.Log($"[WorldQuestController] Item reward granted (fallback log only): {item.name} x{amount}");
        return false;
    }

    private void TryInvokeRunManagerMethod(string methodName)
    {
        if (runManager == null || string.IsNullOrEmpty(methodName))
            return;

        MethodInfo method = runManager.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
        if (method == null || method.GetParameters().Length != 0)
            return;

        method.Invoke(runManager, null);
    }
}