using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class PartyLoadoutUnitEntryUI : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    [Header("References")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private PartyUnitPortraitDragHandleUI portraitDragHandle;
    [SerializeField] private PartyEquipmentSlotUI leftEquipmentSlot;
    [SerializeField] private PartyEquipmentSlotUI rightEquipmentSlot;

    private BottomPartySummaryPanelUI owner;
    private CanvasGroup canvasGroup;

    public PartyMemberData Member { get; private set; }
    public int RepresentedBattleSlotIndex { get; private set; }

    public Image PortraitImage => portraitImage;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void Bind(
        BottomPartySummaryPanelUI panelOwner,
        PartyMemberData member,
        int representedBattleSlotIndex,
        bool showEquipmentSlots)
    {
        owner = panelOwner;
        Member = member;
        RepresentedBattleSlotIndex = Mathf.Clamp(representedBattleSlotIndex, 0, 3);

        bool hasMember = member != null;

        if (portraitImage != null)
        {
            portraitImage.gameObject.SetActive(hasMember);
            portraitImage.sprite = hasMember && member.unitViewDefinition != null
                ? member.unitViewDefinition.GetSlotFaceSprite()
                : null;
        }

        if (portraitDragHandle != null)
            portraitDragHandle.Bind(this, hasMember && owner != null);

        if (leftEquipmentSlot != null)
        {
            ItemDefinition leftItem = hasMember && owner != null ? owner.GetAssignedEquipment(member, 0) : null;
            leftEquipmentSlot.Bind(owner, member, 0, leftItem, showEquipmentSlots && hasMember);
        }

        if (rightEquipmentSlot != null)
        {
            ItemDefinition rightItem = hasMember && owner != null ? owner.GetAssignedEquipment(member, 1) : null;
            rightEquipmentSlot.Bind(owner, member, 1, rightItem, showEquipmentSlots && hasMember);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        owner?.HandleUnitEntryClicked(this);
    }

    public void OnDrop(PointerEventData eventData)
    {
        owner?.HandleUnitEntryDroppedOn(this);
    }

    public void BeginPortraitDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (Member == null || owner == null)
            return;

        canvasGroup.blocksRaycasts = false;
        owner.BeginUnitEntryDrag(this);

        if (portraitImage != null && portraitImage.sprite != null)
            UIDragGhostUI.Show(portraitImage.sprite, portraitImage.rectTransform);
    }

    public void EndPortraitDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        UIDragGhostUI.HideGhost();
        owner?.EndUnitEntryDrag(this);
    }
}
