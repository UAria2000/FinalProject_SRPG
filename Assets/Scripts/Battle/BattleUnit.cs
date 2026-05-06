using System.Collections.Generic;
using UnityEngine;

public class BattleUnit
{
    private readonly PartyMemberData memberData;
    private readonly Dictionary<string, int> skillCooldowns = new Dictionary<string, int>();
    private readonly List<BattleStatusInstance> statuses = new List<BattleStatusInstance>();
    private readonly List<BattleTimedModifierInstance> timedModifiers = new List<BattleTimedModifierInstance>();
    private readonly HashSet<string> armedConditionalSkillKeys = new HashSet<string>();
    private readonly HashSet<string> disabledSkillKeys = new HashSet<string>();
    private readonly List<ItemDefinition> equippedItems = new List<ItemDefinition>();

    private SkillDefinition pendingPassiveSkill;
    private BattleUnit duelLockedTarget;
    private int persistentBattleDmgModifierPercent;

    private bool battleInfoLastWillRolled;
    private bool battleInfoHasLastWill;
    private string battleInfoLastWillText;

    public BattleUnit(PartyMemberData data, TeamType team)
    {
        memberData = data;
        Team = team;
        SlotIndex = data != null ? data.startSlotIndex : 0;
        CacheEquippedItems();

        if (memberData != null && memberData.persistentCurrentHP >= 0)
            CurrentHP = Mathf.Clamp(memberData.persistentCurrentHP, 0, MaxHP);
        else
            CurrentHP = MaxHP;

        CurrentShield = 0;
        endTurnGuardPercent = 0;
        ApplyEquipmentBattleStartEffects();
        InitializeBattleInfoLastWill();
    }

    public TeamType Team { get; private set; }
    public int SlotIndex { get; set; }

    public PartyMemberData MemberData { get { return memberData; } }
    public UnitDefinition Definition { get { return memberData != null ? memberData.unitDefinition : null; } }
    public UnitViewDefinition ViewDefinition { get { return memberData != null ? memberData.unitViewDefinition : null; } }

    public string Name { get { return memberData != null ? memberData.GetDisplayName() : "Unit"; } }
    public string Epitaph { get { return memberData != null ? memberData.fixedEpitaph : string.Empty; } }

    public bool HasBattleInfoLastWill
    {
        get { return battleInfoLastWillRolled && battleInfoHasLastWill && !string.IsNullOrWhiteSpace(battleInfoLastWillText); }
    }

