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

    [Header("Main Player")]
    [Tooltip("체크 시 이 유닛 종은 파티의 고정 메인 플레이어 캐릭터로 취급된다.")]
    public bool isMainPlayerCharacter = false;

    [Header("Capture")]
    [Tooltip("체크 시 이 유닛 종은 포획 대상이 될 수 있다.")]
    public bool canBeCaptured = false;
    [Tooltip("포획 성공 시 아군 인벤토리에 추가할 아이템. 보통 해당 종의 포트레잇 아이템을 연결한다.")]
    public ItemDefinition captureRewardItem;
}
