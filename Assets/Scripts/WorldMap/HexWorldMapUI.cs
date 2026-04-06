using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HexWorldMapUI : MonoBehaviour
{
    [Header("Roots")]
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private RectTransform tileContainer;

    [Header("Prefabs")]
    [SerializeField] private HexTileView tilePrefab;

    [Header("Optional UI")]
    [SerializeField] private Button backgroundButton;
    [SerializeField] private WorldMapDragPan dragPan;

    [Header("Tile Layout")]
    [SerializeField] private float tileRadius = 200f;
    [SerializeField] private bool resizeTileRectFromRadius = true;
    [SerializeField] private float horizontalSpacingMultiplier = 1f;
    [SerializeField] private float verticalSpacingMultiplier = 1f;

    [Header("Aura Sprites")]
    [SerializeField] private Sprite currentAuraSprite;
    [SerializeField] private Color currentAuraColor = Color.white;
    [SerializeField] private Sprite selectedAuraSprite;
    [SerializeField] private Color selectedAuraColor = Color.white;
    [SerializeField] private Sprite reachableAuraSprite;
    [SerializeField] private Color reachableAuraColor = Color.white;

    [Header("Camera Follow")]
    [SerializeField] private bool focusCurrentTileOnGenerate = true;
    [SerializeField] private bool focusCurrentTileOnMove = true;

    private readonly Dictionary<int, HexTileView> tileViews = new Dictionary<int, HexTileView>();
    private WorldRunManager runManager;
    private WorldGenerationSettings settings;

    public RectTransform ContentRoot => contentRoot;

    public void Initialize(WorldRunManager manager, WorldMapData mapData, WorldGenerationSettings generationSettings)
    {
        runManager = manager;
        settings = generationSettings;

        if (dragPan != null)
            dragPan.Configure(contentRoot);

        if (backgroundButton != null)
        {
            backgroundButton.onClick.RemoveAllListeners();
            backgroundButton.onClick.AddListener(OnBackgroundClicked);
        }

        BuildTiles(mapData);
        RefreshAll(mapData);

        if (focusCurrentTileOnGenerate && runManager != null && runManager.CurrentTile != null)
            FocusOnTile(runManager.CurrentTile, true);
    }

    public void RefreshAll(WorldMapData mapData)
    {
        if (mapData == null || settings == null || runManager == null)
            return;

        for (int i = 0; i < mapData.Tiles.Count; i++)
        {
            WorldTileData tile = mapData.Tiles[i];
            if (tile == null)
                continue;

            if (!tileViews.TryGetValue(tile.tileId, out HexTileView view) || view == null)
                continue;

            bool isCurrent = runManager.IsCurrentTile(tile);
            bool isSelected = runManager.IsSelectedTile(tile);
            bool isReachable = !isSelected && !isCurrent && runManager.IsAdjacentReachable(tile);

            Sprite auraSprite = null;
            Color auraColor = Color.white;
            bool showAura = false;

            if (isCurrent)
            {
                auraSprite = currentAuraSprite;
                auraColor = currentAuraColor;
                showAura = auraSprite != null;
            }
            else if (isSelected)
            {
                auraSprite = selectedAuraSprite;
                auraColor = selectedAuraColor;
                showAura = auraSprite != null;
            }
            else if (isReachable)
            {
                auraSprite = reachableAuraSprite;
                auraColor = reachableAuraColor;
                showAura = auraSprite != null;
            }

            Sprite tileSprite = settings.GetFactionTileSprite(tile.currentOwner);
            Color tileColor = settings.GetFactionFallbackColor(tile.currentOwner);
            Sprite eventIcon = settings.GetEventIcon(tile.eventType);
            bool showQuestionMark = !tile.revealed;
            bool disableIcon = tile.currentOwner == FactionType.Player;

            view.SetVisual(
                tileSprite,
                tileColor,
                eventIcon,
                tile.revealed,
                showQuestionMark,
                showAura,
                auraSprite,
                auraColor,
                disableIcon);
        }
    }

    public void FocusOnCurrentTile(bool instant = true)
    {
        if (runManager == null || runManager.CurrentTile == null)
            return;

        FocusOnTile(runManager.CurrentTile, instant);
    }

    public void FocusOnTile(WorldTileData tile, bool instant = true)
    {
        if (tile == null || dragPan == null)
            return;

        Vector2 anchored = CalculateAnchoredPosition(tile.coord);
        dragPan.CenterOnAnchoredPosition(anchored);
    }

    public void NotifyMovedToTile(WorldTileData tile)
    {
        if (focusCurrentTileOnMove)
            FocusOnTile(tile, true);
    }

    private void BuildTiles(WorldMapData mapData)
    {
        ClearTiles();
        if (mapData == null || tilePrefab == null || tileContainer == null)
            return;

        for (int i = 0; i < mapData.Tiles.Count; i++)
        {
            WorldTileData tile = mapData.Tiles[i];
            if (tile == null)
                continue;

            HexTileView view = Instantiate(tilePrefab, tileContainer);
            view.name = $"Tile_{tile.tileId}_{tile.coord.q}_{tile.coord.r}";

            RectTransform rt = view.RectTransform;
            if (rt != null)
            {
                rt.anchoredPosition = CalculateAnchoredPosition(tile.coord);
                if (resizeTileRectFromRadius)
                {
                    float width = tileRadius * 2f;
                    float height = Mathf.Sqrt(3f) * tileRadius;
                    rt.sizeDelta = new Vector2(width, height);
                }
            }

            view.Initialize(tile.tileId, OnTileClicked);
            tileViews.Add(tile.tileId, view);
        }
    }

    private void ClearTiles()
    {
        foreach (KeyValuePair<int, HexTileView> pair in tileViews)
        {
            if (pair.Value != null)
                Destroy(pair.Value.gameObject);
        }

        tileViews.Clear();
    }

    private Vector2 CalculateAnchoredPosition(HexCoord coord)
    {
        float horizontalStep = tileRadius * 1.5f * horizontalSpacingMultiplier;
        float verticalStep = Mathf.Sqrt(3f) * tileRadius * verticalSpacingMultiplier;

        float x = coord.q * horizontalStep;
        float y = (coord.r + coord.q * 0.5f) * verticalStep;
        return new Vector2(x, -y);
    }

    private void OnTileClicked(int tileId)
    {
        if (runManager == null)
            return;

        if (dragPan != null && dragPan.ShouldSuppressClick())
            return;

        runManager.HandleTileClicked(tileId);
    }

    private void OnBackgroundClicked()
    {
        if (runManager == null)
            return;

        if (dragPan != null && dragPan.ShouldSuppressClick())
            return;

        runManager.HandleBackgroundClicked();
    }
}
