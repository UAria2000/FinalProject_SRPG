using UnityEngine;

[CreateAssetMenu(menuName = "Battle/Unit Definition")]
public class UnitDefinition : ScriptableObject
{
    [Header("Info")]
    public string unitName;
    public Sprite sprite;
    public CharacterRangeType rangeType;

    [Header("Base Stats")]
    public int maxHP = 20;
    public int atk = 7;
    public int def = 2;
    public int spd = 5;
    [Range(0, 100)] public int crit = 10;
    [Range(0, 100)] public int acc = 90;
    [Range(0, 100)] public int eva = 5;
}