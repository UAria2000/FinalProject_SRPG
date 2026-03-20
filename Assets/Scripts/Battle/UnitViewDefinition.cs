using UnityEngine;

[CreateAssetMenu(menuName = "Battle/Unit View Definition")]
public class UnitViewDefinition : ScriptableObject
{
    [Header("Prefab")]
    [Tooltip("배틀 씬에서 생성할 공통 기반 UI 유닛 프리팹")]
    public BattleUnitView unitViewPrefab;

    [Header("Visuals")]
    [Tooltip("전투 UI에서 표시할 초상화")]
    public Sprite portraitSprite;

    [Tooltip("전투 UI에서 본체 이미지로 사용할 스프라이트")]
    public Sprite bodySprite;

    [Header("Optional")]
    [Tooltip("이름을 기본 UnitDefinition 이름 대신 따로 보여주고 싶을 때 사용. 비워두면 UnitDefinition 이름 사용")]
    public string displayNameOverride;
}