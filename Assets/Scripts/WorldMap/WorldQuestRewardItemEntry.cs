using System;
using UnityEngine;

[Serializable]
public class WorldQuestRewardItemEntry
{
    public ItemDefinition item;
    [Min(1)] public int amount = 1;
}