using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class WorldQuestController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WorldRunManager runManager;
    [SerializeField] private WorldQuestListPanelUI questListPanelUI;
    [SerializeField] private WorldQuestPopupUI questPopupUI;

    [Header("Quest Definitions")]
    [SerializeField] private List<WorldQuestDefinition> questDefinitions = new List<WorldQuestDefinition>(4);

    [Header("Rules")]
    [SerializeField] private int maxActiveQuests = 5;
    [SerializeField] private float completionPopupDelay = 0.65f;

    private readonly Dictionary<int, WorldQuestState> generatedQuestByTileId = new Dictionary<int, WorldQuestState>();
    private readonly HashSet<int> blockedQuestTileIds = new HashSet<int>();
    private readonly List<WorldQuestState> activeAcceptedQuests = new List<WorldQuestState>();

    private WorldQuestState currentPopupQuest;
    private WorldQuestPopupMode currentPopupMode = WorldQuestPopupMode.None;

    public IReadOnlyList<WorldQuestState> ActiveAcceptedQuests => activeAcceptedQuests;

    private void Awake()
    {
        if (runManager == null)
            runManager = Object.FindFirstObjectByType<WorldRunManager>();
    }

    private void Start()
    {
        if (questListPanelUI != null)
            questListPanelUI.Bind(this);

        if (questPopupUI != null)
            questPopupUI.Initialize(this);

        RefreshQuestListUI();
    }

    public IReadOnlyList<WorldQuestState> GetVisibleQuestList()
    {
        return activeAcceptedQuests;
    }

    public bool HasReachedQuestLimit()
    {
        int count = 0;
        for (int i = 0; i < activeAcceptedQuests.Count; i++)
        {
            WorldQuestState q = activeAcceptedQuests[i];
            if (q != null && !q.isCancelled && !q.completionPopupClosed)
                count++;
        }

        return count >= maxActiveQuests;
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

        // 퀘스트 수락 = 타일 점령 허용 쪽 훅
        TryInvokeRunManagerMethod("HandleQuestAcceptedFromPopup");
        HidePopupOnly();
        RefreshQuestListUI();
    }

    public void RejectCurrentPopupQuest()
    {
        if (currentPopupQuest == null || currentPopupMode != WorldQuestPopupMode.Offer)
            return;

        // 퀘스트 거절 = 타일 점령 안 하고 복귀 훅
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
            EnsureSoulGranted(quest);

            if (questPopupUI != null)
                questPopupUI.ShowCompleted(quest);
        }
        else
        {
            currentPopupMode = WorldQuestPopupMode.Active;
            if (questPopupUI != null)
                questPopupUI.ShowActive(quest);
        }
    }

    public void CancelCurrentPopupQuest()
    {
        if (currentPopupQuest == null)
            return;

        CancelQuestFromList(currentPopupQuest);
        HidePopupOnly();
    }

    public void CancelQuestFromList(WorldQuestState quest)
    {
        if (quest == null || !quest.isAccepted || quest.isCompleted)
            return;

        quest.isAccepted = false;
        quest.isCancelled = true;
        quest.currentProgress = 0;

        blockedQuestTileIds.Add(quest.sourceTileId);
        activeAcceptedQuests.Remove(quest);

        RefreshQuestListUI();
    }

    public void CloseCurrentPopup()
    {
        if (currentPopupQuest != null && currentPopupMode == WorldQuestPopupMode.Completed)
            currentPopupQuest.completionPopupClosed = true;

        HidePopupOnly();
        RefreshQuestListUI();
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
        for (int i = 0; i < activeAcceptedQuests.Count; i++)
        {
            WorldQuestState quest = activeAcceptedQuests[i];
            if (quest == null)
                continue;

            if (!quest.isCompleted || quest.completionPopupClosed || quest.completionPopupShown)
                continue;

            quest.completionPopupShown = true;
            EnsureSoulGranted(quest);

            currentPopupQuest = quest;
            currentPopupMode = WorldQuestPopupMode.Completed;

            if (questPopupUI != null)
                questPopupUI.ShowCompleted(quest);

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

        if (currentPopupQuest.definition == null || currentPopupQuest.definition.itemRewards == null)
            return;

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

        if (questPopupUI != null)
            questPopupUI.ShowCompleted(currentPopupQuest);
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

        // 전투 중 팝업 금지. 월드맵으로 돌아온 뒤 별도 호출로 띄움.
        quest.completionPopupQueued = false;
        quest.completionPopupShown = false;
    }

    private void RefreshQuestListUI()
    {
        if (questListPanelUI != null)
            questListPanelUI.Refresh();
    }

    private void HidePopupOnly()
    {
        currentPopupQuest = null;
        currentPopupMode = WorldQuestPopupMode.None;

        if (questPopupUI != null)
            questPopupUI.Hide();
    }

    private void EnsureSoulGranted(WorldQuestState quest)
    {
        if (quest == null || quest.soulGranted || quest.definition == null)
            return;

        int soul = Mathf.Max(0, quest.definition.soulReward);
        if (soul > 0)
            TryGrantSoulReward(soul);

        quest.soulGranted = true;
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

        // 프로젝트마다 메서드명이 달라질 수 있어서 reflection fallback 사용
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