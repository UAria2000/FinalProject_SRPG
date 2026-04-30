using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 액션휠 왼쪽의 고유 마나 버튼.
/// 공용 행동 버튼과 달리 아이콘/프레임 대신 마나 텍스트와 현재/최대값을 표시한다.
/// </summary>
public class BattleActionWheelManaButtonUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private TMP_Text currentManaText;
    [SerializeField] private TMP_Text maxManaText;
    [SerializeField] private Image disabledDimImage;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Text")]
    [SerializeField] private string label = "마나";
    [SerializeField] private bool includeSlashInMaxText = true;

    public RectTransform RectTransform => transform as RectTransform;

    private void Reset()
    {
        AutoWireFromHierarchy();
    }

    private void Awake()
    {
        EnsureReferences();
        DisableChildRaycasts();
    }

    [ContextMenu("Auto Wire From Current Hierarchy")]
    public void AutoWireFromHierarchy()
    {
        button = GetComponent<Button>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        labelText = FindText("ManaLabelText");
        if (labelText == null)
            labelText = FindText("LabelText");

        currentManaText = FindText("CurrentManaText");
        maxManaText = FindText("MaxManaText");
        disabledDimImage = FindImage("DisabledDim");
    }

    public void Apply(int currentMana, int maxMana, bool interactable, UnityAction onClick)
    {
        EnsureReferences();
        DisableChildRaycasts();

        if (button != null)
            button.onClick.RemoveAllListeners();

        int safeMax = Mathf.Max(0, maxMana);
        int safeCurrent = safeMax > 0 ? Mathf.Clamp(currentMana, 0, safeMax) : Mathf.Max(0, currentMana);

        if (labelText != null)
            labelText.text = string.IsNullOrWhiteSpace(label) ? "마나" : label;

        if (currentManaText != null)
            currentManaText.text = safeCurrent.ToString();

        if (maxManaText != null)
            maxManaText.text = includeSlashInMaxText ? "/" + safeMax : safeMax.ToString();

        bool canClick = interactable && onClick != null;
        if (button != null)
        {
            button.interactable = canClick;
            if (canClick)
                button.onClick.AddListener(onClick);
        }

        if (disabledDimImage != null)
            disabledDimImage.gameObject.SetActive(!canClick);

        if (canvasGroup != null)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
        }
    }

    public void ClearListeners()
    {
        if (button != null)
            button.onClick.RemoveAllListeners();
    }

    public void SetVisible(bool visible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }
        else
        {
            gameObject.SetActive(visible);
        }
    }

    private void EnsureReferences()
    {
        if (button == null)
            button = GetComponent<Button>();
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    private void DisableChildRaycasts()
    {
        if (disabledDimImage != null)
            disabledDimImage.raycastTarget = false;
    }

    private TMP_Text FindText(string childName)
    {
        Transform child = FindDeepChild(transform, childName);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private Image FindImage(string childName)
    {
        Transform child = FindDeepChild(transform, childName);
        return child != null ? child.GetComponent<Image>() : null;
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrWhiteSpace(childName))
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
                return child;

            Transform result = FindDeepChild(child, childName);
            if (result != null)
                return result;
        }

        return null;
    }
}
