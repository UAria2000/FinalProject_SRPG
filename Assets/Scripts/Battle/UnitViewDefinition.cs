using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Battle/Unit View Definition")]
public class UnitViewDefinition : ScriptableObject
{
    [Header("UI Portraits")]
    [FormerlySerializedAs("portrait")]
    public Sprite slotFaceSprite;
    public Sprite bustPortraitSprite;

    [Header("Dead UI Portraits")]
    public Sprite deadSlotFaceSprite;
    public Sprite deadBustPortraitSprite;

    [Header("Battle")]
    [FormerlySerializedAs("bodySprite")]
    public Sprite battleSprite;

    [Header("Dead Battle")]
    public Sprite deadBattleSprite;

    public BattleUnitView viewPrefab;

    public Sprite GetSlotFaceSprite()
    {
        return GetSlotFaceSprite(false);
    }

    public Sprite GetBustPortraitSprite()
    {
        return GetBustPortraitSprite(false);
    }

    public Sprite GetBattleSprite()
    {
        return GetBattleSprite(false);
    }

    public Sprite GetSlotFaceSprite(bool isDead)
    {
        if (isDead)
        {
            if (deadSlotFaceSprite != null)
                return deadSlotFaceSprite;
            if (deadBustPortraitSprite != null)
                return deadBustPortraitSprite;
            if (deadBattleSprite != null)
                return deadBattleSprite;
        }

        if (slotFaceSprite != null)
            return slotFaceSprite;
        if (bustPortraitSprite != null)
            return bustPortraitSprite;
        return battleSprite;
    }

    public Sprite GetBustPortraitSprite(bool isDead)
    {
        if (isDead)
        {
            if (deadBustPortraitSprite != null)
                return deadBustPortraitSprite;
            if (deadSlotFaceSprite != null)
                return deadSlotFaceSprite;
            if (deadBattleSprite != null)
                return deadBattleSprite;
        }

        if (bustPortraitSprite != null)
            return bustPortraitSprite;
        if (slotFaceSprite != null)
            return slotFaceSprite;
        return battleSprite;
    }

    public Sprite GetBattleSprite(bool isDead)
    {
        if (isDead)
        {
            if (deadBattleSprite != null)
                return deadBattleSprite;
            if (deadBustPortraitSprite != null)
                return deadBustPortraitSprite;
            if (deadSlotFaceSprite != null)
                return deadSlotFaceSprite;
        }

        if (battleSprite != null)
            return battleSprite;
        if (bustPortraitSprite != null)
            return bustPortraitSprite;
        return slotFaceSprite;
    }
}