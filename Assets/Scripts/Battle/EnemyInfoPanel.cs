using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyInfoPanel : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text nameValueText;
    [SerializeField] private TMP_Text levelValueText;
    [SerializeField] private TMP_Text hpValueText;

    [Header("Skill Preview")]
    [SerializeField] private GameObject[] skillSlotRoots = new GameObject[4];
    [SerializeField] private Image[] skillIcons = new Image[4];
    [SerializeField] private Image[] cooldownOverlays = new Image[4];
    [SerializeField] private TMP_Text[] cooldownTexts = new TMP_Text[4];

    private BattleUnit currentEnemy;

    public BattleUnit CurrentEnemy { get { return currentEnemy; } }

    public void Show(BattleUnit enemy)
    {
        currentEnemy = enemy;

        if (root != null)
            root.SetActive(enemy != null);

        if (enemy == null)
        {
            Clear();
            return;
        }

        if (nameValueText != null) nameValueText.text = enemy.Name;
        if (levelValueText != null) levelValueText.text = enemy.CurrentLevel.ToString();
        if (hpValueText != null) hpValueText.text = string.Format("{0}/{1}", enemy.CurrentHP, enemy.MaxHP);

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
                if (hasSkill && remaining > 0)
                {
                    float divisor = Mathf.Max(1f, skill.cooldownTurns);
                    cooldownOverlays[i].fillAmount = divisor > 0f ? Mathf.Clamp01(remaining / divisor) : 0f;
                }
                else
                {
                    cooldownOverlays[i].fillAmount = 0f;
                }
            }

            if (i < cooldownTexts.Length && cooldownTexts[i] != null)
                cooldownTexts[i].text = hasSkill && remaining > 0 ? remaining.ToString() : string.Empty;
        }
    }

    public void Refresh()
    {
        Show(currentEnemy);
    }

    private void Clear()
    {
        if (nameValueText != null) nameValueText.text = string.Empty;
        if (levelValueText != null) levelValueText.text = string.Empty;
        if (hpValueText != null) hpValueText.text = string.Empty;

        for (int i = 0; i < 4; i++)
        {
            if (i < skillIcons.Length && skillIcons[i] != null)
            {
                skillIcons[i].sprite = null;
                skillIcons[i].color = new Color(1f, 1f, 1f, 0.2f);
            }
            if (i < cooldownOverlays.Length && cooldownOverlays[i] != null)
            {
                cooldownOverlays[i].gameObject.SetActive(false);
                cooldownOverlays[i].fillAmount = 0f;
            }
            if (i < cooldownTexts.Length && cooldownTexts[i] != null)
                cooldownTexts[i].text = string.Empty;
        }
    }
}
