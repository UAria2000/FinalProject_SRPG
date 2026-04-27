using UnityEngine;

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
    [Tooltip("포획 성공 시 아군 인벤토리에 추가할 아이템. 보통 해당 종의 포트레잇 아이템을 연결한다.")]
    public ItemDefinition captureRewardItem;
}
