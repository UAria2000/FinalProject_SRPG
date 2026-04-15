using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BarracksDetailPanelUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject emptyStateRoot;
    [SerializeField] private GameObject contentRoot;
    [SerializeField] private Image portraitImage;
    [SerializeField] private GameObject exchangeableBadge;
    [SerializeField] private Button favoriteButton;
    [SerializeField] private Image favoriteOnImage;
    [SerializeField] private Image favoriteOffImage;

    [Header("Basic Text")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text expText;
    [SerializeField] private Image hpFillImage;
    [SerializeField] private TMP_Text hpText;

    [Header("Stats")]
    [SerializeField] private TMP_Text dmgText;
    [SerializeField] private TMP_Text spdText;
    [SerializeField] private TMP_Text hitText;
    [SerializeField] private TMP_Text acText;
    [SerializeField] private TMP_Text criText;
    [SerializeField] private TMP_Text crdText;
    [SerializeField] private TMP_Text poisonResText;
    [SerializeField] private TMP_Text bleedResText;
    [SerializeField] private TMP_Text stunResText;
    [SerializeField] private TMP_Text epitaphText;

    [Header("Actions")]
    [SerializeField] private Button levelUpButton;
    [SerializeField] private TMP_Text levelUpCostText;
    [SerializeField] private Button promoteButton;
    [SerializeField] private TMP_Text promoteCostText;
    [SerializeField] private TMP_Text promotionRankText;
    [SerializeField] private TMP_Text promotionBonusText;
    [SerializeField] private TMP_Text classShardTypeText;
    [SerializeField] private TMP_Text classShardCountText;
    [SerializeField] private Button decomposeButton;

    private BarracksPanelUI owner;
    private PersistentProfileController profileController;
    private PersistentRosterUnitData boundUnit;

    private void Awake()
    {
        BindButton(favoriteButton, OnFavoriteClicked);
        BindButton(levelUpButton, OnLevelUpClicked);
        BindButton(promoteButton, OnPromoteClicked);
        BindButton(decomposeButton, OnDecomposeClicked);
    }

    public void Bind(BarracksPanelUI panelOwner, PersistentProfileController profile, PersistentRosterUnitData unit)
    {
        owner = panelOwner;
        profileController = profile;
        boundUnit = unit;

        bool hasUnit = unit != null;
        if (emptyStateRoot != null)
            emptyStateRoot.SetActive(!hasUnit);
        if (contentRoot != null)
            contentRoot.SetActive(hasUnit);

        if (!hasUnit)
            return;

        if (portraitImage != null)
        {
            Sprite bust = unit.unitViewDefinition != null ? unit.unitViewDefinition.GetBustPortraitSprite() : null;
            portraitImage.gameObject.SetActive(bust != null);
            portraitImage.sprite = bust;
        }

        if (exchangeableBadge != null)
            exchangeableBadge.SetActive(unit.isExchangeable);

        if (favoriteOnImage != null)
            favoriteOnImage.gameObject.SetActive(unit.isFavorite);
        if (favoriteOffImage != null)
            favoriteOffImage.gameObject.SetActive(!unit.isFavorite);

        if (nameText != null)
            nameText.text = unit.GetDisplayName();
        if (levelText != null)
            levelText.text = BarracksFormula.FormatLevelWithOriginal(unit);

        int needExp = BarracksFormula.GetExpToNextLevel(unit.currentLevel);
        int cap = profileController != null ? profileController.GetMainCharacterLevelCap() : unit.currentLevel;
        int clampedExp = unit.currentLevel >= cap ? needExp : Mathf.Clamp(unit.currentExp, 0, needExp);

        if (expText != null)
            expText.text = $"{clampedExp}/{needExp}";

        float promotionMultiplier = profileController != null
            ? BarracksFormula.GetPromotionMultiplier(unit.promotionRank, profileController.PromotionBonusPercentPerRank)
            : 1f;

        int maxHpBase = Mathf.Max(1, (unit.unitDefinition != null ? unit.unitDefinition.maxHP : 1) + (unit.statVariance != null ? unit.statVariance.maxHpDelta : 0));
        int maxHp = Mathf.Max(1, Mathf.RoundToInt(maxHpBase * promotionMultiplier));
        int currentHp = unit.persistentCurrentHP < 0 ? maxHp : Mathf.Clamp(unit.persistentCurrentHP, 0, maxHp);

        if (hpFillImage != null)
            hpFillImage.fillAmount = currentHp / (float)maxHp;
        if (hpText != null)
            hpText.text = $"{currentHp}/{maxHp}";

        BarracksEquipmentBonusSummary equip = profileController != null ? profileController.GetEquipmentBonusSummary(unit) : new BarracksEquipmentBonusSummary();
        ApplyStatTexts(unit, equip, promotionMultiplier);

        if (epitaphText != null)
            epitaphText.text = string.IsNullOrWhiteSpace(unit.fixedEpitaph) ? "-" : unit.fixedEpitaph;

        int soulCost = 0;
        bool canLevelUp = profileController != null && profileController.CanLevelUp(unit, out soulCost);

        if (levelUpButton != null)
            levelUpButton.interactable = canLevelUp;
        if (levelUpCostText != null)
            levelUpCostText.text = canLevelUp ? soulCost.ToString() : "MAX";

        ClassShardType classType = BarracksFormula.ResolveClassShardType(unit.unitDefinition);
        int ownedShards = profileController != null ? profileController.GetClassShardCount(classType) : 0;
        int requiredShards = BarracksFormula.GetPromotionCost(unit.promotionRank);
        bool canPromote = profileController != null && profileController.CanPromote(unit, out _);

        if (promoteButton != null)
            promoteButton.interactable = canPromote;
        if (promoteCostText != null)
            promoteCostText.text = requiredShards.ToString();
        if (promotionRankText != null)
            promotionRankText.text = $"승급 {unit.promotionRank}";
        if (promotionBonusText != null)
            promotionBonusText.text = $"올스탯 +{unit.promotionRank * (profileController != null ? profileController.PromotionBonusPercentPerRank : 1f):0.#}%";
        if (classShardTypeText != null)
            classShardTypeText.text = BarracksFormula.GetClassShardTypeLabel(classType);
        if (classShardCountText != null)
        {
            string color = ownedShards >= requiredShards ? "#5BD45B" : "#FF6666";
            classShardCountText.text = $"<color={color}>{ownedShards}/{requiredShards}</color>";
        }

        if (decomposeButton != null)
            decomposeButton.interactable = profileController != null && profileController.CanDecompose(unit);
    }

    private void OnFavoriteClicked()
    {
        if (boundUnit == null)
            return;

        owner?.HandleFavoriteToggleClicked();
    }

    private void OnLevelUpClicked()
    {
        if (boundUnit == null)
            return;

        owner?.HandleLevelUpClicked();
    }

    private void OnPromoteClicked()
    {
        if (boundUnit == null)
            return;

        owner?.HandlePromoteClicked();
    }

    private void OnDecomposeClicked()
    {
        if (boundUnit == null)
            return;

        owner?.HandleDecomposeClicked();
    }

    private void ApplyStatTexts(PersistentRosterUnitData unit, BarracksEquipmentBonusSummary equip, float promotionMultiplier)
    {
        UnitDefinition def = unit.unitDefinition;
        UnitInstanceStatVariance variance = unit.statVariance ?? new UnitInstanceStatVariance();

        if (dmgText != null)
            dmgText.text = FormatIntStat(def != null ? def.dmg : 0, variance.dmgDelta, equip.dmg, promotionMultiplier, "DMG");
        if (spdText != null)
            spdText.text = FormatIntStat(def != null ? def.spd : 0, variance.spdDelta, equip.spd, promotionMultiplier, "SPD");
        if (hitText != null)
            hitText.text = FormatScaledStat(def != null ? def.hit : 0f, variance.hitDeltaX10, equip.hitX10, promotionMultiplier, "HIT");
        if (acText != null)
            acText.text = FormatScaledStat(def != null ? def.ac : 0f, variance.acDeltaX10, equip.acX10, promotionMultiplier, "AC");
        if (criText != null)
            criText.text = FormatIntStat(def != null ? def.cri : 0, variance.criDelta, equip.cri, promotionMultiplier, "CRI");
        if (crdText != null)
            crdText.text = FormatIntStat(def != null ? def.crd : 0, variance.crdDelta, equip.crd, promotionMultiplier, "CRD");
        if (poisonResText != null)
            poisonResText.text = FormatIntStat(def != null ? def.poisonResist : 0, variance.poisonResistDelta, equip.poisonRes, promotionMultiplier, "독저항");
        if (bleedResText != null)
            bleedResText.text = FormatIntStat(def != null ? def.bleedResist : 0, variance.bleedResistDelta, equip.bleedRes, promotionMultiplier, "출혈저항");
        if (stunResText != null)
            stunResText.text = FormatIntStat(def != null ? def.stunResist : 0, variance.stunResistDelta, equip.stunRes, promotionMultiplier, "기절저항");
    }

    private string FormatIntStat(int baseValue, int varianceDelta, int equipBonus, float promotionMultiplier, string label)
    {
        int promotedValue = Mathf.RoundToInt((baseValue + varianceDelta) * promotionMultiplier);
        string baseText = $"{label} {promotedValue}";
        if (varianceDelta != 0)
            baseText = AppendColoredDelta(baseText, varianceDelta);
        if (equipBonus != 0)
            baseText += $" <color=#4AA3FF>({(equipBonus > 0 ? "+" : string.Empty)}{equipBonus})</color>";
        return baseText;
    }

    private string FormatScaledStat(float baseValue, int varianceRawX10, int equipRawX10, float promotionMultiplier, string label)
    {
        int promotedDisplay = Mathf.RoundToInt((baseValue + varianceRawX10) * promotionMultiplier * 10f);
        string text = $"{label} {promotedDisplay}";
        if (varianceRawX10 != 0)
            text = AppendColoredDelta(text, varianceRawX10 * 10);
        if (equipRawX10 != 0)
            text += $" <color=#4AA3FF>({(equipRawX10 > 0 ? "+" : string.Empty)}{equipRawX10 * 10})</color>";
        return text;
    }

    private string AppendColoredDelta(string prefix, int deltaDisplay)
    {
        if (deltaDisplay == 0)
            return prefix;

        string color = deltaDisplay > 0 ? "#5BD45B" : "#FF6666";
        string sign = deltaDisplay > 0 ? "+" : string.Empty;
        return $"{prefix} <color={color}>({sign}{deltaDisplay})</color>";
    }

    private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }
}
