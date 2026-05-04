using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WorldEventRewardSlotUI : MonoBehaviour,
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

    private WorldEventPopupUI owner;
    private int rewardIndex = -1;
    private ItemDefinition boundItem;
    private int boundAmount;
    private bool interactable;

    public bool HasItem => boundItem != null && boundAmount > 0;
    public ItemDefinition BoundItem => boundItem;
    public int BoundAmount => boundAmount;

    private void Awake()
    {
        if (slotButton != null)
        {
            slotButton.onClick.RemoveAllListeners();
            slotButton.onClick.AddListener(HandleClick);
        }
    }

    public void Bind(WorldEventPopupUI popupOwner, int index, ItemDefinition item, int amount, bool canInteract, bool selected)
    {
        owner = popupOwner;
        rewardIndex = index;
        boundItem = item;
        boundAmount = Mathf.Max(0, amount);
        interactable = canInteract && HasItem;

        RefreshVisual(selected);
    }

    public void Clear()
    {
        boundItem = null;
        boundAmount = 0;
        interactable = false;
        RefreshVisual(false);
    }

    private void RefreshVisual(bool selected)
    {
        bool hasItem = HasItem;

        if (emptyRoot != null)
            emptyRoot.SetActive(!hasItem);

        if (lockedRoot != null)
            lockedRoot.SetActive(hasItem && !interactable);

        if (selectedRoot != null)
            selectedRoot.SetActive(hasItem && selected);

        if (iconImage != null)
        {
            iconImage.gameObject.SetActive(hasItem);
            iconImage.sprite = hasItem ? boundItem.icon : null;
            iconImage.color = hasItem ? Color.white : new Color(1f, 1f, 1f, 0f);
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
        }

        if (amountText != null)
            amountText.text = hasItem && boundAmount > 1 ? boundAmount.ToString() : string.Empty;

        if (slotButton != null)
            slotButton.interactable = interactable;
    }

    private void HandleClick()
    {
        if (!interactable || owner == null || rewardIndex < 0)
            return;

        owner.HandleRewardSlotClicked(rewardIndex);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!interactable || owner == null || rewardIndex < 0 || boundItem == null)
            return;

        if (!owner.HandleRewardSlotDragBegin(rewardIndex))
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
        owner?.HandleRewardSlotDragEnd(rewardIndex);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (owner == null || boundItem == null)
            return;

        owner.HandleRewardSlotHoverEnter(boundItem);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        owner?.HandleRewardSlotHoverExit();
    }
}
