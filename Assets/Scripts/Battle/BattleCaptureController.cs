using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BattleCaptureController : MonoBehaviour
{
    private readonly Dictionary<BattleUnit, int> remainingCaptureAttemptsByUnit = new Dictionary<BattleUnit, int>();

    private BattleManager battleManager;
    private int inventoryMaxSlotCount;
    private int maxCaptureAttemptsPerEnemyInstance;
    private List<CaptureChanceRange> captureChanceRanges;

    public void Initialize(
        BattleManager manager,
        int inventoryCapacity,
        int maxCaptureAttempts,
        List<CaptureChanceRange> configuredRanges)
    {
        battleManager = manager;
        inventoryMaxSlotCount = inventoryCapacity;
        maxCaptureAttemptsPerEnemyInstance = maxCaptureAttempts;
        captureChanceRanges = configuredRanges;
    }

    public void InitializeCaptureAttempts()
    {
        remainingCaptureAttemptsByUnit.Clear();

        List<BattleUnit> enemies = battleManager != null && battleManager.EnemyFormation != null
            ? battleManager.EnemyFormation.GetAllUnits()
            : null;

        if (enemies == null)
            return;

        for (int i = 0; i < enemies.Count; i++)
        {
            BattleUnit enemy = enemies[i];
            if (enemy == null)
                continue;

            remainingCaptureAttemptsByUnit[enemy] = Mathf.Max(0, maxCaptureAttemptsPerEnemyInstance);
        }
    }

    public void NotifyUnitLeftBattle(BattleUnit unit)
    {
        if (unit == null)
            return;

        remainingCaptureAttemptsByUnit.Remove(unit);
    }

    public bool IsMainPlayerCharacter(BattleUnit unit)
    {
        return unit != null &&
               unit.Team == TeamType.Ally &&
               unit.Definition != null &&
               unit.Definition.isMainPlayerCharacter;
    }

    public bool IsMainPlayerAliveInBattle()
    {
        if (battleManager == null || battleManager.AllyFormation == null)
            return false;

        List<BattleUnit> allies = battleManager.AllyFormation.GetAllUnits();
        bool hasConfiguredMain = false;

        for (int i = 0; i < allies.Count; i++)
        {
            BattleUnit ally = allies[i];
            if (ally == null)
                continue;

            if (IsMainPlayerCharacter(ally))
            {
                hasConfiguredMain = true;
                if (!ally.IsDead)
                    return true;
            }
        }

        return !hasConfiguredMain && battleManager.AllyFormation.HasLivingUnits();
    }

    public int GetInventoryCapacity()
    {
        return Mathf.Max(1, inventoryMaxSlotCount);
    }

    public bool HasInventorySpaceForCapture()
    {
        // 현재 구조에서는 월드 인벤토리/포로 쪽으로 넘기므로 일단 true 유지.
        // 추후 월드 포로/창고 용량 체크를 여기로 옮기면 됨.
        return true;
    }

    // 재귀 방지용: "커맨드 자체를 열 수 있는가"의 기본 조건만 검사
    private bool CanActorUseCaptureCommandCore(BattleUnit actor)
    {
        return actor != null &&
               actor.Team == TeamType.Ally &&
               battleManager != null &&
               battleManager.IsUnitInBattle(actor) &&
               !actor.IsDead &&
               IsMainPlayerCharacter(actor);
    }

    public bool CanActorUseCaptureCommand(BattleUnit actor)
    {
        return CanActorUseCaptureCommandCore(actor) && HasAnyCaptureTarget(actor);
    }

    public List<BattleUnit> GetValidCaptureTargets(BattleUnit actor)
    {
        List<BattleUnit> results = new List<BattleUnit>();

        if (!CanActorUseCaptureCommandCore(actor) || battleManager == null || battleManager.EnemyFormation == null)
            return results;

        List<BattleUnit> enemies = battleManager.EnemyFormation.GetAllUnits();
        for (int i = 0; i < enemies.Count; i++)
        {
            BattleUnit enemy = enemies[i];
            if (CanTargetBeCaptured(actor, enemy))
                results.Add(enemy);
        }

        return results;
    }

    public bool HasAnyCaptureTarget(BattleUnit actor)
    {
        if (!CanActorUseCaptureCommandCore(actor) || battleManager == null || battleManager.EnemyFormation == null)
            return false;

        List<BattleUnit> enemies = battleManager.EnemyFormation.GetAllUnits();
        for (int i = 0; i < enemies.Count; i++)
        {
            if (CanTargetBeCaptured(actor, enemies[i]))
                return true;
        }

        return false;
    }

    public bool CanTargetBeCaptured(BattleUnit actor, BattleUnit target)
    {
        if (!CanActorUseCaptureCommandCore(actor) || target == null)
            return false;

        if (target.Team != TeamType.Enemy || target.IsDead || !battleManager.IsUnitInBattle(target))
            return false;

        if (target.Definition == null || !target.Definition.canBeCaptured)
            return false;

        if (GetRemainingCaptureAttempts(target) <= 0)
            return false;

        if (!HasInventorySpaceForCapture())
            return false;

        return GetCaptureChancePercent(target) > 0;
    }

    public int GetRemainingCaptureAttempts(BattleUnit target)
    {
        if (target == null)
            return 0;

        return remainingCaptureAttemptsByUnit.TryGetValue(target, out int value)
            ? Mathf.Max(0, value)
            : 0;
    }

    public bool TryConsumeCaptureAttempt(BattleUnit target)
    {
        int remaining = GetRemainingCaptureAttempts(target);
        if (remaining <= 0)
            return false;

        remainingCaptureAttemptsByUnit[target] = remaining - 1;
        return true;
    }

    public void RefundCaptureAttempt(BattleUnit target)
    {
        if (target == null)
            return;

        int remaining = GetRemainingCaptureAttempts(target);
        remainingCaptureAttemptsByUnit[target] = Mathf.Min(maxCaptureAttemptsPerEnemyInstance, remaining + 1);
    }

    public int GetCaptureChancePercent(BattleUnit target)
    {
        if (target == null || target.MaxHP <= 0)
            return 0;

        float hpPercent = target.CurrentHP / (float)target.MaxHP * 100f;

        if (captureChanceRanges != null)
        {
            for (int i = 0; i < captureChanceRanges.Count; i++)
            {
                CaptureChanceRange range = captureChanceRanges[i];
                if (range != null && range.IsInRange(hpPercent))
                    return Mathf.Clamp(Mathf.RoundToInt(range.chancePercent), 0, 100);
            }
        }

        return 0;
    }

    public bool TryAddCapturedRewardToInventory(BattleUnit target, out ItemDefinition addedItem)
    {
        addedItem = target != null && target.Definition != null
            ? target.Definition.captureRewardItem
            : null;

        return target != null &&
               target.Definition != null &&
               target.Definition.canBeCaptured;
    }
}