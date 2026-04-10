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
    [SerializeField] private bool clampPanToBounds = true;

    [Header("Dynamic Pan Padding")]
    [SerializeField] private bool useDynamicPanPadding = true;
    [SerializeField] private Vector2 panPaddingPercent = new Vector2(0.15f, 0.15f);
    [SerializeField] private Vector2 minPanPadding = new Vector2(120f, 120f);
    [SerializeField] private Vector2 maxPanPadding = new Vector2(450f, 350f);

    [Header("Fallback Fixed Pan Padding")]
    [SerializeField] private Vector2 panPadding = new Vector2(300f, 220f);

    [Header("Zoom")]
    [SerializeField] private bool enableZoom = true;
    [SerializeField] private float zoomStep = 0.1f;
    [SerializeField] private float minZoom = 0.5f;
    [SerializeField] private float maxZoom = 2.0f;
    [SerializeField] private bool clampToMinAfterConfigure = true;

    [Header("Initial Fit")]
    [SerializeField] private bool autoFitInitialZoom = true;
    [SerializeField] private Vector2 initialFitPadding = new Vector2(220f, 220f);

    private bool pressed;
    private bool dragging;
    private bool inputLocked;
    private Vector2 pressScreenPosition;
    private Vector2 contentStartPosition;
    private int suppressClickFrames;

    private Bounds contentBounds;
    private bool hasContentBounds;

    public float CurrentZoom => contentRoot != null ? contentRoot.localScale.x : 1f;
    public bool IsDragging => dragging;

    public void SetInputLocked(bool locked)
    {
        inputLocked = locked;

        if (inputLocked)
        {
            pressed = false;
            dragging = false;
        }
    }

    public void Configure(RectTransform inContentRoot)
    {
        contentRoot = inContentRoot;

        if (contentRoot == null)
            return;

        if (clampToMinAfterConfigure)
        {
            float clamped = Mathf.Clamp(contentRoot.localScale.x, minZoom, maxZoom);
            contentRoot.localScale = new Vector3(clamped, clamped, 1f);
        }
    }

    public void SetContentBounds(Bounds bounds)
    {
        contentBounds = bounds;
        hasContentBounds = true;

        if (autoFitInitialZoom)
            FitContentToViewport();

        ClampContentToBounds();
    }

    public bool ShouldSuppressClick()
    {
        return inputLocked || dragging || suppressClickFrames > 0;
    }

    public void CenterOnAnchoredPosition(Vector2 targetAnchoredPosition)
    {
        if (contentRoot == null)
            return;

        float scale = Mathf.Max(0.0001f, contentRoot.localScale.x);
        Vector2 centered = -targetAnchoredPosition * scale;
        contentRoot.anchoredPosition = centered;

        ClampContentToBounds();
    }

    private void Update()
    {
        if (contentRoot == null || Mouse.current == null)
            return;

        if (suppressClickFrames > 0)
            suppressClickFrames--;

        if (inputLocked)
            return;

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
        ClampContentToBounds();
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
            {
                contentRoot.anchoredPosition = contentStartPosition + delta;
                ClampContentToBounds();
            }
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

    private void FitContentToViewport()
    {
        if (!hasContentBounds || viewportRect == null || contentRoot == null)
            return;

        float contentWidth = Mathf.Max(1f, contentBounds.size.x);
        float contentHeight = Mathf.Max(1f, contentBounds.size.y);

        float viewportWidth = Mathf.Max(1f, viewportRect.rect.width - initialFitPadding.x * 2f);
        float viewportHeight = Mathf.Max(1f, viewportRect.rect.height - initialFitPadding.y * 2f);

        float fitScaleX = viewportWidth / contentWidth;
        float fitScaleY = viewportHeight / contentHeight;
        float fitScale = Mathf.Min(fitScaleX, fitScaleY);
        fitScale = Mathf.Clamp(fitScale, minZoom, maxZoom);

        contentRoot.localScale = new Vector3(fitScale, fitScale, 1f);
    }

    private void ClampContentToBounds()
    {
        if (!clampPanToBounds || !hasContentBounds || viewportRect == null || contentRoot == null)
            return;

        float scale = Mathf.Max(0.0001f, contentRoot.localScale.x);

        float contentHalfWidth = contentBounds.extents.x * scale;
        float contentHalfHeight = contentBounds.extents.y * scale;

        float viewportHalfWidth = viewportRect.rect.width * 0.5f;
        float viewportHalfHeight = viewportRect.rect.height * 0.5f;

        Vector2 effectivePadding = GetEffectivePanPadding();

        float minX = -contentHalfWidth + viewportHalfWidth - effectivePadding.x;
        float maxX = contentHalfWidth - viewportHalfWidth + effectivePadding.x;

        float minY = -contentHalfHeight + viewportHalfHeight - effectivePadding.y;
        float maxY = contentHalfHeight - viewportHalfHeight + effectivePadding.y;

        Vector2 anchored = contentRoot.anchoredPosition;

        if (minX > maxX)
        {
            float centerX = (minX + maxX) * 0.5f;
            anchored.x = centerX;
        }
        else
        {
            anchored.x = Mathf.Clamp(anchored.x, minX, maxX);
        }

        if (minY > maxY)
        {
            float centerY = (minY + maxY) * 0.5f;
            anchored.y = centerY;
        }
        else
        {
            anchored.y = Mathf.Clamp(anchored.y, minY, maxY);
        }

        contentRoot.anchoredPosition = anchored;
    }

    private Vector2 GetEffectivePanPadding()
    {
        if (!useDynamicPanPadding || viewportRect == null)
            return panPadding;

        Vector2 viewportSize = viewportRect.rect.size;
        Vector2 dynamic = new Vector2(
            viewportSize.x * panPaddingPercent.x,
            viewportSize.y * panPaddingPercent.y
        );

        dynamic.x = Mathf.Clamp(dynamic.x, minPanPadding.x, maxPanPadding.x);
        dynamic.y = Mathf.Clamp(dynamic.y, minPanPadding.y, maxPanPadding.y);
        return dynamic;
    }
}