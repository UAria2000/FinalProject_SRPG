using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleManager : MonoBehaviour
{
    [Header("Ally Team")]
    public UnitDefinition[] allyDefinitions = new UnitDefinition[4];

    [Header("Enemy Team")]
    public UnitDefinition[] enemyDefinitions = new UnitDefinition[4];

    [Header("View")]
    [SerializeField] private BattleViewManager viewManager;

    [Header("Buttons")]
    [SerializeField] private Button attackButton;
    [SerializeField] private Button moveButton;

    [Header("Round UI")]
    [SerializeField] private TMP_Text turnStartText;
    [SerializeField] private float turnStartTextShowTime = 1.0f;

    [Header("Timings")]
    [SerializeField] private float turnDelay = 0.4f;
    [SerializeField] private float moveAnimationDuration = 0.4f;

    [Header("Attack Move")]
    [SerializeField] private float attackMoveRatio = 0.45f;
    [SerializeField] private float attackMoveMaxDistance = 260f;
    [SerializeField] private float attackMoveDuration = 0.6f;

    private BattleFormation allyFormation;
    private BattleFormation enemyFormation;
    private TurnManager turnManager;

    private TurnState currentState = TurnState.Waiting;
    private BattleResultType battleResult = BattleResultType.None;
    private BattleInputMode inputMode = BattleInputMode.None;

    private int currentRound = 0;

    private BattleUnit currentActingUnit;
    private bool playerActionSubmitted = false;
    private BattleUnit selectedAttackTarget;
    private BattleUnit selectedMoveTarget;

    private void Start()
    {
        if (attackButton != null)
            attackButton.onClick.AddListener(OnAttackButtonClicked);

        if (moveButton != null)
            moveButton.onClick.AddListener(OnMoveButtonClicked);

        StartBattle();
    }

    public void StartBattle()
    {
        allyFormation = new BattleFormation();
        enemyFormation = new BattleFormation();
        turnManager = new TurnManager();

        currentRound = 0;
        inputMode = BattleInputMode.None;
        currentActingUnit = null;
        playerActionSubmitted = false;
        selectedAttackTarget = null;
        selectedMoveTarget = null;
        battleResult = BattleResultType.None;
        currentState = TurnState.Waiting;

        if (turnStartText != null)
            turnStartText.gameObject.SetActive(false);

        SetActionButtonsInteractable(false);

        for (int i = 0; i < 4; i++)
        {
            if (allyDefinitions[i] != null)
            {
                BattleUnit ally = new BattleUnit(allyDefinitions[i], TeamType.Ally, i);
                allyFormation.SetUnit(i, ally);

                if (viewManager != null)
                    viewManager.CreateView(ally, this);
            }

            if (enemyDefinitions[i] != null)
            {
                BattleUnit enemy = new BattleUnit(enemyDefinitions[i], TeamType.Enemy, i);
                enemyFormation.SetUnit(i, enemy);

                if (viewManager != null)
                    viewManager.CreateView(enemy, this);
            }
        }

        if (viewManager != null)
            viewManager.RefreshAllPositionsInstant(allyFormation, enemyFormation);

        ClearAllMarkersAndHighlights();

        StartCoroutine(BattleLoop());
    }

    private IEnumerator BattleLoop()
    {
        while (battleResult == BattleResultType.None)
        {
            currentRound++;
            yield return StartCoroutine(ShowTurnStartText(currentRound));

            List<BattleUnit> allAlive = new List<BattleUnit>();
            allAlive.AddRange(allyFormation.GetAliveUnits());
            allAlive.AddRange(enemyFormation.GetAliveUnits());

            turnManager.BuildTurnQueue(allAlive);

            while (turnManager.HasNextTurn() && battleResult == BattleResultType.None)
            {
                BattleUnit currentUnit = turnManager.GetNextUnit();

                if (currentUnit == null || currentUnit.IsDead)
                    continue;

                yield return StartCoroutine(ExecuteTurn(currentUnit));

                CheckBattleResult();
                if (battleResult != BattleResultType.None)
                    break;

                yield return new WaitForSeconds(turnDelay);
            }
        }

        currentState = TurnState.BattleEnded;
        SetActionButtonsInteractable(false);
        ClearAllMarkersAndHighlights();
    }

    private IEnumerator ExecuteTurn(BattleUnit unit)
    {
        if (unit == null || unit.IsDead)
            yield break;

        currentActingUnit = unit;

        ClearAllMarkersAndHighlights();

        if (unit.Team == TeamType.Ally)
            ShowCurrentTurnMarker(unit, true);

        if (unit.Team == TeamType.Ally)
            yield return StartCoroutine(ExecutePlayerTurn(unit));
        else
            yield return StartCoroutine(ExecuteEnemyTurn(unit));

        if (unit.Team == TeamType.Ally)
            ShowCurrentTurnMarker(unit, false);

        allyFormation.RemoveDeadAndCompress();
        enemyFormation.RemoveDeadAndCompress();

        if (viewManager != null)
        {
            yield return StartCoroutine(
                viewManager.AnimateRefreshAllPositions(
                    allyFormation,
                    enemyFormation,
                    moveAnimationDuration
                )
            );
        }

        currentActingUnit = null;
        currentState = TurnState.TurnEnding;
    }

    private IEnumerator ExecutePlayerTurn(BattleUnit unit)
    {
        currentState = TurnState.PlayerInput;
        inputMode = BattleInputMode.WaitingForAction;
        playerActionSubmitted = false;
        selectedAttackTarget = null;
        selectedMoveTarget = null;

        SetActionButtonsInteractable(true);

        while (!playerActionSubmitted)
            yield return null;

        SetActionButtonsInteractable(false);
        ClearAllTargetMarkersAndHighlights();
        inputMode = BattleInputMode.None;
    }

    private IEnumerator ExecuteEnemyTurn(BattleUnit unit)
    {
        currentState = TurnState.EnemyThinking;

        BattleFormation myFormation = enemyFormation;
        BattleFormation enemyFormationRef = allyFormation;

        List<BattleUnit> targets = BattleTargeting.GetBasicAttackTargets(unit, enemyFormationRef);

        if (targets.Count > 0)
        {
            BattleUnit target = ChooseBestTarget(unit, targets);
            yield return StartCoroutine(ExecuteBasicAttack(unit, target));
        }
        else
        {
            bool moved = TryAutoMove(unit, myFormation);

            if (moved && viewManager != null)
            {
                yield return StartCoroutine(
                    viewManager.AnimateRefreshAllPositions(
                        allyFormation,
                        enemyFormation,
                        moveAnimationDuration
                    )
                );
            }
        }
    }

    public void OnAttackButtonClicked()
    {
        if (currentState != TurnState.PlayerInput)
            return;

        if (currentActingUnit == null || currentActingUnit.IsDead)
            return;

        inputMode = BattleInputMode.WaitingForAttackTarget;
        HighlightAttackableTargets(currentActingUnit);
    }

    public void OnMoveButtonClicked()
    {
        if (currentState != TurnState.PlayerInput)
            return;

        if (currentActingUnit == null || currentActingUnit.IsDead)
            return;

        inputMode = BattleInputMode.WaitingForMoveTarget;
        HighlightMoveableTargets(currentActingUnit);
    }

    public void OnUnitViewClicked(BattleUnitView clickedView)
    {
        if (clickedView == null || clickedView.Unit == null)
            return;

        if (currentState != TurnState.PlayerInput)
            return;

        BattleUnit clickedUnit = clickedView.Unit;

        if (inputMode == BattleInputMode.WaitingForAttackTarget)
        {
            if (clickedUnit.Team != TeamType.Enemy)
                return;

            List<BattleUnit> validTargets = BattleTargeting.GetBasicAttackTargets(currentActingUnit, enemyFormation);
            if (!validTargets.Contains(clickedUnit))
                return;

            selectedAttackTarget = clickedUnit;
            StartCoroutine(ResolvePlayerAttack());
        }
        else if (inputMode == BattleInputMode.WaitingForMoveTarget)
        {
            if (clickedUnit.Team != TeamType.Ally)
                return;

            List<BattleUnit> validTargets = GetMoveableTargets(currentActingUnit, allyFormation);
            if (!validTargets.Contains(clickedUnit))
                return;

            selectedMoveTarget = clickedUnit;
            StartCoroutine(ResolvePlayerMove());
        }
    }

    private IEnumerator ResolvePlayerAttack()
    {
        if (currentActingUnit == null || selectedAttackTarget == null)
            yield break;

        inputMode = BattleInputMode.None;
        ClearAllTargetMarkersAndHighlights();
        SetActionButtonsInteractable(false);

        yield return StartCoroutine(ExecuteBasicAttack(currentActingUnit, selectedAttackTarget));

        playerActionSubmitted = true;
    }

    private IEnumerator ResolvePlayerMove()
    {
        if (currentActingUnit == null || selectedMoveTarget == null)
            yield break;

        inputMode = BattleInputMode.None;
        ClearAllTargetMarkersAndHighlights();
        SetActionButtonsInteractable(false);

        bool moved = TrySwapUnits(currentActingUnit, selectedMoveTarget, allyFormation);

        if (moved && viewManager != null)
        {
            yield return StartCoroutine(
                viewManager.AnimateRefreshAllPositions(
                    allyFormation,
                    enemyFormation,
                    moveAnimationDuration
                )
            );
        }

        playerActionSubmitted = true;
    }

    private void HighlightAttackableTargets(BattleUnit attacker)
    {
        ClearAllTargetMarkersAndHighlights();

        List<BattleUnit> validTargets = BattleTargeting.GetBasicAttackTargets(attacker, enemyFormation);

        foreach (BattleUnit target in validTargets)
        {
            BattleUnitView view = viewManager != null ? viewManager.GetView(target) : null;
            if (view != null)
            {
                view.SetHighlighted(true);
                view.SetTargetMarker(true);
            }
        }

        if (attacker != null)
            ShowCurrentTurnMarker(attacker, true);
    }

    private void HighlightMoveableTargets(BattleUnit mover)
    {
        ClearAllTargetMarkersAndHighlights();

        List<BattleUnit> validTargets = GetMoveableTargets(mover, allyFormation);

        foreach (BattleUnit target in validTargets)
        {
            BattleUnitView view = viewManager != null ? viewManager.GetView(target) : null;
            if (view != null)
            {
                view.SetHighlighted(true);
                view.SetTargetMarker(true);
            }
        }

        if (mover != null)
            ShowCurrentTurnMarker(mover, true);
    }

    private List<BattleUnit> GetMoveableTargets(BattleUnit mover, BattleFormation formation)
    {
        List<BattleUnit> result = new List<BattleUnit>();

        if (mover == null || mover.IsDead || formation == null)
            return result;

        int slot = mover.SlotIndex;

        int forwardIndex = slot - 1;
        int backwardIndex = slot + 1;

        if (forwardIndex >= 0)
        {
            BattleUnit forwardUnit = formation.GetUnit(forwardIndex);
            if (forwardUnit != null && !forwardUnit.IsDead && forwardUnit.Team == mover.Team)
                result.Add(forwardUnit);
        }

        if (backwardIndex < 4)
        {
            BattleUnit backwardUnit = formation.GetUnit(backwardIndex);
            if (backwardUnit != null && !backwardUnit.IsDead && backwardUnit.Team == mover.Team)
                result.Add(backwardUnit);
        }

        return result;
    }

    private bool TrySwapUnits(BattleUnit a, BattleUnit b, BattleFormation formation)
    {
        if (a == null || b == null || formation == null)
            return false;

        if (a.IsDead || b.IsDead)
            return false;

        if (a.Team != b.Team)
            return false;

        int diff = Mathf.Abs(a.SlotIndex - b.SlotIndex);
        if (diff != 1)
            return false;

        int direction = b.SlotIndex > a.SlotIndex ? 1 : -1;
        return formation.TrySwapAdjacent(a.SlotIndex, direction);
    }

    private void ShowCurrentTurnMarker(BattleUnit unit, bool visible)
    {
        if (viewManager == null || unit == null)
            return;

        BattleUnitView view = viewManager.GetView(unit);
        if (view != null)
            view.SetCurrentTurnMarker(visible);
    }

    private void ClearAllTargetMarkersAndHighlights()
    {
        if (viewManager == null)
            return;

        foreach (BattleUnit unit in allyFormation.GetAliveUnits())
        {
            BattleUnitView view = viewManager.GetView(unit);
            if (view != null)
            {
                view.SetHighlighted(false);
                view.SetTargetMarker(false);
            }
        }

        foreach (BattleUnit unit in enemyFormation.GetAliveUnits())
        {
            BattleUnitView view = viewManager.GetView(unit);
            if (view != null)
            {
                view.SetHighlighted(false);
                view.SetTargetMarker(false);
            }
        }
    }

    private void ClearAllMarkersAndHighlights()
    {
        if (viewManager == null)
            return;

        foreach (BattleUnit unit in allyFormation.GetAliveUnits())
        {
            BattleUnitView view = viewManager.GetView(unit);
            if (view != null)
            {
                view.SetHighlighted(false);
                view.SetTargetMarker(false);
                view.SetCurrentTurnMarker(false);
            }
        }

        foreach (BattleUnit unit in enemyFormation.GetAliveUnits())
        {
            BattleUnitView view = viewManager.GetView(unit);
            if (view != null)
            {
                view.SetHighlighted(false);
                view.SetTargetMarker(false);
                view.SetCurrentTurnMarker(false);
            }
        }
    }

    private void SetActionButtonsInteractable(bool interactable)
    {
        if (attackButton != null)
            attackButton.interactable = interactable;

        if (moveButton != null)
            moveButton.interactable = interactable;
    }

    private BattleUnit ChooseBestTarget(BattleUnit attacker, List<BattleUnit> targets)
    {
        BattleUnit bestTarget = targets[0];
        int bestExpectedDamage = -1;

        foreach (BattleUnit target in targets)
        {
            int expectedDamage = Mathf.Max(1, attacker.GetAtk() - target.GetDef());
            if (expectedDamage > bestExpectedDamage)
            {
                bestExpectedDamage = expectedDamage;
                bestTarget = target;
            }
        }

        return bestTarget;
    }

    private IEnumerator ExecuteBasicAttack(BattleUnit attacker, BattleUnit target)
    {
        currentState = TurnState.ExecutingAction;

        if (attacker == null || target == null || attacker.IsDead || target.IsDead)
            yield break;

        BattleUnitView attackerView = viewManager != null ? viewManager.GetView(attacker) : null;
        BattleUnitView targetView = viewManager != null ? viewManager.GetView(target) : null;

        if (attackerView != null && targetView != null)
        {
            yield return StartCoroutine(
                attackerView.PlayAttackMove(
                    targetView.transform.position,
                    attackMoveRatio,
                    attackMoveMaxDistance,
                    attackMoveDuration
                )
            );
        }

        bool hit = BattleCalculator.RollHit(attacker, target, 100);
        if (!hit)
            yield break;

        bool isCritical = BattleCalculator.RollCritical(attacker);
        int damage = BattleCalculator.CalculateDamage(attacker, target, isCritical);

        target.TakeDamage(damage);

        if (targetView != null)
            yield return StartCoroutine(targetView.AnimateHPChange(0.25f));

        if (target.IsDead)
        {
            if (viewManager != null)
                viewManager.RemoveView(target);
        }
    }

    private bool TryAutoMove(BattleUnit unit, BattleFormation formation)
    {
        if (unit == null || unit.IsDead)
            return false;

        int direction = GetPreferredMoveDirection(unit);

        if (direction != 0)
        {
            bool moved = formation.TrySwapAdjacent(unit.SlotIndex, direction);
            if (moved)
                return true;
        }

        if (direction != -1)
        {
            bool movedForward = formation.TrySwapAdjacent(unit.SlotIndex, -1);
            if (movedForward)
                return true;
        }

        if (direction != 1)
        {
            bool movedBackward = formation.TrySwapAdjacent(unit.SlotIndex, 1);
            if (movedBackward)
                return true;
        }

        return false;
    }

    private int GetPreferredMoveDirection(BattleUnit unit)
    {
        int rank = unit.SlotIndex + 1;

        switch (unit.RangeType)
        {
            case CharacterRangeType.Melee:
                if (rank >= 3) return -1;
                return 0;

            case CharacterRangeType.Mid:
                if (rank == 4) return -1;
                return 0;

            case CharacterRangeType.Ranged:
                if (rank == 1) return 1;
                return 0;
        }

        return 0;
    }

    private void CheckBattleResult()
    {
        bool allyAlive = allyFormation.HasAliveUnits();
        bool enemyAlive = enemyFormation.HasAliveUnits();

        if (!enemyAlive)
            battleResult = BattleResultType.Victory;
        else if (!allyAlive)
            battleResult = BattleResultType.Defeat;
    }

    private IEnumerator ShowTurnStartText(int roundNumber)
    {
        if (turnStartText == null)
            yield break;

        turnStartText.text = $"Turn {roundNumber} Start";
        turnStartText.gameObject.SetActive(true);

        yield return new WaitForSeconds(turnStartTextShowTime);

        turnStartText.gameObject.SetActive(false);
    }
}