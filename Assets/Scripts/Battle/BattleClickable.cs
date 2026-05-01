using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// BattleUnitView 클릭/호버 입력 중계자.
/// 이제 유닛 프리팹 루트뿐 아니라 ClickableArea 자식에도 붙여서 사용할 수 있다.
/// </summary>
public class BattleClickable : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    private BattleUnitView view;
    private BattleInputController inputController;

    public void Initialize(BattleUnitView targetView, BattleInputController controller)
    {
        view = targetView;
        inputController = controller;
    }

    private void ResolveReferencesIfNeeded()
    {
        if (view == null)
            view = GetComponentInParent<BattleUnitView>();

        if (inputController == null)
            inputController = Object.FindFirstObjectByType<BattleInputController>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        ResolveReferencesIfNeeded();

        if (view == null || inputController == null)
            return;

        // 우클릭은 액션휠 취소/뒤로가기/열기닫기 입력으로 사용하므로 유닛 선택으로 처리하지 않는다.
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
            return;

        inputController.OnUnitViewClicked(view);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ResolveReferencesIfNeeded();

        if (view == null || inputController == null)
            return;

        inputController.OnUnitViewHoverEntered(view, eventData != null ? eventData.position : Vector2.zero);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        ResolveReferencesIfNeeded();

        if (view == null || inputController == null)
            return;

        inputController.OnUnitViewHoverMoved(view, eventData != null ? eventData.position : Vector2.zero);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ResolveReferencesIfNeeded();

        if (view == null || inputController == null)
            return;

        inputController.OnUnitViewHoverExited(view);
    }
}
