using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StorageSharedConsumableSlotUI : MonoBehaviour,
    IPointerClickHandler,
    IDropHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text labelText;

    private BottomPartySummaryPanelUI owner;
    private ItemDefinition assignedItem;

    public void Bind(BottomPartySummaryPanelUI panelOwner, ItemDefinition item)
    {
        owner = panelOwner;
        assignedItem = item;

        bool hasData = assignedItem != null;

        if (iconImage != null)
        {
            iconImage.gameObject.SetActive(hasData);
            iconImage.sprite = hasData ? assignedItem.icon : null;
        }

        if (labelText != null)
            labelText.text = hasData ? assignedItem.itemName : string.Empty;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        owner?.HandleSharedConsumableClicked();
    }

    public void OnDrop(PointerEventData eventData)
    {
        owner?.HandleSharedConsumableDropped();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (assignedItem == null)
            return;

        owner?.HandleSharedConsumableHoverEnter();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        owner?.HandleSharedConsumableHoverExit();
    }
}
