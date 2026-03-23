using System.Collections.Generic;

public class BattleFormation
{
    private readonly BattleUnit[] slots = new BattleUnit[4];

    public BattleUnit GetUnit(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Length)
            return null;
        return slots[slotIndex];
    }

    public void SetUnit(int slotIndex, BattleUnit unit)
    {
        if (slotIndex < 0 || slotIndex >= slots.Length)
            return;

        slots[slotIndex] = unit;
        if (unit != null)
            unit.SlotIndex = slotIndex;
    }

    public void RemoveUnit(BattleUnit unit)
    {
        if (unit == null) return;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == unit)
            {
                slots[i] = null;
                return;
            }
        }
    }

    public bool Contains(BattleUnit unit)
    {
        if (unit == null)
            return false;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == unit)
                return true;
        }

        return false;
    }

    public List<BattleUnit> GetAliveUnits()
    {
        List<BattleUnit> result = new List<BattleUnit>();
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && !slots[i].IsDead)
                result.Add(slots[i]);
        }
        return result;
    }

    public List<BattleUnit> GetAllUnits()
    {
        List<BattleUnit> result = new List<BattleUnit>();
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
                result.Add(slots[i]);
        }
        return result;
    }

    public void Swap(BattleUnit a, BattleUnit b)
    {
        if (a == null || b == null) return;

        int indexA = -1;
        int indexB = -1;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == a) indexA = i;
            if (slots[i] == b) indexB = i;
        }

        if (indexA < 0 || indexB < 0) return;

        slots[indexA] = b;
        slots[indexB] = a;
        a.SlotIndex = indexB;
        b.SlotIndex = indexA;
    }

    public List<BattleUnit> RemoveDeadAndCompress()
    {
        List<BattleUnit> moved = new List<BattleUnit>();

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && slots[i].IsDead)
                slots[i] = null;
        }

        int writeIndex = 0;
        for (int readIndex = 0; readIndex < slots.Length; readIndex++)
        {
            BattleUnit unit = slots[readIndex];
            if (unit == null) continue;

            if (readIndex != writeIndex)
            {
                slots[writeIndex] = unit;
                slots[readIndex] = null;
                unit.SlotIndex = writeIndex;
                moved.Add(unit);
            }

            writeIndex++;
        }

        return moved;
    }

    public bool HasLivingUnits()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && !slots[i].IsDead)
                return true;
        }
        return false;
    }
}
