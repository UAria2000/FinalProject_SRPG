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
    [SerializeField] private FleeTooltipUI fleeTooltipUI;

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
    [SerializeField] private Button captureButton;
    [SerializeField] private Image captureButtonImage;
    [SerializeField] private Sprite captureEnabledSprite;
    [SerializeField] private Sprite captureDisabledSprite;
    [SerializeField] private GameObject captureEnabledEffectRoot;
    [SerializeField] private GameObject captureDisabledEffectRoot;
    [SerializeField] private Button fleeButton;
    [SerializeField] private Button endTurnButton;
    [SerializeField] private Button inventoryButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button popupLogButton;
    [SerializeField] private Button mapButton;
    [SerializeField] private Button enemyDetailPopupButton;

    [Header("Round UI")]
    [SerializeField] private TMP_Text turnStartText;
    [SerializeField] private float turnStartTextShowTime = 1.0f;

    [Header("Cancel Button Colors")]
    [SerializeField] private Color cancelDisabledNormal = new Color(0.45f, 0.45f, 0.45f, 1f);
    [SerializeField] private Color cancelDisabledHighlighted = new Color(0.50f, 0.50f, 0.50f, 1f);
    [SerializeField] private Color cancelDisabledPressed = new Color(0.38f, 0.38f, 0.38f, 1f);
    [SerializeField] private Color cancelEnabledNormal = new Color(0.82f, 0.20f, 0.20f, 1f);
    [SerializeField] private Color cancelEnabledHighlighted = new Color(0.92f, 0.28f, 0.28f, 1f);
    [SerializeField] private Color cancelEnabledPressed = new Color(0.66f, 0.12f, 0.12f, 1f);

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
        HideFleeTooltip();

        SetBottomContext(BottomContextType.Inventory);

        ApplyButtonNavigationNone(moveButton);
        ApplyButtonNavigationNone(captureButton);
        ApplyButtonNavigationNone(fleeButton);
        ApplyButtonNavigationNone(endTurnButton);
        ApplyButtonNavigationNone(inventoryButton);
        ApplyButtonNavigationNone(cancelButton);
        ApplyButtonNavigationNone(popupLogButton);
        ApplyButtonNavigationNone(mapButton);
        ApplyButtonNavigationNone(enemyDetailPopupButton);

        for (int i = 0; i < actionButtons.Length; i++)
            ApplyButtonNavigationNone(actionButtons[i]);
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

        if (captureButton != null)
        {
            captureButton.onClick.RemoveAllListeners();
            captureButton.onClick.AddListener(battleManager.OnCaptureButtonPressed);
        }

        if (fleeButton != null)
        {
            fleeButton.onClick.RemoveAllListeners();
            fleeButton.onClick.AddListener(battleManager.OnFleeButtonPressed);

            FleeButtonHoverHandler handler = fleeButton.GetComponent<FleeButtonHoverHandler>();
            if (handler == null)
                handler = fleeButton.gameObject.AddComponent<FleeButtonHoverHandler>();
            handler.Initialize(battleManager);
        }

        if (endTurnButton != null)
        {
            endTurnButton.onClick.RemoveAllListeners();
            endTurnButton.onClick.AddListener(battleManager.OnEndTurnButtonPressed);
        }

        if (inventoryButton != null)
        {
            inventoryButton.onClick.RemoveAllListeners();
            inventoryButton.onClick.AddListener(battleManager.OnInventoryTogglePressed);
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

        if (mapButton != null)
        {
            mapButton.onClick.RemoveAllListeners();
            mapButton.onClick.AddListener(battleManager.OnMapButtonPressed);
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
                actionButtons[i].interactable = interactable && hasSkill && unit != null && unit.CanUseSkill(skill);
        }

        bool canAct = interactable && battleManager != null && battleManager.InputMode == BattleInputMode.WaitingForAction;

        if (moveButton != null)
            moveButton.interactable = canAct;

        bool canCapture = canAct && battleManager != null && battleManager.CanActorUseCaptureCommand(unit);

        if (captureButton != null)
            captureButton.interactable = canCapture;

        if (captureButtonImage != null)
        {
            if (canCapture && captureEnabledSprite != null)
                captureButtonImage.sprite = captureEnabledSprite;
            else if (!canCapture && captureDisabledSprite != null)
                captureButtonImage.sprite = captureDisabledSprite;
        }

        if (captureEnabledEffectRoot != null)
            captureEnabledEffectRoot.SetActive(canCapture);

        if (captureDisabledEffectRoot != null)
            captureDisabledEffectRoot.SetActive(!canCapture);

        if (fleeButton != null)
            fleeButton.interactable = canAct;

        if (endTurnButton != null)
            endTurnButton.interactable = canAct;

        if (inventoryButton != null)
            inventoryButton.interactable = true;

        if (mapButton != null)
            mapButton.interactable = battleManager == null || !battleManager.IsBattleInProgress;

        if (cancelButton != null)
            cancelButton.interactable = battleManager != null &&
                                        battleManager.CurrentState == TurnState.PlayerInput &&
                                        battleManager.InputMode != BattleInputMode.WaitingForAction;

        RefreshCancelButtonState();
    }

    public void RefreshInventory(BattleManager manager, PartyDefinition allyParty, int selectedIndex)
    {
        if (inventoryPanelUI == null)
            return;

        inventoryPanelUI.Bind(manager, allyParty != null ? allyParty.inventory : null, selectedIndex);
    }

    public void SetBottomContext(BottomContextType mode)
    {
        bool showEnemyInfo = mode == BottomContextType.EnemyInfo;
        bool showInventory = mode == BottomContextType.Inventory;
        bool showMap = mode == BottomContextType.Map;

        if (enemyInfoContextRoot != null)
            enemyInfoContextRoot.SetActive(showEnemyInfo);

        if (inventoryContextRoot != null)
            inventoryContextRoot.SetActive(showInventory);

        if (mapContextRoot != null)
            mapContextRoot.SetActive(showMap);

        if (inventoryPanelUI != null)
            inventoryPanelUI.Show(showInventory);

        if (!showEnemyInfo && enemyDetailPopupUI != null && enemyDetailPopupUI.IsOpen())
            enemyDetailPopupUI.Hide();
    }

    public void ShowEnemyDetailPopup(BattleUnit enemy)
    {
        if (enemyInfoContextRoot != null)
            enemyInfoContextRoot.SetActive(true);

        if (inventoryContextRoot != null)
            inventoryContextRoot.SetActive(false);

        if (mapContextRoot != null)
            mapContextRoot.SetActive(false);

        if (inventoryPanelUI != null)
            inventoryPanelUI.Show(false);

        if (enemyDetailPopupUI != null)
            enemyDetailPopupUI.Show(enemy);
    }

    public void ToggleEnemyDetailPopup(BattleUnit enemy)
    {
        if (enemyDetailPopupUI == null)
            return;

        if (enemyDetailPopupUI.IsOpen())
            HideEnemyDetailPopup();
        else
            ShowEnemyDetailPopup(enemy);
    }

    public void HideEnemyDetailPopup()
    {
        if (enemyDetailPopupUI != null)
            enemyDetailPopupUI.Hide();

        if (enemyInfoContextRoot != null)
            enemyInfoContextRoot.SetActive(false);
    }

    public bool IsEnemyDetailPopupOpen()
    {
        return enemyDetailPopupUI != null && enemyDetailPopupUI.IsOpen();
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

    public void ShowFleeTooltip(int fleeChancePercent, Vector3 screenPosition)
    {
        if (fleeTooltipUI != null)
            fleeTooltipUI.Show(fleeChancePercent, screenPosition);
    }

    public void HideFleeTooltip()
    {
        if (fleeTooltipUI != null)
            fleeTooltipUI.Hide();
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

    private void RefreshCancelButtonState()
    {
        if (cancelButton == null || battleManager == null)
            return;

        bool canCancel =
            battleManager.CurrentState == TurnState.PlayerInput &&
            (battleManager.InputMode == BattleInputMode.WaitingForSkillTarget ||
             battleManager.InputMode == BattleInputMode.WaitingForMoveTarget ||
             battleManager.InputMode == BattleInputMode.WaitingForItemTarget ||
             battleManager.InputMode == BattleInputMode.WaitingForCaptureTarget);

        cancelButton.interactable = canCancel;

        ColorBlock colors = cancelButton.colors;
        if (canCancel)
        {
            colors.normalColor = cancelEnabledNormal;
            colors.highlightedColor = cancelEnabledHighlighted;
            colors.pressedColor = cancelEnabledPressed;
            colors.selectedColor = cancelEnabledNormal;
        }
        else
        {
            colors.normalColor = cancelDisabledNormal;
            colors.highlightedColor = cancelDisabledHighlighted;
            colors.pressedColor = cancelDisabledPressed;
            colors.selectedColor = cancelDisabledNormal;
        }

        cancelButton.colors = colors;
    }

    private void ApplyButtonNavigationNone(Button button)
    {
        if (button == null)
            return;

        Navigation nav = button.navigation;
        nav.mode = Navigation.Mode.None;
        button.navigation = nav;
    }
}
