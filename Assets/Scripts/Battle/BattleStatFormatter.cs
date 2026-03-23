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

    public static string FormatScaledX10ValueWithDelta(float finalValue, int rawDelta)
    {
        int finalDisplay = Mathf.RoundToInt(finalValue * 10f);
        int deltaDisplay = rawDelta * 10;

        if (deltaDisplay == 0)
            return finalDisplay.ToString();

        string color = deltaDisplay > 0 ? "#5BD45B" : "#FF6666";
        string sign = deltaDisplay > 0 ? "+" : "";
        return string.Format("{0} <color={1}>({2}{3})</color>", finalDisplay, color, sign, deltaDisplay);
    }

    public static string FormatPercent(int value)
    {
        return string.Format("{0}%", value);
    }
}
