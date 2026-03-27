using System;
using System.Collections.Generic;
using UnityEngine;

public static class ExplorationMapGenerator
{
    private static readonly Vector2Int[] CardinalDirs =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    public static ExplorationMapData Generate(
        int width,
        int height,
        Vector2Int startCoord,
        int roomCountExcludingStart,
        int seed)
    {
        System.Random rng = seed == 0 ? new System.Random() : new System.Random(seed);

        for (int attempt = 0; attempt < 128; attempt++)
        {
            ExplorationMapData map = TryGenerate(width, height, startCoord, roomCountExcludingStart, rng);
            if (map != null)
                return map;
        }

        throw new Exception("맵 생성 실패: 조건을 만족하는 연결된 노드를 만들지 못했습니다.");
    }

    private static ExplorationMapData TryGenerate(
        int width,
        int height,
        Vector2Int startCoord,
        int roomCountExcludingStart,
        System.Random rng)
    {
        int totalNodeCount = roomCountExcludingStart + 1;
        HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();
        List<Vector2Int> orderedCoords = new List<Vector2Int>();

        occupied.Add(startCoord);
        orderedCoords.Add(startCoord);

        int guard = 0;
        while (occupied.Count < totalNodeCount && guard < 5000)
        {
            guard++;

            List<Vector2Int> expandable = new List<Vector2Int>();
            for (int i = 0; i < orderedCoords.Count; i++)
            {
                if (GetEmptyNeighbors(orderedCoords[i], occupied, width, height).Count > 0)
                    expandable.Add(orderedCoords[i]);
            }

            if (expandable.Count == 0)
                return null;

            Vector2Int from = expandable[rng.Next(expandable.Count)];
            List<Vector2Int> candidates = GetEmptyNeighbors(from, occupied, width, height);
            if (candidates.Count == 0)
                continue;

            Vector2Int picked = candidates[rng.Next(candidates.Count)];
            occupied.Add(picked);
            orderedCoords.Add(picked);
        }

        if (occupied.Count != totalNodeCount)
            return null;

        ExplorationMapData map = new ExplorationMapData();
        map.width = width;
        map.height = height;
        map.startCoord = startCoord;
        map.uniqueVisitedCount = 1;

        for (int i = 0; i < orderedCoords.Count; i++)
        {
            ExplorationNodeData node = new ExplorationNodeData();
            node.nodeId = i;
            node.coord = orderedCoords[i];
            node.roomType = ExplorationRoomType.Battle;
            map.nodes.Add(node);
        }

        BuildNeighborLinks(map);

        ExplorationNodeData startNode = map.GetNodeAt(startCoord);
        if (startNode == null)
            return null;

        startNode.roomType = ExplorationRoomType.Start;
        startNode.revealed = true;
        startNode.resolved = true;
        map.currentNodeId = startNode.nodeId;

        int bossNodeId = FindFarthestNodeIdFromStart(map, rng);
        ExplorationNodeData bossNode = map.GetNodeById(bossNodeId);
        if (bossNode == null || bossNode.roomType == ExplorationRoomType.Start)
            return null;

        bossNode.roomType = ExplorationRoomType.Boss;
        AssignNonBossRoomTypes(map, roomCountExcludingStart, rng);
        return map;
    }

    private static void BuildNeighborLinks(ExplorationMapData map)
    {
        for (int i = 0; i < map.nodes.Count; i++)
            map.nodes[i].neighborIds.Clear();

        for (int i = 0; i < map.nodes.Count; i++)
        {
            ExplorationNodeData node = map.nodes[i];
            for (int d = 0; d < CardinalDirs.Length; d++)
            {
                ExplorationNodeData neighbor = map.GetNodeAt(node.coord + CardinalDirs[d]);
                if (neighbor == null)
                    continue;

                if (!node.neighborIds.Contains(neighbor.nodeId))
                    node.neighborIds.Add(neighbor.nodeId);
            }
        }
    }

