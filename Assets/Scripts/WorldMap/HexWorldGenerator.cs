using System.Collections.Generic;
using UnityEngine;

public class HexWorldGenerator
{
    private readonly WorldGenerationSettings settings;

    public HexWorldGenerator(WorldGenerationSettings settings)
    {
        this.settings = settings;
    }

    public WorldMapData Generate()
    {
        if (settings == null)
        {
            Debug.LogError("[HexWorldGenerator] WorldGenerationSettings is null.");
            return null;
        }

        List<FactionType> enemyFactions = GetValidEnemyFactions();
        if (enemyFactions.Count <= 0)
        {
            Debug.LogError("[HexWorldGenerator] At least one enemy faction is required.");
            return null;
        }

        for (int attempt = 0; attempt < Mathf.Max(1, settings.maxGenerationAttempts); attempt++)
        {
            WorldMapData mapData = TryGenerateOnce(enemyFactions);
            if (mapData != null)
                return mapData;
        }

        Debug.LogError("[HexWorldGenerator] Failed to generate a valid world map.");
        return null;
    }

    private WorldMapData TryGenerateOnce(List<FactionType> enemyFactions)
    {
        WorldMapData mapData = CreateBaseMap();
        if (mapData == null)
            return null;

        WorldTileData startTile = mapData.GetStartTile();
        if (startTile == null)
            return null;

        int[] allocations = CreateFactionAllocations(mapData.tiles.Count - 1, enemyFactions.Count);
        if (!AssignFactionTerritories(mapData, enemyFactions, allocations))
            return null;

        if (!AssignEvents(mapData, startTile, enemyFactions))
            return null;

        mapData.RebuildLookup();
        return mapData;
    }

    private WorldMapData CreateBaseMap()
    {
        WorldMapData mapData = new WorldMapData
        {
            radius = settings.radius,
        };

        int nextId = 0;
        int worldRadius = settings.radius - 1;

        for (int q = -worldRadius; q <= worldRadius; q++)
        {
            int rMin = Mathf.Max(-worldRadius, -q - worldRadius);
            int rMax = Mathf.Min(worldRadius, -q + worldRadius);

            for (int r = rMin; r <= rMax; r++)
            {
                WorldTileData tile = new WorldTileData
                {
                    tileId = nextId++,
                    coord = new HexCoord(q, r),
                    nativeFaction = FactionType.None,
                    currentOwner = FactionType.None,
                    eventType = WorldTileEventType.None,
                    revealed = false,
                    isPlayerStart = (q == 0 && r == 0),
                    isResolved = false,
                    isIconDisabled = false,
                };

                if (tile.isPlayerStart)
                {
                    tile.nativeFaction = FactionType.Player;
                    tile.currentOwner = FactionType.Player;
                    tile.revealed = true;
                    mapData.startTileId = tile.tileId;
                }

                mapData.tiles.Add(tile);
            }
        }

        mapData.RebuildLookup();
        return mapData;
    }

    private int[] CreateFactionAllocations(int availableTileCount, int factionCount)
    {
        int[] result = new int[factionCount];
        int baseCount = availableTileCount / factionCount;
        int remainder = availableTileCount % factionCount;

        for (int i = 0; i < factionCount; i++)
            result[i] = baseCount;

        List<int> indices = new List<int>(factionCount);
        for (int i = 0; i < factionCount; i++)
            indices.Add(i);

        Shuffle(indices);
        for (int i = 0; i < remainder; i++)
            result[indices[i]]++;

        return result;
    }

