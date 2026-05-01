using System.Collections.Generic;

public class TurnManager
{
    private readonly Queue<BattleUnit> turnQueue = new Queue<BattleUnit>();

    public void BuildTurnQueue(List<BattleUnit> aliveUnits)
    {
        turnQueue.Clear();

        if (aliveUnits == null)
            return;

        SortUnitsForTurnOrder(aliveUnits);

        for (int i = 0; i < aliveUnits.Count; i++)
            turnQueue.Enqueue(aliveUnits[i]);
    }

    public void ResortRemainingTurnsByCurrentSpeed()
    {
        if (turnQueue.Count <= 1)
            return;

        List<BattleUnit> remaining = new List<BattleUnit>(turnQueue);
        remaining.RemoveAll(unit => unit == null || unit.IsDead);
        SortUnitsForTurnOrder(remaining);

        turnQueue.Clear();
        for (int i = 0; i < remaining.Count; i++)
            turnQueue.Enqueue(remaining[i]);
    }

    public bool HasNextTurn()
    {
        return turnQueue.Count > 0;
    }

    public BattleUnit GetNextUnit()
    {
        if (turnQueue.Count <= 0)
            return null;
        return turnQueue.Dequeue();
    }

    public List<BattleUnit> GetOrderedUnitsSnapshot()
    {
        return new List<BattleUnit>(turnQueue);
    }

    private void SortUnitsForTurnOrder(List<BattleUnit> units)
    {
        if (units == null)
            return;

        units.Sort(delegate (BattleUnit a, BattleUnit b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;

            int bySpd = b.SPD.CompareTo(a.SPD);
            if (bySpd != 0) return bySpd;

            if (a.Team != b.Team)
                return a.Team == TeamType.Ally ? -1 : 1;

            return a.SlotIndex.CompareTo(b.SlotIndex);
        });
    }
}
