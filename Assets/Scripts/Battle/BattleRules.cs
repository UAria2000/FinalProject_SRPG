public static class BattleRules
{
    public static bool CanSwapWith(BattleUnit actor, BattleUnit target)
    {
        if (actor == null || target == null) return false;
        if (actor.Team != target.Team) return false;
        if (actor.IsDead || target.IsDead) return false;
        if (actor.IsPositionMovementLocked || target.IsPositionMovementLocked) return false;

        int distance = actor.SlotIndex - target.SlotIndex;
        if (distance < 0) distance = -distance;
        return distance == 1;
    }
}
