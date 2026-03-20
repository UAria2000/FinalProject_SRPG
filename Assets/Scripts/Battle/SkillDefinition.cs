using System;
using System.Collections.Generic;
using UnityEngine;

[Flags]
public enum SkillTag
{
    None = 0,
    Active = 1 << 0,
    Passive = 1 << 1,
    Buff = 1 << 2,
    Common = 1 << 3,
    Unique = 1 << 4,
    Melee = 1 << 5,
    Mid = 1 << 6,
    Ranged = 1 << 7
}

public enum SkillEffectType
{
    MultiHitSingleTarget,   // 연속타격 같은 단일 대상 다단히트
    FrontAndBackShot        // 관통샷 같은 앞대상 + 뒤대상
}

public enum SkillTargetTeam
{
    Enemy,
    Ally,
    Self
}

[CreateAssetMenu(menuName = "Battle/Skill Definition")]
public class SkillDefinition : ScriptableObject
{
    [Header("Info")]
    public string skillId;
    public string skillName;
    [TextArea(2, 5)] public string description;
    public Sprite icon;

    [Header("Tags")]
    public SkillTag tags = SkillTag.Active;

    [Header("Availability")]
    [Tooltip("사용 가능 시작 위치 (0=1열, 3=4열)")]
    [Range(0, 3)] public int usableMinSlotIndex = 0;

    [Tooltip("사용 가능 마지막 위치 (0=1열, 3=4열)")]
    [Range(0, 3)] public int usableMaxSlotIndex = 3;

    [Tooltip("선택 가능한 대상 시작 위치 (0=1열, 3=4열)")]
    [Range(0, 3)] public int targetMinSlotIndex = 0;

    [Tooltip("선택 가능한 대상 마지막 위치 (0=1열, 3=4열)")]
    [Range(0, 3)] public int targetMaxSlotIndex = 3;

    public SkillTargetTeam targetTeam = SkillTargetTeam.Enemy;

    [Header("Core")]
    public SkillEffectType effectType;
    [Min(0)] public int cooldownTurns = 0;

    [Tooltip("평타 최종 명중 구간에 곱하는 배율. 예: 0.9 = 90%")]
    [Range(0f, 2f)] public float accuracyMultiplier = 1f;

    [Header("Damage")]
    [Tooltip("주 대상 피해 계수. 1.0 = DMG의 100%")]
    [Min(0f)] public float primaryDamageMultiplier = 1f;

    [Tooltip("보조 대상 피해 계수. 예: 관통샷의 뒤대상 0.5")]
    [Min(0f)] public float secondaryDamageMultiplier = 0f;

    [Tooltip("단일 대상 반복 타격 횟수. 예: 연속타격 2회")]
    [Min(1)] public int hitCount = 1;

    [Header("Tooltip Display")]
    [Tooltip("툴팁에 표시할 명중률 수치 배율. 예: 0.9 -> 900 표기")]
    [Min(1)] public int tooltipAccuracyScale = 1000;

    [Header("Restrictions")]
    [Tooltip("비어 있으면 누구나 가능. 값이 있으면 해당 unitName만 가능")]
    public List<string> allowedUnitNames = new List<string>();

    public bool HasTag(SkillTag tag)
    {
        return (tags & tag) != 0;
    }

    public bool CanBeUsedByUnit(BattleUnit unit)
    {
        if (unit == null)
            return false;

        int slot = unit.SlotIndex;
        if (slot < usableMinSlotIndex || slot > usableMaxSlotIndex)
            return false;

        if (allowedUnitNames != null && allowedUnitNames.Count > 0)
        {
            string unitName = unit.Definition != null ? unit.Definition.unitName : "";
            if (!allowedUnitNames.Contains(unitName))
                return false;
        }

        return true;
    }

    public bool CanTargetSlot(int targetSlotIndex)
    {
        return targetSlotIndex >= targetMinSlotIndex && targetSlotIndex <= targetMaxSlotIndex;
    }

    public int GetTooltipAccuracyValue()
    {
        return Mathf.RoundToInt(accuracyMultiplier * tooltipAccuracyScale);
    }

    public string GetUsablePositionText()
    {
        return $"{usableMinSlotIndex + 1}~{usableMaxSlotIndex + 1}열";
    }

    public string GetTargetPositionText()
    {
        return $"{targetMinSlotIndex + 1}~{targetMaxSlotIndex + 1}열";
    }

    public string GetDamageText()
    {
        switch (effectType)
        {
            case SkillEffectType.MultiHitSingleTarget:
                return $"DMG의 {Mathf.RoundToInt(primaryDamageMultiplier * 100f)}% × {hitCount}회";

            case SkillEffectType.FrontAndBackShot:
                return $"대상 {Mathf.RoundToInt(primaryDamageMultiplier * 100f)}%, 뒤 대상 {Mathf.RoundToInt(secondaryDamageMultiplier * 100f)}%";

            default:
                return "";
        }
    }
}