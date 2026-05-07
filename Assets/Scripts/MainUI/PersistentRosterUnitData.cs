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
    [Tooltip("포획 포로 아이템에서 즉시 전환되어 생성된 유닛인지 여부. 연결 유닛이 레기온 표시 비활성 상태여도 이 값이 true면 군단 창에 표시할 수 있다.")]
    public bool isConvertedFromPrisoner;

    [Header("Legion Instance")]
    [Tooltip("true면 UnitDefinition의 NFT 기본값과 무관하게 NFT/교환 가능 유닛으로 표시된다.")]
    public bool isNft;
    [Tooltip("구버전 저장 데이터 호환용. 현재 방패 랭크는 promotionRank와 동일하며 이 값은 사용하지 않는다.")]
    [Range(0, 9)] public int unitRankOverride = 0;

    [Header("Level / EXP")]
    public int currentLevel = 1;
    public int originalLevel = 1;
    public int currentExp = 0;

    [Header("Level Growth Total")]
    [Tooltip("레벨업으로 누적 증가한 최대 HP. UnitDefinition 기본값/개체값과 별도로 더해진다.")]
    public int levelGrowthMaxHp = 0;
    [Tooltip("레벨업으로 누적 증가한 DMG. UnitDefinition 기본값/개체값과 별도로 더해진다.")]
    public int levelGrowthDmg = 0;

    [Header("Promotion")]
    [Range(1, 9)] public int promotionRank = 1;

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
        isExchangeable = member.isExchangeable;
        isNft = member.isNft;
        unitDefinition = member.unitDefinition;
        unitViewDefinition = member.unitViewDefinition;
        currentLevel = Mathf.Max(1, member.currentLevel);
        originalLevel = Mathf.Max(1, member.originalLevel);
        currentExp = Mathf.Max(0, member.currentExp);
        levelGrowthMaxHp = Mathf.Max(0, member.levelGrowthMaxHp);
        levelGrowthDmg = Mathf.Max(0, member.levelGrowthDmg);
        promotionRank = LegionFormula.ClampLegionRank(member.promotionRank);
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
        runtime.isExchangeable = isExchangeable;
        runtime.isNft = isNft;
        runtime.currentLevel = Mathf.Max(1, currentLevel);
        runtime.originalLevel = Mathf.Max(1, originalLevel);
        runtime.currentExp = Mathf.Max(0, currentExp);
        runtime.levelGrowthMaxHp = Mathf.Max(0, levelGrowthMaxHp);
        runtime.levelGrowthDmg = Mathf.Max(0, levelGrowthDmg);
        runtime.promotionRank = LegionFormula.ClampLegionRank(promotionRank);
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

    public int GetLegionRank()
    {
        // 방패 랭크는 승급 랭크와 동일하다.
        return LegionFormula.ClampLegionRank(promotionRank);
    }

    public bool IsNftUnit()
    {
        return isNft || isExchangeable || (unitDefinition != null && unitDefinition.isNftUnit);
    }

    public bool CanDefinitionBeDecomposed()
    {
        return unitDefinition == null || unitDefinition.canBeDecomposed;
    }

    public void EnsureDefaults()
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            instanceId = Guid.NewGuid().ToString("N");

        currentLevel = Mathf.Max(1, currentLevel);
        originalLevel = Mathf.Max(1, originalLevel);
        currentExp = Mathf.Max(0, currentExp);
        levelGrowthMaxHp = Mathf.Max(0, levelGrowthMaxHp);
        levelGrowthDmg = Mathf.Max(0, levelGrowthDmg);
        promotionRank = LegionFormula.ClampLegionRank(promotionRank);
        unitRankOverride = Mathf.Clamp(unitRankOverride, 0, 9);

        if (unitDefinition != null && unitDefinition.isNftUnit)
            isNft = true;

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
    public List<PersistentRosterUnitData> graveyardUnits = new List<PersistentRosterUnitData>();
    public PersistentAccountCurrencyState accountCurrencies = new PersistentAccountCurrencyState();
    public long nextObtainedOrder = 1;

    public void EnsureDefaults()
    {
        if (rosterUnits == null)
            rosterUnits = new List<PersistentRosterUnitData>();

        if (graveyardUnits == null)
            graveyardUnits = new List<PersistentRosterUnitData>();

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

        for (int i = 0; i < graveyardUnits.Count; i++)
        {
            if (graveyardUnits[i] != null)
                graveyardUnits[i].EnsureDefaults();
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
