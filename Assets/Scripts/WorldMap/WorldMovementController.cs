using System.Collections.Generic;

public class WorldMovementController
{
    private readonly WorldMapData mapData;

    public WorldMovementController(WorldMapData mapData)
    {
        this.mapData = mapData;
    }

    public bool CanMoveTo(WorldTileData currentTile, WorldTileData targetTile)
    {
        if (mapData == null || currentTile == null || targetTile == null)
            return false;

        if (currentTile.tileId == targetTile.tileId)
            return false;

        if (targetTile.IsPlayerOwned)
            return true;

        return mapData.AreNeighbors(currentTile, targetTile);
    }

    public List<WorldTileData> GetAdjacentReachableTiles(WorldTileData currentTile)
    {
        List<WorldTileData> result = new List<WorldTileData>();
        if (mapData == null || currentTile == null)
            return result;

        List<WorldTileData> neighbors = mapData.GetNeighbors(currentTile);
        for (int i = 0; i < neighbors.Count; i++)
        {
            WorldTileData tile = neighbors[i];
            if (tile != null && !tile.IsPlayerOwned)
                result.Add(tile);
        }

        return result;
    }
}
