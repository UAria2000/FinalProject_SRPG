using System.Collections.Generic;

public static class BattleTargeting
{
    public static List<BattleUnit> GetBasicAttackTargets(BattleUnit attacker, BattleFormation enemyFormation)
    {
        List<BattleUnit> targets = new List<BattleUnit>();

        if (attacker == null || attacker.IsDead)
            return targets;

        if (!BattleRules.CanUseBasicAttackFromSlot(attacker.RangeType, attacker.SlotIndex))
            return targets;

        for (int i = 0; i < 4; i++)
        {
            BattleUnit target = enemyFormation.GetUnit(i);

            if (target == null || target.IsDead)
                continue;

            if (BattleRules.CanTargetWithBasicAttack(attacker.RangeType, i))
                targets.Add(target);
        }

        return targets;
    }
}