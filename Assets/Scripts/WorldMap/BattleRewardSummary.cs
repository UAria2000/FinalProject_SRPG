using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CapturedPrisonerRewardEntry
{
    public ItemDefinition prisonerItem;
    public UnitDefinition fallbackUnit;
    public UnitViewDefinition fallbackView;
    public int capturedLevel = 1;
    public bool isExchangeable;

    public string GetDisplayName()
    {
        if (prisonerItem != null)
        {
            if (!string.IsNullOrWhiteSpace(prisonerItem.itemName))
                return prisonerItem.itemName;

            return prisonerItem.name;
        }

        return fallbackUnit != null ? fallbackUnit.unitName : "Unknown Prisoner";
    }

    public Sprite GetIcon()
    {
        if (prisonerItem != null && prisonerItem.icon != null)
            return prisonerItem.icon;

        if (fallbackUnit != null && fallbackUnit.captureRewardItem != null)
            return fallbackUnit.captureRewardItem.icon;

        return fallbackView != null ? fallbackView.GetSlotFaceSprite() : null;
    }
}

[Serializable]
public class BattleRewardEnemyEntry
{
    public UnitDefinition unitDefinition;
    public UnitViewDefinition unitViewDefinition;
    public int level = 1;
    public int baseSoulReward;
    public int baseExpReward;
    public bool captured;
}

[Serializable]
public class BattleRewardSummary
{
    public int soulReward;
    public int expReward;

    public int baseSoulReward;
    public int baseExpReward;
    public int rewardBonusPercent;
    public int worldSizeBonusPercent;
    public int combatTypeBonusPercent;
    public BattleResultType resultType = BattleResultType.None;

    public readonly List<UnitDefinition> defeatedEnemyUnits = new List<UnitDefinition>();
    public readonly List<BattleRewardEnemyEntry> rewardEnemyEntries = new List<BattleRewardEnemyEntry>();

    // 전투 장비 드랍은 임시 폐기 상태. 구버전 호환을 위해 컬렉션만 유지한다.
    public readonly List<ItemDefinition> droppedItems = new List<ItemDefinition>();

    // 호환용. 새 포획 플로우는 capturedPrisonerRewards/capturedPrisonerItems를 사용한다.
    public readonly List<UnitDefinition> capturedPrisoners = new List<UnitDefinition>();
    public readonly List<ItemDefinition> capturedPrisonerItems = new List<ItemDefinition>();
    public readonly List<CapturedPrisonerRewardEntry> capturedPrisonerRewards = new List<CapturedPrisonerRewardEntry>();

    public int DefeatedOrCapturedEnemyCount
    {
        get
        {
            if (rewardEnemyEntries != null && rewardEnemyEntries.Count > 0)
                return rewardEnemyEntries.Count;

            int count = defeatedEnemyUnits != null ? defeatedEnemyUnits.Count : 0;
            if (capturedPrisonerRewards != null && capturedPrisonerRewards.Count > 0)
                count += capturedPrisonerRewards.Count;
            else if (capturedPrisonerItems != null && capturedPrisonerItems.Count > 0)
                count += capturedPrisonerItems.Count;
            else if (capturedPrisoners != null)
                count += capturedPrisoners.Count;
            return count;
        }
    }

    public void Clear()
    {
        soulReward = 0;
        expReward = 0;
        baseSoulReward = 0;
        baseExpReward = 0;
        rewardBonusPercent = 0;
        worldSizeBonusPercent = 0;
        combatTypeBonusPercent = 0;
        resultType = BattleResultType.None;

        defeatedEnemyUnits.Clear();
        rewardEnemyEntries.Clear();
        droppedItems.Clear();
        capturedPrisoners.Clear();
        capturedPrisonerItems.Clear();
        capturedPrisonerRewards.Clear();
    }

    public void AddEnemyReward(BattleRewardEnemyEntry entry)
    {
        if (entry == null)
            return;

        rewardEnemyEntries.Add(entry);
        baseSoulReward += Mathf.Max(0, entry.baseSoulReward);
        baseExpReward += Mathf.Max(0, entry.baseExpReward);
        soulReward += Mathf.Max(0, entry.baseSoulReward);
        expReward += Mathf.Max(0, entry.baseExpReward);

        if (!entry.captured && entry.unitDefinition != null)
            defeatedEnemyUnits.Add(entry.unitDefinition);
    }

    public void ApplyRewardBonus(int bonusPercent)
    {
        rewardBonusPercent = Mathf.Max(0, bonusPercent);
        soulReward = ApplyBonus(baseSoulReward, rewardBonusPercent);
        expReward = ApplyBonus(baseExpReward, rewardBonusPercent);
    }

    private int ApplyBonus(int baseValue, int bonusPercent)
    {
        return Mathf.Max(0, Mathf.RoundToInt(Mathf.Max(0, baseValue) * (1f + Mathf.Max(0, bonusPercent) * 0.01f)));
    }
}
