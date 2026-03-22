using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PartyMemberData
{
    [Header("Unit Data")]
    public UnitDefinition unitDefinition;

    [Header("View Data")]
    public UnitViewDefinition unitViewDefinition;

    [Header("Formation")]
    [Tooltip("시작 슬롯 번호. 0=앞열, 3=뒷열")]
    [Range(0, 3)]
    public int startSlotIndex = 0;

    [Header("Instance Identity")]
    [Tooltip("비워두면 unitDefinition.unitName을 사용")]
    public string instanceDisplayNameOverride;
    [TextArea(2, 5)]
    public string fixedEpitaph;

    [Header("Instance Stats Override")]
    public bool useInstanceStatOverride = false;
    public int maxHPOverride;
    public int dmgOverride;
    public int spdOverride;
    public float hitOverride;
    public float acOverride;
    public float criOverride;
    public float crdOverride;
    public float poisonResistOverride;
    public float bleedResistOverride;
    public float stunResistOverride;

    [Header("Instance Skills")]
    [Tooltip("비어 있으면 UnitDefinition.defaultSkills 사용")]
    public List<SkillDefinition> equippedSkills = new List<SkillDefinition>();
}
