using System;
using UnityEngine.Serialization;

[Serializable]
public class StatVarianceSaveData
{
    public int maxHpDelta;
    public int dmgDelta;
    public int spdDelta;
    public int idtDelta;
    [FormerlySerializedAs("hitDeltaX10")]
    public int hitDelta;
    [FormerlySerializedAs("acDeltaX10")]
    public int acDelta;
    public int criDelta;
    public int crdDelta;
    [FormerlySerializedAs("poisonResistDelta")]
    public int burnResistDelta;
    public int bleedResistDelta;
    public int stunResistDelta;
    public int frostResistDelta;
    public int blindResistDelta;

    public static StatVarianceSaveData FromRuntime(UnitInstanceStatVariance variance)
    {
        if (variance == null)
            return new StatVarianceSaveData();

        return new StatVarianceSaveData
        {
            maxHpDelta = variance.maxHpDelta,
            dmgDelta = variance.dmgDelta,
            spdDelta = variance.spdDelta,
            idtDelta = variance.idtDelta,
            hitDelta = variance.hitDelta,
            acDelta = variance.acDelta,
            criDelta = variance.criDelta,
            crdDelta = variance.crdDelta,
            burnResistDelta = variance.burnResistDelta,
            bleedResistDelta = variance.bleedResistDelta,
            stunResistDelta = variance.stunResistDelta,
            frostResistDelta = variance.frostResistDelta,
            blindResistDelta = variance.blindResistDelta,
        };
    }

    public UnitInstanceStatVariance ToRuntime()
    {
        return new UnitInstanceStatVariance
        {
            maxHpDelta = maxHpDelta,
            dmgDelta = dmgDelta,
            spdDelta = spdDelta,
            idtDelta = idtDelta,
            hitDelta = hitDelta,
            acDelta = acDelta,
            criDelta = criDelta,
            crdDelta = crdDelta,
            burnResistDelta = burnResistDelta,
            bleedResistDelta = bleedResistDelta,
            stunResistDelta = stunResistDelta,
            frostResistDelta = frostResistDelta,
            blindResistDelta = blindResistDelta,
        };
    }
}
