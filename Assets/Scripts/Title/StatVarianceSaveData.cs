using System;
using UnityEngine;

[Serializable]
public class StatVarianceSaveData
{
    public int maxHpDelta;
    public int dmgDelta;
    public int spdDelta;
    public int hitDeltaX10;
    public int acDeltaX10;
    public int criDelta;
    public int crdDelta;
    public int poisonResistDelta;
    public int bleedResistDelta;
    public int stunResistDelta;

    public static StatVarianceSaveData FromRuntime(UnitInstanceStatVariance variance)
    {
        if (variance == null)
            return new StatVarianceSaveData();

        return new StatVarianceSaveData
        {
            maxHpDelta = variance.maxHpDelta,
            dmgDelta = variance.dmgDelta,
            spdDelta = variance.spdDelta,
            hitDeltaX10 = variance.hitDeltaX10,
            acDeltaX10 = variance.acDeltaX10,
            criDelta = variance.criDelta,
            crdDelta = variance.crdDelta,
            poisonResistDelta = variance.poisonResistDelta,
            bleedResistDelta = variance.bleedResistDelta,
            stunResistDelta = variance.stunResistDelta,
        };
    }

    public UnitInstanceStatVariance ToRuntime()
    {
        return new UnitInstanceStatVariance
        {
            maxHpDelta = maxHpDelta,
            dmgDelta = dmgDelta,
            spdDelta = spdDelta,
            hitDeltaX10 = hitDeltaX10,
            acDeltaX10 = acDeltaX10,
            criDelta = criDelta,
            crdDelta = crdDelta,
            poisonResistDelta = poisonResistDelta,
            bleedResistDelta = bleedResistDelta,
            stunResistDelta = stunResistDelta,
        };
    }
}
