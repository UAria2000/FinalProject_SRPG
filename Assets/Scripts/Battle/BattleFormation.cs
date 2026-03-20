using System.Collections.Generic;

public class BattleFormation
{
    public BattleUnit[] Slots { get; private set; } = new BattleUnit[4];

    public void SetUnit(int slotIndex, BattleUnit unit)
    {
        if (slotIndex < 0 || slotIndex >= Slots.Length)
            return;

        Slots[slotIndex] = unit;

        if (unit != null)
            unit.SlotIndex = slotIndex;
    }

    public BattleUnit GetUnit(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= Slots.Length)
            return null;

        return Slots[slotIndex];
    }

    public List<BattleUnit> GetAliveUnits()
    {
        List<BattleUnit> units = new List<BattleUnit>();

        for (int i = 0; i < Slots.Length; i++)
        {
            if (Slots[i] != null && !Slots[i].IsDead)
                units.Add(Slots[i]);
        }

        return units;
    }

    public bool HasAliveUnits()
    {
        for (int i = 0; i < Slots.Length; i++)
        {
            if (Slots[i] != null && !Slots[i].IsDead)
                return true;
        }

        return false;
    }

    public bool TrySwapAdjacent(int slotIndex, int direction)
    {
        int targetIndex = slotIndex + direction;

        if (slotIndex < 0 || slotIndex >= Slots.Length)
            return false;

        if (targetIndex < 0 || targetIndex >= Slots.Length)
            return false;

        if (Slots[slotIndex] == null || Slots[targetIndex] == null)
            return false;

        BattleUnit a = Slots[slotIndex];
        BattleUnit b = Slots[targetIndex];

        Slots[slotIndex] = b;
        Slots[targetIndex] = a;

        a.SlotIndex = targetIndex;
        b.SlotIndex = slotIndex;

        return true;
    }

    public void RemoveDeadAndCompress()
    {
        List<BattleUnit> alive = new List<BattleUnit>();

        for (int i = 0; i < Slots.Length; i++)
        {
            if (Slots[i] != null && !Slots[i].IsDead)
                alive.Add(Slots[i]);
        }

        for (int i = 0; i < Slots.Length; i++)
            Slots[i] = null;

        for (int i = 0; i < alive.Count; i++)
        {
            Slots[i] = alive[i];
            alive[i].SlotIndex = i;
        }
    }
}