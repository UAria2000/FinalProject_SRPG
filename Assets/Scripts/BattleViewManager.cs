using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleViewManager : MonoBehaviour
{
    [Header("Parent")]
    [SerializeField] private RectTransform viewRoot;

    [Header("Anchors")]
    public RectTransform[] allyAnchors = new RectTransform[4];
    public RectTransform[] enemyAnchors = new RectTransform[4];

    [Header("Prefab")]
    public BattleUnitView unitViewPrefab;

    private Dictionary<BattleUnit, BattleUnitView> unitViews = new Dictionary<BattleUnit, BattleUnitView>();

    public void CreateView(BattleUnit unit, BattleManager battleManager)
    {
        if (unit == null || unitViewPrefab == null || viewRoot == null)
            return;

        string label = GetLabel(unit.Team, unit.SlotIndex);
        Color color = GetRandomReadableColor();

        BattleUnitView view = Instantiate(unitViewPrefab, viewRoot);
        view.Initialize(unit, label, color);
        view.SetPositionInstant(GetAnchorPosition(unit.Team, unit.SlotIndex));

        BattleClickable clickable = view.gameObject.GetComponent<BattleClickable>();
        if (clickable == null)
            clickable = view.gameObject.AddComponent<BattleClickable>();

        clickable.Initialize(view, battleManager);

        unitViews[unit] = view;
    }

    public void RemoveView(BattleUnit unit)
    {
        if (unit == null)
            return;

        if (unitViews.TryGetValue(unit, out BattleUnitView view))
        {
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
        if (team == TeamType.Ally)
            return allyAnchors[slotIndex].position;

        return enemyAnchors[slotIndex].position;
    }

    public void RefreshAllPositionsInstant(BattleFormation allyFormation, BattleFormation enemyFormation)
    {
        for (int i = 0; i < 4; i++)
        {
            BattleUnit ally = allyFormation.GetUnit(i);
            if (ally != null && unitViews.TryGetValue(ally, out BattleUnitView allyView))
                allyView.SetPositionInstant(GetAnchorPosition(TeamType.Ally, i));

            BattleUnit enemy = enemyFormation.GetUnit(i);
            if (enemy != null && unitViews.TryGetValue(enemy, out BattleUnitView enemyView))
                enemyView.SetPositionInstant(GetAnchorPosition(TeamType.Enemy, i));
        }
    }

    public IEnumerator AnimateRefreshAllPositions(BattleFormation allyFormation, BattleFormation enemyFormation, float duration)
    {
        for (int i = 0; i < 4; i++)
        {
            BattleUnit ally = allyFormation.GetUnit(i);
            if (ally != null && unitViews.TryGetValue(ally, out BattleUnitView allyView))
                StartCoroutine(allyView.MoveToPosition(GetAnchorPosition(TeamType.Ally, i), duration));

            BattleUnit enemy = enemyFormation.GetUnit(i);
            if (enemy != null && unitViews.TryGetValue(enemy, out BattleUnitView enemyView))
                StartCoroutine(enemyView.MoveToPosition(GetAnchorPosition(TeamType.Enemy, i), duration));
        }

        yield return new WaitForSeconds(duration);
    }

    private string GetLabel(TeamType team, int slotIndex)
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

    private Color GetRandomReadableColor()
    {
        float h = Random.Range(0f, 1f);
        float s = Random.Range(0.65f, 0.95f);
        float v = Random.Range(0.8f, 1f);
        return Color.HSVToRGB(h, s, v);
    }
}