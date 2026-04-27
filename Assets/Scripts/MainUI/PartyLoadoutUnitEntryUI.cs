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

    [Header("Input / State Roots")]
    [Tooltip("슬롯 전체 클릭을 받는 투명 버튼. 비워두면 이 오브젝트의 IPointerClickHandler가 동작한다.")]
    [SerializeField] private Button entryClickButton;
    [SerializeField] private GameObject emptyRoot;
    [SerializeField] private GameObject filledRoot;
    [SerializeField] private GameObject pendingTargetRoot;
    [SerializeField] private GameObject pendingSourceRoot;

    [Header("Click Settings")]
    [Tooltip("군단 창/편성 모드에서 하단 파티 유닛을 이 시간 안에 두 번 클릭하면 편성에서 제거한다.")]
    [SerializeField] private float doubleClickThreshold = 0.35f;

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
    private float lastLeftClickTime = -999f;

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

        if (entryClickButton != null)
        {
            // 슬롯 전체를 덮는 투명 Button은 클릭뿐 아니라 드래그/드롭도 받아야 한다.
            // Button.onClick만 쓰면 투명 버튼이 드래그를 먹고 편성/제거 드롭이 불안정해진다.
            entryClickButton.onClick.RemoveAllListeners();

            PartyLoadoutEntryInputProxyUI proxy = entryClickButton.GetComponent<PartyLoadoutEntryInputProxyUI>();
            if (proxy == null)
                proxy = entryClickButton.gameObject.AddComponent<PartyLoadoutEntryInputProxyUI>();
            proxy.Bind(this);
        }
    }

    private void OnDisable()
    {
        RestoreDragRaycastState();
    }

    public void Bind(
        BottomPartySummaryPanelUI panelOwner,
        PartyMemberData member,
        int representedBattleSlotIndex,
        bool showEquipmentSlots,
        bool showWorldInfo,
        bool barracksMode = false,
        bool canReceivePendingBarracksUnit = false,
        bool isPendingBarracksMember = false)
    {
        owner = panelOwner;
        Member = member;
        RepresentedBattleSlotIndex = Mathf.Clamp(representedBattleSlotIndex, 0, 3);

        RestoreDragRaycastState();

        bool hasMember = member != null;

        if (emptyRoot != null)
            emptyRoot.SetActive(!hasMember);

        if (filledRoot != null)
            filledRoot.SetActive(hasMember);

        if (pendingTargetRoot != null)
            pendingTargetRoot.SetActive(barracksMode && canReceivePendingBarracksUnit);

        if (pendingSourceRoot != null)
            pendingSourceRoot.SetActive(barracksMode && isPendingBarracksMember);

        if (entryClickButton != null)
            entryClickButton.interactable = owner != null;

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
        HandleLeftClick();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (entryClickButton != null)
            return;

        HandleEntryClickFromInput(eventData);
    }

    public void HandleEntryClickFromInput(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
            return;

        HandleLeftClick();
    }

    private void HandleLeftClick()
    {
        float now = Time.unscaledTime;
        bool isDoubleClick = now - lastLeftClickTime <= Mathf.Max(0.05f, doubleClickThreshold);
        lastLeftClickTime = now;

        if (isDoubleClick && owner != null && owner.TryRemovePartyEntryByDoubleClick(this))
        {
            lastLeftClickTime = -999f;
            return;
        }

        owner?.HandleUnitEntryClicked(this);
    }

    public void OnDrop(PointerEventData eventData)
    {
        HandleEntryDropFromInput(eventData);
    }

    public void HandleEntryDropFromInput(PointerEventData eventData)
    {
        owner?.HandleUnitEntryDroppedOn(this);
    }

    public void RestoreDragRaycastState()
    {
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = true;
    }

    public void BeginPortraitDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (Member == null || owner == null)
            return;

        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = false;

        owner.BeginUnitEntryDrag(this);

        if (portraitImage != null && portraitImage.sprite != null)
            UIDragGhostUI.Show(portraitImage.sprite, portraitImage.rectTransform);
    }

    public void EndPortraitDrag(PointerEventData eventData)
    {
        RestoreDragRaycastState();
        UIDragGhostUI.HideGhost();
        owner?.EndUnitEntryDrag(this);
    }
}
