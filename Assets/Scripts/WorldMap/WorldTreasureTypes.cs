using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보물 이벤트에서 아이템 티어를 뽑기 위한 가중치.
/// </summary>
[Serializable]
public class WorldTreasureTierWeight
{
    public ItemTier tier = ItemTier.Tier1;
    [Min(0f)] public float weight = 1f;
}

/// <summary>
/// 보물 이벤트 1종 보상.
/// 같은 item은 한 번만 들어가는 것을 기본으로 한다.
/// </summary>
[Serializable]
public class WorldTreasureRewardItemEntry
{
    public ItemDefinition item;
    [Min(1)] public int amount = 1;

    public string GetDisplayName()
    {
        if (item == null)
            return "Item";

        if (!string.IsNullOrWhiteSpace(item.itemName))
            return item.itemName;

        return item.name;
    }
}

/// <summary>
/// 보물 이벤트 전체 보상 결과.
/// </summary>
[Serializable]
public class WorldTreasureResult
{
    public List<WorldTreasureRewardItemEntry> rewards = new List<WorldTreasureRewardItemEntry>(4);

    public int Count => rewards != null ? rewards.Count : 0;
    public bool HasAnyReward => rewards != null && rewards.Count > 0;

    public void Add(ItemDefinition item, int amount)
    {
        if (item == null || amount <= 0)
            return;

        if (rewards == null)
            rewards = new List<WorldTreasureRewardItemEntry>(4);

        for (int i = 0; i < rewards.Count; i++)
        {
            WorldTreasureRewardItemEntry existing = rewards[i];
            if (existing != null && existing.item == item)
            {
                existing.amount += Mathf.Max(1, amount);
                return;
            }
        }

        rewards.Add(new WorldTreasureRewardItemEntry
        {
            item = item,
            amount = Mathf.Max(1, amount)
        });
    }
}
