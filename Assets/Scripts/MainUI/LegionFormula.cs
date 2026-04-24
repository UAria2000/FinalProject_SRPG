using UnityEngine;

public static class LegionFormula
{
    public static int GetExpToNextLevel(int currentLevel)
    {
        int level = Mathf.Max(1, currentLevel);
        float x = level - 1;
        return Mathf.Max(1, Mathf.RoundToInt(30f + 18f * x + 7f * Mathf.Pow(x, 1.35f)));
    }

    public static int GetRemainingSoulCostToNextLevel(PersistentRosterUnitData unit, int mainCharacterLevelCap)
    {
        if (unit == null)
            return 0;

        if (unit.currentLevel >= Mathf.Max(1, mainCharacterLevelCap))
            return 0;

        int needExp = GetExpToNextLevel(unit.currentLevel);
        int clampedExp = Mathf.Clamp(unit.currentExp, 0, needExp);
        int baseCost = Mathf.Max(0, needExp - clampedExp);

        if (unit.currentLevel < unit.originalLevel)
            return Mathf.CeilToInt(baseCost * 0.5f);

        return baseCost;
    }

    public static int GetPromotionCost(int currentRank)
    {
        int rank = Mathf.Max(0, currentRank);
        return Mathf.RoundToInt(Mathf.Pow(2f, rank + 1));
    }

    public static int GetTotalInvestedPromotionShards(int currentRank)
    {
        int rank = Mathf.Max(0, currentRank);
        int total = 0;
        for (int r = 0; r < rank; r++)
            total += GetPromotionCost(r);
        return total;
    }

    public static int GetDecomposeRefundPromotionShards(int currentRank)
    {
        return Mathf.FloorToInt(GetTotalInvestedPromotionShards(currentRank) * 0.5f);
    }

    public static float GetPromotionMultiplier(int rank, float promotionPercentPerRank)
    {
        return 1f + Mathf.Max(0, rank) * Mathf.Max(0f, promotionPercentPerRank) * 0.01f;
    }

    public static string FormatLevelWithOriginal(PersistentRosterUnitData unit)
    {
        if (unit == null)
            return "-";

        return $"{unit.currentLevel}({unit.originalLevel})";
    }

    public static string GetPromotionShardLabel() => "승급 파편";
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
    public int poisonRes;
    public int bleedRes;
    public int stunRes;
}
