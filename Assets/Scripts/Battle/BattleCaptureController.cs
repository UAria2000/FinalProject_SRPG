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

        bool hasConfiguredMain = HasConfiguredMainPlayerCharacter();
        List<BattleUnit> allies = battleManager.AllyFormation.GetAllUnits();
        for (int i = 0; i < allies.Count; i++)
        {
            BattleUnit ally = allies[i];
            if (ally == null)
                continue;

            if (IsMainPlayerCharacter(ally))
                return !ally.IsDead;
        }

        return !hasConfiguredMain && battleManager.AllyFormation.HasLivingUnits();
    }

    public int GetInventoryCapacity()
    {
        return Mathf.Max(1, inventoryMaxSlotCount);
    }

    public bool HasInventorySpaceForCapture()
    {
        List<InventoryStackData> inventory = battleManager != null ? battleManager.GetActiveAllyInventory() : null;
        return inventory != null && inventory.Count < GetInventoryCapacity();
    }

    public bool CanActorUseCaptureCommand(BattleUnit actor)
    {
        return actor != null &&
               actor.Team == TeamType.Ally &&
               battleManager != null &&
               battleManager.IsUnitInBattle(actor) &&
               !actor.IsDead &&
               IsMainPlayerCharacter(actor) &&
               HasAnyCaptureTarget(actor);
    }

    public List<BattleUnit> GetValidCaptureTargets(BattleUnit actor)
    {
        List<BattleUnit> results = new List<BattleUnit>();
        if (!CanActorUseCaptureCommand(actor) || battleManager == null || battleManager.EnemyFormation == null)
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
        List<BattleUnit> targets = GetValidCaptureTargets(actor);
        return targets.Count > 0;
    }

    public bool CanTargetBeCaptured(BattleUnit actor, BattleUnit target)
    {
        if (!IsMainPlayerCharacter(actor) || target == null)
            return false;

        if (target.Team != TeamType.Enemy || target.IsDead || !battleManager.IsUnitInBattle(target))
            return false;

        if (GetRemainingCaptureAttempts(target) <= 0)
            return false;

        if (!HasInventorySpaceForCapture())
            return false;

        int chancePercent = GetCaptureChancePercent(target);
        return chancePercent > 0;
    }

    public int GetRemainingCaptureAttempts(BattleUnit target)
    {
        if (target == null)
            return 0;

        int value;
        if (!remainingCaptureAttemptsByUnit.TryGetValue(target, out value))
            return 0;

        return Mathf.Max(0, value);
    }

    public bool TryConsumeCaptureAttempt(BattleUnit target)
    {
        if (target == null)
            return false;

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
                if (range == null)
                    continue;

                if (range.IsInRange(hpPercent))
                    return Mathf.Clamp(Mathf.RoundToInt(range.chancePercent), 0, 100);
            }
        }

        return 0;
    }

    public bool TryAddCapturedRewardToInventory(BattleUnit target, out ItemDefinition addedItem)
    {
        addedItem = null;

        if (target == null || target.Definition == null || battleManager == null)
            return false;

        ItemDefinition rewardItem = target.Definition.captureRewardItem;
        List<InventoryStackData> inventory = battleManager.GetActiveAllyInventory();
        if (rewardItem == null || inventory == null)
            return false;

        if (!HasInventorySpaceForCapture())
            return false;

        InventoryStackData newStack = new InventoryStackData();
        newStack.item = rewardItem;
        newStack.amount = 1;
        inventory.Add(newStack);

        addedItem = rewardItem;
        return true;
    }

    private bool HasConfiguredMainPlayerCharacter()
    {
        BattlePartyRuntimeState allyState = battleManager != null ? battleManager.GetActiveAllyPartyState() : null;
        if (allyState == null || allyState.members == null)
            return false;

        for (int i = 0; i < allyState.members.Count; i++)
        {
            PartyMemberData member = allyState.members[i];
            if (member == null || member.unitDefinition == null)
                continue;

            if (member.unitDefinition.isMainPlayerCharacter)
                return true;
        }

        return false;
    }
}