    private bool AssignFactionTerritories(WorldMapData mapData, List<FactionType> enemyFactions, int[] allocations)
    {
        List<WorldTileData> availableTiles = new List<WorldTileData>();
        for (int i = 0; i < mapData.tiles.Count; i++)
        {
            WorldTileData tile = mapData.tiles[i];
            if (tile != null && !tile.isPlayerStart)
                availableTiles.Add(tile);
        }

        Shuffle(availableTiles);

        Dictionary<FactionType, List<WorldTileData>> territories = new Dictionary<FactionType, List<WorldTileData>>();
        Dictionary<FactionType, HashSet<int>> territoryIds = new Dictionary<FactionType, HashSet<int>>();

        for (int i = 0; i < enemyFactions.Count; i++)
        {
            territories[enemyFactions[i]] = new List<WorldTileData>();
            territoryIds[enemyFactions[i]] = new HashSet<int>();
        }

        List<WorldTileData> unusedSeedCandidates = new List<WorldTileData>(availableTiles);
        for (int i = 0; i < enemyFactions.Count; i++)
        {
            if (allocations[i] <= 0 || unusedSeedCandidates.Count <= 0)
                return false;

            WorldTileData seed = unusedSeedCandidates[0];
            unusedSeedCandidates.RemoveAt(0);
            AssignTileToFaction(seed, enemyFactions[i], territories, territoryIds);
        }

        int assignedCount = enemyFactions.Count;
        int totalNeeded = availableTiles.Count;
        int guard = totalNeeded * 30;

        while (assignedCount < totalNeeded && guard-- > 0)
        {
            bool progress = false;
            List<int> factionOrder = new List<int>(enemyFactions.Count);
            for (int i = 0; i < enemyFactions.Count; i++)
                factionOrder.Add(i);
            Shuffle(factionOrder);

            for (int i = 0; i < factionOrder.Count; i++)
            {
                int factionIndex = factionOrder[i];
                FactionType faction = enemyFactions[factionIndex];
                if (territories[faction].Count >= allocations[factionIndex])
                    continue;

                WorldTileData nextTile = FindExpansionTile(mapData, territories[faction], territoryIds[faction]);
                if (nextTile == null)
                    continue;

                AssignTileToFaction(nextTile, faction, territories, territoryIds);
                assignedCount++;
                progress = true;
            }

            if (!progress)
                return false;
        }

        return assignedCount == totalNeeded;
    }

    private WorldTileData FindExpansionTile(WorldMapData mapData, List<WorldTileData> territory, HashSet<int> territoryIds)
    {
        List<WorldTileData> shuffledTerritory = new List<WorldTileData>(territory);
        Shuffle(shuffledTerritory);

        for (int i = 0; i < shuffledTerritory.Count; i++)
        {
            List<WorldTileData> neighbors = mapData.GetNeighbors(shuffledTerritory[i]);
            Shuffle(neighbors);
            for (int n = 0; n < neighbors.Count; n++)
            {
                WorldTileData candidate = neighbors[n];
                if (candidate.isPlayerStart)
                    continue;
                if (candidate.nativeFaction != FactionType.None)
                    continue;
                if (territoryIds.Contains(candidate.tileId))
                    continue;
                return candidate;
            }
        }

        return null;
    }

    private void AssignTileToFaction(
        WorldTileData tile,
        FactionType faction,
        Dictionary<FactionType, List<WorldTileData>> territories,
        Dictionary<FactionType, HashSet<int>> territoryIds)
    {
        tile.nativeFaction = faction;
        tile.currentOwner = faction;
        territories[faction].Add(tile);
        territoryIds[faction].Add(tile.tileId);
    }

    private bool AssignEvents(WorldMapData mapData, WorldTileData startTile, List<FactionType> enemyFactions)
    {
        List<WorldTileData> pool = new List<WorldTileData>();
        for (int i = 0; i < mapData.tiles.Count; i++)
        {
            WorldTileData tile = mapData.tiles[i];
            if (tile != null && !tile.isPlayerStart)
                pool.Add(tile);
        }

        HashSet<int> blockedNearCenterIds = new HashSet<int>();
        List<WorldTileData> startNeighbors = mapData.GetNeighbors(startTile);
        for (int i = 0; i < startNeighbors.Count; i++)
            blockedNearCenterIds.Add(startNeighbors[i].tileId);

        if (!AssignSingleGraveyard(pool, blockedNearCenterIds))
            return false;

        for (int i = 0; i < enemyFactions.Count; i++)
        {
            if (!AssignSingleBoss(mapData, enemyFactions[i], blockedNearCenterIds))
                return false;
        }

        return AssignWeightedEvents(mapData, blockedNearCenterIds);
    }

    private bool AssignSingleGraveyard(List<WorldTileData> pool, HashSet<int> blockedNearCenterIds)
    {
        List<WorldTileData> candidates = new List<WorldTileData>();
        for (int i = 0; i < pool.Count; i++)
        {
            WorldTileData tile = pool[i];
            if (tile.eventType != WorldTileEventType.None)
                continue;
            candidates.Add(tile);
        }

        if (candidates.Count == 0)
            return false;

        WorldTileData selected = candidates[Random.Range(0, candidates.Count)];
        selected.eventType = WorldTileEventType.Graveyard;
        selected.previewEnemyPortraits.Clear();
        return true;
    }

