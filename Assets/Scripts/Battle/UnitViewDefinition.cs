using UnityEngine;

[CreateAssetMenu(menuName = "Battle/Unit View Definition")]
public class UnitViewDefinition : ScriptableObject
{
    public Sprite portrait;
    public Sprite bodySprite;
    public BattleUnitView viewPrefab;
}
