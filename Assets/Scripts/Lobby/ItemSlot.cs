using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ItemSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerClickHandler
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private GameObject lockOverlay;

    public PrisonerData myData;
    public KeeperData myKeeperData;
    public bool canDrag = false;

    private CanvasGroup canvasGroup;
    private Vector3 originalPosition;
    private Transform originalParent;
    private int originalIndex;
    private ScrollRect parentScrollRect;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = true;
        parentScrollRect = GetComponentInParent<ScrollRect>();
    }

    public void OnPointerDown(PointerEventData eventData) { }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (myKeeperData != null && KeeperInfoUI.Instance != null)
        {
            KeeperInfoUI.Instance.UpdateDisplay(myKeeperData);
        }
    }

    public void SetItem(Sprite icon)
    {
        if (itemIcon == null) return;
        if (icon != null)
        {
            itemIcon.sprite = icon;
            itemIcon.gameObject.SetActive(true);
            itemIcon.color = Color.white;
        }
        else itemIcon.gameObject.SetActive(false);
    }

    public void SetLocked(bool isLocked)
    {
        if (lockOverlay != null) lockOverlay.SetActive(isLocked);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        bool isLocked = lockOverlay != null && lockOverlay.activeSelf;
        if (!canDrag || isLocked || (myData == null && myKeeperData == null))
        {
            eventData.pointerDrag = null;
            return;
        }

        if (parentScrollRect != null) parentScrollRect.enabled = false;
        originalPosition = transform.position;
        originalParent = transform.parent;
        originalIndex = transform.GetSiblingIndex();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.6f;
        transform.SetParent(originalParent.root);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!canvasGroup.blocksRaycasts) transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (parentScrollRect != null) parentScrollRect.enabled = true;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;
        if (transform.parent == originalParent.root)
        {
            transform.SetParent(originalParent);
            transform.SetSiblingIndex(originalIndex);
            transform.position = originalPosition;
        }
    }
}