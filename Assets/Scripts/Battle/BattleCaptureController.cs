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
        return battleManager != null &&
               battleManager.AllyPartyDefinition != null &&
               battleManager.AllyPartyDefinition.inventory != null &&
               battleManager.AllyPartyDefinition.inventory.Count < GetInventoryCapacity();
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
        return BattleTargeting.GetValidCaptureTargets(
            actor,
            battleManager != null ? battleManager.EnemyFormation : null,
            delegate (BattleUnit target)
            {
                return CanTargetBeCaptured(actor, target);
            });
    }

    public bool HasAnyCaptureTarget(BattleUnit actor)
    {
        List<BattleUnit> validTargets = GetValidCaptureTargets(actor);
        return validTargets != null && validTargets.Count > 0;
    }

    public bool CanTargetBeCaptured(BattleUnit actor, BattleUnit target)
    {
        if (actor == null || target == null || battleManager == null)
            return false;

        if (!IsMainPlayerCharacter(actor))
            return false;

        if (actor.IsDead || target.IsDead)
            return false;

        if (!battleManager.IsUnitInBattle(actor) || !battleManager.IsUnitInBattle(target))
            return false;

        if (target.Team != TeamType.Enemy)
            return false;

        if (target.Definition == null || !target.Definition.canBeCaptured)
            return false;

        if (target.Definition.captureRewardItem == null)
            return false;

        if (!HasInventorySpaceForCapture())
            return false;

        return GetRemainingCaptureAttempts(target) > 0;
    }

    public int GetRemainingCaptureAttempts(BattleUnit target)
    {
        if (target == null)
            return 0;

        int remaining;
        if (remainingCaptureAttemptsByUnit.TryGetValue(target, out remaining))
            return Mathf.Max(0, remaining);

        return Mathf.Max(0, maxCaptureAttemptsPerEnemyInstance);
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
        int restored = Mathf.Min(Mathf.Max(0, maxCaptureAttemptsPerEnemyInstance), remaining + 1);
        remainingCaptureAttemptsByUnit[target] = restored;
    }

    public int GetCaptureChancePercent(BattleUnit target)
    {
        if (target == null || target.MaxHP <= 0)
            return 0;

        float hpPercent = Mathf.Clamp((target.CurrentHP / (float)target.MaxHP) * 100f, 0f, 100f);
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
        PartyDefinition allyParty = battleManager.AllyPartyDefinition;
        if (rewardItem == null || allyParty == null || allyParty.inventory == null)
            return false;

        if (!HasInventorySpaceForCapture())
            return false;

        InventoryStackData newStack = new InventoryStackData();
        newStack.item = rewardItem;
        newStack.amount = 1;
        allyParty.inventory.Add(newStack);

        addedItem = rewardItem;
        return true;
    }

    private bool HasConfiguredMainPlayerCharacter()
    {
        if (battleManager == null || battleManager.AllyPartyDefinition == null || battleManager.AllyPartyDefinition.members == null)
            return false;

        for (int i = 0; i < battleManager.AllyPartyDefinition.members.Count; i++)
        {
            PartyMemberData member = battleManager.AllyPartyDefinition.members[i];
            if (member == null || member.unitDefinition == null)
                continue;

            if (member.unitDefinition.isMainPlayerCharacter)
                return true;
        }

        return false;
    }
}
