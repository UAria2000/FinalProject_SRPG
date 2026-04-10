using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class StorageItemTooltipUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform tooltipRect;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text categoryText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text equippedStateText;

    [Header("Offsets")]
    [SerializeField] private Vector2 rightSideOffset = new Vector2(24f, 24f);
    [SerializeField] private Vector2 leftSideOffset = new Vector2(-24f, 24f);

    private Canvas parentCanvas;
    private bool visible;
    private int currentColumnIndex;

    private void Awake()
    {
        if (tooltipRect == null)
            tooltipRect = transform as RectTransform;

        parentCanvas = GetComponentInParent<Canvas>();

        HideImmediate();
    }

    private void Update()
    {
        if (!visible)
            return;

        FollowMouse();
    }

    public void Show(ItemDefinition item, bool isAssigned, int columnIndexInRow)
    {
        if (item == null)
        {
            Hide();
            return;
        }

        currentColumnIndex = Mathf.Clamp(columnIndexInRow, 0, 9);

        if (iconImage != null)
        {
            iconImage.gameObject.SetActive(item.icon != null);
            iconImage.sprite = item.icon;
        }

        if (itemNameText != null)
            itemNameText.text = item.itemName;

        if (categoryText != null)
            categoryText.text = GetCategoryLabel(item);

        if (descriptionText != null)
            descriptionText.text = item.description;

        if (equippedStateText != null)
            equippedStateText.text = isAssigned ? "장착/세팅 중" : string.Empty;

        visible = true;
        gameObject.SetActive(true);
        FollowMouse();
    }

    public void Hide()
    {
        visible = false;
        gameObject.SetActive(false);
    }

    private void HideImmediate()
    {
        visible = false;
        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    private void FollowMouse()
    {
        if (tooltipRect == null)
            return;

        if (Mouse.current == null)
            return;

        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
        Vector2 offset = currentColumnIndex <= 4 ? rightSideOffset : leftSideOffset;

        Canvas canvas = parentCanvas != null ? parentCanvas : GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            tooltipRect.position = mouseScreenPosition + offset;
            return;
        }

        RectTransform canvasRect = canvas.transform as RectTransform;
        Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                mouseScreenPosition,
                eventCamera,
                out Vector2 localPoint))
        {
            return;
        }

        tooltipRect.anchoredPosition = localPoint + offset;
    }

    private string GetCategoryLabel(ItemDefinition item)
    {
        if (item == null)
            return string.Empty;

        switch (item.mainUICategory)
        {
            case MainUIItemCategory.Equipment:
                return "장비";
            case MainUIItemCategory.Consumable:
                return "소모품";
            case MainUIItemCategory.Other:
                return "기타";
            default:
                return string.Empty;
        }
    }
}