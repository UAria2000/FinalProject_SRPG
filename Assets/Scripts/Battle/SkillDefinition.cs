using System.Collections.Generic;
using UnityEngine;

public enum PassiveSkillGimmick
{
    None,
    FleeNextTurnWhenAlone,
    BattleStartEnemyTeamDmgDown10Permanent,
    Bleed25ToAttackerWhenShieldedHit,
    BlackAuraShieldFromDamageTaken
}

public enum ActiveSkillGimmick
{
    None,
    DelayedReinforcement,
    BleedDrainStrike,
    ForceMoveTargetToRankAfterHit,
    PushTargetBackwardAfterHit,
    AbyssReboundSelfRecoil20FromTotalDamage,
    BlackArenaDuel2Turns
}

[CreateAssetMenu(menuName = "Battle/Skill Definition")]
public class SkillDefinition : ScriptableObject
{
    [Header("Info")]
    public string skillId;
    public string skillName;
    [TextArea(2, 5)] public string description;
    public Sprite icon;

    [Header("Identity")]
    public bool isBasicAttack = false;
    public SkillCastType castType = SkillCastType.Active;
    public ActiveSkillRole activeRole = ActiveSkillRole.Attack;
    public SkillLearnTag learnTags = SkillLearnTag.None;
    public CharacterRangeType rangeTag = CharacterRangeType.Melee;
    public PassiveSkillGimmick passiveGimmick = PassiveSkillGimmick.None;
    public ActiveSkillGimmick activeGimmick = ActiveSkillGimmick.None;

    [Header("Targeting")]
    [Range(0, 3)] public int usableMinSlotIndex = 0;
    [Range(0, 3)] public int usableMaxSlotIndex = 3;
    [Range(0, 3)] public int targetMinSlotIndex = 0;
    [Range(0, 3)] public int targetMaxSlotIndex = 3;
    public SkillTargetTeam targetTeam = SkillTargetTeam.Enemy;
    public TargetScope targetScope = TargetScope.Single;

    [Header("Resolution")]
    public SkillResolutionMode resolutionMode = SkillResolutionMode.Attack;
    [Min(0)] public int cooldownTurns = 0;
    [Tooltip("공격 판정형 스킬 전용. 100 = 기본, 80 = 낮음, 120 = 높음")]
    [Range(0f, 300f)] public float accuracyCoefficientPercent = 100f;
    public bool allowCrit = true;
    public bool allowGraze = true;

    [Header("Self Position Move (Optional)")]
    public SkillSelfMoveDirection selfMoveDirection = SkillSelfMoveDirection.None;
    [Range(0, 3)] public int selfMoveSteps = 0;

    [Header("Self Status After Use (Optional)")]
    public StatusEffectType selfApplyStatusAfterUse = StatusEffectType.None;
    [Min(0)] public int selfApplyStatusDurationTurns = 0;

    [Header("Missing HP Power Bonus (Optional)")]
    [Tooltip("체력을 잃을수록 위력이 증가합니다.")]
    public bool useMissingHpPowerBonus = false;
    [Tooltip("최대 체력 기준 몇 %를 잃을 때마다 bonusPowerPerStep 만큼 위력 증가할지")]
    [Min(1)] public int missingHpPercentStep = 1;
    [Tooltip("missingHpPercentStep 마다 증가할 위력")]
    [Min(0f)] public float bonusPowerPerStep = 0f;

    [Header("Secondary Hit (Optional)")]
    public SecondaryTargetRule secondaryTargetRule = SecondaryTargetRule.None;
    [Tooltip("보조 타격 명중계수(%)")]
    [Range(0f, 300f)] public float secondaryAccuracyCoefficientPercent = 100f;
    [Tooltip("보조 타격 DMG 계수(%)")]
    [Min(0f)] public float secondaryDamagePercent = 0f;
    [Tooltip("보조 타격에도 비데미지 부가효과를 적용할지 여부")]
    public bool secondaryApplyNonDamageEffects = false;

