using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전투 유닛 체력바 위에 표시되는 보호막/전투 기믹/상태이상 아이콘 바.
/// 표시 순서: 전투 기믹 상태들, 보호막, 5종 상태이상.
/// 한 줄 최대 6개이며 초과분은 윗줄로 올라간다.
/// </summary>
[DisallowMultipleComponent]
public class BattleStatusIconBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform iconContainer;
    [SerializeField] private BattleStatusIconUI iconPrefab;

    [Header("Special / Gimmick Icons")]
    [SerializeField] private Sprite tauntIcon;
    [SerializeField] private Sprite counterStanceIcon;
    [SerializeField] private Sprite duelArenaIcon;
    [SerializeField] private Sprite stealthIcon;
    [SerializeField] private Sprite endTurnGuardIcon;
    [SerializeField] private Sprite shieldIcon;
    [SerializeField] private Sprite eliteIcon;

    [Header("Ailment Icons")]
    [SerializeField] private Sprite stunIcon;
    [SerializeField] private Sprite bleedIcon;
    [SerializeField] private Sprite burnIcon;
    [SerializeField] private Sprite frostIcon;
    [SerializeField] private Sprite blindIcon;

    [Header("Layout")]
    [SerializeField, Min(1)] private int iconsPerRow = 6;
    [SerializeField, Min(1f)] private float iconSize = 24f;
    [SerializeField, Min(0f)] private float spacing = 2f;
    [Tooltip("켜면 아이콘 묶음이 컨테이너 중앙에 오도록 각 줄의 X 좌표를 보정한다.")]
    [SerializeField] private bool centerRows = true;

    private readonly List<BattleStatusIconUI> iconPool = new List<BattleStatusIconUI>();
    private readonly List<Entry> entries = new List<Entry>();

    private struct Entry
    {
        public Sprite icon;
        public int count;
        public bool showCount;

        public Entry(Sprite icon, int count, bool showCount)
        {
            this.icon = icon;
            this.count = count;
            this.showCount = showCount;
        }
    }

    private void Awake()
    {
        AutoWireIfNeeded();
        HideAll();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoWireIfNeeded();
    }
#endif

    public void Refresh(BattleUnit unit)
    {
        AutoWireIfNeeded();
        entries.Clear();

        if (unit != null)
        {
            AddSpecialState(unit, StatusEffectType.Taunt, tauntIcon);
            AddSpecialState(unit, StatusEffectType.CounterStance, counterStanceIcon);
            AddSpecialState(unit, StatusEffectType.DuelArena, duelArenaIcon);
            AddSpecialState(unit, StatusEffectType.Stealth, stealthIcon);

            if (unit.HasElitePermanentBuff)
                AddEntry(eliteIcon, unit.ElitePermanentAllStatsBuffPercent, eliteIcon != null && unit.ElitePermanentAllStatsBuffPercent > 0);

            if (unit.HasEndTurnGuard)
                AddEntry(endTurnGuardIcon, unit.EndTurnGuardPercent, endTurnGuardIcon != null && unit.EndTurnGuardPercent > 0);

            if (unit.CurrentShield > 0)
                AddEntry(shieldIcon, unit.CurrentShield, true);

            AddAilment(unit, StatusEffectType.Stun, stunIcon);
            AddAilment(unit, StatusEffectType.Bleed, bleedIcon);
            AddAilment(unit, StatusEffectType.Burn, burnIcon);
            AddAilment(unit, StatusEffectType.Frost, frostIcon);
            AddAilment(unit, StatusEffectType.Blind, blindIcon);
        }

        EnsurePool(entries.Count);
        for (int i = 0; i < iconPool.Count; i++)
        {
            BattleStatusIconUI icon = iconPool[i];
            if (icon == null)
                continue;

            if (i < entries.Count)
            {
                Entry entry = entries[i];
                icon.gameObject.SetActive(true);
                icon.Set(entry.icon, entry.count, entry.showCount);
                PositionIcon(icon.RectTransform, i, entries.Count);
            }
            else
            {
                icon.Clear();
            }
        }

        gameObject.SetActive(entries.Count > 0);
    }

    public void HideAll()
    {
        for (int i = 0; i < iconPool.Count; i++)
        {
            if (iconPool[i] != null)
                iconPool[i].Clear();
        }

        gameObject.SetActive(false);
    }

    private void AddSpecialState(BattleUnit unit, StatusEffectType type, Sprite icon)
    {
        if (unit == null || !unit.HasStatus(type))
            return;

        AddEntry(icon, 0, false);
    }

    private void AddAilment(BattleUnit unit, StatusEffectType type, Sprite icon)
    {
        if (unit == null)
            return;

        int stack = unit.GetStatusStackCount(type);
        if (stack <= 0)
            return;

        bool showCount = BattleStatusUtility.IsStackingAilment(type);
        AddEntry(icon, stack, showCount);
    }

    private void AddEntry(Sprite icon, int count, bool showCount)
    {
        if (icon == null)
            return;

        entries.Add(new Entry(icon, Mathf.Max(0, count), showCount));
    }

    private void EnsurePool(int count)
    {
        if (iconPrefab == null || iconContainer == null)
            return;

        while (iconPool.Count < count)
        {
            BattleStatusIconUI icon = Instantiate(iconPrefab, iconContainer);
            icon.gameObject.SetActive(false);
            iconPool.Add(icon);
        }
    }

    private void PositionIcon(RectTransform rect, int index, int totalCount)
    {
        if (rect == null)
            return;

        int safeColumns = Mathf.Max(1, iconsPerRow);
        int row = index / safeColumns;
        int col = index % safeColumns;
        int countInThisRow = Mathf.Min(safeColumns, Mathf.Max(0, totalCount - row * safeColumns));

        float step = iconSize + spacing;
        float rowWidth = countInThisRow * iconSize + Mathf.Max(0, countInThisRow - 1) * spacing;
        float startX = centerRows ? -rowWidth * 0.5f + iconSize * 0.5f : iconSize * 0.5f;
        float x = startX + col * step;
        float y = row * step + iconSize * 0.5f;

        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(iconSize, iconSize);
        rect.anchoredPosition = new Vector2(x, y);
    }

    [ContextMenu("Auto Wire From Children")]
    public void AutoWireIfNeeded()
    {
        if (iconContainer == null)
            iconContainer = transform as RectTransform;

        if (iconPrefab == null)
            iconPrefab = GetComponentInChildren<BattleStatusIconUI>(true);

        if (iconPrefab != null && !iconPool.Contains(iconPrefab))
            iconPrefab.gameObject.SetActive(false);
    }
}
