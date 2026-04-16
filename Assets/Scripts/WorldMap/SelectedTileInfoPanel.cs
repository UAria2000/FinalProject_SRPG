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
    [SerializeField] private TMP_Text moveButtonLabelText;

    [Header("Images")]
    [SerializeField] private Image tileIconImage;

    [Header("Buttons")]
    [SerializeField] private Button moveButton;
    [SerializeField] private Button closeButton;

    [Header("Enemy Preview")]
    [SerializeField] private GameObject enemyPreviewRoot;
    [SerializeField] private List<Image> enemyPreviewImages = new List<Image>(4);
    [SerializeField] private List<GameObject> enemyPreviewUnknownOverlays = new List<GameObject>(4);

    [Header("Unknown State")]
    [SerializeField] private string unknownTileName = "?";
    [SerializeField] private string unknownTileDescription = "아직 정보가 드러나지 않았다.";
    [SerializeField] private string occupyButtonText = "점령";
    [SerializeField] private string moveButtonText = "이동";

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

        bool isUnknown = !tile.revealed && tile.currentOwner != FactionType.Player;

        string factionText = "?";
        if (!isUnknown)
        {
            if (tile.currentOwner == FactionType.Player && runManager != null)
                factionText = runManager.PlayerDisplayName;
            else
            {
                FactionType displayFaction = tile.nativeFaction != FactionType.None ? tile.nativeFaction : tile.currentOwner;
                factionText = settings != null ? settings.GetFactionDisplayName(displayFaction) : displayFaction.ToString();
            }
        }

        if (factionNameText != null)
            factionNameText.text = factionText;

        if (eventNameText != null)
            eventNameText.text = isUnknown
                ? unknownTileName
                : (settings != null ? settings.GetEventDisplayName(tile.eventType) : tile.eventType.ToString());

        if (eventDescriptionText != null)
            eventDescriptionText.text = isUnknown
                ? unknownTileDescription
                : (settings != null ? settings.GetEventDescription(tile.eventType) : string.Empty);

        if (tileIconImage != null)
        {
            Sprite icon = isUnknown
                ? (settings != null ? settings.GetQuestionMarkSprite(tile) : null)
                : (settings != null ? settings.GetTileDisplayIcon(tile) : null);

            tileIconImage.gameObject.SetActive(icon != null);
            tileIconImage.sprite = icon;
            tileIconImage.color = icon != null ? Color.white : new Color(1f, 1f, 1f, 0f);
            tileIconImage.preserveAspect = true;
        }

        RefreshEnemyPreview(tile, isUnknown);

        bool canMove = moveButton != null && runManager != null && runManager.CanMoveTo(tile);
        if (moveButton != null)
            moveButton.interactable = canMove;
        if (moveButtonLabelText != null)
            moveButtonLabelText.text = tile.currentOwner == FactionType.Player ? moveButtonText : occupyButtonText;
    }

    public void HidePanel()
    {
        currentTile = null;
        gameObject.SetActive(false);
    }

    private void RefreshEnemyPreview(WorldTileData tile, bool isUnknownTile)
    {
        bool showPreview = tile != null && tile.IsCombatEvent;
        if (enemyPreviewRoot != null)
            enemyPreviewRoot.SetActive(showPreview);

        int revealCount = runManager != null ? runManager.RevealedEnemyPreviewCount : 0;

        for (int i = 0; i < enemyPreviewImages.Count; i++)
        {
            Image slot = enemyPreviewImages[i];
            if (slot == null)
                continue;

            bool hasPortrait = showPreview && tile.previewEnemyPortraits != null && i < tile.previewEnemyPortraits.Count && tile.previewEnemyPortraits[i] != null;
            bool isRevealedSlot = showPreview && !isUnknownTile && i < revealCount && hasPortrait;

            slot.gameObject.SetActive(showPreview);
            slot.sprite = isRevealedSlot ? tile.previewEnemyPortraits[i] : null;
            slot.color = isRevealedSlot ? Color.white : new Color(1f, 1f, 1f, 0f);
            slot.preserveAspect = true;

            if (i < enemyPreviewUnknownOverlays.Count && enemyPreviewUnknownOverlays[i] != null)
                enemyPreviewUnknownOverlays[i].SetActive(showPreview && !isRevealedSlot);
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
