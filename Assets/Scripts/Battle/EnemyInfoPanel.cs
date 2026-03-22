using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyInfoPanel : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panelRoot;

    [Header("Basic")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text basicAttackText;

    [Header("Skill Slots")]
    [SerializeField] private Button[] skillButtons = new Button[2];
    [SerializeField] private Image[] skillIcons = new Image[2];
    [SerializeField] private Image[] skillCooldownOverlayImages = new Image[2];

    private BattleUnit currentEnemy;

    public BattleUnit CurrentEnemy => currentEnemy;

    public void Show(BattleUnit enemy)
    {
        currentEnemy = enemy;

        if (panelRoot != null)
            panelRoot.SetActive(enemy != null);

        if (enemy == null)
        {
            Clear();
            return;
        }

        if (nameText != null)
            nameText.text = enemy.Name;

        if (levelText != null)
            levelText.text = enemy.Level.ToString();

        if (hpText != null)
            hpText.text = $"{enemy.CurrentHP}/{enemy.MaxHP}";

        if (basicAttackText != null)
            basicAttackText.text = BuildBasicAttackText(enemy);

        RefreshSkillSlots();
    }

    public void Refresh()
    {
        Show(currentEnemy);
    }

    private void Clear()
    {
        if (nameText != null) nameText.text = "";
        if (levelText != null) levelText.text = "";
        if (hpText != null) hpText.text = "";
        if (basicAttackText != null) basicAttackText.text = "";

        for (int i = 0; i < skillButtons.Length; i++)
        {
            if (skillButtons[i] != null)
                skillButtons[i].interactable = false;

            if (i < skillIcons.Length && skillIcons[i] != null)
            {
                skillIcons[i].sprite = null;
                skillIcons[i].color = new Color(1f, 1f, 1f, 0.2f);
            }

            if (i < skillCooldownOverlayImages.Length && skillCooldownOverlayImages[i] != null)
            {
                skillCooldownOverlayImages[i].gameObject.SetActive(false);
                skillCooldownOverlayImages[i].fillAmount = 0f;
            }
        }
    }

    private void RefreshSkillSlots()
    {
        for (int i = 0; i < skillButtons.Length; i++)
        {
            SkillDefinition skill = currentEnemy != null ? currentEnemy.GetSkillAt(i) : null;
            bool hasSkill = skill != null;

            if (skillButtons[i] != null)
                skillButtons[i].interactable = false; // 적 패널은 정보 표시용, 클릭 사용 안 함

            if (i < skillIcons.Length && skillIcons[i] != null)
            {
                skillIcons[i].sprite = hasSkill ? skill.icon : null;
                skillIcons[i].color = hasSkill ? Color.white : new Color(1f, 1f, 1f, 0.2f);
            }

            if (i < skillCooldownOverlayImages.Length && skillCooldownOverlayImages[i] != null)
            {
                if (!hasSkill)
                {
                    skillCooldownOverlayImages[i].gameObject.SetActive(false);
                }
                else
                {
                    int remaining = currentEnemy.GetRemainingCooldown(skill);
                    if (remaining > 0)
                    {
                        skillCooldownOverlayImages[i].gameObject.SetActive(true);

                        float divisor = Mathf.Max(1f, skill.cooldownTurns + 1f);
                        skillCooldownOverlayImages[i].fillAmount = Mathf.Clamp01(remaining / divisor);
                    }
                    else
                    {
                        skillCooldownOverlayImages[i].gameObject.SetActive(false);
                        skillCooldownOverlayImages[i].fillAmount = 0f;
                    }
                }
            }
        }
    }

    private string BuildBasicAttackText(BattleUnit unit)
    {
        if (unit == null)
            return "";

        string rangeText = unit.RangeType switch
        {
            CharacterRangeType.Melee => "근거리",
            CharacterRangeType.Mid => "중거리",
            CharacterRangeType.Ranged => "원거리",
            _ => "기본"
        };

        return $"{rangeText} / {unit.DMG}";
    }
}