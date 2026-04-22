using System;

[Serializable]
public class WorldInventoryItemSaveData
{
    public string itemId;
    public int amount;
    public InventoryOwnershipType ownershipType = InventoryOwnershipType.WorldOnly;
}
