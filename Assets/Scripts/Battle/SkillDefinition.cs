using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public enum PassiveSkillGimmick
{
    None,
    FleeNextTurnWhenAlone,
    BattleStartEnemyTeamDmgDown10Permanent,
    Bleed25ToAttackerWhenShieldedHit,
    BlackAuraShieldFromDamageTaken,
    HealSelfMaxHpPercentOnTurnStart,
    TeamStatusResistAuraWhileAlive,
    HumanJudgeEnrageWhenLinkedBossDies,
    HumanHighPriestReviveLinkedBossOnDeath,
    DragonIdt99WhileDragonSoldierAlive
}

public enum ActiveSkillGimmick
{
    None,
    DelayedReinforcement,
    BleedDrainStrike,
    ForceMoveTargetToRankAfterHit,
    PushTargetBackwardAfterHit,
    AbyssReboundSelfRecoil20FromTotalDamage,
    BlackArenaDuel2Turns,
    FleeOnNextOwnTurn,
    RandomRepositionTargetsOnHit,
    ImmediateSummonInFront,
    PullTargetForwardAfterHit,
    ShieldSelfFromDamageDealt,
    ChainLightning,
    ChainExecutionOnce
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
    [FormerlySerializedAs("learnTags")]
    public SkillClass skillClass = SkillClass.Melee;
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

    [Header("Use Conditions")]
    [Tooltip("이 상태가 자기 자신에게 있어야 사용할 수 있습니다. None이면 조건 없음.")]
    public StatusEffectType requiredSelfStatusToUse = StatusEffectType.None;
    [Tooltip("이 상태가 자기 자신에게 있으면 사용할 수 없습니다. None이면 조건 없음.")]
    public StatusEffectType blockedSelfStatusToUse = StatusEffectType.None;
    [Tooltip("같은 진영에 자신 혼자만 남았을 때만 사용할 수 있습니다.")]
    public bool onlyUsableWhenAlone = false;
    [Tooltip("같은 진영의 생존 개체 수가 maxOwnTeamLivingCountToUse 이하일 때만 사용할 수 있습니다.")]
    public bool requireOwnTeamLivingCountAtOrBelow = false;
    [Range(1, 4)] public int maxOwnTeamLivingCountToUse = 3;

    [Header("Resolution")]
    public SkillResolutionMode resolutionMode = SkillResolutionMode.Attack;
    [Tooltip("주 타격 반복 횟수. 2 이상이면 같은 대상을 순차 타격합니다.")]
    [Min(1)] public int primaryHitCount = 1;
    [Min(0)] public int cooldownTurns = 0;
    [Tooltip("전투 시작 시 처음부터 쿨타임으로 시작할 턴 수. 4면 4번째 자기 턴부터 사용 가능해집니다.")]
    [Min(0)] public int initialCooldownTurns = 0;
    [Tooltip("공격 판정형 스킬 전용. 100 = 기본, 80 = 낮음, 120 = 높음")]
    [Range(0f, 300f)] public float accuracyCoefficientPercent = 100f;
    public bool allowCrit = true;
    public bool allowGraze = true;

    [Header("Extra Target Resolution (Optional)")]
    [Tooltip("아군 단일 대상 스킬에서 선택 대상 외에 시전자 자신에게도 같은 효과를 적용합니다.")]
    public bool alsoApplyToSelfWhenTargetingAlly = false;

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
    [Tooltip("명중 후 강제 이동이 실제로 발동할 확률입니다.")]
    [Range(0f, 100f)] public float forcedTargetMoveChancePercent = 100f;
    [Tooltip("뒤로 밀치기 실패 시 적용할 최종 피해 계수. 0이면 미사용")]
    [Min(0f)] public float pushBackFailFinalPowerPercent = 0f;

    [Header("Damage Bonus Conditions (Optional)")]
    [Tooltip("대상에게 보호막이 있을 때 이 수치가 0보다 크면 주 타격 피해 계수를 이 값 이상으로 보정합니다.")]
    [Min(0f)] public float shieldedTargetDamagePowerPercent = 0f;

    [Tooltip("대상에게 특정 상태가 있을 때 주 타격 피해 계수에 더할 추가 계수입니다. 0이면 미사용.")]
    public StatusEffectType targetStatusBonusType = StatusEffectType.None;
    [Min(0f)] public float targetStatusBonusPowerAddPercent = 0f;

    [Tooltip("보조 타격 대상이 없을 때 주 타격 피해 계수를 이 값 이상으로 보정합니다. 0이면 미사용.")]
    [Min(0f)] public float missingSecondaryTargetDamagePowerPercent = 0f;

