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

    [Header("Raycast / Flicker Guard")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private bool disableChildGraphicRaycasts = true;

    private bool visible;

    private void Awake()
    {
        if (tooltipRect == null)
            tooltipRect = transform as RectTransform;

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
        if (statNameText != null) statNameText.text = statName;
        if (totalValueText != null) totalValueText.text = totalValue;
        if (baseValueText != null) baseValueText.text = baseValue;
        if (varianceValueText != null) varianceValueText.text = varianceValue;
        if (equipmentValueText != null) equipmentValueText.text = equipmentValue;

        EnsureNonBlockingRaycast();
        visible = true;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        visible = false;
        gameObject.SetActive(false);
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
