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
        List<BattleUnit> candidates = formation.GetAliveUnits();

        for (int i = 0; i < candidates.Count; i++)
        {
            BattleUnit unit = candidates[i];
            if (unit == null || unit.IsDead) continue;
            if (item.targetTeam == SkillTargetTeam.Ally && unit.Team != actor.Team) continue;
            if (item.targetTeam == SkillTargetTeam.Enemy && unit.Team == actor.Team) continue;
            result.Add(unit);
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
}
