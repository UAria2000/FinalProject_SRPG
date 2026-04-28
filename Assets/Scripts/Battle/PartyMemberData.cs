using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PartyMemberData
{
    [Header("Unit")]
    public UnitDefinition unitDefinition;
    public UnitViewDefinition unitViewDefinition;

    [Header("Formation")]
    [Range(0, 3)] public int startSlotIndex = 0;

    [Header("Identity")]
    public string instanceId;
    public string instanceDisplayNameOverride;
    [TextArea(2, 5)] public string fixedEpitaph;

    [Header("Level")]
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
    [Range(0f, 100f)] public float promotionBonusPercentPerRank = 1f;

    [Header("Fixed Runtime Data")]
    public UnitInstanceStatVariance statVariance = new UnitInstanceStatVariance();

    [Tooltip("평타 제외, 최대 3개. 전투 외부에서 이미 결정된 상태를 넣는다.")]
    public List<SkillDefinition> learnedSkills = new List<SkillDefinition>();

    [Header("Battle Loot Drops")]
    public List<ItemDropDefinition> battleLootDrops = new List<ItemDropDefinition>();

    [Header("Persistent Run State")]
    [Tooltip("-1이면 아직 초기화되지 않은 상태로 간주하고 전투 시작 시 최대 체력을 사용한다.")]
    public int persistentCurrentHP = -1;

    public PartyMemberData CloneRuntime()
    {
        PartyMemberData clone = new PartyMemberData();
        clone.unitDefinition = unitDefinition;
        clone.unitViewDefinition = unitViewDefinition;
        clone.startSlotIndex = startSlotIndex;
        clone.instanceId = instanceId;
        clone.instanceDisplayNameOverride = instanceDisplayNameOverride;
        clone.fixedEpitaph = fixedEpitaph;
        clone.currentLevel = Mathf.Max(1, currentLevel);
        clone.originalLevel = Mathf.Max(1, originalLevel);
        clone.currentExp = Mathf.Max(0, currentExp);
        clone.levelGrowthMaxHp = Mathf.Max(0, levelGrowthMaxHp);
        clone.levelGrowthDmg = Mathf.Max(0, levelGrowthDmg);
        clone.promotionRank = Mathf.Clamp(promotionRank <= 0 ? 1 : promotionRank, 1, 9);
        clone.promotionBonusPercentPerRank = promotionBonusPercentPerRank;
        clone.statVariance = statVariance != null ? statVariance.CloneRuntime() : new UnitInstanceStatVariance();
        clone.learnedSkills = learnedSkills != null ? new List<SkillDefinition>(learnedSkills) : new List<SkillDefinition>();
        clone.battleLootDrops = battleLootDrops != null ? new List<ItemDropDefinition>(battleLootDrops) : new List<ItemDropDefinition>();
        clone.persistentCurrentHP = persistentCurrentHP;
        return clone;
    }

    public string GetDisplayName()
    {
        if (!string.IsNullOrEmpty(instanceDisplayNameOverride))
            return instanceDisplayNameOverride;

        return unitDefinition != null ? unitDefinition.unitName : "Unit";
    }

    public int GetMaxHP()
    {
        if (unitDefinition == null)
            return 1;

        int varianceHp = statVariance != null ? statVariance.maxHpDelta : 0;
        int growthHp = Mathf.Max(0, levelGrowthMaxHp);
        float promo = LegionFormula.GetPromotionMultiplier(promotionRank, promotionBonusPercentPerRank);
        return Mathf.Max(1, Mathf.RoundToInt((unitDefinition.maxHP + varianceHp + growthHp) * promo));
    }

    public int GetPersistentCurrentHPOrFull()
    {
        int maxHp = GetMaxHP();
        if (persistentCurrentHP < 0)
            return maxHp;

        return Mathf.Clamp(persistentCurrentHP, 0, maxHp);
    }

    public void ResetPersistentHPToFull()
    {
        if (unitDefinition == null)
        {
            persistentCurrentHP = -1;
            return;
        }

        persistentCurrentHP = GetMaxHP();
    }
}

[Serializable]
public class ItemDropDefinition
{
    public ItemDefinition item;
    [Range(0f, 100f)] public float dropChancePercent = 20f;
}