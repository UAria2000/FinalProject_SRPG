using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ExplorationMapUI : MonoBehaviour
{
    [System.Serializable]
    private class ConnectionView
    {
        public int aId;
        public int bId;
        public Image image;
    }

    [Header("References")]
    [SerializeField] private RectTransform boardRoot;
    [SerializeField] private ExplorationNodeButtonUI nodePrefab;
    [SerializeField] private Image connectionPrefab;

    [Header("Layout")]
    [SerializeField] private Vector2 cellSpacing = new Vector2(80f, 40f);
    [SerializeField] private Vector2 originOffset = new Vector2(40f, 20f);
    [SerializeField] private Vector2 nodeSize = new Vector2(30f, 30f);
    [SerializeField] private float lineThickness = 5f;

    [Header("Colors")]
    [SerializeField] private Color hiddenNodeColor = new Color(0.35f, 0.35f, 0.35f, 1f);
    [SerializeField] private Color battleColor = new Color(0.78f, 0.32f, 0.32f, 1f);
    [SerializeField] private Color treasureColor = new Color(0.92f, 0.76f, 0.22f, 1f);
    [SerializeField] private Color restColor = new Color(0.36f, 0.7f, 0.46f, 1f);
    [SerializeField] private Color bossColor = new Color(0.6f, 0.24f, 0.82f, 1f);
    [SerializeField] private Color startColor = new Color(0.28f, 0.56f, 0.95f, 1f);
    [SerializeField] private Color reachableLineColor = new Color(1f, 1f, 1f, 0.95f);
    [SerializeField] private Color normalLineColor = new Color(1f, 1f, 1f, 0.25f);

    private ExplorationRunController runController;
    private ExplorationMapData mapData;
    private readonly Dictionary<int, ExplorationNodeButtonUI> nodeViews = new Dictionary<int, ExplorationNodeButtonUI>();
    private readonly List<ConnectionView> connectionViews = new List<ConnectionView>();

    public void Build(ExplorationRunController controller, ExplorationMapData data)
    {
        runController = controller;
        mapData = data;

        ClearAll();

        if (boardRoot == null || nodePrefab == null || connectionPrefab == null || mapData == null)
            return;

        BuildConnections();
        BuildNodes();
        Refresh();
    }

    public void Refresh()
    {
        if (mapData == null || runController == null)
            return;

        ExplorationNodeData currentNode = mapData.GetNodeById(mapData.currentNodeId);

        for (int i = 0; i < mapData.nodes.Count; i++)
        {
            ExplorationNodeData node = mapData.nodes[i];
            ExplorationNodeButtonUI view;
            if (!nodeViews.TryGetValue(node.nodeId, out view) || view == null)
                continue;

            bool isCurrent = currentNode != null && currentNode.nodeId == node.nodeId;
            bool isReachable = runController.CanMoveTo(node);

            string label = GetNodeLabel(node);

            if (isCurrent)
                label = "현재";
            else if (isReachable)
                label = "!";

            Color nodeColor = GetNodeColor(node);
            if (isCurrent)
                nodeColor = Color.cyan;
            else if (isReachable)
                nodeColor = Color.yellow;

            view.SetLabel(label);
            view.SetVisual(nodeColor, Color.black, isReachable, isCurrent);
        }

        for (int i = 0; i < connectionViews.Count; i++)
        {
            ConnectionView connection = connectionViews[i];
            if (connection.image == null)
                continue;

            bool highlight = false;
            if (currentNode != null)
            {
                highlight =
                    (connection.aId == currentNode.nodeId && currentNode.neighborIds.Contains(connection.bId)) ||
                    (connection.bId == currentNode.nodeId && currentNode.neighborIds.Contains(connection.aId));
            }

            connection.image.color = highlight ? reachableLineColor : normalLineColor;
        }
    }

    public void ShowMap(bool show)
    {
        gameObject.SetActive(show);
        if (show)
            Refresh();
    }

    private void BuildNodes()
    {
        for (int i = 0; i < mapData.nodes.Count; i++)
        {
            ExplorationNodeData node = mapData.nodes[i];
            ExplorationNodeButtonUI view = Instantiate(nodePrefab, boardRoot);

            RectTransform rt = view.RectTransform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = nodeSize;
            rt.anchoredPosition = CoordToAnchoredPosition(node.coord);

            view.Initialize(node.nodeId, HandleNodeClicked);
            nodeViews[node.nodeId] = view;
        }
    }

    private void BuildConnections()
    {
        HashSet<string> created = new HashSet<string>();

        for (int i = 0; i < mapData.nodes.Count; i++)
        {
            ExplorationNodeData node = mapData.nodes[i];

            for (int j = 0; j < node.neighborIds.Count; j++)
            {
                int neighborId = node.neighborIds[j];
                int minId = Mathf.Min(node.nodeId, neighborId);
                int maxId = Mathf.Max(node.nodeId, neighborId);
                string key = minId + "_" + maxId;

                if (created.Contains(key))
                    continue;
                created.Add(key);

                ExplorationNodeData neighbor = mapData.GetNodeById(neighborId);
                if (neighbor == null)
                    continue;

                Image line = Instantiate(connectionPrefab, boardRoot);
                SetupConnectionLine(line.rectTransform, node.coord, neighbor.coord);

                connectionViews.Add(new ConnectionView
                {
                    aId = node.nodeId,
                    bId = neighborId,
                    image = line
                });
            }
        }
    }

    private void SetupConnectionLine(RectTransform lineRect, Vector2Int fromCoord, Vector2Int toCoord)
    {
        Vector2 from = CoordToAnchoredPosition(fromCoord);
        Vector2 to = CoordToAnchoredPosition(toCoord);
        Vector2 delta = to - from;
        float length = delta.magnitude;

        lineRect.anchorMin = new Vector2(0f, 0f);
        lineRect.anchorMax = new Vector2(0f, 0f);
        lineRect.pivot = new Vector2(0f, 0.5f);
        lineRect.anchoredPosition = from;
        lineRect.sizeDelta = new Vector2(length, lineThickness);
        lineRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        lineRect.SetAsFirstSibling();
    }

    private Vector2 CoordToAnchoredPosition(Vector2Int coord)
    {
        return new Vector2(
            originOffset.x + coord.x * cellSpacing.x,
            originOffset.y + coord.y * cellSpacing.y);
    }

    private Color GetNodeColor(ExplorationNodeData node)
    {
        if (!node.revealed && node.roomType != ExplorationRoomType.Start)
            return hiddenNodeColor;

        switch (node.roomType)
        {
            case ExplorationRoomType.Start: return startColor;
            case ExplorationRoomType.Battle: return battleColor;
            case ExplorationRoomType.Treasure: return treasureColor;
            case ExplorationRoomType.Rest: return restColor;
            case ExplorationRoomType.Boss: return bossColor;
            default: return hiddenNodeColor;
        }
    }

    private string GetNodeLabel(ExplorationNodeData node)
    {
        if (!node.revealed && node.roomType != ExplorationRoomType.Start)
            return "?";

        switch (node.roomType)
        {
            case ExplorationRoomType.Start: return "시작";
            case ExplorationRoomType.Battle: return "전투";
            case ExplorationRoomType.Treasure: return "보물";
            case ExplorationRoomType.Rest: return "휴식";
            case ExplorationRoomType.Boss: return "보스";
            default: return "?";
        }
    }

    private void HandleNodeClicked(int nodeId)
    {
        if (runController != null)
            runController.HandleNodeClicked(nodeId);
    }

    private void ClearAll()
    {
        nodeViews.Clear();
        connectionViews.Clear();

        if (boardRoot == null)
            return;

        for (int i = boardRoot.childCount - 1; i >= 0; i--)
            Object.Destroy(boardRoot.GetChild(i).gameObject);
    }
}
