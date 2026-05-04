using System;
using System.Collections.Generic;

[Serializable]
public class WorldRunTransientState
{
    public List<InventoryStackData> inventory = new List<InventoryStackData>();
    public List<PrisonerRuntimeData> prisoners = new List<PrisonerRuntimeData>();
    public ItemDefinition sharedConsumableItem;
    public int worldEarnedSoulAlreadyGranted;
    public long nextPrisonerSequence = 1;
    public List<PartyEquipmentAssignmentData> partyEquipmentAssignments = new List<PartyEquipmentAssignmentData>();
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
        prisoners.Clear();
        sharedConsumableItem = null;
        worldEarnedSoulAlreadyGranted = 0;
        nextPrisonerSequence = 1;
        partyEquipmentAssignments.Clear();
    }

    public void AddItem(ItemDefinition item, int amount = 1)
    {
        if (item == null || amount <= 0)
            return;

        InventoryStackData existing = inventory.Find(x => x != null && x.item == item);
        if (existing != null)
            existing.amount += amount;
        else
            inventory.Add(new InventoryStackData { item = item, amount = amount });
    }

    public void AddPrisoner(UnitDefinition unit, int capturedLevel = 1)
    {
        if (unit == null)
            return;

        prisoners.Add(PrisonerRuntimeData.CreateFromCapturedUnit(unit, capturedLevel, nextPrisonerSequence++));
    }

    public void AddPrisonerFromItem(ItemDefinition prisonerItem, int capturedLevel = 1, UnitDefinition fallbackUnit = null)
    {
        if (prisonerItem == null && fallbackUnit == null)
            return;

        prisoners.Add(PrisonerRuntimeData.CreateFromPrisonerItem(prisonerItem, capturedLevel, nextPrisonerSequence++, fallbackUnit));
    }

    public void AddSoulEarnedInWorld(int amount)
    {
        worldEarnedSoulAlreadyGranted += Math.Max(0, amount);
    }
}
