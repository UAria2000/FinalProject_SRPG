using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ItemSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private GameObject lockOverlay;

    public PrisonerData myData;
    public bool canDrag = false;

    private CanvasGroup canvasGroup;
    private Vector3 originalPosition;
    private Transform originalParent;
    private ScrollRect parentScrollRect;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // 선배! Blocks Raycasts가 켜져 있어야 드래그가 시작되는 거 잊지 마세요!
        canvasGroup.blocksRaycasts = true;
        parentScrollRect = GetComponentInParent<ScrollRect>();
    }

    // 선배! 클릭하는 순간 이벤트를 선점하기 위해 이게 꼭 필요해요!
    public void OnPointerDown(PointerEventData eventData) { }

    public void SetItem(Sprite icon)
    {
        if (itemIcon == null) return;
        if (icon != null)
        {
            itemIcon.sprite = icon;
            itemIcon.gameObject.SetActive(true);
        }
        else itemIcon.gameObject.SetActive(false);
    }

    public void SetLocked(bool isLocked)
    {
        if (lockOverlay != null) lockOverlay.SetActive(isLocked);
    }

    // --- 드래그 로직 (이벤트 가두기) ---
    public void OnBeginDrag(PointerEventData eventData)
    {
        bool isLocked = lockOverlay != null && lockOverlay.activeSelf;

        // 드래그 금지 상태라면 이벤트를 그냥 '증발'시켜서 스크롤 뷰가 못 가져가게 막아요!
        if (!canDrag || isLocked || myData == null)
        {
            eventData.pointerDrag = null;
            return;
        }

        // 스크롤 뷰가 방해하지 못하게 잠시 기절시켜요.
        if (parentScrollRect != null) parentScrollRect.enabled = false;

        originalPosition = transform.position;
        originalParent = transform.parent;

        canvasGroup.blocksRaycasts = false; // 마우스가 뚫고 지나가야 드롭 구역(CorruptSlot)이 인식을 해요!
        canvasGroup.alpha = 0.6f;
        transform.SetParent(originalParent.root);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 드래그 중엔 마우스 위치로!
        if (!canvasGroup.blocksRaycasts)
        {
            transform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 스크롤 뷰 다시 깨우기!
        if (parentScrollRect != null) parentScrollRect.enabled = true;

        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        // 만약 드롭에 실패해서 부모가 여전히 루트(root)라면 제자리로 복귀!
        if (transform.parent == originalParent.root)
        {
            transform.SetParent(originalParent);
            transform.position = originalPosition;
        }
    }
}