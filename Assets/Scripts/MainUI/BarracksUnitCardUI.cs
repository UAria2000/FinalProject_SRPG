using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class BarracksUnitCardUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private GameObject contentRoot;
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Image hpFillImage;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private GameObject exchangeableBadge;
    [SerializeField] private GameObject inPartyAura;
    [SerializeField] private GameObject favoriteIcon;

    private BarracksPanelUI owner;
    private CanvasGroup canvasGroup;

    public PersistentRosterUnitData BoundUnit { get; private set; }

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void Bind(BarracksPanelUI panelOwner, PersistentRosterUnitData unit, bool isInParty)
    {
        owner = panelOwner;
        BoundUnit = unit;

        bool hasUnit = unit != null;
        if (contentRoot != null)
            contentRoot.SetActive(hasUnit);

        if (!hasUnit)
            return;

        if (portraitImage != null)
        {
            Sprite slotFace = unit.unitViewDefinition != null ? unit.unitViewDefinition.GetSlotFaceSprite() : null;
            portraitImage.gameObject.SetActive(slotFace != null);
            portraitImage.sprite = slotFace;
        }

        if (nameText != null)
            nameText.text = unit.GetDisplayName();

        if (levelText != null)
            levelText.text = $"Lv.{unit.currentLevel}";

        float promoMultiplier = owner != null && owner.TryGetPromotionBonusPercentPerRank(out float perRank)
            ? BarracksFormula.GetPromotionMultiplier(unit.promotionRank, perRank)
            : 1f;

        int maxHpBase = Mathf.Max(1, (unit.unitDefinition != null ? unit.unitDefinition.maxHP : 1) + (unit.statVariance != null ? unit.statVariance.maxHpDelta : 0));
        int maxHp = Mathf.Max(1, Mathf.RoundToInt(maxHpBase * promoMultiplier));
        int currentHp = unit.persistentCurrentHP < 0 ? maxHp : Mathf.Clamp(unit.persistentCurrentHP, 0, maxHp);
        if (hpFillImage != null)
            hpFillImage.fillAmount = currentHp / (float)maxHp;
        if (hpText != null)
            hpText.text = $"{currentHp}/{maxHp}";

        if (exchangeableBadge != null)
            exchangeableBadge.SetActive(unit.isExchangeable);
        if (inPartyAura != null)
            inPartyAura.SetActive(isInParty);
        if (favoriteIcon != null)
            favoriteIcon.SetActive(unit.isFavorite);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || BoundUnit == null)
            return;

        owner?.HandleUnitCardClicked(this);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || BoundUnit == null || owner == null)
            return;

        canvasGroup.blocksRaycasts = false;
        owner.BeginUnitCardDrag(this);

        if (portraitImage != null && portraitImage.sprite != null)
            UIDragGhostUI.Show(portraitImage.sprite, portraitImage.rectTransform);
    }

    public void OnDrag(PointerEventData eventData) { }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        UIDragGhostUI.HideGhost();
        owner?.EndUnitCardDrag(this);
    }
}
