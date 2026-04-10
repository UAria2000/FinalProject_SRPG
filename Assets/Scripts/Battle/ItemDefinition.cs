using System.Collections.Generic;
using UnityEngine;

public enum MainUIItemCategory
{
    Equipment,
    Consumable,
    Other,
}

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
    [Min(0)] public int baseSoulValue = 0;
    public bool consumeOnUse = true;
    public bool consumeTurnOnUse = true;

    [Header("Main UI")]
    public MainUIItemCategory mainUICategory = MainUIItemCategory.Other;
    [Tooltip("창고 하단 파티 공용 소모품 슬롯에 장착 가능한 아이템인지 여부")]
    public bool canAssignToSharedConsumableSlot = false;

    [Header("Effects")]
    public List<BattleEffectBlock> effects = new List<BattleEffectBlock>();
}
