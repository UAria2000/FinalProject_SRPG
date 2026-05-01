using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투 하단 아군/적군 초상화 슬롯 바.
/// 아군 바와 적군 바를 각각 하나씩 만들고 Team을 다르게 설정한다.
/// </summary>
public class BattleBottomPortraitBarUI : MonoBehaviour
{
    [Header("Team")]
    [SerializeField] private TeamType team = TeamType.Ally;
    [Tooltip("비워두면 기본 순서를 사용합니다. 아군 기본값은 3,2,1,0 / 적군 기본값은 0,1,2,3입니다.")]
    [SerializeField] private int[] slotIndexOrder;

    [Header("Slots")]
    [SerializeField] private RectTransform slotsRoot;
    [SerializeField] private BattleBottomPortraitSlotUI slotPrefab;
    [SerializeField] private BattleBottomPortraitSlotUI[] slots = new BattleBottomPortraitSlotUI[4];

    [Header("Display")]
    [SerializeField] private bool showEmptySlots = true;
    [SerializeField] private bool showSlotIndex = false;

    private BattleManager battleManager;

    public TeamType Team => team;

    public void Initialize(BattleManager manager)
    {
        battleManager = manager;
        EnsureSlots();
    }

    public void Refresh(BattleManager manager)
    {
        if (manager != null)
            battleManager = manager;

        EnsureSlots();

        BattleFormation formation = GetFormation();
        int[] order = GetEffectiveSlotOrder();

        for (int visualIndex = 0; visualIndex < slots.Length; visualIndex++)
        {
            BattleBottomPortraitSlotUI slotUI = slots[visualIndex];
            if (slotUI == null)
                continue;

            int formationSlot = visualIndex < order.Length ? Mathf.Clamp(order[visualIndex], 0, 3) : visualIndex;
            BattleUnit unit = formation != null ? formation.GetUnit(formationSlot) : null;

            bool hasUnit = unit != null;
            if (!showEmptySlots && slotUI.gameObject.activeSelf != hasUnit)
                slotUI.gameObject.SetActive(hasUnit);
            else if (showEmptySlots && !slotUI.gameObject.activeSelf)
                slotUI.gameObject.SetActive(true);

            bool isSelected = false;
            bool isCurrent = false;
            bool isFinished = false;

            if (battleManager != null && unit != null)
            {
                isSelected = unit.Team == TeamType.Ally
                    ? battleManager.SelectedAllyInfoUnit == unit
                    : battleManager.SelectedEnemyInfoUnit == unit;
                isCurrent = battleManager.CurrentActingUnit == unit;
                isFinished = battleManager.HasUnitFinishedTurnThisRound(unit);
            }

            slotUI.Initialize(this, team, formationSlot);
            slotUI.Bind(unit, isSelected, isCurrent, isFinished, showSlotIndex);
        }
    }

    public void HandleSlotClicked(BattleBottomPortraitSlotUI slot, BattleUnit unit, TeamType clickedTeam, int formationSlotIndex)
    {
        if (battleManager == null)
            return;

        if (unit == null)
        {
            battleManager.PresentationController?.ClearInfoSelectionForTeam(clickedTeam);
            return;
        }

        battleManager.PresentationController?.ToggleUnitInfoFromBottomPortrait(unit);
    }

    private BattleFormation GetFormation()
    {
        if (battleManager == null)
            return null;

        return team == TeamType.Ally ? battleManager.AllyFormation : battleManager.EnemyFormation;
    }

    private int[] GetEffectiveSlotOrder()
    {
        if (slotIndexOrder != null && slotIndexOrder.Length > 0)
            return slotIndexOrder;

        if (team == TeamType.Ally)
            return new[] { 3, 2, 1, 0 };

        return new[] { 0, 1, 2, 3 };
    }

    private void EnsureSlots()
    {
        if (slotsRoot == null)
            slotsRoot = transform as RectTransform;

        if (slots == null || slots.Length < 4)
        {
            BattleBottomPortraitSlotUI[] next = new BattleBottomPortraitSlotUI[4];
            if (slots != null)
            {
                for (int i = 0; i < Mathf.Min(slots.Length, next.Length); i++)
                    next[i] = slots[i];
            }
            slots = next;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
                continue;

            BattleBottomPortraitSlotUI existing = null;
            if (slotsRoot != null && i < slotsRoot.childCount)
                existing = slotsRoot.GetChild(i).GetComponent<BattleBottomPortraitSlotUI>();

            if (existing == null && slotPrefab != null && slotsRoot != null)
            {
                existing = Instantiate(slotPrefab, slotsRoot);
                existing.name = string.Format("{0}BottomPortraitSlot_{1:00}", team, i);
            }

            slots[i] = existing;
        }
    }
}
