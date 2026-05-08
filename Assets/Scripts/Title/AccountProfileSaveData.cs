using System;
using System.Collections.Generic;

public enum WorldSettlementResultState
{
    None = 0,
    Victory = 1,
    Failure = 2
}

[Serializable]
public class AccountProfileSaveData
{
    public string accountId;
    public string nickname;
    public long lastSavedUnixTime;

    public AccountCurrencySaveData currencies = new AccountCurrencySaveData();
    public List<PersistentInventoryItemSaveData> persistentInventory = new List<PersistentInventoryItemSaveData>();
    public List<RosterUnitSaveData> rosterUnits = new List<RosterUnitSaveData>();
    public List<RosterUnitSaveData> graveyardUnits = new List<RosterUnitSaveData>();

    // battle slot index order: 0,1,2,3
    public List<string> activePartyUnitInstanceIds = new List<string>();

    public ProfileUpgradeSaveData upgrades = new ProfileUpgradeSaveData();
    public long nextObtainedOrder = 1;

    public WorldSettlementResultState lastWorldSettlementResult = WorldSettlementResultState.None;
}
