using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleUIController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private CurrentUnitInfoPanel currentUnitInfoPanel;
    [SerializeField] private EnemyInfoPanel enemyInfoPanel;
    [SerializeField] private EnemyDetailPopupUI enemyDetailPopupUI;
    [SerializeField] private InventoryPanelUI inventoryPanelUI;

    [Header("Tooltips")]
    [SerializeField] private SkillTooltipUI skillTooltipUI;
    [SerializeField] private EnemySkillTooltipUI enemySkillTooltipUI;
    [SerializeField] private TargetPreviewHoverUI targetPreviewHoverUI;

    [Header("Bottom Context Roots")]
    [SerializeField] private GameObject enemyInfoContextRoot;
    [SerializeField] private GameObject inventoryContextRoot;
    [SerializeField] private GameObject mapContextRoot;

    [Header("Action Buttons")]
    [SerializeField] private Button[] actionButtons = new Button[4];
    [SerializeField] private Image[] actionIcons = new Image[4];
    [SerializeField] private Image[] actionCooldownOverlays = new Image[4];
    [SerializeField] private TMP_Text[] actionCooldownTexts = new TMP_Text[4];
    [SerializeField] private Button moveButton;
    [SerializeField] private Button inventoryButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button popupLogButton;
    [SerializeField] private Button enemyDetailPopupButton;

    [Header("Round UI")]
    [SerializeField] private TMP_Text turnStartText;
    [SerializeField] private float turnStartTextShowTime = 1.0f;

    private BattleManager battleManager;

    public void Initialize(BattleManager manager)
    {
        battleManager = manager;

        if (turnStartText != null)
            turnStartText.gameObject.SetActive(false);

        if (enemyDetailPopupUI != null)
            enemyDetailPopupUI.Hide();

        HideSkillTooltip();
        HideEnemySkillTooltip();
        HideTargetPreview();
        SetBottomContext(BottomContextType.Inventory);
    }

    public void BindButtonEvents()
    {
        if (battleManager == null)
            return;

        for (int i = 0; i < actionButtons.Length; i++)
        {
            int slotIndex = i;
            if (actionButtons[i] == null)
                continue;

            actionButtons[i].onClick.RemoveAllListeners();
            actionButtons[i].onClick.AddListener(delegate { battleManager.OnActionSlotPressed(slotIndex); });

            SkillButtonHoverHandler handler = actionButtons[i].GetComponent<SkillButtonHoverHandler>();
            if (handler == null)
                handler = actionButtons[i].gameObject.AddComponent<SkillButtonHoverHandler>();
            handler.Initialize(battleManager, slotIndex);
        }

        if (moveButton != null)
        {
            moveButton.onClick.RemoveAllListeners();
            moveButton.onClick.AddListener(battleManager.OnMoveButtonPressed);
        }

        if (inventoryButton != null)
        {
            inventoryButton.interactable = true;
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(battleManager.OnCancelButtonPressed);
        }

        if (popupLogButton != null)
        {
            popupLogButton.onClick.RemoveAllListeners();
            popupLogButton.onClick.AddListener(battleManager.OnPopupLogButtonPressed);
        }

        if (enemyDetailPopupButton != null)
        {
            enemyDetailPopupButton.onClick.RemoveAllListeners();
            enemyDetailPopupButton.onClick.AddListener(battleManager.OnEnemyDetailPopupButtonPressed);
        }
    }

    public void BindEnemySkillHoverEvents(GameObject[] enemySkillTargets)
    {
        if (battleManager == null || enemySkillTargets == null)
            return;

        for (int i = 0; i < enemySkillTargets.Length; i++)
        {
            if (enemySkillTargets[i] == null)
                continue;

            EnemySkillButtonHoverHandler handler = enemySkillTargets[i].GetComponent<EnemySkillButtonHoverHandler>();
            if (handler == null)
                handler = enemySkillTargets[i].AddComponent<EnemySkillButtonHoverHandler>();
            handler.Initialize(battleManager, i);
        }
    }

    public void RefreshCurrentUnitPanel(BattleUnit unit)
    {
        if (currentUnitInfoPanel != null)
            currentUnitInfoPanel.Show(unit);
    }

    public void RefreshEnemyPanels(BattleUnit enemy)
    {
        if (enemyInfoPanel != null)
            enemyInfoPanel.Show(enemy);

        if (enemyDetailPopupUI != null && enemyDetailPopupUI.IsOpen())
            enemyDetailPopupUI.Show(enemy);
    }

    public void RefreshActionButtons(BattleUnit unit, bool interactable)
    {
        for (int i = 0; i < actionButtons.Length; i++)
        {
            SkillDefinition skill = unit != null ? unit.GetActionSkillAt(i) : null;
            bool hasSkill = skill != null;

            if (i < actionIcons.Length && actionIcons[i] != null)
            {
                actionIcons[i].sprite = hasSkill ? skill.icon : null;
                actionIcons[i].color = hasSkill ? Color.white : new Color(1f, 1f, 1f, 0.2f);
            }

            int remaining = hasSkill ? unit.GetRemainingCooldown(skill) : 0;

            if (i < actionCooldownOverlays.Length && actionCooldownOverlays[i] != null)
            {
                actionCooldownOverlays[i].gameObject.SetActive(hasSkill && remaining > 0);
                if (hasSkill && remaining > 0)
                {
                    float divisor = Mathf.Max(1f, skill.cooldownTurns);
                    actionCooldownOverlays[i].fillAmount = divisor > 0f ? Mathf.Clamp01(remaining / divisor) : 0f;
                }
                else
                {
                    actionCooldownOverlays[i].fillAmount = 0f;
                }
            }

            if (i < actionCooldownTexts.Length && actionCooldownTexts[i] != null)
                actionCooldownTexts[i].text = hasSkill && remaining > 0 ? remaining.ToString() : string.Empty;

            if (i < actionButtons.Length && actionButtons[i] != null)
                actionButtons[i].interactable = interactable && hasSkill && unit.CanUseSkill(skill);
        }

        if (moveButton != null)
            moveButton.interactable = interactable;

        if (inventoryButton != null)
            inventoryButton.interactable = interactable;
    }

    public void RefreshInventory(BattleManager manager, PartyDefinition allyParty, int selectedIndex)
    {
        if (inventoryPanelUI == null)
            return;

        inventoryPanelUI.Bind(manager, allyParty != null ? allyParty.inventory : null, selectedIndex);
    }

    public void SetBottomContext(BottomContextType mode)
    {
        if (enemyInfoContextRoot != null)
            enemyInfoContextRoot.SetActive(mode == BottomContextType.EnemyInfo);

        if (inventoryContextRoot != null)
            inventoryContextRoot.SetActive(mode == BottomContextType.Inventory);

        if (mapContextRoot != null)
            mapContextRoot.SetActive(mode == BottomContextType.Map);

        if (inventoryPanelUI != null)
            inventoryPanelUI.Show(mode == BottomContextType.Inventory);
    }

    public void ToggleEnemyDetailPopup(BattleUnit enemy)
    {
        if (enemyDetailPopupUI == null)
            return;

        if (enemyDetailPopupUI.IsOpen())
            enemyDetailPopupUI.Hide();
        else
            enemyDetailPopupUI.Show(enemy);
    }

    public void HideEnemyDetailPopup()
    {
        if (enemyDetailPopupUI != null)
            enemyDetailPopupUI.Hide();
    }

    public void ShowPlayerSkillTooltip(SkillDefinition skill, Vector3 screenPosition)
    {
        if (skillTooltipUI != null)
            skillTooltipUI.Show(skill, screenPosition);
    }

    public void HideSkillTooltip()
    {
        if (skillTooltipUI != null)
            skillTooltipUI.Hide();
    }

    public void ShowEnemySkillTooltip(SkillDefinition skill, Vector3 screenPosition)
    {
        if (enemySkillTooltipUI != null)
            enemySkillTooltipUI.Show(skill, screenPosition);
    }

    public void HideEnemySkillTooltip()
    {
        if (enemySkillTooltipUI != null)
            enemySkillTooltipUI.Hide();
    }

    public void ShowTargetPreview(TargetPreviewData data, Vector3 screenPosition)
    {
        if (targetPreviewHoverUI != null)
            targetPreviewHoverUI.Show(data, screenPosition);
    }

    public void HideTargetPreview()
    {
        if (targetPreviewHoverUI != null)
            targetPreviewHoverUI.Hide();
    }

    public IEnumerator ShowTurnStartTextRoutine(int round)
    {
        if (turnStartText == null)
            yield break;

        turnStartText.gameObject.SetActive(true);
        turnStartText.text = string.Format("Turn {0}", round);
        yield return new WaitForSeconds(turnStartTextShowTime);
        turnStartText.gameObject.SetActive(false);
    }
}
