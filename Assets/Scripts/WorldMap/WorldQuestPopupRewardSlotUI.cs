using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WorldQuestPopupRewardSlotUI : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("References")]
    [SerializeField] private Button slotButton;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private GameObject lockedRoot;
    [SerializeField] private GameObject emptyRoot;
    [SerializeField] private GameObject selectedRoot;

    private WorldQuestPopupUI owner;
    private WorldQuestState quest;
    private int rewardIndex = -1;
    private ItemDefinition boundItem;
    private int boundAmount;
    private bool clickable;

    private void Awake()
    {
        if (slotButton != null)
        {
            slotButton.onClick.RemoveAllListeners();
            slotButton.onClick.AddListener(HandleClick);
        }
    }

    public void Bind(
        WorldQuestPopupUI popupOwner,
        WorldQuestState boundQuest,
        int index,
        ItemDefinition item,
        int amount,
        bool showLocked,
        bool canClick,
        bool selected)
    {
        owner = popupOwner;
        quest = boundQuest;
        rewardIndex = index;
        boundItem = item;
        boundAmount = Mathf.Max(0, amount);
        clickable = canClick && item != null;

        bool hasItem = item != null;

        if (emptyRoot != null)
            emptyRoot.SetActive(!hasItem);

        if (lockedRoot != null)
            lockedRoot.SetActive(hasItem && showLocked);

        if (selectedRoot != null)
            selectedRoot.SetActive(hasItem && selected);

        if (iconImage != null)
        {
            iconImage.gameObject.SetActive(hasItem);
            iconImage.sprite = hasItem ? item.icon : null;
            iconImage.color = hasItem ? Color.white : new Color(1f, 1f, 1f, 0f);
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
        }

        if (amountText != null)
            amountText.text = hasItem ? boundAmount.ToString() : string.Empty;

        if (slotButton != null)
            slotButton.interactable = clickable;
    }

    private void HandleClick()
    {
        if (!clickable || owner == null || quest == null)
            return;

        owner.HandleRewardSlotClicked(quest, rewardIndex);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!clickable || owner == null || quest == null || boundItem == null)
            return;

        if (!owner.HandleRewardSlotDragBegin(quest, rewardIndex))
            return;

        if (iconImage != null && iconImage.sprite != null)
            UIDragGhostUI.Show(iconImage.sprite, transform as RectTransform);
    }

    public void OnDrag(PointerEventData eventData)
    {
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        UIDragGhostUI.HideGhost();
        owner?.HandleRewardSlotDragEnd(quest, rewardIndex);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (owner == null || boundItem == null)
            return;

        owner.HandleRewardSlotHoverEnter(boundItem);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (owner == null)
            return;

        owner.HandleRewardSlotHoverExit();
    }
}
