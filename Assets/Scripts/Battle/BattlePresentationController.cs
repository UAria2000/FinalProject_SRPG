using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class BattlePresentationController : MonoBehaviour
{
    private BattleManager battleManager;
    private BattleUIController uiController;
    private GameObject popupLogPanel;
    private BottomContextType bottomContextType = BottomContextType.Inventory;

    public BottomContextType BottomContextType
    {
        get { return bottomContextType; }
    }

    public void Initialize(BattleManager manager, BattleUIController ui, GameObject popupPanel)
    {
        battleManager = manager;
        uiController = ui;
        popupLogPanel = popupPanel;
        ResetForBattleStart();
    }

    public void ResetForBattleStart()
    {
        bottomContextType = BottomContextType.Inventory;

        if (popupLogPanel != null)
            popupLogPanel.SetActive(false);

        if (uiController != null)
        {
            uiController.HideEnemyDetailPopup();
            uiController.HideTargetPreview();
            uiController.HideSkillTooltip();
            uiController.HideEnemySkillTooltip();
            uiController.HideFleeTooltip();
            uiController.SetBottomContext(bottomContextType);
        }
    }

    public void RefreshAllUI()
    {
        if (battleManager == null)
            return;

        BattleUnit shownAlly = battleManager.IsUnitInBattle(battleManager.LastShownAllyUnit)
            ? battleManager.LastShownAllyUnit
            : battleManager.GetDefaultShownAllyUnit();

        BattleUnit shownEnemy = battleManager.IsUnitInBattle(battleManager.SelectedEnemyInfoUnit)
            ? battleManager.SelectedEnemyInfoUnit
            : battleManager.GetDefaultShownEnemyUnit();

        BattleUnit actionOwner = battleManager.CurrentActingUnit != null &&
                                 battleManager.CurrentActingUnit.Team == TeamType.Ally &&
                                 battleManager.IsUnitInBattle(battleManager.CurrentActingUnit)
            ? battleManager.CurrentActingUnit
            : shownAlly;

        bool canPlayerAct = battleManager.CurrentState == TurnState.PlayerInput &&
                            battleManager.CurrentActingUnit != null &&
                            battleManager.CurrentActingUnit.Team == TeamType.Ally &&
                            battleManager.IsUnitInBattle(battleManager.CurrentActingUnit);

        battleManager.SetLastShownAllyUnit(shownAlly);
        battleManager.SelectedEnemyInfoUnit = shownEnemy;

        if (uiController == null)
            return;

        uiController.RefreshCurrentUnitPanel(shownAlly);
        uiController.RefreshEnemyPanels(shownEnemy);
        uiController.RefreshActionButtons(actionOwner, canPlayerAct);
        uiController.RefreshInventory(battleManager, battleManager.AllyPartyDefinition, battleManager.SelectedInventoryIndex);
        uiController.SetBottomContext(bottomContextType);
    }

    public void NotifyUnitLeftBattle(BattleUnit unit)
    {
        if (unit == null)
            return;

        battleManager.ClearTargetMarkers();

        if (uiController != null)
        {
            uiController.HideTargetPreview();
            uiController.HideSkillTooltip();
            uiController.HideEnemySkillTooltip();
            uiController.HideFleeTooltip();
        }
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
        if (uiController == null)
            return;

        bool isClosingCurrentPopup = bottomContextType == BottomContextType.EnemyInfo && uiController.IsEnemyDetailPopupOpen();
        if (isClosingCurrentPopup)
        {
            uiController.HideEnemyDetailPopup();
            bottomContextType = BottomContextType.Inventory;
        }
        else
        {
            bottomContextType = BottomContextType.EnemyInfo;
            uiController.ShowEnemyDetailPopup(battleManager.SelectedEnemyInfoUnit);
        }

        ClearUISelection();
        RefreshAllUI();
    }

    public void OnPlayerSkillButtonHoverEnter(int slotIndex, Vector3 screenPosition)
    {
        BattleUnit unit = battleManager.CurrentActingUnit != null && battleManager.CurrentActingUnit.Team == TeamType.Ally
            ? battleManager.CurrentActingUnit
            : battleManager.LastShownAllyUnit;

        SkillDefinition skill = unit != null ? unit.GetActionSkillAt(slotIndex) : null;
        if (skill != null && uiController != null)
            uiController.ShowPlayerSkillTooltip(skill, screenPosition);
    }

    public void OnPlayerSkillButtonHoverExit()
    {
        if (uiController != null)
            uiController.HideSkillTooltip();
    }

    public void OnFleeButtonHoverEnter(Vector3 screenPosition)
    {
        if (uiController == null ||
            battleManager.CurrentState != TurnState.PlayerInput ||
            battleManager.CurrentActingUnit == null ||
            battleManager.CurrentActingUnit.Team != TeamType.Ally)
            return;

        int fleeChancePercent = BattleCalculator.CalculateFleeChancePercent(battleManager.CurrentActingUnit, battleManager.EnemyFormation);
        uiController.ShowFleeTooltip(fleeChancePercent, screenPosition);
    }

    public void OnFleeButtonHoverExit()
    {
        if (uiController != null)
            uiController.HideFleeTooltip();
    }

    public void OnEnemySkillHoverEnter(int slotIndex, Vector3 screenPosition)
    {
        if (battleManager.SelectedEnemyInfoUnit == null || uiController == null)
            return;

        SkillDefinition skill = battleManager.SelectedEnemyInfoUnit.GetActionSkillAt(slotIndex);
        if (skill != null)
            uiController.ShowEnemySkillTooltip(skill, screenPosition);
    }

    public void OnEnemySkillHoverExit()
    {
        if (uiController != null)
            uiController.HideEnemySkillTooltip();
    }

    public void ClearUISelection()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }
}
