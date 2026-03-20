public enum TeamType
{
    Ally,
    Enemy
}

public enum CharacterRangeType
{
    Melee,
    Mid,
    Ranged
}

public enum BattleActionType
{
    None,
    BasicAttack,
    Move
}

public enum TurnState
{
    Waiting,
    PlayerInput,
    EnemyThinking,
    ExecutingAction,
    TurnEnding,
    BattleEnded
}

public enum BattleResultType
{
    None,
    Victory,
    Defeat
}