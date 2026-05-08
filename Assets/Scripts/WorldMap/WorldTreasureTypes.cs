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
/// 보물 이벤트에서 드랍 개수를 뽑기 위한 가중치.
/// </summary>
[Serializable]
public class WorldTreasureDropCountWeight
{
    [Range(0, 4)] public int dropCount = 0;
    [Min(0f)] public float weight = 1f;
}

/// <summary>
/// 보물 이벤트 1종 보상.
/// 중복 드랍을 허용하므로 같은 item이 여러 엔트리로 들어갈 수 있다.
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
    [Min(0)] public int soulAmount = 0;
    public bool soulGranted = false;

    public int Count => rewards != null ? rewards.Count : 0;
    public bool HasAnyReward => (rewards != null && rewards.Count > 0) || soulAmount > 0;

    public void Add(ItemDefinition item, int amount)
    {
        if (item == null || amount <= 0)
            return;

        if (rewards == null)
            rewards = new List<WorldTreasureRewardItemEntry>(4);

        rewards.Add(new WorldTreasureRewardItemEntry
        {
            item = item,
            amount = Mathf.Max(1, amount)
        });
    }
}
