using System.Collections.Generic;

public class WorldRevealController
{
    private readonly WorldMapData mapData;

    public WorldRevealController(WorldMapData mapData)
    {
        this.mapData = mapData;
    }

    public void RevealAround(WorldTileData centerTile)
    {
        if (mapData == null || centerTile == null)
            return;

        centerTile.revealed = true;

        List<WorldTileData> neighbors = mapData.GetNeighbors(centerTile);
        for (int i = 0; i < neighbors.Count; i++)
            neighbors[i].revealed = true;
    }
}
