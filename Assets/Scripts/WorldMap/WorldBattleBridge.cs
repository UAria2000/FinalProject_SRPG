using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldBattleBridge : MonoBehaviour
{
    [Header("Battle References")]
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private RandomEnemyEncounterBootstrapper encounterBootstrapper;

    [Header("Optional Roots")]
    [SerializeField] private GameObject worldMapRoot;
    [SerializeField] private GameObject battleRoot;
    [SerializeField] private bool hideWorldMapDuringBattle = true;
    [SerializeField] private bool showBattleRootDuringBattle = true;
    [SerializeField] private bool waitOneFrameAfterBattleRootActivation = true;

    private WorldRunManager runManager;
    private WorldGenerationSettings settings;
    private WorldTileData pendingTile;
    private bool isBattleRunning;

    public bool IsBattleRunning => isBattleRunning;

    public void Initialize(WorldRunManager manager, WorldGenerationSettings generationSettings)
    {
        runManager = manager;
        settings = generationSettings;
    }

    private void OnEnable()
    {
        if (battleManager != null)
            battleManager.BattleEnded += HandleBattleEnded;
    }

    private void OnDisable()
    {
        if (battleManager != null)
            battleManager.BattleEnded -= HandleBattleEnded;
    }

    public bool StartBattleForTile(WorldTileData tile)
    {
        if (tile == null || !tile.IsCombatEvent)
            return false;

        if (battleManager == null)
        {
            Debug.LogWarning("[WorldBattleBridge] BattleManager reference is missing.");
            return false;
        }

        if (settings == null || runManager == null)
        {
            Debug.LogWarning("[WorldBattleBridge] WorldRunManager or WorldGenerationSettings is missing.");
            return false;
        }

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

    private IEnumerator BeginBattleRoutine(WorldTileData tile, FactionBattleConfig config)
    {
        SetWorldBattleRoots(isInBattle: true);

        if (battleRoot != null && battleRoot.activeInHierarchy && waitOneFrameAfterBattleRootActivation)
            yield return null;

        if (!PrepareEnemyParty(tile, config))
        {
            isBattleRunning = false;
            SetWorldBattleRoots(isInBattle: false);
            yield break;
        }

        battleManager.StartBattle();
    }

    private bool PrepareEnemyParty(WorldTileData tile, FactionBattleConfig config)
    {
        if (tile.eventType == WorldTileEventType.Boss && config.bossPartyDefinition != null)
        {
            battleManager.SetEnemyPartyDefinition(config.bossPartyDefinition);
            return true;
        }

        EnemyEncounterTable table = config.GetEncounterTable(tile.eventType, ResolveProgressTier(tile.nativeFaction));
        if (table == null)
        {
            Debug.LogWarning($"[WorldBattleBridge] No encounter table configured for {tile.nativeFaction} / {tile.eventType}.");
            return false;
        }

        if (encounterBootstrapper == null)
        {
            Debug.LogWarning("[WorldBattleBridge] RandomEnemyEncounterBootstrapper reference is missing.");
            return false;
        }

        encounterBootstrapper.GenerateAndApplyEnemyPartyFromTable(table);
        return true;
    }

    private int ResolveProgressTier(FactionType faction)
    {
        if (runManager == null || runManager.MapData == null)
            return 0;

        List<WorldTileData> factionTiles = runManager.MapData.GetTilesByNativeFaction(faction);
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

        if (total <= 0)
            return 0;

        float ratio = conquered / (float)total;
        if (ratio < 1f / 3f)
            return 0;
        if (ratio < 2f / 3f)
            return 1;
        return 2;
    }

    private void HandleBattleEnded(BattleResultType result)
    {
        if (!isBattleRunning)
            return;

        SetWorldBattleRoots(isInBattle: false);
        isBattleRunning = false;

        if (pendingTile != null && runManager != null)
        {
            if (result == BattleResultType.Victory)
                runManager.ResolveCombatVictory(pendingTile);
            else
                runManager.ResolveCombatDefeat(pendingTile, true);
        }

        pendingTile = null;
    }

    private void SetWorldBattleRoots(bool isInBattle)
    {
        if (worldMapRoot != null && hideWorldMapDuringBattle)
            worldMapRoot.SetActive(!isInBattle);

        if (battleRoot != null && showBattleRootDuringBattle)
            battleRoot.SetActive(isInBattle);
    }
}
