using System;

public enum FactionType
{
    None = 0,
    Player = 1,
    FactionA = 2,
    FactionB = 3,
}

public enum WorldTileEventType
{
    None = 0,
    Battle = 1,
    Rest = 2,
    Treasure = 3,
    Merchant = 4,
    Quest = 5,
    Graveyard = 6,
    EliteBattle = 7,
    Boss = 8,
}

public static class WorldTileEventTypeExtensions
{
    public static bool IsCombatEvent(this WorldTileEventType eventType)
    {
        return eventType == WorldTileEventType.Battle
            || eventType == WorldTileEventType.EliteBattle
            || eventType == WorldTileEventType.Boss;
    }

    public static bool IsWeightedEvent(this WorldTileEventType eventType)
    {
        return eventType == WorldTileEventType.Battle
            || eventType == WorldTileEventType.Rest
            || eventType == WorldTileEventType.Treasure
            || eventType == WorldTileEventType.Merchant
            || eventType == WorldTileEventType.Quest
            || eventType == WorldTileEventType.EliteBattle;
    }
}
