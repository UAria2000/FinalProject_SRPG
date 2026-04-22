using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WorldQuestState
{
    public WorldQuestDefinition definition;
    public int sourceTileId = -1;

    // CaptureSpecificTile 용
    public int assignedTargetTileId = -1;

    public int currentProgress = 0;
    public int targetProgress = 1;

    public bool isAccepted = false;
    public bool isCancelled = false;
    public bool isCompleted = false;

    // 완료 후 팝업/보상 처리용
    public bool completionPopupQueued = false;
    public bool completionPopupShown = false;
    public bool completionPopupClosed = false;

    // 즉시 지급 보상
    public bool soulGranted = false;
    public bool experienceGranted = false;

    public List<bool> itemClaimed = new List<bool>();

    public void Initialize(WorldQuestDefinition questDefinition, int tileId)
    {
        definition = questDefinition;
        sourceTileId = tileId;
        assignedTargetTileId = -1;
        currentProgress = 0;
        targetProgress = questDefinition != null ? Mathf.Max(1, questDefinition.targetCount) : 1;
        isAccepted = false;
        isCancelled = false;
        isCompleted = false;
        completionPopupQueued = false;
        completionPopupShown = false;
        completionPopupClosed = false;
        soulGranted = false;
        experienceGranted = false;

        itemClaimed.Clear();
        int rewardCount = questDefinition != null && questDefinition.itemRewards != null
            ? questDefinition.itemRewards.Count
            : 0;

        for (int i = 0; i < rewardCount; i++)
            itemClaimed.Add(false);
    }

    public string GetProgressText()
    {
        if (definition == null)
            return string.Empty;

        switch (definition.questType)
        {
            case WorldQuestType.KillEnemies:
                return $"적 {targetProgress}기 처치 ({currentProgress}/{targetProgress})";

            case WorldQuestType.CaptureSpecificTile:
                return isCompleted
                    ? "지정지역 점령하기 (완료)"
                    : "지정지역 점령하기";

            case WorldQuestType.WinEliteBattle:
                return $"정예 전투 {targetProgress}회 승리 ({currentProgress}/{targetProgress})";

            case WorldQuestType.WinBossBattle:
                return $"보스 전투 {targetProgress}회 승리 ({currentProgress}/{targetProgress})";

            default:
                return definition.displayName;
        }
    }

    public string GetListProgressTextRich()
    {
        if (definition == null)
            return string.Empty;

        switch (definition.questType)
        {
            case WorldQuestType.CaptureSpecificTile:
                return isCompleted
                    ? "<color=#FF4B4B>지정지역</color> 점령하기 (완료)"
                    : "<color=#FF4B4B>지정지역</color> 점령하기";

            default:
                return GetProgressText();
        }
    }

    public string GetDetailDescription()
    {
        if (definition == null)
            return string.Empty;

        if (!string.IsNullOrEmpty(definition.description))
            return definition.description;

        return GetProgressText();
    }

    public void AddProgress(int amount)
    {
        if (isCancelled || isCompleted || definition == null)
            return;

        currentProgress = Mathf.Clamp(currentProgress + Mathf.Max(0, amount), 0, targetProgress);

        if (currentProgress >= targetProgress)
            isCompleted = true;
    }

    public void MarkCompleted()
    {
        if (isCancelled || definition == null)
            return;

        currentProgress = targetProgress;
        isCompleted = true;
    }

    public bool CanClaimItemAt(int index)
    {
        if (definition == null || definition.itemRewards == null)
            return false;

        if (index < 0 || index >= definition.itemRewards.Count)
            return false;

        if (index >= itemClaimed.Count)
            return false;

        return !itemClaimed[index];
    }

    public void MarkItemClaimed(int index)
    {
        if (index < 0 || index >= itemClaimed.Count)
            return;

        itemClaimed[index] = true;
    }

    public bool AreAllItemsClaimed()
    {
        if (definition == null || definition.itemRewards == null || definition.itemRewards.Count == 0)
            return true;

        for (int i = 0; i < itemClaimed.Count; i++)
        {
            if (!itemClaimed[i])
                return false;
        }

        return true;
    }

    public bool HasAnyUnclaimedItemRewards()
    {
        if (definition == null || definition.itemRewards == null || definition.itemRewards.Count == 0)
            return false;

        for (int i = 0; i < itemClaimed.Count; i++)
        {
            if (!itemClaimed[i])
                return true;
        }

        return false;
    }
}