    private static int FindFarthestNodeIdFromStart(ExplorationMapData map, System.Random rng)
    {
        ExplorationNodeData startNode = map.GetNodeAt(map.startCoord);
        Dictionary<int, int> dist = new Dictionary<int, int>();
        Queue<int> queue = new Queue<int>();

        dist[startNode.nodeId] = 0;
        queue.Enqueue(startNode.nodeId);

        while (queue.Count > 0)
        {
            int id = queue.Dequeue();
            ExplorationNodeData node = map.GetNodeById(id);
            for (int i = 0; i < node.neighborIds.Count; i++)
            {
                int nextId = node.neighborIds[i];
                if (dist.ContainsKey(nextId))
                    continue;

                dist[nextId] = dist[id] + 1;
                queue.Enqueue(nextId);
            }
        }

        int maxDist = -1;
        List<int> farthest = new List<int>();
        for (int i = 0; i < map.nodes.Count; i++)
        {
            ExplorationNodeData node = map.nodes[i];
            if (node.roomType == ExplorationRoomType.Start)
                continue;

            int currentDist = dist.ContainsKey(node.nodeId) ? dist[node.nodeId] : -1;
            if (currentDist > maxDist)
            {
                maxDist = currentDist;
                farthest.Clear();
                farthest.Add(node.nodeId);
            }
            else if (currentDist == maxDist)
            {
                farthest.Add(node.nodeId);
            }
        }

        return farthest[rng.Next(farthest.Count)];
    }

    private static void AssignNonBossRoomTypes(ExplorationMapData map, int roomCountExcludingStart, System.Random rng)
    {
        int battleCount;
        int treasureCount;
        int restCount;
        PickCounts(roomCountExcludingStart, rng, out battleCount, out treasureCount, out restCount);

        List<ExplorationNodeData> assignable = new List<ExplorationNodeData>();
        for (int i = 0; i < map.nodes.Count; i++)
        {
            ExplorationNodeData node = map.nodes[i];
            if (node.roomType == ExplorationRoomType.Start || node.roomType == ExplorationRoomType.Boss)
                continue;

            assignable.Add(node);
        }

        Shuffle(assignable, rng);

        int index = 0;
        for (int i = 0; i < restCount; i++)
            assignable[index++].roomType = ExplorationRoomType.Rest;

        for (int i = 0; i < treasureCount; i++)
            assignable[index++].roomType = ExplorationRoomType.Treasure;

        for (int i = 0; i < battleCount && index < assignable.Count; i++)
            assignable[index++].roomType = ExplorationRoomType.Battle;
    }

    private static void PickCounts(int roomCountExcludingStart, System.Random rng, out int battle, out int treasure, out int rest)
    {
        int distributable = roomCountExcludingStart - 1; // boss 제외

        for (int attempt = 0; attempt < 128; attempt++)
        {
            battle = NextInclusive(rng, 12, 18);
            rest = NextInclusive(rng, 1, 2);
            treasure = distributable - battle - rest;

            if (treasure >= 1 && treasure <= 5)
                return;
        }

        battle = 16;
        rest = 1;
        treasure = 2;
    }

    private static List<Vector2Int> GetEmptyNeighbors(Vector2Int origin, HashSet<Vector2Int> occupied, int width, int height)
    {
        List<Vector2Int> result = new List<Vector2Int>();

        for (int i = 0; i < CardinalDirs.Length; i++)
        {
            Vector2Int next = origin + CardinalDirs[i];
            if (next.x < 0 || next.x >= width || next.y < 0 || next.y >= height)
                continue;
            if (occupied.Contains(next))
                continue;

            result.Add(next);
        }

        return result;
    }

    private static int NextInclusive(System.Random rng, int minInclusive, int maxInclusive)
    {
        return rng.Next(minInclusive, maxInclusive + 1);
    }

    private static void Shuffle<T>(List<T> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}
