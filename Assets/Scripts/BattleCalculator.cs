using UnityEngine;

public static class BattleCalculator
{
    public static bool RollHit(BattleUnit attacker, BattleUnit target, int baseAccuracy = 100)
    {
        int finalAccuracy = baseAccuracy + attacker.GetAcc() - target.GetEva();
        finalAccuracy = Mathf.Clamp(finalAccuracy, 0, 100);

        int roll = Random.Range(0, 100);
        return roll < finalAccuracy;
    }

    public static bool RollCritical(BattleUnit attacker)
    {
        int roll = Random.Range(0, 100);
        return roll < attacker.GetCrit();
    }

    public static int CalculateDamage(BattleUnit attacker, BattleUnit target, bool isCritical)
    {
        int damage = attacker.GetAtk() - target.GetDef();
        damage = Mathf.Max(1, damage);

        if (isCritical)
            damage = Mathf.RoundToInt(damage * 1.5f);

        return damage;
    }
}