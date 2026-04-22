using System;
using System.Collections.Generic;

[Serializable]
public class AccountProfileSaveData
{
    public string accountId;
    public string nickname;
    public long lastSavedUnixTime;

    public AccountCurrencySaveData currencies = new AccountCurrencySaveData();
    public List<PersistentInventoryItemSaveData> persistentInventory = new List<PersistentInventoryItemSaveData>();
    public List<RosterUnitSaveData> rosterUnits = new List<RosterUnitSaveData>();

    // battle slot index order: 0,1,2,3
    public List<string> activePartyUnitInstanceIds = new List<string>();

    public ProfileUpgradeSaveData upgrades = new ProfileUpgradeSaveData();
    public long nextObtainedOrder = 1;
}
