using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BattleInputController : MonoBehaviour
{
    private BattleManager battleManager;
    private BattleUIController uiController;
    private BattleActionController actionController;
    private BattleLogController logController;

    public void Initialize(BattleManager manager, BattleUIController ui, BattleActionController action, BattleLogController log)
    {
        battleManager = manager;
        uiController = ui;
        actionController = action;
        logController = log;
    }

    public void HandleActionSlotPressed(int slotIndex)
    {
        if (!CanAcceptPlayerInput())
            return;

        BattleUnit actor = battleManager.CurrentActingUnit;
        SkillDefinition skill = actor != null ? actor.GetActionSkillAt(slotIndex) : null;
        if (actor == null || skill == null || !actor.CanUseSkill(skill))
            return;

        List<BattleUnit> validTargets = BattleTargeting.GetValidSkillTargets(
            actor,
            skill,
            battleManager.AllyFormation,
            battleManager.EnemyFormation);

        if (validTargets.Count <= 0)
            return;

        battleManager.SelectedSkillSlotIndex = slotIndex;
        battleManager.SelectedSkill = skill;
        battleManager.SelectedInventoryIndex = -1;
        battleManager.SetInputMode(BattleInputMode.WaitingForSkillTarget);

        battleManager.ShowTargetMarkers(validTargets);
        uiController.HideSkillTooltip();
        uiController.HideTargetPreview();

        ClearUISelection();
        battleManager.RefreshAllUI();
    }

    public void HandleMovePressed()
    {
        if (!CanAcceptPlayerInput())
            return;

        List<BattleUnit> validTargets = BattleTargeting.GetMovableTargets(
            battleManager.CurrentActingUnit,
            battleManager.AllyFormation);

        if (validTargets.Count <= 0)
            return;

        battleManager.SelectedSkill = null;
        battleManager.SelectedInventoryIndex = -1;
        battleManager.SelectedSkillSlotIndex = -1;
        battleManager.SetInputMode(BattleInputMode.WaitingForMoveTarget);
        battleManager.ShowTargetMarkers(validTargets);
        uiController.HideTargetPreview();
        uiController.HideSkillTooltip();

        ClearUISelection();
        battleManager.RefreshAllUI();
    }

    public void HandleInventorySlotPressed(int inventoryIndex)
    {
        if (!CanAcceptPlayerInput())
            return;

        PartyDefinition allyParty = battleManager.AllyPartyDefinition;
        if (allyParty == null || inventoryIndex < 0 || inventoryIndex >= allyParty.inventory.Count)
            return;

        InventoryStackData stack = allyParty.inventory[inventoryIndex];
        if (stack == null || stack.item == null || stack.amount <= 0)
            return;

        List<BattleUnit> validTargets = BattleTargeting.GetValidItemTargets(
            battleManager.CurrentActingUnit,
            stack.item,
            battleManager.AllyFormation,
            battleManager.EnemyFormation);

        if (validTargets.Count <= 0)
            return;

        battleManager.SelectedInventoryIndex = inventoryIndex;
        battleManager.SelectedSkill = null;
        battleManager.SelectedSkillSlotIndex = -1;
        battleManager.SetInputMode(BattleInputMode.WaitingForItemTarget);
        battleManager.ShowTargetMarkers(validTargets);
        uiController.HideTargetPreview();
        uiController.HideSkillTooltip();

        ClearUISelection();
        battleManager.RefreshAllUI();
    }

    public void CancelCurrentInput()
    {
        if (battleManager.CurrentState != TurnState.PlayerInput)
            return;

        battleManager.SelectedSkill = null;
        battleManager.SelectedInventoryIndex = -1;
        battleManager.SelectedSkillSlotIndex = -1;
        battleManager.SetInputMode(BattleInputMode.WaitingForAction);
        battleManager.ClearTargetMarkers();
        uiController.HideTargetPreview();
        uiController.HideSkillTooltip();

        ClearUISelection();
        battleManager.RefreshAllUI();
    }

    public void OnUnitViewClicked(BattleUnitView clickedView)
    {
        if (clickedView == null || clickedView.Unit == null)
            return;

        BattleUnit clickedUnit = clickedView.Unit;

        if (clickedUnit.Team == TeamType.Enemy)
            battleManager.SelectedEnemyInfoUnit = clickedUnit;

        switch (battleManager.InputMode)
        {
            case BattleInputMode.WaitingForSkillTarget:
                HandleSkillTargetClick(clickedUnit);
                break;
            case BattleInputMode.WaitingForMoveTarget:
                HandleMoveTargetClick(clickedUnit);
                break;
            case BattleInputMode.WaitingForItemTarget:
                HandleItemTargetClick(clickedUnit);
                break;
        }

        battleManager.RefreshAllUI();
    }

    // 기존 BattleClickable과 호환
    public void OnUnitViewHoverEntered(BattleUnitView hoveredView)
    {
        if (hoveredView == null)
            return;

        UpdateTargetPreviewHover(hoveredView, hoveredView.HoverAnchor != null ? hoveredView.HoverAnchor.position : Vector3.zero);
    }

    // 새 PointerEvent 기반과 호환
    public void OnUnitViewHoverEntered(BattleUnitView hoveredView, Vector2 pointerScreenPosition)
    {
        UpdateTargetPreviewHover(hoveredView, pointerScreenPosition);
    }

    public void OnUnitViewHoverMoved(BattleUnitView hoveredView, Vector2 pointerScreenPosition)
    {
        UpdateTargetPreviewHover(hoveredView, pointerScreenPosition);
    }

    public void OnUnitViewHoverExited(BattleUnitView hoveredView)
    {
        uiController.HideTargetPreview();
    }

    private void UpdateTargetPreviewHover(BattleUnitView hoveredView, Vector3 screenPosition)
    {
        if (hoveredView == null || hoveredView.Unit == null)
            return;

        if (battleManager.InputMode != BattleInputMode.WaitingForSkillTarget)
            return;

        SkillDefinition skill = battleManager.SelectedSkill;
        if (skill == null)
            return;

        BattleUnit hoveredUnit = hoveredView.Unit;
        List<BattleUnit> validTargets = BattleTargeting.GetValidSkillTargets(
            battleManager.CurrentActingUnit,
            skill,
            battleManager.AllyFormation,
            battleManager.EnemyFormation);

        if (!validTargets.Contains(hoveredUnit))
            return;

        if (!skill.ShouldShowTargetPreview())
            return;

        if (skill.targetTeam != SkillTargetTeam.Enemy)
            return;

        TargetPreviewData data = BattleCalculator.BuildSkillPreview(battleManager.CurrentActingUnit, hoveredUnit, skill);
        uiController.ShowTargetPreview(data, screenPosition);
    }

    private void HandleSkillTargetClick(BattleUnit clickedUnit)
    {
        SkillDefinition skill = battleManager.SelectedSkill;
        if (skill == null) return;

        List<BattleUnit> validTargets = BattleTargeting.GetValidSkillTargets(
            battleManager.CurrentActingUnit,
            skill,
            battleManager.AllyFormation,
            battleManager.EnemyFormation);

        if (!validTargets.Contains(clickedUnit))
            return;

        battleManager.StartManagedCoroutine(actionController.ExecuteSkill(battleManager.CurrentActingUnit, skill, clickedUnit));
    }

    private void HandleMoveTargetClick(BattleUnit clickedUnit)
    {
        List<BattleUnit> validTargets = BattleTargeting.GetMovableTargets(
            battleManager.CurrentActingUnit,
            battleManager.AllyFormation);

        if (!validTargets.Contains(clickedUnit))
            return;

        battleManager.StartManagedCoroutine(actionController.ExecuteMove(battleManager.CurrentActingUnit, clickedUnit));
    }

    private void HandleItemTargetClick(BattleUnit clickedUnit)
    {
        int index = battleManager.SelectedInventoryIndex;
        PartyDefinition allyParty = battleManager.AllyPartyDefinition;
        if (allyParty == null || index < 0 || index >= allyParty.inventory.Count)
            return;

        ItemDefinition item = allyParty.inventory[index].item;
        List<BattleUnit> validTargets = BattleTargeting.GetValidItemTargets(
            battleManager.CurrentActingUnit,
            item,
            battleManager.AllyFormation,
            battleManager.EnemyFormation);

        if (!validTargets.Contains(clickedUnit))
            return;

        battleManager.StartManagedCoroutine(actionController.ExecuteItem(battleManager.CurrentActingUnit, index, clickedUnit));
    }

    private bool CanAcceptPlayerInput()
    {
        return battleManager != null &&
               battleManager.CurrentState == TurnState.PlayerInput &&
               battleManager.CurrentActingUnit != null &&
               battleManager.CurrentActingUnit.Team == TeamType.Ally;
    }

    private void ClearUISelection()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }
}