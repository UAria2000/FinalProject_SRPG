using System.Collections.Generic;
using UnityEngine;

public class BattleUnit
{
    public UnitDefinition Definition { get; private set; }
    public UnitViewDefinition ViewDefinition { get; private set; }
    public TeamType Team { get; private set; }

    public int CurrentHP { get; private set; }
    public int SlotIndex { get; set; }

    public bool IsDead => CurrentHP <= 0;

    // 실제 장착 스킬(개체 기준)
    private readonly List<SkillDefinition> equippedSkills = new List<SkillDefinition>();

    // 스킬별 남은 쿨타임
    private readonly Dictionary<string, int> skillCooldowns = new Dictionary<string, int>();

    public BattleUnit(UnitDefinition definition, UnitViewDefinition viewDefinition, TeamType team, int slotIndex)
    {
        Definition = definition;
        ViewDefinition = viewDefinition;
        Team = team;
        SlotIndex = slotIndex;
        CurrentHP = definition != null ? definition.maxHP : 0;

        InitializeSkills();
    }

    public string Name
    {
        get
        {
            if (ViewDefinition != null && !string.IsNullOrWhiteSpace(ViewDefinition.displayNameOverride))
                return ViewDefinition.displayNameOverride;

            return Definition != null ? Definition.unitName : "Unknown";
        }
    }

    public CharacterRangeType RangeType => Definition != null ? Definition.rangeType : CharacterRangeType.Melee;

    public int Level => Definition != null ? Definition.level : 1;
    public int Exp => Definition != null ? Definition.exp : 0;

    public int MaxHP => Definition != null ? Definition.maxHP : 0;
    public int DMG => Definition != null ? Definition.dmg : 0;
    public int SPD => Definition != null ? Definition.spd : 0;

    public float HIT => Definition != null ? Definition.hit : 0f;
    public float AC => Definition != null ? Definition.ac : 0f;
    public float CRI => Definition != null ? Definition.cri : 0f;
    public float CRD => Definition != null ? Definition.crd : 0f;

    public float PoisonResist => Definition != null ? Definition.poisonResist : 0f;
    public float BleedResist => Definition != null ? Definition.bleedResist : 0f;
    public float StunResist => Definition != null ? Definition.stunResist : 0f;

    public BattleUnitView ViewPrefab => ViewDefinition != null ? ViewDefinition.unitViewPrefab : null;
    public Sprite PortraitSprite => ViewDefinition != null ? ViewDefinition.portraitSprite : null;
    public Sprite BodySprite => ViewDefinition != null ? ViewDefinition.bodySprite : null;

    public IReadOnlyList<SkillDefinition> EquippedSkills => equippedSkills;

    public void TakeDamage(int amount)
    {
        CurrentHP = Mathf.Max(0, CurrentHP - amount);
    }

    public void Heal(int amount)
    {
        CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
    }

    private void InitializeSkills()
    {
        equippedSkills.Clear();
        skillCooldowns.Clear();

        if (Definition == null || Definition.defaultSkills == null)
            return;

        int maxSkillCount = Team == TeamType.Ally ? 3 : 2;
        int count = Mathf.Min(Definition.defaultSkills.Count, maxSkillCount);

        HashSet<string> usedSkillIds = new HashSet<string>();

        for (int i = 0; i < count; i++)
        {
            SkillDefinition skill = Definition.defaultSkills[i];
            if (skill == null)
                continue;

            string key = GetSkillKey(skill);
            if (usedSkillIds.Contains(key))
                continue;

            equippedSkills.Add(skill);
            usedSkillIds.Add(key);
            skillCooldowns[key] = 0;
        }
    }

    public SkillDefinition GetSkillAt(int index)
    {
        if (index < 0 || index >= equippedSkills.Count)
            return null;

        return equippedSkills[index];
    }

    public int GetMaxSkillSlotCount()
    {
        return Team == TeamType.Ally ? 3 : 2;
    }

    public bool HasSkillInSlot(int index)
    {
        return GetSkillAt(index) != null;
    }

    public bool IsSkillOnCooldown(SkillDefinition skill)
    {
        if (skill == null)
            return false;

        string key = GetSkillKey(skill);
        return skillCooldowns.TryGetValue(key, out int remaining) && remaining > 0;
    }

    public int GetRemainingCooldown(SkillDefinition skill)
    {
        if (skill == null)
            return 0;

        string key = GetSkillKey(skill);
        if (skillCooldowns.TryGetValue(key, out int remaining))
            return remaining;

        return 0;
    }

    public bool CanUseSkill(SkillDefinition skill)
    {
        if (skill == null)
            return false;

        if (IsDead)
            return false;

        if (!equippedSkills.Contains(skill))
            return false;

        if (IsSkillOnCooldown(skill))
            return false;

        if (!skill.CanBeUsedByUnit(this))
            return false;

        return true;
    }

    public void ConsumeSkillCooldown(SkillDefinition skill)
    {
        if (skill == null)
            return;

        string key = GetSkillKey(skill);
        skillCooldowns[key] = Mathf.Max(0, skill.cooldownTurns + 1);
    }

    // 자기 턴 시작 시 호출
    public void OnTurnStart()
    {
        List<string> keys = new List<string>(skillCooldowns.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            string key = keys[i];
            if (skillCooldowns[key] > 0)
                skillCooldowns[key]--;
        }
    }

    private string GetSkillKey(SkillDefinition skill)
    {
        if (skill == null)
            return "";

        if (!string.IsNullOrEmpty(skill.skillId))
            return skill.skillId;

        return skill.name;
    }

    public override string ToString()
    {
        return $"{Name}({Team}) [Slot:{SlotIndex + 1}, HP:{CurrentHP}/{MaxHP}]";
    }
}