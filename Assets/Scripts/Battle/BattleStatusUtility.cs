using UnityEngine;

/// <summary>
/// 상태이상/전투 기믹 상태 공통 규칙.
/// </summary>
public static class BattleStatusUtility
{
    public const int MaxStack = 99;
    public const int BleedCurrentHpDamagePercentPerStack = 5;
    public const int BurnIdtPenaltyPercentPerStack = 10;
    public const int FrostAcSpdPenaltyPercentPerStack = 10;
    public const int BlindFinalHitChancePenaltyPercent = 30;
    public const int HuntingTargetBonusCritChancePercentPerStack = 5;
    public const int LifeStealHealPercent = 30;

    public static StatusEffectType Normalize(StatusEffectType statusType)
    {
        return statusType;
    }

    public static bool IsRealAilment(StatusEffectType statusType)
    {
        statusType = Normalize(statusType);
        return statusType == StatusEffectType.Stun ||
               statusType == StatusEffectType.Bleed ||
               statusType == StatusEffectType.Burn ||
               statusType == StatusEffectType.Frost ||
               statusType == StatusEffectType.Blind;
    }

    public static bool IsStackingAilment(StatusEffectType statusType)
    {
        statusType = Normalize(statusType);
        return statusType == StatusEffectType.Bleed ||
               statusType == StatusEffectType.Burn ||
               statusType == StatusEffectType.Frost ||
               statusType == StatusEffectType.Hunting ||
               statusType == StatusEffectType.LifeSteal;
    }

    public static bool IsNonStackingAilment(StatusEffectType statusType)
    {
        statusType = Normalize(statusType);
        return statusType == StatusEffectType.Stun ||
               statusType == StatusEffectType.Blind;
    }

    public static bool IsBattleSpecialState(StatusEffectType statusType)
    {
        return statusType == StatusEffectType.Taunt ||
               statusType == StatusEffectType.CounterStance ||
               statusType == StatusEffectType.DuelArena ||
               statusType == StatusEffectType.Stealth ||
               statusType == StatusEffectType.BattleStance ||
               statusType == StatusEffectType.Marked ||
               statusType == StatusEffectType.Hunting ||
               statusType == StatusEffectType.LifeSteal;
    }

    public static int ClampStack(int stack)
    {
        return Mathf.Clamp(stack, 0, MaxStack);
    }

    public static string GetDisplayName(StatusEffectType statusType)
    {
        statusType = Normalize(statusType);
        switch (statusType)
        {
            case StatusEffectType.Stun: return "기절";
            case StatusEffectType.Bleed: return "출혈";
            case StatusEffectType.Burn: return "화상";
            case StatusEffectType.Frost: return "동상";
            case StatusEffectType.Blind: return "실명";
            case StatusEffectType.Taunt: return "도발";
            case StatusEffectType.CounterStance: return "반격 태세";
            case StatusEffectType.DuelArena: return "결투";
            case StatusEffectType.Stealth: return "은신";
            case StatusEffectType.BattleStance: return "전투 자세";
            case StatusEffectType.Marked: return "표식";
            case StatusEffectType.Hunting: return "사냥 표식";
            case StatusEffectType.LifeSteal: return "흡혈";
            default: return statusType.ToString();
        }
    }

    public static int GetResistance(BattleUnit unit, StatusEffectType statusType)
    {
        if (unit == null)
            return 0;

        statusType = Normalize(statusType);
        int baseResist;
        switch (statusType)
        {
            case StatusEffectType.Stun: baseResist = unit.StunResist; break;
            case StatusEffectType.Bleed: baseResist = unit.BleedResist; break;
            case StatusEffectType.Burn: baseResist = unit.BurnResist; break;
            case StatusEffectType.Frost: baseResist = unit.FrostResist; break;
            case StatusEffectType.Blind: baseResist = unit.BlindResist; break;
            default: return 0;
        }

        return Mathf.Max(0, baseResist + BattlePassiveController.GetActiveStatusResistanceAuraBonus(unit));
    }
}

/// <summary>
/// 저항 판정 대상이 아닌 전투 기믹 상태를 점진적으로 분리하기 위한 enum.
/// 이번 단계에서는 기존 StatusEffectType.Taunt/CounterStance/DuelArena/Stealth 호환을 유지한다.
/// </summary>
public enum BattleSpecialStateType
{
    None,
    Taunt,
    CounterStance,
    DuelArena,
    Stealth,
    Shield
}
