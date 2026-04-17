using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class WorldMapHoverPressButtonUI : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("References")]
    [SerializeField] private Image targetImage;
    [SerializeField] private RectTransform targetRect;

    [Header("Sprites")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite hoverSprite;

    [Header("Pressed Feel")]
    [SerializeField] private Vector2 pressedOffset = new Vector2(3f, -3f);
    [SerializeField] private float pressedScale = 0.96f;
    [SerializeField] private Color pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);

    private Vector2 originalAnchoredPosition;
    private Vector3 originalScale;
    private Color originalColor;
    private bool isHovered;
    private bool isPressed;

    private void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        if (targetRect == null)
            targetRect = transform as RectTransform;

        if (targetRect != null)
            originalAnchoredPosition = targetRect.anchoredPosition;

        if (targetRect != null)
            originalScale = targetRect.localScale;

        if (targetImage != null)
            originalColor = targetImage.color;

        ApplyVisual();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        ApplyVisual();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        isPressed = false;
        ApplyVisual();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        isPressed = true;
        ApplyVisual();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        isPressed = false;
        ApplyVisual();
    }

    private void ApplyVisual()
    {
        if (targetImage != null)
        {
            targetImage.sprite = isHovered && hoverSprite != null ? hoverSprite : normalSprite;
            targetImage.color = isPressed ? pressedColor : originalColor;
        }

        if (targetRect != null)
        {
            targetRect.anchoredPosition = isPressed
                ? originalAnchoredPosition + pressedOffset
                : originalAnchoredPosition;

            targetRect.localScale = isPressed
                ? originalScale * pressedScale
                : originalScale;
        }
    }
}