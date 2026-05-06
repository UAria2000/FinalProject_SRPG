using System.Collections.Generic;
using UnityEngine;

public static class BattleSkillInfoFormatter
{
    public static SkillClass GetSkillClass(SkillDefinition skill)
    {
        if (skill == null)
            return SkillClass.Melee;

        int raw = (int)skill.skillClass;

        // Former learnTags was a flags enum. If an old asset still contains multiple bits,
        // normalize it into one display class using this priority.
        if ((raw & (int)SkillClass.Unique) != 0) return SkillClass.Unique;
        if ((raw & (int)SkillClass.Common) != 0) return SkillClass.Common;
        if ((raw & (int)SkillClass.Melee) != 0) return SkillClass.Melee;
        if ((raw & (int)SkillClass.Mid) != 0) return SkillClass.Mid;
        if ((raw & (int)SkillClass.Ranged) != 0) return SkillClass.Ranged;

        // Old None/empty data falls back to the unit-style range tag so existing assets do not show blank badges.
        switch (skill.rangeTag)
        {
            case CharacterRangeType.Mid:
                return SkillClass.Mid;
            case CharacterRangeType.Ranged:
                return SkillClass.Ranged;
            default:
                return SkillClass.Melee;
        }
    }

    public static string GetSkillClassLabel(SkillDefinition skill)
    {
        return GetSkillClassLabel(GetSkillClass(skill));
    }

    public static string GetSkillClassLabel(SkillClass skillClass)
    {
        switch (skillClass)
        {
            case SkillClass.Unique:
                return "고유";
            case SkillClass.Common:
                return "공통";
            case SkillClass.Mid:
                return "미드";
            case SkillClass.Ranged:
                return "레인지";
            default:
                return "밀리";
        }
    }

    public static string GetPowerText(SkillDefinition skill)
    {
        if (skill == null || skill.effects == null || skill.effects.Count == 0)
            return "위력: -";

        List<string> entries = new List<string>();
        for (int i = 0; i < skill.effects.Count; i++)
        {
            BattleEffectBlock block = skill.effects[i];
            if (block == null)
                continue;

            switch (block.kind)
            {
                case BattleEffectKind.Damage:
                    entries.Add("피해 " + FormatEffectPower(block));
                    break;
                case BattleEffectKind.Heal:
                    entries.Add(IsDrainSkill(skill) ? "흡혈 " + FormatEffectPower(block) : "회복 " + FormatEffectPower(block));
                    break;
                case BattleEffectKind.Shield:
                    entries.Add("보호막 " + FormatEffectPower(block));
                    break;
            }
        }

        return entries.Count > 0 ? "위력: " + string.Join(" / ", entries) : "위력: -";
    }

    public static string GetSuccessText(SkillDefinition skill)
    {
        if (skill == null)
            return "성공률: -";

        if (skill.resolutionMode == SkillResolutionMode.Attack || skill.HasDamageEffect())
            return string.Format("명중률: {0}%", FormatPercentNumber(skill.accuracyCoefficientPercent));

        float success = 100f;
        bool found = false;
        if (skill.effects != null)
        {
            for (int i = 0; i < skill.effects.Count; i++)
            {
                BattleEffectBlock block = skill.effects[i];
                if (block == null)
                    continue;

                found = true;
                success = Mathf.Min(success, block.successChancePercent);
            }
        }

        return found ? string.Format("성공률: {0}%", FormatPercentNumber(success)) : "성공률: 100%";
    }

    public static string GetCooldownText(SkillDefinition skill)
    {
        if (skill == null)
            return "쿨타임: -";

        return skill.cooldownTurns > 0 ? string.Format("쿨타임: {0}턴", skill.cooldownTurns) : "쿨타임: 없음";
    }

