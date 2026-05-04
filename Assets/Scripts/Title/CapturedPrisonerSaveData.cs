using System;
using UnityEngine;

[System.Serializable]
public class CapturedPrisonerSaveData
{
    public string prisonerInstanceId;
    public string sourceUnitId;
    public string sourcePrisonerItemId;
    public string prisonerNameOverride;
    public int capturedLevel = 1;
    public bool isExchangeable;
    public int corruptionConditionType;
    public int targetValue = 1;
    public int currentValue = 0;
    public long captureSequence;

    public static CapturedPrisonerSaveData FromRuntime(PrisonerRuntimeData runtime)
    {
        if (runtime == null)
            return null;

        return new CapturedPrisonerSaveData
        {
            prisonerInstanceId = runtime.prisonerInstanceId,
            sourceUnitId = runtime.sourceUnit != null ? runtime.sourceUnit.unitId : string.Empty,
            sourcePrisonerItemId = runtime.sourcePrisonerItem != null ? runtime.sourcePrisonerItem.itemId : string.Empty,
            prisonerNameOverride = runtime.prisonerNameOverride,
            capturedLevel = runtime.capturedLevel,
            isExchangeable = runtime.isExchangeable,
            corruptionConditionType = (int)runtime.corruptionConditionType,
            targetValue = runtime.targetValue,
            currentValue = runtime.currentValue,
            captureSequence = runtime.captureSequence,
        };
    }
}
