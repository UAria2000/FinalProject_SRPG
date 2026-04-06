using UnityEngine;

public class WorldMapDragPan : MonoBehaviour
{
    [SerializeField] private RectTransform viewportRect;
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private float dragThreshold = 12f;

    private bool pressed;
    private bool dragging;
    private Vector2 pressScreenPosition;
    private Vector2 contentStartPosition;
    private int suppressClickFrames;

    public void Configure(RectTransform inContentRoot)
    {
        contentRoot = inContentRoot;
    }

    public bool ShouldSuppressClick()
    {
        return dragging || suppressClickFrames > 0;
    }

    private void Update()
    {
        if (contentRoot == null || viewportRect == null)
            return;

        if (suppressClickFrames > 0)
            suppressClickFrames--;

        Vector2 mousePosition = Input.mousePosition;
        bool pointerInside = RectTransformUtility.RectangleContainsScreenPoint(viewportRect, mousePosition, null);

        if (Input.GetMouseButtonDown(0) && pointerInside)
        {
            pressed = true;
            dragging = false;
            pressScreenPosition = mousePosition;
            contentStartPosition = contentRoot.anchoredPosition;
        }

        if (pressed && Input.GetMouseButton(0))
        {
            Vector2 delta = mousePosition - pressScreenPosition;
            if (!dragging && delta.magnitude >= dragThreshold)
                dragging = true;

            if (dragging)
                contentRoot.anchoredPosition = contentStartPosition + delta;
        }

        if (pressed && Input.GetMouseButtonUp(0))
        {
            if (dragging)
                suppressClickFrames = 2;

            pressed = false;
            dragging = false;
        }
    }
}
