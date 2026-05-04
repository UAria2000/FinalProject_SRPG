using System.Collections.Generic;
using UnityEngine;

public enum MainUIItemCategory
{
    Equipment,
    Consumable,
    Other,
}

public enum ItemTier
{
    Tier1 = 1,
    Tier2 = 2,
    Tier3 = 3,
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

    [Header("Tier")]
    public ItemTier itemTier = ItemTier.Tier1;

    [Header("Usage")]
    public bool usableInBattle = true;
    [Min(0)] public int baseSoulValue = 0;
    public bool consumeOnUse = true;
    public bool consumeTurnOnUse = true;

    [Header("Main UI")]
    public MainUIItemCategory mainUICategory = MainUIItemCategory.Other;
    [Tooltip("창고 하단 파티 공용 소모품 슬롯에 장착 가능한 아이템인지 여부")]
    public bool canAssignToSharedConsumableSlot = false;


    [Header("Equipment Bonuses")]
    [Tooltip("장비 착용 중 적용되는 최대 HP 고정 보너스")]
    public int equipmentMaxHpBonus = 0;
    [Tooltip("장비 착용 중 적용되는 DMG 고정 보너스")]
    public int equipmentDmgBonus = 0;
    [Tooltip("장비 착용 중 적용되는 SPD 고정 보너스")]
    public int equipmentSpdBonus = 0;
    [Tooltip("장비 착용 중 적용되는 HIT 표시값 보너스. 예: 10 입력 시 UI HIT +10, 전투 실스탯 +1.0")]
    public int equipmentHitBonusX10 = 0;
    [Tooltip("장비 착용 중 적용되는 AC 표시값 보너스. 예: 10 입력 시 UI AC +10, 전투 실스탯 +1.0")]
    public int equipmentAcBonusX10 = 0;
    [Tooltip("장비 착용 중 적용되는 CRI 고정 보너스(%)")]
    public int equipmentCriBonus = 0;
    [Tooltip("장비 착용 중 적용되는 CRD 고정 보너스(%)")]
    public int equipmentCrdBonus = 0;

    [Header("Equipment Resistance Bonuses")]
    [Tooltip("기절/출혈/화상/동상/실명 저항에 모두 더해지는 장비 보너스")]
    public int equipmentAllResistBonus = 0;
    public int equipmentBurnResistBonus = 0;
    public int equipmentBleedResistBonus = 0;
    public int equipmentStunResistBonus = 0;
    public int equipmentFrostResistBonus = 0;
    public int equipmentBlindResistBonus = 0;

    [Header("Equipment Battle Start Effects")]
    [Tooltip("전투 시작 시 착용자에게 최대 체력 비율로 보호막을 부여합니다. 예: 10 = 최대 체력의 10% 보호막")]
    [Range(0f, 100f)] public float equipmentStartShieldPercentOfMaxHP = 0f;

    [Header("Prisoner Item")]
    [Tooltip("체크 시 이 아이템은 포획 보상용 포로 아이템으로 취급된다. 전투 종료 후 월드 포로 데이터로 변환된다.")]
    public bool isPrisonerItem = false;
    [Tooltip("이 포로 아이템이 타락 완료 후 생성할 유닛 정의. 비워두면 포로로 변환할 수 없다.")]
    public UnitDefinition prisonerSourceUnitDefinition;
    [Tooltip("포로 UI에서 이 아이템 아이콘을 초상화로 우선 사용한다.")]
    public bool useItemIconAsPrisonerPortrait = true;

    [Header("Effects")]
    public List<BattleEffectBlock> effects = new List<BattleEffectBlock>();
}
