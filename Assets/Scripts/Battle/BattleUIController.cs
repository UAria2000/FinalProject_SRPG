using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleUIController : MonoBehaviour
{
    [Header("Current Unit Info UI")]
    [SerializeField] private CurrentUnitInfoPanel currentUnitInfoPanel;

    [Header("Player Skill Tooltip UI")]
    [SerializeField] private SkillTooltipUI skillTooltipUI;

    [Header("Enemy Info UI")]
    [SerializeField] private EnemyInfoPanel enemyInfoPanel;
    [SerializeField] private EnemyDetailPopupUI enemyDetailPopupUI;
    [SerializeField] private Button enemyDetailPopupButton;

    [Header("Enemy Skill Tooltip UI")]
    [SerializeField] private EnemySkillTooltipUI enemySkillTooltipUI;

    [Header("Buttons")]
    [SerializeField] private Button attackButton;
    [SerializeField] private Button moveButton;
    [SerializeField] private Button[] skillButtons = new Button[3];
    [SerializeField] private Image[] skillIconImages = new Image[3];
    [SerializeField] private Image[] skillCooldownOverlayImages = new Image[3];
    [SerializeField] private Button itemButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button popupLogButton;

    [Header("Round UI")]
    [SerializeField] private TMP_Text turnStartText;
    [SerializeField] private float turnStartTextShowTime = 1.0f;

    [Header("Cancel Button Colors")]
    [SerializeField] private Color cancelNormalColor = Color.white;
    [SerializeField] private Color cancelEnabledColor = new Color(0.9f, 0.25f, 0.25f, 1f);

    private Image cancelButtonImage;
    private ColorBlock cancelButtonColors;

    private BattleManager battleManager;

    public void Initialize(BattleManager manager)
    {
        battleManager = manager;

        if (cancelButton != null)
        {
            cancelButtonImage = cancelButton.GetComponent<Image>();
            cancelButtonColors = cancelButton.colors;
        }

        if (turnStartText != null)
            turnStartText.gameObject.SetActive(false);

        if (enemyDetailPopupUI != null)
            enemyDetailPopupUI.Hide();

        HideSkillTooltip();
        HideEnemySkillTooltip();
    }

    public void BindButtonEvents()
    {
        if (battleManager == null)
            return;

        if (attackButton != null)
            attackButton.onClick.AddListener(battleManager.OnAttackButtonClicked);

        if (moveButton != null)
            moveButton.onClick.AddListener(battleManager.OnMoveButtonClicked);

        for (int i = 0; i < skillButtons.Length; i++)
        {
            int skillIndex = i;

            if (skillButtons[i] != null)
            {
                skillButtons[i].onClick.AddListener(() => battleManager.OnSkillButtonClicked(skillIndex));

                SkillButtonHoverHandler hoverHandler = skillButtons[i].GetComponent<SkillButtonHoverHandler>();
                if (hoverHandler == null)
                    hoverHandler = skillButtons[i].gameObject.AddComponent<SkillButtonHoverHandler>();

                hoverHandler.Initialize(battleManager, skillIndex);
            }
        }

        if (itemButton != null)
            itemButton.onClick.AddListener(battleManager.OnItemButtonClicked);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(battleManager.OnCancelButtonClicked);

        if (popupLogButton != null)
            popupLogButton.onClick.AddListener(battleManager.OnPopupLogButtonClicked);

        if (enemyDetailPopupButton != null)
            enemyDetailPopupButton.onClick.AddListener(battleManager.OnEnemyDetailPopupButtonClicked);
    }

    public void BindEnemySkillHoverEvents(Button[] enemySkillButtons)
    {
        if (battleManager == null || enemySkillButtons == null)
            return;

        for (int i = 0; i < enemySkillButtons.Length; i++)
        {
            int skillIndex = i;

            if (enemySkillButtons[i] != null)
            {
                EnemySkillButtonHoverHandler hoverHandler = enemySkillButtons[i].GetComponent<EnemySkillButtonHoverHandler>();
                if (hoverHandler == null)
                    hoverHandler = enemySkillButtons[i].gameObject.AddComponent<EnemySkillButtonHoverHandler>();

                hoverHandler.Initialize(battleManager, skillIndex);
            }
        }
    }

    public void SetActionButtonsInteractable(bool interactable)
    {
        if (attackButton != null) attackButton.interactable = interactable;
        if (moveButton != null) moveButton.interactable = interactable;
        if (itemButton != null) itemButton.interactable = interactable;
    }

    public void SetAttackButtonInteractable(bool interactable)
    {
        if (attackButton != null)
            attackButton.interactable = interactable;
    }

    public void SetMoveButtonInteractable(bool interactable)
    {
        if (moveButton != null)
            moveButton.interactable = interactable;
    }

    public void SetItemButtonInteractable(bool interactable)
    {
        if (itemButton != null)
            itemButton.interactable = interactable;
    }

    public void RefreshCancelButtonState(bool canCancel)
    {
        if (cancelButton != null)
            cancelButton.interactable = canCancel;

        if (cancelButtonImage != null)
            cancelButtonImage.color = canCancel ? cancelEnabledColor : cancelNormalColor;

        if (cancelButton != null)
        {
            ColorBlock cb = cancelButtonColors;
            cb.normalColor = canCancel ? cancelEnabledColor : cancelNormalColor;
            cb.highlightedColor = canCancel ? cancelEnabledColor * 1.05f : cancelNormalColor * 1.05f;
            cb.selectedColor = cb.highlightedColor;
            cb.pressedColor = canCancel ? cancelEnabledColor * 0.9f : cancelNormalColor * 0.9f;
            cancelButton.colors = cb;
        }
    }

    public void ShowCurrentUnitInfo(BattleUnit unit)
    {
        if (currentUnitInfoPanel != null && unit != null)
            currentUnitInfoPanel.Show(unit);
    }

    public void HideCurrentUnitInfo()
    {
        if (currentUnitInfoPanel != null)
            currentUnitInfoPanel.Hide();
    }

    public void RefreshPlayerSkillButtons(BattleUnit displayUnit, BattleUnit currentActingUnit, bool allowUse)
    {
        for (int i = 0; i < skillButtons.Length; i++)
        {
            Button button = i < skillButtons.Length ? skillButtons[i] : null;
            Image iconImage = i < skillIconImages.Length ? skillIconImages[i] : null;
            Image overlayImage = i < skillCooldownOverlayImages.Length ? skillCooldownOverlayImages[i] : null;

            UpdatePlayerSkillButton(button, iconImage, overlayImage, displayUnit, currentActingUnit, i, allowUse);
        }
    }

    private void UpdatePlayerSkillButton(
        Button button,
        Image iconImage,
        Image overlayImage,
        BattleUnit displayUnit,
        BattleUnit currentActingUnit,
        int slotIndex,
        bool allowUse)
    {
        if (button == null)
            return;

        SkillDefinition skill = displayUnit != null ? displayUnit.GetSkillAt(slotIndex) : null;
        bool hasSkill = skill != null;

        if (iconImage != null)
        {
            iconImage.sprite = hasSkill ? skill.icon : null;
            iconImage.color = hasSkill ? Color.white : new Color(1f, 1f, 1f, 0.2f);
        }

        bool interactable = false;
        if (allowUse && displayUnit != null && displayUnit == currentActingUnit && hasSkill)
            interactable = currentActingUnit.CanUseSkill(skill);

        button.interactable = interactable;

        if (overlayImage != null)
        {
            if (!hasSkill)
            {
                overlayImage.gameObject.SetActive(false);
                overlayImage.fillAmount = 0f;
            }
            else
            {
                int remaining = displayUnit.GetRemainingCooldown(skill);
                if (remaining > 0)
                {
                    overlayImage.gameObject.SetActive(true);

                    float divisor = Mathf.Max(1f, skill.cooldownTurns + 1f);
                    overlayImage.fillAmount = Mathf.Clamp01(remaining / divisor);
                }
                else
                {
                    overlayImage.gameObject.SetActive(false);
                    overlayImage.fillAmount = 0f;
                }
            }
        }
    }

    public void RefreshEnemyInfo(BattleUnit selectedEnemy, string epitaph)
    {
        if (enemyInfoPanel != null)
            enemyInfoPanel.Show(selectedEnemy);

        if (enemyDetailPopupUI != null && enemyDetailPopupUI.IsOpen())
            enemyDetailPopupUI.Show(selectedEnemy, epitaph);
    }

    public void ToggleEnemyDetailPopup(BattleUnit selectedEnemy, string epitaph)
    {
        if (enemyDetailPopupUI == null)
            return;

        if (enemyDetailPopupUI.IsOpen())
            enemyDetailPopupUI.Hide();
        else if (selectedEnemy != null)
            enemyDetailPopupUI.Show(selectedEnemy, epitaph);
    }

    public void HideEnemyDetailPopup()
    {
        if (enemyDetailPopupUI != null)
            enemyDetailPopupUI.Hide();
    }

    public IEnumerator ShowTurnStartTextRoutine(int roundNumber)
    {
        if (turnStartText == null)
            yield break;

        turnStartText.text = $"Turn {roundNumber} Start";
        turnStartText.gameObject.SetActive(true);

        yield return new WaitForSeconds(turnStartTextShowTime);

        turnStartText.gameObject.SetActive(false);
    }

    public void ShowSkillTooltip(int skillSlotIndex, BattleUnit displayUnit, Vector2 screenPosition)
    {
        if (skillTooltipUI == null)
            return;

        if (displayUnit == null)
        {
            skillTooltipUI.Hide();
            return;
        }

        SkillDefinition skill = displayUnit.GetSkillAt(skillSlotIndex);
        if (skill == null)
        {
            skillTooltipUI.Hide();
            return;
        }

        skillTooltipUI.Show(skill, screenPosition);
    }

    public void MoveSkillTooltip(Vector2 screenPosition)
    {
        if (skillTooltipUI != null)
            skillTooltipUI.Move(screenPosition);
    }

    public void HideSkillTooltip()
    {
        if (skillTooltipUI != null)
            skillTooltipUI.Hide();
    }

    public void ShowEnemySkillTooltip(int skillSlotIndex, BattleUnit selectedEnemy, Vector2 screenPosition)
    {
        if (enemySkillTooltipUI == null)
            return;

        if (selectedEnemy == null)
        {
            enemySkillTooltipUI.Hide();
            return;
        }

        SkillDefinition skill = selectedEnemy.GetSkillAt(skillSlotIndex);
        if (skill == null)
        {
            enemySkillTooltipUI.Hide();
            return;
        }

        enemySkillTooltipUI.Show(skill, screenPosition);
    }

    public void MoveEnemySkillTooltip(Vector2 screenPosition)
    {
        if (enemySkillTooltipUI != null)
            enemySkillTooltipUI.Move(screenPosition);
    }

    public void HideEnemySkillTooltip()
    {
        if (enemySkillTooltipUI != null)
            enemySkillTooltipUI.Hide();
    }
}