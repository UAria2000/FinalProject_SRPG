using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CorruptSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private Image portraitDisplay;
    [SerializeField] private GameObject emptyVisual;
    [SerializeField] private GameObject cancelOverlay;
    [SerializeField] private Button cancelButton;

    private PrisonerData currentPrisoner;

    void Awake()
    {
        if (cancelButton != null) cancelButton.onClick.AddListener(CancelCorruption);
        if (cancelOverlay != null) cancelOverlay.SetActive(false);
        if (portraitDisplay != null) portraitDisplay.gameObject.SetActive(false);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            ItemSlot draggedSlot = eventData.pointerDrag.GetComponent<ItemSlot>();

            if (draggedSlot != null && draggedSlot.myData != null && currentPrisoner == null)
            {
                currentPrisoner = draggedSlot.myData;
                currentPrisoner.isCorrupting = true;

                if (portraitDisplay != null)
                {
                    portraitDisplay.sprite = currentPrisoner.portrait;
                    portraitDisplay.gameObject.SetActive(true);

                    RectTransform rect = portraitDisplay.GetComponent<RectTransform>();

                    // 1. 피벗과 앵커를 상단 중앙으로 확실히 고정!
                    rect.pivot = new Vector2(0.5f, 1f);
                    rect.anchorMin = new Vector2(0.5f, 1f);
                    rect.anchorMax = new Vector2(0.5f, 1f);
                    rect.anchoredPosition = Vector2.zero;

                    // 2. 일단 원본 크기로 키운 다음...
                    portraitDisplay.SetNativeSize();

                    // 3. ★ 핵심: 가로폭이 마스크(부모)보다 작거나 너무 크면 안 되니까 가로폭을 맞춤!
                    // 부모(PortraitMask)의 너비를 가져와서 내 너비로 설정해요.
                    float parentWidth = rect.parent.GetComponent<RectTransform>().rect.width;
                    float ratio = parentWidth / rect.sizeDelta.x;
                    rect.sizeDelta = new Vector2(parentWidth, rect.sizeDelta.y * ratio);
                }

                if (emptyVisual != null) emptyVisual.SetActive(false);

                Destroy(eventData.pointerDrag);

                PrisonerManager.Instance.OnPrisonerListChanged?.Invoke();
                Debug.Log($"{currentPrisoner.prisonerName} 타락 시작, 선배!");
            }
        }
    }

    // --- 마우스 오버 및 취소 로직은 기존과 동일 ---
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentPrisoner != null && cancelOverlay != null) cancelOverlay.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (cancelOverlay != null) cancelOverlay.SetActive(false);
    }

    public void CancelCorruption()
    {
        if (currentPrisoner == null) return;
        currentPrisoner.isCorrupting = false;
        currentPrisoner = null;
        if (portraitDisplay != null) portraitDisplay.gameObject.SetActive(false);
        if (emptyVisual != null) emptyVisual.SetActive(true);
        if (cancelOverlay != null) cancelOverlay.SetActive(false);
        PrisonerManager.Instance.OnPrisonerListChanged?.Invoke();
    }
}