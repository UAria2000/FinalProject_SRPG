using System;
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
    Defeat
}

public enum BattleInputMode
{
    None,
    WaitingForAction,
    WaitingForSkillTarget,
    WaitingForMoveTarget,
    WaitingForItemTarget
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

[Flags]
public enum SkillLearnTag
{
    None    = 0,
    Unique  = 1 << 0,
    Common  = 1 << 1,
    Melee   = 1 << 2,
    Mid     = 1 << 3,
    Ranged  = 1 << 4
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
    None,
    Poison,
    Bleed,
    Stun
}

public enum StatModifierType
{
    None,
    DMG,
    SPD,
    HIT,
    AC,
    CRI,
    CRD
}

public enum AttackResultType
{
    Crit,
    Hit,
    Graze,
    Miss
}
