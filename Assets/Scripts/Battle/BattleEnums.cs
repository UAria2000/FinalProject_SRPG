using UnityEngine;

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
    Defeat,
    Flee,
    WorldFailure
}

public enum BattleInputMode
{
    None,
    WaitingForAction,
    WaitingForSkillTarget,
    WaitingForMoveTarget,
    WaitingForItemTarget,
    WaitingForCaptureTarget
}

public enum BottomContextType
{
    EnemyInfo,
    Inventory,
    Map
}

public enum SkillCastType
{
    Active,
    Passive
}

public enum ActiveSkillRole
{
    Attack,
    Buff,
    Debuff,
    Utility
}

public enum SkillSelfMoveDirection
{
    None,
    Forward,
    Backward
}

public enum SkillClass
{
    Melee = 1 << 2,
    Mid = 1 << 3,
    Ranged = 1 << 4,
    Common = 1 << 1,
    Unique = 1 << 0
}

public enum SkillTargetTeam
{
    Enemy,
    Ally,
    Self
}

public enum TargetScope
{
    Single,
    All
}

public enum SkillResolutionMode
{
    Attack,
    SuccessOnly
}

public enum SecondaryTargetRule
{
    None,
    BackOne
}

public enum EffectValueReference
{
    ActorDMG,
    TargetMaxHP
}

public enum BattleEffectKind
{
    Damage,
    Heal,
    Shield,
    Buff,
    Debuff,
    ApplyStatus,
    RemoveStatus
}

public enum StatusEffectType
{
    None = 0,

    Burn = 1,
    Bleed = 2,
    Stun = 3,

    // Backward-compatible special states. These are not resistance-based ailments.
    Taunt = 4,
    CounterStance = 5,
    DuelArena = 6,
    Stealth = 7,

    Frost = 8,
    Blind = 9
}


public enum StatModifierType
{
    None,
    DMG,
    SPD,
    HIT,
    AC,
    CRI,
    CRD,
    IncomingDamageTakenPercent,
    PierceBackOne
}

public enum AttackResultType
{
    Crit,
    Hit,
    Graze,
    Miss
}