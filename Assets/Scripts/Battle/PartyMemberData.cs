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

    [Header("Fixed Runtime Data")]
    public UnitInstanceStatVariance statVariance = new UnitInstanceStatVariance();

    [Tooltip("평타 제외, 최대 3개. 전투 외부에서 이미 결정된 상태를 넣는다.")]
    public List<SkillDefinition> learnedSkills = new List<SkillDefinition>();

    [Header("Persistent Run State")]
    [Tooltip("-1이면 아직 초기화되지 않은 상태로 간주하고 전투 시작 시 최대 체력을 사용한다.")]
    public int persistentCurrentHP = -1;

    public string GetDisplayName()
    {
        if (!string.IsNullOrEmpty(instanceDisplayNameOverride))
            return instanceDisplayNameOverride;

        return unitDefinition != null ? unitDefinition.unitName : "Unit";
    }

    public void ResetPersistentHPToFull()
    {
        if (unitDefinition == null)
                {
            persistentCurrentHP = -1;
            return;
        }

        int varianceHp = statVariance != null ? statVariance.maxHpDelta : 0;
        int maxHp = Mathf.Max(1, unitDefinition.maxHP + varianceHp);
        persistentCurrentHP = maxHp;
    }
}
