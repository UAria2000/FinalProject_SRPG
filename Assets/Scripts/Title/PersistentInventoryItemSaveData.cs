using System;

[Serializable]
public class PersistentInventoryItemSaveData
{
    public string itemId;
    public int amount;
    public InventoryOwnershipType ownershipType = InventoryOwnershipType.Persistent;
}