    [Header("Target Forced Move (Optional)")]
    [Tooltip("명중 시 대상을 이동시킬 대열 번호(1~4)")]
    [Range(1, 4)] public int forcedTargetMoveToRank = 1;
    [Tooltip("명중 시 대상을 뒤로 밀 칸 수")]
    [Range(1, 3)] public int forcedTargetMoveSteps = 1;
    [Tooltip("뒤로 밀치기 실패 시 적용할 최종 피해 계수. 0이면 미사용")]
    [Min(0f)] public float pushBackFailFinalPowerPercent = 0f;

    [Header("Active Gimmick Settings")]
    [Tooltip("증원 스킬 사용 후 몇 라운드 뒤에 소환될지")]
    [Min(1)] public int delayedReinforcementDelayRounds = 2;

    [Tooltip("심연 반동: 총 HP 피해량의 몇 %를 반동으로 받을지")]
    [Range(0f, 300f)] public float abyssReboundRecoilPercentFromTotalDamage = 20f;

    [Tooltip("결투 지속 턴")]
    [Min(1)] public int blackArenaDuelDurationTurns = 2;

    [Header("Passive Gimmick Settings")]
    [Tooltip("전투 시작 시 적 전체 DMG 감소 수치")]
    [Min(0)] public int battleStartEnemyTeamDmgDownPercent = 10;

    [Tooltip("0이면 지속 턴 미사용. permanent가 false일 때만 의미 있음")]
    [Min(0)] public int battleStartEnemyTeamDmgDownDurationTurns = 0;

    [Tooltip("체크 시 전투 종료까지 영구 적용")]
    public bool battleStartEnemyTeamDmgDownPermanent = true;

    [Tooltip("보호막이 있는 상태에서 피격 시 공격자 출혈 부여 확률")]
    [Range(0f, 100f)] public float shieldedHitBleedChancePercent = 100f;

    [Tooltip("보호막 피격 출혈 스택 수")]
    [Min(1)] public int shieldedHitBleedStacks = 1;

    [Tooltip("불멸의 메아리: 받은 HP 피해량의 몇 %만큼 보호막 획득")]
    [Range(0f, 500f)] public float blackAuraShieldGainPercentFromHpDamage = 100f;

    [Tooltip("불멸의 메아리: 추가 고정 보호막")]
    public int blackAuraShieldFlatBonus = 0;

    [Header("Effects")]
    public List<BattleEffectBlock> effects = new List<BattleEffectBlock>();

    [Tooltip("체크 시 이 스킬은 해당 전투에서 1회 사용 후 다시 사용할 수 없습니다.")]
    public bool disableAfterUseInBattle = false;

    public bool HasDamageEffect()
    {
        if (effects == null) return false;
        for (int i = 0; i < effects.Count; i++)
            if (effects[i] != null && effects[i].kind == BattleEffectKind.Damage)
                return true;
        return false;
    }

    public bool CanBeUsedFromSlot(int slotIndex)
    {
        return slotIndex >= usableMinSlotIndex && slotIndex <= usableMaxSlotIndex;
    }

    public bool CanTargetSlot(int slotIndex)
    {
        return slotIndex >= targetMinSlotIndex && slotIndex <= targetMaxSlotIndex;
    }

    public bool IsEnemyTargetAttackSkill()
    {
        return castType == SkillCastType.Active &&
               resolutionMode == SkillResolutionMode.Attack &&
               targetTeam == SkillTargetTeam.Enemy &&
               HasDamageEffect();
    }

    public bool ShouldShowTargetPreview()
    {
        return targetTeam == SkillTargetTeam.Enemy &&
               castType == SkillCastType.Active;
    }

    public bool HasSecondaryHit()
    {
        return secondaryTargetRule != SecondaryTargetRule.None && secondaryDamagePercent > 0f;
    }

    public bool HasSelfMoveAfterUse()
    {
        return selfMoveDirection != SkillSelfMoveDirection.None && selfMoveSteps > 0;
    }

    public bool HasSelfStatusAfterUse()
    {
        return selfApplyStatusAfterUse != StatusEffectType.None && selfApplyStatusDurationTurns > 0;
    }

