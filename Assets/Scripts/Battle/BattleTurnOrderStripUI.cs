using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Top-left turn order strip.
/// Shows current round, alive units in current turn order, current-turn yellow frame, finished-turn dim,
/// and focuses the unit when a portrait is clicked.
/// </summary>
[DisallowMultipleComponent]
public class BattleTurnOrderStripUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform slotsRoot;
    [SerializeField] private BattleTurnOrderPortraitUI portraitPrefab;
    [SerializeField] private BattleManager battleManager;

    [Header("Round Display")]
    [SerializeField] private GameObject roundRoot;
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private string roundTextFormat = "{0}";

    [Header("Slots")]
    [Min(1)] [SerializeField] private int maxPortraits = 12;
    [SerializeField] private bool leftAlign = true;
    [SerializeField] private bool configureHorizontalLayoutGroup = true;
    [SerializeField] private float slotSpacing = -10f;
    [SerializeField] private Vector2 slotSize = new Vector2(116f, 136f);

    private readonly List<BattleTurnOrderPortraitUI> runtimePortraits = new List<BattleTurnOrderPortraitUI>();
    private readonly List<BattleUnit> visibleOrderBuffer = new List<BattleUnit>();
    private BattlePresentationController owner;

    public void Initialize(BattlePresentationController presentationController)
    {
        owner = presentationController;

        if (battleManager == null)
            battleManager = FindFirstObjectByType<BattleManager>();

        ConfigureLayout();
        EnsurePortraits();
        RefreshRoundText();
    }

    public void Refresh(IReadOnlyList<BattleUnit> order, int currentCursor)
    {
        if (battleManager == null)
            battleManager = FindFirstObjectByType<BattleManager>();

        ConfigureLayout();
        EnsurePortraits();
        RefreshRoundText();
        BuildVisibleOrder(order);

        for (int i = 0; i < runtimePortraits.Count; i++)
        {
            BattleUnit unit = i < visibleOrderBuffer.Count ? visibleOrderBuffer[i] : null;
            if (unit == null)
            {
                runtimePortraits[i].gameObject.SetActive(false);
                continue;
            }

            bool isCurrent = IsCurrentUnit(unit, currentCursor, order);
            bool isFinished = IsFinishedUnit(unit, currentCursor, order);
            bool isUpcoming = !isCurrent && !isFinished;

            runtimePortraits[i].Bind(this, unit, i, isCurrent, isFinished, isUpcoming);
        }
    }

    public void HandlePortraitClicked(BattleUnit unit)
    {
        if (unit == null || unit.IsDead)
            return;

        owner?.SelectUnitForInfo(unit);
    }

    private void BuildVisibleOrder(IReadOnlyList<BattleUnit> order)
    {
        visibleOrderBuffer.Clear();
        if (order == null)
            return;

        for (int i = 0; i < order.Count; i++)
        {
            BattleUnit unit = order[i];
            if (!ShouldShowUnit(unit))
                continue;

            visibleOrderBuffer.Add(unit);
        }
    }

    private bool ShouldShowUnit(BattleUnit unit)
    {
        if (unit == null || unit.IsDead)
            return false;

        if (battleManager != null && !battleManager.IsUnitInBattle(unit))
            return false;

        return true;
    }

    private bool IsCurrentUnit(BattleUnit unit, int currentCursor, IReadOnlyList<BattleUnit> order)
    {
        if (battleManager != null && battleManager.CurrentActingUnit == unit)
            return true;

        if (order == null || currentCursor < 0 || currentCursor >= order.Count)
            return false;

        return order[currentCursor] == unit;
    }

    private bool IsFinishedUnit(BattleUnit unit, int currentCursor, IReadOnlyList<BattleUnit> order)
    {
        if (unit == null)
            return false;

        if (battleManager != null)
            return battleManager.HasUnitFinishedTurnThisRound(unit);

        if (order == null || currentCursor < 0)
            return false;

        int index = IndexOf(order, unit);
        return index >= 0 && index < currentCursor;
    }

    private int IndexOf(IReadOnlyList<BattleUnit> order, BattleUnit unit)
    {
        if (order == null || unit == null)
            return -1;

        for (int i = 0; i < order.Count; i++)
        {
            if (order[i] == unit)
                return i;
        }

        return -1;
    }

    private void RefreshRoundText()
    {
        int round = battleManager != null ? Mathf.Max(1, battleManager.CurrentRound) : 1;

        if (roundRoot != null)
            roundRoot.SetActive(true);

        if (roundText != null)
            roundText.text = string.Format(string.IsNullOrEmpty(roundTextFormat) ? "{0}" : roundTextFormat, round);
    }

    private void ConfigureLayout()
    {
        if (slotsRoot == null || !configureHorizontalLayoutGroup)
            return;

        HorizontalLayoutGroup layout = slotsRoot.GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
            layout = slotsRoot.gameObject.AddComponent<HorizontalLayoutGroup>();

        layout.childAlignment = leftAlign ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;
        layout.spacing = slotSpacing;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childScaleWidth = false;
        layout.childScaleHeight = false;
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
            {
                ApplySlotSize(existing);
                runtimePortraits.Add(existing);
            }
        }

        while (runtimePortraits.Count < maxPortraits)
        {
            BattleTurnOrderPortraitUI created = Instantiate(portraitPrefab, slotsRoot);
            created.name = $"TurnOrderPortrait_{runtimePortraits.Count:00}";
            ApplySlotSize(created);
            runtimePortraits.Add(created);
        }

        for (int i = 0; i < runtimePortraits.Count; i++)
            runtimePortraits[i].gameObject.SetActive(false);
    }

    private void ApplySlotSize(BattleTurnOrderPortraitUI slot)
    {
        if (slot == null)
            return;

        RectTransform rt = slot.GetComponent<RectTransform>();
        if (rt != null)
            rt.sizeDelta = slotSize;

        LayoutElement layoutElement = slot.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = slot.gameObject.AddComponent<LayoutElement>();

        layoutElement.preferredWidth = slotSize.x;
        layoutElement.preferredHeight = slotSize.y;
        layoutElement.flexibleWidth = 0f;
        layoutElement.flexibleHeight = 0f;
    }
}
