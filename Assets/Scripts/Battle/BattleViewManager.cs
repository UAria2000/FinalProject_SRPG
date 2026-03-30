using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleViewManager : MonoBehaviour
{
    [SerializeField] private RectTransform viewRoot;
    [SerializeField] private RectTransform[] allyAnchors = new RectTransform[4];
    [SerializeField] private RectTransform[] enemyAnchors = new RectTransform[4];
    [SerializeField] private BattleUnitView defaultUnitViewPrefab;

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
        view.SetPositionInstant(GetAnchorPosition(unit.Team, unit.SlotIndex));

        BattleClickable clickable = view.GetComponent<BattleClickable>();
        if (clickable == null)
            clickable = view.gameObject.AddComponent<BattleClickable>();

        clickable.Initialize(view, inputController);

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
            routines.Add(view.MoveToPosition(GetAnchorPosition(team, unit.SlotIndex), duration));
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
                view.SetPositionInstant(GetAnchorPosition(team, units[i].SlotIndex));
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

    private string GetSlotLabel(TeamType team, int slotIndex)
    {
        string prefix = team == TeamType.Ally ? "A" : "E";
        return string.Format("{0}{1}", prefix, slotIndex + 1);
    }
}
