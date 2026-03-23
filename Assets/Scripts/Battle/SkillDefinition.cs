using System.Collections.Generic;
using UnityEngine;

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

    [Header("Secondary Hit (Optional)")]
    public SecondaryTargetRule secondaryTargetRule = SecondaryTargetRule.None;
    [Tooltip("보조 타격 명중계수(%)")]
    [Range(0f, 300f)] public float secondaryAccuracyCoefficientPercent = 100f;
    [Tooltip("보조 타격 DMG 계수(%)")]
    [Min(0f)] public float secondaryDamagePercent = 0f;
    [Tooltip("보조 타격에도 비데미지 부가효과를 적용할지 여부")]
    public bool secondaryApplyNonDamageEffects = false;

    [Header("Effects")]
    public List<BattleEffectBlock> effects = new List<BattleEffectBlock>();

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

    public int GetPrimaryPowerPercent()
    {
        if (effects == null) return 100;
        for (int i = 0; i < effects.Count; i++)
        {
            BattleEffectBlock block = effects[i];
            if (block != null && block.kind == BattleEffectKind.Damage)
                return Mathf.RoundToInt(block.powerPercent);
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
}
