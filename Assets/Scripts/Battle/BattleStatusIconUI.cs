using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 체력바 위에 표시되는 보호막/전투 기믹/상태이상 아이콘 1칸.
/// 숫자는 보호막 수치 또는 상태이상 스택 수를 표시한다.
/// </summary>
[DisallowMultipleComponent]
public class BattleStatusIconUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private GameObject countRoot;

    private RectTransform rectTransform;
    public RectTransform RectTransform
    {
        get
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();
            return rectTransform;
        }
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        AutoWireIfNeeded();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoWireIfNeeded();
    }
#endif

    public void Set(Sprite icon, int count, bool showCount)
    {
        AutoWireIfNeeded();

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
        }

        bool countActive = showCount && countText != null;
        if (countRoot != null)
            countRoot.SetActive(countActive);
        if (countText != null)
        {
            countText.gameObject.SetActive(countActive);
            countText.text = count.ToString();
        }

        gameObject.SetActive(icon != null);
    }

    public void Clear()
    {
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        if (countRoot != null)
            countRoot.SetActive(false);
        if (countText != null)
        {
            countText.text = string.Empty;
            countText.gameObject.SetActive(false);
        }

        gameObject.SetActive(false);
    }

    [ContextMenu("Auto Wire From Children")]
    public void AutoWireIfNeeded()
    {
        if (iconImage == null)
        {
            Transform icon = transform.Find("IconImage");
            if (icon != null)
                iconImage = icon.GetComponent<Image>();
        }

        if (iconImage == null)
            iconImage = GetComponentInChildren<Image>(true);

        if (countText == null)
        {
            Transform count = transform.Find("CountText");
            if (count != null)
                countText = count.GetComponent<TMP_Text>();
        }

        if (countText == null)
            countText = GetComponentInChildren<TMP_Text>(true);

        if (countRoot == null && countText != null)
            countRoot = countText.gameObject;
    }
}