    public bool HasMissingHpPowerBonus()
    {
        return useMissingHpPowerBonus && missingHpPercentStep > 0 && bonusPowerPerStep > 0f;
    }

    public float GetMissingHpBonusPowerPercent(BattleUnit actor)
    {
        if (!HasMissingHpPowerBonus() || actor == null || actor.MaxHP <= 0)
            return 0f;

        int missingHp = Mathf.Max(0, actor.MaxHP - actor.CurrentHP);
        if (missingHp <= 0)
            return 0f;

        float missingPercent = (missingHp / (float)actor.MaxHP) * 100f;
        int steps = Mathf.FloorToInt(missingPercent / missingHpPercentStep);
        if (steps <= 0)
            return 0f;

        return steps * bonusPowerPerStep;
    }

    public int GetPrimaryPowerPercent()
    {
        if (effects == null) return 100;
        for (int i = 0; i < effects.Count; i++)
        {
            BattleEffectBlock block = effects[i];
            if (block != null && block.kind == BattleEffectKind.Damage)
            {
                if (block.useRandomPowerPercentRange)
                    return Mathf.RoundToInt((block.GetMinPowerPercent() + block.GetMaxPowerPercent()) * 0.5f);

                return Mathf.RoundToInt(block.powerPercent);
            }
        }
        return 100;
    }

    public string GetUsablePositionText()
    {
        return string.Format("{0}~{1}", usableMinSlotIndex + 1, usableMaxSlotIndex + 1);
    }

    public string GetTargetPositionText()
    {
        return string.Format("{0}~{1}", targetMinSlotIndex + 1, targetMaxSlotIndex + 1);
    }

    public bool HasForcedTargetMoveAfterHit()
    {
        return activeGimmick == ActiveSkillGimmick.ForceMoveTargetToRankAfterHit;
    }

    public int GetForcedTargetMoveTargetSlotIndex()
    {
        return Mathf.Clamp(forcedTargetMoveToRank - 1, 0, 3);
    }

    public bool HasForcedTargetPushBackAfterHit()
    {
        return activeGimmick == ActiveSkillGimmick.PushTargetBackwardAfterHit && forcedTargetMoveSteps > 0;
    }

    public int GetForcedTargetMoveSteps()
    {
        return Mathf.Max(1, forcedTargetMoveSteps);
    }

    public float GetPushBackFailFinalPowerPercent()
    {
        return Mathf.Max(0f, pushBackFailFinalPowerPercent);
    }

    public int GetDelayedReinforcementDelayRounds()
    {
        return Mathf.Max(1, delayedReinforcementDelayRounds);
    }

    public float GetAbyssReboundRecoilPercentFromTotalDamage()
    {
        return Mathf.Max(0f, abyssReboundRecoilPercentFromTotalDamage);
    }

    public int GetBlackArenaDuelDurationTurns()
    {
        return Mathf.Max(1, blackArenaDuelDurationTurns);
    }

    public int GetBattleStartEnemyTeamDmgDownPercent()
    {
        return Mathf.Max(0, battleStartEnemyTeamDmgDownPercent);
    }

    public int GetBattleStartEnemyTeamDmgDownDurationTurns()
    {
        return Mathf.Max(0, battleStartEnemyTeamDmgDownDurationTurns);
    }

    public bool IsBattleStartEnemyTeamDmgDownPermanent()
    {
        return battleStartEnemyTeamDmgDownPermanent || battleStartEnemyTeamDmgDownDurationTurns <= 0;
    }

    public float GetShieldedHitBleedChancePercent()
    {
        return Mathf.Clamp(shieldedHitBleedChancePercent, 0f, 100f);
    }

    public int GetShieldedHitBleedStacks()
    {
        return Mathf.Max(1, shieldedHitBleedStacks);
    }

    public float GetBlackAuraShieldGainPercentFromHpDamage()
    {
        return Mathf.Max(0f, blackAuraShieldGainPercentFromHpDamage);
    }

    public int GetBlackAuraShieldFlatBonus()
    {
        return blackAuraShieldFlatBonus;
    }
}