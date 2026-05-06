using System;
using UnityEngine.Serialization;

[Serializable]
public class StatVarianceSaveData
{
    public int maxHpDelta;
    public int dmgDelta;
    public int spdDelta;
    public int idtDelta;
    public int hitDeltaX10;
    public int acDeltaX10;
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
            hitDeltaX10 = variance.hitDeltaX10,
            acDeltaX10 = variance.acDeltaX10,
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
            hitDeltaX10 = hitDeltaX10,
            acDeltaX10 = acDeltaX10,
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
