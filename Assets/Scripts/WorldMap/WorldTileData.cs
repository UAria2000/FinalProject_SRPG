using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WorldTileData
{
    public int tileId;
    public HexCoord coord;
    public FactionType nativeFaction = FactionType.None;
    public FactionType currentOwner = FactionType.None;
    public WorldTileEventType eventType = WorldTileEventType.None;
    public bool revealed;
    public bool isPlayerStart;
    public bool isResolved;
    public bool isIconDisabled;
    public List<Sprite> previewEnemyPortraits = new List<Sprite>();

    public bool IsPlayerOwned => currentOwner == FactionType.Player;
    public bool IsCombatEvent => eventType.IsCombatEvent();
}
