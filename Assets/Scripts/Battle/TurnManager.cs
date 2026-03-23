using System.Collections.Generic;

public class TurnManager
{
    private readonly Queue<BattleUnit> turnQueue = new Queue<BattleUnit>();

    public void BuildTurnQueue(List<BattleUnit> aliveUnits)
    {
        turnQueue.Clear();

        if (aliveUnits == null)
            return;

        aliveUnits.Sort(delegate (BattleUnit a, BattleUnit b)
        {
            int bySpd = b.SPD.CompareTo(a.SPD);
            if (bySpd != 0) return bySpd;

            if (a.Team != b.Team)
                return a.Team == TeamType.Ally ? -1 : 1;

            return a.SlotIndex.CompareTo(b.SlotIndex);
        });

        for (int i = 0; i < aliveUnits.Count; i++)
            turnQueue.Enqueue(aliveUnits[i]);
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
}
