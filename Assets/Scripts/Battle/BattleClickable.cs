using UnityEngine;
using UnityEngine.EventSystems;

public class BattleClickable : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private BattleUnitView view;
    private BattleInputController inputController;

    public void Initialize(BattleUnitView targetView, BattleInputController controller)
    {
        view = targetView;
        inputController = controller;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (view == null || inputController == null)
            return;

        inputController.OnUnitViewClicked(view);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (view == null || inputController == null)
            return;

        inputController.OnUnitViewHoverEntered(view);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (view == null || inputController == null)
            return;

        inputController.OnUnitViewHoverExited(view);
    }
}
