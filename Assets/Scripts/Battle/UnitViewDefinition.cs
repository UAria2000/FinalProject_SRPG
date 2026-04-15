using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Battle/Unit View Definition")]
public class UnitViewDefinition : ScriptableObject
{
    [Header("UI Portraits")]
    [FormerlySerializedAs("portrait")]
    public Sprite slotFaceSprite;
    public Sprite bustPortraitSprite;

    [Header("Battle")]
    [FormerlySerializedAs("bodySprite")]
    public Sprite battleSprite;
    public BattleUnitView viewPrefab;

    public Sprite GetSlotFaceSprite()
    {
        if (slotFaceSprite != null)
            return slotFaceSprite;
        if (bustPortraitSprite != null)
            return bustPortraitSprite;
        return battleSprite;
    }

    public Sprite GetBustPortraitSprite()
    {
        if (bustPortraitSprite != null)
            return bustPortraitSprite;
        if (slotFaceSprite != null)
            return slotFaceSprite;
        return battleSprite;
    }

    public Sprite GetBattleSprite()
    {
        if (battleSprite != null)
            return battleSprite;
        if (bustPortraitSprite != null)
            return bustPortraitSprite;
        return slotFaceSprite;
    }
}
