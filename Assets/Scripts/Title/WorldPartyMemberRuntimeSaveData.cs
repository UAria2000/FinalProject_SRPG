using System;
using UnityEngine;

[Serializable]
public class WorldPartyMemberRuntimeSaveData
{
    public string unitInstanceId;
    public int currentLevel;
    public int currentExp;
    public int persistentCurrentHP;
    public int startSlotIndex;

    public static WorldPartyMemberRuntimeSaveData FromRuntime(PartyMemberData member)
    {
        if (member == null)
            return null;

        return new WorldPartyMemberRuntimeSaveData
        {
            unitInstanceId = member.instanceId,
            currentLevel = Mathf.Max(1, member.currentLevel),
            currentExp = 0,
            persistentCurrentHP = member.persistentCurrentHP,
            startSlotIndex = Mathf.Clamp(member.startSlotIndex, 0, 3),
        };
    }
}
