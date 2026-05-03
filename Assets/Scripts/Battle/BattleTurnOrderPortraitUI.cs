using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// One portrait cell in the top-left battle turn order strip.
/// Goal size is 116x136 at 2560x1440 reference resolution.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class BattleTurnOrderPortraitUI : MonoBehaviour, IPointerClickHandler
{
    [Header("References")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image frameImage;
    [SerializeField] private TMP_Text orderText;

    [Header("Team Backgrounds")]
    [SerializeField] private Sprite allyBackgroundSprite;
    [SerializeField] private Sprite enemyBackgroundSprite;
    [SerializeField] private GameObject allyBackgroundRoot;
    [SerializeField] private GameObject enemyBackgroundRoot;

    [Header("Current Turn Frame")]
    [Tooltip("Yellow frame shown only on the current turn owner. In your prefab this can be TurnFrameImage.")]
    [SerializeField] private Image currentTurnFrameImage;
    [SerializeField] private GameObject currentTurnFrameRoot;

    [Header("Finished Turn Dim")]
    [Tooltip("Dark overlay for units that already finished their turn this round. In your prefab this can be a child image covering the slot.")]
    [SerializeField] private Image dimOverlayImage;
    [SerializeField] private GameObject dimOverlayRoot;
    [SerializeField] private Color finishedDimColor = new Color(0f, 0f, 0f, 0.62f);

    [Header("Sizing")]
    [SerializeField] private bool enforceLayoutElementSize = true;
    [SerializeField] private Vector2 preferredSlotSize = new Vector2(116f, 136f);

    private BattleTurnOrderStripUI owner;
    private BattleUnit unit;
    private Graphic rootRaycastGraphic;

    public BattleUnit BoundUnit => unit;

    private void Awake()
    {
        EnsureRaycastTarget();
        ApplyPreferredSize();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ApplyPreferredSize();
    }
#endif

    public void Bind(
        BattleTurnOrderStripUI panelOwner,
        BattleUnit targetUnit,
        int displayIndex,
        bool isCurrent,
        bool isFinished,
        bool isUpcoming)
    {
        owner = panelOwner;
        unit = targetUnit;

        bool hasUnit = targetUnit != null && !targetUnit.IsDead;
        gameObject.SetActive(hasUnit);
        if (!hasUnit)
            return;

        if (portraitImage != null)
        {
            portraitImage.sprite = targetUnit.SlotFaceSprite;
            portraitImage.enabled = targetUnit.SlotFaceSprite != null;
            portraitImage.color = Color.white;
            portraitImage.raycastTarget = false;
        }

        RefreshTeamBackground(targetUnit.Team);
        RefreshCurrentFrame(isCurrent);
        RefreshFinishedDim(isFinished);

        if (orderText != null)
            orderText.text = (displayIndex + 1).ToString();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
            return;

        if (owner == null || unit == null || unit.IsDead)
            return;

        owner.HandlePortraitClicked(unit);
    }

    private void RefreshTeamBackground(TeamType team)
    {
        if (allyBackgroundRoot != null)
            allyBackgroundRoot.SetActive(team == TeamType.Ally);
        if (enemyBackgroundRoot != null)
            enemyBackgroundRoot.SetActive(team == TeamType.Enemy);

        if (backgroundImage != null)
        {
            Sprite sprite = team == TeamType.Ally ? allyBackgroundSprite : enemyBackgroundSprite;
            if (sprite != null)
                backgroundImage.sprite = sprite;

            backgroundImage.raycastTarget = false;
        }

        if (frameImage != null)
            frameImage.raycastTarget = false;
    }

    private void RefreshCurrentFrame(bool isCurrent)
    {
        if (currentTurnFrameRoot != null)
            currentTurnFrameRoot.SetActive(isCurrent);

        if (currentTurnFrameImage != null)
        {
            currentTurnFrameImage.gameObject.SetActive(isCurrent);
            currentTurnFrameImage.raycastTarget = false;
        }
    }

    private void RefreshFinishedDim(bool isFinished)
    {
        if (dimOverlayRoot != null)
            dimOverlayRoot.SetActive(isFinished);

        if (dimOverlayImage != null)
        {
            dimOverlayImage.gameObject.SetActive(isFinished);
            dimOverlayImage.color = isFinished ? finishedDimColor : new Color(0f, 0f, 0f, 0f);
            dimOverlayImage.raycastTarget = false;
        }
    }

    private void EnsureRaycastTarget()
    {
        rootRaycastGraphic = GetComponent<Graphic>();
        if (rootRaycastGraphic == null)
        {
            Image image = gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = true;
            rootRaycastGraphic = image;
        }
        else
        {
            rootRaycastGraphic.raycastTarget = true;
        }
    }

    private void ApplyPreferredSize()
    {
        if (!enforceLayoutElementSize)
            return;

        LayoutElement element = GetComponent<LayoutElement>();
        if (element == null)
            element = gameObject.AddComponent<LayoutElement>();

        element.preferredWidth = preferredSlotSize.x;
        element.preferredHeight = preferredSlotSize.y;
        element.flexibleWidth = 0f;
        element.flexibleHeight = 0f;
    }
}
