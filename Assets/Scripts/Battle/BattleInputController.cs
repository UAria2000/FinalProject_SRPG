using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleInputController : MonoBehaviour
{
    private BattleManager battleManager;
    private BattleUIController uiController;
    private BattleActionController actionController;
    private BattleLogController logController;

    public void Initialize(
        BattleManager manager,
        BattleUIController ui,
        BattleActionController action,
        BattleLogController log)
    {
        battleManager = manager;
        uiController = ui;
        actionController = action;
        logController = log;
    }

    public void OnAttackButtonClicked()
    {
        if (!CanAcceptPlayerInput()) return;

        battleManager.SetInputMode(BattleInputMode.WaitingForAttackTarget);
        battleManager.ClearSelectedSkill();

        HighlightAttackableTargets();
        RefreshCancelUI();
        uiController?.HideSkillTooltip();
    }

    public void OnMoveButtonClicked()
    {
        if (!CanAcceptPlayerInput()) return;

        battleManager.SetInputMode(BattleInputMode.WaitingForMoveTarget);
        battleManager.ClearSelectedSkill();

        HighlightMoveableTargets();
        RefreshCancelUI();
        uiController?.HideSkillTooltip();
    }

    public void OnSkillButtonClicked(int skillIndex)
    {
        if (!CanAcceptPlayerInput()) return;

        BattleUnit actor = battleManager.CurrentActingUnit;
        SkillDefinition skill = actor.GetSkillAt(skillIndex);
        if (skill == null) return;
        if (!actor.CanUseSkill(skill)) return;

        List<BattleUnit> validTargets = battleManager.GetPrimarySkillTargets(actor, skill);
        if (validTargets.Count <= 0) return;

        battleManager.SetSelectedSkill(skill);
        battleManager.SetInputMode(BattleInputMode.WaitingForSkillTarget);

        HighlightSkillTargets(actor, skill);
        RefreshCancelUI();
        uiController?.HideSkillTooltip();
    }

    public void OnItemButtonClicked()
    {
        if (!CanAcceptPlayerInput()) return;

        battleManager.SetInputMode(BattleInputMode.WaitingForItemTarget);
        battleManager.ClearSelectedSkill();

        HighlightItemTargets();
        RefreshCancelUI();
        uiController?.HideSkillTooltip();
    }

    public void OnCancelButtonClicked()
    {
        if (!battleManager.CanCancelCurrentSelection())
            return;

        CancelCurrentSelection();
    }

    public void OnUnitViewClicked(BattleUnitView clickedView)
    {
        if (clickedView == null || clickedView.Unit == null)
            return;

        BattleUnit clickedUnit = clickedView.Unit;

        if (clickedUnit.Team == TeamType.Enemy)
            battleManager.SetSelectedEnemyInfoUnit(clickedUnit);

        if (battleManager.CurrentState != TurnState.PlayerInput)
            return;

        switch (battleManager.InputMode)
        {
            case BattleInputMode.WaitingForAttackTarget:
                HandleAttackTargetClick(clickedUnit);
                break;

            case BattleInputMode.WaitingForMoveTarget:
                HandleMoveTargetClick(clickedUnit);
                break;

            case BattleInputMode.WaitingForItemTarget:
                HandleItemTargetClick(clickedUnit);
                break;

            case BattleInputMode.WaitingForSkillTarget:
                HandleSkillTargetClick(clickedUnit);
                break;
        }
    }

    private void HandleAttackTargetClick(BattleUnit clickedUnit)
    {
        if (clickedUnit.Team != TeamType.Enemy)
            return;

        List<BattleUnit> validTargets = BattleTargeting.GetBasicAttackTargets(
            battleManager.CurrentActingUnit,
            battleManager.EnemyFormation
        );

        if (!validTargets.Contains(clickedUnit))
            return;

        battleManager.SetSelectedAttackTarget(clickedUnit);
        battleManager.StartManagedCoroutine(ResolvePlayerAttack());
    }

    private void HandleMoveTargetClick(BattleUnit clickedUnit)
    {
        if (clickedUnit.Team != TeamType.Ally)
            return;

        List<BattleUnit> validTargets = battleManager.GetMoveableTargets(
            battleManager.CurrentActingUnit,
            battleManager.AllyFormation
        );

        if (!validTargets.Contains(clickedUnit))
            return;

        battleManager.SetSelectedMoveTarget(clickedUnit);
        battleManager.StartManagedCoroutine(ResolvePlayerMove());
    }

    private void HandleItemTargetClick(BattleUnit clickedUnit)
    {
        if (clickedUnit.Team != TeamType.Ally)
            return;

        List<BattleUnit> validTargets = battleManager.GetItemTargets(
            battleManager.CurrentActingUnit,
            battleManager.AllyFormation
        );

        if (!validTargets.Contains(clickedUnit))
            return;

        battleManager.SetSelectedItemTarget(clickedUnit);
        battleManager.StartManagedCoroutine(ResolvePlayerItemUse());
    }

    private void HandleSkillTargetClick(BattleUnit clickedUnit)
    {
        SkillDefinition selectedSkill = battleManager.SelectedSkill;
        if (selectedSkill == null)
            return;

        List<BattleUnit> validTargets = battleManager.GetPrimarySkillTargets(
            battleManager.CurrentActingUnit,
            selectedSkill
        );

        if (!validTargets.Contains(clickedUnit))
            return;

        battleManager.SetSelectedSkillTarget(clickedUnit);
        battleManager.StartManagedCoroutine(ResolvePlayerSkillUse());
    }

    private IEnumerator ResolvePlayerAttack()
    {
        battleManager.SetInputMode(BattleInputMode.None);
        battleManager.ClearAllTargetMarkersAndHighlights();

        uiController?.SetActionButtonsInteractable(false);
        uiController?.RefreshCancelButtonState(false);

        yield return actionController.ExecuteBasicAttack(
            battleManager.CurrentActingUnit,
            battleManager.SelectedAttackTarget
        );

        battleManager.MarkPlayerActionSubmitted();
    }

    private IEnumerator ResolvePlayerMove()
    {
        battleManager.SetInputMode(BattleInputMode.None);
        battleManager.ClearAllTargetMarkersAndHighlights();

        uiController?.SetActionButtonsInteractable(false);
        uiController?.RefreshCancelButtonState(false);

        bool moved = actionController.TrySwapUnits(
            battleManager.CurrentActingUnit,
            battleManager.SelectedMoveTarget,
            battleManager.AllyFormation
        );

        if (moved)
        {
            logController?.AppendBattleLog(
                logController.BuildMoveLog(battleManager.CurrentActingUnit, battleManager.SelectedMoveTarget)
            );

            if (battleManager.ViewManager != null)
            {
                yield return battleManager.ViewManager.AnimateRefreshAllPositions(
                    battleManager.AllyFormation,
                    battleManager.EnemyFormation,
                    battleManager.MoveAnimationDuration
                );
            }
        }

        battleManager.MarkPlayerActionSubmitted();
    }

    private IEnumerator ResolvePlayerItemUse()
    {
        battleManager.SetInputMode(BattleInputMode.None);
        battleManager.ClearAllTargetMarkersAndHighlights();

        uiController?.SetActionButtonsInteractable(false);
        uiController?.RefreshCancelButtonState(false);

        yield return actionController.ExecutePotionUse(
            battleManager.CurrentActingUnit,
            battleManager.SelectedItemTarget,
            battleManager.PotionHealAmount
        );

        battleManager.MarkPlayerActionSubmitted();
    }

    private IEnumerator ResolvePlayerSkillUse()
    {
        battleManager.SetInputMode(BattleInputMode.None);
        battleManager.ClearAllTargetMarkersAndHighlights();

        uiController?.SetActionButtonsInteractable(false);
        uiController?.RefreshCancelButtonState(false);

        yield return actionController.ExecuteSkill(
            battleManager.CurrentActingUnit,
            battleManager.SelectedSkillTarget,
            battleManager.SelectedSkill
        );

        battleManager.MarkPlayerActionSubmitted();
    }

    private void CancelCurrentSelection()
    {
        battleManager.SetInputMode(BattleInputMode.WaitingForAction);
        battleManager.ClearSelectedTargetsAndSkill();
        battleManager.ClearAllTargetMarkersAndHighlights();

        if (battleManager.CurrentActingUnit != null)
            battleManager.ShowCurrentTurnMarker(battleManager.CurrentActingUnit, true);

        battleManager.RefreshPlayerActionUI();
        uiController?.HideSkillTooltip();
    }

    private bool CanAcceptPlayerInput()
    {
        return battleManager != null &&
               battleManager.CurrentState == TurnState.PlayerInput &&
               battleManager.CurrentActingUnit != null &&
               !battleManager.CurrentActingUnit.IsDead;
    }

    private void RefreshCancelUI()
    {
        uiController?.RefreshCancelButtonState(battleManager.CanCancelCurrentSelection());
    }

    private void HighlightAttackableTargets()
    {
        BattleUnit actor = battleManager.CurrentActingUnit;
        battleManager.ClearAllTargetMarkersAndHighlights();

        List<BattleUnit> validTargets = BattleTargeting.GetBasicAttackTargets(actor, battleManager.EnemyFormation);
        foreach (BattleUnit target in validTargets)
        {
            BattleUnitView view = battleManager.ViewManager.GetView(target);
            if (view != null)
            {
                view.SetHighlighted(true);
                view.SetTargetMarker(true);
            }
        }

        battleManager.ShowCurrentTurnMarker(actor, true);
    }

    private void HighlightMoveableTargets()
    {
        BattleUnit actor = battleManager.CurrentActingUnit;
        battleManager.ClearAllTargetMarkersAndHighlights();

        List<BattleUnit> validTargets = battleManager.GetMoveableTargets(actor, battleManager.AllyFormation);
        foreach (BattleUnit target in validTargets)
        {
            BattleUnitView view = battleManager.ViewManager.GetView(target);
            if (view != null)
            {
                view.SetHighlighted(true);
                view.SetTargetMarker(true);
            }
        }

        battleManager.ShowCurrentTurnMarker(actor, true);
    }

    private void HighlightItemTargets()
    {
        BattleUnit actor = battleManager.CurrentActingUnit;
        battleManager.ClearAllTargetMarkersAndHighlights();

        List<BattleUnit> validTargets = battleManager.GetItemTargets(actor, battleManager.AllyFormation);
        foreach (BattleUnit target in validTargets)
        {
            BattleUnitView view = battleManager.ViewManager.GetView(target);
            if (view != null)
            {
                view.SetHighlighted(true);
                view.SetTargetMarker(true);
            }
        }

        battleManager.ShowCurrentTurnMarker(actor, true);
    }

    private void HighlightSkillTargets(BattleUnit actor, SkillDefinition skill)
    {
        battleManager.ClearAllTargetMarkersAndHighlights();

        List<BattleUnit> validTargets = battleManager.GetPrimarySkillTargets(actor, skill);
        foreach (BattleUnit target in validTargets)
        {
            BattleUnitView view = battleManager.ViewManager.GetView(target);
            if (view != null)
            {
                view.SetHighlighted(true);
                view.SetTargetMarker(true);
            }
        }

        battleManager.ShowCurrentTurnMarker(actor, true);
    }
}