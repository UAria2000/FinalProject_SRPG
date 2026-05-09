using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class LegionStatTooltipUI : MonoBehaviour
{
    [SerializeField] private RectTransform tooltipRect;
    [SerializeField] private TMP_Text statNameText;
    [SerializeField] private TMP_Text totalValueText;
    [SerializeField] private TMP_Text baseValueText;
    [SerializeField] private TMP_Text varianceValueText;
    [SerializeField] private TMP_Text equipmentValueText;
    [SerializeField] private Vector2 cursorOffset = new Vector2(20f, -20f);

    [Header("Value Colors")]
    [SerializeField] private Color positiveVarianceColor = new Color(0.3568628f, 0.8313726f, 0.3568628f, 1f); // #5BD45B
    [SerializeField] private Color negativeVarianceColor = new Color(1f, 0.4f, 0.4f, 1f); // #FF6666
    [SerializeField] private Color equipmentValueColor = new Color(0.2941177f, 0.6352941f, 0.9568628f, 1f); // #4BA2F4

    private Color statNameDefaultColor = Color.white;
    private Color totalDefaultColor = Color.white;
    private Color baseDefaultColor = Color.white;
    private Color varianceDefaultColor = Color.white;
    private Color equipmentDefaultColor = Color.white;
    private bool defaultColorsCached;

    [Header("Raycast / Flicker Guard")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private bool disableChildGraphicRaycasts = true;

    private bool visible;

    private void Awake()
    {
        if (tooltipRect == null)
            tooltipRect = transform as RectTransform;

        CacheDefaultColors();
        EnsureNonBlockingRaycast();
        Hide();
    }

    private void OnEnable()
    {
        EnsureNonBlockingRaycast();
    }

    private void Update()
    {
        if (!visible || Mouse.current == null || tooltipRect == null)
            return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        tooltipRect.position = mousePos + cursorOffset;
    }

    public void Show(string statName, string totalValue, string baseValue, string varianceValue, string equipmentValue)
    {
        Show(statName, totalValue, baseValue, varianceValue, equipmentValue, 0, 0);
    }

    public void Show(
        string statName,
        string totalValue,
        string baseValue,
        string varianceValue,
        string equipmentValue,
        int rawVarianceValue,
        int rawEquipmentValue)
    {
        CacheDefaultColors();

        if (statNameText != null)
        {
            statNameText.text = statName;
            statNameText.color = statNameDefaultColor;
        }

        if (totalValueText != null)
        {
            totalValueText.text = totalValue;
            totalValueText.color = totalDefaultColor;
        }

        if (baseValueText != null)
        {
            baseValueText.text = baseValue;
            baseValueText.color = baseDefaultColor;
        }

        if (varianceValueText != null)
        {
            varianceValueText.text = varianceValue;
            varianceValueText.color = rawVarianceValue > 0
                ? positiveVarianceColor
                : rawVarianceValue < 0
                    ? negativeVarianceColor
                    : varianceDefaultColor;
        }

        if (equipmentValueText != null)
        {
            equipmentValueText.text = equipmentValue;
            equipmentValueText.color = rawEquipmentValue != 0 ? equipmentValueColor : equipmentDefaultColor;
        }

        EnsureNonBlockingRaycast();
        visible = true;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        visible = false;
        gameObject.SetActive(false);
    }

    private void CacheDefaultColors()
    {
        if (defaultColorsCached)
            return;

        if (statNameText != null) statNameDefaultColor = statNameText.color;
        if (totalValueText != null) totalDefaultColor = totalValueText.color;
        if (baseValueText != null) baseDefaultColor = baseValueText.color;
        if (varianceValueText != null) varianceDefaultColor = varianceValueText.color;
        if (equipmentValueText != null) equipmentDefaultColor = equipmentValueText.color;

        defaultColorsCached = true;
    }

    private void EnsureNonBlockingRaycast()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        if (!disableChildGraphicRaycasts)
            return;

        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null)
                graphics[i].raycastTarget = false;
        }
    }
}
