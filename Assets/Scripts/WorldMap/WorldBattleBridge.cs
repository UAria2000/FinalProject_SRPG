using System.Collections;
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
    [SerializeField] private BattleRewardPopupUI battleRewardPopupUI;
    [SerializeField] private BattleOutcomeMessageUI outcomeMessageUI;
    [SerializeField] private WorldSettlementPopupUI worldSettlementPopupUI;

    [Header("Fade")]
    [SerializeField] private SimpleScreenFader screenFader;
    [SerializeField] private float battleEnterFadeOutDuration = 0.2f;
    [SerializeField] private float battleEnterFadeInDuration = 0.2f;
    [SerializeField] private float battleExitFadeOutDuration = 0.2f;
    [SerializeField] private float battleExitFadeInDuration = 0.2f;

    [Header("Scene Exit")]
    [SerializeField] private string titleSceneName = "Title";

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

        if (screenFader != null)
            yield return screenFader.FadeIn(battleEnterFadeInDuration);
    }

    private bool PrepareEnemyParty(WorldTileData tile, FactionBattleConfig config)
    {
        if (tile.eventType == WorldTileEventType.Boss && config.bossPartyDefinition != null)
        {
            battleManager.SetEnemyPartyDefinition(config.bossPartyDefinition);
            return true;
        }

        EnemyEncounterTable table = config.GetEncounterTable(tile.eventType, ResolveProgressTier(tile.nativeFaction));
        if (table == null || encounterBootstrapper == null)
            return false;

        encounterBootstrapper.GenerateAndApplyEnemyPartyFromTable(table);
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

        switch (result)
        {
            case BattleResultType.Victory:
                yield return StartCoroutine(ShowVictoryRewardRoutine());
                break;

            case BattleResultType.Flee:
                if (outcomeMessageUI != null)
                {
                    bool waiting = true;
                    outcomeMessageUI.Open("전투에서 도주했습니다.", "확인", () => waiting = false);
                    while (waiting) yield return null;
                }
                yield return StartCoroutine(ReturnToWorldAfterDefeatRoutine(true, false));
                break;

            case BattleResultType.WorldFailure:
                if (outcomeMessageUI != null)
                {
                    bool waitingFail = true;
                    outcomeMessageUI.Open("메인 캐릭터가 사망했습니다. 월드 정복 실패", "확인", () => waitingFail = false);
                    while (waitingFail) yield return null;
                }
                yield return StartCoroutine(ReturnToWorldAfterDefeatRoutine(true, true));
                break;

            default:
                yield return StartCoroutine(ReturnToWorldAfterDefeatRoutine(true, false));
                break;
        }
    }

    private IEnumerator ShowVictoryRewardRoutine()
    {
        BattleRewardSummary summary = battleManager != null ? battleManager.CurrentBattleRewardSummary : null;

        if (summary != null && questController != null)
        {
            int defeatedCount = summary.defeatedEnemyUnits != null ? summary.defeatedEnemyUnits.Count : 0;
            if (defeatedCount > 0)
                questController.NotifyEnemyKilled(defeatedCount);
        }

        if (summary != null && runManager != null)
        {
            runManager.AddWorldSoul(summary.soulReward);
            runManager.AddLootToWorldInventory(summary.droppedItems);
            runManager.AddCapturedPrisoners(summary.capturedPrisoners);
        }

        if (battleRewardPopupUI != null && summary != null)
        {
            bool waiting = true;
            battleRewardPopupUI.Open(summary, () => waiting = false);
            while (waiting) yield return null;
        }

        if (screenFader != null)
            yield return screenFader.FadeOut(battleExitFadeOutDuration);

        SetWorldBattleRoots(false);

        if (pendingTile != null && runManager != null)
            runManager.ResolveCombatVictory(pendingTile);

        pendingTile = null;

        if (screenFader != null)
            yield return screenFader.FadeIn(battleExitFadeInDuration);
    }

    private IEnumerator ReturnToWorldAfterDefeatRoutine(bool returnToStartTile, bool openSettlementAfterReturn)
    {
        if (screenFader != null)
            yield return screenFader.FadeOut(battleExitFadeOutDuration);

        SetWorldBattleRoots(false);

        if (pendingTile != null && runManager != null)
            runManager.ResolveCombatDefeat(pendingTile, returnToStartTile);

        pendingTile = null;

        if (screenFader != null)
            yield return screenFader.FadeIn(battleExitFadeInDuration);

        if (openSettlementAfterReturn)
            yield return StartCoroutine(OpenSettlementRoutine(false));
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
                if (!string.IsNullOrWhiteSpace(titleSceneName))
                    SceneManager.LoadScene(titleSceneName);
            });
            while (waiting) yield return null;
        }
        else
        {
            runManager.FinalizeWorldSettlement(summary);
            if (!string.IsNullOrWhiteSpace(titleSceneName))
                SceneManager.LoadScene(titleSceneName);
        }
    }

    private void SetWorldBattleRoots(bool isInBattle)
    {
        if (worldMapRoot != null && hideWorldMapDuringBattle)
            worldMapRoot.SetActive(!isInBattle);
        if (battleRoot != null && showBattleRootDuringBattle)
            battleRoot.SetActive(isInBattle);
    }
}