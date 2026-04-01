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
        if (a.IsPositionMovementLocked || b.IsPositionMovementLocked) return;

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

    public bool MoveUnitByDelta(BattleUnit unit, int delta)
    {
        if (unit == null || delta == 0)
            return false;

        if (unit.IsPositionMovementLocked)
            return false;

        int currentIndex = -1;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == unit)
            {
                currentIndex = i;
                break;
            }
        }

        if (currentIndex < 0)
            return false;

        int targetIndex = currentIndex + delta;
        if (targetIndex < 0) targetIndex = 0;
        if (targetIndex >= slots.Length) targetIndex = slots.Length - 1;

        return MoveUnitTo(unit, targetIndex);
    }

    public bool MoveUnitTo(BattleUnit unit, int targetIndex)
    {
        if (unit == null)
            return false;

        if (unit.IsPositionMovementLocked)
            return false;

        if (targetIndex < 0) targetIndex = 0;
        if (targetIndex >= slots.Length) targetIndex = slots.Length - 1;

        int currentIndex = -1;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == unit)
            {
                currentIndex = i;
                break;
            }
        }

        if (currentIndex < 0 || currentIndex == targetIndex)
            return false;

        if (targetIndex > currentIndex)
        {
            for (int i = currentIndex + 1; i <= targetIndex; i++)
            {
                if (slots[i] != null && slots[i].IsPositionMovementLocked)
                    return false;
            }

            for (int i = currentIndex; i < targetIndex; i++)
            {
                slots[i] = slots[i + 1];
                if (slots[i] != null)
                    slots[i].SlotIndex = i;
            }
        }
        else
        {
            for (int i = targetIndex; i < currentIndex; i++)
            {
                if (slots[i] != null && slots[i].IsPositionMovementLocked)
                    return false;
            }

            for (int i = currentIndex; i > targetIndex; i--)
            {
                slots[i] = slots[i - 1];
                if (slots[i] != null)
                    slots[i].SlotIndex = i;
            }
        }

        slots[targetIndex] = unit;
        unit.SlotIndex = targetIndex;
        return true;
    }

    public List<BattleUnit> RemoveDeadAndCompress()
    {
        List<BattleUnit> moved = new List<BattleUnit>();

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && slots[i].IsDead)
                slots[i] = null;
        }

        BattleUnit[] nextSlots = new BattleUnit[4];
        bool[] fixedIndices = new bool[4];

        for (int i = 0; i < slots.Length; i++)
        {
            BattleUnit unit = slots[i];
            if (unit != null && unit.IsPositionMovementLocked)
            {
                nextSlots[i] = unit;
                fixedIndices[i] = true;
                unit.SlotIndex = i;
            }
        }

        int writeIndex = 0;

        for (int readIndex = 0; readIndex < slots.Length; readIndex++)
        {
            BattleUnit unit = slots[readIndex];
            if (unit == null)
                continue;

            if (unit.IsPositionMovementLocked)
                continue;

            while (writeIndex < nextSlots.Length && fixedIndices[writeIndex])
                writeIndex++;

            if (writeIndex >= nextSlots.Length)
                break;

            nextSlots[writeIndex] = unit;

            if (readIndex != writeIndex)
            {
                unit.SlotIndex = writeIndex;
                moved.Add(unit);
            }
            else
            {
                unit.SlotIndex = writeIndex;
            }

            writeIndex++;
        }

        for (int i = 0; i < slots.Length; i++)
            slots[i] = nextSlots[i];

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
