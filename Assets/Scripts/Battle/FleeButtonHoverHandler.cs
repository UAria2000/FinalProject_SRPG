using UnityEngine;
using UnityEngine.EventSystems;

public class FleeButtonHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    private BattleManager battleManager;

    public void Initialize(BattleManager manager)
    {
        battleManager = manager;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (battleManager != null)
            battleManager.OnFleeButtonHoverEnter(eventData.position);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (battleManager != null)
            battleManager.OnFleeButtonHoverEnter(eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (battleManager != null)
            battleManager.OnFleeButtonHoverExit();
    }
}
