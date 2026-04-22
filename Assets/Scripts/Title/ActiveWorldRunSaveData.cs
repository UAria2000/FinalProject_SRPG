using System;
using System.Collections.Generic;

[Serializable]
public class ActiveWorldRunSaveData
{
    public string ownerAccountId;
    public bool hasActiveWorld;

    public int worldSeed;
    public string difficultyId;
    public int mapRadius;
    public long createdUnixTime;

    public int currentTileId = -1;
    public int selectedTileId = -1;

    public List<WorldTileSaveData> tiles = new List<WorldTileSaveData>();
    public List<WorldQuestSaveData> activeQuests = new List<WorldQuestSaveData>();

    public List<WorldInventoryItemSaveData> worldInventory = new List<WorldInventoryItemSaveData>();
    public List<WorldEquipmentItemSaveData> worldEquipments = new List<WorldEquipmentItemSaveData>();
    public List<WorldEquipmentAssignmentSaveData> equipmentAssignments = new List<WorldEquipmentAssignmentSaveData>();

    public List<CapturedPrisonerSaveData> prisoners = new List<CapturedPrisonerSaveData>();
    public string sharedConsumableItemId;

    public List<WorldPartyMemberRuntimeSaveData> worldPartyMembers = new List<WorldPartyMemberRuntimeSaveData>();
}
