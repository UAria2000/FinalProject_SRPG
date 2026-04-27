using System;
using UnityEngine;

[Serializable]
public class WorldPartyMemberRuntimeSaveData
{
    public string unitInstanceId;
    public int currentLevel;
    public int currentExp;
    public int levelGrowthMaxHp;
    public int levelGrowthDmg;
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
            currentExp = Mathf.Max(0, member.currentExp),
            levelGrowthMaxHp = Mathf.Max(0, member.levelGrowthMaxHp),
            levelGrowthDmg = Mathf.Max(0, member.levelGrowthDmg),
            persistentCurrentHP = member.persistentCurrentHP,
            startSlotIndex = Mathf.Clamp(member.startSlotIndex, 0, 3),
        };
    }
}
