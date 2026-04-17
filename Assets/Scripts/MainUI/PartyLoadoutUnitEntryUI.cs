using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class PartyLoadoutUnitEntryUI : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    [Header("References")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private PartyUnitPortraitDragHandleUI portraitDragHandle;
    [SerializeField] private GameObject equipmentSlotsRoot;
    [SerializeField] private PartyEquipmentSlotUI leftEquipmentSlot;
    [SerializeField] private PartyEquipmentSlotUI rightEquipmentSlot;

    [Header("World View")]
    [SerializeField] private GameObject worldDetailsRoot;
    [SerializeField] private TMP_Text worldLevelText;
    [SerializeField] private TMP_Text worldHpText;
    [SerializeField] private Image warningDim25Image;
    [SerializeField] private Image warningDim50Image;
    [SerializeField] private Image warningDim75Image;

    private BottomPartySummaryPanelUI owner;
    private CanvasGroup canvasGroup;
    private Button portraitRootButton;

    public PartyMemberData Member { get; private set; }
    public int RepresentedBattleSlotIndex { get; private set; }

    public Image PortraitImage => portraitImage;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        if (portraitImage != null)
            portraitRootButton = portraitImage.GetComponentInParent<Button>();

        if (portraitRootButton != null)
        {
            portraitRootButton.onClick.RemoveListener(HandlePortraitButtonClicked);
            portraitRootButton.onClick.AddListener(HandlePortraitButtonClicked);
        }
    }

    public void Bind(
        BottomPartySummaryPanelUI panelOwner,
        PartyMemberData member,
        int representedBattleSlotIndex,
        bool showEquipmentSlots,
        bool showWorldInfo)
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
            portraitImage.color = hasMember ? Color.white : new Color(1f, 1f, 1f, 0f);
        }

        if (portraitDragHandle != null)
            portraitDragHandle.Bind(this, hasMember && owner != null);

        if (equipmentSlotsRoot != null)
            equipmentSlotsRoot.SetActive(hasMember && showEquipmentSlots);

        if (leftEquipmentSlot != null)
        {
            ItemDefinition leftItem = hasMember && owner != null ? owner.GetAssignedEquipment(member, 0) : null;
            leftEquipmentSlot.Bind(owner, member, 0, leftItem, hasMember && showEquipmentSlots);
        }

        if (rightEquipmentSlot != null)
        {
            ItemDefinition rightItem = hasMember && owner != null ? owner.GetAssignedEquipment(member, 1) : null;
            rightEquipmentSlot.Bind(owner, member, 1, rightItem, hasMember && showEquipmentSlots);
        }

        if (worldDetailsRoot != null)
            worldDetailsRoot.SetActive(hasMember && showWorldInfo);

        if (worldLevelText != null)
            worldLevelText.text = hasMember && owner != null ? owner.GetWorldLevelText(member) : string.Empty;

        if (worldHpText != null)
            worldHpText.text = hasMember && owner != null ? owner.GetWorldHPText(member) : string.Empty;

        int warningStage = hasMember && owner != null ? owner.GetWorldWarningStage(member) : 0;

        if (warningDim25Image != null)
            warningDim25Image.gameObject.SetActive(hasMember && warningStage == 1);

        if (warningDim50Image != null)
            warningDim50Image.gameObject.SetActive(hasMember && warningStage == 2);

        if (warningDim75Image != null)
            warningDim75Image.gameObject.SetActive(hasMember && warningStage == 3);
    }

    private void HandlePortraitButtonClicked()
    {
        owner?.HandleUnitEntryClicked(this);
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