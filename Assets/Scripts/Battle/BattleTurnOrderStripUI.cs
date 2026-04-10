using System.Collections.Generic;
using UnityEngine;

public class BattleTurnOrderStripUI : MonoBehaviour
{
    [SerializeField] private RectTransform slotsRoot;
    [SerializeField] private BattleTurnOrderPortraitUI portraitPrefab;
    [SerializeField] private int maxPortraits = 8;

    private readonly List<BattleTurnOrderPortraitUI> runtimePortraits = new List<BattleTurnOrderPortraitUI>();
    private BattlePresentationController owner;

    public void Initialize(BattlePresentationController presentationController)
    {
        owner = presentationController;
        EnsurePortraits();
    }

    public void Refresh(IReadOnlyList<BattleUnit> order, int currentCursor)
    {
        EnsurePortraits();

        for (int i = 0; i < runtimePortraits.Count; i++)
        {
            BattleUnit unit = order != null && i < order.Count ? order[i] : null;
            if (unit == null)
            {
                runtimePortraits[i].gameObject.SetActive(false);
                continue;
            }

            bool isCurrent = i == currentCursor;
            bool isFinished = currentCursor >= 0 && i < currentCursor;
            bool isUpcoming = currentCursor >= 0 && i > currentCursor;
            runtimePortraits[i].Bind(this, unit, i, isCurrent, isFinished, isUpcoming);
        }
    }

    public void HandlePortraitClicked(BattleUnit unit)
    {
        owner?.SelectUnitForInfo(unit);
    }

    private void EnsurePortraits()
    {
        if (slotsRoot == null || portraitPrefab == null)
            return;

        runtimePortraits.Clear();
        for (int i = 0; i < slotsRoot.childCount; i++)
        {
            BattleTurnOrderPortraitUI existing = slotsRoot.GetChild(i).GetComponent<BattleTurnOrderPortraitUI>();
            if (existing != null)
                runtimePortraits.Add(existing);
        }

        while (runtimePortraits.Count < maxPortraits)
        {
            BattleTurnOrderPortraitUI created = Object.Instantiate(portraitPrefab, slotsRoot);
            created.name = $"TurnOrderPortrait_{runtimePortraits.Count:00}";
            runtimePortraits.Add(created);
        }

        for (int i = 0; i < runtimePortraits.Count; i++)
            runtimePortraits[i].gameObject.SetActive(false);
    }
}
