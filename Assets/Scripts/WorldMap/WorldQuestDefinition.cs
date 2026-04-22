using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WorldQuestDefinition
{
    [Header("Identity")]
    public string questId;
    public string displayName;
    [TextArea(2, 5)] public string description;

    [Header("Rules")]
    public WorldQuestType questType;
    [Min(1)] public int targetCount = 1;
    public bool enabled = true;

    [Header("Rewards")]
    [Min(0)] public int soulReward = 0;
    [Min(0)] public int experienceReward = 0;
    public List<WorldQuestRewardItemEntry> itemRewards = new List<WorldQuestRewardItemEntry>(4);
}