using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum LegionStatKind
{
    Dmg,
    Spd,
    Hit,
    Ac,
    Idt,
    Cri,
    Crd,
    Poison,
    Bleed,
    Stun,
}

public class LegionDetailPanelUI : MonoBehaviour
{
    [Header("Roots")]
    [SerializeField] private GameObject emptyStateRoot;
    [SerializeField] private GameObject contentRoot;

    [Header("Header")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text expText;

    [Header("Rank")]
    [SerializeField] private Image rankImage;
    [SerializeField] private Sprite[] rankSprites; // 1~9

    [SerializeField] private GameObject exchangeableBadge;
    [SerializeField] private GameObject favoriteOnRoot;
    [SerializeField] private GameObject favoriteOffRoot;
    [SerializeField] private GameObject meleeIcon;
    [SerializeField] private GameObject midIcon;
    [SerializeField] private GameObject rangedIcon;

    [Header("Actions")]
    [SerializeField] private Button favoriteButton;
    [SerializeField] private Button renameButton;
    [SerializeField] private Button promoteButton;
    [SerializeField] private TMP_Text promoteCostText;
    [SerializeField] private Button levelUpButton;
    [SerializeField] private TMP_Text levelUpCostText;

    [Header("Skills")]
    [SerializeField] private LegionSkillEntryUI[] skillEntries;
    [SerializeField] private LegionSkillTooltipUI skillTooltipUI;

    [Header("Stats")]
    [SerializeField] private TMP_Text dmgText;
    [SerializeField] private TMP_Text spdText;
    [SerializeField] private TMP_Text hitText;
    [SerializeField] private TMP_Text acText;
    [SerializeField] private TMP_Text idtText;
    [SerializeField] private TMP_Text criText;
    [SerializeField] private TMP_Text crdText;
    [SerializeField] private TMP_Text poisonResText;
    [SerializeField] private TMP_Text bleedResText;
    [SerializeField] private TMP_Text stunResText;

    [Header("Stat Hover")]
    [SerializeField] private LegionStatHoverTargetUI dmgHover;
    [SerializeField] private LegionStatHoverTargetUI spdHover;
    [SerializeField] private LegionStatHoverTargetUI hitHover;
    [SerializeField] private LegionStatHoverTargetUI acHover;
    [SerializeField] private LegionStatHoverTargetUI idtHover;
    [SerializeField] private LegionStatHoverTargetUI criHover;
    [SerializeField] private LegionStatHoverTargetUI crdHover;
    [SerializeField] private LegionStatHoverTargetUI poisonHover;
    [SerializeField] private LegionStatHoverTargetUI bleedHover;
    [SerializeField] private LegionStatHoverTargetUI stunHover;
    [SerializeField] private LegionStatTooltipUI statTooltipUI;

    private LegionPanelUI owner;
    private PersistentProfileController profileController;
    private PersistentRosterUnitData boundUnit;

    public void Bind(LegionPanelUI ownerPanel, PersistentProfileController controller, PersistentRosterUnitData unit)
    {
        owner = ownerPanel;
        profileController = controller;
        boundUnit = unit;

        bool hasUnit = boundUnit != null;
        if (emptyStateRoot != null)
            emptyStateRoot.SetActive(!hasUnit);
        if (contentRoot != null)
            contentRoot.SetActive(hasUnit);

        if (!hasUnit)
            return;

        BindButton(favoriteButton, () => owner?.HandleFavoriteToggleClicked());
        BindButton(renameButton, () => owner?.HandleRenameClicked());
        BindButton(promoteButton, () => owner?.HandlePromoteClicked());
        BindButton(levelUpButton, () => owner?.HandleLevelUpClicked());

        RefreshHeader();
        RefreshSkills();
        RefreshStats();
        RefreshButtons();
        BindStatHoverTargets();
    }

    private void RefreshHeader()
    {
        if (boundUnit == null)
            return;

        int maxHp = GetMaxHp(boundUnit, out _, out _, out _);
        int currentHp = boundUnit.persistentCurrentHP < 0 ? maxHp : Mathf.Clamp(boundUnit.persistentCurrentHP, 0, maxHp);
        bool isDead = currentHp <= 0;

        if (portraitImage != null)
        {
            Sprite portrait = boundUnit.unitViewDefinition != null
                ? boundUnit.unitViewDefinition.GetBustPortraitSprite(isDead)
                : null;

            portraitImage.sprite = portrait;
            portraitImage.enabled = portrait != null;
        }

        SetText(nameText, boundUnit.GetDisplayName());
        SetText(levelText, $"Lv.{LegionFormula.FormatLevelWithOriginal(boundUnit)}");
        SetText(expText, $"{Mathf.Max(0, boundUnit.currentExp)}/{LegionFormula.GetExpToNextLevel(boundUnit.currentLevel)}");
        SetText(hpText, $"{currentHp}/{maxHp}");

        RefreshRankImage();

        if (exchangeableBadge != null)
            exchangeableBadge.SetActive(boundUnit.IsNftUnit());
        if (favoriteOnRoot != null)
            favoriteOnRoot.SetActive(boundUnit.isFavorite);
        if (favoriteOffRoot != null)
            favoriteOffRoot.SetActive(!boundUnit.isFavorite);

        CharacterRangeType range = boundUnit.unitDefinition != null ? boundUnit.unitDefinition.rangeType : CharacterRangeType.Melee;
        if (meleeIcon != null) meleeIcon.SetActive(range == CharacterRangeType.Melee);
        if (midIcon != null) midIcon.SetActive(range == CharacterRangeType.Mid);
        if (rangedIcon != null) rangedIcon.SetActive(range == CharacterRangeType.Ranged);
    }

    private void RefreshRankImage()
    {
        if (rankImage == null)
            return;

        int rank = boundUnit != null ? boundUnit.GetLegionRank() : 0;

        if (rank <= 0 || rankSprites == null || rankSprites.Length < rank || rankSprites[rank - 1] == null)
        {
            rankImage.gameObject.SetActive(false);
            return;
        }

        rankImage.gameObject.SetActive(true);
        rankImage.sprite = rankSprites[rank - 1];
    }

    private void RefreshSkills()
    {
        if (skillEntries == null)
            return;

        for (int i = 0; i < skillEntries.Length; i++)
        {
            if (skillEntries[i] == null)
                continue;

            SkillDefinition skill = null;
            if (boundUnit != null && boundUnit.learnedSkills != null && i < boundUnit.learnedSkills.Count)
                skill = boundUnit.learnedSkills[i];

            if (skill != null)
                skillEntries[i].Bind(skill, skillTooltipUI);
            else
                skillEntries[i].BindHidden();
        }
    }

    private void RefreshButtons()
    {
        if (profileController == null || boundUnit == null)
            return;

        bool canPromote = profileController.CanPromote(boundUnit, out int promoteCost);
        if (promoteButton != null)
            promoteButton.interactable = canPromote;
        SetText(promoteCostText, $"{profileController.GetPromotionShardCount():N0}/{promoteCost:N0}");

        bool canLevelUp = profileController.CanLevelUp(boundUnit, out int levelUpCost);
        if (levelUpButton != null)
            levelUpButton.interactable = canLevelUp;

        int soul = owner != null && owner.RuntimeWorldRunManager != null
            ? owner.RuntimeWorldRunManager.PersistentSoul
            : 0;

        SetText(levelUpCostText, $"{soul:N0}/{levelUpCost:N0}");
    }

    private void RefreshStats()
    {
        if (boundUnit == null)
            return;

        LegionEquipmentBonusSummary bonus = profileController != null ? profileController.GetEquipmentBonusSummary(boundUnit) : default;
        UnitDefinition def = boundUnit.unitDefinition;
        UnitInstanceStatVariance var = boundUnit.statVariance ?? new UnitInstanceStatVariance();

        SetText(dmgText, FormatStatValue((def?.dmg ?? 0) + var.dmgDelta + bonus.dmg));
        SetText(spdText, FormatStatValue((def?.spd ?? 0) + var.spdDelta + bonus.spd));
        SetText(hitText, FormatStatValue(Mathf.RoundToInt((def != null ? def.hit : 0f) * 10f) + var.hitDeltaX10 + bonus.hitX10));
        SetText(acText, FormatStatValue(Mathf.RoundToInt((def != null ? def.ac : 0f) * 10f) + var.acDeltaX10 + bonus.acX10));
        SetText(idtText, FormatPercentStat(GetIncomingDamageTakenTotal(def, var, bonus)));
        SetText(criText, FormatStatValue((def?.cri ?? 0) + var.criDelta + bonus.cri));
        SetText(crdText, FormatStatValue((def?.crd ?? 0) + var.crdDelta + bonus.crd));
        SetText(poisonResText, FormatStatValue((def?.poisonResist ?? 0) + bonus.poisonRes));
        SetText(bleedResText, FormatStatValue((def?.bleedResist ?? 0) + bonus.bleedRes));
        SetText(stunResText, FormatStatValue((def?.stunResist ?? 0) + bonus.stunRes));
    }

    private void BindStatHoverTargets()
    {
        BindHover(dmgHover, LegionStatKind.Dmg, "DMG");
        BindHover(spdHover, LegionStatKind.Spd, "SPD");
        BindHover(hitHover, LegionStatKind.Hit, "HIT");
        BindHover(acHover, LegionStatKind.Ac, "AC");
        BindHover(idtHover, LegionStatKind.Idt, "IDT");
        BindHover(criHover, LegionStatKind.Cri, "CRI");
        BindHover(crdHover, LegionStatKind.Crd, "CRD");
        BindHover(poisonHover, LegionStatKind.Poison, "중독 저항");
        BindHover(bleedHover, LegionStatKind.Bleed, "출혈 저항");
        BindHover(stunHover, LegionStatKind.Stun, "기절 저항");
    }

    private void BindHover(LegionStatHoverTargetUI target, LegionStatKind kind, string label)
    {
        if (target != null)
            target.Bind(this, kind, label);
    }

    public void ShowStatTooltip(LegionStatKind kind, string statLabel)
    {
        if (statTooltipUI == null || boundUnit == null)
            return;

        UnitDefinition def = boundUnit.unitDefinition;
        UnitInstanceStatVariance var = boundUnit.statVariance ?? new UnitInstanceStatVariance();
        LegionEquipmentBonusSummary bonus = profileController != null ? profileController.GetEquipmentBonusSummary(boundUnit) : default;

        int baseValue = 0;
        int varianceValue = 0;
        int equipValue = 0;
        string suffix = string.Empty;

        switch (kind)
        {
            case LegionStatKind.Dmg:
                baseValue = def != null ? def.dmg : 0;
                varianceValue = var.dmgDelta;
                equipValue = bonus.dmg;
                break;

            case LegionStatKind.Spd:
                baseValue = def != null ? def.spd : 0;
                varianceValue = var.spdDelta;
                equipValue = bonus.spd;
                break;

            case LegionStatKind.Hit:
                baseValue = Mathf.RoundToInt((def != null ? def.hit : 0f) * 10f);
                varianceValue = var.hitDeltaX10;
                equipValue = bonus.hitX10;
                break;

            case LegionStatKind.Ac:
                baseValue = Mathf.RoundToInt((def != null ? def.ac : 0f) * 10f);
                varianceValue = var.acDeltaX10;
                equipValue = bonus.acX10;
                break;

            case LegionStatKind.Idt:
                baseValue = GetOptionalInt(def,
                    "incomingDamageTakenReduction",
                    "idt",
                    "incomingDamageTaken",
                    "damageTakenReduction",
                    "incomingDamageReduction");
                varianceValue = GetOptionalInt(var,
                    "idtDelta",
                    "incomingDamageTakenReductionDelta",
                    "damageTakenReductionDelta");
                equipValue = GetOptionalInt(bonus,
                    "idt",
                    "incomingDamageTakenReduction",
                    "damageTakenReduction");
                suffix = "%";
                break;

            case LegionStatKind.Cri:
                baseValue = def != null ? def.cri : 0;
                varianceValue = var.criDelta;
                equipValue = bonus.cri;
                suffix = "%";
                break;

            case LegionStatKind.Crd:
                baseValue = def != null ? def.crd : 0;
                varianceValue = var.crdDelta;
                equipValue = bonus.crd;
                suffix = "%";
                break;

            case LegionStatKind.Poison:
                baseValue = def != null ? def.poisonResist : 0;
                equipValue = bonus.poisonRes;
                suffix = "%";
                break;

            case LegionStatKind.Bleed:
                baseValue = def != null ? def.bleedResist : 0;
                equipValue = bonus.bleedRes;
                suffix = "%";
                break;

            case LegionStatKind.Stun:
                baseValue = def != null ? def.stunResist : 0;
                equipValue = bonus.stunRes;
                suffix = "%";
                break;
        }

        int total = baseValue + varianceValue + equipValue;
        statTooltipUI.Show(
            statLabel,
            total + suffix,
            baseValue + suffix,
            Signed(varianceValue) + suffix,
            Signed(equipValue) + suffix);
    }

    public void HideStatTooltip()
    {
        statTooltipUI?.Hide();
    }

    private int GetMaxHp(PersistentRosterUnitData unit, out int baseHp, out int varianceHp, out int equipHp)
    {
        baseHp = unit != null && unit.unitDefinition != null ? unit.unitDefinition.maxHP : 1;
        varianceHp = unit != null && unit.statVariance != null ? unit.statVariance.maxHpDelta : 0;
        LegionEquipmentBonusSummary bonus = profileController != null ? profileController.GetEquipmentBonusSummary(unit) : default;
        equipHp = bonus.maxHp;

        float promo = profileController != null
            ? LegionFormula.GetPromotionMultiplier(unit.promotionRank, profileController.PromotionBonusPercentPerRank)
            : 1f;

        return Mathf.Max(1, Mathf.RoundToInt((baseHp + varianceHp + equipHp) * promo));
    }

    private int GetIncomingDamageTakenTotal(UnitDefinition def, UnitInstanceStatVariance var, LegionEquipmentBonusSummary bonus)
    {
        int baseValue = GetOptionalInt(def,
            "incomingDamageTakenReduction",
            "idt",
            "incomingDamageTaken",
            "damageTakenReduction",
            "incomingDamageReduction");

        int varianceValue = GetOptionalInt(var,
            "idtDelta",
            "incomingDamageTakenReductionDelta",
            "damageTakenReductionDelta");

        int equipValue = GetOptionalInt(bonus,
            "idt",
            "incomingDamageTakenReduction",
            "damageTakenReduction");

        return baseValue + varianceValue + equipValue;
    }

    private static int GetOptionalInt(object target, params string[] candidateNames)
    {
        if (target == null || candidateNames == null)
            return 0;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        System.Type type = target.GetType();

        for (int i = 0; i < candidateNames.Length; i++)
        {
            string name = candidateNames[i];
            if (string.IsNullOrWhiteSpace(name))
                continue;

            FieldInfo field = type.GetField(name, flags);
            if (field != null)
                return ConvertToInt(field.GetValue(target));

            PropertyInfo property = type.GetProperty(name, flags);
            if (property != null && property.CanRead)
                return ConvertToInt(property.GetValue(target));
        }

        return 0;
    }

    private static int ConvertToInt(object value)
    {
        if (value == null)
            return 0;

        switch (value)
        {
            case int intValue:
                return intValue;
            case float floatValue:
                return Mathf.RoundToInt(floatValue);
            case double doubleValue:
                return Mathf.RoundToInt((float)doubleValue);
            case long longValue:
                return (int)longValue;
            case short shortValue:
                return shortValue;
            case byte byteValue:
                return byteValue;
            default:
                return 0;
        }
    }

    private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value;
    }

    private static string FormatStatValue(int value) => value.ToString();
    private static string FormatPercentStat(int value) => $"{value}%";
    private static string Signed(int value) => value > 0 ? $"+{value}" : value.ToString();
}