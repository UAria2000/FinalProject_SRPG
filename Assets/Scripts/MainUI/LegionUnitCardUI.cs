using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LegionUnitCardUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private GameObject contentRoot;
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Image hpFillImage;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private GameObject exchangeableBadge;
    [SerializeField] private GameObject inPartyAura;
    [SerializeField] private GameObject favoriteOnRoot;
    [SerializeField] private GameObject favoriteOffRoot;
    [SerializeField] private Button favoriteButton;
    [SerializeField] private GameObject selectedGoldFrame;
    [SerializeField] private GameObject decomposeSelectedRoot;
    [SerializeField] private GameObject meleeIcon;
    [SerializeField] private GameObject midIcon;
    [SerializeField] private GameObject rangedIcon;

    private LegionPanelUI owner;

    public PersistentRosterUnitData BoundUnit { get; private set; }

    private void Awake()
    {
        if (favoriteButton != null)
        {
            favoriteButton.onClick.RemoveAllListeners();
            favoriteButton.onClick.AddListener(() => owner?.HandleCardFavoriteClicked(this));
        }
    }

    public void Bind(LegionPanelUI panelOwner, PersistentRosterUnitData unit, bool isInParty, bool isCurrentSelected, bool isDecomposeSelected, bool selectionMode)
    {
        owner = panelOwner;
        BoundUnit = unit;

        bool hasUnit = unit != null;
        if (contentRoot != null)
            contentRoot.SetActive(hasUnit);
        else
            gameObject.SetActive(hasUnit);

        if (!hasUnit)
            return;

        if (portraitImage != null)
        {
            Sprite battle = unit.unitViewDefinition != null ? unit.unitViewDefinition.GetBattleSprite() : null;
            portraitImage.gameObject.SetActive(battle != null);
            portraitImage.sprite = battle;
        }

        if (nameText != null)
            nameText.text = unit.GetDisplayName();
        if (levelText != null)
            levelText.text = $"Lv.{unit.currentLevel}";
        if (rankText != null)
            rankText.text = unit.promotionRank.ToString();

        float promoMultiplier = owner != null && owner.TryGetPromotionBonusPercentPerRank(out float perRank)
            ? LegionFormula.GetPromotionMultiplier(unit.promotionRank, perRank)
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
        if (favoriteOnRoot != null)
            favoriteOnRoot.SetActive(unit.isFavorite);
        if (favoriteOffRoot != null)
            favoriteOffRoot.SetActive(!unit.isFavorite);
        if (selectedGoldFrame != null)
            selectedGoldFrame.SetActive(isCurrentSelected);
        if (decomposeSelectedRoot != null)
            decomposeSelectedRoot.SetActive(selectionMode && isDecomposeSelected);

        CharacterRangeType range = unit.unitDefinition != null ? unit.unitDefinition.rangeType : CharacterRangeType.Melee;
        if (meleeIcon != null) meleeIcon.SetActive(range == CharacterRangeType.Melee);
        if (midIcon != null) midIcon.SetActive(range == CharacterRangeType.Mid);
        if (rangedIcon != null) rangedIcon.SetActive(range == CharacterRangeType.Ranged);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || BoundUnit == null)
            return;

        owner?.HandleUnitCardClicked(this);
    }
}
