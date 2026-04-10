using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StorageItemSlotUI : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private GameObject equippedMarkRoot;

    private StoragePanelUI owner;

    public InventoryStackData StackData { get; private set; }
    public int ColumnIndexInRow { get; private set; }

    public void Bind(StoragePanelUI panelOwner, InventoryStackData stack, int columnIndexInRow, bool isAssigned)
    {
        owner = panelOwner;
        StackData = stack;
        ColumnIndexInRow = Mathf.Clamp(columnIndexInRow, 0, 9);

        bool hasData = stack != null && stack.item != null && stack.amount > 0;

        if (iconImage != null)
        {
            iconImage.gameObject.SetActive(hasData);
            iconImage.sprite = hasData ? stack.item.icon : null;
        }

        if (amountText != null)
            amountText.text = hasData && stack.amount > 1 ? stack.amount.ToString() : string.Empty;

        if (equippedMarkRoot != null)
            equippedMarkRoot.SetActive(hasData && isAssigned);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (StackData == null || StackData.item == null)
            return;

        owner?.HandleItemHovered(this, StackData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        owner?.HideTooltip();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        owner?.HandleItemClicked(this);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || StackData == null || StackData.item == null)
            return;

        owner?.HandleItemDragBegin(this);

        if (iconImage != null && iconImage.sprite != null)
            UIDragGhostUI.Show(iconImage.sprite, transform as RectTransform);
    }

    public void OnDrag(PointerEventData eventData)
    {
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        UIDragGhostUI.HideGhost();

        if (StackData == null || StackData.item == null)
            return;

        owner?.HandleItemDragEnd(this);
    }
}