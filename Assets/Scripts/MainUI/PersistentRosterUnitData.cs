using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PersistentRosterUnitData
{
    [Header("Identity")]
    public string instanceId;
    public string instanceDisplayNameOverride;
    [TextArea(2, 5)] public string fixedEpitaph;
    public long obtainedOrder;

    [Header("Base References")]
    public UnitDefinition unitDefinition;
    public UnitViewDefinition unitViewDefinition;
    public bool isExchangeable;
    public bool isFavorite;

    [Header("Level / EXP")]
    public int currentLevel = 1;
    public int originalLevel = 1;
    public int currentExp = 0;

    [Header("Promotion")]
    [Min(0)] public int promotionRank = 0;

    [Header("Stats")]
    public UnitInstanceStatVariance statVariance = new UnitInstanceStatVariance();

    [Header("Skills / Drops")]
    public List<SkillDefinition> learnedSkills = new List<SkillDefinition>();
    public List<ItemDropDefinition> battleLootDrops = new List<ItemDropDefinition>();

    [Header("Runtime Carryover")]
    [Tooltip("-1이면 초기화되지 않은 상태로 간주.")]
    public int persistentCurrentHP = -1;

    public static PersistentRosterUnitData CreateFromPartyMember(PartyMemberData member, bool exchangeable, long obtainedOrder)
    {
        PersistentRosterUnitData data = new PersistentRosterUnitData();
        data.OverwriteFromPartyMember(member);
        data.isExchangeable = exchangeable;
        data.obtainedOrder = obtainedOrder;
        data.EnsureDefaults();
        return data;
    }

    public void OverwriteFromPartyMember(PartyMemberData member)
    {
        if (member == null)
            return;

        instanceId = string.IsNullOrWhiteSpace(member.instanceId)
            ? Guid.NewGuid().ToString("N")
            : member.instanceId;

        instanceDisplayNameOverride = member.instanceDisplayNameOverride;
        fixedEpitaph = member.fixedEpitaph;
        unitDefinition = member.unitDefinition;
        unitViewDefinition = member.unitViewDefinition;
        currentLevel = Mathf.Max(1, member.currentLevel);
        originalLevel = Mathf.Max(1, member.originalLevel);
        promotionRank = Mathf.Max(0, member.promotionRank);
        statVariance = member.statVariance != null ? member.statVariance.CloneRuntime() : new UnitInstanceStatVariance();
        learnedSkills = member.learnedSkills != null ? new List<SkillDefinition>(member.learnedSkills) : new List<SkillDefinition>();
        battleLootDrops = member.battleLootDrops != null ? new List<ItemDropDefinition>(member.battleLootDrops) : new List<ItemDropDefinition>();
        persistentCurrentHP = member.persistentCurrentHP;

        EnsureDefaults();
    }

    public PartyMemberData CreateRuntimePartyMember(int startSlotIndex, float promotionBonusPercentPerRank = 1f)
    {
        EnsureDefaults();

        PartyMemberData runtime = new PartyMemberData();
        runtime.unitDefinition = unitDefinition;
        runtime.unitViewDefinition = unitViewDefinition;
        runtime.startSlotIndex = Mathf.Clamp(startSlotIndex, 0, 3);
        runtime.instanceId = instanceId;
        runtime.instanceDisplayNameOverride = instanceDisplayNameOverride;
        runtime.fixedEpitaph = fixedEpitaph;
        runtime.currentLevel = Mathf.Max(1, currentLevel);
        runtime.originalLevel = Mathf.Max(1, originalLevel);
        runtime.promotionRank = Mathf.Max(0, promotionRank);
        runtime.promotionBonusPercentPerRank = Mathf.Max(0f, promotionBonusPercentPerRank);
        runtime.statVariance = statVariance != null ? statVariance.CloneRuntime() : new UnitInstanceStatVariance();
        runtime.learnedSkills = learnedSkills != null ? new List<SkillDefinition>(learnedSkills) : new List<SkillDefinition>();
        runtime.battleLootDrops = battleLootDrops != null ? new List<ItemDropDefinition>(battleLootDrops) : new List<ItemDropDefinition>();
        runtime.persistentCurrentHP = persistentCurrentHP;
        return runtime;
    }

    public string GetDisplayName()
    {
        if (!string.IsNullOrWhiteSpace(instanceDisplayNameOverride))
            return instanceDisplayNameOverride;

        return unitDefinition != null ? unitDefinition.unitName : "Unit";
    }

    public void EnsureDefaults()
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            instanceId = Guid.NewGuid().ToString("N");

        currentLevel = Mathf.Max(1, currentLevel);
        originalLevel = Mathf.Max(1, originalLevel);
        promotionRank = Mathf.Max(0, promotionRank);

        if (statVariance == null)
            statVariance = new UnitInstanceStatVariance();

        if (learnedSkills == null)
            learnedSkills = new List<SkillDefinition>();

        if (battleLootDrops == null)
            battleLootDrops = new List<ItemDropDefinition>();

        if (persistentCurrentHP < -1)
            persistentCurrentHP = -1;
    }
}

[Serializable]
public class PersistentProfileState
{
    public List<PersistentRosterUnitData> rosterUnits = new List<PersistentRosterUnitData>();
    public PersistentAccountCurrencyState accountCurrencies = new PersistentAccountCurrencyState();
    public long nextObtainedOrder = 1;

    public void EnsureDefaults()
    {
        if (rosterUnits == null)
            rosterUnits = new List<PersistentRosterUnitData>();

        if (accountCurrencies == null)
            accountCurrencies = new PersistentAccountCurrencyState();

        accountCurrencies.EnsureDefaults();

        if (nextObtainedOrder < 1)
            nextObtainedOrder = 1;

        for (int i = 0; i < rosterUnits.Count; i++)
        {
            if (rosterUnits[i] != null)
                rosterUnits[i].EnsureDefaults();
        }
    }

    public long ConsumeObtainedOrder()
    {
        EnsureDefaults();
        long order = nextObtainedOrder;
        nextObtainedOrder++;
        return order;
    }
}
