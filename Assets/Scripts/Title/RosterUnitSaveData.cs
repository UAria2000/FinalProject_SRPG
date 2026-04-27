using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RosterUnitSaveData
{
    public string unitInstanceId;
    public string unitDefinitionId;
    public string unitViewDefinitionName;
    public bool isMainCharacter;
    public bool isExchangeable;
    public bool isFavorite;
    public bool isNft;
    public int unitRankOverride;
    public bool canDismantle = true;

    public string instanceDisplayNameOverride;
    public string fixedEpitaph;
    public long obtainedOrder;

    public int level = 1;
    public int originalLevel = 1;
    public int currentExp = 0;
    public int levelGrowthMaxHp = 0;
    public int levelGrowthDmg = 0;

    public int promotionRank = 1;
    public float promotionBonusPercentPerRank = 1f;

    public int persistentCurrentHP = -1;

    public StatVarianceSaveData statVariance = new StatVarianceSaveData();
    public List<string> learnedSkillIds = new List<string>();
    public List<BattleLootDropSaveData> battleLootDrops = new List<BattleLootDropSaveData>();

    public static RosterUnitSaveData FromPersistent(PersistentRosterUnitData unit, float promotionPercentPerRank = 1f)
    {
        if (unit == null)
            return null;

        RosterUnitSaveData data = new RosterUnitSaveData
        {
            unitInstanceId = unit.instanceId,
            unitDefinitionId = unit.unitDefinition != null ? unit.unitDefinition.unitId : string.Empty,
            unitViewDefinitionName = unit.unitViewDefinition != null ? unit.unitViewDefinition.name : string.Empty,
            isMainCharacter = unit.unitDefinition != null && unit.unitDefinition.isMainPlayerCharacter,
            isExchangeable = unit.isExchangeable,
            isFavorite = unit.isFavorite,
            isNft = unit.IsNftUnit(),
            unitRankOverride = Mathf.Clamp(unit.unitRankOverride, 0, 9),
            canDismantle = unit.CanDefinitionBeDecomposed() && !(unit.unitDefinition != null && unit.unitDefinition.isMainPlayerCharacter),
            instanceDisplayNameOverride = unit.instanceDisplayNameOverride,
            fixedEpitaph = unit.fixedEpitaph,
            obtainedOrder = unit.obtainedOrder,
            level = Mathf.Max(1, unit.currentLevel),
            originalLevel = Mathf.Max(1, unit.originalLevel),
            currentExp = Mathf.Max(0, unit.currentExp),
            levelGrowthMaxHp = Mathf.Max(0, unit.levelGrowthMaxHp),
            levelGrowthDmg = Mathf.Max(0, unit.levelGrowthDmg),
            promotionRank = LegionFormula.ClampLegionRank(unit.promotionRank),
            promotionBonusPercentPerRank = Mathf.Max(0f, promotionPercentPerRank),
            persistentCurrentHP = unit.persistentCurrentHP,
            statVariance = StatVarianceSaveData.FromRuntime(unit.statVariance),
        };

        if (unit.learnedSkills != null)
        {
            for (int i = 0; i < unit.learnedSkills.Count; i++)
            {
                SkillDefinition skill = unit.learnedSkills[i];
                if (skill != null && !string.IsNullOrWhiteSpace(skill.skillId))
                    data.learnedSkillIds.Add(skill.skillId);
            }
        }

        if (unit.battleLootDrops != null)
        {
            for (int i = 0; i < unit.battleLootDrops.Count; i++)
            {
                ItemDropDefinition drop = unit.battleLootDrops[i];
                if (drop == null || drop.item == null || string.IsNullOrWhiteSpace(drop.item.itemId))
                    continue;

                data.battleLootDrops.Add(new BattleLootDropSaveData
                {
                    itemId = drop.item.itemId,
                    dropChancePercent = drop.dropChancePercent,
                });
            }
        }

        return data;
    }
}
