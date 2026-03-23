using System.Collections.Generic;
using UnityEngine;

public struct AttackResult
{
    public AttackResultType ResultType;
    public int Damage;
    public float CritChance;
    public float HitChance;
    public float GrazeChance;
    public float MissChance;

    public bool DidHit
    {
        get
        {
            return ResultType == AttackResultType.Crit ||
                   ResultType == AttackResultType.Hit ||
                   ResultType == AttackResultType.Graze;
        }
    }
}

public static class BattleCalculator
{
    public const float HitScaleK = 1.5f;
    public const float MinHitChance = 5f;
    public const float MaxHitChance = 95f;

    public const float BaseMissRatio = 0.3f;
    public const float MissRatioA = 0.02f;
    public const float MinMissRatio = 0.1f;
    public const float MaxMissRatio = 0.9f;

    public static AttackResult ResolveAttack(BattleUnit attacker, BattleUnit target, SkillDefinition skill)
    {
        return ResolveAttack(attacker, target, skill, -1f, -1f);
    }

    public static AttackResult ResolveAttack(BattleUnit attacker, BattleUnit target, SkillDefinition skill, float accuracyCoefficientOverridePercent, float damagePowerPercentOverride)
    {
        float accuracyPercent = accuracyCoefficientOverridePercent >= 0f
            ? accuracyCoefficientOverridePercent
            : (skill != null ? skill.accuracyCoefficientPercent : 100f);

        float effectiveHit = attacker.HIT * (accuracyPercent * 0.01f);
        float acStat = target.AC;

        float totalHitChance = CalculateTotalHitChance(effectiveHit, acStat);
        float failChance = 100f - totalHitChance;

        float missRatio = CalculateMissRatio(effectiveHit, acStat);
        float grazeRatio = 1f - missRatio;

        float missChance = failChance * missRatio;
        float grazeChance = skill != null && skill.allowGraze ? failChance * grazeRatio : 0f;
        if (skill != null && !skill.allowGraze)
            missChance = failChance;

        float critRate = skill != null && skill.allowCrit ? Mathf.Clamp(attacker.CRI, 0f, 100f) : 0f;
        float critChance = totalHitChance * (critRate / 100f);
        float normalHitChance = totalHitChance - critChance;

        float roll = Random.Range(0f, 100f);

        AttackResultType resultType;
        if (roll < critChance)
            resultType = AttackResultType.Crit;
        else if (roll < critChance + normalHitChance)
            resultType = AttackResultType.Hit;
        else if (roll < critChance + normalHitChance + grazeChance)
            resultType = AttackResultType.Graze;
        else
            resultType = AttackResultType.Miss;

        int baseRollDamage = RollDamageFromUnitDmg(attacker.DMG);
        int damage = 0;

        float damageMultiplier = GetTotalDamageMultiplier(skill, damagePowerPercentOverride);
        int scaledDamage = Mathf.Max(0, Mathf.FloorToInt(baseRollDamage * damageMultiplier));

        switch (resultType)
        {
            case AttackResultType.Crit:
                damage = CalculateCritDamage(scaledDamage, attacker.CRD);
                break;
            case AttackResultType.Hit:
                damage = CalculateHitDamage(scaledDamage);
                break;
            case AttackResultType.Graze:
                damage = CalculateGrazeDamage(scaledDamage);
                break;
            case AttackResultType.Miss:
                damage = 0;
                break;
        }

        AttackResult result = new AttackResult();
        result.ResultType = resultType;
        result.Damage = damage;
        result.CritChance = critChance;
        result.HitChance = normalHitChance;
        result.GrazeChance = grazeChance;
        result.MissChance = missChance;
        return result;
    }

