using System.Text;
using UnityEngine;

public class WorldEventController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WorldEventPopupUI eventPopupUI;
    [SerializeField] private WorldBattleBridge battleBridge;
    [SerializeField] private BattleManager battleManager;

    [Header("Popup Labels")]
    [SerializeField] private string defaultConfirmText = "확인";
    [SerializeField] private string battleMissingText = "전투 연결이 아직 설정되지 않았습니다.";
    [SerializeField] private string restResolvedSuffix = "\n\n파티가 휴식을 취해 체력을 회복했다.";
    [SerializeField] private string graveyardSuffix = "\n\n묘지는 재사용 가능한 이벤트로 남아 있습니다.";
    [SerializeField] private string merchantSuffix = "\n\n상점 상세 기능은 추후 연결 예정입니다.";
    [SerializeField] private string questSuffix = "\n\n퀘스트 상세 분기는 추후 연결 예정입니다.";
    [SerializeField] private string treasureSuffix = "\n\n보상을 지급하는 세부 로직은 추후 연결 예정입니다.";

    private WorldRunManager runManager;
    private WorldGenerationSettings settings;
    private bool popupOpen;

    public bool IsBusy => popupOpen || (battleBridge != null && battleBridge.IsBattleRunning);

    public void Initialize(WorldRunManager manager, WorldGenerationSettings generationSettings)
    {
        runManager = manager;
        settings = generationSettings;

        if (battleBridge != null)
            battleBridge.Initialize(manager, generationSettings);
    }

    public bool TryHandleArrival(WorldTileData tile)
    {
        if (tile == null || !tile.ShouldTriggerEventOnArrival)
            return false;

        if (tile.IsCombatEvent)
            return TryStartCombatEvent(tile);

        return TryOpenSimpleEvent(tile);
    }

    public void OpenWorldSettlementFromMap()
    {
        if (battleBridge == null || runManager == null || !runManager.IsWorldConquestAvailable())
            return;

        battleBridge.OpenSettlementFromWorldMap(true);
    }

    private bool TryStartCombatEvent(WorldTileData tile)
    {
        if (battleBridge != null && battleBridge.StartBattleForTile(tile))
            return true;

        OpenFallbackPopup(tile, battleMissingText, () =>
        {
            popupOpen = false;
            if (runManager != null)
                runManager.ResolveCombatDefeat(tile, true);
        });

        return false;
    }

    private bool TryOpenSimpleEvent(WorldTileData tile)
    {
        if (eventPopupUI == null)
        {
            Debug.LogWarning("[WorldEventController] WorldEventPopupUI reference is missing.");
            return false;
        }

        popupOpen = true;
        string title = settings != null ? settings.GetEventDisplayName(tile.eventType) : tile.eventType.ToString();
        string body = BuildEventBody(tile);

        eventPopupUI.Open(title, body, defaultConfirmText, () => ConfirmSimpleEvent(tile), () => popupOpen = false);
        return true;
    }

    private void ConfirmSimpleEvent(WorldTileData tile)
    {
        popupOpen = false;
        ApplyImmediateEventEffects(tile);

        bool isReusable = tile != null && tile.IsReusableEvent;
        bool disableIcon = !isReusable;
        bool markResolved = !isReusable;

        if (runManager != null)
            runManager.ResolveMapEvent(tile, true, markResolved, disableIcon);
    }

    private void ApplyImmediateEventEffects(WorldTileData tile)
    {
        if (tile == null)
            return;

        if (tile.eventType == WorldTileEventType.Rest)
            RestorePartyToFull();
    }

    private void RestorePartyToFull()
    {
        BattlePartyRuntimeState partyState = null;
        if (runManager != null)
            partyState = runManager.GetOrCreatePlayerPartyRuntimeState();
        if (partyState == null && battleManager != null)
            partyState = battleManager.AllyRuntimePartyState;
        partyState?.ResetPersistentHPToFull();
    }

    private string BuildEventBody(WorldTileData tile)
    {
        StringBuilder sb = new StringBuilder();
        if (settings != null)
            sb.Append(settings.GetEventDescription(tile.eventType));

        switch (tile.eventType)
        {
            case WorldTileEventType.Rest: sb.Append(restResolvedSuffix); break;
            case WorldTileEventType.Treasure: sb.Append(treasureSuffix); break;
            case WorldTileEventType.Merchant: sb.Append(merchantSuffix); break;
            case WorldTileEventType.Quest: sb.Append(questSuffix); break;
            case WorldTileEventType.Graveyard: sb.Append(graveyardSuffix); break;
        }
        return sb.ToString();
    }

    private void OpenFallbackPopup(WorldTileData tile, string body, System.Action onConfirm)
    {
        if (eventPopupUI == null)
        {
            Debug.LogWarning("[WorldEventController] Fallback popup could not open because WorldEventPopupUI is missing.");
            onConfirm?.Invoke();
            return;
        }

        popupOpen = true;
        string title = settings != null ? settings.GetEventDisplayName(tile.eventType) : tile.eventType.ToString();
        eventPopupUI.Open(title, body, defaultConfirmText, onConfirm, () => popupOpen = false);
    }
}
