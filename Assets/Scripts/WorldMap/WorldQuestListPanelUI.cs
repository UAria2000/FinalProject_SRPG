using System.Collections.Generic;
using UnityEngine;

public class WorldQuestListPanelUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform rowRoot;
    [SerializeField] private WorldQuestRowUI rowPrefab;
    [SerializeField] private int maxRows = 5;

    private readonly List<WorldQuestRowUI> rowInstances = new List<WorldQuestRowUI>();
    private WorldQuestController owner;

    private void Awake()
    {
        EnsureRows();
    }

    public void Bind(WorldQuestController controller)
    {
        owner = controller;
        EnsureRows();
        Refresh();
    }

    public void Refresh()
    {
        EnsureRows();

        IReadOnlyList<WorldQuestState> activeQuests =
            owner != null ? owner.GetVisibleQuestList() : null;

        for (int i = 0; i < rowInstances.Count; i++)
        {
            WorldQuestRowUI row = rowInstances[i];
            if (row == null)
                continue;

            bool visible = activeQuests != null && i < activeQuests.Count;
            WorldQuestState quest = visible ? activeQuests[i] : null;
            row.Bind(this, quest, visible);
        }
    }

    public void HandleQuestRowClicked(WorldQuestState quest)
    {
        owner?.OpenQuestFromList(quest);
    }

    public void HandleQuestCancelClicked(WorldQuestState quest)
    {
        owner?.CancelQuestFromList(quest);
    }

    private void EnsureRows()
    {
        if (rowRoot == null || rowPrefab == null)
            return;

        rowInstances.RemoveAll(r => r == null);

        while (rowInstances.Count < maxRows)
        {
            WorldQuestRowUI instance = Instantiate(rowPrefab, rowRoot);
            instance.name = $"QuestRow_{rowInstances.Count}";
            rowInstances.Add(instance);
        }
    }
}