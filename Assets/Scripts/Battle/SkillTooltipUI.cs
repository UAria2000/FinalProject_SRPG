using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillTooltipUI : HoverPopupUIBase
{
    [SerializeField] private GameObject root;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descText;
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private TMP_Text accuracyOrSuccessText;
    [SerializeField] private TMP_Text positionText;
    [SerializeField] private TMP_Text cooldownText;

    public virtual void Show(SkillDefinition skill, Vector2 pointerScreenPosition)
    {
        if (skill == null)
        {
            Hide();
            return;
        }

        ShowRootAt(root, pointerScreenPosition);

        if (iconImage != null) iconImage.sprite = skill.icon;
        if (nameText != null) nameText.text = skill.skillName;
        if (descText != null) descText.text = skill.description;

        if (typeText != null)
            typeText.text = skill.resolutionMode == SkillResolutionMode.Attack ? "공격형" : "성공판정형";

        if (accuracyOrSuccessText != null)
        {
            if (skill.resolutionMode == SkillResolutionMode.Attack)
                accuracyOrSuccessText.text = $"명중계수 {Mathf.RoundToInt(skill.accuracyCoefficientPercent)}%";
            else
                accuracyOrSuccessText.text = "효과별 성공률 사용";
        }

        if (positionText != null)
            positionText.text = $"사용 {skill.GetUsablePositionText()} / 대상 {skill.GetTargetPositionText()}";

        if (cooldownText != null)
            cooldownText.text = $"CD {skill.cooldownTurns}";
    }

    public virtual void Hide()
    {
        HideRoot(root);
    }
}