    public static string GetEffectText(SkillDefinition skill)
    {
        if (skill == null)
            return "효과: -";

        List<string> entries = new List<string>();

        if (skill.effects != null)
        {
            for (int i = 0; i < skill.effects.Count; i++)
            {
                BattleEffectBlock block = skill.effects[i];
                if (block == null)
                    continue;

                AddEffectEntry(entries, FormatEffectBlock(block));
            }
        }

        switch (skill.activeGimmick)
        {
            case ActiveSkillGimmick.DelayedReinforcement:
                AddEffectEntry(entries, "지연 증원");
                break;
            case ActiveSkillGimmick.BleedDrainStrike:
                AddEffectEntry(entries, "흡혈");
                break;
            case ActiveSkillGimmick.ForceMoveTargetToRankAfterHit:
                AddEffectEntry(entries, string.Format("대상 {0}열 이동", Mathf.Clamp(skill.forcedTargetMoveToRank, 1, 4)));
                break;
            case ActiveSkillGimmick.PushTargetBackwardAfterHit:
                AddEffectEntry(entries, string.Format("대상 뒤로 {0}칸", Mathf.Max(1, skill.forcedTargetMoveSteps)));
                break;
            case ActiveSkillGimmick.AbyssReboundSelfRecoil20FromTotalDamage:
                AddEffectEntry(entries, "심연 반동");
                break;
            case ActiveSkillGimmick.BlackArenaDuel2Turns:
                AddEffectEntry(entries, string.Format("결투 {0}턴", Mathf.Max(1, skill.blackArenaDuelDurationTurns)));
                break;
        }

        if (skill.HasSelfMoveAfterUse())
            AddEffectEntry(entries, skill.selfMoveDirection == SkillSelfMoveDirection.Forward ? "사용자 전진" : "사용자 후퇴");

        if (skill.HasSelfStatusAfterUse())
            AddEffectEntry(entries, "자가 " + BattleStatusUtility.GetDisplayName(skill.selfApplyStatusAfterUse));

        if (skill.disableAfterUseInBattle)
            AddEffectEntry(entries, "전투당 1회");

        return entries.Count > 0 ? "효과: " + string.Join(", ", entries) : "효과: -";
    }

    private static string FormatEffectBlock(BattleEffectBlock block)
    {
        if (block == null)
            return string.Empty;

        switch (block.kind)
        {
            case BattleEffectKind.Heal:
                return "회복";
            case BattleEffectKind.Shield:
                return "보호막";
            case BattleEffectKind.Buff:
                return FormatTimedModifier(block, true);
            case BattleEffectKind.Debuff:
                return FormatTimedModifier(block, false);
            case BattleEffectKind.ApplyStatus:
                return FormatStatus(block.statusType, block.durationTurns);
            case BattleEffectKind.RemoveStatus:
                return BattleStatusUtility.GetDisplayName(block.statusType) + " 해제";
            default:
                return string.Empty;
        }
    }

    private static string FormatTimedModifier(BattleEffectBlock block, bool isBuff)
    {
        string stat = GetStatModifierLabel(block.statModifierType);
        int amount = Mathf.Abs(block.flatValue);
        string direction = isBuff ? "증가" : "감소";

        if (block.statModifierType == StatModifierType.IncomingDamageTakenPercent)
            direction = isBuff ? "받는 피해 감소" : "받는 피해 증가";

        if (amount > 0 && block.statModifierType != StatModifierType.IncomingDamageTakenPercent)
            return string.Format("{0} {1}% {2}", stat, amount, direction);

        if (amount > 0)
            return string.Format("{0} {1}%", direction, amount);

        return stat + " " + direction;
    }

    private static string FormatStatus(StatusEffectType statusType, int durationTurns)
    {
        string label = BattleStatusUtility.GetDisplayName(statusType);
        return durationTurns > 0 ? string.Format("{0} {1}턴", label, durationTurns) : label;
    }

    private static string FormatEffectPower(BattleEffectBlock block)
    {
        if (block == null)
            return "-";

        if (block.flatValue > 0)
            return block.flatValue.ToString();

        string basis = block.valueReference == EffectValueReference.TargetMaxHP ? "대상 최대 HP" : "DMG";
        if (block.useRandomPowerPercentRange)
            return string.Format("{0} {1}~{2}%", basis, block.GetMinPowerPercent(), block.GetMaxPowerPercent());

        return string.Format("{0} {1}%", basis, FormatPercentNumber(block.powerPercent));
    }

    private static string GetStatModifierLabel(StatModifierType type)
    {
        switch (type)
        {
            case StatModifierType.DMG: return "DMG";
            case StatModifierType.SPD: return "SPD";
            case StatModifierType.HIT: return "HIT";
            case StatModifierType.AC: return "AC";
            case StatModifierType.CRI: return "CRI";
            case StatModifierType.CRD: return "CRD";
            case StatModifierType.IncomingDamageTakenPercent: return "IDT";
            case StatModifierType.PierceBackOne: return "관통";
            default: return "효과";
        }
    }

    private static bool IsDrainSkill(SkillDefinition skill)
    {
        return skill != null && skill.activeGimmick == ActiveSkillGimmick.BleedDrainStrike;
    }


    private static void AddEffectEntry(List<string> entries, string value)
    {
        if (entries == null || string.IsNullOrWhiteSpace(value))
            return;

        if (!entries.Contains(value))
            entries.Add(value);
    }

    private static string FormatPercentNumber(float value)
    {
        return Mathf.Approximately(value, Mathf.Round(value))
            ? Mathf.RoundToInt(value).ToString()
            : value.ToString("0.#");
    }
}
