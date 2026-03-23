using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Battle/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    [Header("Info")]
    public string itemId;
    public string itemName;
    [TextArea(2, 5)] public string description;
    public Sprite icon;

    [Header("Targeting")]
    public SkillTargetTeam targetTeam = SkillTargetTeam.Ally;
    public TargetScope targetScope = TargetScope.Single;

    [Header("Usage")]
    public bool usableInBattle = true;
    public bool consumeOnUse = true;
    public bool consumeTurnOnUse = true;

    [Header("Effects")]
    public List<BattleEffectBlock> effects = new List<BattleEffectBlock>();
}
