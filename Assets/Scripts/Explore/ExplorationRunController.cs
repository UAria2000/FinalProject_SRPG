using System.Collections;
using UnityEngine;

public class ExplorationRunController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ExplorationMapUI mapUI;
    [SerializeField] private SimpleScreenFader screenFader;
    [SerializeField] private ExplorationBattleBridge battleBridge;
    [SerializeField] private BattleManager battleManager;

    [Header("Map Settings")]
    [SerializeField] private int mapWidth = 10;
    [SerializeField] private int mapHeight = 5;
    [SerializeField] private Vector2Int startCoord = new Vector2Int(0, 2);
    [SerializeField] private int roomCountExcludingStart = 20;
    [SerializeField] private bool useRandomSeed = true;
    [SerializeField] private int fixedSeed = 12345;
    [SerializeField] private bool showMapOnStart = true;

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 0.2f;

    private ExplorationMapData mapData;
    private bool isBusy;
    private int uniqueVisitedCount = 1; // 시작 노드 포함
    private int currentZoneIndex = 0;   // 0,1,2

    public ExplorationMapData MapData => mapData;
    public int UniqueVisitedCount => uniqueVisitedCount;
    public int CurrentZoneIndex => currentZoneIndex;

    private void Start()
    {
        GenerateNewMap();
    }

    public void GenerateNewMap()
    {
        if (battleManager != null)
            battleManager.ResetPersistentAllyPartyHPForNewMap();

        int seed = useRandomSeed ? Random.Range(int.MinValue, int.MaxValue) : fixedSeed;

        mapData = ExplorationMapGenerator.Generate(
            mapWidth,
            mapHeight,
            startCoord,
            roomCountExcludingStart,
            seed);

        uniqueVisitedCount = 1;
        currentZoneIndex = 0;
        isBusy = false;

        if (mapUI != null)
        {
            mapUI.Build(this, mapData);
            mapUI.gameObject.SetActive(showMapOnStart);
            mapUI.Refresh();
        }

        if (battleBridge != null)
            battleBridge.ApplyZoneBackground(currentZoneIndex);
    }

    public void OpenMap()
    {
        if (mapUI != null)
        {
            mapUI.gameObject.SetActive(true);
            mapUI.Refresh();
        }
    }

    public void CloseMap()
    {
        if (mapUI != null)
            mapUI.gameObject.SetActive(false);
    }

    public void ToggleMap()
    {
        if (mapUI != null)
        {
            bool next = !mapUI.gameObject.activeSelf;
            mapUI.gameObject.SetActive(next);

            if (next)
                mapUI.Refresh();
        }
    }

    public bool CanMoveTo(ExplorationNodeData targetNode)
    {
        if (mapData == null || targetNode == null)
            return false;

        // 전투 중에는 맵은 보여도 이동 불가
        if (battleManager != null && battleManager.IsBattleInProgress)
            return false;

        ExplorationNodeData currentNode = mapData.GetNodeById(mapData.currentNodeId);
        if (currentNode == null)
            return false;

        if (targetNode.nodeId == currentNode.nodeId)
            return false;

        return currentNode.neighborIds.Contains(targetNode.nodeId);
    }

    public void HandleNodeClicked(int nodeId)
    {
        if (isBusy || mapData == null)
            return;

        // 전투 중에는 노드 클릭 무시
        if (battleManager != null && battleManager.IsBattleInProgress)
            return;

        ExplorationNodeData targetNode = mapData.GetNodeById(nodeId);
        if (targetNode == null)
            return;

        if (!CanMoveTo(targetNode))
            return;

        StartCoroutine(MoveToNodeRoutine(targetNode));
    }

    public void OnReturnedToMap()
    {
        if (mapUI != null)
            mapUI.Refresh();
    }

    private IEnumerator MoveToNodeRoutine(ExplorationNodeData targetNode)
    {
        isBusy = true;
        bool firstEnter = !targetNode.resolved;

        if (screenFader != null)
            yield return StartCoroutine(screenFader.FadeOut(fadeDuration));

        mapData.currentNodeId = targetNode.nodeId;
        targetNode.revealed = true;

        if (firstEnter)
        {
            targetNode.resolved = true;
            uniqueVisitedCount++;

            int nextZoneIndex = GetZoneIndexFromVisitedCount(uniqueVisitedCount);
            bool zoneChanged = nextZoneIndex != currentZoneIndex;
            currentZoneIndex = nextZoneIndex;

            if (battleBridge != null)
            {
                if (targetNode.roomType == ExplorationRoomType.Boss)
                {
                    battleBridge.ApplyBossBackground();
                }
                else if (zoneChanged || uniqueVisitedCount == 2)
                {
                    battleBridge.ApplyZoneBackground(currentZoneIndex);
                }
            }
        }

        if (mapUI != null)
            mapUI.Refresh();

        if (screenFader != null)
            yield return StartCoroutine(screenFader.FadeIn(fadeDuration));

        if (firstEnter)
            ResolveNode(targetNode);

        isBusy = false;
    }

    private void ResolveNode(ExplorationNodeData node)
    {
        if (battleBridge == null || node == null)
            return;

        switch (node.roomType)
        {
            case ExplorationRoomType.Battle:
                battleBridge.StartNormalBattle(currentZoneIndex);
                break;

            case ExplorationRoomType.Treasure:
                battleBridge.OpenTreasureEvent(node);
                break;

            case ExplorationRoomType.Rest:
                battleBridge.OpenRestEvent(node);
                break;

            case ExplorationRoomType.Boss:
                battleBridge.StartBossBattle();
                break;
        }
    }

    private int GetZoneIndexFromVisitedCount(int visitedCount)
    {
        if (visitedCount <= 7)
            return 0;

        if (visitedCount <= 14)
            return 1;

        return 2;
    }
}
