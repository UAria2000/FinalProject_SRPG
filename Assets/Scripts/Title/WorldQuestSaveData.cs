using System;
using System.Collections.Generic;

[Serializable]
public class WorldQuestSaveData
{
    public string questId;
    public int sourceTileId;
    public int assignedTargetTileId;

    public int currentProgress;
    public int targetProgress;

    public bool isAccepted;
    public bool isCancelled;
    public bool isCompleted;

    public bool completionPopupQueued;
    public bool completionPopupShown;
    public bool completionPopupClosed;

    public bool soulGranted;
    public bool experienceGranted;

    public List<bool> itemClaimed = new List<bool>();

    public static WorldQuestSaveData FromRuntime(WorldQuestState quest)
    {
        if (quest == null)
            return null;

        WorldQuestSaveData data = new WorldQuestSaveData
        {
            questId = quest.definition != null ? quest.definition.questId : string.Empty,
            sourceTileId = quest.sourceTileId,
            assignedTargetTileId = quest.assignedTargetTileId,
            currentProgress = quest.currentProgress,
            targetProgress = quest.targetProgress,
            isAccepted = quest.isAccepted,
            isCancelled = quest.isCancelled,
            isCompleted = quest.isCompleted,
            completionPopupQueued = quest.completionPopupQueued,
            completionPopupShown = quest.completionPopupShown,
            completionPopupClosed = quest.completionPopupClosed,
            soulGranted = quest.soulGranted,
            experienceGranted = quest.experienceGranted,
            itemClaimed = new List<bool>(quest.itemClaimed ?? new List<bool>()),
        };

        return data;
    }
}
