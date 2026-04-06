using System;
using System.Collections.Generic;
using UnityEngine;

public enum FactionType
{
    None = 0,
    Player = 1,
    FactionA = 10,
    FactionB = 11,
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
}

[Serializable]
public class WorldFactionPresentationEntry
{
    public FactionType factionType = FactionType.None;
    public string displayName = "Unknown";
    public Sprite tileSprite;
    public Color fallbackColor = Color.white;
    public List<Sprite> battlePreviewPortraits = new List<Sprite>();
    public List<Sprite> elitePreviewPortraits = new List<Sprite>();
    public List<Sprite> bossPreviewPortraits = new List<Sprite>();
}

[Serializable]
public class WorldEventPresentationEntry
{
    public WorldTileEventType eventType = WorldTileEventType.None;
    public string displayName = "None";
    [TextArea(2, 5)] public string description = "";
    public Sprite icon;
}

[Serializable]
public class WorldEventWeightEntry
{
    public WorldTileEventType eventType = WorldTileEventType.Battle;
    [Min(0f)] public float weight = 1f;
}
