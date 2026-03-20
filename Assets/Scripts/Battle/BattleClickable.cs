using UnityEngine;
using UnityEngine.EventSystems;

public class BattleClickable : MonoBehaviour, IPointerClickHandler
{
    private BattleUnitView unitView;
    private BattleManager battleManager;

    public void Initialize(BattleUnitView view, BattleManager manager)
    {
        unitView = view;
        battleManager = manager;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (unitView == null || battleManager == null)
            return;

        battleManager.OnUnitViewClicked(unitView);
    }
}