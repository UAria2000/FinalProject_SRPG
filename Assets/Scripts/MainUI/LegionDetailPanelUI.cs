using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public enum LegionStatKind
{
    Dmg,
    Hit,
    Ac,
    Idt,
    Cri,
    Crd,
    Spd,
    Stun,
    Bleed,
    Burn,
    Frost,
    Blind,
}

public class LegionDetailPanelUI : MonoBehaviour
{
    private enum InfoViewMode
    {
        MainStats,
        ResistStats
    }

    [Header("Roots")]
    [SerializeField] private GameObject emptyStateRoot;
    [SerializeField] private GameObject contentRoot;

    [Header("Header")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text nameText;
    [Tooltip("구형 통합 레벨 텍스트. 새 UI에서는 비워두고 Current Level Value Text를 쓰면 됩니다.")]
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text currentLevelValueText;
    [Tooltip("오리지널 레벨 라벨/괄호까지 함께 숨기고 싶으면 이 루트를 연결합니다.")]
    [SerializeField] private GameObject originalLevelRoot;
    [SerializeField] private TMP_Text originalLevelValueText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private Image hpFillImage;
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TMP_Text expText;
    [SerializeField] private TMP_Text expCurrentValueText;
    [SerializeField] private TMP_Text expMaxValueText;

    [Header("Rank")]
    [SerializeField] private Image rankImage;
    [SerializeField] private Sprite[] rankSprites; // 1~9

    [Header("Badges")]
    [SerializeField] private GameObject exchangeableBadge;
    [SerializeField] private GameObject favoriteOnRoot;
    [SerializeField] private GameObject favoriteOffRoot;
    [SerializeField] private GameObject meleeIcon;
    [SerializeField] private GameObject midIcon;
    [SerializeField] private GameObject rangedIcon;
    [SerializeField] private GameObject classBadgeRoot;
    [SerializeField] private Image classBadgeImage;
    [SerializeField] private Sprite meleeClassBadgeSprite;
    [SerializeField] private Sprite midClassBadgeSprite;
    [SerializeField] private Sprite rangedClassBadgeSprite;

    [Header("Equipment Display - Read Only")]
    [SerializeField] private GameObject[] equipmentSlotRoots = new GameObject[2];
    [SerializeField] private Image[] equipmentIconImages = new Image[2];

    [Header("Actions")]
    [SerializeField] private Button favoriteButton;
    [SerializeField] private Button renameButton;
    [SerializeField] private Button promoteButton;
    [SerializeField] private TMP_Text promoteCostText;
    [SerializeField] private Button levelUpButton;
    [SerializeField] private TMP_Text levelUpCostText;

    [Header("Info Area Roots")]
    [Tooltip("7개 기본 스탯 영역. 클릭 시 저항 스탯으로 전환합니다.")]
    [SerializeField] private GameObject mainStatsRoot;
    [Tooltip("5개 저항 스탯 영역. 클릭 시 기본 스탯으로 전환합니다.")]
    [SerializeField] private GameObject resistStatsRoot;
    [Tooltip("스킬 설명 영역. 스킬 버튼을 누르면 기본/저항 영역 대신 표시합니다.")]
    [SerializeField] private GameObject skillDescriptionRoot;
    [SerializeField] private Button infoAreaToggleButton;
    [SerializeField] private Button skillDescriptionBackButton;
    [SerializeField] private TMP_Text infoModeLabelText;

    [Header("Skills")]
    [Tooltip("구형/간단 스킬 슬롯. 0=평타, 1~3=보유 스킬로 바인딩됩니다.")]
    [SerializeField] private LegionSkillEntryUI[] skillEntries;
    [SerializeField] private LegionSkillTooltipUI skillTooltipUI;
    [Tooltip("새 UI용 직접 버튼 배열. 비워두면 LegionSkillEntryUI 클릭만 사용합니다.")]
    [SerializeField] private Button[] skillButtons = new Button[4];
    [SerializeField] private Image[] skillIcons = new Image[4];
    [SerializeField] private TMP_Text[] skillNameTexts = new TMP_Text[4];
    [SerializeField] private GameObject[] skillSlotRoots = new GameObject[4];
    [Tooltip("각 스킬 버튼 안의 SelectedFrame 오브젝트를 순서대로 연결합니다.")]
    [SerializeField] private GameObject[] selectedFrameRoots = new GameObject[4];

    [Header("Main Stats - DMG / HIT / AC / IDT / CRI / CRD / SPD")]
    [SerializeField] private TMP_Text dmgText;
    [SerializeField] private TMP_Text hitText;
    [SerializeField] private TMP_Text acText;
    [SerializeField] private TMP_Text idtText;
    [SerializeField] private TMP_Text criText;
    [SerializeField] private TMP_Text crdText;
    [SerializeField] private TMP_Text spdText;

    [Header("Resistance Stats - Stun / Bleed / Burn / Frost / Blind")]
    [SerializeField] private TMP_Text stunResText;
    [SerializeField] private TMP_Text bleedResText;
    [FormerlySerializedAs("poisonResText")]
    [SerializeField] private TMP_Text burnResText;
    [SerializeField] private TMP_Text frostResText;
    [SerializeField] private TMP_Text blindResText;

    [Header("Skill Description - Header")]
    [SerializeField] private Image selectedSkillIcon;
    [SerializeField] private Image selectedSkillClassBadgeImage;
    [SerializeField] private TMP_Text selectedSkillClassBadgeText;
    [SerializeField] private TMP_Text selectedSkillNameText;

    [Header("Skill Class Badge Sprites")]
    [SerializeField] private Sprite meleeSkillClassBadgeSprite;
    [SerializeField] private Sprite midSkillClassBadgeSprite;
    [SerializeField] private Sprite rangedSkillClassBadgeSprite;
    [SerializeField] private Sprite commonSkillClassBadgeSprite;
    [SerializeField] private Sprite uniqueSkillClassBadgeSprite;

    [Header("Skill Description - Positions")]
    [SerializeField] private GameObject[] usablePositionHexRoots = new GameObject[4];
    [SerializeField] private Image[] usablePositionHexImages = new Image[4];
    [SerializeField] private TMP_Text[] usablePositionHexTexts = new TMP_Text[4];
    [SerializeField] private GameObject[] targetPositionHexRoots = new GameObject[4];
    [SerializeField] private Image[] targetPositionHexImages = new Image[4];
    [SerializeField] private TMP_Text[] targetPositionHexTexts = new TMP_Text[4];

    [Header("Skill Description - Position Hex Sprites")]
    [SerializeField] private Sprite usablePositionHexSprite;
    [SerializeField] private Sprite targetEnemyPositionHexSprite;
    [SerializeField] private Sprite targetAllyPositionHexSprite;
    [SerializeField] private Sprite emptyPositionHexSprite;

    [Header("Skill Description - Texts")]
    [SerializeField] private TMP_Text selectedSkillDescriptionText;
    [SerializeField] private TMP_Text selectedSkillPowerText;
    [SerializeField] private TMP_Text selectedSkillAccuracyText;
    [SerializeField] private TMP_Text selectedSkillCooldownText;
    [SerializeField] private TMP_Text selectedSkillEffectText;

    [Header("Stat Hover")]
    [SerializeField] private LegionStatHoverTargetUI dmgHover;
    [SerializeField] private LegionStatHoverTargetUI hitHover;
    [SerializeField] private LegionStatHoverTargetUI acHover;
    [SerializeField] private LegionStatHoverTargetUI idtHover;
    [SerializeField] private LegionStatHoverTargetUI criHover;
    [SerializeField] private LegionStatHoverTargetUI crdHover;
    [SerializeField] private LegionStatHoverTargetUI spdHover;
    [SerializeField] private LegionStatHoverTargetUI stunHover;
    [SerializeField] private LegionStatHoverTargetUI bleedHover;
    [FormerlySerializedAs("poisonHover")]
    [SerializeField] private LegionStatHoverTargetUI burnHover;
    [SerializeField] private LegionStatHoverTargetUI frostHover;
    [SerializeField] private LegionStatHoverTargetUI blindHover;
    [SerializeField] private LegionStatTooltipUI statTooltipUI;

    private LegionPanelUI owner;
    private PersistentProfileController profileController;
    private PersistentRosterUnitData boundUnit;
    private InfoViewMode viewMode = InfoViewMode.MainStats;
    private int selectedSkillIndex = -1;
    private bool buttonsBound;

    private void Awake()
    {
        BindButtonsOnce();
    }

    private void OnDisable()
    {
        ResetToInitialView();
        HideStatTooltip();
        skillTooltipUI?.Hide();
    }

    public void Bind(LegionPanelUI ownerPanel, PersistentProfileController controller, PersistentRosterUnitData unit)
    {
        BindButtonsOnce();

        bool changedUnit = boundUnit == null || unit == null || boundUnit.instanceId != unit.instanceId;
        owner = ownerPanel;
        profileController = controller;
        boundUnit = unit;

        if (changedUnit)
            ResetToInitialView();

        bool hasUnit = boundUnit != null;
        if (emptyStateRoot != null)
            emptyStateRoot.SetActive(!hasUnit);
        if (contentRoot != null)
            contentRoot.SetActive(hasUnit);

        if (!hasUnit)
        {
            RefreshViewRoots();
            RefreshSelectedFrames();
            return;
        }

        BindButton(favoriteButton, () => owner?.HandleFavoriteToggleClicked());
        BindButton(renameButton, () => owner?.HandleRenameClicked());
        BindButton(promoteButton, () => owner?.HandlePromoteClicked());
        BindButton(levelUpButton, () => owner?.HandleLevelUpClicked());

        RefreshHeader();
        RefreshRankImage();
        RefreshBadges();
        RefreshClassBadge();
        RefreshEquipmentSlots();
        RefreshSkills();
        RefreshStats();
        RefreshButtons();
        BindStatHoverTargets();
        RefreshViewRoots();
        RefreshSelectedFrames();

        if (selectedSkillIndex >= 0)
            RefreshSkillDescription(GetActionSkillAt(selectedSkillIndex));
    }

    public void ToggleStatResistanceMode()
    {
        viewMode = viewMode == InfoViewMode.MainStats ? InfoViewMode.ResistStats : InfoViewMode.MainStats;
        RefreshViewRoots();
        RefreshSelectedFrames();
    }

    public void ReturnToMainStats()
    {
        ResetToInitialView();
        RefreshViewRoots();
        RefreshSelectedFrames();
        RefreshSkills();
    }

    private void HideSkillDescription()
    {
        selectedSkillIndex = -1;
        RefreshViewRoots();
        RefreshSelectedFrames();
        RefreshSkills();
    }

    private void ResetToInitialView()
    {
        viewMode = InfoViewMode.MainStats;
        selectedSkillIndex = -1;
    }

    private void BindButtonsOnce()
    {
        if (buttonsBound)
            return;

        buttonsBound = true;

        if (infoAreaToggleButton != null)
        {
            infoAreaToggleButton.onClick.RemoveAllListeners();
            infoAreaToggleButton.onClick.AddListener(ToggleStatResistanceMode);
        }

        if (skillDescriptionBackButton != null)
        {
            skillDescriptionBackButton.onClick.RemoveAllListeners();
            skillDescriptionBackButton.onClick.AddListener(HideSkillDescription);
        }

        for (int i = 0; i < 4; i++)
        {
            int slot = i;
            Button button = GetSkillButton(slot);
            if (button == null)
                continue;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(delegate { OnSkillButtonPressed(slot); });
        }
    }

    private Button GetSkillButton(int index)
    {
        if (skillButtons != null && index >= 0 && index < skillButtons.Length && skillButtons[index] != null)
            return skillButtons[index];

        if (skillSlotRoots != null && index >= 0 && index < skillSlotRoots.Length && skillSlotRoots[index] != null)
            return skillSlotRoots[index].GetComponent<Button>();

        return null;
    }

    private void OnSkillButtonPressed(int slotIndex)
    {
        SkillDefinition skill = GetActionSkillAt(slotIndex);
        if (skill == null)
            return;

        if (selectedSkillIndex == slotIndex)
        {
            HideSkillDescription();
            return;
        }

        selectedSkillIndex = slotIndex;
        RefreshSkillDescription(skill);
        RefreshViewRoots();
        RefreshSelectedFrames();
        RefreshSkills();
    }

    private void OnSkillEntryPressed(SkillDefinition skill)
    {
        if (skill == null)
            return;

        int index = FindSkillSlotIndex(skill);
        if (index < 0)
            return;

        OnSkillButtonPressed(index);
    }

    private int FindSkillSlotIndex(SkillDefinition skill)
    {
        if (skill == null)
            return -1;

        for (int i = 0; i < 4; i++)
        {
            if (GetActionSkillAt(i) == skill)
                return i;
        }

        return -1;
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
        RefreshLevelTexts();
        RefreshHp(maxHp, currentHp);
        RefreshExpTexts();
    }

    private void RefreshLevelTexts()
    {
        int current = boundUnit != null ? Mathf.Max(1, boundUnit.currentLevel) : 1;
        int original = boundUnit != null ? Mathf.Max(1, boundUnit.originalLevel) : current;
        bool showOriginal = current < original;

        SetText(levelText, $"Lv.{current}");
        SetText(currentLevelValueText, current.ToString());

        if (originalLevelValueText != null)
        {
            originalLevelValueText.text = $"({original})";
            if (originalLevelRoot == null)
                originalLevelValueText.gameObject.SetActive(showOriginal);
        }

        if (originalLevelRoot != null)
            originalLevelRoot.SetActive(showOriginal);
    }

    private void RefreshHp(int maxHp, int currentHp)
    {
        SetText(hpText, $"{currentHp}/{maxHp}");
        float hp01 = maxHp > 0 ? Mathf.Clamp01(currentHp / (float)maxHp) : 0f;

        if (hpFillImage != null)
            hpFillImage.fillAmount = hp01;

        if (hpSlider != null)
        {
            hpSlider.minValue = 0f;
            hpSlider.maxValue = 1f;
            hpSlider.value = hp01;
        }
    }

    private void RefreshExpTexts()
    {
        if (boundUnit == null)
            return;

        int need = LegionFormula.GetExpToNextLevel(boundUnit.currentLevel);
        int current = Mathf.Clamp(boundUnit.currentExp, 0, need);

        SetText(expText, $"{current}/{need}");
        SetText(expCurrentValueText, current.ToString("N0"));
        SetText(expMaxValueText, need.ToString("N0"));
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

    private void RefreshBadges()
    {
        if (boundUnit == null)
            return;

        if (exchangeableBadge != null)
            exchangeableBadge.SetActive(boundUnit.IsNftUnit());
        if (favoriteOnRoot != null)
            favoriteOnRoot.SetActive(boundUnit.isFavorite);
        if (favoriteOffRoot != null)
            favoriteOffRoot.SetActive(!boundUnit.isFavorite);
    }

    private void RefreshClassBadge()
    {
        CharacterRangeType range = boundUnit != null && boundUnit.unitDefinition != null
            ? boundUnit.unitDefinition.rangeType
            : CharacterRangeType.Melee;

        if (meleeIcon != null) meleeIcon.SetActive(range == CharacterRangeType.Melee);
        if (midIcon != null) midIcon.SetActive(range == CharacterRangeType.Mid);
        if (rangedIcon != null) rangedIcon.SetActive(range == CharacterRangeType.Ranged);

        Sprite badge = null;
        switch (range)
        {
            case CharacterRangeType.Mid:
                badge = midClassBadgeSprite;
                break;
            case CharacterRangeType.Ranged:
                badge = rangedClassBadgeSprite;
                break;
            default:
                badge = meleeClassBadgeSprite;
                break;
        }

        if (classBadgeImage != null)
        {
            classBadgeImage.sprite = badge;
            classBadgeImage.gameObject.SetActive(badge != null);
        }

        if (classBadgeRoot != null)
            classBadgeRoot.SetActive(badge != null || meleeIcon != null || midIcon != null || rangedIcon != null);
    }

    private void RefreshEquipmentSlots()
    {
        for (int i = 0; i < 2; i++)
        {
            if (equipmentSlotRoots != null && i < equipmentSlotRoots.Length && equipmentSlotRoots[i] != null)
                equipmentSlotRoots[i].SetActive(boundUnit != null);

            ItemDefinition item = GetAssignedEquipment(i);
            if (equipmentIconImages != null && i < equipmentIconImages.Length && equipmentIconImages[i] != null)
            {
                equipmentIconImages[i].sprite = item != null ? item.icon : null;
                equipmentIconImages[i].enabled = item != null && item.icon != null;
                equipmentIconImages[i].color = item != null && item.icon != null ? Color.white : new Color(1f, 1f, 1f, 0f);
            }
        }
    }

    private ItemDefinition GetAssignedEquipment(int slotIndex)
    {
        if (owner == null || owner.RuntimeWorldRunManager == null || boundUnit == null)
            return null;

        PartyMemberData member = FindRuntimePartyMember(boundUnit.instanceId);
        return member != null ? owner.RuntimeWorldRunManager.GetAssignedEquipmentItem(member, slotIndex) : null;
    }

    private PartyMemberData FindRuntimePartyMember(string instanceId)
    {
        if (owner == null || owner.RuntimeWorldRunManager == null || string.IsNullOrWhiteSpace(instanceId))
            return null;

        BattlePartyRuntimeState runtime = owner.RuntimeWorldRunManager.GetOrCreatePlayerPartyRuntimeState();
        if (runtime == null || runtime.members == null)
            return null;

        for (int i = 0; i < runtime.members.Count; i++)
        {
            PartyMemberData member = runtime.members[i];
            if (member != null && member.instanceId == instanceId)
                return member;
        }

        return null;
    }

    private void RefreshSkills()
    {
        for (int i = 0; i < 4; i++)
        {
            SkillDefinition skill = GetActionSkillAt(i);
            bool hasSkill = skill != null;

            if (skillSlotRoots != null && i < skillSlotRoots.Length && skillSlotRoots[i] != null)
                skillSlotRoots[i].SetActive(true);

            if (skillIcons != null && i < skillIcons.Length && skillIcons[i] != null)
            {
                skillIcons[i].sprite = hasSkill ? skill.icon : null;
                skillIcons[i].color = hasSkill && skill.icon != null ? Color.white : new Color(1f, 1f, 1f, 0.2f);
                skillIcons[i].enabled = hasSkill && skill.icon != null;
            }

            if (skillNameTexts != null && i < skillNameTexts.Length && skillNameTexts[i] != null)
                skillNameTexts[i].text = hasSkill ? skill.skillName : string.Empty;

            Button button = GetSkillButton(i);
            if (button != null)
                button.interactable = hasSkill;

            if (skillEntries != null && i < skillEntries.Length && skillEntries[i] != null)
            {
                if (hasSkill)
                    skillEntries[i].Bind(skill, skillTooltipUI, OnSkillEntryPressed, selectedSkillIndex == i);
                else
                    skillEntries[i].BindHidden();
            }
        }
    }

    private SkillDefinition GetActionSkillAt(int slotIndex)
    {
        if (boundUnit == null || boundUnit.unitDefinition == null)
            return null;

        if (slotIndex == 0)
            return boundUnit.unitDefinition.basicAttack;

        int learnedIndex = slotIndex - 1;
        if (boundUnit.learnedSkills == null || learnedIndex < 0 || learnedIndex >= boundUnit.learnedSkills.Count)
            return null;

        return boundUnit.learnedSkills[learnedIndex];
    }

    private void RefreshStats()
    {
        if (boundUnit == null)
            return;

        LegionStatSnapshot stats = BuildStatSnapshot(boundUnit);

        SetText(dmgText, stats.dmg.ToString());
        SetText(hitText, stats.hitDisplay.ToString());
        SetText(acText, stats.acDisplay.ToString());
        SetText(idtText, BattleStatFormatter.FormatPercent(stats.idt));
        SetText(criText, BattleStatFormatter.FormatPercent(stats.cri));
        SetText(crdText, stats.crd.ToString());
        SetText(spdText, stats.spd.ToString());

        SetText(stunResText, BattleStatFormatter.FormatPercent(stats.stunRes));
        SetText(bleedResText, BattleStatFormatter.FormatPercent(stats.bleedRes));
        SetText(burnResText, BattleStatFormatter.FormatPercent(stats.burnRes));
        SetText(frostResText, BattleStatFormatter.FormatPercent(stats.frostRes));
        SetText(blindResText, BattleStatFormatter.FormatPercent(stats.blindRes));
    }

    private LegionStatSnapshot BuildStatSnapshot(PersistentRosterUnitData unit)
    {
        LegionStatSnapshot stats = new LegionStatSnapshot();
        if (unit == null)
            return stats;

        UnitDefinition def = unit.unitDefinition;
        UnitInstanceStatVariance var = unit.statVariance ?? new UnitInstanceStatVariance();
        LegionEquipmentBonusSummary bonus = profileController != null ? profileController.GetEquipmentBonusSummary(unit) : default;

        float promo = profileController != null
            ? LegionFormula.GetPromotionMultiplier(unit.promotionRank, profileController.PromotionBonusPercentPerRank)
            : 1f;

        stats.maxHp = ApplyPromotionToInt(Mathf.Max(1, (def?.maxHP ?? 1) + var.maxHpDelta + Mathf.Max(0, unit.levelGrowthMaxHp) + bonus.maxHp), promo);
        stats.dmg = ApplyPromotionToInt(Mathf.Max(0, (def?.dmg ?? 0) + var.dmgDelta + Mathf.Max(0, unit.levelGrowthDmg) + bonus.dmg), promo);
        stats.spd = ApplyPromotionToInt(Mathf.Max(0, (def?.spd ?? 0) + var.spdDelta + bonus.spd), promo);
        stats.idt = ApplyPromotionToInt((def?.idt ?? 0) + var.idtDelta + bonus.idt, promo);
        stats.cri = ApplyPromotionToInt(Mathf.Max(0, (def?.cri ?? 0) + var.criDelta + bonus.cri), promo);
        stats.crd = ApplyPromotionToInt(Mathf.Max(0, (def?.crd ?? 0) + var.crdDelta + bonus.crd), promo);

        float hit = ApplyPromotionToFloat(Mathf.Max(0f, (def != null ? def.hit : 0f) + var.hitDelta + bonus.hit), promo);
        float ac = ApplyPromotionToFloat(Mathf.Max(0f, (def != null ? def.ac : 0f) + var.acDelta + bonus.ac), promo);
        stats.hitDisplay = Mathf.RoundToInt(hit);
        stats.acDisplay = Mathf.RoundToInt(ac);

        stats.burnRes = ApplyPromotionToInt(Mathf.Max(0, (def?.burnResist ?? 0) + var.burnResistDelta), promo) + bonus.burnRes;
        stats.bleedRes = ApplyPromotionToInt(Mathf.Max(0, (def?.bleedResist ?? 0) + var.bleedResistDelta), promo) + bonus.bleedRes;
        stats.stunRes = ApplyPromotionToInt(Mathf.Max(0, (def?.stunResist ?? 0) + var.stunResistDelta), promo) + bonus.stunRes;
        stats.frostRes = ApplyPromotionToInt(Mathf.Max(0, (def?.frostResist ?? 0) + var.frostResistDelta), promo) + bonus.frostRes;
        stats.blindRes = ApplyPromotionToInt(Mathf.Max(0, (def?.blindResist ?? 0) + var.blindResistDelta), promo) + bonus.blindRes;
        return stats;
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

    private void RefreshViewRoots()
    {
        bool hasUnit = boundUnit != null;
        bool main = hasUnit && viewMode == InfoViewMode.MainStats;
        bool resist = hasUnit && viewMode == InfoViewMode.ResistStats;
        bool skill = hasUnit && selectedSkillIndex >= 0;

        if (mainStatsRoot != null)
            mainStatsRoot.SetActive(main);
        if (resistStatsRoot != null)
            resistStatsRoot.SetActive(resist);
        if (skillDescriptionRoot != null)
            skillDescriptionRoot.SetActive(skill);

        if (infoModeLabelText != null)
            infoModeLabelText.text = resist ? "저항" : "능력치";
    }

    private void RefreshSelectedFrames()
    {
        for (int i = 0; i < 4; i++)
        {
            bool selected = selectedSkillIndex == i;
            if (selectedFrameRoots != null && i < selectedFrameRoots.Length && selectedFrameRoots[i] != null)
                selectedFrameRoots[i].SetActive(selected);

            if (skillEntries != null && i < skillEntries.Length && skillEntries[i] != null)
                skillEntries[i].SetSelected(selected);
        }
    }

    private void RefreshSkillDescription(SkillDefinition skill)
    {
        if (selectedSkillIcon != null)
        {
            selectedSkillIcon.sprite = skill != null ? skill.icon : null;
            selectedSkillIcon.enabled = skill != null && skill.icon != null;
        }

        RefreshSkillClassBadge(skill);

        SetText(selectedSkillNameText, skill != null ? skill.skillName : string.Empty);
        SetText(selectedSkillDescriptionText, skill != null ? skill.description : string.Empty);
        SetText(selectedSkillPowerText, BattleSkillInfoFormatter.GetPowerValueText(skill));
        SetText(selectedSkillAccuracyText, BattleSkillInfoFormatter.GetSuccessValueText(skill));
        SetText(selectedSkillCooldownText, BattleSkillInfoFormatter.GetCooldownValueText(skill));
        SetText(selectedSkillEffectText, BattleSkillInfoFormatter.GetEffectValueText(skill));

        RefreshPositionHexes(skill);
    }

    private void RefreshSkillClassBadge(SkillDefinition skill)
    {
        SkillClass skillClass = BattleSkillInfoFormatter.GetSkillClass(skill);
        Sprite sprite = GetSkillClassBadgeSprite(skillClass);

        if (selectedSkillClassBadgeImage != null)
        {
            selectedSkillClassBadgeImage.sprite = sprite;
            selectedSkillClassBadgeImage.enabled = sprite != null;
        }

        SetText(selectedSkillClassBadgeText, BattleSkillInfoFormatter.GetSkillClassLabel(skillClass));
    }

    private Sprite GetSkillClassBadgeSprite(SkillClass skillClass)
    {
        switch (skillClass)
        {
            case SkillClass.Unique:
                return uniqueSkillClassBadgeSprite;
            case SkillClass.Common:
                return commonSkillClassBadgeSprite;
            case SkillClass.Mid:
                return midSkillClassBadgeSprite;
            case SkillClass.Ranged:
                return rangedSkillClassBadgeSprite;
            default:
                return meleeSkillClassBadgeSprite;
        }
    }

    private void RefreshPositionHexes(SkillDefinition skill)
    {
        for (int i = 0; i < 4; i++)
        {
            bool usable = skill != null && skill.CanBeUsedFromSlot(i);
            SetHex(usablePositionHexRoots, usablePositionHexImages, usablePositionHexTexts, i, usable ? usablePositionHexSprite : emptyPositionHexSprite, i + 1);

            bool target = skill != null && skill.CanTargetSlot(i);
            Sprite targetSprite = emptyPositionHexSprite;
            if (target)
                targetSprite = IsAllyTargetSkill(skill) ? targetAllyPositionHexSprite : targetEnemyPositionHexSprite;
            SetHex(targetPositionHexRoots, targetPositionHexImages, targetPositionHexTexts, i, targetSprite, i + 1);
        }
    }

    private static bool IsAllyTargetSkill(SkillDefinition skill)
    {
        if (skill == null)
            return false;

        return skill.targetTeam == SkillTargetTeam.Ally || skill.targetTeam == SkillTargetTeam.Self;
    }

    private static void SetHex(GameObject[] roots, Image[] images, TMP_Text[] texts, int index, Sprite sprite, int label)
    {
        if (roots != null && index >= 0 && index < roots.Length && roots[index] != null)
            roots[index].SetActive(true);

        if (images != null && index >= 0 && index < images.Length && images[index] != null)
        {
            images[index].sprite = sprite;
            images[index].enabled = sprite != null;
        }

        if (texts != null && index >= 0 && index < texts.Length && texts[index] != null)
            texts[index].text = label.ToString();
    }

    private void BindStatHoverTargets()
    {
        BindHover(dmgHover, LegionStatKind.Dmg, "DMG");
        BindHover(hitHover, LegionStatKind.Hit, "HIT");
        BindHover(acHover, LegionStatKind.Ac, "AC");
        BindHover(idtHover, LegionStatKind.Idt, "IDT");
        BindHover(criHover, LegionStatKind.Cri, "CRI");
        BindHover(crdHover, LegionStatKind.Crd, "CRD");
        BindHover(spdHover, LegionStatKind.Spd, "SPD");
        BindHover(stunHover, LegionStatKind.Stun, "기절 저항");
        BindHover(bleedHover, LegionStatKind.Bleed, "출혈 저항");
        BindHover(burnHover, LegionStatKind.Burn, "화상 저항");
        BindHover(frostHover, LegionStatKind.Frost, "동상 저항");
        BindHover(blindHover, LegionStatKind.Blind, "실명 저항");
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

        LegionStatBreakdown breakdown = BuildStatBreakdown(kind);
        statTooltipUI.Show(
            statLabel,
            FormatBreakdownValue(kind, breakdown.Total),
            FormatBreakdownValue(kind, breakdown.BaseValue),
            FormatSignedBreakdownValue(kind, breakdown.VarianceValue),
            FormatSignedBreakdownValue(kind, breakdown.EquipmentValue));
    }

    public void HideStatTooltip()
    {
        statTooltipUI?.Hide();
    }

    private LegionStatBreakdown BuildStatBreakdown(LegionStatKind kind)
    {
        UnitDefinition def = boundUnit != null ? boundUnit.unitDefinition : null;
        UnitInstanceStatVariance var = boundUnit != null ? boundUnit.statVariance ?? new UnitInstanceStatVariance() : new UnitInstanceStatVariance();
        LegionEquipmentBonusSummary bonus = profileController != null ? profileController.GetEquipmentBonusSummary(boundUnit) : default;

        LegionStatBreakdown result = new LegionStatBreakdown();

        switch (kind)
        {
            case LegionStatKind.Dmg:
                result.BaseValue = def != null ? def.dmg : 0;
                result.VarianceValue = var.dmgDelta + Mathf.Max(0, boundUnit != null ? boundUnit.levelGrowthDmg : 0);
                result.EquipmentValue = bonus.dmg;
                break;
            case LegionStatKind.Hit:
                result.BaseValue = Mathf.RoundToInt(def != null ? def.hit : 0f);
                result.VarianceValue = var.hitDelta;
                result.EquipmentValue = bonus.hit;
                break;
            case LegionStatKind.Ac:
                result.BaseValue = Mathf.RoundToInt(def != null ? def.ac : 0f);
                result.VarianceValue = var.acDelta;
                result.EquipmentValue = bonus.ac;
                break;
            case LegionStatKind.Idt:
                result.BaseValue = def != null ? def.idt : 0;
                result.VarianceValue = var.idtDelta;
                result.EquipmentValue = bonus.idt;
                break;
            case LegionStatKind.Cri:
                result.BaseValue = def != null ? def.cri : 0;
                result.VarianceValue = var.criDelta;
                result.EquipmentValue = bonus.cri;
                break;
            case LegionStatKind.Crd:
                result.BaseValue = def != null ? def.crd : 0;
                result.VarianceValue = var.crdDelta;
                result.EquipmentValue = bonus.crd;
                break;
            case LegionStatKind.Spd:
                result.BaseValue = def != null ? def.spd : 0;
                result.VarianceValue = var.spdDelta;
                result.EquipmentValue = bonus.spd;
                break;
            case LegionStatKind.Stun:
                result.BaseValue = def != null ? def.stunResist : 0;
                result.VarianceValue = var.stunResistDelta;
                result.EquipmentValue = bonus.stunRes;
                break;
            case LegionStatKind.Bleed:
                result.BaseValue = def != null ? def.bleedResist : 0;
                result.VarianceValue = var.bleedResistDelta;
                result.EquipmentValue = bonus.bleedRes;
                break;
            case LegionStatKind.Burn:
                result.BaseValue = def != null ? def.burnResist : 0;
                result.VarianceValue = var.burnResistDelta;
                result.EquipmentValue = bonus.burnRes;
                break;
            case LegionStatKind.Frost:
                result.BaseValue = def != null ? def.frostResist : 0;
                result.VarianceValue = var.frostResistDelta;
                result.EquipmentValue = bonus.frostRes;
                break;
            case LegionStatKind.Blind:
                result.BaseValue = def != null ? def.blindResist : 0;
                result.VarianceValue = var.blindResistDelta;
                result.EquipmentValue = bonus.blindRes;
                break;
        }

        result.Total = result.BaseValue + result.VarianceValue + result.EquipmentValue;
        return result;
    }

    private static string FormatBreakdownValue(LegionStatKind kind, int value)
    {
        return UsesPercentSuffix(kind) ? BattleStatFormatter.FormatPercent(value) : value.ToString();
    }

    private static string FormatSignedBreakdownValue(LegionStatKind kind, int value)
    {
        string prefix = value > 0 ? "+" : string.Empty;
        return UsesPercentSuffix(kind) ? $"{prefix}{value}%" : $"{prefix}{value}";
    }

    private static bool UsesPercentSuffix(LegionStatKind kind)
    {
        switch (kind)
        {
            case LegionStatKind.Idt:
            case LegionStatKind.Cri:
            case LegionStatKind.Stun:
            case LegionStatKind.Bleed:
            case LegionStatKind.Burn:
            case LegionStatKind.Frost:
            case LegionStatKind.Blind:
                return true;
            default:
                return false;
        }
    }

    private int GetMaxHp(PersistentRosterUnitData unit, out int baseHp, out int varianceHp, out int equipHp)
    {
        baseHp = unit != null && unit.unitDefinition != null ? unit.unitDefinition.maxHP : 1;
        varianceHp = unit != null && unit.statVariance != null ? unit.statVariance.maxHpDelta + Mathf.Max(0, unit.levelGrowthMaxHp) : Mathf.Max(0, unit != null ? unit.levelGrowthMaxHp : 0);
        LegionEquipmentBonusSummary bonus = profileController != null ? profileController.GetEquipmentBonusSummary(unit) : default;
        equipHp = bonus.maxHp;

        float promo = profileController != null
            ? LegionFormula.GetPromotionMultiplier(unit.promotionRank, profileController.PromotionBonusPercentPerRank)
            : 1f;

        return Mathf.Max(1, Mathf.RoundToInt((baseHp + varianceHp + equipHp) * promo));
    }

    private static int ApplyPromotionToInt(int value, float multiplier)
    {
        return Mathf.RoundToInt(value * Mathf.Max(0f, multiplier));
    }

    private static float ApplyPromotionToFloat(float value, float multiplier)
    {
        return value * Mathf.Max(0f, multiplier);
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

    private struct LegionStatBreakdown
    {
        public int BaseValue;
        public int VarianceValue;
        public int EquipmentValue;
        public int Total;
    }

    private struct LegionStatSnapshot
    {
        public int maxHp;
        public int dmg;
        public int hitDisplay;
        public int acDisplay;
        public int idt;
        public int cri;
        public int crd;
        public int spd;
        public int stunRes;
        public int bleedRes;
        public int burnRes;
        public int frostRes;
        public int blindRes;
    }
}