    [Header("Active Gimmick Settings")]
    [Tooltip("명중한 대상마다 무작위 위치 강제배치가 발동할 확률입니다.")]
    [Range(0f, 100f)] public float randomRepositionChancePercent = 20f;

    [Tooltip("증원 스킬 사용 후 몇 라운드 뒤에 소환될지")]
    [Min(1)] public int delayedReinforcementDelayRounds = 2;

    [Tooltip("심연 반동: 총 HP 피해량의 몇 %를 반동으로 받을지")]
    [Range(0f, 300f)] public float abyssReboundRecoilPercentFromTotalDamage = 20f;

    [Tooltip("입힌 HP 피해량의 몇 %를 자신에게 보호막으로 전환할지")]
    [Range(0f, 500f)] public float selfShieldFromDamageDealtPercent = 100f;

    [Tooltip("연쇄 번개 첫 번째 추가 타격 계수")]
    [Range(0f, 300f)] public float chainLightningFirstJumpPowerPercent = 50f;
    [Tooltip("연쇄 번개 두 번째 추가 타격 계수")]
    [Range(0f, 300f)] public float chainLightningSecondJumpPowerPercent = 25f;

    [Tooltip("결투 지속 턴")]
    [Min(1)] public int blackArenaDuelDurationTurns = 2;

    [Header("Summon Settings")]
    public UnitDefinition summonUnitDefinition;
    public UnitViewDefinition summonUnitViewDefinition;
    [Tooltip("0이면 시전자 레벨을 사용합니다.")]
    [Min(0)] public int summonLevelOverride = 0;
    [Tooltip("소환 스킬 사용을 허용할 같은 팀 생존 개체 수 상한입니다.")]
    [Range(1, 4)] public int maxLivingAlliesForSummon = 3;

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

    [Tooltip("턴 시작 재생: 자신의 최대 체력 기준 회복 비율")]
    [Range(0f, 100f)] public float turnStartSelfHealMaxHpPercent = 5f;

    [Tooltip("생존 중 같은 편 전체 상태이상 저항 증가량")]
    [Range(0, 100)] public int teamStatusResistAuraPercent = 30;

    [Header("Boss Gimmick Settings")]
    [Tooltip("심판관/대사제처럼 서로 연동되는 보스 패시브에서 감시/부활 대상이 되는 UnitDefinition입니다.")]
    public UnitDefinition linkedBossUnitDefinition;

    [Tooltip("연동 보스 사망 시 심판관에게 부여할 전투 종료까지 DMG 증가율입니다.")]
    [Range(0, 300)] public int bossEnrageDmgPercent = 40;

    [Tooltip("연동 보스 사망 시 심판관에게 부여할 전투 종료까지 HIT 증가율입니다.")]
    [Range(0, 300)] public int bossEnrageHitPercent = 15;

    [Tooltip("연동 보스 사망 시 심판관에게 부여할 취약화 수치입니다. 실제 적용은 받는 피해 증가형으로 처리됩니다.")]
    [Range(0, 100)] public int bossEnrageIncomingDamageTakenPercent = 15;

    [Tooltip("연동 보스 사망 시 심판관이 최대 체력 기준 회복할 비율입니다.")]
    [Range(0f, 100f)] public float bossEnrageHealMaxHpPercent = 25f;

    [Tooltip("대사제 패시브로 심판관을 부활시킬 때 최대 체력 기준 회복 비율입니다.")]
    [Range(1f, 100f)] public float linkedBossReviveHpPercent = 30f;

    [Tooltip("대사제 패시브 발동 시 대사제 본인이 최대 체력 기준 회복할 비율입니다.")]
    [Range(0f, 100f)] public float bossReviverHealMaxHpPercent = 25f;

    [Tooltip("드래곤 보호 패시브에서 용아병으로 취급할 UnitDefinition입니다. 비워두면 Summon Settings의 summonUnitDefinition을 사용합니다.")]
    public UnitDefinition dragonSoldierUnitDefinition;

    [Tooltip("용아병이 살아 있을 때 드래곤의 최종 IDT를 최소 이 값으로 보정합니다.")]
    [Range(0, 100)] public int dragonSoldierProtectionIdtPercent = 99;

    [Header("Effects")]
    public List<BattleEffectBlock> effects = new List<BattleEffectBlock>();

    [Tooltip("체크 시 이 스킬은 해당 전투에서 1회 사용 후 다시 사용할 수 없습니다.")]
    public bool disableAfterUseInBattle = false;

    [Header("Visual Effects")]
    public GameObject castEffectPrefab; // 시전 시 사용자 위치에서 발생
    public GameObject hitEffectPrefab;  // 타격 시 대상 위치에서 발생

