using System;
using System.Collections.Generic;

[Serializable]
public class WorldRunTransientState
{
    public List<InventoryStackData> inventory = new List<InventoryStackData>();
    public List<UnitDefinition> capturedPrisoners = new List<UnitDefinition>();
    public int worldEarnedSoulAlreadyGranted;

    public static WorldRunTransientState CreateForNewWorld(PartyDefinition playerPartyTemplate)
    {
        WorldRunTransientState state = new WorldRunTransientState();
        if (playerPartyTemplate != null)
            state.inventory = playerPartyTemplate.CreateInventoryRuntime();
        return state;
    }

    public void ResetForNewWorld(PartyDefinition playerPartyTemplate)
    {
        inventory = playerPartyTemplate != null ? playerPartyTemplate.CreateInventoryRuntime() : new List<InventoryStackData>();
        capturedPrisoners.Clear();
        worldEarnedSoulAlreadyGranted = 0;
    }

    public void AddItem(ItemDefinition item, int amount = 1)
    {
        if (item == null || amount <= 0)
            return;

        InventoryStackData existing = inventory.Find(x => x != null && x.item == item);
        if (existing != null) existing.amount += amount;
        else inventory.Add(new InventoryStackData { item = item, amount = amount });
    }

    public void AddPrisoner(UnitDefinition unit)
    {
        if (unit != null) capturedPrisoners.Add(unit);
    }

    public void AddSoulEarnedInWorld(int amount)
    {
        worldEarnedSoulAlreadyGranted += Math.Max(0, amount);
    }
}
