using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Legacy tooltip kept for scene compatibility.
// Skill enhancement UI has been removed from Barracks.
public class BarracksSkillTooltipUI : MonoBehaviour
{
    [SerializeField] private RectTransform tooltipRect;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text shardTypeText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Vector2 cursorOffset = new Vector2(24f, -24f);

    private bool visible;

    private void Awake()
    {
        if (tooltipRect == null)
            tooltipRect = transform as RectTransform;
        Hide();
    }

    private void Update()
    {
        if (!visible || Mouse.current == null || tooltipRect == null)
            return;
        Vector2 mousePos = Mouse.current.position.ReadValue();
        tooltipRect.position = new Vector3(mousePos.x + cursorOffset.x, mousePos.y + cursorOffset.y, 0f);
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
            levelText.text = $"Lv.{Mathf.Max(1, skillLevel)}";
        if (shardTypeText != null)
            shardTypeText.text = string.Empty;
        if (descriptionText != null)
            descriptionText.text = skill.description;

        visible = true;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        visible = false;
        gameObject.SetActive(false);
    }
}
