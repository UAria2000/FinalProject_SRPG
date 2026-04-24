using UnityEngine;
using UnityEngine.EventSystems;

public class LegionStatHoverTargetUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private string statLabel;
    private LegionDetailPanelUI owner;
    private LegionStatKind statKind;

    public void Bind(LegionDetailPanelUI panelOwner, LegionStatKind kind, string label)
    {
        owner = panelOwner;
        statKind = kind;
        statLabel = label;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        owner?.ShowStatTooltip(statKind, statLabel);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        owner?.HideStatTooltip();
    }
}
