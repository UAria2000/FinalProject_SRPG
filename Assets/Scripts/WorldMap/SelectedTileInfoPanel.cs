using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectedTileInfoPanel : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField] private TMP_Text factionNameText;
    [SerializeField] private TMP_Text eventNameText;
    [SerializeField] private TMP_Text eventDescriptionText;

    [Header("Buttons")]
    [SerializeField] private Button moveButton;
    [SerializeField] private Button closeButton;

    [Header("Enemy Preview")]
    [SerializeField] private GameObject enemyPreviewRoot;
    [SerializeField] private Image[] enemyPortraitSlots = new Image[6];
    [SerializeField] private Color portraitVisibleColor = Color.white;
    [SerializeField] private Color portraitHiddenColor = new Color(1f, 1f, 1f, 0f);

    private WorldRunManager runManager;
    private WorldGenerationSettings settings;

    public void Initialize(WorldRunManager inRunManager, WorldGenerationSettings inSettings)
    {
        runManager = inRunManager;
        settings = inSettings;

        if (runManager != null)
        {
            runManager.OnTileSelectionChanged -= HandleSelectionChanged;
            runManager.OnTileSelectionChanged += HandleSelectionChanged;
        }

        if (moveButton != null)
        {
            moveButton.onClick.RemoveAllListeners();
            moveButton.onClick.AddListener(HandleMoveClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(HideAndClearSelection);
        }

        HidePanel();
    }

    public void HidePanel()
    {
        gameObject.SetActive(false);
    }

    private void HandleSelectionChanged(WorldTileData tile)
    {
        if (tile == null || tile.IsPlayerOwned)
        {
            HidePanel();
            return;
        }

        gameObject.SetActive(true);
        Bind(tile);
    }

    private void Bind(WorldTileData tile)
    {
        if (tile == null || settings == null)
        {
            HidePanel();
            return;
        }

        if (factionNameText != null)
            factionNameText.text = settings.GetFactionDisplayName(tile.nativeFaction);

        bool isRevealed = tile.revealed || tile.isPlayerStart;
        if (eventNameText != null)
            eventNameText.text = isRevealed ? settings.GetEventDisplayName(tile.eventType) : "미공개";

        if (eventDescriptionText != null)
            eventDescriptionText.text = isRevealed ? settings.GetEventDescription(tile.eventType) : "아직 이 타일의 정보가 공개되지 않았습니다.";

        if (moveButton != null)
            moveButton.interactable = runManager != null && runManager.CanMoveTo(tile);

        RefreshEnemyPreview(tile, isRevealed);
    }

    private void RefreshEnemyPreview(WorldTileData tile, bool isRevealed)
    {
        bool showPreview = isRevealed && tile != null && tile.IsCombatEvent;
        if (enemyPreviewRoot != null)
            enemyPreviewRoot.SetActive(showPreview);

        for (int i = 0; i < enemyPortraitSlots.Length; i++)
        {
            Image slot = enemyPortraitSlots[i];
            if (slot == null)
                continue;

            bool visible = showPreview && tile.previewEnemyPortraits != null && i < tile.previewEnemyPortraits.Count && tile.previewEnemyPortraits[i] != null;
            slot.gameObject.SetActive(visible);
            slot.sprite = visible ? tile.previewEnemyPortraits[i] : null;
            slot.color = visible ? portraitVisibleColor : portraitHiddenColor;
        }
    }

    private void HandleMoveClicked()
    {
        runManager?.TryMoveToSelectedTile();
    }

    private void HideAndClearSelection()
    {
        runManager?.ClearSelection();
    }
}
