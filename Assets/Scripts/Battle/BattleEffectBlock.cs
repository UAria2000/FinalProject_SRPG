using System;
using UnityEngine;

[Serializable]
public class BattleEffectBlock
{
    public BattleEffectKind kind = BattleEffectKind.Damage;
    public StatusEffectType statusType = StatusEffectType.None;
    public StatModifierType statModifierType = StatModifierType.None;

    [Tooltip("성공률(%) - 공격 판정형 스킬의 비공격 부가효과 / 성공 판정형 스킬 / 아이템에 사용")]
    [Range(0f, 100f)] public float successChancePercent = 100f;

    [Tooltip("대상 저항을 반영할지 여부")]
    public bool affectedByResistance = false;

    [Tooltip("DMG 기반 계수(%)")]
    [Min(0f)] public float powerPercent = 100f;

    [Tooltip("고정 수치")]
    public int flatValue = 0;

    [Tooltip("지속 턴")]
    [Min(0)] public int durationTurns = 0;

    [Tooltip("툴팁/프리뷰용 아이콘. 비어 있으면 statusType 기준으로만 판단")]
    public Sprite displayIcon;

    public bool IsStatusRelated
    {
        get
        {
            return kind == BattleEffectKind.ApplyStatus ||
                   kind == BattleEffectKind.RemoveStatus;
        }
    }
}

[Serializable]
public class InventoryStackData
{
    public ItemDefinition item;
    [Min(0)] public int amount = 1;
}

[Serializable]
public class UnitInstanceStatVariance
{
    public int maxHpDelta;
    public int dmgDelta;
    public int spdDelta;

    [Tooltip("실스탯 단위. UI는 x10 표시")]
    public int hitDeltaX10;
    [Tooltip("실스탯 단위. UI는 x10 표시")]
    public int acDeltaX10;

    public int criDelta;
    public int crdDelta;

    public int poisonResistDelta;
    public int bleedResistDelta;
    public int stunResistDelta;
}

[Serializable]
public class StatVarianceRules
{
    public Vector2Int maxHpRange = Vector2Int.zero;
    public Vector2Int dmgRange = Vector2Int.zero;
    public Vector2Int spdRange = Vector2Int.zero;
    public Vector2Int hitRangeX10 = Vector2Int.zero;
    public Vector2Int acRangeX10 = Vector2Int.zero;
    public Vector2Int criRange = Vector2Int.zero;
    public Vector2Int crdRange = Vector2Int.zero;
    public Vector2Int poisonResistRange = Vector2Int.zero;
    public Vector2Int bleedResistRange = Vector2Int.zero;
    public Vector2Int stunResistRange = Vector2Int.zero;
}

[Serializable]
public class BattleStatusInstance
{
    public StatusEffectType statusType = StatusEffectType.None;
    public int remainingTurns = 0;
}

[Serializable]
public class StatusChancePreviewData
{
    public Sprite icon;
    public StatusEffectType statusType;
    public int successPercent;
}

[Serializable]
public class TargetPreviewData
{
    public bool showHitChance;
    public bool showDamageRange;
    public bool showSuccessOnly;

    public int hitChancePercent;
    public int damageMin;
    public int damageMax;
    public int successPercent;

    public readonly System.Collections.Generic.List<StatusChancePreviewData> statusChances =
        new System.Collections.Generic.List<StatusChancePreviewData>();
}

[Serializable]
public class BattleLogEntry
{
    public string text;
}
