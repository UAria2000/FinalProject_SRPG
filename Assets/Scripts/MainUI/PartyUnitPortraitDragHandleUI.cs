using UnityEngine;
using UnityEngine.EventSystems;

public class PartyUnitPortraitDragHandleUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private PartyLoadoutUnitEntryUI ownerEntry;

    private bool dragEnabled;

    public void Bind(PartyLoadoutUnitEntryUI entry, bool enabled)
    {
        ownerEntry = entry;
        dragEnabled = enabled;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!dragEnabled || ownerEntry == null)
            return;

        ownerEntry.BeginPortraitDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (ownerEntry == null)
            return;

        ownerEntry.EndPortraitDrag(eventData);
    }
}