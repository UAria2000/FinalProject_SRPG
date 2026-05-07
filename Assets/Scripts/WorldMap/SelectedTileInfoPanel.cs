using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectedTileInfoPanel : MonoBehaviour
{
    [Header("Frame Roots")]
    [SerializeField] private GameObject battleFrameRoot;
    [SerializeField] private GameObject nonBattleFrameRoot;

    [Header("Header")]
    [SerializeField] private List<Image> tileIconImages = new List<Image>(2);
    [SerializeField] private TMP_Text factionNameText;
    [SerializeField] private TMP_Text eventNameText;
    [SerializeField] private TMP_Text eventDescriptionText;

    [Header("Buttons")]
    [SerializeField] private Button moveButton;
    [SerializeField] private TMP_Text moveButtonLabelText;
    [SerializeField] private Button closeButton;

    [Header("Unknown Tile")]
    [SerializeField] private Sprite unknownTileIconSprite;
    [SerializeField] private string unknownTitleText = "?";
    [SerializeField] private string unknownDescriptionText = "아직 공개되지 않은 지역입니다.";

    [Header("Enemy Preview")]
    [SerializeField] private GameObject enemyPreviewRoot;
    [SerializeField] private List<Image> enemyPortraitSlots = new List<Image>(4);
    [SerializeField] private List<TMP_Text> enemyUnknownTexts = new List<TMP_Text>(4);

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

    private void Update()
    {
        if (!gameObject.activeInHierarchy)
            return;

        RefreshMoveButtonState();
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

        bool isCombatTile = tile.IsCombatEvent;
        bool isUnknownTile = !tile.revealed && tile.currentOwner != FactionType.Player;

        if (battleFrameRoot != null)
            battleFrameRoot.SetActive(isCombatTile);

        if (nonBattleFrameRoot != null)
            nonBattleFrameRoot.SetActive(!isCombatTile);

        if (isUnknownTile)
        {
            ApplyUnknownTileView();
        }
        else
        {
            ApplyKnownTileView(tile);
        }

        RefreshEnemyPreview(tile, isCombatTile, isUnknownTile);

        RefreshMoveButtonState();
    }

    public void HidePanel()
    {
        currentTile = null;
        gameObject.SetActive(false);
    }

    public void RefreshMoveButtonState()
    {
        if (moveButton == null)
            return;

        moveButton.interactable = runManager != null && !runManager.IsBusy && runManager.CanMoveTo(currentTile);
    }

    private void ApplyUnknownTileView()
    {
        SetAllTileIcons(unknownTileIconSprite);

        if (factionNameText != null)
            factionNameText.text = unknownTitleText;

        if (eventNameText != null)
            eventNameText.text = unknownTitleText;

        if (eventDescriptionText != null)
            eventDescriptionText.text = unknownDescriptionText;

        if (moveButtonLabelText != null)
            moveButtonLabelText.text = "이동";
    }

    private void ApplyKnownTileView(WorldTileData tile)
    {
        Sprite icon = settings != null ? settings.GetTileDisplayIcon(tile) : null;
        SetAllTileIcons(icon);

        if (factionNameText != null)
            factionNameText.text = settings != null
                ? settings.GetFactionDisplayName(tile.nativeFaction)
                : tile.nativeFaction.ToString();

        if (eventNameText != null)
            eventNameText.text = settings != null
                ? settings.GetEventDisplayName(tile.eventType)
                : tile.eventType.ToString();

        if (eventDescriptionText != null)
            eventDescriptionText.text = settings != null
                ? settings.GetEventDescription(tile.eventType)
                : string.Empty;

        if (moveButtonLabelText != null)
            moveButtonLabelText.text = "점령";
    }

    private void SetAllTileIcons(Sprite sprite)
    {
        for (int i = 0; i < tileIconImages.Count; i++)
        {
            Image img = tileIconImages[i];
            if (img == null)
                continue;

            bool hasSprite = sprite != null;
            img.gameObject.SetActive(hasSprite);
            img.sprite = sprite;
            img.color = hasSprite ? Color.white : new Color(1f, 1f, 1f, 0f);
            img.preserveAspect = true;
        }
    }

    private void RefreshEnemyPreview(WorldTileData tile, bool isCombatTile, bool isUnknownTile)
    {
        bool showPreview = tile != null && isCombatTile;
        if (enemyPreviewRoot != null)
            enemyPreviewRoot.SetActive(showPreview);

        for (int i = 0; i < enemyPortraitSlots.Count; i++)
        {
            Image portrait = enemyPortraitSlots[i];
            TMP_Text unknownText = i < enemyUnknownTexts.Count ? enemyUnknownTexts[i] : null;

            if (portrait == null && unknownText == null)
                continue;

            int revealCount = runManager != null ? runManager.RevealedEnemyPreviewCount : enemyPortraitSlots.Count;
            bool canRevealSlot = !isUnknownTile && i < revealCount;
            bool hasSprite =
                showPreview &&
                canRevealSlot &&
                tile.previewEnemyPortraits != null &&
                i < tile.previewEnemyPortraits.Count &&
                tile.previewEnemyPortraits[i] != null;

            if (portrait != null)
            {
                portrait.gameObject.SetActive(showPreview && hasSprite);
                if (hasSprite)
                {
                    portrait.sprite = tile.previewEnemyPortraits[i];
                    portrait.color = Color.white;
                    portrait.preserveAspect = true;
                }
            }

            if (unknownText != null)
            {
                bool showQuestion = showPreview && !hasSprite;
                unknownText.gameObject.SetActive(showQuestion);
                if (showQuestion)
                    unknownText.text = "?";
            }
        }
    }

    private void HandleMoveButtonClicked()
    {
        if (runManager == null || runManager.IsBusy)
            return;

        if (runManager.TryMoveToSelectedTile())
            HidePanel();
    }
}