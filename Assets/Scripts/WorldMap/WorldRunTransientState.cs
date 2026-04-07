using System;
using System.Collections.Generic;

[Serializable]
public class WorldRunTransientState
{
    public List<InventoryStackData> inventory = new List<InventoryStackData>();

    // 포로/장비 시스템이 월드맵 런 상태에 붙기 시작하면 여기에 보관하고 월드 시작 시 비운다.
    public List<string> prisonerRuntimeKeys = new List<string>();
    public List<string> temporaryEquipmentRuntimeKeys = new List<string>();

    public static WorldRunTransientState CreateForNewWorld(PartyDefinition playerPartyTemplate)
    {
        WorldRunTransientState state = new WorldRunTransientState();

        if (playerPartyTemplate != null)
            state.inventory = playerPartyTemplate.CreateInventoryRuntime();

        return state;
    }

    public void ResetForNewWorld(PartyDefinition playerPartyTemplate)
    {
        inventory = playerPartyTemplate != null
            ? playerPartyTemplate.CreateInventoryRuntime()
            : new List<InventoryStackData>();

        prisonerRuntimeKeys.Clear();
        temporaryEquipmentRuntimeKeys.Clear();
    }
}
