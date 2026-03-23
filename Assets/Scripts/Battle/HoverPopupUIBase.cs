using UnityEngine;

public abstract class HoverPopupUIBase : MonoBehaviour
{
    [Header("Hover Popup")]
    [SerializeField] protected Vector2 screenOffset = new Vector2(120f, -50f);

    protected void ShowRootAt(GameObject rootObject, Vector2 pointerScreenPosition)
    {
        if (rootObject == null)
            return;

        rootObject.SetActive(true);

        RectTransform rectTransform = rootObject.transform as RectTransform;
        if (rectTransform != null)
            rectTransform.position = pointerScreenPosition + screenOffset;
    }

    protected void HideRoot(GameObject rootObject)
    {
        if (rootObject != null)
            rootObject.SetActive(false);
    }
}
