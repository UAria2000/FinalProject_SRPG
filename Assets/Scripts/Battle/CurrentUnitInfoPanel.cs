using TMPro;
using UnityEngine;
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
    [SerializeField] private TMP_Text originalLevelValueText;
    [SerializeField] private TMP_Text hpValueText;
    [SerializeField] private Color allyNameColor = new Color(0.7647f, 0.2392f, 0.2902f, 1f); // #C33D4A

    [Header("Ally Rank Icon")]
    [SerializeField] private GameObject rankRoot;
    [SerializeField] private Image rankImage;
    [SerializeField] private Sprite[] rankSprites; // 1~9

    [Header("Info Area Roots")]
    [Tooltip("7개 기본 스탯을 담은 루트. 클릭 시 저항 스탯으로 전환할 버튼/영역은 infoAreaToggleButton에 연결합니다.")]
    [SerializeField] private GameObject mainStatsRoot;
    [Tooltip("5개 저항 스탯을 담은 루트.")]
    [SerializeField] private GameObject resistStatsRoot;
    [Tooltip("스킬 설명 전체 루트. 스킬 버튼을 누르면 기본/저항 스탯 영역 대신 이 루트가 켜집니다.")]
    [SerializeField] private GameObject skillDescriptionRoot;
    [SerializeField] private Button infoAreaToggleButton;
    [SerializeField] private Button skillDescriptionBackButton;
    [SerializeField] private TMP_Text infoModeLabelText;

    [Header("Main Stats - 7 Slots")]
    [SerializeField] private TMP_Text dmgValueText;
    [SerializeField] private TMP_Text spdValueText;
    [SerializeField] private TMP_Text hitValueText;
    [SerializeField] private TMP_Text acValueText;
    [SerializeField] private TMP_Text criValueText;
    [SerializeField] private TMP_Text crdValueText;
    [Tooltip("방어 스탯이 별도로 없으므로 현재는 0 또는 비워둘 수 있습니다. 향후 방어 스탯 추가 시 연결용입니다.")]
    [SerializeField] private TMP_Text defenseValueText;

    [Header("Resistance Stats - 5 Slots")]
    [SerializeField] private TMP_Text poisonResistValueText;
    [SerializeField] private TMP_Text bleedResistValueText;
    [SerializeField] private TMP_Text stunResistValueText;
    [SerializeField] private TMP_Text frostResistValueText;
    [SerializeField] private TMP_Text blindResistValueText;
    [Header("Legacy Optional Resistance Texts")]
    [SerializeField] private TMP_Text burnResistValueText;
    [SerializeField] private TMP_Text epitaphText;

    [Header("Skill Buttons - Ally Uses 4 Slots")]
    [SerializeField] private GameObject[] skillSlotRoots = new GameObject[4];
    [SerializeField] private Button[] skillButtons = new Button[4];
    [SerializeField] private Image[] skillIcons = new Image[4];
    [SerializeField] private TMP_Text[] skillNameTexts = new TMP_Text[4];
    [SerializeField] private Image[] cooldownOverlays = new Image[4];
    [SerializeField] private TMP_Text[] cooldownTexts = new TMP_Text[4];
    [SerializeField] private GameObject[] selectedSkillRoots = new GameObject[4];

    [Header("Skill Description")]
    [SerializeField] private Image selectedSkillIcon;
    [SerializeField] private TMP_Text selectedSkillNameText;
    [SerializeField] private TMP_Text selectedSkillDescriptionText;
    [SerializeField] private TMP_Text selectedSkillCooldownText;
    [SerializeField] private TMP_Text selectedSkillTargetText;
    [SerializeField] private TMP_Text selectedSkillTargetRanksText;
    [SerializeField] private TMP_Text selectedSkillUsableRanksText;

    [Header("Skill Target Type Icons")]
    [Tooltip("선택 사항. 0=단일, 1=전체, 2=자기, 3=아군/적군 요약 아이콘으로 사용합니다.")]
    [SerializeField] private GameObject[] targetKindRoots = new GameObject[4];
    [SerializeField] private Image[] targetKindImages = new Image[4];
    [SerializeField] private TMP_Text[] targetKindTexts = new TMP_Text[4];

    [Header("Skill Target Rank Icons")]
    [Tooltip("선택 사항. 대상 가능 열 1~4 아이콘입니다.")]
    [SerializeField] private Image[] targetRankImages = new Image[4];
    [SerializeField] private TMP_Text[] targetRankTexts = new TMP_Text[4];

    [Header("Skill Usable Rank Icons")]
    [Tooltip("선택 사항. 사용 가능 열 1~4 아이콘입니다. 텍스트 표시는 selectedSkillUsableRanksText가 담당합니다.")]
    [SerializeField] private Image[] usableRankImages = new Image[4];
    [SerializeField] private TMP_Text[] usableRankTexts = new TMP_Text[4];

    [Header("Icon State Colors")]
    [SerializeField] private Color enabledIconColor = Color.white;
    [SerializeField] private Color disabledIconColor = new Color(1f, 1f, 1f, 0.25f);

    private BattleUnit currentUnit;
    private InfoViewMode viewMode = InfoViewMode.MainStats;
    private InfoViewMode lastStatViewMode = InfoViewMode.MainStats;
    private int selectedSkillIndex = -1;
    private bool buttonsBound;

    private void Awake()
    {
        BindButtonsOnce();
    }

    public void Show(BattleUnit unit)
    {
        BindButtonsOnce();

        if (unit == null)
        {
            Hide();
            return;
        }

        if (currentUnit != unit)
        {
            currentUnit = unit;
            viewMode = InfoViewMode.MainStats;
            lastStatViewMode = InfoViewMode.MainStats;
            selectedSkillIndex = -1;
        }

        if (root != null)
            root.SetActive(true);

        RefreshIdentity(unit);
        RefreshStats(unit);
        RefreshSkillButtons(unit);
        RefreshViewRoots();
    }

    public void Hide()
    {
        currentUnit = null;
        selectedSkillIndex = -1;
        viewMode = InfoViewMode.MainStats;
        lastStatViewMode = InfoViewMode.MainStats;

        if (root != null)
            root.SetActive(false);
    }

    public void ToggleStatResistanceMode()
    {
        if (viewMode == InfoViewMode.MainStats)
        {
            viewMode = InfoViewMode.ResistStats;
            lastStatViewMode = viewMode;
        }
        else
        {
            viewMode = InfoViewMode.MainStats;
            lastStatViewMode = viewMode;
        }

        selectedSkillIndex = -1;
        RefreshViewRoots();
        RefreshSelectedSkillFrames();
    }

    public void ReturnToStatMode()
    {
        viewMode = lastStatViewMode == InfoViewMode.ResistStats ? InfoViewMode.ResistStats : InfoViewMode.MainStats;
        selectedSkillIndex = -1;
        RefreshViewRoots();
        RefreshSelectedSkillFrames();
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
            skillDescriptionBackButton.onClick.AddListener(ReturnToStatMode);
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
            ReturnToStatMode();
            return;
        }

        if (viewMode == InfoViewMode.MainStats || viewMode == InfoViewMode.ResistStats)
            lastStatViewMode = viewMode;

        selectedSkillIndex = slotIndex;
        viewMode = InfoViewMode.SkillDescription;
        RefreshSkillDescription(skill);
        RefreshViewRoots();
        RefreshSelectedSkillFrames();
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

        if (currentLevelValueText != null) currentLevelValueText.text = unit.CurrentLevel.ToString();
        if (originalLevelValueText != null) originalLevelValueText.text = unit.OriginalLevel.ToString();
        if (hpValueText != null) hpValueText.text = $"{unit.CurrentHP}/{unit.MaxHP}";

        RefreshRankIcon(unit);
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

    private void RefreshStats(BattleUnit unit)
    {
        UnitInstanceStatVariance variance = unit.GetVariance();

        if (dmgValueText != null) dmgValueText.text = BattleStatFormatter.FormatIntValueWithDelta(unit.DMG, variance.dmgDelta);
        if (defenseValueText != null) defenseValueText.text = "0";
        if (spdValueText != null) spdValueText.text = BattleStatFormatter.FormatIntValueWithDelta(unit.SPD, variance.spdDelta);
        if (hitValueText != null) hitValueText.text = BattleStatFormatter.FormatScaledX10ValueWithDelta(unit.HIT, variance.hitDeltaX10);
        if (acValueText != null) acValueText.text = BattleStatFormatter.FormatScaledX10ValueWithDelta(unit.AC, variance.acDeltaX10);
        if (criValueText != null) criValueText.text = BattleStatFormatter.FormatIntValueWithDelta(unit.CRI, variance.criDelta);
        if (crdValueText != null) crdValueText.text = BattleStatFormatter.FormatIntValueWithDelta(unit.CRD, variance.crdDelta);

        if (poisonResistValueText != null) poisonResistValueText.text = BattleStatFormatter.FormatPercent(unit.PoisonResist);
        if (burnResistValueText != null) burnResistValueText.text = BattleStatFormatter.FormatPercent(unit.BurnResist);
        if (bleedResistValueText != null) bleedResistValueText.text = BattleStatFormatter.FormatPercent(unit.BleedResist);
        if (stunResistValueText != null) stunResistValueText.text = BattleStatFormatter.FormatPercent(unit.StunResist);
        if (frostResistValueText != null) frostResistValueText.text = BattleStatFormatter.FormatPercent(unit.FrostResist);
        if (blindResistValueText != null) blindResistValueText.text = BattleStatFormatter.FormatPercent(unit.BlindResist);
        if (epitaphText != null) epitaphText.text = string.IsNullOrWhiteSpace(unit.Epitaph) ? "-" : unit.Epitaph;
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
                skillIcons[i].color = hasSkill ? Color.white : new Color(1f, 1f, 1f, 0.2f);
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

        RefreshSelectedSkillFrames();
    }

    private void RefreshViewRoots()
    {
        bool showMain = viewMode == InfoViewMode.MainStats;
        bool showResist = viewMode == InfoViewMode.ResistStats;
        bool showSkill = viewMode == InfoViewMode.SkillDescription;

        if (mainStatsRoot != null)
            mainStatsRoot.SetActive(showMain);
        if (resistStatsRoot != null)
            resistStatsRoot.SetActive(showResist);
        if (skillDescriptionRoot != null)
            skillDescriptionRoot.SetActive(showSkill);

        if (infoModeLabelText != null)
        {
            if (showMain) infoModeLabelText.text = "기본 능력치";
            else if (showResist) infoModeLabelText.text = "내성 정보";
            else infoModeLabelText.text = "스킬 정보";
        }

        if (showSkill && currentUnit != null && selectedSkillIndex >= 0)
            RefreshSkillDescription(currentUnit.GetActionSkillAt(selectedSkillIndex));
    }

    private void RefreshSelectedSkillFrames()
    {
        for (int i = 0; i < 4; i++)
            SetActiveInArray(selectedSkillRoots, i, viewMode == InfoViewMode.SkillDescription && selectedSkillIndex == i);
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

        if (selectedSkillNameText != null) selectedSkillNameText.text = skill.skillName;
        if (selectedSkillDescriptionText != null) selectedSkillDescriptionText.text = skill.description;
        if (selectedSkillCooldownText != null) selectedSkillCooldownText.text = skill.cooldownTurns > 0 ? $"쿨타임 {skill.cooldownTurns}턴" : "쿨타임 없음";
        if (selectedSkillTargetText != null) selectedSkillTargetText.text = GetTargetSummary(skill);
        if (selectedSkillTargetRanksText != null) selectedSkillTargetRanksText.text = $"대상 열: {FormatSlotRange(skill.targetMinSlotIndex, skill.targetMaxSlotIndex)}";
        if (selectedSkillUsableRanksText != null) selectedSkillUsableRanksText.text = $"사용 가능 열: {FormatSlotRange(skill.usableMinSlotIndex, skill.usableMaxSlotIndex)}";

        RefreshTargetKindIcons(skill);
        RefreshRankIcons(targetRankImages, targetRankTexts, skill.targetMinSlotIndex, skill.targetMaxSlotIndex);
        RefreshRankIcons(usableRankImages, usableRankTexts, skill.usableMinSlotIndex, skill.usableMaxSlotIndex);
    }

    private void RefreshTargetKindIcons(SkillDefinition skill)
    {
        bool isSelf = skill.targetTeam == SkillTargetTeam.Self;
        bool isAll = skill.targetScope == TargetScope.All;
        bool isSingle = !isSelf && skill.targetScope == TargetScope.Single;
        bool isTeam = skill.targetTeam == SkillTargetTeam.Ally || skill.targetTeam == SkillTargetTeam.Enemy;

        SetTargetKindState(0, isSingle, "단일");
        SetTargetKindState(1, isAll, "전체");
        SetTargetKindState(2, isSelf, "자기");
        SetTargetKindState(3, isTeam, skill.targetTeam == SkillTargetTeam.Ally ? "아군" : skill.targetTeam == SkillTargetTeam.Enemy ? "적군" : "자기");
    }

    private void SetTargetKindState(int index, bool enabled, string label)
    {
        if (targetKindRoots != null && index >= 0 && index < targetKindRoots.Length && targetKindRoots[index] != null)
            targetKindRoots[index].SetActive(true);

        if (targetKindImages != null && index >= 0 && index < targetKindImages.Length && targetKindImages[index] != null)
            targetKindImages[index].color = enabled ? enabledIconColor : disabledIconColor;

        if (targetKindTexts != null && index >= 0 && index < targetKindTexts.Length && targetKindTexts[index] != null)
        {
            targetKindTexts[index].text = label;
            targetKindTexts[index].color = enabled ? enabledIconColor : disabledIconColor;
        }
    }

    private void RefreshRankIcons(Image[] images, TMP_Text[] texts, int minSlot, int maxSlot)
    {
        for (int i = 0; i < 4; i++)
        {
            bool enabled = i >= minSlot && i <= maxSlot;
            if (images != null && i < images.Length && images[i] != null)
                images[i].color = enabled ? enabledIconColor : disabledIconColor;
            if (texts != null && i < texts.Length && texts[i] != null)
            {
                texts[i].text = $"{i + 1}열";
                texts[i].color = enabled ? enabledIconColor : disabledIconColor;
            }
        }
    }

    private static string FormatSlotRange(int minSlot, int maxSlot)
    {
        minSlot = Mathf.Clamp(minSlot, 0, 3);
        maxSlot = Mathf.Clamp(maxSlot, minSlot, 3);
        if (minSlot == maxSlot)
            return $"{minSlot + 1}열";
        return $"{minSlot + 1}~{maxSlot + 1}열";
    }

    private static string GetTargetSummary(SkillDefinition skill)
    {
        if (skill == null)
            return string.Empty;

        string team;
        switch (skill.targetTeam)
        {
            case SkillTargetTeam.Ally:
                team = "아군";
                break;
            case SkillTargetTeam.Self:
                team = "자기 자신";
                break;
            default:
                team = "적군";
                break;
        }

        string scope = skill.targetScope == TargetScope.All ? "전체" : "단일";
        return skill.targetTeam == SkillTargetTeam.Self ? team : $"{team} {scope}";
    }

    private static void SetActiveInArray(GameObject[] roots, int index, bool active)
    {
        if (roots != null && index >= 0 && index < roots.Length && roots[index] != null)
            roots[index].SetActive(active);
    }
}
