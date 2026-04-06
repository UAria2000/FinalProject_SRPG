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

        Debug.LogError("[HexWorldGenerator] Failed to generate a valid world map within the retry limit.");
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

        List<WorldTileData> nonStartTiles = new List<WorldTileData>();
        for (int i = 0; i < mapData.tiles.Count; i++)
        {
            if (mapData.tiles[i] != null && !mapData.tiles[i].isPlayerStart)
                nonStartTiles.Add(mapData.tiles[i]);
        }

        int[] allocations = CreateFactionAllocations(nonStartTiles.Count, enemyFactions.Count);
        if (!AssignFactionTerritories(mapData, startTile, enemyFactions, allocations))
            return null;

        if (!AssignEvents(mapData, startTile, enemyFactions))
            return null;

        mapData.RebuildLookup();
        return mapData;
    }

    private WorldMapData CreateBaseMap()
    {
        WorldMapData mapData = new WorldMapData();
        mapData.radius = settings.radius;

        int nextId = 0;
        for (int q = -settings.radius + 1; q <= settings.radius - 1; q++)
        {
            int rMin = Mathf.Max(-settings.radius + 1, -q - settings.radius + 1);
            int rMax = Mathf.Min(settings.radius - 1, -q + settings.radius - 1);

            for (int r = rMin; r <= rMax; r++)
            {
                WorldTileData tile = new WorldTileData
                {
                    tileId = nextId++,
                    coord = new HexCoord(q, r),
                    eventType = WorldTileEventType.None,
                    nativeFaction = FactionType.None,
                    currentOwner = FactionType.None,
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
        if (factionCount <= 0)
            return result;

        int baseCount = availableTileCount / factionCount;
        int remainder = availableTileCount % factionCount;

        for (int i = 0; i < factionCount; i++)
            result[i] = baseCount;

        List<int> indices = new List<int>();
        for (int i = 0; i < factionCount; i++)
            indices.Add(i);

        Shuffle(indices);
        for (int i = 0; i < remainder; i++)
            result[indices[i]]++;

        return result;
    }

    private bool AssignFactionTerritories(WorldMapData mapData, WorldTileData startTile, List<FactionType> enemyFactions, int[] allocations)
    {
        List<WorldTileData> candidates = new List<WorldTileData>();
        for (int i = 0; i < mapData.tiles.Count; i++)
        {
            WorldTileData tile = mapData.tiles[i];
            if (tile != null && !tile.isPlayerStart)
                candidates.Add(tile);
        }

        Shuffle(candidates);

        Dictionary<FactionType, List<WorldTileData>> territoryByFaction = new Dictionary<FactionType, List<WorldTileData>>();
        Dictionary<FactionType, HashSet<int>> territoryIdsByFaction = new Dictionary<FactionType, HashSet<int>>();

        for (int i = 0; i < enemyFactions.Count; i++)
        {
            territoryByFaction[enemyFactions[i]] = new List<WorldTileData>();
            territoryIdsByFaction[enemyFactions[i]] = new HashSet<int>();
        }

        List<WorldTileData> availableSeeds = new List<WorldTileData>(candidates);
        for (int i = 0; i < enemyFactions.Count; i++)
        {
            if (allocations[i] <= 0)
                return false;

            if (availableSeeds.Count <= 0)
                return false;

            WorldTileData seed = availableSeeds[0];
            availableSeeds.RemoveAt(0);
            AssignTileToFaction(seed, enemyFactions[i], territoryByFaction, territoryIdsByFaction);
        }

        int totalAssigned = enemyFactions.Count;
        int totalNeeded = candidates.Count;
        int safeGuard = totalNeeded * 30;

        while (totalAssigned < totalNeeded && safeGuard-- > 0)
        {
            bool progress = false;
            List<int> factionIndices = new List<int>();
            for (int i = 0; i < enemyFactions.Count; i++)
                factionIndices.Add(i);
            Shuffle(factionIndices);

            for (int orderIndex = 0; orderIndex < factionIndices.Count; orderIndex++)
            {
                int factionIndex = factionIndices[orderIndex];
                FactionType factionType = enemyFactions[factionIndex];
                if (territoryByFaction[factionType].Count >= allocations[factionIndex])
                    continue;

                List<WorldTileData> frontier = BuildFrontier(mapData, territoryByFaction[factionType], territoryIdsByFaction[factionType]);
                if (frontier.Count <= 0)
                    continue;

                Shuffle(frontier);
                WorldTileData selected = frontier[0];
                AssignTileToFaction(selected, factionType, territoryByFaction, territoryIdsByFaction);
                totalAssigned++;
                progress = true;
            }

            if (!progress)
                return false;
        }

        if (totalAssigned != totalNeeded)
            return false;

        return true;
    }

    private List<WorldTileData> BuildFrontier(WorldMapData mapData, List<WorldTileData> territory, HashSet<int> territoryIds)
    {
        List<WorldTileData> result = new List<WorldTileData>();
        HashSet<int> added = new HashSet<int>();

        for (int i = 0; i < territory.Count; i++)
        {
            List<WorldTileData> neighbors = mapData.GetNeighbors(territory[i]);
            for (int j = 0; j < neighbors.Count; j++)
            {
                WorldTileData neighbor = neighbors[j];
                if (neighbor == null || neighbor.isPlayerStart)
                    continue;
                if (neighbor.nativeFaction != FactionType.None)
                    continue;
                if (added.Contains(neighbor.tileId) || territoryIds.Contains(neighbor.tileId))
                    continue;

                added.Add(neighbor.tileId);
                result.Add(neighbor);
            }
        }

        return result;
    }

    private void AssignTileToFaction(
        WorldTileData tile,
        FactionType factionType,
        Dictionary<FactionType, List<WorldTileData>> territoryByFaction,
        Dictionary<FactionType, HashSet<int>> territoryIdsByFaction)
    {
        tile.nativeFaction = factionType;
        tile.currentOwner = factionType;
        territoryByFaction[factionType].Add(tile);
        territoryIdsByFaction[factionType].Add(tile.tileId);
    }

    private bool AssignEvents(WorldMapData mapData, WorldTileData startTile, List<FactionType> enemyFactions)
    {
        HashSet<int> blockedNearCenter = new HashSet<int>();
        List<WorldTileData> centerNeighbors = mapData.GetNeighbors(startTile);
        for (int i = 0; i < centerNeighbors.Count; i++)
            blockedNearCenter.Add(centerNeighbors[i].tileId);

        List<WorldTileData> allEnemyTiles = new List<WorldTileData>();
        for (int i = 0; i < mapData.tiles.Count; i++)
        {
            WorldTileData tile = mapData.tiles[i];
            if (tile != null && !tile.isPlayerStart)
                allEnemyTiles.Add(tile);
        }

        for (int i = 0; i < enemyFactions.Count; i++)
        {
            List<WorldTileData> factionTiles = mapData.GetTilesByNativeFaction(enemyFactions[i]);
            Shuffle(factionTiles);

            WorldTileData bossTile = null;
            for (int j = 0; j < factionTiles.Count; j++)
            {
                if (settings.forbidBossNearCenter && blockedNearCenter.Contains(factionTiles[j].tileId))
                    continue;

                bossTile = factionTiles[j];
                break;
            }

            if (bossTile == null)
                return false;

            bossTile.eventType = WorldTileEventType.Boss;
            bossTile.previewEnemyPortraits = CreateEnemyPreviewList(enemyFactions[i], WorldTileEventType.Boss);
        }

        List<WorldTileData> graveyardCandidates = new List<WorldTileData>();
        for (int i = 0; i < allEnemyTiles.Count; i++)
        {
            if (allEnemyTiles[i].eventType == WorldTileEventType.None)
                graveyardCandidates.Add(allEnemyTiles[i]);
        }

        if (graveyardCandidates.Count < settings.graveyardCount)
            return false;

        Shuffle(graveyardCandidates);
        for (int i = 0; i < settings.graveyardCount; i++)
            graveyardCandidates[i].eventType = WorldTileEventType.Graveyard;

        for (int i = 0; i < allEnemyTiles.Count; i++)
        {
            WorldTileData tile = allEnemyTiles[i];
            if (tile.eventType != WorldTileEventType.None)
                continue;

            WorldTileEventType assignedType = PickWeightedEventType(tile, blockedNearCenter);
            tile.eventType = assignedType;

            if (tile.eventType.IsCombatEvent())
                tile.previewEnemyPortraits = CreateEnemyPreviewList(tile.nativeFaction, tile.eventType);
        }

        return true;
    }

    private WorldTileEventType PickWeightedEventType(WorldTileData tile, HashSet<int> blockedNearCenter)
    {
        List<WorldEventWeightEntry> sourceEntries = new List<WorldEventWeightEntry>();
        if (settings.eventWeightSettings != null && settings.eventWeightSettings.Entries != null)
        {
            for (int i = 0; i < settings.eventWeightSettings.Entries.Count; i++)
                sourceEntries.Add(settings.eventWeightSettings.Entries[i]);
        }

        if (sourceEntries.Count <= 0)
            return WorldTileEventType.Battle;

        float totalWeight = 0f;
        List<WorldEventWeightEntry> filteredEntries = new List<WorldEventWeightEntry>();
        bool isNearCenter = blockedNearCenter.Contains(tile.tileId);

        for (int i = 0; i < sourceEntries.Count; i++)
        {
            WorldEventWeightEntry entry = sourceEntries[i];
            if (entry == null || entry.weight <= 0f)
                continue;

            if (entry.eventType == WorldTileEventType.Boss || entry.eventType == WorldTileEventType.Graveyard)
                continue;

            if (settings.forbidEliteNearCenter && isNearCenter && entry.eventType == WorldTileEventType.EliteBattle)
                continue;

            filteredEntries.Add(entry);
            totalWeight += entry.weight;
        }

        if (filteredEntries.Count <= 0 || totalWeight <= 0f)
            return WorldTileEventType.Battle;

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        for (int i = 0; i < filteredEntries.Count; i++)
        {
            cumulative += filteredEntries[i].weight;
            if (roll <= cumulative)
                return filteredEntries[i].eventType;
        }

        return filteredEntries[filteredEntries.Count - 1].eventType;
    }

    private List<Sprite> CreateEnemyPreviewList(FactionType factionType, WorldTileEventType eventType)
    {
        List<Sprite> pool = settings.GetPreviewPortraitPool(factionType, eventType);
        List<Sprite> result = new List<Sprite>();

        if (pool == null || pool.Count <= 0)
            return result;

        int minCount = Mathf.Max(1, settings.minCombatPreviewCount);
        int maxCount = Mathf.Max(minCount, settings.maxCombatPreviewCount);
        int previewCount = Random.Range(minCount, maxCount + 1);
        previewCount = Mathf.Min(previewCount, 6);

        List<Sprite> working = new List<Sprite>(pool);
        Shuffle(working);

        for (int i = 0; i < previewCount; i++)
        {
            if (working.Count > 0)
            {
                result.Add(working[0]);
                working.RemoveAt(0);
            }
            else
            {
                result.Add(pool[Random.Range(0, pool.Count)]);
            }
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

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[swapIndex];
            list[swapIndex] = temp;
        }
    }
}
