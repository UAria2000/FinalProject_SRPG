using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleViewManager : MonoBehaviour
{
    [SerializeField] private RectTransform viewRoot;
    [SerializeField] private RectTransform[] allyAnchors = new RectTransform[4];
    [SerializeField] private RectTransform[] enemyAnchors = new RectTransform[4];
    [SerializeField] private BattleUnitView defaultUnitViewPrefab;

    [Header("Floating Feedback")]
    [SerializeField] private BattleFloatingTextUI floatingTextPrefab;
    [SerializeField] private RectTransform floatingTextRoot;
    [SerializeField] private float defaultFloatingTextDuration = 1f;
    [SerializeField] private float defaultFloatingTextRiseDistance = 40f;
    [SerializeField] private Vector2 floatingTextOffset = new Vector2(0f, 80f);

    private readonly Dictionary<BattleUnit, BattleUnitView> unitViews = new Dictionary<BattleUnit, BattleUnitView>();

    public void CreateView(BattleUnit unit, BattleInputController inputController)
    {
        if (unit == null || viewRoot == null)
            return;

        BattleUnitView prefab = unit.ViewDefinition != null && unit.ViewDefinition.viewPrefab != null
            ? unit.ViewDefinition.viewPrefab
            : defaultUnitViewPrefab;

        if (prefab == null)
        {
            Debug.LogWarning("[BattleViewManager] Missing defaultUnitViewPrefab.");
            return;
        }

        BattleUnitView view = Instantiate(prefab, viewRoot);
        view.Initialize(unit, GetSlotLabel(unit.Team, unit.SlotIndex));
        view.SetPositionInstant(GetAnchorAnchoredPosition(unit.Team, unit.SlotIndex));

        view.ConfigureClickHandling(inputController);

        unitViews[unit] = view;
    }

    public BattleUnitView GetView(BattleUnit unit)
    {
        if (unit == null) return null;
        BattleUnitView view;
        unitViews.TryGetValue(unit, out view);
        return view;
    }

    public IEnumerable<BattleUnitView> GetAllViews()
    {
        return unitViews.Values;
    }


    public void ClearAllViews()
    {
        foreach (KeyValuePair<BattleUnit, BattleUnitView> pair in unitViews)
        {
            if (pair.Value != null)
                Destroy(pair.Value.gameObject);
        }

        unitViews.Clear();
    }

    public void RemoveView(BattleUnit unit)
    {
        if (unit == null) return;
        BattleUnitView view;
        if (unitViews.TryGetValue(unit, out view))
        {
            if (view != null)
                Destroy(view.gameObject);
            unitViews.Remove(unit);
        }
    }

    public Vector2 GetAnchorAnchoredPosition(TeamType team, int slotIndex)
    {
        RectTransform[] anchors = team == TeamType.Ally ? allyAnchors : enemyAnchors;
        if (anchors == null || slotIndex < 0 || slotIndex >= anchors.Length || anchors[slotIndex] == null)
            return Vector2.zero;

        RectTransform anchor = anchors[slotIndex];
        if (viewRoot != null && anchor.parent == viewRoot)
            return anchor.anchoredPosition;

        if (viewRoot != null)
        {
            Vector3 local = viewRoot.InverseTransformPoint(anchor.position);
            return new Vector2(local.x, local.y);
        }

        return anchor.anchoredPosition;
    }

    public Vector3 GetAnchorPosition(TeamType team, int slotIndex)
    {
        RectTransform[] anchors = team == TeamType.Ally ? allyAnchors : enemyAnchors;
        if (anchors == null || slotIndex < 0 || slotIndex >= anchors.Length || anchors[slotIndex] == null)
            return viewRoot != null ? viewRoot.position : Vector3.zero;

        return anchors[slotIndex].position;
    }

    public void RefreshAllPositionsInstant(BattleFormation allyFormation, BattleFormation enemyFormation)
    {
        RefreshFormationPositionsInstant(allyFormation, TeamType.Ally);
        RefreshFormationPositionsInstant(enemyFormation, TeamType.Enemy);
    }

    public IEnumerator AnimateRefreshAllPositions(BattleFormation allyFormation, BattleFormation enemyFormation, float duration)
    {
        List<IEnumerator> routines = new List<IEnumerator>();
        AddFormationMoveRoutines(routines, allyFormation, TeamType.Ally, duration);
        AddFormationMoveRoutines(routines, enemyFormation, TeamType.Enemy, duration);

        for (int i = 0; i < routines.Count; i++)
            StartCoroutine(routines[i]);

        yield return new WaitForSeconds(duration);
    }

    private void AddFormationMoveRoutines(List<IEnumerator> routines, BattleFormation formation, TeamType team, float duration)
    {
        if (formation == null) return;
        List<BattleUnit> units = formation.GetAllUnits();
        for (int i = 0; i < units.Count; i++)
        {
            BattleUnit unit = units[i];
            BattleUnitView view = GetView(unit);
            if (view == null) continue;
            routines.Add(view.MoveToPosition(GetAnchorAnchoredPosition(team, unit.SlotIndex), duration));
        }
    }

    private void RefreshFormationPositionsInstant(BattleFormation formation, TeamType team)
    {
        if (formation == null) return;
        List<BattleUnit> units = formation.GetAllUnits();
        for (int i = 0; i < units.Count; i++)
        {
            BattleUnitView view = GetView(units[i]);
            if (view != null)
                view.SetPositionInstant(GetAnchorAnchoredPosition(team, units[i].SlotIndex));
        }
    }

    public void ClearAllMarkers()
    {
        foreach (KeyValuePair<BattleUnit, BattleUnitView> pair in unitViews)
        {
            if (pair.Value == null) continue;
            pair.Value.SetTurnMark(false);
            pair.Value.SetTargetMark(false);
            pair.Value.SetHighlighted(false);
        }
    }

    public void SetTurnMarker(BattleUnit currentUnit)
    {
        foreach (KeyValuePair<BattleUnit, BattleUnitView> pair in unitViews)
        {
            if (pair.Value == null) continue;
            pair.Value.SetTurnMark(pair.Key == currentUnit);
        }
    }

    public void SetTargetMarkers(List<BattleUnit> units)
    {
        ClearTargetMarkers();
        if (units == null) return;

        for (int i = 0; i < units.Count; i++)
        {
            BattleUnitView view = GetView(units[i]);
            if (view != null)
                view.SetTargetMark(true);
        }
    }

    public void ClearTargetMarkers()
    {
        foreach (KeyValuePair<BattleUnit, BattleUnitView> pair in unitViews)
            if (pair.Value != null)
                pair.Value.SetTargetMark(false);
    }

    public void PlayEffect(GameObject prefab, Vector3 worldPosition, float duration = 2f)
    {
        if (prefab == null || viewRoot == null) return;

        GameObject effect = Instantiate(prefab, viewRoot);
        effect.transform.position = worldPosition;

        Destroy(effect, duration);
    }

    public void ShowFloatingText(BattleUnit unit, string text, Color color, float duration = -1f)
    {
        if (unit == null || string.IsNullOrWhiteSpace(text))
            return;

        BattleUnitView unitView = GetView(unit);
        if (unitView == null)
            return;

        RectTransform parent = floatingTextRoot != null ? floatingTextRoot : viewRoot;
        if (parent == null)
            return;

        BattleFloatingTextUI floating = null;
        if (floatingTextPrefab != null)
        {
            floating = Instantiate(floatingTextPrefab, parent);
        }
        else
        {
            GameObject go = new GameObject("BattleFloatingText", typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(parent, false);
            TMP_Text tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 28f;
            tmp.raycastTarget = false;
            floating = go.AddComponent<BattleFloatingTextUI>();
            floating.Bind(tmp, go.GetComponent<CanvasGroup>());
        }

        RectTransform floatingRect = floating.GetComponent<RectTransform>();
        RectTransform anchor = unitView.HoverAnchor;
        if (floatingRect != null && anchor != null)
        {
            Vector3 world = anchor.position;
            if (parent != null)
            {
                Vector3 local = parent.InverseTransformPoint(world);
                floatingRect.anchoredPosition = new Vector2(local.x, local.y) + floatingTextOffset;
            }
            else
            {
                floatingRect.position = world;
            }
        }

        floating.Play(text, color, duration > 0f ? duration : defaultFloatingTextDuration, defaultFloatingTextRiseDistance);
    }

    public void RefreshBattleVisualStates(BattleManager manager)
    {
        if (manager == null)
            return;

        foreach (KeyValuePair<BattleUnit, BattleUnitView> pair in unitViews)
        {
            BattleUnit unit = pair.Key;
            BattleUnitView view = pair.Value;
            if (unit == null || view == null)
                continue;

            bool isCurrent = manager.CurrentActingUnit == unit;
            bool isInfoSelected = manager.SelectedAllyInfoUnit == unit || manager.SelectedEnemyInfoUnit == unit;
            bool isFinished = manager.HasUnitFinishedTurnThisRound(unit);

            // Upcoming/finished gray overlays are deprecated.
            // A unit that already ended its turn this round uses its dead battle sprite until the next round starts.
            view.RefreshBattleVisualState(isCurrent, isInfoSelected, isFinished);
        }
    }

    private string GetSlotLabel(TeamType team, int slotIndex)
    {
        string prefix = team == TeamType.Ally ? "A" : "E";
        return string.Format("{0}{1}", prefix, slotIndex + 1);
    }
}