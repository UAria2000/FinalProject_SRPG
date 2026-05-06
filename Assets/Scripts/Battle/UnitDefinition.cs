using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Battle/Unit Definition")]
public class UnitDefinition : ScriptableObject
{
    [Header("Identity")]
    public string unitId;
    public string unitName;
    public CharacterRangeType rangeType = CharacterRangeType.Melee;

    [Header("Legion Metadata")]
    [Tooltip("NFT/교환 가능 필터와 배지에 사용할 기본값. 인스턴스별 isExchangeable/isNft와 함께 true로 취급된다.")]
    public bool isNftUnit = false;
    [Tooltip("레기온 화면에 표시할지 여부. false면 로스터에 있어도 레기온 목록에서 숨긴다.")]
    public bool showInLegion = true;
    [Tooltip("동일 조건 정렬 시 보조 우선순위. 큰 값이 먼저 온다.")]
    public int legionSortPriority = 0;
    [Tooltip("선택 사항. UI 텍스트/툴팁 확장용 분류명.")]
    public string legionCategoryLabel;

    [Header("Base Stats")]
    public int maxHP = 10;
    public int dmg = 5;
    public int spd = 5;

    [Tooltip("받는 피해 감소율. 20이면 최종 받는 직접 공격 피해가 20% 감소합니다. 화상은 이 값을 중첩 수만큼 감소시킵니다.")]
    public int idt = 0;

    [Header("Level Growth")]
    [Tooltip("레벨업 1회당 증가할 HP 범위. 레벨업 시 범위 내 정수 1개가 무작위로 선택된다.")]
    public Vector2Int hpGrowthPerLevel = new Vector2Int(2, 4);
    [Tooltip("레벨업 1회당 증가할 DMG 범위. 레벨업 시 범위 내 정수 1개가 무작위로 선택된다.")]
    public Vector2Int dmgGrowthPerLevel = new Vector2Int(1, 3);

    [Tooltip("실스탯. UI는 x10")]
    public float hit = 9f;
    [Tooltip("실스탯. UI는 x10")]
    public float ac = 5f;
    public int cri = 10;
    public int crd = 150;

    [Header("Resist")]
    [FormerlySerializedAs("poisonResist")]
    public int burnResist = 0;
    public int bleedResist = 0;
    public int stunResist = 0;
    public int frostResist = 0;
    public int blindResist = 0;

    [Header("Battle")]
    public SkillDefinition basicAttack;
    public StatVarianceRules varianceRules = new StatVarianceRules();

    [Header("Enemy Equipment")]
    [Tooltip("무작위 적 생성 시 실제 장착 슬롯에 들어갈 수 있는 장비 후보. 최대 2개까지 장착합니다.")]
    public List<ItemDropDefinition> randomEnemyEquipment = new List<ItemDropDefinition>();

    [Header("Last Will")]
    [Range(0f, 100f)] public float lastWillChancePercent = 30f;
    public BattleLastWillTextTable lastWillTextTable;

    [Header("Main Player")]
    [Tooltip("체크 시 이 유닛 종은 파티의 고정 메인 플레이어 캐릭터로 취급된다.")]
    public bool isMainPlayerCharacter = false;

    [Header("Rewards")]
    [Min(0)] public int baseSoulReward = 0;

    [Header("Legion Decompose")]
    [Tooltip("체크 해제 시 메인 캐릭터/즐겨찾기/파티 편성 여부와 무관하게 분해할 수 없다.")]
    public bool canBeDecomposed = true;
    [Tooltip("분해 시 기본으로 얻는 공용 유닛 파편. 실제 보상은 최소 1이며, 승급 투자분 50% 환급이 추가된다.")]
    [Min(1)] public int decomposeShardReward = 1;

    [Header("Capture")]
    [Tooltip("체크 시 이 유닛 종은 포획 대상이 될 수 있다.")]
    public bool canBeCaptured = false;
    [Tooltip("포획 가능한 적으로 등장할 때 NFT/교환 가능 배지를 가질 확률입니다.")]
    [Range(0f, 100f)] public float capturableEnemyNftChancePercent = 0f;
    [Tooltip("포획 성공 시 아군 인벤토리에 추가할 아이템. 보통 해당 종의 포트레잇 아이템을 연결한다.")]
    public ItemDefinition captureRewardItem;
}
