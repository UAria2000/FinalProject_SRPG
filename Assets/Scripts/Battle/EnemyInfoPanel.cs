using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class EnemyInfoPanel : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text nameValueText;
    [SerializeField] private TMP_Text levelValueText;
    [SerializeField] private TMP_Text hpValueText;
    [SerializeField] private Button lastWillButton;

    [Header("Skill Preview")]
    [SerializeField] private GameObject[] skillSlotRoots = new GameObject[4];
    [SerializeField] private Image[] skillIcons = new Image[4];
    [SerializeField] private Image[] cooldownOverlays = new Image[4];
    [SerializeField] private TMP_Text[] cooldownTexts = new TMP_Text[4];

    private BattleUnit currentEnemy;

    public BattleUnit CurrentEnemy => currentEnemy;

    public void SetLastWillButtonAction(UnityAction action)
    {
        if (lastWillButton == null)
            return;

        lastWillButton.onClick.RemoveAllListeners();
        if (action != null)
            lastWillButton.onClick.AddListener(action);
    }

    public void Show(BattleUnit enemy)
    {
        currentEnemy = enemy;

        if (enemy == null)
        {
            Hide();
            return;
        }

        if (root != null)
            root.SetActive(true);

        if (portraitImage != null)
        {
            portraitImage.sprite = enemy.BustPortraitSprite;
            portraitImage.color = enemy.BustPortraitSprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        }

        if (nameValueText != null) nameValueText.text = enemy.Name;
        if (levelValueText != null) levelValueText.text = enemy.CurrentLevel.ToString();
        if (hpValueText != null) hpValueText.text = $"{enemy.CurrentHP}/{enemy.MaxHP}";

        if (lastWillButton != null)
            lastWillButton.gameObject.SetActive(true);

        for (int i = 0; i < 4; i++)
        {
            SkillDefinition skill = enemy.GetActionSkillAt(i);
            bool hasSkill = skill != null;

            if (i < skillSlotRoots.Length && skillSlotRoots[i] != null)
                skillSlotRoots[i].SetActive(true);

            if (i < skillIcons.Length && skillIcons[i] != null)
            {
                skillIcons[i].sprite = hasSkill ? skill.icon : null;
                skillIcons[i].color = hasSkill ? Color.white : new Color(1f, 1f, 1f, 0.2f);
            }

            int remaining = hasSkill ? enemy.GetRemainingCooldown(skill) : 0;
            if (i < cooldownOverlays.Length && cooldownOverlays[i] != null)
            {
                cooldownOverlays[i].gameObject.SetActive(hasSkill && remaining > 0);
                cooldownOverlays[i].fillAmount = hasSkill && remaining > 0
                    ? Mathf.Clamp01(remaining / Mathf.Max(1f, skill.cooldownTurns))
                    : 0f;
            }

            if (i < cooldownTexts.Length && cooldownTexts[i] != null)
                cooldownTexts[i].text = hasSkill && remaining > 0 ? remaining.ToString() : string.Empty;
        }
    }

    public void Refresh()
    {
        Show(currentEnemy);
    }

    public void Hide()
    {
        currentEnemy = null;
        if (root != null)
            root.SetActive(false);
    }
}