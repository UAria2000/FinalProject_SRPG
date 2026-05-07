using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BattleResultPartyMemberSnapshot
{
    public PartyMemberData memberData;
    public UnitDefinition unitDefinition;
    public UnitViewDefinition unitViewDefinition;

    public string displayName;
    public bool isDead;
    public bool isExchangeable;
    public bool isNft;

    public int promotionRank;
    public int levelBefore;
    public int levelAfter;
    public int originalLevel;
    public int expBefore;
    public int expAfter;
    public int expToNextBefore;
    public int expToNextAfter;
    public int gainedExp;

    public float ExpBeforeNormalized
    {
        get
        {
            if (expToNextBefore <= 0)
                return 0f;
            return Mathf.Clamp01(expBefore / (float)expToNextBefore);
        }
    }

    public float ExpAfterNormalized
    {
        get
        {
            if (expToNextAfter <= 0)
                return 0f;
            return Mathf.Clamp01(expAfter / (float)expToNextAfter);
        }
    }

    public Sprite GetPortraitSprite()
    {
        if (unitViewDefinition == null)
            return null;

        return unitViewDefinition.GetBustPortraitSprite(isDead);
    }
}

[Serializable]
public class BattleResultPopupData
{
    public BattleResultType resultType = BattleResultType.None;
    public string title;

    public int soulReward;
    public int expRewardTotal;
    public int expRewardPerLivingUnit;
    public int defeatedOrCapturedEnemyCount;

    public int baseSoulReward;
    public int baseExpReward;
    public int totalBonusPercent;
    public int worldSizeBonusPercent;
    public int combatTypeBonusPercent;

    public readonly List<CapturedPrisonerRewardEntry> capturedPrisoners = new List<CapturedPrisonerRewardEntry>();
    public readonly List<BattleResultPartyMemberSnapshot> partyMembers = new List<BattleResultPartyMemberSnapshot>();

    public string GetTitleOrDefault()
    {
        if (!string.IsNullOrWhiteSpace(title))
            return title;

        switch (resultType)
        {
            case BattleResultType.Victory:
                return "전투 승리";
            case BattleResultType.Flee:
                return "전투 이탈";
            case BattleResultType.Defeat:
            case BattleResultType.WorldFailure:
                return "전투 패배";
            default:
                return "전투 결과";
        }
    }
}
