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
    public event Action<WorldTileData> OnCurrentTileChanged;

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

        revealController = new WorldRevealController(MapData);
        movementController = new WorldMovementController(MapData);

        CurrentTile = MapData.GetStartTile();
        SelectedTile = null;
        revealController.RevealAround(CurrentTile);

        if (selectedTileInfoPanel != null)
        {
            selectedTileInfoPanel.Initialize(this, generationSettings);
            selectedTileInfoPanel.HidePanel();
        }

        if (worldMapUI != null)
            worldMapUI.Initialize(this, MapData, generationSettings);

        RaiseSelectionChanged();
        RaiseWorldStateChanged();
        OnCurrentTileChanged?.Invoke(CurrentTile);
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
        return tile != null && movementController != null && movementController.CanMoveTo(CurrentTile, tile);
    }

    public bool TryMoveToSelectedTile()
    {
        if (SelectedTile == null || !CanMoveTo(SelectedTile))
            return false;

        MoveToTile(SelectedTile);
        return true;
    }

    public bool IsCurrentTile(WorldTileData tile)
    {
        return tile != null && CurrentTile != null && tile.tileId == CurrentTile.tileId;
    }

    public bool IsSelectedTile(WorldTileData tile)
    {
        return tile != null && SelectedTile != null && tile.tileId == SelectedTile.tileId;
    }

    public bool IsAdjacentReachable(WorldTileData tile)
    {
        return tile != null && CurrentTile != null && movementController != null && movementController.IsAdjacentReachable(CurrentTile, tile);
    }

    private void MoveToTile(WorldTileData tile)
    {
        if (tile == null || !CanMoveTo(tile))
            return;

        CurrentTile = tile;
        SelectedTile = null;
        revealController?.RevealAround(tile);

        if (selectedTileInfoPanel != null)
            selectedTileInfoPanel.HidePanel();

        if (worldMapUI != null)
            worldMapUI.NotifyMovedToTile(tile);

        OnCurrentTileChanged?.Invoke(CurrentTile);
        RaiseSelectionChanged();
        RaiseWorldStateChanged();
    }

    private void RaiseWorldStateChanged()
    {
        OnWorldStateChanged?.Invoke();
        if (worldMapUI != null && MapData != null)
            worldMapUI.RefreshAll(MapData);
    }

    private void RaiseSelectionChanged()
    {
        OnTileSelectionChanged?.Invoke(SelectedTile);

        if (selectedTileInfoPanel == null)
            return;

        if (SelectedTile == null)
            selectedTileInfoPanel.HidePanel();
        else
            selectedTileInfoPanel.ShowTile(SelectedTile);
    }
}
