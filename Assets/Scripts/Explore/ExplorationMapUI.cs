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
    [SerializeField] private Vector2 cellSpacing = new Vector2(120f, 120f);
    [SerializeField] private Vector2 originOffset = new Vector2(60f, 60f);
    [SerializeField] private float lineThickness = 8f;

    [Header("Colors")]
    [SerializeField] private Color hiddenNodeColor = new Color(0.35f, 0.35f, 0.35f, 1f);
    [SerializeField] private Color battleColor = new Color(0.80f, 0.30f, 0.30f, 1f);
    [SerializeField] private Color treasureColor = new Color(0.92f, 0.74f, 0.20f, 1f);
    [SerializeField] private Color restColor = new Color(0.30f, 0.72f, 0.42f, 1f);
    [SerializeField] private Color bossColor = new Color(0.55f, 0.25f, 0.80f, 1f);
    [SerializeField] private Color startColor = new Color(0.20f, 0.56f, 0.95f, 1f);
    [SerializeField] private Color currentNodeTint = Color.white;
    [SerializeField] private Color reachableLineColor = new Color(1f, 1f, 1f, 0.95f);
    [SerializeField] private Color normalLineColor = new Color(1f, 1f, 1f, 0.25f);

    private ExplorationRunController controller;
    private ExplorationMapData mapData;
    private readonly Dictionary<int, ExplorationNodeButtonUI> nodeViews = new Dictionary<int, ExplorationNodeButtonUI>();
    private readonly List<ConnectionView> connectionViews = new List<ConnectionView>();

    public void Build(ExplorationRunController owner, ExplorationMapData data)
    {
        controller = owner;
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
        if (controller == null || mapData == null)
            return;

        ExplorationNodeData currentNode = mapData.GetNodeById(mapData.currentNodeId);

        for (int i = 0; i < mapData.nodes.Count; i++)
        {
            ExplorationNodeData node = mapData.nodes[i];
            ExplorationNodeButtonUI view;
            if (!nodeViews.TryGetValue(node.nodeId, out view) || view == null)
                continue;

            bool isCurrent = currentNode != null && currentNode.nodeId == node.nodeId;
            bool canMove = controller.CanMoveTo(node);

            view.SetLabel(GetNodeLabel(node));
            view.SetVisual(GetNodeColor(node, isCurrent), Color.white, canMove, isCurrent);
        }

        for (int i = 0; i < connectionViews.Count; i++)
        {
            ConnectionView line = connectionViews[i];
            if (line.image == null)
                continue;

            bool highlight = false;
            if (currentNode != null)
            {
                highlight =
                    (line.aId == currentNode.nodeId && currentNode.neighborIds.Contains(line.bId)) ||
                    (line.bId == currentNode.nodeId && currentNode.neighborIds.Contains(line.aId));
            }

            line.image.color = highlight ? reachableLineColor : normalLineColor;
        }
    }

    private void BuildNodes()
    {
        for (int i = 0; i < mapData.nodes.Count; i++)
        {
            ExplorationNodeData node = mapData.nodes[i];
            ExplorationNodeButtonUI view = Instantiate(nodePrefab, boardRoot);
            view.Initialize(node.nodeId, HandleNodeClicked);
            view.RectTransform.anchoredPosition = CoordToAnchoredPosition(node.coord);
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
                connectionViews.Add(new ConnectionView { aId = node.nodeId, bId = neighborId, image = line });
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
        return new Vector2(originOffset.x + coord.x * cellSpacing.x, originOffset.y + coord.y * cellSpacing.y);
    }

    private Color GetNodeColor(ExplorationNodeData node, bool isCurrent)
    {
        Color baseColor;
        if (!node.revealed && node.roomType != ExplorationRoomType.Start)
        {
            baseColor = hiddenNodeColor;
        }
        else
        {
            switch (node.roomType)
            {
                case ExplorationRoomType.Start: baseColor = startColor; break;
                case ExplorationRoomType.Battle: baseColor = battleColor; break;
                case ExplorationRoomType.Treasure: baseColor = treasureColor; break;
                case ExplorationRoomType.Rest: baseColor = restColor; break;
                case ExplorationRoomType.Boss: baseColor = bossColor; break;
                default: baseColor = hiddenNodeColor; break;
            }
        }

        return isCurrent ? Color.Lerp(baseColor, currentNodeTint, 0.35f) : baseColor;
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
        if (controller != null)
            controller.HandleNodeClicked(nodeId);
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