    public string BattleInfoLastWillText
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(battleInfoLastWillText))
                return battleInfoLastWillText;

            return Epitaph;
        }
    }

    public Sprite SlotFaceSprite { get { return ViewDefinition != null ? ViewDefinition.GetSlotFaceSprite() : null; } }
    public Sprite BustPortraitSprite { get { return ViewDefinition != null ? ViewDefinition.GetBustPortraitSprite() : SlotFaceSprite; } }
    public Sprite BattleSprite { get { return ViewDefinition != null ? ViewDefinition.GetBattleSprite() : SlotFaceSprite; } }

    // Backward-compatible aliases for older UI code.
    public Sprite PortraitSprite { get { return SlotFaceSprite; } }
    public Sprite BodySprite { get { return BattleSprite; } }

    public int PromotionRank { get { return memberData != null ? Mathf.Max(0, memberData.promotionRank) : 0; } }
    public float PromotionBonusPercentPerRank { get { return memberData != null ? Mathf.Max(0f, memberData.promotionBonusPercentPerRank) : 0f; } }
    public float PromotionMultiplier { get { return LegionFormula.GetPromotionMultiplier(PromotionRank, PromotionBonusPercentPerRank); } }

    public int CurrentLevel { get { return memberData != null ? memberData.currentLevel : 1; } }
    public int OriginalLevel { get { return memberData != null ? memberData.originalLevel : 1; } }

    public int CurrentHP { get; private set; }
    public int CurrentShield { get; private set; }
    public bool IsDead { get { return CurrentHP <= 0; } }

    public BattleUnit DuelLockedTarget
    {
        get
        {
            if (!HasStatus(StatusEffectType.DuelArena))
                return null;

            if (duelLockedTarget == null || duelLockedTarget.IsDead)
                return null;

            return duelLockedTarget;
        }
    }

    public bool HasActiveDuelLock
    {
        get { return DuelLockedTarget != null; }
    }

    public bool IsPositionMovementLocked
    {
        get { return HasActiveDuelLock; }
    }

    public bool HasStealth
    {
        get { return HasStatus(StatusEffectType.Stealth); }
    }

    public bool BlocksDirectSingleTargeting
    {
        get { return HasStealth; }
    }

    public CharacterRangeType RangeType { get { return Definition != null ? Definition.rangeType : CharacterRangeType.Melee; } }
    public bool IsExchangeable { get { return memberData != null && memberData.isExchangeable; } }
    public bool IsNftUnit { get { return (memberData != null && (memberData.isNft || memberData.isExchangeable)) || (Definition != null && Definition.isNftUnit); } }

    public int BaseMaxHP { get { return Definition != null ? Definition.maxHP : 0; } }
    public int BaseDMG { get { return Definition != null ? Definition.dmg : 0; } }
    public int LevelGrowthMaxHP { get { return memberData != null ? Mathf.Max(0, memberData.levelGrowthMaxHp) : 0; } }
    public int LevelGrowthDMG { get { return memberData != null ? Mathf.Max(0, memberData.levelGrowthDmg) : 0; } }
    public int BaseSPD { get { return Definition != null ? Definition.spd : 0; } }
    public int BaseIDT { get { return Definition != null ? Definition.idt : 0; } }
    public float BaseHIT { get { return Definition != null ? Definition.hit : 0f; } }
    public float BaseAC { get { return Definition != null ? Definition.ac : 0f; } }
    public int BaseCRI { get { return Definition != null ? Definition.cri : 0; } }
    public int BaseCRD { get { return Definition != null ? Definition.crd : 0; } }
    public int BaseBurnResist { get { return Definition != null ? Definition.burnResist : 0; } }
    public int BaseBleedResist { get { return Definition != null ? Definition.bleedResist : 0; } }
    public int BaseStunResist { get { return Definition != null ? Definition.stunResist : 0; } }
    public int BaseFrostResist { get { return Definition != null ? Definition.frostResist : 0; } }
    public int BaseBlindResist { get { return Definition != null ? Definition.blindResist : 0; } }

    public int MaxHP { get { return ApplyPromotionToInt(Mathf.Max(1, BaseMaxHP + GetVariance().maxHpDelta + LevelGrowthMaxHP + EquipmentMaxHpBonus)); } }

    public int DMG
    {
        get
        {
            int baseValue = ApplyPromotionToInt(Mathf.Max(0, BaseDMG + GetVariance().dmgDelta + LevelGrowthDMG + EquipmentDmgBonus));
            int totalModifierPercent = GetTimedModifierMagnitude(StatModifierType.DMG) + persistentBattleDmgModifierPercent;
            if (totalModifierPercent == 0)
                return baseValue;

            return Mathf.Max(0, Mathf.RoundToInt(baseValue * (1f + totalModifierPercent / 100f)));
        }
    }

    public int SPD
    {
        get
        {
            int baseValue = ApplyPromotionToInt(Mathf.Max(0, BaseSPD + GetVariance().spdDelta + EquipmentSpdBonus));
            return ApplyPercentTimedModifierToInt(baseValue, StatModifierType.SPD, -FrostStatPenaltyPercent);
        }
    }

    public float HIT
    {
        get
        {
            float baseValue = ApplyPromotionToFloat(Mathf.Max(0f, BaseHIT + GetVariance().hitDeltaX10 + (EquipmentHitBonusX10 * 0.1f)));
            return ApplyPercentTimedModifierToFloat(baseValue, StatModifierType.HIT);
        }
    }

    public float AC
    {
        get
        {
            float baseValue = ApplyPromotionToFloat(Mathf.Max(0f, BaseAC + GetVariance().acDeltaX10 + (EquipmentAcBonusX10 * 0.1f)));
            return ApplyPercentTimedModifierToFloat(baseValue, StatModifierType.AC, -FrostStatPenaltyPercent);
        }
    }

    public int CRI
    {
        get
        {
            int baseValue = ApplyPromotionToInt(Mathf.Max(0, BaseCRI + GetVariance().criDelta + EquipmentCriBonus));
            return ApplyPercentTimedModifierToInt(baseValue, StatModifierType.CRI);
        }
    }

    public int CRD
    {
        get
        {
            int baseValue = ApplyPromotionToInt(Mathf.Max(0, BaseCRD + GetVariance().crdDelta + EquipmentCrdBonus));
            return ApplyPercentTimedModifierToInt(baseValue, StatModifierType.CRD);
        }
    }

    public int IDT
    {
        get
        {
            int baseValue = ApplyPromotionToInt(BaseIDT + GetVariance().idtDelta + EquipmentIdtBonus);
            int incomingDamageTakenPercent = GetTimedModifierMagnitude(StatModifierType.IncomingDamageTakenPercent);
            return baseValue - incomingDamageTakenPercent - BurnIdtPenaltyPercent;
        }
    }

    public int BurnResist { get { return ApplyPromotionToInt(Mathf.Max(0, BaseBurnResist + GetVariance().burnResistDelta)) + EquipmentAllResistBonus + EquipmentBurnResistBonus; } }
    public int BleedResist { get { return ApplyPromotionToInt(Mathf.Max(0, BaseBleedResist + GetVariance().bleedResistDelta)) + EquipmentAllResistBonus + EquipmentBleedResistBonus; } }
    public int StunResist { get { return ApplyPromotionToInt(Mathf.Max(0, BaseStunResist + GetVariance().stunResistDelta)) + EquipmentAllResistBonus + EquipmentStunResistBonus; } }
    public int FrostResist { get { return ApplyPromotionToInt(Mathf.Max(0, BaseFrostResist + GetVariance().frostResistDelta)) + EquipmentAllResistBonus + EquipmentFrostResistBonus; } }
    public int BlindResist { get { return ApplyPromotionToInt(Mathf.Max(0, BaseBlindResist + GetVariance().blindResistDelta)) + EquipmentAllResistBonus + EquipmentBlindResistBonus; } }

    public int BurnStackCount { get { return GetStatusStackCount(StatusEffectType.Burn); } }
    public int BleedStackCount { get { return GetStatusStackCount(StatusEffectType.Bleed); } }
    public int FrostStackCount { get { return GetStatusStackCount(StatusEffectType.Frost); } }
    public int BurnIdtPenaltyPercent { get { return Mathf.Max(0, BurnStackCount * BattleStatusUtility.BurnIdtPenaltyPercentPerStack); } }
    public int FrostStatPenaltyPercent { get { return Mathf.Max(0, FrostStackCount * BattleStatusUtility.FrostAcSpdPenaltyPercentPerStack); } }
    public int BlindFinalHitPenaltyPercent { get { return HasStatus(StatusEffectType.Blind) ? BattleStatusUtility.BlindFinalHitChancePenaltyPercent : 0; } }

    private int endTurnGuardPercent;
    public int EndTurnGuardPercent { get { return endTurnGuardPercent > 0 ? endTurnGuardPercent : 0; } }
    public bool HasEndTurnGuard { get { return endTurnGuardPercent > 0; } }


    private int EquipmentMaxHpBonus { get { return SumEquipmentIntBonus(EquipmentIntBonusKind.MaxHP); } }
    private int EquipmentDmgBonus { get { return SumEquipmentIntBonus(EquipmentIntBonusKind.DMG); } }
    private int EquipmentSpdBonus { get { return SumEquipmentIntBonus(EquipmentIntBonusKind.SPD); } }
    private int EquipmentIdtBonus { get { return SumEquipmentIntBonus(EquipmentIntBonusKind.IDT); } }
    private int EquipmentHitBonusX10 { get { return SumEquipmentIntBonus(EquipmentIntBonusKind.HITX10); } }
    private int EquipmentAcBonusX10 { get { return SumEquipmentIntBonus(EquipmentIntBonusKind.ACX10); } }
    private int EquipmentCriBonus { get { return SumEquipmentIntBonus(EquipmentIntBonusKind.CRI); } }
    private int EquipmentCrdBonus { get { return SumEquipmentIntBonus(EquipmentIntBonusKind.CRD); } }
    private int EquipmentAllResistBonus { get { return SumEquipmentIntBonus(EquipmentIntBonusKind.AllResist); } }
    private int EquipmentBurnResistBonus { get { return SumEquipmentIntBonus(EquipmentIntBonusKind.BurnResist); } }
    private int EquipmentBleedResistBonus { get { return SumEquipmentIntBonus(EquipmentIntBonusKind.BleedResist); } }
    private int EquipmentStunResistBonus { get { return SumEquipmentIntBonus(EquipmentIntBonusKind.StunResist); } }
    private int EquipmentFrostResistBonus { get { return SumEquipmentIntBonus(EquipmentIntBonusKind.FrostResist); } }
    private int EquipmentBlindResistBonus { get { return SumEquipmentIntBonus(EquipmentIntBonusKind.BlindResist); } }

    private enum EquipmentIntBonusKind
    {
        MaxHP,
        DMG,
        SPD,
        IDT,
        HITX10,
        ACX10,
        CRI,
        CRD,
        AllResist,
        BurnResist,
        BleedResist,
        StunResist,
        FrostResist,
        BlindResist,
    }

    private void CacheEquippedItems()
    {
        equippedItems.Clear();

        if (memberData == null)
            return;

        if (Team == TeamType.Ally)
        {
            WorldRunManager runManager = Object.FindFirstObjectByType<WorldRunManager>();
            if (runManager == null)
                return;

            AddEquippedItemIfValid(runManager.GetAssignedEquipmentItem(memberData, 0));
            AddEquippedItemIfValid(runManager.GetAssignedEquipmentItem(memberData, 1));
            return;
        }

        if (memberData.equippedItems == null)
            return;

        for (int i = 0; i < memberData.equippedItems.Count && equippedItems.Count < 2; i++)
            AddEquippedItemIfValid(memberData.equippedItems[i]);
    }

    private void AddEquippedItemIfValid(ItemDefinition item)
    {
        if (item == null)
            return;

        if (item.mainUICategory != MainUIItemCategory.Equipment)
            return;

        if (equippedItems.Count < 2)
            equippedItems.Add(item);
    }

    public ItemDefinition GetEquippedItemAt(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= equippedItems.Count)
            return null;

        return equippedItems[slotIndex];
    }

    private int SumEquipmentIntBonus(EquipmentIntBonusKind kind)
    {
        int total = 0;
        for (int i = 0; i < equippedItems.Count; i++)
        {
            ItemDefinition item = equippedItems[i];
            if (item == null)
                continue;

            switch (kind)
            {
                case EquipmentIntBonusKind.MaxHP:
                    total += item.equipmentMaxHpBonus;
                    break;
                case EquipmentIntBonusKind.DMG:
                    total += item.equipmentDmgBonus;
                    break;
                case EquipmentIntBonusKind.SPD:
                    total += item.equipmentSpdBonus;
                    break;
                case EquipmentIntBonusKind.IDT:
                    total += item.equipmentIdtBonus;
                    break;
                case EquipmentIntBonusKind.HITX10:
                    total += item.equipmentHitBonusX10;
                    break;
                case EquipmentIntBonusKind.ACX10:
                    total += item.equipmentAcBonusX10;
                    break;
                case EquipmentIntBonusKind.CRI:
                    total += item.equipmentCriBonus;
                    break;
                case EquipmentIntBonusKind.CRD:
                    total += item.equipmentCrdBonus;
                    break;
                case EquipmentIntBonusKind.AllResist:
                    total += item.equipmentAllResistBonus;
                    break;
                case EquipmentIntBonusKind.BurnResist:
                    total += item.equipmentBurnResistBonus;
                    break;
                case EquipmentIntBonusKind.BleedResist:
                    total += item.equipmentBleedResistBonus;
                    break;
                case EquipmentIntBonusKind.StunResist:
                    total += item.equipmentStunResistBonus;
                    break;
                case EquipmentIntBonusKind.FrostResist:
                    total += item.equipmentFrostResistBonus;
                    break;
                case EquipmentIntBonusKind.BlindResist:
                    total += item.equipmentBlindResistBonus;
                    break;
            }
        }

        return total;
    }

    private float GetEquipmentStartShieldPercentOfMaxHP()
    {
        float total = 0f;
        for (int i = 0; i < equippedItems.Count; i++)
        {
            ItemDefinition item = equippedItems[i];
            if (item == null)
                continue;

            total += Mathf.Max(0f, item.equipmentStartShieldPercentOfMaxHP);
        }

        return total;
    }

    private void ApplyEquipmentBattleStartEffects()
    {
        float shieldPercent = GetEquipmentStartShieldPercentOfMaxHP();
        if (shieldPercent <= 0f)
            return;

        int shieldAmount = Mathf.CeilToInt(MaxHP * shieldPercent * 0.01f);
        if (shieldAmount > 0)
            AddShield(shieldAmount);
    }

    private int ApplyPromotionToInt(int value)
    {
        return Mathf.Max(0, Mathf.RoundToInt(value * PromotionMultiplier));
    }

    private float ApplyPromotionToFloat(float value)
    {
        return Mathf.Max(0f, value * PromotionMultiplier);
    }

    public UnitInstanceStatVariance GetVariance()
    {
        return memberData != null && memberData.statVariance != null
            ? memberData.statVariance
            : new UnitInstanceStatVariance();
    }

    public SkillDefinition BasicAttack { get { return Definition != null ? Definition.basicAttack : null; } }

    private void InitializeBattleInfoLastWill()
    {
        battleInfoLastWillRolled = false;
        battleInfoHasLastWill = false;
        battleInfoLastWillText = string.Empty;

        if (Team != TeamType.Enemy)
            return;

        battleInfoLastWillRolled = true;

        if (!string.IsNullOrWhiteSpace(Epitaph))
        {
            battleInfoHasLastWill = true;
            battleInfoLastWillText = Epitaph;
            return;
        }

        UnitDefinition definition = Definition;
        if (definition == null)
            return;

        if (Random.Range(0f, 100f) > Mathf.Clamp(definition.lastWillChancePercent, 0f, 100f))
            return;

        string picked = definition.lastWillTextTable != null ? definition.lastWillTextTable.GetRandomText() : string.Empty;
        if (string.IsNullOrWhiteSpace(picked))
            return;

        battleInfoHasLastWill = true;
        battleInfoLastWillText = picked;
    }


    public SkillDefinition GetActionSkillAt(int slotIndex)
    {
        if (slotIndex == 0)
            return BasicAttack;

        if (memberData == null || memberData.learnedSkills == null)
            return null;

        int learnedIndex = slotIndex - 1;
        if (learnedIndex < 0 || learnedIndex >= memberData.learnedSkills.Count)
            return null;

        return memberData.learnedSkills[learnedIndex];
    }

    public int GetActionSkillSlotCount()
    {
        return 4;
    }

    public bool CanUseSkill(SkillDefinition skill)
    {
        if (skill == null || IsDead)
            return false;

        if (skill.castType == SkillCastType.Passive)
            return false;

        if (IsSkillDisabled(skill))
            return false;

        if (skill.activeGimmick == ActiveSkillGimmick.DelayedReinforcement && !IsConditionalSkillArmed(skill))
            return false;

        if (!skill.CanBeUsedFromSlot(SlotIndex))
            return false;

        return GetRemainingCooldown(skill) <= 0;
    }

    public bool TryGetPassiveSkillByGimmick(PassiveSkillGimmick gimmick, out SkillDefinition skill)
    {
        skill = null;

        if (gimmick == PassiveSkillGimmick.None || memberData == null || memberData.learnedSkills == null)
            return false;

        for (int i = 0; i < memberData.learnedSkills.Count; i++)
        {
            SkillDefinition candidate = memberData.learnedSkills[i];
            if (candidate == null)
                continue;

            if (candidate.passiveGimmick != gimmick)
                continue;

            skill = candidate;
            return true;
        }

        return false;
    }

    public bool TryGetActiveSkillByGimmick(ActiveSkillGimmick gimmick, out SkillDefinition skill)
    {
        skill = null;

        if (gimmick == ActiveSkillGimmick.None || memberData == null || memberData.learnedSkills == null)
            return false;

        for (int i = 0; i < memberData.learnedSkills.Count; i++)
        {
            SkillDefinition candidate = memberData.learnedSkills[i];
            if (candidate == null)
                continue;

            if (candidate.castType != SkillCastType.Active)
                continue;

            if (candidate.activeGimmick != gimmick)
                continue;

            skill = candidate;
            return true;
        }

        return false;
    }

    public bool IsConditionalSkillArmed(SkillDefinition skill)
    {
        if (skill == null)
            return false;

        return armedConditionalSkillKeys.Contains(GetSkillKey(skill));
    }

    public bool TryArmConditionalSkill(SkillDefinition skill)
    {
        if (skill == null || skill.castType != SkillCastType.Active)
            return false;

        if (IsSkillDisabled(skill))
            return false;

        return armedConditionalSkillKeys.Add(GetSkillKey(skill));
    }

    public void ConsumeConditionalSkillArm(SkillDefinition skill)
    {
        if (skill == null)
            return;

        armedConditionalSkillKeys.Remove(GetSkillKey(skill));
    }

    public bool IsSkillDisabled(SkillDefinition skill)
    {
        if (skill == null)
            return false;

        return disabledSkillKeys.Contains(GetSkillKey(skill));
    }

    public void DisableSkill(SkillDefinition skill)
    {
        if (skill == null)
            return;

        disabledSkillKeys.Add(GetSkillKey(skill));
        armedConditionalSkillKeys.Remove(GetSkillKey(skill));
    }

    public bool HasPendingPassiveSkill
    {
        get { return pendingPassiveSkill != null; }
    }

    public SkillDefinition PeekPendingPassiveSkill()
    {
        return pendingPassiveSkill;
    }

    public bool TryArmPendingPassiveSkill(SkillDefinition passiveSkill)
    {
        if (passiveSkill == null || passiveSkill.castType != SkillCastType.Passive)
            return false;

        if (pendingPassiveSkill != null)
            return false;

        pendingPassiveSkill = passiveSkill;
        return true;
    }

    public SkillDefinition ConsumePendingPassiveSkill()
    {
        SkillDefinition consumed = pendingPassiveSkill;
        pendingPassiveSkill = null;
        return consumed;
    }

    public int GetRemainingCooldown(SkillDefinition skill)
    {
        if (skill == null)
            return 0;

        string key = GetSkillKey(skill);
        int value;
        if (skillCooldowns.TryGetValue(key, out value))
            return Mathf.Max(0, value);

        return 0;
    }

    public void ConsumeSkillCooldown(SkillDefinition skill)
    {
        if (skill == null)
            return;

        string key = GetSkillKey(skill);
        int configuredCooldown = Mathf.Max(0, skill.cooldownTurns);

        if (configuredCooldown <= 0)
        {
            skillCooldowns.Remove(key);
            return;
        }

        skillCooldowns[key] = configuredCooldown + 1;
    }

    public void OnOwnTurnStart()
    {
        ClearEndTurnGuard();

        List<string> keys = new List<string>(skillCooldowns.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            string key = keys[i];
            if (skillCooldowns[key] > 0)
                skillCooldowns[key]--;

            if (skillCooldowns[key] <= 0)
                skillCooldowns.Remove(key);
        }

        for (int i = timedModifiers.Count - 1; i >= 0; i--)
        {
            timedModifiers[i].remainingTurns--;
            if (timedModifiers[i].remainingTurns <= 0)
                timedModifiers.RemoveAt(i);
        }
    }

    public int ApplyDamage(int amount)
    {
        amount = Mathf.Max(0, amount);

        int shieldAbsorb = Mathf.Min(CurrentShield, amount);
        CurrentShield -= shieldAbsorb;
        int hpDamage = amount - shieldAbsorb;

        CurrentHP = Mathf.Max(0, CurrentHP - hpDamage);
        return hpDamage;
    }

    public int ApplyDirectHpDamage(int amount)
    {
        amount = Mathf.Max(0, amount);
        int before = CurrentHP;
        CurrentHP = Mathf.Max(0, CurrentHP - amount);
        return before - CurrentHP;
    }

    public int ApplyIncomingAttackDamageReduction(int amount)
    {
        amount = Mathf.Max(0, amount);

        if (HasEndTurnGuard)
        {
            float guardMultiplier = 1f - (endTurnGuardPercent / 100f);
            amount = Mathf.Max(0, Mathf.RoundToInt(amount * guardMultiplier));
        }

        int idt = IDT;
        if (idt != 0)
        {
            float multiplier = 1f - (idt / 100f);
            amount = Mathf.Max(0, Mathf.RoundToInt(amount * multiplier));
        }

        return amount;
    }

    public void AddPersistentBattleDmgModifierPercent(int amount)
    {
        if (amount == 0)
            return;

        persistentBattleDmgModifierPercent += amount;
    }

    public void ApplyEndTurnGuard(int guardPercent)
    {
        endTurnGuardPercent = Mathf.Clamp(guardPercent, 0, 100);
    }

    public void ClearEndTurnGuard()
    {
        endTurnGuardPercent = 0;
    }

    public bool TryApplyTimedModifier(StatModifierType statType, int magnitude, int duration)
    {
        if (statType == StatModifierType.None || duration <= 0 || magnitude == 0)
            return false;

        for (int i = 0; i < timedModifiers.Count; i++)
        {
            BattleTimedModifierInstance existing = timedModifiers[i];
            if (existing.statModifierType != statType)
                continue;

            int newAbs = Mathf.Abs(magnitude);
            int oldAbs = Mathf.Abs(existing.magnitude);

            if (newAbs > oldAbs)
            {
                existing.magnitude = magnitude;
                existing.remainingTurns = duration;
                return true;
            }

            if (newAbs == oldAbs)
            {
                existing.magnitude = magnitude;
                existing.remainingTurns = Mathf.Max(existing.remainingTurns, duration);
                return true;
            }

            return false;
        }

        BattleTimedModifierInstance instance = new BattleTimedModifierInstance();
        instance.statModifierType = statType;
        instance.magnitude = magnitude;
        instance.remainingTurns = duration;
        timedModifiers.Add(instance);
        return true;
    }

    private int ApplyPercentTimedModifierToInt(int baseValue, StatModifierType statType)
    {
        return ApplyPercentTimedModifierToInt(baseValue, statType, 0);
    }

    private int ApplyPercentTimedModifierToInt(int baseValue, StatModifierType statType, int extraModifierPercent)
    {
        int modifierPercent = GetTimedModifierMagnitude(statType) + extraModifierPercent;
        if (modifierPercent == 0)
            return baseValue;

        return Mathf.Max(0, Mathf.RoundToInt(baseValue * (1f + modifierPercent / 100f)));
    }

    private float ApplyPercentTimedModifierToFloat(float baseValue, StatModifierType statType)
    {
        return ApplyPercentTimedModifierToFloat(baseValue, statType, 0);
    }

    private float ApplyPercentTimedModifierToFloat(float baseValue, StatModifierType statType, int extraModifierPercent)
    {
        int modifierPercent = GetTimedModifierMagnitude(statType) + extraModifierPercent;
        if (modifierPercent == 0)
            return baseValue;

        return Mathf.Max(0f, baseValue * (1f + modifierPercent / 100f));
    }

    public int GetTimedModifierMagnitude(StatModifierType statType)
    {
        for (int i = 0; i < timedModifiers.Count; i++)
        {
            if (timedModifiers[i].statModifierType == statType)
                return timedModifiers[i].magnitude;
        }

        return 0;
    }

    public int GetTimedModifierRemainingTurns(StatModifierType statType)
    {
        for (int i = 0; i < timedModifiers.Count; i++)
        {
            if (timedModifiers[i].statModifierType == statType)
                return timedModifiers[i].remainingTurns;
        }

        return 0;
    }

    public bool HasTimedModifier(StatModifierType statType)
    {
        return GetTimedModifierMagnitude(statType) != 0;
    }

    public bool HasPierceBackOneBuff
    {
        get { return GetTimedModifierMagnitude(StatModifierType.PierceBackOne) > 0; }
    }

    public int Heal(int amount)
    {
        amount = Mathf.Max(0, amount);
        int before = CurrentHP;
        CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
        return CurrentHP - before;
    }

    public void SavePersistentHPToMemberData()
    {
        if (memberData == null)
            return;

        memberData.persistentCurrentHP = Mathf.Clamp(CurrentHP, 0, MaxHP);
    }

    public void ResetPersistentHPToFull()
    {
        CurrentHP = MaxHP;

        if (memberData != null)
            memberData.persistentCurrentHP = MaxHP;
    }

    public int AddShield(int amount)
    {
        amount = Mathf.Max(0, amount);
        CurrentShield += amount;
        return amount;
    }

    public void ApplyDuelLock(BattleUnit opponent, int duration)
    {
        duelLockedTarget = opponent;
        ApplyStatus(StatusEffectType.DuelArena, duration);
    }

    public void ClearDuelLock()
    {
        duelLockedTarget = null;
        RemoveStatus(StatusEffectType.DuelArena);
    }

    private BattleStatusInstance FindStatusInstance(StatusEffectType statusType)
    {
        statusType = BattleStatusUtility.Normalize(statusType);
        for (int i = 0; i < statuses.Count; i++)
        {
            if (BattleStatusUtility.Normalize(statuses[i].statusType) == statusType)
                return statuses[i];
        }

        return null;
    }

    public IReadOnlyList<BattleStatusInstance> Statuses
    {
        get { return statuses; }
    }

    public int GetStatusStackCount(StatusEffectType statusType)
    {
        statusType = BattleStatusUtility.Normalize(statusType);
        BattleStatusInstance instance = FindStatusInstance(statusType);
        if (instance == null)
            return 0;

        if (BattleStatusUtility.IsStackingAilment(statusType))
            return Mathf.Max(0, instance.remainingTurns);

        return instance.remainingTurns > 0 ? 1 : 0;
    }

    public BattleTurnStartStatusResult ResolveTurnStartStatuses()
    {
        BattleTurnStartStatusResult result = new BattleTurnStartStatusResult();
        if (IsDead)
            return result;

        int hpAtTurnStart = CurrentHP;
        int bleedStacks = GetStatusStackCount(StatusEffectType.Bleed);
        if (bleedStacks > 0)
        {
            int bleedDamagePerStack = Mathf.Max(1, Mathf.CeilToInt(hpAtTurnStart * 0.05f));
            int totalBleedDamage = bleedDamagePerStack * bleedStacks;
            result.bleedDamage = ApplyDirectHpDamage(totalBleedDamage);
        }

        if (!IsDead && HasStatus(StatusEffectType.Stun))
            result.wasStunned = true;

        for (int i = statuses.Count - 1; i >= 0; i--)
        {
            StatusEffectType normalizedType = BattleStatusUtility.Normalize(statuses[i].statusType);
            statuses[i].statusType = normalizedType;
            statuses[i].remainingTurns--;

            if (statuses[i].remainingTurns <= 0)
            {
                result.expiredStatuses.Add(normalizedType);
                statuses.RemoveAt(i);

                if (normalizedType == StatusEffectType.DuelArena)
                    duelLockedTarget = null;
            }
        }

        return result;
    }

    public void ApplyStatus(StatusEffectType statusType, int duration)
    {
        statusType = BattleStatusUtility.Normalize(statusType);
        if (statusType == StatusEffectType.None || duration <= 0)
            return;

        BattleStatusInstance existing = FindStatusInstance(statusType);
        if (existing != null)
        {
            existing.statusType = statusType;
            if (BattleStatusUtility.IsStackingAilment(statusType))
                existing.remainingTurns = BattleStatusUtility.ClampStack(existing.remainingTurns + duration);
            else
            {
                int effectiveDuration = statusType == StatusEffectType.Blind ? duration + 1 : duration;
                existing.remainingTurns = Mathf.Max(existing.remainingTurns, effectiveDuration);
            }

            return;
        }

        BattleStatusInstance instance = new BattleStatusInstance();
        instance.statusType = statusType;
        instance.remainingTurns = BattleStatusUtility.IsStackingAilment(statusType)
            ? BattleStatusUtility.ClampStack(duration)
            : Mathf.Max(1, statusType == StatusEffectType.Blind ? duration + 1 : duration);
        statuses.Add(instance);
    }

    public void RemoveStatus(StatusEffectType statusType)
    {
        statusType = BattleStatusUtility.Normalize(statusType);
        for (int i = statuses.Count - 1; i >= 0; i--)
        {
            if (BattleStatusUtility.Normalize(statuses[i].statusType) == statusType)
                statuses.RemoveAt(i);
        }

        if (statusType == StatusEffectType.DuelArena)
            duelLockedTarget = null;
    }

    public bool HasStatus(StatusEffectType statusType)
    {
        statusType = BattleStatusUtility.Normalize(statusType);
        for (int i = 0; i < statuses.Count; i++)
        {
            if (BattleStatusUtility.Normalize(statuses[i].statusType) == statusType && statuses[i].remainingTurns > 0)
                return true;
        }

        return false;
    }

    public int GetResistance(StatusEffectType statusType)
    {
        return BattleStatusUtility.GetResistance(this, statusType);
    }

    private string GetSkillKey(SkillDefinition skill)
    {
        if (!string.IsNullOrEmpty(skill.skillId))
            return skill.skillId;

        return skill.name;
    }
}

public class BattleTurnStartStatusResult
{
    public int bleedDamage;
    public bool wasStunned;
    public readonly List<StatusEffectType> expiredStatuses = new List<StatusEffectType>();
}