using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ExplorationRunController : MonoBehaviour
{
    [Serializable]
    public class ExplorationZoneConfig
    {
        public string zoneName = "Zone";
        public Sprite backgroundSprite;
        public EnemyEncounterTable encounterTable;
    }

    [Header("Map Settings")]
    [SerializeField] private int mapWidth = 10;
    [SerializeField] private int mapHeight = 5;
    [SerializeField] private Vector2Int startCoord = new Vector2Int(0, 2);
    [SerializeField] private int roomCountExcludingStart = 20;
    [SerializeField] private bool generateOnStart = true;
    [SerializeField] private bool useRandomSeed = true;
    [SerializeField] private int fixedSeed = 12345;

    [Header("Battle Integration")]
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private RandomEnemyEncounterBootstrapper encounterBootstrapper;
    [SerializeField] private Image mainBackgroundImage;
    [SerializeField] private ExplorationZoneConfig[] zones = new ExplorationZoneConfig[3];
    [SerializeField] private Sprite bossBackgroundSprite;
    [SerializeField] private PartyDefinition bossPartyDefinition;

    [Header("UI")]
    [SerializeField] private ExplorationMapUI mapUI;
    [SerializeField] private SimpleScreenFader screenFader;
    [SerializeField] private float fadeDuration = 0.25f;

    [Header("Room Events")]
    [SerializeField] private UnityEvent onTreasureRoomFirstEnter;
    [SerializeField] private UnityEvent onRestRoomFirstEnter;
    [SerializeField] private UnityEvent onRevisitRoom;
    [SerializeField] private UnityEvent onNormalBattleVictory;
    [SerializeField] private UnityEvent onNormalBattleDefeat;
    [SerializeField] private UnityEvent onBossBattleVictory;
    [SerializeField] private UnityEvent onBossBattleDefeat;

    private ExplorationMapData mapData;
    private bool isBusy;
    private bool awaitingBattleResult;
    private bool awaitingBossResult;
    private int lastSeedUsed;

    public ExplorationMapData MapData => mapData;
    public int UniqueVisitedCount => mapData != null ? Mathf.Max(1, mapData.uniqueVisitedCount) : 1;
    public int CurrentZoneIndex => GetZoneIndexByVisitedCount(UniqueVisitedCount);

    private void OnEnable()
    {
        if (battleManager != null)
            battleManager.BattleFinished += HandleBattleFinished;
    }

    private void OnDisable()
    {
        if (battleManager != null)
            battleManager.BattleFinished -= HandleBattleFinished;
    }

    private void Start()
    {
        if (generateOnStart)
            GenerateNewRun();
    }

    public void GenerateNewRun()
    {
        int seed = useRandomSeed ? UnityEngine.Random.Range(int.MinValue, int.MaxValue) : fixedSeed;
        lastSeedUsed = seed;

        mapData = ExplorationMapGenerator.Generate(mapWidth, mapHeight, startCoord, roomCountExcludingStart, seed);
        ApplyBackgroundForCurrentProgress(false);

        if (mapUI != null)
            mapUI.Build(this, mapData);

        if (battleManager != null)
            battleManager.OnInventoryTogglePressed();

        Debug.Log($"[ExplorationRun] 새 탐색 맵 생성 완료. Seed={lastSeedUsed}");
    }

    public bool CanMoveTo(ExplorationNodeData targetNode)
    {
        if (isBusy || awaitingBattleResult || targetNode == null || mapData == null)
            return false;

        if (battleManager != null && battleManager.IsBattleInProgress)
            return false;

        ExplorationNodeData current = mapData.GetNodeById(mapData.currentNodeId);
        if (current == null || current.nodeId == targetNode.nodeId)
            return false;

        return current.neighborIds.Contains(targetNode.nodeId);
    }

    public void HandleNodeClicked(int nodeId)
    {
        if (mapData == null || isBusy)
            return;

        ExplorationNodeData targetNode = mapData.GetNodeById(nodeId);
        if (!CanMoveTo(targetNode))
            return;

        StartCoroutine(MoveToNodeRoutine(targetNode));
    }

    private IEnumerator MoveToNodeRoutine(ExplorationNodeData targetNode)
    {
        isBusy = true;

        bool firstEnter = !targetNode.resolved;
        int projectedVisitedCount = mapData.uniqueVisitedCount + (firstEnter ? 1 : 0);
        int previousZoneIndex = GetZoneIndexByVisitedCount(mapData.uniqueVisitedCount);
        int nextZoneIndex = GetZoneIndexByVisitedCount(projectedVisitedCount);

        if (screenFader != null)
            yield return StartCoroutine(screenFader.FadeOut(fadeDuration));

        mapData.currentNodeId = targetNode.nodeId;
        targetNode.revealed = true;

        if (firstEnter)
        {
            targetNode.resolved = true;
            mapData.uniqueVisitedCount = projectedVisitedCount;
        }

        bool zoneChanged = previousZoneIndex != nextZoneIndex;
        if (zoneChanged || targetNode.roomType == ExplorationRoomType.Boss)
            ApplyBackgroundForNode(targetNode, nextZoneIndex);

        if (battleManager != null)
            battleManager.OnInventoryTogglePressed();

        Coroutine fadeInRoutine = null;
        if (screenFader != null)
            fadeInRoutine = StartCoroutine(screenFader.FadeIn(fadeDuration));

        if (firstEnter)
            ResolveNode(targetNode);
        else
        {
            onRevisitRoom?.Invoke();
            Debug.Log($"[ExplorationRun] 재방문: {targetNode.roomType} / {targetNode.coord}");
        }

        if (fadeInRoutine != null)
            yield return fadeInRoutine;

        if (mapUI != null)
            mapUI.Refresh();

        isBusy = false;
    }

    private void ResolveNode(ExplorationNodeData node)
    {
        if (node == null)
            return;

        switch (node.roomType)
        {
            case ExplorationRoomType.Battle:
                StartNormalEncounterBattle();
                break;

            case ExplorationRoomType.Treasure:
                Debug.Log($"[ExplorationRun] 보물 이벤트 @ {node.coord}");
                onTreasureRoomFirstEnter?.Invoke();
                break;

            case ExplorationRoomType.Rest:
                Debug.Log($"[ExplorationRun] 휴식 이벤트 @ {node.coord}");
                onRestRoomFirstEnter?.Invoke();
                break;

            case ExplorationRoomType.Boss:
                StartBossEncounterBattle();
                break;
        }
    }

    private void StartNormalEncounterBattle()
    {
        if (battleManager == null)
        {
            Debug.LogWarning("[ExplorationRun] BattleManager reference is missing.");
            return;
        }

        if (encounterBootstrapper == null)
        {
            Debug.LogWarning("[ExplorationRun] RandomEnemyEncounterBootstrapper reference is missing.");
            return;
        }

        ExplorationZoneConfig zone = GetCurrentZoneConfig();
        if (zone == null || zone.encounterTable == null)
        {
            Debug.LogWarning("[ExplorationRun] 현재 구역의 EnemyEncounterTable이 비어 있습니다.");
            return;
        }

        encounterBootstrapper.GenerateAndApplyEnemyParty(zone.encounterTable);
        awaitingBattleResult = true;
        awaitingBossResult = false;
        battleManager.StartBattle();
        Debug.Log($"[ExplorationRun] 일반 전투 시작 / 구역 {CurrentZoneIndex + 1} / 방문 수 {UniqueVisitedCount}");
    }

    private void StartBossEncounterBattle()
    {
        if (battleManager == null)
        {
            Debug.LogWarning("[ExplorationRun] BattleManager reference is missing.");
            return;
        }

        if (bossPartyDefinition == null)
        {
            Debug.LogWarning("[ExplorationRun] bossPartyDefinition reference is missing.");
            return;
        }

        battleManager.SetEnemyPartyDefinition(bossPartyDefinition);
        awaitingBattleResult = true;
        awaitingBossResult = true;
        battleManager.StartBattle();
        Debug.Log($"[ExplorationRun] 보스 전투 시작 / 방문 수 {UniqueVisitedCount}");
    }

    private void HandleBattleFinished(BattleResultType result)
    {
        if (!awaitingBattleResult)
            return;

        bool wasBoss = awaitingBossResult;
        awaitingBattleResult = false;
        awaitingBossResult = false;

        if (wasBoss)
        {
            if (result == BattleResultType.Victory)
                onBossBattleVictory?.Invoke();
            else if (result == BattleResultType.Defeat)
                onBossBattleDefeat?.Invoke();
        }
        else
        {
            if (result == BattleResultType.Victory)
                onNormalBattleVictory?.Invoke();
            else if (result == BattleResultType.Defeat)
                onNormalBattleDefeat?.Invoke();
        }

        if (mapUI != null)
            mapUI.Refresh();
    }

    private void ApplyBackgroundForCurrentProgress(bool log)
    {
        ApplyBackgroundForZone(CurrentZoneIndex, log);
    }

    private void ApplyBackgroundForNode(ExplorationNodeData node, int zoneIndex)
    {
        if (node != null && node.roomType == ExplorationRoomType.Boss)
        {
            if (mainBackgroundImage != null && bossBackgroundSprite != null)
                mainBackgroundImage.sprite = bossBackgroundSprite;

            Debug.Log("[ExplorationRun] 보스 방 배경 적용");
            return;
        }

        ApplyBackgroundForZone(zoneIndex, true);
    }

    private void ApplyBackgroundForZone(int zoneIndex, bool log)
    {
        ExplorationZoneConfig zone = GetZoneConfig(zoneIndex);
        if (mainBackgroundImage != null && zone != null && zone.backgroundSprite != null)
            mainBackgroundImage.sprite = zone.backgroundSprite;

        if (log)
            Debug.Log($"[ExplorationRun] 구역 배경 적용: Zone {zoneIndex + 1}");
    }

    private ExplorationZoneConfig GetCurrentZoneConfig()
    {
        return GetZoneConfig(CurrentZoneIndex);
    }

    private ExplorationZoneConfig GetZoneConfig(int zoneIndex)
    {
        if (zones == null || zones.Length == 0)
            return null;

        zoneIndex = Mathf.Clamp(zoneIndex, 0, zones.Length - 1);
        return zones[zoneIndex];
    }

    private int GetZoneIndexByVisitedCount(int visitedCount)
    {
        int clamped = Mathf.Max(1, visitedCount);
        if (clamped <= 7)
            return 0;
        if (clamped <= 14)
            return 1;
        return 2;
    }
}
