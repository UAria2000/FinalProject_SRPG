using UnityEngine;

public static class LegionFormula
{
    public static int GetExpToNextLevel(int currentLevel)
    {
        int level = Mathf.Max(1, currentLevel);
        float x = level - 1;
        return Mathf.Max(1, Mathf.RoundToInt(30f + 18f * x + 7f * Mathf.Pow(x, 1.35f)));
    }

    public static int GetSoulCostToFillMissingExp(PersistentRosterUnitData unit, int levelCap, float soulPerMissingExp)
    {
        if (unit == null)
            return 0;

        if (unit.currentLevel >= Mathf.Max(1, levelCap))
            return 0;

        int needExp = GetExpToNextLevel(unit.currentLevel);
        int clampedExp = Mathf.Clamp(unit.currentExp, 0, needExp);
        int missingExp = Mathf.Max(0, needExp - clampedExp);
        return Mathf.CeilToInt(missingExp * Mathf.Max(0f, soulPerMissingExp));
    }

    public static int GetRemainingSoulCostToNextLevel(PersistentRosterUnitData unit, int levelCap)
    {
        return GetSoulCostToFillMissingExp(unit, levelCap, 1f);
    }

    public static int GetTotalSoulCostToReachLevel(int targetLevel)
    {
        int level = Mathf.Max(1, targetLevel);
        int total = 0;
        for (int lv = 1; lv < level; lv++)
            total += GetExpToNextLevel(lv);
        return Mathf.Max(0, total);
    }

    public static int GetScaledEnemySoulReward(UnitDefinition definition, int enemyLevel, float soulRewardIncreasePercentPerLevel)
    {
        if (definition == null)
            return 0;

        int baseSoul = Mathf.Max(0, definition.baseSoulReward);
        int level = Mathf.Max(1, enemyLevel);
        float multiplier = 1f + Mathf.Max(0f, soulRewardIncreasePercentPerLevel) * 0.01f * (level - 1);
        return Mathf.Max(0, Mathf.RoundToInt(baseSoul * multiplier));
    }

    public static int GetEnemyExpReward(UnitDefinition definition, int enemyLevel, float soulRewardIncreasePercentPerLevel, float expPercentOfScaledSoulReward)
    {
        int scaledSoul = GetScaledEnemySoulReward(definition, enemyLevel, soulRewardIncreasePercentPerLevel);
        return Mathf.Max(0, Mathf.RoundToInt(scaledSoul * Mathf.Max(0f, expPercentOfScaledSoulReward) * 0.01f));
    }

    public const int MinPromotionRank = 1;
    public const int MaxPromotionRank = 9;

    public static int GetPromotionCost(int currentRank)
    {
        int rank = ClampLegionRank(currentRank);
        if (rank >= MaxPromotionRank)
            return 0;

        // 기본 랭크 1은 무료 시작값이다.
        // Rank 1 -> 2 비용 2, Rank 2 -> 3 비용 4 ...
        return Mathf.RoundToInt(Mathf.Pow(2f, rank));
    }

    public static int GetTotalInvestedPromotionShards(int currentRank)
    {
        int rank = ClampLegionRank(currentRank);
        int total = 0;
        for (int r = MinPromotionRank; r < rank; r++)
            total += GetPromotionCost(r);
        return total;
    }

    public static int GetDecomposeRefundPromotionShards(int currentRank)
    {
        return Mathf.FloorToInt(GetTotalInvestedPromotionShards(currentRank) * 0.5f);
    }

    public static int GetBaseDecomposeShardReward(PersistentRosterUnitData unit)
    {
        if (unit == null || unit.unitDefinition == null)
            return 1;

        return Mathf.Max(1, unit.unitDefinition.decomposeShardReward);
    }

    public static int GetTotalDecomposeShardReward(PersistentRosterUnitData unit)
    {
        if (unit == null)
            return 0;

        return Mathf.Max(1, GetBaseDecomposeShardReward(unit))
             + Mathf.Max(0, GetDecomposeRefundPromotionShards(unit.promotionRank));
    }

    public static int GetDecomposeSoulReward(PersistentRosterUnitData unit)
    {
        if (unit == null)
            return 0;

        int totalCostToLevel = GetTotalSoulCostToReachLevel(unit.currentLevel);
        return Mathf.FloorToInt(totalCostToLevel * 0.25f);
    }

    public static int ClampLegionRank(int rank)
    {
        return Mathf.Clamp(rank <= 0 ? MinPromotionRank : rank, MinPromotionRank, MaxPromotionRank);
    }

    public static bool IsMaxPromotionRank(int rank)
    {
        return ClampLegionRank(rank) >= MaxPromotionRank;
    }

    public static float GetPromotionMultiplier(int rank, float promotionPercentPerRank)
    {
        // 랭크 1은 기본 상태라 능력치 보너스가 없다.
        int paidRanks = Mathf.Max(0, ClampLegionRank(rank) - MinPromotionRank);
        return 1f + paidRanks * Mathf.Max(0f, promotionPercentPerRank) * 0.01f;
    }

    public static string FormatLevelWithOriginal(PersistentRosterUnitData unit)
    {
        if (unit == null)
            return "-";

        return $"{unit.currentLevel}({unit.originalLevel})";
    }

    public static string GetPromotionShardLabel() => "유닛 파편";
}

public struct LegionEquipmentBonusSummary
{
    public int maxHp;
    public int dmg;
    public int spd;
    public int hitX10;
    public int acX10;
    public int cri;
    public int crd;

    // poisonRes는 구버전 UI 호환용. 신규 정책에서는 burnRes와 같은 값으로 취급한다.
    public int poisonRes;
    public int burnRes;
    public int bleedRes;
    public int stunRes;
    public int frostRes;
    public int blindRes;
}
