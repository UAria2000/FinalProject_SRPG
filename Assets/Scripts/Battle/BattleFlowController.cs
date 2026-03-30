using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BattleFlowController : MonoBehaviour
{
    private BattleManager battleManager;
    private BattleViewManager viewManager;
    private BattleUIController uiController;
    private BattleLogController logController;
    private EnemyAIController enemyAIController;
    private BattlePassiveController passiveController;
    private BattleSkillGimmickController skillGimmickController;
    private BattleCaptureController captureController;
    private BattlePersistenceController persistenceController;

    private TurnManager turnManager;

    public void Initialize(
        BattleManager manager,
        BattleViewManager view,
        BattleUIController ui,
        BattleLogController log,
        EnemyAIController enemyAI,
        BattlePassiveController passive,
        BattleSkillGimmickController gimmick,
        BattleCaptureController capture,
        BattlePersistenceController persistence)
    {
        battleManager = manager;
        viewManager = view;
        uiController = ui;
        logController = log;
        enemyAIController = enemyAI;
        passiveController = passive;
        skillGimmickController = gimmick;
        captureController = capture;
        persistenceController = persistence;
    }

    public void StartBattle()
    {
        if (battleManager == null)
            return;

        StopAllCoroutines();
        battleManager.StopManagedCoroutines();

        if (viewManager != null)
            viewManager.ClearAllViews();

        battleManager.ResetRuntimeForBattleStart();
        battleManager.AssignFormations(new BattleFormation(), new BattleFormation());
        turnManager = new TurnManager();

        if (logController != null)
            logController.ClearBattleLog();

        SpawnPartyIntoFormation(battleManager.AllyPartyDefinition, TeamType.Ally, battleManager.AllyFormation);
        SpawnPartyIntoFormation(battleManager.EnemyPartyDefinition, TeamType.Enemy, battleManager.EnemyFormation);

        battleManager.AllyFormation.RemoveDeadAndCompress();
        battleManager.EnemyFormation.RemoveDeadAndCompress();
        RemoveDeadViews();

        if (captureController != null)
            captureController.InitializeCaptureAttempts();

        if (viewManager != null)
            viewManager.RefreshAllPositionsInstant(battleManager.AllyFormation, battleManager.EnemyFormation);

        battleManager.SetLastShownAllyUnit(battleManager.GetDefaultShownAllyUnit());
        battleManager.SelectedEnemyInfoUnit = battleManager.GetDefaultShownEnemyUnit();
        battleManager.SetBattleStarted(true);
        battleManager.SetBattleEndEventSent(false);

        if (skillGimmickController != null)
            skillGimmickController.ResetRuntimeState();

        if (battleManager.PresentationController != null)
            battleManager.PresentationController.ResetForBattleStart();

        battleManager.RefreshAllUI();
        battleManager.ClearUISelection();
        StartCoroutine(BattleLoopRoutine());
    }

    public IEnumerator HandleDeathsAndCompressionRoutine()
    {
        battleManager.MarkDeadUnitPresence(TeamType.Ally, HasDeadUnits(battleManager.AllyFormation));
        battleManager.MarkDeadUnitPresence(TeamType.Enemy, HasDeadUnits(battleManager.EnemyFormation));

        List<BattleUnit> movedAllies = battleManager.AllyFormation.RemoveDeadAndCompress();
        List<BattleUnit> movedEnemies = battleManager.EnemyFormation.RemoveDeadAndCompress();

        for (int i = 0; i < movedAllies.Count; i++)
            logController.AppendBattleLog(logController.BuildAutoMoveLog(movedAllies[i]));

        for (int i = 0; i < movedEnemies.Count; i++)
            logController.AppendBattleLog(logController.BuildAutoMoveLog(movedEnemies[i]));

        if (viewManager != null)
        {
            RemoveDeadViews();
            yield return StartCoroutine(viewManager.AnimateRefreshAllPositions(
                battleManager.AllyFormation,
                battleManager.EnemyFormation,
                battleManager.MoveAnimationDuration));
        }

        if (!IsUnitInBattle(battleManager.SelectedEnemyInfoUnit))
            battleManager.SelectedEnemyInfoUnit = battleManager.GetDefaultShownEnemyUnit();

        if (!IsUnitInBattle(battleManager.LastShownAllyUnit))
            battleManager.SetLastShownAllyUnit(battleManager.GetDefaultShownAllyUnit());
    }

    public void OnActionExecutionFinished(bool consumeTurn)
    {
        battleManager.ResetSelections();
        battleManager.SetInputMode(BattleInputMode.WaitingForAction);
        battleManager.ClearTargetMarkers();

        if (uiController != null)
        {
            uiController.HideTargetPreview();
            uiController.HideSkillTooltip();
            uiController.HideFleeTooltip();
        }

        battleManager.ClearUISelection();
        battleManager.SetTurnState(TurnState.PlayerInput);
        battleManager.RefreshAllUI();

        if (battleManager.CurrentActingUnit == null ||
            battleManager.CurrentActingUnit.Team == TeamType.Enemy ||
            consumeTurn)
        {
            battleManager.SetWaitingForPlayerAction(false);
            battleManager.SetTurnState(TurnState.TurnEnding);
        }
    }

    public void NotifyUnitLeftBattle(BattleUnit unit)
    {
        if (unit == null)
            return;

        if (captureController != null)
            captureController.NotifyUnitLeftBattle(unit);

        if (battleManager.LastShownAllyUnit == unit)
            battleManager.SetLastShownAllyUnit(battleManager.GetDefaultShownAllyUnit());

        if (battleManager.SelectedEnemyInfoUnit == unit)
            battleManager.SelectedEnemyInfoUnit = battleManager.GetDefaultShownEnemyUnit();

        if (battleManager.PresentationController != null)
            battleManager.PresentationController.NotifyUnitLeftBattle(unit);
    }

    public bool IsUnitInBattle(BattleUnit unit)
    {
        if (unit == null)
            return false;

        return (battleManager.AllyFormation != null && battleManager.AllyFormation.Contains(unit)) ||
               (battleManager.EnemyFormation != null && battleManager.EnemyFormation.Contains(unit));
    }

    private void SpawnPartyIntoFormation(PartyDefinition partyDefinition, TeamType team, BattleFormation formation)
    {
        if (partyDefinition == null || formation == null)
            return;

        if (!partyDefinition.IsValidMemberCount())
        {
            Debug.LogWarning("[BattleManager] Party member count must be 1~4.");
            return;
        }

        if (partyDefinition.HasDuplicateSlotIndex())
        {
            Debug.LogWarning("[BattleManager] Duplicate start slot index.");
            return;
        }

        if (partyDefinition.HasNullDefinitions())
        {
            Debug.LogWarning("[BattleManager] Null unit/view definition found.");
            return;
        }

        for (int i = 0; i < partyDefinition.members.Count; i++)
        {
            PartyMemberData data = partyDefinition.members[i];
            if (data == null)
                continue;

            BattleUnit unit = new BattleUnit(data, team);
            formation.SetUnit(data.startSlotIndex, unit);

            if (viewManager != null && battleManager.InputController != null)
                viewManager.CreateView(unit, battleManager.InputController);
        }
    }

    private IEnumerator BattleLoopRoutine()
    {
        while (battleManager.IsBattleInProgress)
        {
            battleManager.IncrementCurrentRound();
            logController.AppendBattleLog(logController.BuildTurnStartLog(battleManager.CurrentRound));

            if (skillGimmickController != null)
            {
                yield return StartCoroutine(skillGimmickController.ResolveRoundStartGimmicks(battleManager.CurrentRound));
                CheckBattleResult();
                battleManager.RefreshAllUI();

                if (battleManager.BattleResult != BattleResultType.None)
                    break;
            }

            if (uiController != null)
                yield return StartCoroutine(uiController.ShowTurnStartTextRoutine(battleManager.CurrentRound));

            List<BattleUnit> alive = new List<BattleUnit>();
            alive.AddRange(battleManager.AllyFormation.GetAliveUnits());
            alive.AddRange(battleManager.EnemyFormation.GetAliveUnits());
            turnManager.BuildTurnQueue(alive);

            while (turnManager.HasNextTurn() && battleManager.BattleResult == BattleResultType.None)
            {
                BattleUnit unit = turnManager.GetNextUnit();
                if (unit == null || unit.IsDead || !IsUnitInBattle(unit))
                    continue;

                battleManager.SetCurrentActingUnit(unit);
                battleManager.CurrentActingUnit.OnOwnTurnStart();
                battleManager.ClearDeadUnitPresenceFlags();

                if (passiveController != null)
                    yield return StartCoroutine(passiveController.ResolveTurnStartPassive(battleManager.CurrentActingUnit));

                if (battleManager.CurrentActingUnit != null && battleManager.CurrentActingUnit.Team == TeamType.Ally)
                    battleManager.SetLastShownAllyUnit(battleManager.CurrentActingUnit);

                if (viewManager != null)
                {
                    viewManager.ClearAllMarkers();
                    viewManager.SetTurnMarker(battleManager.CurrentActingUnit);
                }

                battleManager.SetCurrentTurnSkippedByStatus(false);
                yield return StartCoroutine(ResolveTurnStartStatusesRoutine(battleManager.CurrentActingUnit));
                CheckBattleResult();
                battleManager.RefreshAllUI();

                if (battleManager.BattleResult != BattleResultType.None)
                    break;

                if (battleManager.CurrentActingUnit == null ||
                    battleManager.CurrentActingUnit.IsDead ||
                    !IsUnitInBattle(battleManager.CurrentActingUnit) ||
                    battleManager.CurrentTurnSkippedByStatus)
                {
                    EvaluateEndOfTurnGimmicks(unit);

                    CheckBattleResult();
                    battleManager.RefreshAllUI();

                    if (battleManager.BattleResult != BattleResultType.None)
                        break;

                    yield return new WaitForSeconds(battleManager.TurnDelay);
                    continue;
                }

                battleManager.SetInputMode(BattleInputMode.WaitingForAction);
                battleManager.ResetSelections();

                if (battleManager.CurrentActingUnit.Team == TeamType.Ally)
                {
                    battleManager.SetTurnState(TurnState.PlayerInput);
                    battleManager.SetWaitingForPlayerAction(true);
                }
                else
                {
                    battleManager.SetTurnState(TurnState.EnemyThinking);
                }

                battleManager.RefreshAllUI();

                if (battleManager.CurrentActingUnit.Team == TeamType.Ally)
                {
                    while (battleManager.WaitingForPlayerAction && battleManager.BattleResult == BattleResultType.None)
                        yield return null;
                }
                else if (enemyAIController != null)
                {
                    yield return StartCoroutine(enemyAIController.ExecuteTurn(battleManager.CurrentActingUnit));
                }

                CheckBattleResult();
                battleManager.RefreshAllUI();

                if (battleManager.BattleResult != BattleResultType.None)
                    break;

                EvaluateEndOfTurnGimmicks(unit);

                CheckBattleResult();
                battleManager.RefreshAllUI();

                if (battleManager.BattleResult != BattleResultType.None)
                    break;

                yield return new WaitForSeconds(battleManager.TurnDelay);
            }
        }

        battleManager.SetTurnState(TurnState.BattleEnded);
        if (battleManager.BattleResult == BattleResultType.Victory)
            logController.AppendBattleLog(logController.BuildVictoryLog());
        else if (battleManager.BattleResult == BattleResultType.Defeat)
            logController.AppendBattleLog(logController.BuildDefeatLog());

        battleManager.RefreshAllUI();
        battleManager.ClearUISelection();
        NotifyBattleEndedIfNeeded();
    }

    private IEnumerator ResolveTurnStartStatusesRoutine(BattleUnit unit)
    {
        if (unit == null || unit.IsDead || !IsUnitInBattle(unit))
            yield break;

        BattleTurnStartStatusResult result = unit.ResolveTurnStartStatuses();

        if (result.poisonDamage > 0)
            logController.AppendBattleLog(logController.BuildTurnStartPoisonLog(unit, result.poisonDamage));

        if (result.bleedDamage > 0)
            logController.AppendBattleLog(logController.BuildTurnStartBleedLog(unit, result.bleedDamage));

        if (unit.IsDead)
        {
            logController.AppendBattleLog(logController.BuildDeathLog(unit));
            yield return StartCoroutine(HandleDeathsAndCompressionRoutine());
            yield break;
        }

        if (result.wasStunned)
        {
            logController.AppendBattleLog(logController.BuildTurnStartStunLog(unit));
            battleManager.SetWaitingForPlayerAction(false);
            battleManager.SetTurnState(TurnState.TurnEnding);
            battleManager.SetCurrentTurnSkippedByStatus(true);
        }

        for (int i = 0; i < result.expiredStatuses.Count; i++)
            logController.AppendBattleLog(logController.BuildStatusExpiredLog(unit, result.expiredStatuses[i]));
    }

    private void RemoveDeadViews()
    {
        if (viewManager == null)
            return;

        List<BattleUnit> allyUnits = battleManager.AllyFormation.GetAllUnits();
        List<BattleUnit> enemyUnits = battleManager.EnemyFormation.GetAllUnits();

        HashSet<BattleUnit> aliveSet = new HashSet<BattleUnit>();
        for (int i = 0; i < allyUnits.Count; i++) aliveSet.Add(allyUnits[i]);
        for (int i = 0; i < enemyUnits.Count; i++) aliveSet.Add(enemyUnits[i]);

        List<BattleUnit> removeTargets = new List<BattleUnit>();
        foreach (BattleUnitView view in viewManager.GetAllViews())
        {
            if (view == null || view.Unit == null)
                continue;

            if (!aliveSet.Contains(view.Unit) || view.Unit.IsDead)
                removeTargets.Add(view.Unit);
        }

        for (int i = 0; i < removeTargets.Count; i++)
            viewManager.RemoveView(removeTargets[i]);
    }

    private void EvaluateEndOfTurnGimmicks(BattleUnit endedTurnUnit)
    {
        if (passiveController != null)
            passiveController.EvaluateAfterTurnEnd(endedTurnUnit);

        if (skillGimmickController != null)
        {
            skillGimmickController.EvaluateAfterTurnEnd(
                endedTurnUnit,
                battleManager.AllyDeadUnitPresentThisTurn,
                battleManager.EnemyDeadUnitPresentThisTurn);
        }

        battleManager.ClearDeadUnitPresenceFlags();
    }

    private bool HasDeadUnits(BattleFormation formation)
    {
        if (formation == null)
            return false;

        List<BattleUnit> units = formation.GetAllUnits();
        for (int i = 0; i < units.Count; i++)
        {
            BattleUnit unit = units[i];
            if (unit != null && unit.IsDead)
                return true;
        }

        return false;
    }

    private void CheckBattleResult()
    {
        bool alliesAlive = battleManager.AllyFormation != null && battleManager.AllyFormation.HasLivingUnits();
        bool enemiesAlive = battleManager.EnemyFormation != null && battleManager.EnemyFormation.HasLivingUnits();

        if (captureController != null && !captureController.IsMainPlayerAliveInBattle())
        {
            battleManager.SetBattleResult(BattleResultType.Defeat);
            return;
        }

        if (alliesAlive && enemiesAlive)
            battleManager.SetBattleResult(BattleResultType.None);
        else if (alliesAlive)
            battleManager.SetBattleResult(BattleResultType.Victory);
        else
            battleManager.SetBattleResult(BattleResultType.Defeat);
    }

    private void NotifyBattleEndedIfNeeded()
    {
        if (battleManager.BattleEndEventSent || battleManager.BattleResult == BattleResultType.None)
            return;

        if (persistenceController != null)
            persistenceController.SavePersistentAllyPartyHP();

        battleManager.SetBattleEndEventSent(true);
        battleManager.InvokeBattleEnded();
    }
}
