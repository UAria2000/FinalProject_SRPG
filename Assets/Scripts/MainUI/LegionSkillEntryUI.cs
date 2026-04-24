using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class LegionSkillEntryUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject root;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;

    private LegionSkillTooltipUI tooltip;
    private SkillDefinition boundSkill;

    public void Bind(SkillDefinition skill, LegionSkillTooltipUI tooltipUI)
    {
        boundSkill = skill;
        tooltip = tooltipUI;

        bool hasSkill = skill != null;
        if (root != null) root.SetActive(hasSkill); else gameObject.SetActive(hasSkill);
        if (!hasSkill)
            return;

        if (iconImage != null)
        {
            iconImage.gameObject.SetActive(skill.icon != null);
            iconImage.sprite = skill.icon;
        }
        if (nameText != null)
            nameText.text = skill.skillName;
        if (levelText != null)
            levelText.text = skill.isBasicAttack ? "평타" : string.Empty;
    }

    public void BindHidden()
    {
        boundSkill = null;
        if (root != null) root.SetActive(false); else gameObject.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (boundSkill != null)
            tooltip?.Show(boundSkill, 1);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        tooltip?.Hide();
    }
}
