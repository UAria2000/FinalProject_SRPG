using System;
using System.Collections.Generic;
using UnityEngine;

public enum ExplorationRoomType
{
    Start,
    Battle,
    Treasure,
    Rest,
    Boss
}

[Serializable]
public class ExplorationNodeData
{
    public int nodeId;
    public Vector2Int coord;
    public ExplorationRoomType roomType;
    public bool revealed;
    public bool resolved;
    public List<int> neighborIds = new List<int>();
}

[Serializable]
public class ExplorationMapData
{
    public int width;
    public int height;
    public Vector2Int startCoord;
    public int currentNodeId;
    public int uniqueVisitedCount;
    public List<ExplorationNodeData> nodes = new List<ExplorationNodeData>();

    public ExplorationNodeData GetNodeById(int nodeId)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].nodeId == nodeId)
                return nodes[i];
        }

        return null;
    }

    public ExplorationNodeData GetNodeAt(Vector2Int coord)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].coord == coord)
                return nodes[i];
        }

        return null;
    }
}
