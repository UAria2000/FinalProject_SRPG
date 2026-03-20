public static class BattleRules
{
    public static bool CanUseBasicAttackFromSlot(CharacterRangeType rangeType, int slotIndex)
    {
        int rank = slotIndex + 1;

        switch (rangeType)
        {
            case CharacterRangeType.Melee:
                return rank == 1 || rank == 2;

            case CharacterRangeType.Mid:
                return rank >= 1 && rank <= 3;

            case CharacterRangeType.Ranged:
                return rank >= 2 && rank <= 4;
        }

        return false;
    }

    public static bool CanTargetWithBasicAttack(CharacterRangeType rangeType, int targetSlotIndex)
    {
        int rank = targetSlotIndex + 1;

        switch (rangeType)
        {
            case CharacterRangeType.Melee:
                return rank == 1 || rank == 2;

            case CharacterRangeType.Mid:
                return rank >= 1 && rank <= 3;

            case CharacterRangeType.Ranged:
                return rank >= 2 && rank <= 4;
        }

        return false;
    }
}