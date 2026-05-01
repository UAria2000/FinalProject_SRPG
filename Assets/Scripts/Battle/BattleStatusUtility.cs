using UnityEngine;

/// <summary>
/// 상태이상/전투 기믹 상태 공통 규칙.
/// Poison은 구 에셋 호환용으로 Burn과 같은 enum 값으로 유지된다.
/// </summary>
public static class BattleStatusUtility
{
    public const int MaxStack = 99;
    public const int BleedCurrentHpDamagePercentPerStack = 5;
    public const int BurnIncomingDamageTakenPercentPerStack = 10;
    public const int FrostAcSpdPenaltyPercentPerStack = 10;
    public const int BlindFinalHitChancePenaltyPercent = 30;

    public static StatusEffectType Normalize(StatusEffectType statusType)
    {
        if (statusType == StatusEffectType.Poison)
            return StatusEffectType.Burn;
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
               statusType == StatusEffectType.Frost;
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
               statusType == StatusEffectType.Stealth;
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
            default: return statusType.ToString();
        }
    }

    public static int GetResistance(BattleUnit unit, StatusEffectType statusType)
    {
        if (unit == null)
            return 0;

        statusType = Normalize(statusType);
        switch (statusType)
        {
            case StatusEffectType.Stun: return unit.StunResist;
            case StatusEffectType.Bleed: return unit.BleedResist;
            case StatusEffectType.Burn: return unit.BurnResist;
            case StatusEffectType.Frost: return unit.FrostResist;
            case StatusEffectType.Blind: return unit.BlindResist;
            default: return 0;
        }
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
