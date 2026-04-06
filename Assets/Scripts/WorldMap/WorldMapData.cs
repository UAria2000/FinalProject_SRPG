using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WorldMapData
{
    public int radius;
    public int startTileId = -1;
    public List<WorldTileData> tiles = new List<WorldTileData>();

    private Dictionary<int, WorldTileData> tileById;
    private Dictionary<HexCoord, WorldTileData> tileByCoord;

    public IReadOnlyList<WorldTileData> Tiles => tiles;

    public void RebuildLookup()
    {
        tileById = new Dictionary<int, WorldTileData>(tiles.Count);
        tileByCoord = new Dictionary<HexCoord, WorldTileData>(tiles.Count);

        for (int i = 0; i < tiles.Count; i++)
        {
            WorldTileData tile = tiles[i];
            if (tile == null)
                continue;

            tileById[tile.tileId] = tile;
            tileByCoord[tile.coord] = tile;
        }
    }

    public WorldTileData GetTileById(int tileId)
    {
        EnsureLookup();
        tileById.TryGetValue(tileId, out WorldTileData tile);
        return tile;
    }

    public WorldTileData GetTileByCoord(HexCoord coord)
    {
        EnsureLookup();
        tileByCoord.TryGetValue(coord, out WorldTileData tile);
        return tile;
    }

    public List<WorldTileData> GetNeighbors(WorldTileData tile)
    {
        List<WorldTileData> result = new List<WorldTileData>(6);
        if (tile == null)
            return result;

        EnsureLookup();

        List<HexCoord> coords = tile.coord.GetNeighbors();
        for (int i = 0; i < coords.Count; i++)
        {
            WorldTileData neighbor = GetTileByCoord(coords[i]);
            if (neighbor != null)
                result.Add(neighbor);
        }

        return result;
    }

    public bool AreNeighbors(WorldTileData a, WorldTileData b)
    {
        if (a == null || b == null)
            return false;

        return HexCoord.Distance(a.coord, b.coord) == 1;
    }

    public List<WorldTileData> GetTilesByCurrentOwner(FactionType owner)
    {
        List<WorldTileData> result = new List<WorldTileData>();
        for (int i = 0; i < tiles.Count; i++)
        {
            if (tiles[i] != null && tiles[i].currentOwner == owner)
                result.Add(tiles[i]);
        }
        return result;
    }

    public List<WorldTileData> GetTilesByNativeFaction(FactionType nativeFaction)
    {
        List<WorldTileData> result = new List<WorldTileData>();
        for (int i = 0; i < tiles.Count; i++)
        {
            if (tiles[i] != null && tiles[i].nativeFaction == nativeFaction)
                result.Add(tiles[i]);
        }
        return result;
    }

    public WorldTileData GetStartTile()
    {
        return GetTileById(startTileId);
    }

    private void EnsureLookup()
    {
        if (tileById == null || tileByCoord == null)
            RebuildLookup();
    }
}
