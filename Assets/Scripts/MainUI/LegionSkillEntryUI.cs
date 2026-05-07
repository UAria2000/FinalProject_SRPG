using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class LegionSkillEntryUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private GameObject root;
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private GameObject selectedFrameRoot;

    private SkillDefinition boundSkill;
    private Action<SkillDefinition> clickAction;

    public SkillDefinition BoundSkill => boundSkill;

    private void Awake()
    {
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClicked);
        }
    }

    public void Bind(SkillDefinition skill, LegionSkillTooltipUI tooltipUI)
    {
        // tooltipUI는 구형 인스펙터/호출 호환용으로만 남긴다. 스킬 설명은 호버가 아니라 클릭 시 디테일 패널 내부에 표시한다.
        Bind(skill, tooltipUI, null, false);
    }

    public void Bind(SkillDefinition skill, LegionSkillTooltipUI tooltipUI, Action<SkillDefinition> onClicked, bool selected)
    {
        boundSkill = skill;
        clickAction = onClicked;

        bool hasSkill = skill != null;
        if (root != null) root.SetActive(hasSkill); else gameObject.SetActive(hasSkill);

        if (selectedFrameRoot != null)
            selectedFrameRoot.SetActive(hasSkill && selected);

        if (button != null)
            button.interactable = hasSkill;

        if (!hasSkill)
            return;

        if (iconImage != null)
        {
            iconImage.gameObject.SetActive(skill.icon != null);
            iconImage.sprite = skill.icon;
            iconImage.color = skill.icon != null ? Color.white : new Color(1f, 1f, 1f, 0.2f);
        }
        if (nameText != null)
            nameText.text = skill.skillName;
        if (levelText != null)
            levelText.text = skill.isBasicAttack ? "평타" : string.Empty;
    }

    public void BindHidden()
    {
        boundSkill = null;
        clickAction = null;
        if (selectedFrameRoot != null)
            selectedFrameRoot.SetActive(false);
        if (button != null)
            button.interactable = false;
        if (root != null) root.SetActive(false); else gameObject.SetActive(false);
    }

    public void SetSelected(bool selected)
    {
        if (selectedFrameRoot != null)
            selectedFrameRoot.SetActive(boundSkill != null && selected);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (button != null)
            return;

        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
            return;

        HandleClicked();
    }

    private void HandleClicked()
    {
        if (boundSkill == null)
            return;

        clickAction?.Invoke(boundSkill);
    }
}
