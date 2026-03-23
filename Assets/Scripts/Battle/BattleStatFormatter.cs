using TMPro;
using UnityEngine;

public static class BattleStatFormatter
{
    public static string FormatIntValueWithDelta(int finalValue, int delta)
    {
        if (delta == 0)
            return finalValue.ToString();

        string color = delta > 0 ? "#5BD45B" : "#FF6666";
        string sign = delta > 0 ? "+" : "";
        return string.Format("{0} <color={1}>({2}{3})</color>", finalValue, color, sign, delta);
    }

    public static string FormatScaledX10ValueWithDelta(float finalValue, int deltaX10)
    {
        int finalDisplay = Mathf.RoundToInt(finalValue * 10f);
        if (deltaX10 == 0)
            return finalDisplay.ToString();

        string color = deltaX10 > 0 ? "#5BD45B" : "#FF6666";
        string sign = deltaX10 > 0 ? "+" : "";
        return string.Format("{0} <color={1}>({2}{3})</color>", finalDisplay, color, sign, deltaX10);
    }

    public static string FormatPercent(int value)
    {
        return string.Format("{0}%", value);
    }
}
