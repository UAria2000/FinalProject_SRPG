using TMPro;
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

    [Header("World HUD")]
    [SerializeField] private GameObject worldDetailsRoot;
    [SerializeField] private TMP_Text worldLevelText;
    [SerializeField] private TMP_Text worldHpText;
    [SerializeField] private Image warningDim25Image;
    [SerializeField] private Image warningDim50Image;
    [SerializeField] private Image warningDim75Image;

    private BottomPartySummaryPanelUI owner;
    private CanvasGroup canvasGroup;
    private bool worldDetailsExpanded;

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
        bool worldMapMode = owner != null && owner.IsWorldMapHudMode();

        if (!hasMember)
            worldDetailsExpanded = false;

        if (portraitImage != null)
        {
            portraitImage.gameObject.SetActive(hasMember);
            portraitImage.sprite = hasMember && member.unitViewDefinition != null
                ? member.unitViewDefinition.GetSlotFaceSprite()
                : null;
        }

        if (portraitDragHandle != null)
            portraitDragHandle.Bind(this, hasMember && owner != null);

        bool showWorldDetails = worldMapMode && hasMember && (worldDetailsRoot == null || worldDetailsExpanded);

        if (worldLevelText != null)
        {
            worldLevelText.gameObject.SetActive(showWorldDetails);
            worldLevelText.text = hasMember ? $"Lv.{member.currentLevel}" : string.Empty;
        }

        if (worldHpText != null)
        {
            worldHpText.gameObject.SetActive(showWorldDetails);
            worldHpText.text = hasMember && owner != null
                ? $"{owner.GetMemberCurrentHP(member)}/{owner.GetMemberMaxHP(member)}"
                : string.Empty;
        }

        if (worldDetailsRoot != null)
            worldDetailsRoot.SetActive(worldMapMode && hasMember && worldDetailsExpanded);

        ApplyWarningDims(worldMapMode, hasMember ? member : null);

        bool visibleEquipmentSlots = (showEquipmentSlots || (worldMapMode && hasMember && worldDetailsExpanded));

        if (leftEquipmentSlot != null)
        {
            ItemDefinition leftItem = hasMember && owner != null ? owner.GetAssignedEquipment(member, 0) : null;
            leftEquipmentSlot.Bind(owner, member, 0, leftItem, visibleEquipmentSlots && hasMember);
        }

        if (rightEquipmentSlot != null)
        {
            ItemDefinition rightItem = hasMember && owner != null ? owner.GetAssignedEquipment(member, 1) : null;
            rightEquipmentSlot.Bind(owner, member, 1, rightItem, visibleEquipmentSlots && hasMember);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (owner != null && owner.IsBarracksMode())
        {
            owner.HandleUnitEntryClicked(this);
            return;
        }

        if (owner != null && owner.IsWorldMapHudMode() && Member != null)
        {
            worldDetailsExpanded = !worldDetailsExpanded;
            owner.RefreshAll();
        }
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

    private void ApplyWarningDims(bool worldMapMode, PartyMemberData member)
    {
        int currentHP = member != null && owner != null ? owner.GetMemberCurrentHP(member) : 0;
        int maxHP = member != null && owner != null ? owner.GetMemberMaxHP(member) : 0;
        float hpRatio = maxHP > 0 ? currentHP / (float)maxHP : 1f;

        bool show25 = worldMapMode && member != null && hpRatio <= 0.75f;
        bool show50 = worldMapMode && member != null && hpRatio <= 0.50f;
        bool show75 = worldMapMode && member != null && hpRatio <= 0.25f;

        if (warningDim25Image != null) warningDim25Image.gameObject.SetActive(show25);
        if (warningDim50Image != null) warningDim50Image.gameObject.SetActive(show50);
        if (warningDim75Image != null) warningDim75Image.gameObject.SetActive(show75);
    }
}
