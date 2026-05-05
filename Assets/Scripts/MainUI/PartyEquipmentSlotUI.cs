using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class PartyEquipmentSlotUI : MonoBehaviour,
    IPointerClickHandler,
    IDropHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [SerializeField] private Image iconImage;

    private BottomPartySummaryPanelUI owner;
    private CanvasGroup canvasGroup;

    public PartyMemberData Member { get; private set; }
    public int SlotIndex { get; private set; }
    public ItemDefinition AssignedItem { get; private set; }

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void Bind(
        BottomPartySummaryPanelUI panelOwner,
        PartyMemberData member,
        int slotIndex,
        ItemDefinition assignedItem,
        bool visible)
    {
        owner = panelOwner;
        Member = member;
        SlotIndex = Mathf.Clamp(slotIndex, 0, 1);
        AssignedItem = assignedItem;

        gameObject.SetActive(visible);

        bool hasItem = AssignedItem != null;
        if (iconImage != null)
        {
            iconImage.gameObject.SetActive(hasItem);
            iconImage.sprite = hasItem ? AssignedItem.icon : null;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        owner?.HandleEquipmentSlotClicked(this);
    }

    public void OnDrop(PointerEventData eventData)
    {
        owner?.HandleEquipmentSlotDropped(this);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || AssignedItem == null || owner == null)
            return;

        if (!owner.BeginEquipmentDrag(this))
            return;

        canvasGroup.blocksRaycasts = false;

        if (iconImage != null && iconImage.sprite != null)
            UIDragGhostUI.Show(iconImage.sprite, transform as RectTransform);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 비워둬도 됨. 드래그 체인 유지용.
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        UIDragGhostUI.HideGhost();
        owner?.EndEquipmentDrag(this);
    }
}