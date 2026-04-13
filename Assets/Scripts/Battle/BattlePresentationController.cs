using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class BattlePresentationController : MonoBehaviour
{
    private BattleManager battleManager;
    private BattleUIController uiController;
    private BattleViewManager viewManager;
    private GameObject popupLogPanel;
    private BottomContextType bottomContextType = BottomContextType.Inventory;

    // 아군 턴 시작 시 정보 패널 자동 표시를 위한 추적값
    private BattleUnit lastAutoShownActingAlly;

    public BottomContextType BottomContextType => bottomContextType;

    public void Initialize(BattleManager manager, BattleUIController ui, GameObject popupPanel)
    {
        battleManager = manager;
        uiController = ui;
        popupLogPanel = popupPanel;
        viewManager = battleManager != null ? battleManager.ViewManager : null;

        if (uiController != null)
            uiController.SetPresentationController(this);

        ResetForBattleStart();
    }

    public void ResetForBattleStart()
    {
        bottomContextType = BottomContextType.Inventory;
        lastAutoShownActingAlly = null;

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

        // 아군 턴이 새로 시작됐을 때만 자동으로 해당 아군 정보를 띄움
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
            // 다음 아군 턴에서 다시 자동 표시가 되도록 리셋
            lastAutoShownActingAlly = null;
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

        // 상단 턴 순서 포트레잇 클릭 / 필드 유닛 클릭 시 즉시 패널 갱신
        RefreshAllUI();
    }

    public void OnBlankBattlefieldLeftClicked()
    {
        if (battleManager == null)
            return;

        // 대상 선택 중이었다면 먼저 취소
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