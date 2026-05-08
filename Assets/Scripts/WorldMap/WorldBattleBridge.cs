using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WorldBattleBridge : MonoBehaviour
{
    [Header("Battle References")]
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private RandomEnemyEncounterBootstrapper encounterBootstrapper;
    [SerializeField] private WorldQuestController questController;

    [Header("Transition Roots")]
    [SerializeField] private GameObject worldMapRoot;
    [SerializeField] private GameObject battleRoot;
    [SerializeField] private bool hideWorldMapDuringBattle = true;
    [SerializeField] private bool showBattleRootDuringBattle = true;
    [SerializeField] private bool waitOneFrameAfterBattleRootActivation = true;

    [Header("UI")]
    [SerializeField] private BattleResultPopupUI battleResultPopupUI;
    [SerializeField] private BattleRewardPopupUI battleRewardPopupUI;
    [SerializeField] private BattleOutcomeMessageUI outcomeMessageUI;
    [SerializeField] private WorldSettlementPopupUI worldSettlementPopupUI;

    [Header("Fade")]
    [SerializeField] private SimpleScreenFader screenFader;
    [SerializeField] private float battleEnterFadeOutDuration = 0.2f;
    [SerializeField] private float battleEnterFadeInDuration = 0.2f;
    [SerializeField] private float battleExitFadeOutDuration = 0.2f;
    [SerializeField] private float battleExitFadeInDuration = 0.2f;

    [Header("Elite Battle")]
    [SerializeField, Range(0, 100)] private int eliteEnemyAllStatsBuffPercent = 10;

    [Header("Scene Exit")]
    [SerializeField] private string titleSceneName = "Bootstrap";

    private WorldRunManager runManager;
    private WorldGenerationSettings settings;
    private WorldTileData pendingTile;
    private bool isBattleRunning;
    private bool subscribed;

    public bool IsBattleRunning => isBattleRunning;

    public void Initialize(WorldRunManager manager, WorldGenerationSettings generationSettings)
    {
        runManager = manager;
        settings = generationSettings;

        if (questController == null)
            questController = UnityEngine.Object.FindFirstObjectByType<WorldQuestController>();

        EnsureBattleEndedSubscription();
    }

    private void OnEnable() => EnsureBattleEndedSubscription();

    private void OnDisable()
    {
        if (battleManager != null && subscribed)
        {
            battleManager.BattleEnded -= HandleBattleEnded;
            subscribed = false;
        }
    }

    private void EnsureBattleEndedSubscription()
    {
        if (battleManager == null || subscribed)
            return;

        battleManager.BattleEnded += HandleBattleEnded;
        subscribed = true;
    }

    public bool StartBattleForTile(WorldTileData tile)
    {
        if (tile == null || !tile.IsCombatEvent)
            return false;
        if (battleManager == null || settings == null || runManager == null)
            return false;

        FactionBattleConfig config = settings.GetFactionBattleConfig(tile.nativeFaction);
        if (config == null)
        {
            Debug.LogWarning($"[WorldBattleBridge] No faction battle config found for {tile.nativeFaction}.");
            return false;
        }

        pendingTile = tile;
        isBattleRunning = true;
        StartCoroutine(BeginBattleRoutine(tile, config));
        return true;
    }

    public void OpenSettlementFromWorldMap(bool wasVictory)
    {
        if (runManager == null)
            return;

        StartCoroutine(OpenSettlementRoutine(wasVictory));
    }

    private IEnumerator BeginBattleRoutine(WorldTileData tile, FactionBattleConfig config)
    {
        if (screenFader != null)
            yield return screenFader.FadeOut(battleEnterFadeOutDuration);

        SetWorldBattleRoots(true);

        if (waitOneFrameAfterBattleRootActivation)
            yield return null;

        BattlePartyRuntimeState allyState = runManager.GetOrCreatePlayerPartyRuntimeState();
        battleManager.SetWorldRunManager(runManager);
        battleManager.SetAllyRuntimePartyState(allyState);
        battleManager.SetAllyRuntimeInventory(runManager.GetActiveWorldInventory());

        if (!PrepareEnemyParty(tile, config))
        {
            isBattleRunning = false;
            SetWorldBattleRoots(false);
            if (screenFader != null)
                yield return screenFader.FadeIn(battleEnterFadeInDuration);
            yield break;
        }

        battleManager.StartBattle();

        if (tile != null && tile.eventType == WorldTileEventType.EliteBattle)
            battleManager.ApplyElitePermanentBuffToEnemies(eliteEnemyAllStatsBuffPercent);

        if (screenFader != null)
            yield return screenFader.FadeIn(battleEnterFadeInDuration);
    }

    private bool PrepareEnemyParty(WorldTileData tile, FactionBattleConfig config)
    {
        int mainLevel = runManager != null ? runManager.GetMainCharacterLevelForEnemyScaling() : 1;
        WorldDifficulty difficulty = settings != null ? settings.difficulty : WorldDifficulty.Normal;

        if (tile.eventType == WorldTileEventType.Boss && config.bossPartyDefinition != null)
        {
            if (encounterBootstrapper != null)
            {
                encounterBootstrapper.GenerateAndApplyEnemyPartyFromPartyDefinition(config.bossPartyDefinition, mainLevel, difficulty);
                return true;
            }

            battleManager.SetEnemyPartyDefinition(config.bossPartyDefinition);
            return true;
        }

        WorldTileEventType encounterType = tile.eventType == WorldTileEventType.EliteBattle
            ? WorldTileEventType.Battle
            : tile.eventType;
        EnemyEncounterTable table = config.GetEncounterTable(encounterType, ResolveProgressTier(tile.nativeFaction));
        if (table == null || encounterBootstrapper == null)
            return false;

        encounterBootstrapper.GenerateAndApplyEnemyPartyFromTable(table, mainLevel, difficulty);
        return true;
    }

    private int ResolveProgressTier(FactionType faction)
    {
        if (runManager == null || runManager.MapData == null)
            return 0;

        var factionTiles = runManager.MapData.GetTilesByNativeFaction(faction);
        int total = 0;
        int conquered = 0;
        for (int i = 0; i < factionTiles.Count; i++)
        {
            WorldTileData tile = factionTiles[i];
            if (tile == null || tile.isPlayerStart)
                continue;
            total++;
            if (tile.currentOwner == FactionType.Player)
                conquered++;
        }
        if (total <= 0) return 0;
        float ratio = conquered / (float)total;
        if (ratio < 1f / 3f) return 0;
        if (ratio < 2f / 3f) return 1;
        return 2;
    }

    private void HandleBattleEnded(BattleResultType result)
    {
        if (!isBattleRunning)
            return;

        StartCoroutine(HandleBattleEndedRoutine(result));
    }

    private IEnumerator HandleBattleEndedRoutine(BattleResultType result)
    {
        isBattleRunning = false;

        yield return StartCoroutine(ShowBattleResultRoutine(result));

        if (screenFader != null)
            yield return screenFader.FadeOut(battleExitFadeOutDuration);

        SetWorldBattleRoots(false);

        if (runManager != null)
            runManager.RemoveDeadPartyMembersFromActiveParty();

        bool openSettlementAfterReturn = result == BattleResultType.WorldFailure;

        if (pendingTile != null && runManager != null)
        {
            if (result == BattleResultType.Victory)
                runManager.ResolveCombatVictory(pendingTile);
            else
                runManager.ResolveCombatDefeat(pendingTile, true);
        }

        pendingTile = null;

        if (screenFader != null)
            yield return screenFader.FadeIn(battleExitFadeInDuration);

        if (openSettlementAfterReturn)
            yield return StartCoroutine(OpenSettlementRoutine(false));
    }

    private IEnumerator ShowBattleResultRoutine(BattleResultType result)
    {
        BattleRewardSummary summary = battleManager != null ? battleManager.CurrentBattleRewardSummary : null;
        BattleResultPopupData popupData = BuildAndGrantBattleResultData(summary, result);

        if (battleResultPopupUI != null)
        {
            bool waiting = true;
            battleResultPopupUI.Open(popupData, () => waiting = false);
            while (waiting) yield return null;
            yield break;
        }

        if (battleRewardPopupUI != null && summary != null)
        {
            bool waiting = true;
            battleRewardPopupUI.Open(summary, () => waiting = false);
            while (waiting) yield return null;
            yield break;
        }

        if (outcomeMessageUI != null)
        {
            bool waiting = true;
            outcomeMessageUI.Open(popupData != null ? popupData.GetTitleOrDefault() : GetResultTitle(result), "전투완료", () => waiting = false);
            while (waiting) yield return null;
        }
    }

    private BattleResultPopupData BuildAndGrantBattleResultData(BattleRewardSummary summary, BattleResultType result)
    {
        if (summary == null)
            summary = new BattleRewardSummary();

        summary.resultType = result;
        ApplyRewardBonuses(summary);

        int totalExp = Mathf.Max(0, summary.expReward);
        int livingCount = runManager != null ? runManager.CountLivingActivePartyMembers() : 0;
        int perLivingExp = livingCount > 0 ? totalExp / livingCount : 0;

        BattleResultPopupData data = new BattleResultPopupData
        {
            resultType = result,
            title = GetResultTitle(result),
            soulReward = Mathf.Max(0, summary.soulReward),
            expRewardTotal = totalExp,
            expRewardPerLivingUnit = perLivingExp,
            defeatedOrCapturedEnemyCount = summary.DefeatedOrCapturedEnemyCount,
            baseSoulReward = summary.baseSoulReward,
            baseExpReward = summary.baseExpReward,
            totalBonusPercent = summary.rewardBonusPercent,
            worldSizeBonusPercent = summary.worldSizeBonusPercent,
            combatTypeBonusPercent = summary.combatTypeBonusPercent,
        };

        if (summary.capturedPrisonerRewards != null)
        {
            for (int i = 0; i < summary.capturedPrisonerRewards.Count && i < 4; i++)
                data.capturedPrisoners.Add(summary.capturedPrisonerRewards[i]);
        }

        List<BattleResultPartyMemberSnapshot> snapshots = CapturePartySnapshotsBefore(perLivingExp);

        if (summary.DefeatedOrCapturedEnemyCount > 0 && questController != null)
            questController.NotifyEnemyKilled(summary.DefeatedOrCapturedEnemyCount);

        if (runManager != null)
        {
            runManager.AddWorldSoul(summary.soulReward);
            runManager.ConvertCapturedPrisonerRewardsToRoster(summary.capturedPrisonerRewards);

            if (perLivingExp > 0)
                runManager.AddPartyExperienceToAllMembers(perLivingExp);
        }

        RefreshPartySnapshotsAfter(snapshots);

        for (int i = 0; i < snapshots.Count && i < 4; i++)
            data.partyMembers.Add(snapshots[i]);

        return data;
    }

    private void ApplyRewardBonuses(BattleRewardSummary summary)
    {
        if (summary == null)
            return;

        int sizeBonus = settings != null ? settings.GetBattleRewardSizeBonusPercent() : 0;
        int combatBonus = settings != null && pendingTile != null ? settings.GetBattleRewardCombatBonusPercent(pendingTile.eventType) : 0;
        int totalBonus = Mathf.Max(0, sizeBonus) + Mathf.Max(0, combatBonus);

        summary.worldSizeBonusPercent = Mathf.Max(0, sizeBonus);
        summary.combatTypeBonusPercent = Mathf.Max(0, combatBonus);
        summary.ApplyRewardBonus(totalBonus);
    }

    private List<BattleResultPartyMemberSnapshot> CapturePartySnapshotsBefore(int perLivingExp)
    {
        List<BattleResultPartyMemberSnapshot> snapshots = new List<BattleResultPartyMemberSnapshot>();
        BattlePartyRuntimeState runtime = runManager != null ? runManager.GetOrCreatePlayerPartyRuntimeState() : null;
        if (runtime == null || runtime.members == null)
            return snapshots;

        List<PartyMemberData> ordered = new List<PartyMemberData>();
        for (int i = 0; i < runtime.members.Count; i++)
        {
            if (runtime.members[i] != null)
                ordered.Add(runtime.members[i]);
        }

        ordered.Sort((a, b) => a.startSlotIndex.CompareTo(b.startSlotIndex));

        for (int i = 0; i < ordered.Count && i < 4; i++)
        {
            PartyMemberData member = ordered[i];
            bool isDead = member.persistentCurrentHP == 0;
            int level = Mathf.Max(1, member.currentLevel);
            int expToNext = LegionFormula.GetExpToNextLevel(level);

            snapshots.Add(new BattleResultPartyMemberSnapshot
            {
                memberData = member,
                unitDefinition = member.unitDefinition,
                unitViewDefinition = member.unitViewDefinition,
                displayName = member.GetDisplayName(),
                isDead = isDead,
                isExchangeable = member.isExchangeable,
                isNft = member.isNft || member.isExchangeable || (member.unitDefinition != null && member.unitDefinition.isNftUnit),
                promotionRank = member.promotionRank,
                levelBefore = level,
                levelAfter = level,
                originalLevel = Mathf.Max(1, member.originalLevel),
                expBefore = Mathf.Max(0, member.currentExp),
                expAfter = Mathf.Max(0, member.currentExp),
                expToNextBefore = expToNext,
                expToNextAfter = expToNext,
                gainedExp = isDead ? 0 : Mathf.Max(0, perLivingExp),
            });
        }

        return snapshots;
    }

    private void RefreshPartySnapshotsAfter(List<BattleResultPartyMemberSnapshot> snapshots)
    {
        if (snapshots == null || snapshots.Count == 0 || runManager == null)
            return;

        BattlePartyRuntimeState runtime = runManager.GetOrCreatePlayerPartyRuntimeState();
        if (runtime == null || runtime.members == null)
            return;

        for (int i = 0; i < snapshots.Count; i++)
        {
            BattleResultPartyMemberSnapshot snapshot = snapshots[i];
            if (snapshot == null || snapshot.memberData == null)
                continue;

            PartyMemberData after = FindRuntimeMemberByInstanceId(runtime, snapshot.memberData.instanceId);
            if (after == null)
                after = snapshot.memberData;

            snapshot.memberData = after;
            snapshot.levelAfter = Mathf.Max(1, after.currentLevel);
            snapshot.expAfter = Mathf.Max(0, after.currentExp);
            snapshot.expToNextAfter = LegionFormula.GetExpToNextLevel(snapshot.levelAfter);
            snapshot.originalLevel = Mathf.Max(1, after.originalLevel);
            snapshot.promotionRank = after.promotionRank;
            snapshot.isExchangeable = after.isExchangeable;
            snapshot.isNft = after.isNft || after.isExchangeable || (after.unitDefinition != null && after.unitDefinition.isNftUnit);
        }
    }

    private PartyMemberData FindRuntimeMemberByInstanceId(BattlePartyRuntimeState runtime, string instanceId)
    {
        if (runtime == null || runtime.members == null || string.IsNullOrWhiteSpace(instanceId))
            return null;

        for (int i = 0; i < runtime.members.Count; i++)
        {
            PartyMemberData member = runtime.members[i];
            if (member != null && member.instanceId == instanceId)
                return member;
        }

        return null;
    }

    private string GetResultTitle(BattleResultType result)
    {
        switch (result)
        {
            case BattleResultType.Victory:
                return "전투 승리";
            case BattleResultType.Flee:
                return "전투 이탈";
            case BattleResultType.Defeat:
            case BattleResultType.WorldFailure:
                return "전투 패배";
            default:
                return "전투 결과";
        }
    }

    private IEnumerator OpenSettlementRoutine(bool wasVictory)
    {
        if (runManager == null)
            yield break;

        WorldSettlementSummary summary = runManager.BuildSettlementSummary(wasVictory);
        if (worldSettlementPopupUI != null)
        {
            bool waiting = true;
            worldSettlementPopupUI.Open(summary, () =>
            {
                runManager.FinalizeWorldSettlement(summary);
                waiting = false;
                ReturnToTitleSceneIfAvailable();
            });
            while (waiting) yield return null;
        }
        else
        {
            runManager.FinalizeWorldSettlement(summary);
            ReturnToTitleSceneIfAvailable();
        }
    }

    private void ReturnToTitleSceneIfAvailable()
    {
        if (string.IsNullOrWhiteSpace(titleSceneName))
            return;

        if (Application.CanStreamedLevelBeLoaded(titleSceneName))
        {
            SceneManager.LoadScene(titleSceneName);
            return;
        }

        Debug.LogWarning($"[WorldBattleBridge] Scene '{titleSceneName}' is not in the active Build Profile or shared scene list. World settlement was finalized, but scene load was skipped.");
    }

    private void SetWorldBattleRoots(bool isInBattle)
    {
        if (worldMapRoot != null && hideWorldMapDuringBattle)
            worldMapRoot.SetActive(!isInBattle);
        if (battleRoot != null && showBattleRootDuringBattle)
            battleRoot.SetActive(isInBattle);
    }
}
