using System;
using UnityEngine;

[Obsolete("클래스별/스킬별 샤드는 폐기되었습니다. 모든 호출은 공용 유닛 파편으로 처리됩니다.")]
public enum ClassShardType
{
    Melee,
    Mid,
    Ranged,
}

[Serializable]
[Obsolete("클래스별/스킬별 샤드는 폐기되었습니다. 저장/신규 로직에서 사용하지 않습니다.")]
public class ClassShardAmountData
{
    public ClassShardType shardType = ClassShardType.Melee;
    public int amount = 0;
}

[Serializable]
public class PersistentAccountCurrencyState
{
    public int cashCurrency = 0;

    [Header("Common Legion Currency")]
    [Tooltip("전 유닛 공통 승급/분해 파편. 기존 클래스별/스킬별 샤드는 더 이상 사용하지 않는다.")]
    public int unitShardCurrency = 0;

    public int GetCommonShardCount()
    {
        EnsureDefaults();
        return Mathf.Max(0, unitShardCurrency);
    }

    public void SetCommonShardCount(int amount)
    {
        unitShardCurrency = Mathf.Max(0, amount);
    }

    public void AddCommonShards(int amount)
    {
        if (amount == 0)
            return;

        unitShardCurrency = Mathf.Max(0, unitShardCurrency + amount);
    }

    public bool TrySpendCommonShards(int amount)
    {
        int clamped = Mathf.Max(0, amount);
        if (clamped <= 0)
            return true;

        EnsureDefaults();
        if (unitShardCurrency < clamped)
            return false;

        unitShardCurrency -= clamped;
        return true;
    }

    public int GetShardCount(ClassShardType type)
    {
        // 신규 정책: 파편은 전 유닛 공통이다. type은 호환용으로만 남긴다.
        return GetCommonShardCount();
    }

    public void AddShards(ClassShardType type, int amount)
    {
        // 신규 정책: 파편은 전 유닛 공통이다. type은 호환용으로만 남긴다.
        AddCommonShards(amount);
    }

    public bool TrySpendShards(ClassShardType type, int amount)
    {
        // 신규 정책: 파편은 전 유닛 공통이다. type은 호환용으로만 남긴다.
        return TrySpendCommonShards(amount);
    }

    public int GetLegacyClassShardCount(ClassShardType type)
    {
        return 0;
    }

    public int GetLegacyClassShardTotal()
    {
        return 0;
    }

    public void ClearLegacyClassShards()
    {
        // 클래스별/스킬별 샤드는 폐기. 저장 데이터 이관도 하지 않는다.
    }

    public void EnsureDefaults()
    {
        cashCurrency = Mathf.Max(0, cashCurrency);
        unitShardCurrency = Mathf.Max(0, unitShardCurrency);
    }
}
