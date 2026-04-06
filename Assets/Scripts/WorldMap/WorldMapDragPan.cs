using UnityEngine;
using UnityEngine.InputSystem;

public class WorldMapDragPan : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform viewportRect;
    [SerializeField] private RectTransform contentRoot;

    [Header("Optional Drag Blockers")]
    [SerializeField] private RectTransform[] dragBlockers;

    [Header("Pan")]
    [SerializeField] private float dragThreshold = 12f;

    [Header("Zoom")]
    [SerializeField] private bool enableZoom = true;
    [SerializeField] private float zoomStep = 0.1f;
    [SerializeField] private float minZoom = 0.5f;
    [SerializeField] private float maxZoom = 2.0f;
    [SerializeField] private bool clampToMinAfterConfigure = true;

    private bool pressed;
    private bool dragging;
    private Vector2 pressScreenPosition;
    private Vector2 contentStartPosition;
    private int suppressClickFrames;

    public float CurrentZoom => contentRoot != null ? contentRoot.localScale.x : 1f;

    public bool IsDragging => dragging;

    public void Configure(RectTransform inContentRoot)
    {
        contentRoot = inContentRoot;
        if (contentRoot != null && clampToMinAfterConfigure)
        {
            float clamped = Mathf.Clamp(contentRoot.localScale.x, minZoom, maxZoom);
            contentRoot.localScale = new Vector3(clamped, clamped, 1f);
        }
    }

    public bool ShouldSuppressClick()
    {
        return dragging || suppressClickFrames > 0;
    }

    public void CenterOnAnchoredPosition(Vector2 targetAnchoredPosition)
    {
        if (contentRoot == null)
            return;

        contentRoot.anchoredPosition = -targetAnchoredPosition;
    }

    private void Update()
    {
        if (contentRoot == null || Mouse.current == null)
            return;

        if (suppressClickFrames > 0)
            suppressClickFrames--;

        HandleZoom();
        HandlePan();
    }

    private void HandleZoom()
    {
        if (!enableZoom || Mouse.current == null)
            return;

        float scrollY = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scrollY) <= 0.001f)
            return;

        float currentScale = contentRoot.localScale.x;
        float nextScale = currentScale + Mathf.Sign(scrollY) * zoomStep;
        nextScale = Mathf.Clamp(nextScale, minZoom, maxZoom);

        if (Mathf.Approximately(currentScale, nextScale))
            return;

        contentRoot.localScale = new Vector3(nextScale, nextScale, 1f);
    }

    private void HandlePan()
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (!IsMouseInsideScreen(mousePosition))
                return;

            if (IsPointerOverBlockingUI(mousePosition))
                return;

            pressed = true;
            dragging = false;
            pressScreenPosition = mousePosition;
            contentStartPosition = contentRoot.anchoredPosition;
        }

        if (pressed && Mouse.current.leftButton.isPressed)
        {
            Vector2 delta = mousePosition - pressScreenPosition;

            if (!dragging && delta.magnitude >= dragThreshold)
                dragging = true;

            if (dragging)
                contentRoot.anchoredPosition = contentStartPosition + delta;
        }

        if (pressed && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            if (dragging)
                suppressClickFrames = 2;

            pressed = false;
            dragging = false;
        }
    }

    private bool IsMouseInsideScreen(Vector2 mousePosition)
    {
        return mousePosition.x >= 0f &&
               mousePosition.y >= 0f &&
               mousePosition.x <= Screen.width &&
               mousePosition.y <= Screen.height;
    }

    private bool IsPointerOverBlockingUI(Vector2 mousePosition)
    {
        Camera eventCamera = GetEventCamera();

        if (dragBlockers == null)
            return false;

        for (int i = 0; i < dragBlockers.Length; i++)
        {
            RectTransform blocker = dragBlockers[i];
            if (blocker == null || !blocker.gameObject.activeInHierarchy)
                continue;

            if (RectTransformUtility.RectangleContainsScreenPoint(blocker, mousePosition, eventCamera))
                return true;
        }

        return false;
    }

    private Camera GetEventCamera()
    {
        if (viewportRect == null)
            return null;

        Canvas canvas = viewportRect.GetComponentInParent<Canvas>();
        if (canvas == null)
            return null;

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera;
    }
}