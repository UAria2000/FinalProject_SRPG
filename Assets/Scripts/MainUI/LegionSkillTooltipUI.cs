using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class LegionSkillTooltipUI : MonoBehaviour
{
    [SerializeField] private RectTransform tooltipRect;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Vector2 cursorOffset = new Vector2(24f, -24f);

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
        tooltipRect.position = new Vector3(
            mousePos.x + cursorOffset.x,
            mousePos.y + cursorOffset.y,
            0f);
    }

    public void Show(SkillDefinition skill, int skillLevel)
    {
        if (skill == null)
        {
            Hide();
            return;
        }

        if (iconImage != null)
        {
            iconImage.gameObject.SetActive(skill.icon != null);
            iconImage.sprite = skill.icon;
        }

        if (titleText != null)
            titleText.text = skill.skillName;

        if (levelText != null)
            levelText.text = skill.isBasicAttack ? "평타" : $"Lv.{Mathf.Max(1, skillLevel)}";

        if (descriptionText != null)
            descriptionText.text = skill.description;

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
            if (graphics[i] != null && graphics[i] != iconImage)
                graphics[i].raycastTarget = false;
            else if (graphics[i] == iconImage)
                graphics[i].raycastTarget = false;
        }
    }
}
