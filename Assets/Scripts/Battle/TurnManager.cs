using System.Collections.Generic;
using UnityEngine;

public class TurnManager
{
    private Queue<BattleUnit> turnQueue = new Queue<BattleUnit>();

    public void BuildTurnQueue(List<BattleUnit> allAliveUnits)
    {
        List<BattleUnit> sorted = new List<BattleUnit>(allAliveUnits);

        sorted.Sort((a, b) =>
        {
            int speedCompare = b.SPD.CompareTo(a.SPD);
            if (speedCompare != 0)
                return speedCompare;

            return Random.Range(0, 2) == 0 ? -1 : 1;
        });

        turnQueue.Clear();

        foreach (BattleUnit unit in sorted)
            turnQueue.Enqueue(unit);
    }

    public bool HasNextTurn()
    {
        return turnQueue.Count > 0;
    }

    public BattleUnit GetNextUnit()
    {
        while (turnQueue.Count > 0)
        {
            BattleUnit unit = turnQueue.Dequeue();
            if (unit != null && !unit.IsDead)
                return unit;
        }

        return null;
    }
}