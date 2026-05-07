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

    [Header("Blank Battlefield Click")]
    [Tooltip("빈 전장 영역을 좌클릭했을 때 아군/적군 정보 패널과 적 상세 팝업을 닫을지 여부입니다. 끄면 외부 클릭으로 UI가 닫히지 않습니다.")]
    [SerializeField] private bool closeInfoPanelsOnBlankBattlefieldLeftClick = false;

    [Tooltip("빈 전장 영역을 좌클릭했을 때 현재 스킬/이동/아이템/포획 대상 선택을 취소할지 여부입니다. 우클릭 취소 구조라면 끄는 것을 추천합니다.")]
    [SerializeField] private bool cancelPendingActionOnBlankBattlefieldLeftClick = false;

    [Tooltip("빈 전장 영역 좌클릭 시 Unity UI의 현재 선택 상태만 해제합니다. 정보 패널은 닫지 않습니다.")]
    [SerializeField] private bool clearEventSystemSelectionOnBlankBattlefieldLeftClick = true;

    private GameObject popupLogPanel;
    private BottomContextType bottomContextType = BottomContextType.Inventory;

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

        if (canPlayerAct)
        {
            lastAutoShownActingAlly = actingAlly;
        }
        else
        {
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
        uiController.RefreshBottomPortraitBars(battleManager);
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

        stageCameraController?.FocusUnitInstant(unit);
        ClearUISelection();
        RefreshAllUI();
    }

    public void ToggleUnitInfoFromBottomPortrait(BattleUnit unit)
    {
        if (battleManager == null || unit == null || !battleManager.IsUnitInBattle(unit))
            return;

        if (unit.Team == TeamType.Ally)
        {
            battleManager.SelectedAllyInfoUnit = battleManager.SelectedAllyInfoUnit == unit ? null : unit;
        }
        else
        {
            if (battleManager.SelectedEnemyInfoUnit == unit)
            {
                battleManager.SelectedEnemyInfoUnit = null;
                if (uiController != null && uiController.IsEnemyDetailPopupOpen())
                    uiController.HideEnemyDetailPopup();
            }
            else
            {
                if (uiController != null && uiController.IsEnemyDetailPopupOpen())
                    uiController.HideEnemyDetailPopup();
                battleManager.SelectedEnemyInfoUnit = unit;
            }
        }

        stageCameraController?.FocusUnitInstant(unit);
        ClearUISelection();
        RefreshAllUI();
    }

    public void ClearInfoSelectionForTeam(TeamType team)
    {
        if (battleManager == null)
            return;

        if (team == TeamType.Ally)
        {
            battleManager.SelectedAllyInfoUnit = null;
        }
        else
        {
            battleManager.SelectedEnemyInfoUnit = null;
            if (uiController != null && uiController.IsEnemyDetailPopupOpen())
                uiController.HideEnemyDetailPopup();
        }

        ClearUISelection();
        RefreshAllUI();
    }

    public void OnBlankBattlefieldLeftClicked()
    {
        if (battleManager == null)
            return;

        bool changed = false;

        if (cancelPendingActionOnBlankBattlefieldLeftClick &&
            battleManager.InputMode != BattleInputMode.WaitingForAction &&
            battleManager.CurrentState == TurnState.PlayerInput &&
            battleManager.InputController != null)
        {
            battleManager.InputController.CancelCurrentInput();
            changed = true;
        }

        if (closeInfoPanelsOnBlankBattlefieldLeftClick)
        {
            battleManager.ClearInfoSelections();

            if (uiController != null)
            {
                uiController.HideEnemyDetailPopup();
                uiController.HandleBlankFieldLeftClick();
            }

            changed = true;
        }

        if (clearEventSystemSelectionOnBlankBattlefieldLeftClick)
            ClearUISelection();

        if (changed)
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
