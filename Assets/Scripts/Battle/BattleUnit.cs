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

    private readonly int maxHP;
    private readonly int dmg;
    private readonly int spd;
    private readonly float hit;
    private readonly float ac;
    private readonly float cri;
    private readonly float crd;
    private readonly float poisonResist;
    private readonly float bleedResist;
    private readonly float stunResist;

    private readonly string runtimeName;
    private readonly string epitaph;

    private readonly List<SkillDefinition> equippedSkills = new List<SkillDefinition>();
    private readonly Dictionary<string, int> skillCooldowns = new Dictionary<string, int>();

    public BattleUnit(UnitDefinition definition, UnitViewDefinition viewDefinition, TeamType team, int slotIndex)
    {
        Definition = definition;
        ViewDefinition = viewDefinition;
        Team = team;
        SlotIndex = slotIndex;

        runtimeName = definition != null ? definition.unitName : "Unknown";
        epitaph = "";

        maxHP = definition != null ? definition.maxHP : 0;
        dmg = definition != null ? definition.dmg : 0;
        spd = definition != null ? definition.spd : 0;
        hit = definition != null ? definition.hit : 0f;
        ac = definition != null ? definition.ac : 0f;
        cri = definition != null ? definition.cri : 0f;
        crd = definition != null ? definition.crd : 0f;
        poisonResist = definition != null ? definition.poisonResist : 0f;
        bleedResist = definition != null ? definition.bleedResist : 0f;
        stunResist = definition != null ? definition.stunResist : 0f;

        CurrentHP = maxHP;
        InitializeSkills(definition != null ? definition.defaultSkills : null);
    }

    public BattleUnit(PartyMemberData member, TeamType team)
    {
        Definition = member != null ? member.unitDefinition : null;
        ViewDefinition = member != null ? member.unitViewDefinition : null;
        Team = team;
        SlotIndex = member != null ? member.startSlotIndex : 0;

        string baseName = Definition != null ? Definition.unitName : "Unknown";
        runtimeName = member != null && !string.IsNullOrWhiteSpace(member.instanceDisplayNameOverride)
            ? member.instanceDisplayNameOverride
            : baseName;
        epitaph = member != null ? member.fixedEpitaph : "";

        bool useOverride = member != null && member.useInstanceStatOverride;

        maxHP = useOverride ? member.maxHPOverride : (Definition != null ? Definition.maxHP : 0);
        dmg = useOverride ? member.dmgOverride : (Definition != null ? Definition.dmg : 0);
        spd = useOverride ? member.spdOverride : (Definition != null ? Definition.spd : 0);
        hit = useOverride ? member.hitOverride : (Definition != null ? Definition.hit : 0f);
        ac = useOverride ? member.acOverride : (Definition != null ? Definition.ac : 0f);
        cri = useOverride ? member.criOverride : (Definition != null ? Definition.cri : 0f);
        crd = useOverride ? member.crdOverride : (Definition != null ? Definition.crd : 0f);
        poisonResist = useOverride ? member.poisonResistOverride : (Definition != null ? Definition.poisonResist : 0f);
        bleedResist = useOverride ? member.bleedResistOverride : (Definition != null ? Definition.bleedResist : 0f);
        stunResist = useOverride ? member.stunResistOverride : (Definition != null ? Definition.stunResist : 0f);

        CurrentHP = maxHP;

        List<SkillDefinition> sourceSkills = null;
        if (member != null && member.equippedSkills != null && member.equippedSkills.Count > 0)
            sourceSkills = member.equippedSkills;
        else if (Definition != null)
            sourceSkills = Definition.defaultSkills;

        InitializeSkills(sourceSkills);
    }

    public string Name => runtimeName;
    public string Epitaph => epitaph;
    public CharacterRangeType RangeType => Definition != null ? Definition.rangeType : CharacterRangeType.Melee;
    public int Level => Definition != null ? Definition.level : 1;
    public int Exp => Definition != null ? Definition.exp : 0;
    public int MaxHP => maxHP;
    public int DMG => dmg;
    public int SPD => spd;
    public float HIT => hit;
    public float AC => ac;
    public float CRI => cri;
    public float CRD => crd;
    public float PoisonResist => poisonResist;
    public float BleedResist => bleedResist;
    public float StunResist => stunResist;
    public BattleUnitView ViewPrefab => ViewDefinition != null ? ViewDefinition.unitViewPrefab : null;
    public Sprite PortraitSprite => ViewDefinition != null ? ViewDefinition.portraitSprite : null;
    public Sprite BodySprite => ViewDefinition != null ? ViewDefinition.bodySprite : null;
    public IReadOnlyList<SkillDefinition> EquippedSkills => equippedSkills;

    public void TakeDamage(int amount) => CurrentHP = Mathf.Max(0, CurrentHP - amount);
    public void Heal(int amount) => CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);

    private void InitializeSkills(List<SkillDefinition> sourceSkills)
    {
        equippedSkills.Clear();
        skillCooldowns.Clear();

        if (sourceSkills == null)
            return;

        int maxSkillCount = Team == TeamType.Ally ? 3 : 2;
        int count = Mathf.Min(sourceSkills.Count, maxSkillCount);
        HashSet<string> usedSkillIds = new HashSet<string>();

        for (int i = 0; i < count; i++)
        {
            SkillDefinition skill = sourceSkills[i];
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

    public int GetMaxSkillSlotCount() => Team == TeamType.Ally ? 3 : 2;
    public bool HasSkillInSlot(int index) => GetSkillAt(index) != null;

    public bool IsSkillOnCooldown(SkillDefinition skill)
    {
        if (skill == null) return false;
        string key = GetSkillKey(skill);
        return skillCooldowns.TryGetValue(key, out int remaining) && remaining > 0;
    }

    public int GetRemainingCooldown(SkillDefinition skill)
    {
        if (skill == null) return 0;
        string key = GetSkillKey(skill);
        return skillCooldowns.TryGetValue(key, out int remaining) ? remaining : 0;
    }

    public bool CanUseSkill(SkillDefinition skill)
    {
        if (skill == null || IsDead) return false;
        if (!equippedSkills.Contains(skill)) return false;
        if (IsSkillOnCooldown(skill)) return false;
        return skill.CanBeUsedByUnit(this);
    }

    public void ConsumeSkillCooldown(SkillDefinition skill)
    {
        if (skill == null) return;
        string key = GetSkillKey(skill);
        skillCooldowns[key] = Mathf.Max(0, skill.cooldownTurns + 1);
    }

    // 실제로는 턴 종료 감소 용도. 호환성 때문에 이름 유지.
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
        if (skill == null) return "";
        if (!string.IsNullOrEmpty(skill.skillId)) return skill.skillId;
        return skill.name;
    }

    public override string ToString() => $"{Name}({Team}) [Slot:{SlotIndex + 1}, HP:{CurrentHP}/{MaxHP}]";
}
