using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ExplorationBattleBridge : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ExplorationRunController runController;
    [SerializeField] private ExplorationMapUI mapUI;
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private RandomEnemyEncounterBootstrapper encounterBootstrapper;

    [Header("Main Background")]
    [SerializeField] private Image mainBackgroundImage;
    [SerializeField] private Sprite zone1Background;
    [SerializeField] private Sprite zone2Background;
    [SerializeField] private Sprite zone3Background;
    [SerializeField] private Sprite bossBackground;

    [Header("Encounter Tables")]
    [SerializeField] private EnemyEncounterTable zone1EncounterTable;
    [SerializeField] private EnemyEncounterTable zone2EncounterTable;
    [SerializeField] private EnemyEncounterTable zone3EncounterTable;

    [Header("Boss")]
    [SerializeField] private PartyDefinition bossPartyDefinition;

    [Header("Popup Events")]
    [SerializeField] private ExplorationSimpleEventPanel treasureEventPanel;
    [SerializeField] private string treasureTitle = "보물";
    [SerializeField] private string treasureBody = "보물을 발견했다.";
    [SerializeField] private ExplorationSimpleEventPanel restEventPanel;
    [SerializeField] private string restTitle = "휴식";
    [SerializeField] private string restBody = "잠시 숨을 고르고 체력을 정비했다.";

    [Header("Optional Hooks")]
    [SerializeField] private UnityEvent onBeforeBattleStart;
    [SerializeField] private UnityEvent onReturnToMap;

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

    public void ApplyZoneBackground(int zoneIndex)
    {
        if (mainBackgroundImage == null)
            return;

        switch (zoneIndex)
        {
            case 0:
                if (zone1Background != null) mainBackgroundImage.sprite = zone1Background;
                break;
            case 1:
                if (zone2Background != null) mainBackgroundImage.sprite = zone2Background;
                break;
            case 2:
                if (zone3Background != null) mainBackgroundImage.sprite = zone3Background;
                break;
        }
    }

    public void ApplyBossBackground()
    {
        if (mainBackgroundImage != null && bossBackground != null)
            mainBackgroundImage.sprite = bossBackground;
    }

    public void StartNormalBattle(int zoneIndex)
    {
        EnemyEncounterTable table = GetEncounterTable(zoneIndex);
        if (table == null || encounterBootstrapper == null || battleManager == null)
        {
            Debug.LogWarning("[ExplorationBattleBridge] 일반 전투 시작에 필요한 참조가 비어 있습니다.");
            ReturnToMap();
            return;
        }

        onBeforeBattleStart?.Invoke();

        if (mapUI != null)
            mapUI.ShowMap(false);

        encounterBootstrapper.GenerateAndApplyEnemyPartyFromTable(table);
        battleManager.StartBattle();
    }

    public void StartBossBattle()
    {
        if (bossPartyDefinition == null || battleManager == null)
        {
            Debug.LogWarning("[ExplorationBattleBridge] 보스 전투 시작에 필요한 참조가 비어 있습니다.");
            ReturnToMap();
            return;
        }

        onBeforeBattleStart?.Invoke();

        if (mapUI != null)
            mapUI.ShowMap(false);

        battleManager.SetEnemyPartyDefinition(bossPartyDefinition);
        battleManager.StartBattle();
    }

    public void OpenTreasureEvent(ExplorationNodeData node)
    {
        if (mapUI != null)
            mapUI.ShowMap(false);

        if (treasureEventPanel != null)
            treasureEventPanel.Open(treasureTitle, treasureBody);
        else
            ReturnToMap();
    }

    public void OpenRestEvent(ExplorationNodeData node)
    {
        if (mapUI != null)
            mapUI.ShowMap(false);

        if (restEventPanel != null)
            restEventPanel.Open(restTitle, restBody);
        else
            ReturnToMap();
    }

    public void ReturnToMap()
    {
        if (mapUI != null)
            mapUI.ShowMap(true);

        if (runController != null)
            runController.OnReturnedToMap();

        onReturnToMap?.Invoke();
    }

    private void HandleBattleEnded(BattleResultType result)
    {
        ReturnToMap();
    }

    private EnemyEncounterTable GetEncounterTable(int zoneIndex)
    {
        switch (zoneIndex)
        {
            case 0: return zone1EncounterTable;
            case 1: return zone2EncounterTable;
            case 2: return zone3EncounterTable;
            default: return zone1EncounterTable;
        }
    }
}
