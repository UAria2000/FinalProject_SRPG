using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UIDragGhostUI : MonoBehaviour
{
    private static UIDragGhostUI instance;

    [Header("References")]
    [SerializeField] private RectTransform ghostRect;
    [SerializeField] private Image ghostImage;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Appearance")]
    [SerializeField] private Vector2 cursorOffset = new Vector2(24f, -24f);
    [SerializeField][Range(0f, 1f)] private float ghostAlpha = 0.9f;

    private bool visible;

    private void Awake()
    {
        instance = this;

        if (ghostRect == null)
            ghostRect = transform as RectTransform;

        if (ghostImage == null)
            ghostImage = GetComponent<Image>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (ghostImage != null)
            ghostImage.raycastTarget = false;

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            canvasGroup.alpha = ghostAlpha;
        }

        HideImmediate();
    }

    private void LateUpdate()
    {
        if (!visible)
            return;

        FollowMouse();
        transform.SetAsLastSibling();
    }

    public static void Show(Sprite sprite, RectTransform sourceRect = null)
    {
        if (instance == null || sprite == null)
            return;

        instance.InternalShow(sprite, sourceRect);
    }

    public static void HideGhost()
    {
        if (instance == null)
            return;

        instance.InternalHide();
    }

    private void InternalShow(Sprite sprite, RectTransform sourceRect)
    {
        if (ghostImage == null || ghostRect == null)
            return;

        ghostImage.sprite = sprite;
        ghostImage.enabled = true;

        if (sourceRect != null)
            ghostRect.sizeDelta = sourceRect.rect.size;

        visible = true;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        FollowMouse();
    }

    private void InternalHide()
    {
        visible = false;

        if (ghostImage != null)
        {
            ghostImage.sprite = null;
            ghostImage.enabled = false;
        }

        gameObject.SetActive(false);
    }

    private void HideImmediate()
    {
        visible = false;

        if (ghostImage != null)
        {
            ghostImage.sprite = null;
            ghostImage.enabled = false;
        }

        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    private void FollowMouse()
    {
        if (ghostRect == null || Mouse.current == null)
            return;

        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
        Vector3 screenPosition = new Vector3(
            mouseScreenPosition.x + cursorOffset.x,
            mouseScreenPosition.y + cursorOffset.y,
            0f
        );

        ghostRect.position = screenPosition;
    }
}