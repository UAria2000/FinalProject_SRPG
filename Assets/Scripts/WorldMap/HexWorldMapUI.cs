using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HexWorldMapUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform tileContainer;
    [SerializeField] private HexTileView tilePrefab;
    [SerializeField] private Button backgroundButton;
    [SerializeField] private WorldMapDragPan dragPan;

    [Header("Tile Layout")]
    [SerializeField] private float tileRadius = 96f;
    [SerializeField] private bool resizeTileRectFromRadius = false;
    [SerializeField] private float horizontalSpacingMultiplier = 1f;
    [SerializeField] private float verticalSpacingMultiplier = 1f;

    [Header("Tile Auras")]
    [SerializeField] private Sprite currentAuraSprite;
    [SerializeField] private Color currentAuraColor = Color.white;
    [SerializeField] private Sprite selectedAuraSprite;
    [SerializeField] private Color selectedAuraColor = Color.white;
    [SerializeField] private Sprite reachableAuraSprite;
    [SerializeField] private Color reachableAuraColor = Color.white;

    [Header("Camera Focus")]
    [SerializeField] private bool focusCurrentTileOnGenerate = true;
    [SerializeField] private bool focusCurrentTileOnMove = true;

    private WorldRunManager runManager;
    private WorldGenerationSettings settings;
    private Dictionary<int, HexTileView> tileViews = new Dictionary<int, HexTileView>();
    private Bounds generatedLocalBounds;

    public void Initialize(WorldRunManager manager, WorldMapData mapData, WorldGenerationSettings generationSettings)
    {
        runManager = manager;
        settings = generationSettings;

        if (backgroundButton != null)
        {
            backgroundButton.onClick.RemoveAllListeners();
            backgroundButton.onClick.AddListener(OnBackgroundClicked);
        }

        BuildTiles(mapData);

        if (dragPan != null)
            dragPan.SetContentBounds(generatedLocalBounds);

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

            FactionType visualFaction = tile.nativeFaction != FactionType.None ? tile.nativeFaction : tile.currentOwner;
            Sprite tileSprite = settings.GetFactionTileSprite(visualFaction);
            Color tileColor = settings.GetFactionFallbackColor(visualFaction);
            Sprite iconSprite = settings.GetTileDisplayIcon(tile);
            Sprite questionSprite = settings.GetQuestionMarkSprite(tile);
            bool showQuestionMark = !tile.revealed && tile.currentOwner != FactionType.Player;
            bool iconAlwaysVisible = tile.isPlayerStart && settings.StartTileIcon != null;
            bool iconVisible = iconAlwaysVisible || tile.revealed || tile.currentOwner == FactionType.Player;

            view.SetVisual(
                tileSprite,
                tileColor,
                iconSprite,
                iconVisible,
                questionSprite,
                showQuestionMark,
                showAura,
                auraSprite,
                auraColor,
                false);
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
        generatedLocalBounds = new Bounds(Vector3.zero, Vector3.zero);

        if (mapData == null || tilePrefab == null || tileContainer == null)
            return;

        float tileWidth = tileRadius * 2f;
        float tileHeight = Mathf.Sqrt(3f) * tileRadius;
        float halfWidth = tileWidth * 0.5f;
        float halfHeight = tileHeight * 0.5f;
        bool firstBounds = true;
        float minX = 0f;
        float minY = 0f;
        float maxX = 0f;
        float maxY = 0f;

        for (int i = 0; i < mapData.Tiles.Count; i++)
        {
            WorldTileData tile = mapData.Tiles[i];
            if (tile == null)
                continue;

            HexTileView view = Instantiate(tilePrefab, tileContainer);
            view.name = $"Tile_{tile.tileId}_{tile.coord.q}_{tile.coord.r}";

            RectTransform rt = view.RectTransform;
            Vector2 anchoredPosition = CalculateAnchoredPosition(tile.coord);
            if (rt != null)
            {
                rt.anchoredPosition = anchoredPosition;
                if (resizeTileRectFromRadius)
                    rt.sizeDelta = new Vector2(tileWidth, tileHeight);
            }

            float tileMinX = anchoredPosition.x - halfWidth;
            float tileMaxX = anchoredPosition.x + halfWidth;
            float tileMinY = anchoredPosition.y - halfHeight;
            float tileMaxY = anchoredPosition.y + halfHeight;

            if (firstBounds)
            {
                minX = tileMinX;
                minY = tileMinY;
                maxX = tileMaxX;
                maxY = tileMaxY;
                firstBounds = false;
            }
            else
            {
                minX = Mathf.Min(minX, tileMinX);
                minY = Mathf.Min(minY, tileMinY);
                maxX = Mathf.Max(maxX, tileMaxX);
                maxY = Mathf.Max(maxY, tileMaxY);
            }

            view.Initialize(tile.tileId, OnTileClicked);
            tileViews.Add(tile.tileId, view);
        }

        if (!firstBounds)
        {
            Vector3 center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
            Vector3 size = new Vector3(maxX - minX, maxY - minY, 0f);
            generatedLocalBounds = new Bounds(center, size);
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
