using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 레기온 패널 안에 두는 드롭 영역.
/// 하단 파티 요약 패널의 유닛 포트레잇을 이 영역으로 드래그하면 파티 편성에서 제거한다.
/// 평상시에는 Raycast를 통과시켜 카드/버튼 클릭을 막지 않는다.
/// </summary>
public class LegionPartyRemoveDropZoneUI : MonoBehaviour, IDropHandler, ICanvasRaycastFilter
{
    [SerializeField] private LegionPanelUI legionPanelUI;
    [SerializeField] private BottomPartySummaryPanelUI bottomPartySummaryPanelUI;

    private void Awake()
    {
        if (legionPanelUI == null)
            legionPanelUI = GetComponentInParent<LegionPanelUI>();

        if (bottomPartySummaryPanelUI == null)
            bottomPartySummaryPanelUI = Object.FindFirstObjectByType<BottomPartySummaryPanelUI>();
    }

    public bool IsRaycastLocationValid(Vector2 screenPosition, Camera eventCamera)
    {
        // 이 조건이 없으면 투명 제거 영역이 레기온 카드/필터/정렬 버튼을 전부 막는다.
        return bottomPartySummaryPanelUI != null && bottomPartySummaryPanelUI.HasDraggedPartyEntry;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (bottomPartySummaryPanelUI == null || !bottomPartySummaryPanelUI.HasDraggedPartyEntry)
            return;

        if (legionPanelUI != null)
        {
            legionPanelUI.HandlePartyEntryDroppedToLegionPanel();
            return;
        }

        bottomPartySummaryPanelUI.TryRemoveDraggedPartyEntryToBarracks();
    }
}
