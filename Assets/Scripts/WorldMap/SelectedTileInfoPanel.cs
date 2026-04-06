using System.Collections.Generic;
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
    [SerializeField] private List<Image> enemyPortraitSlots = new List<Image>(6);

    private WorldRunManager runManager;
    private WorldGenerationSettings settings;
    private WorldTileData currentTile;

    public void Initialize(WorldRunManager manager, WorldGenerationSettings generationSettings)
    {
        runManager = manager;
        settings = generationSettings;

        if (moveButton != null)
        {
            moveButton.onClick.RemoveAllListeners();
            moveButton.onClick.AddListener(HandleMoveButtonClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(HidePanel);
        }
    }

    public void ShowTile(WorldTileData tile)
    {
        currentTile = tile;
        if (tile == null)
        {
            HidePanel();
            return;
        }

        gameObject.SetActive(true);

        if (factionNameText != null)
            factionNameText.text = settings != null ? settings.GetFactionDisplayName(tile.nativeFaction) : tile.nativeFaction.ToString();

        if (eventNameText != null)
            eventNameText.text = settings != null ? settings.GetEventDisplayName(tile.eventType) : tile.eventType.ToString();

        if (eventDescriptionText != null)
            eventDescriptionText.text = settings != null ? settings.GetEventDescription(tile.eventType) : string.Empty;

        RefreshEnemyPreview(tile);

        if (moveButton != null && runManager != null)
            moveButton.interactable = runManager.CanMoveTo(tile);
    }

    public void HidePanel()
    {
        currentTile = null;
        gameObject.SetActive(false);
    }

    private void RefreshEnemyPreview(WorldTileData tile)
    {
        bool showPreview = tile != null && tile.IsCombatEvent;
        if (enemyPreviewRoot != null)
            enemyPreviewRoot.SetActive(showPreview);

        for (int i = 0; i < enemyPortraitSlots.Count; i++)
        {
            Image slot = enemyPortraitSlots[i];
            if (slot == null)
                continue;

            bool hasSprite = showPreview && tile.previewEnemyPortraits != null && i < tile.previewEnemyPortraits.Count && tile.previewEnemyPortraits[i] != null;
            slot.gameObject.SetActive(hasSprite);
            if (hasSprite)
            {
                slot.sprite = tile.previewEnemyPortraits[i];
                slot.color = Color.white;
                slot.preserveAspect = true;
            }
        }
    }

    private void HandleMoveButtonClicked()
    {
        if (runManager == null)
            return;

        if (runManager.TryMoveToSelectedTile())
            HidePanel();
    }
}