    private bool AssignSingleBoss(WorldMapData mapData, FactionType faction, HashSet<int> blockedNearCenterIds)
    {
        List<WorldTileData> candidates = new List<WorldTileData>();
        List<WorldTileData> factionTiles = mapData.GetTilesByNativeFaction(faction);

        for (int i = 0; i < factionTiles.Count; i++)
        {
            WorldTileData tile = factionTiles[i];
            if (tile.eventType != WorldTileEventType.None)
                continue;
            if (settings.forbidBossNearCenter && blockedNearCenterIds.Contains(tile.tileId))
                continue;
            candidates.Add(tile);
        }

        if (candidates.Count == 0)
            return false;

        WorldTileData selected = candidates[Random.Range(0, candidates.Count)];
        selected.eventType = WorldTileEventType.Boss;
        selected.previewEnemyPortraits = BuildEnemyPreviewList(faction, true);
        return true;
    }

    private bool AssignWeightedEvents(WorldMapData mapData, HashSet<int> blockedNearCenterIds)
    {
        WorldEventWeightSettings weights = settings.eventWeightSettings;
        if (weights == null)
        {
            Debug.LogError("[HexWorldGenerator] WorldEventWeightSettings is missing.");
            return false;
        }

        List<WorldTileData> unassigned = new List<WorldTileData>();
        for (int i = 0; i < mapData.tiles.Count; i++)
        {
            WorldTileData tile = mapData.tiles[i];
            if (tile != null && tile.eventType == WorldTileEventType.None && !tile.isPlayerStart)
                unassigned.Add(tile);
        }

        for (int i = 0; i < unassigned.Count; i++)
        {
            WorldTileData tile = unassigned[i];
            WorldTileEventType eventType = PickWeightedEventType(weights, tile, blockedNearCenterIds);
            tile.eventType = eventType;
            tile.previewEnemyPortraits = tile.IsCombatEvent ? BuildEnemyPreviewList(tile.nativeFaction, eventType == WorldTileEventType.Boss) : new List<Sprite>();
        }

        return true;
    }

    private WorldTileEventType PickWeightedEventType(WorldEventWeightSettings weights, WorldTileData tile, HashSet<int> blockedNearCenterIds)
    {
        List<WorldTileEventType> candidates = new List<WorldTileEventType>
        {
            WorldTileEventType.Battle,
            WorldTileEventType.Rest,
            WorldTileEventType.Treasure,
            WorldTileEventType.Merchant,
            WorldTileEventType.Quest,
            WorldTileEventType.EliteBattle,
        };

        int totalWeight = 0;
        List<int> weightValues = new List<int>(candidates.Count);

        for (int i = 0; i < candidates.Count; i++)
        {
            WorldTileEventType eventType = candidates[i];
            int weight = weights.GetWeight(eventType);

            if (eventType == WorldTileEventType.EliteBattle && settings.forbidEliteNearCenter && blockedNearCenterIds.Contains(tile.tileId))
                weight = 0;

            weightValues.Add(weight);
            totalWeight += weight;
        }

        if (totalWeight <= 0)
            return WorldTileEventType.Battle;

        int roll = Random.Range(0, totalWeight);
        int running = 0;

        for (int i = 0; i < candidates.Count; i++)
        {
            running += weightValues[i];
            if (roll < running)
                return candidates[i];
        }

        return WorldTileEventType.Battle;
    }

    private List<Sprite> BuildEnemyPreviewList(FactionType faction, bool isBoss)
    {
        List<Sprite> result = new List<Sprite>();
        IReadOnlyList<Sprite> pool = settings.GetFactionEnemyPortraitPool(faction);
        if (pool == null || pool.Count == 0)
            return result;

        int minCount = Mathf.Clamp(settings.enemyPortraitMinCount, 1, 6);
        int maxCount = Mathf.Clamp(settings.enemyPortraitMaxCount, minCount, 6);
        int count = isBoss ? 1 : Random.Range(minCount, maxCount + 1);

        for (int i = 0; i < count; i++)
        {
            Sprite sprite = pool[Random.Range(0, pool.Count)];
            result.Add(sprite);
        }

        return result;
    }

    private List<FactionType> GetValidEnemyFactions()
    {
        List<FactionType> result = new List<FactionType>();
        if (settings.enemyFactions == null)
            return result;

        for (int i = 0; i < settings.enemyFactions.Count; i++)
        {
            FactionType faction = settings.enemyFactions[i];
            if (faction == FactionType.None || faction == FactionType.Player)
                continue;
            if (!result.Contains(faction))
                result.Add(faction);
        }

        return result;
    }

    private void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
