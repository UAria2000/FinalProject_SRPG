using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WorldDominationPanelUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WorldRunManager runManager;
    [SerializeField] private WorldGenerationSettings generationSettings;

    [Header("Row Prefab Setup")]
    [SerializeField] private Transform rowRoot;
    [SerializeField] private WorldDominationConditionRowUI rowPrefab;
    [SerializeField] private int maxRows = 3;

    [Header("Conquest Button")]
    [SerializeField] private GameObject conquestButtonRoot;
    [SerializeField] private Button conquestButton;

    [Header("Text Formats")]
    [SerializeField] private string bossConditionFormat = "모든 보스 타일 점령 ({0}/{1})";
    [SerializeField] private string conquestConditionFormat = "전체 지역의 {0}% 점령 달성 ({1}/{2})";

    private readonly List<WorldDominationConditionRowUI> rowInstances = new List<WorldDominationConditionRowUI>();

    private void Awake()
    {
        if (runManager == null)
            runManager = Object.FindFirstObjectByType<WorldRunManager>();

        if (runManager != null && generationSettings == null)
            generationSettings = runManager.Settings;

        if (conquestButton != null)
        {
            conquestButton.onClick.RemoveAllListeners();
            conquestButton.onClick.AddListener(HandleConquestButtonClicked);
        }

        EnsureRows();
    }

    private void OnEnable()
    {
        if (runManager == null)
            runManager = Object.FindFirstObjectByType<WorldRunManager>();

        if (runManager != null && generationSettings == null)
            generationSettings = runManager.Settings;

        if (runManager != null)
        {
            runManager.OnWorldStateChanged += RefreshUI;
            runManager.OnCurrentTileChanged += HandleCurrentTileChanged;
        }

        EnsureRows();
        RefreshUI();
    }

    private void OnDisable()
    {
        if (runManager != null)
        {
            runManager.OnWorldStateChanged -= RefreshUI;
            runManager.OnCurrentTileChanged -= HandleCurrentTileChanged;
        }
    }

    private void HandleCurrentTileChanged(WorldTileData _)
    {
        RefreshUI();
    }

    private void EnsureRows()
    {
        if (rowRoot == null || rowPrefab == null)
            return;

        rowInstances.RemoveAll(r => r == null);

        while (rowInstances.Count < maxRows)
        {
            WorldDominationConditionRowUI instance = Instantiate(rowPrefab, rowRoot);
            instance.name = $"DominationRow_{rowInstances.Count}";
            rowInstances.Add(instance);
        }

        // 혹시 rowRoot 밑에 수동으로 많이 만든 경우 남는 건 꺼둠
        for (int i = 0; i < rowInstances.Count; i++)
        {
            if (rowInstances[i] != null)
                rowInstances[i].gameObject.SetActive(true);
        }
    }

    public void RefreshUI()
    {
        if (runManager == null || generationSettings == null || runManager.MapData == null)
        {
            BindEmpty();
            return;
        }

        EnsureRows();

        WorldMapData map = runManager.MapData;

        int totalBossTiles = 0;
        int conqueredBossTiles = 0;
        int nonStartTiles = 0;
        int conqueredTiles = 0;

        IReadOnlyList<WorldTileData> tiles = map.Tiles;
        for (int i = 0; i < tiles.Count; i++)
        {
            WorldTileData tile = tiles[i];
            if (tile == null)
                continue;

            if (!tile.isPlayerStart)
            {
                nonStartTiles++;
                if (tile.currentOwner == FactionType.Player)
                    conqueredTiles++;
            }

            if (tile.eventType == WorldTileEventType.Boss)
            {
                totalBossTiles++;
                if (tile.currentOwner == FactionType.Player)
                    conqueredBossTiles++;
            }
        }

        int requiredPercent = generationSettings.GetConquestRequiredPercent();
        int currentPercent = nonStartTiles > 0
            ? Mathf.FloorToInt((conqueredTiles / (float)nonStartTiles) * 100f)
            : 0;

        bool bossCompleted = totalBossTiles > 0 && conqueredBossTiles >= totalBossTiles;
        bool conquestCompleted = currentPercent >= requiredPercent;

        string bossText = string.Format(bossConditionFormat, conqueredBossTiles, totalBossTiles);
        string conquestText = string.Format(conquestConditionFormat, requiredPercent, conqueredTiles, nonStartTiles);

        BindRow(0, true, bossCompleted, bossText);
        BindRow(1, true, conquestCompleted, conquestText);

        // 3칸째는 지금 비워두되, 나중 조건 추가 대비
        for (int i = 2; i < maxRows; i++)
            BindRow(i, false, false, string.Empty);

        bool canConquer = bossCompleted && conquestCompleted;

        if (conquestButtonRoot != null)
            conquestButtonRoot.SetActive(canConquer);
        else if (conquestButton != null)
            conquestButton.gameObject.SetActive(canConquer);
    }

    private void BindEmpty()
    {
        EnsureRows();

        for (int i = 0; i < maxRows; i++)
            BindRow(i, false, false, string.Empty);

        if (conquestButtonRoot != null)
            conquestButtonRoot.SetActive(false);
        else if (conquestButton != null)
            conquestButton.gameObject.SetActive(false);
    }

    private void BindRow(int index, bool visible, bool completed, string text)
    {
        if (index < 0 || index >= rowInstances.Count)
            return;

        WorldDominationConditionRowUI row = rowInstances[index];
        if (row == null)
            return;

        row.Bind(visible, completed, text);
    }

    private void HandleConquestButtonClicked()
    {
        if (runManager == null)
            return;

        if (runManager.IsWorldConquestAvailable())
            runManager.HandleWorldConquestButtonPressed();
    }
}