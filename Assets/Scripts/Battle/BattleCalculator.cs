using UnityEngine;

public enum AttackResultType
{
    Crit,
    Hit,
    Graze,
    Miss
}

public struct AttackResult
{
    public AttackResultType ResultType;
    public int Damage;

    public float CritChance;
    public float HitChance;
    public float GrazeChance;
    public float MissChance;

    public bool DidHit =>
        ResultType == AttackResultType.Crit ||
        ResultType == AttackResultType.Hit ||
        ResultType == AttackResultType.Graze;
}

public static class BattleCalculator
{
    // ----- 튜닝값 -----
    // HitChance = Clamp(50 + (HIT - AC) * HitScaleK, MinHitChance, MaxHitChance)
    public const float HitScaleK = 1.5f;
    public const float MinHitChance = 5f;
    public const float MaxHitChance = 95f;

    // MissRatio = Clamp(0.3 + MissRatioA * (AC - HIT), MinMissRatio, MaxMissRatio)
    public const float BaseMissRatio = 0.3f;
    public const float MissRatioA = 0.02f;
    public const float MinMissRatio = 0.1f;
    public const float MaxMissRatio = 0.9f;

    public static AttackResult RollAttack(BattleUnit attacker, BattleUnit target)
    {
        float hitStat = attacker.HIT;
        float acStat = target.AC;

        // CRI는 % 표기이므로 0~100 기준
        float critRate = Mathf.Clamp(attacker.CRI, 0f, 100f);

        float delta = hitStat - acStat;

        float totalHitChance = Mathf.Clamp(
            50f + (delta * HitScaleK),
            MinHitChance,
            MaxHitChance
        );

        float failChance = 100f - totalHitChance;

        float missRatio = Mathf.Clamp(
            BaseMissRatio + MissRatioA * (acStat - hitStat),
            MinMissRatio,
            MaxMissRatio
        );

        float grazeRatio = 1f - missRatio;

        float missChance = failChance * missRatio;
        float grazeChance = failChance * grazeRatio;

        float critChance = totalHitChance * (critRate / 100f);
        float normalHitChance = totalHitChance - critChance;

        float roll = Random.Range(0f, 100f);

        AttackResultType resultType;
        int damage;

        if (roll < critChance)
        {
            resultType = AttackResultType.Crit;
            damage = CalculateCritDamage(attacker.DMG, attacker.CRD);
        }
        else if (roll < critChance + normalHitChance)
        {
            resultType = AttackResultType.Hit;
            damage = CalculateHitDamage(attacker.DMG);
        }
        else if (roll < critChance + normalHitChance + grazeChance)
        {
            resultType = AttackResultType.Graze;
            damage = CalculateGrazeDamage(attacker.DMG);
        }
        else
        {
            resultType = AttackResultType.Miss;
            damage = 0;
        }

        return new AttackResult
        {
            ResultType = resultType,
            Damage = damage,
            CritChance = critChance,
            HitChance = normalHitChance,
            GrazeChance = grazeChance,
            MissChance = missChance
        };
    }

    public static float CalculateTotalHitChance(float hit, float ac)
    {
        float delta = hit - ac;
        return Mathf.Clamp(50f + (delta * HitScaleK), MinHitChance, MaxHitChance);
    }

    public static float CalculateMissRatio(float hit, float ac)
    {
        return Mathf.Clamp(
            BaseMissRatio + MissRatioA * (ac - hit),
            MinMissRatio,
            MaxMissRatio
        );
    }

    public static int CalculateHitDamage(int baseDamage)
    {
        return Mathf.Max(0, baseDamage);
    }

    public static int CalculateGrazeDamage(int baseDamage)
    {
        return Mathf.Max(1, Mathf.RoundToInt(baseDamage * 0.25f));
    }

    public static int CalculateCritDamage(int baseDamage, float critDamagePercent)
    {
        float multiplier = Mathf.Max(0f, critDamagePercent) / 100f;
        return Mathf.Max(1, Mathf.RoundToInt(baseDamage * multiplier));
    }
}