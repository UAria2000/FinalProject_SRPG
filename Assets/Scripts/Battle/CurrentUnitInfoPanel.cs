using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class CurrentUnitInfoPanel : MonoBehaviour
{
    private enum InfoViewMode
    {
        MainStats,
        ResistStats,
        SkillDescription
    }

    [Header("Root")]
    [SerializeField] private GameObject root;
    [SerializeField] private Image portraitImage;

    [Header("Identity")]
    [SerializeField] private TMP_Text nameValueText;
    [SerializeField] private TMP_Text currentLevelValueText;
    [Tooltip("오리지널 레벨 라벨/괄호까지 함께 숨기고 싶으면 이 루트를 연결합니다. 비워두면 originalLevelValueText만 숨깁니다.")]
    [SerializeField] private GameObject originalLevelRoot;
    [SerializeField] private TMP_Text originalLevelValueText;
    [SerializeField] private TMP_Text hpValueText;
    [SerializeField] private Image hpFillImage;
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Color allyNameColor = new Color(0.7647f, 0.2392f, 0.2902f, 1f); // #C33D4A

    [Header("Badges")]
    [SerializeField] private GameObject nftBadgeRoot;
    [SerializeField] private GameObject exchangeableBadge;
    [SerializeField] private GameObject meleeIcon;
    [SerializeField] private GameObject midIcon;
    [SerializeField] private GameObject rangedIcon;

    [Header("Ally Rank Icon")]
    [SerializeField] private GameObject rankRoot;
    [SerializeField] private Image rankImage;
    [SerializeField] private Sprite[] rankSprites; // 1~9

    [Header("Equipment Display - Read Only")]
    [SerializeField] private GameObject[] equipmentSlotRoots = new GameObject[2];
    [SerializeField] private Image[] equipmentIconImages = new Image[2];

    [Header("Info Area Roots")]
    [Tooltip("7개 기본 스탯을 담은 루트. 기본/저항 상태에서만 클릭 전환이 동작합니다.")]
    [SerializeField] private GameObject mainStatsRoot;
    [Tooltip("5개 저항 스탯을 담은 루트.")]
    [SerializeField] private GameObject resistStatsRoot;
    [Tooltip("스킬 설명 전체 루트. 스킬 버튼을 누르면 기본/저항 스탯 영역 대신 이 루트가 켜집니다.")]
    [SerializeField] private GameObject skillDescriptionRoot;
    [SerializeField] private Button infoAreaToggleButton;
    [SerializeField] private Button skillDescriptionBackButton;
    [SerializeField] private TMP_Text infoModeLabelText;

    [Header("Main Stats - DMG / HIT / AC / IDT / CRI / CRD / SPD")]
    [SerializeField] private TMP_Text dmgValueText;
    [SerializeField] private TMP_Text hitValueText;
    [SerializeField] private TMP_Text acValueText;
    [FormerlySerializedAs("defenseValueText")]
    [SerializeField] private TMP_Text idtValueText;
    [SerializeField] private TMP_Text criValueText;
    [SerializeField] private TMP_Text crdValueText;
    [SerializeField] private TMP_Text spdValueText;

    [Header("Resistance Stats - Stun / Bleed / Burn / Frost / Blind")]
    [SerializeField] private TMP_Text stunResistValueText;
    [SerializeField] private TMP_Text bleedResistValueText;
    [FormerlySerializedAs("poisonResistValueText")]
    [SerializeField] private TMP_Text burnResistValueText;
    [SerializeField] private TMP_Text frostResistValueText;
    [SerializeField] private TMP_Text blindResistValueText;

    [Header("Skill Buttons - Ally Uses 4 Slots")]
    [SerializeField] private GameObject[] skillSlotRoots = new GameObject[4];
    [SerializeField] private Button[] skillButtons = new Button[4];
    [SerializeField] private Image[] skillIcons = new Image[4];
    [SerializeField] private TMP_Text[] skillNameTexts = new TMP_Text[4];
    [SerializeField] private Image[] cooldownOverlays = new Image[4];
    [SerializeField] private TMP_Text[] cooldownTexts = new TMP_Text[4];
    [Tooltip("각 스킬 버튼 안의 SelectedFrame 오브젝트를 순서대로 연결합니다.")]
    [FormerlySerializedAs("selectedSkillRoots")]
    [SerializeField] private GameObject[] selectedFrameRoots = new GameObject[4];

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
    [Tooltip("스킬 사용 가능 위치 1~4. 회색 육각형 이미지를 연결합니다.")]
    [SerializeField] private GameObject[] usablePositionHexRoots = new GameObject[4];
    [SerializeField] private Image[] usablePositionHexImages = new Image[4];
    [SerializeField] private TMP_Text[] usablePositionHexTexts = new TMP_Text[4];
    [Tooltip("스킬 대상 위치 1~4. 적 대상은 붉은색, 아군/자신 대상은 노란색으로 표시합니다.")]
    [SerializeField] private GameObject[] targetPositionHexRoots = new GameObject[4];
    [SerializeField] private Image[] targetPositionHexImages = new Image[4];
    [SerializeField] private TMP_Text[] targetPositionHexTexts = new TMP_Text[4];
    [Header("Skill Description - Position Hex Sprites")]
    [Tooltip("스킬 사용 가능 위치에 표시할 회색 육각형 이미지입니다.")]
    [SerializeField] private Sprite usablePositionHexSprite;
    [Tooltip("적 대상 가능 위치에 표시할 붉은색 육각형 이미지입니다.")]
    [SerializeField] private Sprite targetEnemyPositionHexSprite;
    [Tooltip("아군/자신 대상 가능 위치에 표시할 노란색 육각형 이미지입니다.")]
    [SerializeField] private Sprite targetAllyPositionHexSprite;
    [Tooltip("불가능/비활성 위치에 표시할 빈칸 이미지입니다.")]
    [SerializeField] private Sprite emptyPositionHexSprite;

    [Header("Skill Description - Texts")]
    [SerializeField] private TMP_Text selectedSkillDescriptionText;
    [SerializeField] private TMP_Text selectedSkillPowerText;
    [SerializeField] private TMP_Text selectedSkillAccuracyText;
    [SerializeField] private TMP_Text selectedSkillCooldownText;
    [SerializeField] private TMP_Text selectedSkillEffectText;

    private BattleUnit currentUnit;
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
    }

    public void Show(BattleUnit unit)
    {
        BindButtonsOnce();

        if (unit == null)
        {
            Hide();
            return;
        }

        bool wasClosed = root != null && !root.activeSelf;
        if (currentUnit != unit || wasClosed)
            ResetToInitialView();

        currentUnit = unit;

        if (root != null)
            root.SetActive(true);

        RefreshIdentity(unit);
        RefreshBadges(unit);
        RefreshClassBadge(unit);
        RefreshRankIcon(unit);
        RefreshEquipmentSlots(unit);
        RefreshStats(unit);
        RefreshSkillButtons(unit);
        RefreshViewRoots();
        RefreshSelectedFrames();
    }

    public void Hide()
    {
        currentUnit = null;
        ResetToInitialView();

        if (root != null)
            root.SetActive(false);
    }

    public void ToggleStatResistanceMode()
    {
        if (viewMode == InfoViewMode.SkillDescription)
            return;

        viewMode = viewMode == InfoViewMode.MainStats ? InfoViewMode.ResistStats : InfoViewMode.MainStats;
        selectedSkillIndex = -1;
        RefreshViewRoots();
        RefreshSelectedFrames();
    }

    public void ReturnToMainStats()
    {
        ResetToInitialView();
        RefreshViewRoots();
        RefreshSelectedFrames();
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
            skillDescriptionBackButton.onClick.AddListener(ReturnToMainStats);
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
        if (currentUnit == null)
            return;

        SkillDefinition skill = currentUnit.GetActionSkillAt(slotIndex);
        if (skill == null)
            return;

        if (viewMode == InfoViewMode.SkillDescription && selectedSkillIndex == slotIndex)
        {
            ReturnToMainStats();
            return;
        }

        selectedSkillIndex = slotIndex;
        viewMode = InfoViewMode.SkillDescription;
        RefreshSkillDescription(skill);
        RefreshViewRoots();
        RefreshSelectedFrames();
    }

    private void RefreshIdentity(BattleUnit unit)
    {
        if (portraitImage != null)
        {
            portraitImage.sprite = unit.BustPortraitSprite;
            portraitImage.color = unit.BustPortraitSprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        }

        if (nameValueText != null)
        {
            nameValueText.text = unit.Name;
            nameValueText.color = allyNameColor;
        }

        if (currentLevelValueText != null)
            currentLevelValueText.text = unit.CurrentLevel.ToString();

        RefreshOriginalLevel(unit);

        if (hpValueText != null)
            hpValueText.text = $"{unit.CurrentHP}/{unit.MaxHP}";

        RefreshHpBar(unit);
    }

    private void RefreshOriginalLevel(BattleUnit unit)
    {
        bool showOriginalLevel = unit != null && unit.CurrentLevel < unit.OriginalLevel;

        if (originalLevelValueText != null)
        {
            originalLevelValueText.text = unit != null ? $"({unit.OriginalLevel})" : string.Empty;
            if (originalLevelRoot == null)
                originalLevelValueText.gameObject.SetActive(showOriginalLevel);
        }

        if (originalLevelRoot != null)
            originalLevelRoot.SetActive(showOriginalLevel);
    }

    private void RefreshHpBar(BattleUnit unit)
    {
        float hp01 = unit != null && unit.MaxHP > 0 ? Mathf.Clamp01(unit.CurrentHP / (float)unit.MaxHP) : 0f;

        if (hpFillImage != null)
            hpFillImage.fillAmount = hp01;

        if (hpSlider != null)
        {
            hpSlider.minValue = 0f;
            hpSlider.maxValue = 1f;
            hpSlider.value = hp01;
        }
    }

    private void RefreshBadges(BattleUnit unit)
    {
        bool showNft = unit != null && unit.IsNftUnit;
        if (nftBadgeRoot != null)
            nftBadgeRoot.SetActive(showNft);
        if (exchangeableBadge != null)
            exchangeableBadge.SetActive(showNft);
    }

    private void RefreshClassBadge(BattleUnit unit)
    {
        CharacterRangeType rangeType = unit != null ? unit.RangeType : CharacterRangeType.Melee;
        if (meleeIcon != null) meleeIcon.SetActive(rangeType == CharacterRangeType.Melee);
        if (midIcon != null) midIcon.SetActive(rangeType == CharacterRangeType.Mid);
        if (rangedIcon != null) rangedIcon.SetActive(rangeType == CharacterRangeType.Ranged);
    }

    private void RefreshRankIcon(BattleUnit unit)
    {
        if (rankRoot == null && rankImage == null)
            return;

        int rank = LegionFormula.ClampLegionRank(unit != null ? unit.PromotionRank : 0);
        Sprite sprite = null;
        if (rankSprites != null && rank > 0 && rank <= rankSprites.Length)
            sprite = rankSprites[rank - 1];

        bool show = sprite != null;
        if (rankRoot != null)
            rankRoot.SetActive(show);
        if (rankImage != null)
        {
            rankImage.sprite = sprite;
            rankImage.color = show ? Color.white : new Color(1f, 1f, 1f, 0f);
        }
    }

    private void RefreshEquipmentSlots(BattleUnit unit)
    {
        for (int i = 0; i < 2; i++)
        {
            ItemDefinition item = unit != null ? unit.GetEquippedItemAt(i) : null;
            if (equipmentSlotRoots != null && i < equipmentSlotRoots.Length && equipmentSlotRoots[i] != null)
                equipmentSlotRoots[i].SetActive(true);

            if (equipmentIconImages != null && i < equipmentIconImages.Length && equipmentIconImages[i] != null)
            {
                equipmentIconImages[i].sprite = item != null ? item.icon : null;
                equipmentIconImages[i].color = item != null && item.icon != null ? Color.white : new Color(1f, 1f, 1f, 0f);
            }
        }
    }

    private void RefreshStats(BattleUnit unit)
    {
        if (dmgValueText != null) dmgValueText.text = unit.DMG.ToString();
        if (hitValueText != null) hitValueText.text = Mathf.RoundToInt(unit.HIT).ToString();
        if (acValueText != null) acValueText.text = Mathf.RoundToInt(unit.AC).ToString();
        if (idtValueText != null) idtValueText.text = BattleStatFormatter.FormatPercent(unit.IDT);
        if (criValueText != null) criValueText.text = BattleStatFormatter.FormatPercent(unit.CRI);
        if (crdValueText != null) crdValueText.text = unit.CRD.ToString();
        if (spdValueText != null) spdValueText.text = unit.SPD.ToString();

        if (stunResistValueText != null) stunResistValueText.text = BattleStatFormatter.FormatPercent(unit.StunResist);
        if (bleedResistValueText != null) bleedResistValueText.text = BattleStatFormatter.FormatPercent(unit.BleedResist);
        if (burnResistValueText != null) burnResistValueText.text = BattleStatFormatter.FormatPercent(unit.BurnResist);
        if (frostResistValueText != null) frostResistValueText.text = BattleStatFormatter.FormatPercent(unit.FrostResist);
        if (blindResistValueText != null) blindResistValueText.text = BattleStatFormatter.FormatPercent(unit.BlindResist);
    }

    private void RefreshSkillButtons(BattleUnit unit)
    {
        for (int i = 0; i < 4; i++)
        {
            SkillDefinition skill = unit != null ? unit.GetActionSkillAt(i) : null;
            bool hasSkill = skill != null;

            SetActiveInArray(skillSlotRoots, i, true);

            if (skillIcons != null && i < skillIcons.Length && skillIcons[i] != null)
            {
                skillIcons[i].sprite = hasSkill ? skill.icon : null;
                skillIcons[i].color = hasSkill && skill.icon != null ? Color.white : new Color(1f, 1f, 1f, 0.2f);
            }

            if (skillNameTexts != null && i < skillNameTexts.Length && skillNameTexts[i] != null)
                skillNameTexts[i].text = hasSkill ? skill.skillName : string.Empty;

            int remaining = hasSkill ? unit.GetRemainingCooldown(skill) : 0;
            if (cooldownOverlays != null && i < cooldownOverlays.Length && cooldownOverlays[i] != null)
            {
                cooldownOverlays[i].gameObject.SetActive(hasSkill && remaining > 0);
                cooldownOverlays[i].fillAmount = hasSkill && remaining > 0
                    ? Mathf.Clamp01(remaining / Mathf.Max(1f, skill.cooldownTurns))
                    : 0f;
            }

            if (cooldownTexts != null && i < cooldownTexts.Length && cooldownTexts[i] != null)
                cooldownTexts[i].text = hasSkill && remaining > 0 ? remaining.ToString() : string.Empty;

            Button button = GetSkillButton(i);
            if (button != null)
                button.interactable = hasSkill;
        }
    }

    private void RefreshViewRoots()
    {
        bool showMain = viewMode == InfoViewMode.MainStats;
        bool showResist = viewMode == InfoViewMode.ResistStats;
        bool showSkill = viewMode == InfoViewMode.SkillDescription;

        if (mainStatsRoot != null) mainStatsRoot.SetActive(showMain);
        if (resistStatsRoot != null) resistStatsRoot.SetActive(showResist);
        if (skillDescriptionRoot != null) skillDescriptionRoot.SetActive(showSkill);

        if (infoModeLabelText != null)
        {
            if (showMain) infoModeLabelText.text = "기본 능력치";
            else if (showResist) infoModeLabelText.text = "내성 정보";
            else infoModeLabelText.text = "스킬 정보";
        }

        if (showSkill && currentUnit != null && selectedSkillIndex >= 0)
            RefreshSkillDescription(currentUnit.GetActionSkillAt(selectedSkillIndex));
    }

    private void RefreshSelectedFrames()
    {
        for (int i = 0; i < 4; i++)
            SetActiveInArray(selectedFrameRoots, i, viewMode == InfoViewMode.SkillDescription && selectedSkillIndex == i);
    }

    private void RefreshSkillDescription(SkillDefinition skill)
    {
        if (skill == null)
            return;

        if (selectedSkillIcon != null)
        {
            selectedSkillIcon.sprite = skill.icon;
            selectedSkillIcon.color = skill.icon != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        }

        RefreshSelectedSkillClassBadge(skill);

        if (selectedSkillNameText != null)
            selectedSkillNameText.text = skill.skillName;

        if (selectedSkillDescriptionText != null)
            selectedSkillDescriptionText.text = skill.description;

        if (selectedSkillPowerText != null)
            selectedSkillPowerText.text = BattleSkillInfoFormatter.GetPowerValueText(skill);

        if (selectedSkillAccuracyText != null)
            selectedSkillAccuracyText.text = BattleSkillInfoFormatter.GetSuccessValueText(skill);

        if (selectedSkillCooldownText != null)
            selectedSkillCooldownText.text = BattleSkillInfoFormatter.GetCooldownValueText(skill);

        if (selectedSkillEffectText != null)
            selectedSkillEffectText.text = BattleSkillInfoFormatter.GetEffectValueText(skill);

        RefreshPositionHexes(skill);
    }

    private void RefreshSelectedSkillClassBadge(SkillDefinition skill)
    {
        SkillClass skillClass = BattleSkillInfoFormatter.GetSkillClass(skill);
        Sprite badgeSprite = GetSkillClassBadgeSprite(skillClass);

        if (selectedSkillClassBadgeImage != null)
        {
            selectedSkillClassBadgeImage.sprite = badgeSprite;
            selectedSkillClassBadgeImage.enabled = badgeSprite != null;
            selectedSkillClassBadgeImage.gameObject.SetActive(badgeSprite != null);
        }

        if (selectedSkillClassBadgeText != null)
            selectedSkillClassBadgeText.text = BattleSkillInfoFormatter.GetSkillClassLabel(skillClass);
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
        if (skill == null)
        {
            RefreshPositionHexGroup(usablePositionHexRoots, usablePositionHexImages, usablePositionHexTexts,
                -1, -1, usablePositionHexSprite);
            RefreshPositionHexGroup(targetPositionHexRoots, targetPositionHexImages, targetPositionHexTexts,
                -1, -1, targetEnemyPositionHexSprite);
            return;
        }

        RefreshPositionHexGroup(usablePositionHexRoots, usablePositionHexImages, usablePositionHexTexts,
            skill.usableMinSlotIndex, skill.usableMaxSlotIndex, usablePositionHexSprite);

        Sprite targetSprite = GetTargetPositionHexSprite(skill);
        RefreshPositionHexGroup(targetPositionHexRoots, targetPositionHexImages, targetPositionHexTexts,
            skill.targetMinSlotIndex, skill.targetMaxSlotIndex, targetSprite);
    }

    private Sprite GetTargetPositionHexSprite(SkillDefinition skill)
    {
        if (skill != null && (skill.targetTeam == SkillTargetTeam.Ally || skill.targetTeam == SkillTargetTeam.Self))
            return targetAllyPositionHexSprite;

        return targetEnemyPositionHexSprite;
    }

    private void RefreshPositionHexGroup(GameObject[] roots, Image[] images, TMP_Text[] texts, int minSlot, int maxSlot, Sprite enabledSprite)
    {
        bool hasValidRange = minSlot >= 0 && maxSlot >= minSlot;
        minSlot = Mathf.Clamp(minSlot, 0, 3);
        maxSlot = Mathf.Clamp(maxSlot, 0, 3);

        for (int i = 0; i < 4; i++)
        {
            bool enabled = hasValidRange && i >= minSlot && i <= maxSlot;

            SetActiveInArray(roots, i, true);

            if (images != null && i < images.Length && images[i] != null)
            {
                Sprite sprite = enabled ? enabledSprite : emptyPositionHexSprite;
                images[i].sprite = sprite;
                images[i].color = Color.white;
                images[i].enabled = sprite != null;
            }

            if (texts != null && i < texts.Length && texts[i] != null)
            {
                texts[i].text = enabled ? (i + 1).ToString() : string.Empty;
                texts[i].color = Color.white;
            }
        }
    }

    private static void SetActiveInArray(GameObject[] roots, int index, bool active)
    {
        if (roots != null && index >= 0 && index < roots.Length && roots[index] != null)
            roots[index].SetActive(active);
    }
}
