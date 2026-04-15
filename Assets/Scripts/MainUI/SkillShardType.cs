using System;
using System.Collections.Generic;
using UnityEngine;

public enum ClassShardType
{
    Melee,
    Mid,
    Ranged,
}

[Serializable]
public class ClassShardAmountData
{
    public ClassShardType shardType = ClassShardType.Melee;
    public int amount = 0;
}

[Serializable]
public class PersistentAccountCurrencyState
{
    public int cashCurrency = 0;
    public List<ClassShardAmountData> classShards = new List<ClassShardAmountData>();

    public int GetShardCount(ClassShardType type)
    {
        EnsureDefaults();
        for (int i = 0; i < classShards.Count; i++)
        {
            if (classShards[i] != null && classShards[i].shardType == type)
                return Mathf.Max(0, classShards[i].amount);
        }

        return 0;
    }

    public void AddShards(ClassShardType type, int amount)
    {
        if (amount == 0)
            return;

        EnsureDefaults();
        ClassShardAmountData entry = GetOrCreateEntry(type);
        entry.amount = Mathf.Max(0, entry.amount + amount);
    }

    public bool TrySpendShards(ClassShardType type, int amount)
    {
        int clamped = Mathf.Max(0, amount);
        if (clamped <= 0)
            return true;

        EnsureDefaults();
        ClassShardAmountData entry = GetOrCreateEntry(type);
        if (entry.amount < clamped)
            return false;

        entry.amount -= clamped;
        return true;
    }

    public void EnsureDefaults()
    {
        if (classShards == null)
            classShards = new List<ClassShardAmountData>();

        EnsureEntry(ClassShardType.Melee);
        EnsureEntry(ClassShardType.Mid);
        EnsureEntry(ClassShardType.Ranged);
    }

    private void EnsureEntry(ClassShardType type)
    {
        for (int i = 0; i < classShards.Count; i++)
        {
            if (classShards[i] != null && classShards[i].shardType == type)
                return;
        }

        classShards.Add(new ClassShardAmountData { shardType = type, amount = 0 });
    }

    private ClassShardAmountData GetOrCreateEntry(ClassShardType type)
    {
        EnsureEntry(type);
        for (int i = 0; i < classShards.Count; i++)
        {
            if (classShards[i] != null && classShards[i].shardType == type)
                return classShards[i];
        }

        ClassShardAmountData fallback = new ClassShardAmountData { shardType = type, amount = 0 };
        classShards.Add(fallback);
        return fallback;
    }
}
