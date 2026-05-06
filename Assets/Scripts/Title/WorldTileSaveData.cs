using System;
using System.Collections.Generic;

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

    public List<string> previewEnemyPortraitSpriteNames = new List<string>();

    public static WorldTileSaveData FromRuntime(WorldTileData tile)
    {
        if (tile == null)
            return null;

        WorldTileSaveData data = new WorldTileSaveData
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

        if (tile.previewEnemyPortraits != null)
        {
            for (int i = 0; i < tile.previewEnemyPortraits.Count; i++)
            {
                if (tile.previewEnemyPortraits[i] != null && !string.IsNullOrWhiteSpace(tile.previewEnemyPortraits[i].name))
                    data.previewEnemyPortraitSpriteNames.Add(tile.previewEnemyPortraits[i].name);
            }
        }

        return data;
    }
}
