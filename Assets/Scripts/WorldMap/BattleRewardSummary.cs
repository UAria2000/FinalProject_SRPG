using System;
using System.Collections.Generic;

[Serializable]
public class BattleRewardSummary
{
    public int soulReward;
    public int expReward;

    public readonly List<UnitDefinition> defeatedEnemyUnits = new List<UnitDefinition>();
    public readonly List<ItemDefinition> droppedItems = new List<ItemDefinition>();
    public readonly List<UnitDefinition> capturedPrisoners = new List<UnitDefinition>();

    public void Clear()
    {
        soulReward = 0;
        expReward = 0;
        defeatedEnemyUnits.Clear();
        droppedItems.Clear();
        capturedPrisoners.Clear();
    }
}
