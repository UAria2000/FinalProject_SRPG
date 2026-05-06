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
public class BattleRewardSummary
{
    public int soulReward;
    public int expReward;

    public readonly List<UnitDefinition> defeatedEnemyUnits = new List<UnitDefinition>();
    public readonly List<ItemDefinition> droppedItems = new List<ItemDefinition>();

    // 호환용. 새 포획 플로우는 capturedPrisonerRewards/capturedPrisonerItems를 사용한다.
    public readonly List<UnitDefinition> capturedPrisoners = new List<UnitDefinition>();
    public readonly List<ItemDefinition> capturedPrisonerItems = new List<ItemDefinition>();
    public readonly List<CapturedPrisonerRewardEntry> capturedPrisonerRewards = new List<CapturedPrisonerRewardEntry>();

    public void Clear()
    {
        soulReward = 0;
        expReward = 0;
        defeatedEnemyUnits.Clear();
        droppedItems.Clear();
        capturedPrisoners.Clear();
        capturedPrisonerItems.Clear();
        capturedPrisonerRewards.Clear();
    }
}