    [Header("Audio")]
    [Tooltip("현재는 자동 재생하지 않습니다. 추후 시전 시작 사운드가 필요할 때 사용합니다.")]
    public AudioClip useSfx;
    [Tooltip("피격/효과 적용 타이밍에 재생할 스킬 효과음입니다. 비워두면 재생하지 않습니다.")]
    public AudioClip hitSfx;

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

    public bool RequiresSelfStatusToUse()
    {
        return requiredSelfStatusToUse != StatusEffectType.None;
    }

    public bool BlocksUseWhenSelfHasStatus()
    {
        return blockedSelfStatusToUse != StatusEffectType.None;
    }

    public int GetInitialCooldownTurns()
    {
        return Mathf.Max(0, initialCooldownTurns);
    }

    public float GetRandomRepositionChancePercent()
    {
        return Mathf.Clamp(randomRepositionChancePercent, 0f, 100f);
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

    public int GetPrimaryHitCount()
    {
        return Mathf.Max(1, primaryHitCount);
    }

    public bool HasMultiplePrimaryHits()
    {
        return GetPrimaryHitCount() > 1;
    }

    public bool ShouldAlsoApplyToSelfWhenTargetingAlly()
    {
        return alsoApplyToSelfWhenTargetingAlly && targetTeam == SkillTargetTeam.Ally;
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

    public float GetForcedTargetMoveChancePercent()
    {
        return Mathf.Clamp(forcedTargetMoveChancePercent, 0f, 100f);
    }

    public bool HasShieldedTargetDamageBonus()
    {
        return shieldedTargetDamagePowerPercent > 0f;
    }

    public float GetShieldedTargetDamagePowerPercent()
    {
        return Mathf.Max(0f, shieldedTargetDamagePowerPercent);
    }

    public bool HasTargetStatusDamageBonus()
    {
        return targetStatusBonusType != StatusEffectType.None && targetStatusBonusPowerAddPercent > 0f;
    }

    public float GetTargetStatusBonusPowerAddPercent()
    {
        return Mathf.Max(0f, targetStatusBonusPowerAddPercent);
    }

    public bool HasMissingSecondaryTargetDamageBonus()
    {
        return missingSecondaryTargetDamagePowerPercent > 0f;
    }

    public float GetMissingSecondaryTargetDamagePowerPercent()
    {
        return Mathf.Max(0f, missingSecondaryTargetDamagePowerPercent);
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

    public float GetSelfShieldFromDamageDealtPercent()
    {
        return Mathf.Max(0f, selfShieldFromDamageDealtPercent);
    }

    public float GetChainLightningFirstJumpPowerPercent()
    {
        return Mathf.Max(0f, chainLightningFirstJumpPowerPercent);
    }

    public float GetChainLightningSecondJumpPowerPercent()
    {
        return Mathf.Max(0f, chainLightningSecondJumpPowerPercent);
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

    public float GetTurnStartSelfHealMaxHpPercent()
    {
        return Mathf.Clamp(turnStartSelfHealMaxHpPercent, 0f, 100f);
    }

    public int GetTeamStatusResistAuraPercent()
    {
        return Mathf.Clamp(teamStatusResistAuraPercent, 0, 100);
    }

    public UnitDefinition GetLinkedBossUnitDefinition()
    {
        return linkedBossUnitDefinition;
    }

    public int GetBossEnrageDmgPercent()
    {
        return Mathf.Max(0, bossEnrageDmgPercent);
    }

    public int GetBossEnrageHitPercent()
    {
        return Mathf.Max(0, bossEnrageHitPercent);
    }

    public int GetBossEnrageIncomingDamageTakenPercent()
    {
        return Mathf.Clamp(bossEnrageIncomingDamageTakenPercent, 0, 100);
    }

    public float GetBossEnrageHealMaxHpPercent()
    {
        return Mathf.Clamp(bossEnrageHealMaxHpPercent, 0f, 100f);
    }

    public float GetLinkedBossReviveHpPercent()
    {
        return Mathf.Clamp(linkedBossReviveHpPercent, 1f, 100f);
    }

    public float GetBossReviverHealMaxHpPercent()
    {
        return Mathf.Clamp(bossReviverHealMaxHpPercent, 0f, 100f);
    }

    public UnitDefinition GetDragonSoldierUnitDefinition()
    {
        return dragonSoldierUnitDefinition != null ? dragonSoldierUnitDefinition : summonUnitDefinition;
    }

    public int GetDragonSoldierProtectionIdtPercent()
    {
        return Mathf.Clamp(dragonSoldierProtectionIdtPercent, 0, 100);
    }
}