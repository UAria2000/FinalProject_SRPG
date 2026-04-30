using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class BattlePresentationController : MonoBehaviour
{
    private BattleManager battleManager;
    private BattleUIController uiController;
    private BattleViewManager viewManager;
    [Header("Stage Camera")]
    [SerializeField] private BattleStageCameraController stageCameraController;
    private GameObject popupLogPanel;
    private BottomContextType bottomContextType = BottomContextType.Inventory;

    // �Ʊ� �� ���� �� ���� �г� �ڵ� ǥ�ø� ���� ������
    private BattleUnit lastAutoShownActingAlly;
    private BattleUnit lastCameraFocusedActingUnit;

    public BottomContextType BottomContextType => bottomContextType;
    public BattleStageCameraController StageCameraController => stageCameraController;

    public void Initialize(BattleManager manager, BattleUIController ui, GameObject popupPanel)
    {
        battleManager = manager;
        uiController = ui;
        popupLogPanel = popupPanel;
        viewManager = battleManager != null ? battleManager.ViewManager : null;

        if (stageCameraController == null)
            stageCameraController = GetComponent<BattleStageCameraController>();
        if (stageCameraController == null)
            stageCameraController = FindFirstObjectByType<BattleStageCameraController>();
        if (stageCameraController != null)
            stageCameraController.Initialize(viewManager);

        if (uiController != null)
            uiController.SetPresentationController(this);

        ResetForBattleStart();
    }

    public void ResetForBattleStart()
    {
        bottomContextType = BottomContextType.Inventory;
        lastAutoShownActingAlly = null;
        lastCameraFocusedActingUnit = null;

        if (popupLogPanel != null)
            popupLogPanel.SetActive(false);

        if (battleManager != null)
            battleManager.ClearInfoSelections();

        if (uiController != null)
        {
            uiController.HideEnemyDetailPopup();
            uiController.HideTargetPreview();
            uiController.HideSkillTooltip();
            uiController.HideEnemySkillTooltip();
            uiController.HideFleeTooltip();
            uiController.HandleBlankFieldLeftClick();
            uiController.SetBottomContext(bottomContextType);
        }
    }

    public void RefreshAllUI()
    {
        if (battleManager == null || uiController == null)
            return;

        BattleUnit actingAlly =
            battleManager.CurrentActingUnit != null &&
            battleManager.CurrentActingUnit.Team == TeamType.Ally &&
            battleManager.IsUnitInBattle(battleManager.CurrentActingUnit)
                ? battleManager.CurrentActingUnit
                : null;

        bool canPlayerAct =
            battleManager.CurrentState == TurnState.PlayerInput &&
            actingAlly != null;

        // �Ʊ� ���� ���� ���۵��� ���� �ڵ����� �ش� �Ʊ� ������ ���
        if (canPlayerAct)
        {
            if (actingAlly != lastAutoShownActingAlly)
            {
                battleManager.SelectedAllyInfoUnit = actingAlly;
                lastAutoShownActingAlly = actingAlly;
            }
        }
        else
        {
            // ���� �Ʊ� �Ͽ��� �ٽ� �ڵ� ǥ�ð� �ǵ��� ����
            lastAutoShownActingAlly = null;
        }

        BattleUnit focusUnit = battleManager.CurrentActingUnit;
        if (focusUnit != null && focusUnit != lastCameraFocusedActingUnit && battleManager.IsUnitInBattle(focusUnit))
        {
            stageCameraController?.FocusUnitSmooth(focusUnit);
            lastCameraFocusedActingUnit = focusUnit;
        }
        else if (focusUnit == null)
        {
            lastCameraFocusedActingUnit = null;
        }

        BattleUnit selectedAlly =
            battleManager.IsUnitInBattle(battleManager.SelectedAllyInfoUnit)
                ? battleManager.SelectedAllyInfoUnit
                : null;

        BattleUnit selectedEnemy =
            battleManager.IsUnitInBattle(battleManager.SelectedEnemyInfoUnit)
                ? battleManager.SelectedEnemyInfoUnit
                : null;

        uiController.RefreshInfoPanels(selectedAlly, selectedEnemy);
        uiController.RefreshActionButtons(actingAlly, canPlayerAct);
        uiController.RefreshActionWheel(actingAlly, canPlayerAct);
        uiController.RefreshInventory(
            battleManager,
            battleManager.GetActiveAllyInventory(),
            battleManager.SelectedInventoryIndex);
        uiController.RefreshTurnOrderStrip(
            battleManager.CurrentRoundTurnOrder,
            battleManager.CurrentRoundTurnCursor);
        uiController.SetBottomContext(bottomContextType);

        if (viewManager != null)
            viewManager.RefreshBattleVisualStates(battleManager);
    }

    public void NotifyUnitLeftBattle(BattleUnit unit)
    {
        if (unit == null || battleManager == null)
            return;

        battleManager.ClearTargetMarkers();

        if (battleManager.SelectedAllyInfoUnit == unit)
            battleManager.SelectedAllyInfoUnit = null;

        if (battleManager.SelectedEnemyInfoUnit == unit)
            battleManager.SelectedEnemyInfoUnit = null;

        if (lastAutoShownActingAlly == unit)
            lastAutoShownActingAlly = null;

        if (uiController != null)
        {
            uiController.HideTargetPreview();
            uiController.HideSkillTooltip();
            uiController.HideEnemySkillTooltip();
            uiController.HideFleeTooltip();
            uiController.HideEnemyDetailPopup();
        }
    }

    public void SelectUnitForInfo(BattleUnit unit)
    {
        if (battleManager == null || unit == null)
            return;

        if (unit.Team == TeamType.Ally)
        {
            battleManager.SelectedAllyInfoUnit = unit;
        }
        else
        {
            if (battleManager.SelectedEnemyInfoUnit != unit &&
                uiController != null &&
                uiController.IsEnemyDetailPopupOpen())
            {
                uiController.HideEnemyDetailPopup();
            }

            battleManager.SelectedEnemyInfoUnit = unit;
        }

        // ��� �� ���� ��Ʈ���� Ŭ�� / �ʵ� ���� Ŭ�� �� ��� �г� ����
        stageCameraController?.FocusUnitInstant(unit);

        RefreshAllUI();
    }

    public void OnBlankBattlefieldLeftClicked()
    {
        if (battleManager == null)
            return;

        // ��� ���� ���̾��ٸ� ���� ���
        if (battleManager.InputMode != BattleInputMode.WaitingForAction &&
            battleManager.CurrentState == TurnState.PlayerInput &&
            battleManager.InputController != null)
        {
            battleManager.InputController.CancelCurrentInput();
        }

        battleManager.ClearInfoSelections();

        if (uiController != null)
        {
            uiController.HideEnemyDetailPopup();
            uiController.HandleBlankFieldLeftClick();
        }

        ClearUISelection();
        RefreshAllUI();
    }

    public void OnInventoryTogglePressed()
    {
        bottomContextType = BottomContextType.Inventory;

        if (uiController != null)
            uiController.HideEnemyDetailPopup();

        ClearUISelection();
        RefreshAllUI();
    }

    public void OnMapButtonPressed()
    {
        bottomContextType = BottomContextType.Map;

        if (uiController != null)
            uiController.HideEnemyDetailPopup();

        ClearUISelection();
        RefreshAllUI();
    }

    public void OnPopupLogButtonPressed()
    {
        if (popupLogPanel != null)
            popupLogPanel.SetActive(!popupLogPanel.activeSelf);

        ClearUISelection();
    }

    public void OnEnemyDetailPopupButtonPressed()
    {
        if (uiController == null || battleManager == null)
            return;

        bool isClosingCurrentPopup = uiController.IsEnemyDetailPopupOpen();

        if (isClosingCurrentPopup)
            uiController.HideEnemyDetailPopup();
        else
            uiController.ShowEnemyDetailPopup(battleManager.SelectedEnemyInfoUnit);

        ClearUISelection();
        RefreshAllUI();
    }

    public void OnPlayerSkillButtonHoverEnter(int slotIndex, Vector3 screenPosition)
    {
        if (battleManager == null)
            return;

        BattleUnit unit =
            battleManager.CurrentActingUnit != null && battleManager.CurrentActingUnit.Team == TeamType.Ally
                ? battleManager.CurrentActingUnit
                : battleManager.SelectedAllyInfoUnit;

        SkillDefinition skill = unit != null ? unit.GetActionSkillAt(slotIndex) : null;

        if (skill != null && uiController != null)
            uiController.ShowPlayerSkillTooltip(skill, screenPosition);
    }

    public void OnPlayerSkillButtonHoverExit()
    {
        uiController?.HideSkillTooltip();
    }

    public void OnFleeButtonHoverEnter(Vector3 screenPosition)
    {
        if (uiController == null ||
            battleManager == null ||
            battleManager.CurrentState != TurnState.PlayerInput ||
            battleManager.CurrentActingUnit == null ||
            battleManager.CurrentActingUnit.Team != TeamType.Ally)
            return;

        int fleeChancePercent = BattleCalculator.CalculateFleeChancePercent(
            battleManager.CurrentActingUnit,
            battleManager.EnemyFormation);

        uiController.ShowFleeTooltip(fleeChancePercent, screenPosition);
    }

    public void OnFleeButtonHoverExit()
    {
        uiController?.HideFleeTooltip();
    }

    public void OnEnemySkillHoverEnter(int slotIndex, Vector3 screenPosition)
    {
        if (battleManager == null || battleManager.SelectedEnemyInfoUnit == null || uiController == null)
            return;

        SkillDefinition skill = battleManager.SelectedEnemyInfoUnit.GetActionSkillAt(slotIndex);

        if (skill != null)
            uiController.ShowEnemySkillTooltip(skill, screenPosition);
    }

    public void OnEnemySkillHoverExit()
    {
        uiController?.HideEnemySkillTooltip();
    }

    public void ClearUISelection()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }
}