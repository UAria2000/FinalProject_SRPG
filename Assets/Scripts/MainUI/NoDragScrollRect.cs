using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// 선배! 이 스크립트가 붙으면 마우스로 잡아끄는 건 안 되고 '휠'만 먹게 될 거예요!
public class NoDragScrollRect : ScrollRect
{
    public override void OnBeginDrag(PointerEventData eventData) { }
    public override void OnDrag(PointerEventData eventData) { }
    public override void OnEndDrag(PointerEventData eventData) { }
}