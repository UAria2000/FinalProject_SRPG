using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HexWorldMapUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private RectTransform tileContainer;
    [SerializeField] private HexTileView tilePrefab;
    [SerializeField] private Button backgroundButton;
    [SerializeField] private WorldMapDragPan dragPan;

    [Header("Layout")]
    [SerializeField, Min(1f)] private float tileRadius = 64f;
    [SerializeField, Min(0.1f)] private float horizontalSpacingMultiplier = 1f;
    [SerializeField, Min(0.1f)] private float verticalSpacingMultiplier = 1f;
    [SerializeField] private bool resizeTileRectFromRadius = true;

    [Header("Aura")]
    [SerializeField] private Sprite currentAuraSprite;
    [SerializeField] private Sprite reachableAuraSprite;
    [SerializeField] private Color currentAuraColor = new Color(0.72f, 0.38f, 0.95f, 0.95f);
    [SerializeField] private Color reachableAuraColor = new Color(0.35f, 0.75f, 1f, 0.9f);

    private readonly Dictionary<int, HexTileView> tileViews = new Dictionary<int, HexTileView>();

    private WorldRunManager runManager;
    private WorldMapData mapData;
    private WorldGenerationSettings settings;

    public void Build(WorldRunManager inRunManager, WorldMapData inMapData, WorldGenerationSettings inSettings)
    {
        runManager = inRunManager;
        mapData = inMapData;
        settings = inSettings;

        ClearAll();

        if (runManager == null || mapData == null || tilePrefab == null || tileContainer == null)
            return;

        CreateTiles();
        PositionTiles();

        if (backgroundButton != null)
        {
            backgroundButton.onClick.RemoveAllListeners();
            backgroundButton.onClick.AddListener(HandleBackgroundClicked);
        }

        if (dragPan != null)
            dragPan.Configure(contentRoot);

        runManager.OnWorldStateChanged -= Refresh;
        runManager.OnWorldStateChanged += Refresh;

        Refresh();
    }

    public void Refresh()
    {
        if (mapData == null || settings == null || runManager == null)
            return;

        for (int i = 0; i < mapData.tiles.Count; i++)
        {
            WorldTileData tile = mapData.tiles[i];
            if (tile == null)
                continue;

            HexTileView view;
            if (!tileViews.TryGetValue(tile.tileId, out view) || view == null)
                continue;

            FactionType displayFaction = tile.currentOwner == FactionType.Player ? FactionType.Player : tile.nativeFaction;
            Sprite tileSprite = settings.GetFactionTileSprite(displayFaction);
            Color fallbackColor = settings.GetFactionFallbackColor(displayFaction);

            bool revealed = tile.revealed || tile.isPlayerStart;
            bool showQuestionMark = !revealed;
            Sprite iconSprite = revealed ? settings.GetEventIcon(tile.eventType) : null;
            bool disableIcon = tile.currentOwner == FactionType.Player && tile.eventType != WorldTileEventType.None;

            bool showAura = false;
            Sprite auraSprite = null;
            Color auraColor = Color.white;

            if (runManager.IsCurrentTile(tile))
            {
                showAura = true;
                auraSprite = currentAuraSprite;
                auraColor = currentAuraColor;
            }
            else if (runManager.IsAdjacentReachable(tile))
            {
                showAura = true;
                auraSprite = reachableAuraSprite;
                auraColor = reachableAuraColor;
            }

            view.SetVisual(tileSprite, fallbackColor, iconSprite, revealed, showQuestionMark, showAura, auraSprite, auraColor, disableIcon);
        }
    }

    private void CreateTiles()
    {
        for (int i = 0; i < mapData.tiles.Count; i++)
        {
            WorldTileData tile = mapData.tiles[i];
            HexTileView view = Instantiate(tilePrefab, tileContainer);
            view.Initialize(tile.tileId, HandleTileClicked);
            tileViews[tile.tileId] = view;
        }
    }

    private void PositionTiles()
    {
        Dictionary<int, Vector2> positions = new Dictionary<int, Vector2>();
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        for (int i = 0; i < mapData.tiles.Count; i++)
        {
            WorldTileData tile = mapData.tiles[i];
            Vector2 pos = AxialToAnchoredPosition(tile.coord);
            positions[tile.tileId] = pos;

            if (pos.x < minX) minX = pos.x;
            if (pos.x > maxX) maxX = pos.x;
            if (pos.y < minY) minY = pos.y;
            if (pos.y > maxY) maxY = pos.y;
        }

        Vector2 centerOffset = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);

        for (int i = 0; i < mapData.tiles.Count; i++)
        {
            WorldTileData tile = mapData.tiles[i];
            HexTileView view = tileViews[tile.tileId];
            RectTransform rt = view.RectTransform;
            if (rt == null)
                continue;

            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = positions[tile.tileId] - centerOffset;

            if (resizeTileRectFromRadius)
                rt.sizeDelta = new Vector2(TileWidth, TileHeight);
        }

        float contentWidth = (maxX - minX) + TileWidth * 1.25f;
        float contentHeight = (maxY - minY) + TileHeight * 1.25f;
        if (contentRoot != null)
            contentRoot.sizeDelta = new Vector2(contentWidth, contentHeight);
        if (tileContainer != null)
            tileContainer.sizeDelta = new Vector2(contentWidth, contentHeight);
    }

    private float TileWidth => tileRadius * 2f;
    private float TileHeight => Mathf.Sqrt(3f) * tileRadius;
    private float HorizontalStep => tileRadius * 1.5f * horizontalSpacingMultiplier;
    private float VerticalStep => TileHeight * verticalSpacingMultiplier;

    private Vector2 AxialToAnchoredPosition(HexCoord coord)
    {
        float x = HorizontalStep * coord.q;
        float y = VerticalStep * (coord.r + coord.q * 0.5f);
        return new Vector2(x, -y);
    }

    private void HandleTileClicked(int tileId)
    {
        if (dragPan != null && dragPan.ShouldSuppressClick())
            return;

        runManager?.HandleTileClicked(tileId);
    }

    private void HandleBackgroundClicked()
    {
        if (dragPan != null && dragPan.ShouldSuppressClick())
            return;

        runManager?.HandleBackgroundClicked();
    }

    private void ClearAll()
    {
        if (runManager != null)
            runManager.OnWorldStateChanged -= Refresh;

        tileViews.Clear();

        if (tileContainer == null)
            return;

        for (int i = tileContainer.childCount - 1; i >= 0; i--)
            Destroy(tileContainer.GetChild(i).gameObject);
    }
}
