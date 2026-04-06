using System;
using UnityEngine;

public class WorldRunManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WorldGenerationSettings generationSettings;
    [SerializeField] private HexWorldMapUI worldMapUI;
    [SerializeField] private SelectedTileInfoPanel selectedTileInfoPanel;

    [Header("Startup")]
    [SerializeField] private bool generateOnStart = true;

    public WorldMapData MapData { get; private set; }
    public WorldTileData CurrentTile { get; private set; }
    public WorldTileData SelectedTile { get; private set; }
    public WorldGenerationSettings Settings => generationSettings;

    public event Action OnWorldStateChanged;
    public event Action<WorldTileData> OnTileSelectionChanged;

    private WorldRevealController revealController;
    private WorldMovementController movementController;

    private void Start()
    {
        if (generateOnStart)
            GenerateNewWorld();
    }

    public void GenerateNewWorld()
    {
        HexWorldGenerator generator = new HexWorldGenerator(generationSettings);
        MapData = generator.Generate();
        if (MapData == null)
            return;

        movementController = new WorldMovementController(MapData);
        revealController = new WorldRevealController(MapData);

        CurrentTile = MapData.GetStartTile();
        SelectedTile = null;

        if (CurrentTile != null)
            revealController.RevealAround(CurrentTile);

        if (worldMapUI != null)
            worldMapUI.Build(this, MapData, generationSettings);

        if (selectedTileInfoPanel != null)
        {
            selectedTileInfoPanel.Initialize(this, generationSettings);
            selectedTileInfoPanel.HidePanel();
        }

        RaiseWorldStateChanged();
        RaiseSelectionChanged();
    }

    public void HandleTileClicked(int tileId)
    {
        if (MapData == null)
            return;

        WorldTileData tile = MapData.GetTileById(tileId);
        HandleTileClicked(tile);
    }

    public void HandleTileClicked(WorldTileData tile)
    {
        if (tile == null || CurrentTile == null || movementController == null)
            return;

        if (tile.tileId == CurrentTile.tileId)
        {
            ClearSelection();
            return;
        }

        if (tile.IsPlayerOwned)
        {
            MoveToTile(tile);
            return;
        }

        if (SelectedTile != null && SelectedTile.tileId == tile.tileId)
        {
            if (movementController.CanMoveTo(CurrentTile, tile))
            {
                MoveToTile(tile);
                return;
            }
        }

        SelectedTile = tile;
        RaiseSelectionChanged();
        RaiseWorldStateChanged();
    }

    public void HandleBackgroundClicked()
    {
        ClearSelection();
    }

    public void ClearSelection()
    {
        if (SelectedTile == null)
            return;

        SelectedTile = null;
        RaiseSelectionChanged();
        RaiseWorldStateChanged();
    }

    public bool CanMoveTo(WorldTileData tile)
    {
        return movementController != null && movementController.CanMoveTo(CurrentTile, tile);
    }

    public bool TryMoveToSelectedTile()
    {
        if (SelectedTile == null)
            return false;

        if (!CanMoveTo(SelectedTile))
            return false;

        MoveToTile(SelectedTile);
        return true;
    }

    public bool IsAdjacentReachable(WorldTileData tile)
    {
        if (tile == null || CurrentTile == null || movementController == null)
            return false;

        return !tile.IsPlayerOwned && MapData.AreNeighbors(CurrentTile, tile);
    }

    public bool IsCurrentTile(WorldTileData tile)
    {
        return CurrentTile != null && tile != null && CurrentTile.tileId == tile.tileId;
    }

    private void MoveToTile(WorldTileData tile)
    {
        if (tile == null)
            return;

        CurrentTile = tile;
        revealController?.RevealAround(tile);
        SelectedTile = null;
        RaiseSelectionChanged();
        RaiseWorldStateChanged();
    }

    private void RaiseWorldStateChanged()
    {
        OnWorldStateChanged?.Invoke();
    }

    private void RaiseSelectionChanged()
    {
        OnTileSelectionChanged?.Invoke(SelectedTile);
    }
}
