using System;

[Serializable]
public class WorldTileSaveData
{
    public int tileId;
    public int q;
    public int r;
    public int nativeFaction;
    public int currentOwner;
    public int eventType;

    public bool revealed;
    public bool isPlayerStart;
    public bool isResolved;
    public bool isIconDisabled;

    public static WorldTileSaveData FromRuntime(WorldTileData tile)
    {
        if (tile == null)
            return null;

        return new WorldTileSaveData
        {
            tileId = tile.tileId,
            q = tile.coord.q,
            r = tile.coord.r,
            nativeFaction = (int)tile.nativeFaction,
            currentOwner = (int)tile.currentOwner,
            eventType = (int)tile.eventType,
            revealed = tile.revealed,
            isPlayerStart = tile.isPlayerStart,
            isResolved = tile.isResolved,
            isIconDisabled = tile.isIconDisabled,
        };
    }
}
