using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// LegionUnitCardUI의 투명 클릭 영역에 붙는 입력 프록시.
/// 투명 Button이 카드 위를 덮을 때 클릭뿐 아니라 드래그 시작/종료도 카드로 전달한다.
/// </summary>
public class LegionCardClickAreaUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private LegionUnitCardUI owner;

    public void Bind(LegionUnitCardUI card)
    {
        owner = card;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        owner?.HandleCardInputClicked(eventData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        owner?.BeginCardInputDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        owner?.HandleCardInputDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        owner?.EndCardInputDrag(eventData);
    }
}
