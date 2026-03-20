using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Battle/Unit Definition")]
public class UnitDefinition : ScriptableObject
{
    [Header("Info")]
    public string unitName;
    public CharacterRangeType rangeType;

    [Header("Growth")]
    public int level = 1;

    [Tooltip("유저에게 직접 표시하지 않는 내부 성장값")]
    public int exp = 0;

    [Header("Base Stats")]
    public int maxHP = 20;
    public int dmg = 7;
    public int spd = 5;

    [Header("Combat Rates (%)")]
    [Range(0f, 100f)] public float hit = 90f;
    [Range(0f, 100f)] public float ac = 5f;
    [Range(0f, 100f)] public float cri = 10f;
    [Range(0f, 300f)] public float crd = 150f;

    [Header("Status Resistances (%)")]
    [Range(0f, 100f)] public float poisonResist = 0f;
    [Range(0f, 100f)] public float bleedResist = 0f;
    [Range(0f, 100f)] public float stunResist = 0f;

    [Header("Default Skills")]
    public List<SkillDefinition> defaultSkills = new List<SkillDefinition>();
}