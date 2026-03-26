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

    private SkillDefinition pendingPassiveSkill;

    public BattleUnit(PartyMemberData data, TeamType team)
    {
        memberData = data;
        Team = team;
        SlotIndex = data != null ? data.startSlotIndex : 0;
        CurrentHP = MaxHP;
        CurrentShield = 0;
        endTurnGuardPercent = 0;
    }

    public TeamType Team { get; private set; }
    public int SlotIndex { get; set; }

    public PartyMemberData MemberData { get { return memberData; } }
    public UnitDefinition Definition { get { return memberData != null ? memberData.unitDefinition : null; } }
    public UnitViewDefinition ViewDefinition { get { return memberData != null ? memberData.unitViewDefinition : null; } }

    public string Name { get { return memberData != null ? memberData.GetDisplayName() : "Unit"; } }
    public string Epitaph { get { return memberData != null ? memberData.fixedEpitaph : string.Empty; } }

    public Sprite PortraitSprite { get { return ViewDefinition != null ? ViewDefinition.portrait : null; } }
    public Sprite BodySprite { get { return ViewDefinition != null ? ViewDefinition.bodySprite : null; } }

    public int CurrentLevel { get { return memberData != null ? memberData.currentLevel : 1; } }
    public int OriginalLevel { get { return memberData != null ? memberData.originalLevel : 1; } }

    public int CurrentHP { get; private set; }
    public int CurrentShield { get; private set; }
    public bool IsDead { get { return CurrentHP <= 0; } }

    public CharacterRangeType RangeType { get { return Definition != null ? Definition.rangeType : CharacterRangeType.Melee; } }

    public int BaseMaxHP { get { return Definition != null ? Definition.maxHP : 0; } }
    public int BaseDMG { get { return Definition != null ? Definition.dmg : 0; } }
    public int BaseSPD { get { return Definition != null ? Definition.spd : 0; } }
    public float BaseHIT { get { return Definition != null ? Definition.hit : 0f; } }
    public float BaseAC { get { return Definition != null ? Definition.ac : 0f; } }
    public int BaseCRI { get { return Definition != null ? Definition.cri : 0; } }
    public int BaseCRD { get { return Definition != null ? Definition.crd : 0; } }
    public int BasePoisonResist { get { return Definition != null ? Definition.poisonResist : 0; } }
    public int BaseBleedResist { get { return Definition != null ? Definition.bleedResist : 0; } }
    public int BaseStunResist { get { return Definition != null ? Definition.stunResist : 0; } }

    public int MaxHP { get { return Mathf.Max(1, BaseMaxHP + GetVariance().maxHpDelta); } }
    public int DMG { get { return Mathf.Max(0, BaseDMG + GetVariance().dmgDelta); } }
    public int SPD { get { return Mathf.Max(0, BaseSPD + GetVariance().spdDelta); } }
    public float HIT { get { return Mathf.Max(0f, BaseHIT + GetVariance().hitDeltaX10); } }
    public float AC { get { return Mathf.Max(0f, BaseAC + GetVariance().acDeltaX10); } }
    public int CRI { get { return Mathf.Max(0, BaseCRI + GetVariance().criDelta); } }
    public int CRD { get { return Mathf.Max(0, BaseCRD + GetVariance().crdDelta); } }

    public int PoisonResist { get { return BasePoisonResist + GetVariance().poisonResistDelta; } }
    public int BleedResist { get { return BaseBleedResist + GetVariance().bleedResistDelta; } }
    public int StunResist { get { return BaseStunResist + GetVariance().stunResistDelta; } }

    private int endTurnGuardPercent;
    public int EndTurnGuardPercent { get { return endTurnGuardPercent; } }
    public bool HasEndTurnGuard { get { return endTurnGuardPercent > 0; } }

    public UnitInstanceStatVariance GetVariance()
    {
        return memberData != null && memberData.statVariance != null
            ? memberData.statVariance
            : new UnitInstanceStatVariance();
    }

    public SkillDefinition BasicAttack { get { return Definition != null ? Definition.basicAttack : null; } }

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

            if (candidate.castType != SkillCastType.Passive)
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
        if (skill == null) return 0;
        string key = GetSkillKey(skill);
        int value;
        if (skillCooldowns.TryGetValue(key, out value))
            return Mathf.Max(0, value);
        return 0;
    }

    public void ConsumeSkillCooldown(SkillDefinition skill)
    {
        if (skill == null) return;
        string key = GetSkillKey(skill);
        skillCooldowns[key] = Mathf.Max(0, skill.cooldownTurns);
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

    public int ApplyIncomingAttackDamageReduction(int amount)
    {
        amount = Mathf.Max(0, amount);

        if (HasEndTurnGuard)
        {
            float guardMultiplier = 1f - (endTurnGuardPercent / 100f);
            amount = Mathf.Max(0, Mathf.RoundToInt(amount * guardMultiplier));
        }

        int takenModifierPercent = GetTimedModifierMagnitude(StatModifierType.IncomingDamageTakenPercent);
        if (takenModifierPercent != 0)
        {
            float multiplier = 1f + (takenModifierPercent / 100f);
            amount = Mathf.Max(0, Mathf.RoundToInt(amount * multiplier));
        }

        return amount;
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

    public int AddShield(int amount)
    {
        amount = Mathf.Max(0, amount);
        CurrentShield += amount;
        return amount;
    }


    public BattleTurnStartStatusResult ResolveTurnStartStatuses()
    {
        BattleTurnStartStatusResult result = new BattleTurnStartStatusResult();
        if (IsDead)
            return result;

        int hpAtTurnStart = CurrentHP;

        if (HasStatus(StatusEffectType.Poison))
        {
            int poisonDamage = Mathf.Max(1, Mathf.FloorToInt(MaxHP * 0.03f));
            result.poisonDamage = ApplyDamage(poisonDamage);
        }

        if (!IsDead && HasStatus(StatusEffectType.Bleed))
        {
            int bleedDamage = Mathf.Max(1, Mathf.FloorToInt(hpAtTurnStart * 0.05f));
            result.bleedDamage = ApplyDamage(bleedDamage);
        }

        if (!IsDead && HasStatus(StatusEffectType.Stun))
            result.wasStunned = true;

        for (int i = statuses.Count - 1; i >= 0; i--)
        {
            statuses[i].remainingTurns--;
            if (statuses[i].remainingTurns <= 0)
            {
                result.expiredStatuses.Add(statuses[i].statusType);
                statuses.RemoveAt(i);
            }
        }

        return result;
    }

    public void ApplyStatus(StatusEffectType statusType, int duration)
    {
        if (statusType == StatusEffectType.None || duration <= 0)
            return;

        for (int i = 0; i < statuses.Count; i++)
        {
            if (statuses[i].statusType == statusType)
            {
                statuses[i].remainingTurns = Mathf.Max(statuses[i].remainingTurns, duration);
                return;
            }
        }

        BattleStatusInstance instance = new BattleStatusInstance();
        instance.statusType = statusType;
        instance.remainingTurns = duration;
        statuses.Add(instance);
    }

    public void RemoveStatus(StatusEffectType statusType)
    {
        for (int i = statuses.Count - 1; i >= 0; i--)
        {
            if (statuses[i].statusType == statusType)
                statuses.RemoveAt(i);
        }
    }

    public bool HasStatus(StatusEffectType statusType)
    {
        for (int i = 0; i < statuses.Count; i++)
            if (statuses[i].statusType == statusType)
                return true;
        return false;
    }

    public int GetResistance(StatusEffectType statusType)
    {
        switch (statusType)
        {
            case StatusEffectType.Poison: return PoisonResist;
            case StatusEffectType.Bleed: return BleedResist;
            case StatusEffectType.Stun: return StunResist;
        }
        return 0;
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
    public int poisonDamage;
    public int bleedDamage;
    public bool wasStunned;
    public readonly List<StatusEffectType> expiredStatuses = new List<StatusEffectType>();
}