    public static TargetPreviewData BuildSkillPreview(BattleUnit attacker, BattleUnit target, SkillDefinition skill)
    {
        TargetPreviewData data = new TargetPreviewData();
        if (attacker == null || target == null || skill == null)
            return data;

        if (skill.resolutionMode == SkillResolutionMode.Attack && skill.HasDamageEffect())
        {
            data.showHitChance = true;
            data.showDamageRange = true;

            float effectiveHit = attacker.HIT * (skill.accuracyCoefficientPercent * 0.01f);
            float totalHitChance = CalculateTotalHitChance(effectiveHit, target.AC);
            float critChance = skill.allowCrit ? totalHitChance * (Mathf.Clamp(attacker.CRI, 0f, 100f) / 100f) : 0f;
            float normalHitChance = Mathf.Max(0f, totalHitChance - critChance);

            data.hitChancePercent = Mathf.RoundToInt(critChance + normalHitChance);

            int minBase;
            int maxBase;
            GetDamageVarianceRange(attacker.DMG, out minBase, out maxBase);
            float multiplier = GetTotalDamageMultiplier(skill, -1f);
            data.damageMin = Mathf.Max(0, Mathf.FloorToInt(minBase * multiplier));
            data.damageMax = Mathf.Max(0, Mathf.FloorToInt(maxBase * multiplier));

            AppendStatusChances(data, target, skill.effects);
        }
        else
        {
            data.showSuccessOnly = true;

            int maxChance = 0;
            for (int i = 0; i < skill.effects.Count; i++)
            {
                BattleEffectBlock block = skill.effects[i];
                if (block == null) continue;

                int finalChance = CalculateEffectSuccessChance(block, target);
                if (finalChance > maxChance)
                    maxChance = finalChance;
            }
            data.successPercent = maxChance;
            AppendStatusChances(data, target, skill.effects);
        }

        return data;
    }

    public static int CalculateEffectSuccessChance(BattleEffectBlock block, BattleUnit target)
    {
        if (block == null)
            return 0;

        float chance = Mathf.Clamp(block.successChancePercent, 0f, 100f);
        if (target != null && block.affectedByResistance)
        {
            int resist = target.GetResistance(block.statusType);
            chance *= Mathf.Clamp01((100f - resist) / 100f);
        }

        return Mathf.RoundToInt(chance);
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

    public static int CalculateCritDamage(int baseDamage, int critDamagePercent)
    {
        float multiplier = Mathf.Max(0f, critDamagePercent) / 100f;
        return Mathf.Max(1, Mathf.RoundToInt(baseDamage * multiplier));
    }

    public static int RollDamageFromUnitDmg(int unitDamage)
    {
        int minValue;
        int maxValue;
        GetDamageVarianceRange(unitDamage, out minValue, out maxValue);
        return Random.Range(minValue, maxValue + 1);
    }

    public static void GetDamageVarianceRange(int unitDamage, out int minValue, out int maxValue)
    {
        int variance = Mathf.FloorToInt(unitDamage * 0.1f);
        minValue = unitDamage - variance;
        maxValue = unitDamage + variance;
        if (minValue < 0) minValue = 0;
        if (maxValue < minValue) maxValue = minValue;
    }

    public static float GetTotalDamageMultiplier(SkillDefinition skill, float damagePowerPercentOverride)
    {
        if (damagePowerPercentOverride >= 0f)
            return damagePowerPercentOverride * 0.01f;

        if (skill == null || skill.effects == null)
            return 1f;

        float total = 0f;
        for (int i = 0; i < skill.effects.Count; i++)
        {
            BattleEffectBlock block = skill.effects[i];
            if (block == null) continue;
            if (block.kind == BattleEffectKind.Damage)
                total += block.powerPercent * 0.01f;
        }

        return total > 0f ? total : 1f;
    }

    public static void AppendStatusChances(TargetPreviewData data, BattleUnit target, List<BattleEffectBlock> effects)
    {
        if (data == null || effects == null)
            return;

        for (int i = 0; i < effects.Count; i++)
        {
            BattleEffectBlock block = effects[i];
            if (block == null) continue;
            if (block.statusType == StatusEffectType.None) continue;
            if (block.kind != BattleEffectKind.ApplyStatus) continue;

            StatusChancePreviewData preview = new StatusChancePreviewData();
            preview.icon = block.displayIcon;
            preview.statusType = block.statusType;
            preview.successPercent = CalculateEffectSuccessChance(block, target);
            data.statusChances.Add(preview);
        }
    }
}
