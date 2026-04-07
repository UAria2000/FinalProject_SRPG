using System;
using System.Collections.Generic;

[Serializable]
public class WorldSettlementSummary
{
    public bool wasVictory;
    public int worldEarnedSoulAlreadyGranted;
    public int convertedItemSoul;
    public int convertedPrisonerSoul;
    public int sizeBonusPercent;
    public int difficultyBonusPercent;
    public int victoryBonusPercent;
    public int totalSettlementSoulAward;

    public readonly List<ItemDefinition> inventoryItems = new List<ItemDefinition>();
    public readonly List<UnitDefinition> prisonerUnits = new List<UnitDefinition>();
}
