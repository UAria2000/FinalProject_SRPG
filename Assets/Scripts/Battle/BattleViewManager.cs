using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleViewManager : MonoBehaviour
{
    [Header("Parent")]
    [SerializeField] private RectTransform viewRoot;

    [Header("Anchors")]
    [SerializeField] private RectTransform[] allyAnchors = new RectTransform[4];
    [SerializeField] private RectTransform[] enemyAnchors = new RectTransform[4];

    [Header("Fallback Prefab")]
    [Tooltip("UnitViewDefinition에 프리팹이 비어 있을 때 사용할 기본 공통 프리팹")]
    [SerializeField] private BattleUnitView defaultUnitViewPrefab;

    private readonly Dictionary<BattleUnit, BattleUnitView> unitViews = new Dictionary<BattleUnit, BattleUnitView>();

    public void CreateView(BattleUnit unit, BattleManager battleManager)
    {
        if (unit == null || viewRoot == null)
            return;

        BattleUnitView prefab = GetPrefabForUnit(unit);
        if (prefab == null)
        {
            Debug.LogWarning($"[BattleViewManager] View prefab is missing for unit: {unit.Name}");
            return;
        }

        if (unitViews.ContainsKey(unit))
        {
            Debug.LogWarning($"[BattleViewManager] View already exists for unit: {unit.Name}");
            return;
        }

        string slotLabel = GetSlotLabel(unit.Team, unit.SlotIndex);

        BattleUnitView view = Instantiate(prefab, viewRoot);
        view.Initialize(unit, slotLabel);
        view.SetPositionInstant(GetAnchorPosition(unit.Team, unit.SlotIndex));

        BattleClickable clickable = view.GetComponent<BattleClickable>();
        if (clickable == null)
            clickable = view.gameObject.AddComponent<BattleClickable>();

        clickable.Initialize(view, battleManager);

        unitViews.Add(unit, view);
    }

    public void RemoveView(BattleUnit unit)
    {
        if (unit == null)
            return;

        if (unitViews.TryGetValue(unit, out BattleUnitView view))
        {
            if (view != null)
                Destroy(view.gameObject);

            unitViews.Remove(unit);
        }
    }

    public BattleUnitView GetView(BattleUnit unit)
    {
        if (unit == null)
            return null;

        unitViews.TryGetValue(unit, out BattleUnitView view);
        return view;
    }

    public Vector3 GetAnchorPosition(TeamType team, int slotIndex)
    {
        RectTransform[] anchors = team == TeamType.Ally ? allyAnchors : enemyAnchors;

        if (anchors == null || slotIndex < 0 || slotIndex >= anchors.Length || anchors[slotIndex] == null)
        {
            Debug.LogWarning($"[BattleViewManager] Invalid anchor. Team={team}, SlotIndex={slotIndex}");
            return viewRoot != null ? viewRoot.position : Vector3.zero;
        }

        return anchors[slotIndex].position;
    }

    public void RefreshAllPositionsInstant(BattleFormation allyFormation, BattleFormation enemyFormation)
    {
        RefreshFormationPositionsInstant(allyFormation, TeamType.Ally);
        RefreshFormationPositionsInstant(enemyFormation, TeamType.Enemy);
    }

    public IEnumerator AnimateRefreshAllPositions(BattleFormation allyFormation, BattleFormation enemyFormation, float duration)
    {
        RefreshFormationPositionsAnimated(allyFormation, TeamType.Ally, duration);
        RefreshFormationPositionsAnimated(enemyFormation, TeamType.Enemy, duration);

        yield return new WaitForSeconds(duration);
    }

    private void RefreshFormationPositionsInstant(BattleFormation formation, TeamType team)
    {
        if (formation == null)
            return;

        for (int i = 0; i < 4; i++)
        {
            BattleUnit unit = formation.GetUnit(i);
            if (unit == null)
                continue;

            if (unitViews.TryGetValue(unit, out BattleUnitView view) && view != null)
                view.SetPositionInstant(GetAnchorPosition(team, i));
        }
    }

    private void RefreshFormationPositionsAnimated(BattleFormation formation, TeamType team, float duration)
    {
        if (formation == null)
            return;

        for (int i = 0; i < 4; i++)
        {
            BattleUnit unit = formation.GetUnit(i);
            if (unit == null)
                continue;

            if (unitViews.TryGetValue(unit, out BattleUnitView view) && view != null)
                StartCoroutine(view.MoveToPosition(GetAnchorPosition(team, i), duration));
        }
    }

    private BattleUnitView GetPrefabForUnit(BattleUnit unit)
    {
        if (unit == null)
            return null;

        if (unit.ViewPrefab != null)
            return unit.ViewPrefab;

        return defaultUnitViewPrefab;
    }

    private string GetSlotLabel(TeamType team, int slotIndex)
    {
        if (team == TeamType.Ally)
        {
            switch (slotIndex)
            {
                case 0: return "A";
                case 1: return "B";
                case 2: return "C";
                case 3: return "D";
            }
        }
        else
        {
            switch (slotIndex)
            {
                case 0: return "E";
                case 1: return "F";
                case 2: return "G";
                case 3: return "H";
            }
        }

        return "?";
    }
}