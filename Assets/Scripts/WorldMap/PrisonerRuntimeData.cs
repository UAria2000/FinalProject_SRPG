using System;
using UnityEngine;

public enum PrisonerCorruptionConditionType
{
    BattleCount,
    KillCount,
    SpendSoul,
    EliteOrBossKill,
}

[Serializable]
public class PrisonerRuntimeData
{
    public string prisonerInstanceId;
    public UnitDefinition sourceUnit;
    public UnitViewDefinition sourceUnitViewDefinition;
    public ItemDefinition sourcePrisonerItem;
    public string prisonerNameOverride;
    public int capturedLevel = 1;
    public bool isExchangeable;
    public PrisonerCorruptionConditionType corruptionConditionType;
    [Min(1)] public int targetValue = 1;
    [Min(0)] public int currentValue = 0;
    public long captureSequence;

    public string GetDisplayName()
    {
        if (!string.IsNullOrWhiteSpace(prisonerNameOverride))
            return prisonerNameOverride;

        if (sourcePrisonerItem != null && !string.IsNullOrWhiteSpace(sourcePrisonerItem.itemName))
            return sourcePrisonerItem.itemName;

        return sourceUnit != null ? sourceUnit.unitName : "Unknown Prisoner";
    }

    public Sprite GetPortrait()
    {
        if (sourcePrisonerItem != null && sourcePrisonerItem.useItemIconAsPrisonerPortrait && sourcePrisonerItem.icon != null)
            return sourcePrisonerItem.icon;

        if (sourceUnit != null && sourceUnit.captureRewardItem != null && sourceUnit.captureRewardItem.icon != null)
            return sourceUnit.captureRewardItem.icon;

        if (sourceUnitViewDefinition != null)
            return sourceUnitViewDefinition.GetBustPortraitSprite();

        return null;
    }

    public bool IsReadyToCorrupt => currentValue >= targetValue;
    public bool RequiresSoulPayment => corruptionConditionType == PrisonerCorruptionConditionType.SpendSoul && !IsReadyToCorrupt;

    public float GetProgress01()
    {
        if (targetValue <= 0)
            return IsReadyToCorrupt ? 1f : 0f;

        return Mathf.Clamp01(currentValue / (float)targetValue);
    }

    public string GetConditionLabel()
    {
        switch (corruptionConditionType)
        {
            case PrisonerCorruptionConditionType.BattleCount:
                return $"전투 {currentValue}/{targetValue}";
            case PrisonerCorruptionConditionType.KillCount:
                return $"적 처치 {currentValue}/{targetValue}";
            case PrisonerCorruptionConditionType.SpendSoul:
                return IsReadyToCorrupt ? "소울 납부 완료" : $"소울 {targetValue}";
            case PrisonerCorruptionConditionType.EliteOrBossKill:
                return $"엘리트/보스 처치 {currentValue}/{targetValue}";
            default:
                return string.Empty;
        }
    }

    public void AddBattleProgress(int amount = 1)
    {
        if (corruptionConditionType != PrisonerCorruptionConditionType.BattleCount || IsReadyToCorrupt)
            return;

        currentValue = Mathf.Min(targetValue, currentValue + Mathf.Max(0, amount));
    }

    public void AddKillProgress(int amount = 1)
    {
        if (corruptionConditionType != PrisonerCorruptionConditionType.KillCount || IsReadyToCorrupt)
            return;

        currentValue = Mathf.Min(targetValue, currentValue + Mathf.Max(0, amount));
    }

    public void AddEliteOrBossKillProgress(int amount = 1)
    {
        if (corruptionConditionType != PrisonerCorruptionConditionType.EliteOrBossKill || IsReadyToCorrupt)
            return;

        currentValue = Mathf.Min(targetValue, currentValue + Mathf.Max(0, amount));
    }

    public void MarkSoulPaid()
    {
        if (corruptionConditionType != PrisonerCorruptionConditionType.SpendSoul)
            return;

        currentValue = targetValue;
    }

    public static PrisonerRuntimeData CreateFromPrisonerItem(ItemDefinition prisonerItem, int capturedLevel, long sequence, UnitDefinition fallbackUnit = null, UnitViewDefinition fallbackView = null, bool isExchangeable = false)
    {
        UnitDefinition sourceUnit = prisonerItem != null && prisonerItem.prisonerSourceUnitDefinition != null
            ? prisonerItem.prisonerSourceUnitDefinition
            : fallbackUnit;

        PrisonerRuntimeData data = CreateFromCapturedUnit(sourceUnit, capturedLevel, sequence, fallbackView, isExchangeable);
        data.sourcePrisonerItem = prisonerItem;
        if (prisonerItem != null && !string.IsNullOrWhiteSpace(prisonerItem.itemName))
            data.prisonerNameOverride = prisonerItem.itemName;
        return data;
    }

    public static PrisonerRuntimeData CreateFromCapturedUnit(UnitDefinition unit, int capturedLevel, long sequence, UnitViewDefinition viewDefinition = null, bool isExchangeable = false)
    {
        var data = new PrisonerRuntimeData();
        data.prisonerInstanceId = Guid.NewGuid().ToString("N");
        data.sourceUnit = unit;
        data.sourceUnitViewDefinition = viewDefinition;
        data.capturedLevel = Mathf.Max(1, capturedLevel);
        data.captureSequence = sequence;
        data.isExchangeable = isExchangeable || (unit != null && unit.isNftUnit);

        data.corruptionConditionType = (PrisonerCorruptionConditionType)UnityEngine.Random.Range(0, 4);
        switch (data.corruptionConditionType)
        {
            case PrisonerCorruptionConditionType.BattleCount:
                data.targetValue = UnityEngine.Random.Range(1, 6);
                break;
            case PrisonerCorruptionConditionType.KillCount:
                data.targetValue = UnityEngine.Random.Range(4, 16);
                break;
            case PrisonerCorruptionConditionType.SpendSoul:
                data.targetValue = UnityEngine.Random.Range(50, 201);
                break;
            case PrisonerCorruptionConditionType.EliteOrBossKill:
                data.targetValue = UnityEngine.Random.Range(1, 3);
                break;
            default:
                data.targetValue = 1;
                break;
        }

        data.currentValue = 0;
        return data;
    }
}
