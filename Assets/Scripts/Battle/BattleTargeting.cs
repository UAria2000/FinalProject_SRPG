using System.Collections.Generic;

public static class BattleTargeting
{
    public static List<BattleUnit> GetValidSkillTargets(
        BattleUnit actor,
        SkillDefinition skill,
        BattleFormation allyFormation,
        BattleFormation enemyFormation)
    {
        List<BattleUnit> result = new List<BattleUnit>();
        if (actor == null || skill == null) return result;
        if (!actor.CanUseSkill(skill)) return result;

        if (skill.targetTeam == SkillTargetTeam.Self)
        {
            result.Add(actor);
            return result;
        }

        BattleFormation formation = skill.targetTeam == SkillTargetTeam.Ally ? allyFormation : enemyFormation;
        if (formation == null)
            return result;

        List<BattleUnit> candidates = formation.GetAliveUnits();

        for (int i = 0; i < candidates.Count; i++)
        {
            BattleUnit unit = candidates[i];
            if (unit == null || unit.IsDead) continue;
            if (!skill.CanTargetSlot(unit.SlotIndex)) continue;
            if (skill.targetTeam == SkillTargetTeam.Ally && unit.Team != actor.Team) continue;
            if (skill.targetTeam == SkillTargetTeam.Enemy && unit.Team == actor.Team) continue;
            result.Add(unit);
        }

        if (skill.targetTeam == SkillTargetTeam.Enemy &&
            skill.targetScope == TargetScope.Single)
        {
            RestrictToTauntingUnitsIfNeeded(result, formation);
        }

        return result;
    }

    public static List<BattleUnit> ResolveSkillTargets(
        BattleUnit actor,
        SkillDefinition skill,
        BattleUnit clickedTarget,
        BattleFormation allyFormation,
        BattleFormation enemyFormation)
    {
        List<BattleUnit> result = new List<BattleUnit>();
        List<BattleUnit> valid = GetValidSkillTargets(actor, skill, allyFormation, enemyFormation);
        if (valid.Count == 0) return result;

        if (skill.targetScope == TargetScope.All)
        {
            result.AddRange(valid);
            return result;
        }

        if (clickedTarget != null && valid.Contains(clickedTarget))
            result.Add(clickedTarget);

        return result;
    }

    public static List<BattleUnit> GetMovableTargets(BattleUnit actor, BattleFormation allyFormation)
    {
        List<BattleUnit> result = new List<BattleUnit>();
        if (actor == null || actor.IsDead || allyFormation == null) return result;

        BattleUnit left = allyFormation.GetUnit(actor.SlotIndex - 1);
        BattleUnit right = allyFormation.GetUnit(actor.SlotIndex + 1);

        if (BattleRules.CanSwapWith(actor, left))
            result.Add(left);

        if (BattleRules.CanSwapWith(actor, right))
            result.Add(right);

        return result;
    }

    public static List<BattleUnit> GetValidItemTargets(
        BattleUnit actor,
        ItemDefinition item,
        BattleFormation allyFormation,
        BattleFormation enemyFormation)
    {
        List<BattleUnit> result = new List<BattleUnit>();
        if (actor == null || item == null) return result;

        if (item.targetTeam == SkillTargetTeam.Self)
        {
            result.Add(actor);
            return result;
        }

        BattleFormation formation = item.targetTeam == SkillTargetTeam.Ally ? allyFormation : enemyFormation;
        if (formation == null)
            return result;

        List<BattleUnit> candidates = formation.GetAliveUnits();

        for (int i = 0; i < candidates.Count; i++)
        {
            BattleUnit unit = candidates[i];
            if (unit == null || unit.IsDead) continue;
            if (item.targetTeam == SkillTargetTeam.Ally && unit.Team != actor.Team) continue;
            if (item.targetTeam == SkillTargetTeam.Enemy && unit.Team == actor.Team) continue;
            result.Add(unit);
        }

        if (item.targetTeam == SkillTargetTeam.Enemy &&
            item.targetScope == TargetScope.Single)
        {
            RestrictToTauntingUnitsIfNeeded(result, formation);
        }

        return result;
    }

    public static List<BattleUnit> ResolveItemTargets(
        BattleUnit actor,
        ItemDefinition item,
        BattleUnit clickedTarget,
        BattleFormation allyFormation,
        BattleFormation enemyFormation)
    {
        List<BattleUnit> valid = GetValidItemTargets(actor, item, allyFormation, enemyFormation);
        List<BattleUnit> result = new List<BattleUnit>();
        if (item == null) return result;

        if (item.targetScope == TargetScope.All)
        {
            result.AddRange(valid);
            return result;
        }

        if (clickedTarget != null && valid.Contains(clickedTarget))
            result.Add(clickedTarget);

        return result;
    }

    public static List<BattleUnit> GetValidCaptureTargets(
        BattleUnit actor,
        BattleFormation enemyFormation,
        System.Func<BattleUnit, bool> predicate)
    {
        List<BattleUnit> result = new List<BattleUnit>();
        if (actor == null || actor.IsDead || enemyFormation == null || predicate == null)
            return result;

        List<BattleUnit> candidates = enemyFormation.GetAliveUnits();
        for (int i = 0; i < candidates.Count; i++)
        {
            BattleUnit unit = candidates[i];
            if (unit == null || unit.IsDead)
                continue;

            if (!predicate(unit))
                continue;

            result.Add(unit);
        }

        RestrictToTauntingUnitsIfNeeded(result, enemyFormation);
        return result;
    }

    public static BattleUnit GetSecondaryTarget(
        BattleUnit actor,
        SkillDefinition skill,
        BattleUnit primaryTarget,
        BattleFormation allyFormation,
        BattleFormation enemyFormation)
    {
        if (actor == null || skill == null || primaryTarget == null)
            return null;

        if (skill.secondaryTargetRule == SecondaryTargetRule.None)
            return null;

        BattleFormation targetFormation = primaryTarget.Team == TeamType.Ally ? allyFormation : enemyFormation;
        if (targetFormation == null)
            return null;

        switch (skill.secondaryTargetRule)
        {
            case SecondaryTargetRule.BackOne:
                {
                    BattleUnit back = targetFormation.GetUnit(primaryTarget.SlotIndex + 1);
                    if (back == null || back.IsDead)
                        return null;
                    return back;
                }
        }

        return null;
    }

    private static void RestrictToTauntingUnitsIfNeeded(List<BattleUnit> candidates, BattleFormation targetFormation)
    {
        if (candidates == null || candidates.Count == 0 || targetFormation == null)
            return;

        bool hasTauntingUnit = false;
        List<BattleUnit> aliveUnits = targetFormation.GetAliveUnits();

        for (int i = 0; i < aliveUnits.Count; i++)
        {
            BattleUnit unit = aliveUnits[i];
            if (unit != null && !unit.IsDead && unit.HasStatus(StatusEffectType.Taunt))
            {
                hasTauntingUnit = true;
                break;
            }
        }

        if (!hasTauntingUnit)
            return;

        for (int i = candidates.Count - 1; i >= 0; i--)
        {
            BattleUnit unit = candidates[i];
            if (unit == null || !unit.HasStatus(StatusEffectType.Taunt))
                candidates.RemoveAt(i);
        }
    }
}