using System.Collections;
using TMPro;
using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    [Header("Ally Team")]
    public UnitDefinition[] allyDefinitions = new UnitDefinition[4];

    [Header("Enemy Team")]
    public UnitDefinition[] enemyDefinitions = new UnitDefinition[4];

    [Header("View")]
    [SerializeField] private BattleViewManager viewManager;

    [Header("Timings")]
    [SerializeField] private float turnDelay = 0.6f;
    [SerializeField] private float moveAnimationDuration = 0.55f;

    [Header("Attack Move")]
    [SerializeField] private float attackMoveRatio = 0.45f;
    [SerializeField] private float attackMoveMaxDistance = 260f;
    [SerializeField] private float attackMoveDuration = 0.6f;

    [Header("Round UI")]
    [SerializeField] private TMP_Text turnStartText;
    [SerializeField] private float turnStartTextShowTime = 1.0f;

    private BattleFormation allyFormation;
    private BattleFormation enemyFormation;
    private TurnManager turnManager;
    private int currentRound = 0;
    private TurnState currentState = TurnState.Waiting;
    private BattleResultType battleResult = BattleResultType.None;

    private void Start()
    {
        StartBattle();
    }

    public void StartBattle()
    {
        allyFormation = new BattleFormation();
        enemyFormation = new BattleFormation();
        turnManager = new TurnManager();
        currentRound = 0;

        if (turnStartText != null)
            turnStartText.gameObject.SetActive(false);

        for (int i = 0; i < 4; i++)
        {
            if (allyDefinitions[i] != null)
            {
                BattleUnit ally = new BattleUnit(allyDefinitions[i], TeamType.Ally, i);
                allyFormation.SetUnit(i, ally);

                if (viewManager != null)
                    viewManager.CreateView(ally);
            }

            if (enemyDefinitions[i] != null)
            {
                BattleUnit enemy = new BattleUnit(enemyDefinitions[i], TeamType.Enemy, i);
                enemyFormation.SetUnit(i, enemy);

                if (viewManager != null)
                    viewManager.CreateView(enemy);
            }
        }

        if (viewManager != null)
            viewManager.RefreshAllPositionsInstant(allyFormation, enemyFormation);

        Debug.Log("=== BATTLE START ===");
        PrintFormations();

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
        Debug.Log($"=== BATTLE ENDED : {battleResult} ===");
    }

    private IEnumerator ExecuteTurn(BattleUnit unit)
    {
        if (unit == null || unit.IsDead)
            yield break;

        Debug.Log($"\n-- TURN START: {unit}");

        currentState = unit.Team == TeamType.Ally ? TurnState.PlayerInput : TurnState.EnemyThinking;

        BattleFormation myFormation = unit.Team == TeamType.Ally ? allyFormation : enemyFormation;
        BattleFormation enemyFormationRef = unit.Team == TeamType.Ally ? enemyFormation : allyFormation;

        List<BattleUnit> targets = BattleTargeting.GetBasicAttackTargets(unit, enemyFormationRef);

        if (targets.Count > 0)
        {
            BattleUnit target = ChooseBestTarget(unit, targets);
            yield return StartCoroutine(ExecuteBasicAttack(unit, target));
        }
        else
        {
            bool moved = TryAutoMove(unit, myFormation);

            if (moved)
            {
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
            }
            else
            {
                Debug.Log($"{unit.Name} cannot attack or move.");
            }
        }

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

        PrintFormations();

        currentState = TurnState.TurnEnding;
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

        Debug.Log($"{attacker.Name} attacks {target.Name}");

        bool hit = BattleCalculator.RollHit(attacker, target, 100);
        if (!hit)
        {
            Debug.Log("-> Miss!");
            yield break;
        }

        bool isCritical = BattleCalculator.RollCritical(attacker);
        int damage = BattleCalculator.CalculateDamage(attacker, target, isCritical);

        target.TakeDamage(damage);

        if (targetView != null)
            yield return StartCoroutine(targetView.AnimateHPChange(0.25f));

        if (isCritical)
            Debug.Log($"-> Critical! {damage} damage");
        else
            Debug.Log($"-> {damage} damage");

        if (target.IsDead)
        {
            Debug.Log($"-> {target.Name} died");

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
            {
                Debug.Log($"{unit.Name} moved to slot {unit.SlotIndex + 1}");
                return true;
            }
        }

        if (direction != -1)
        {
            bool movedForward = formation.TrySwapAdjacent(unit.SlotIndex, -1);
            if (movedForward)
            {
                Debug.Log($"{unit.Name} moved forward to slot {unit.SlotIndex + 1}");
                return true;
            }
        }

        if (direction != 1)
        {
            bool movedBackward = formation.TrySwapAdjacent(unit.SlotIndex, 1);
            if (movedBackward)
            {
                Debug.Log($"{unit.Name} moved backward to slot {unit.SlotIndex + 1}");
                return true;
            }
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

    private void PrintFormations()
    {
        Debug.Log("=== ALLY FORMATION ===");
        PrintFormation(allyFormation);

        Debug.Log("=== ENEMY FORMATION ===");
        PrintFormation(enemyFormation);
    }

    private void PrintFormation(BattleFormation formation)
    {
        for (int i = 0; i < 4; i++)
        {
            BattleUnit unit = formation.GetUnit(i);
            string text = unit == null ? "[Empty]" : unit.ToString();
            Debug.Log($"Slot {i + 1}: {text}");
        }
    }

    private IEnumerator ShowTurnStartText(int roundNumber)
    {
        if (turnStartText == null)
            yield break;

        turnStartText.text = $"Turn {roundNumber}";
        turnStartText.gameObject.SetActive(true);

        yield return new WaitForSeconds(turnStartTextShowTime);

        turnStartText.gameObject.SetActive(false);
    }
}