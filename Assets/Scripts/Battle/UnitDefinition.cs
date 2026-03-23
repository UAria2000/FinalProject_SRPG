using UnityEngine;

[CreateAssetMenu(menuName = "Battle/Unit Definition")]
public class UnitDefinition : ScriptableObject
{
    [Header("Identity")]
    public string unitId;
    public string unitName;
    public CharacterRangeType rangeType = CharacterRangeType.Melee;

    [Header("Base Stats")]
    public int maxHP = 10;
    public int dmg = 5;
    public int spd = 5;
    [Tooltip("실스탯. UI는 x10")]
    public float hit = 9f;
    [Tooltip("실스탯. UI는 x10")]
    public float ac = 5f;
    public int cri = 10;
    public int crd = 150;

    [Header("Resist")]
    public int poisonResist = 0;
    public int bleedResist = 0;
    public int stunResist = 0;

    [Header("Battle")]
    public SkillDefinition basicAttack;
    public StatVarianceRules varianceRules = new StatVarianceRules();
}
