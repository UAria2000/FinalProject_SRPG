using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// BottomPartySummaryPanel 슬롯 전체를 덮는 투명 버튼에 붙는 입력 프록시.
/// 빈 슬롯/채워진 슬롯 모두 클릭, 더블클릭, 드래그, 드롭을 안정적으로 받게 한다.
/// </summary>
public class PartyLoadoutEntryInputProxyUI : MonoBehaviour, IPointerClickHandler, IDropHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private PartyLoadoutUnitEntryUI owner;

    public void Bind(PartyLoadoutUnitEntryUI entry)
    {
        owner = entry;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        owner?.HandleEntryClickFromInput(eventData);
    }

    public void OnDrop(PointerEventData eventData)
    {
        owner?.HandleEntryDropFromInput(eventData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        owner?.BeginPortraitDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        owner?.EndPortraitDrag(eventData);
    }
}
