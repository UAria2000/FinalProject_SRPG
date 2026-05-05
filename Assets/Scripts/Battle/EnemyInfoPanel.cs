using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class EnemyInfoPanel : MonoBehaviour
{
    private enum InfoViewMode
    {
        MainStats,
        ResistStats,
        SkillDescription,
        LastWill
    }

    [Header("Root")]
    [SerializeField] private GameObject root;
    [SerializeField] private Image portraitImage;

    [Header("Identity")]
    [SerializeField] private TMP_Text nameValueText;
    [SerializeField] private TMP_Text levelValueText;
    [SerializeField] private TMP_Text hpValueText;
    [SerializeField] private Color enemyNameColor = new Color(0.2941f, 0.6353f, 0.9569f, 1f); // #4BA2F4

    [Header("Info Area Roots")]
    [Tooltip("7개 기본 스탯을 담은 루트. 클릭 시 저항 스탯으로 전환할 버튼/영역은 infoAreaToggleButton에 연결합니다.")]
    [SerializeField] private GameObject mainStatsRoot;
    [Tooltip("5개 저항 스탯을 담은 루트.")]
    [SerializeField] private GameObject resistStatsRoot;
    [Tooltip("스킬 설명 전체 루트. 스킬 버튼을 누르면 기본/저항 스탯 영역 대신 이 루트가 켜집니다.")]
    [SerializeField] private GameObject skillDescriptionRoot;
    [Tooltip("유언장 설명 루트. 유언장 버튼을 누르면 기본/저항/스킬 영역 대신 이 루트가 켜집니다.")]
    [SerializeField] private GameObject lastWillDescriptionRoot;
    [SerializeField] private Button infoAreaToggleButton;
    [SerializeField] private Button descriptionBackButton;
    [SerializeField] private TMP_Text infoModeLabelText;

    [Header("Main Stats - 7 Slots")]
    [SerializeField] private TMP_Text dmgValueText;
    [Tooltip("방어 스탯이 별도로 없으므로 현재는 0 또는 비워둘 수 있습니다. 향후 방어 스탯 추가 시 연결용입니다.")]
    [SerializeField] private TMP_Text defenseValueText;
    [SerializeField] private TMP_Text spdValueText;
    [SerializeField] private TMP_Text hitValueText;
    [SerializeField] private TMP_Text acValueText;
    [SerializeField] private TMP_Text criValueText;
    [SerializeField] private TMP_Text crdValueText;

    [Header("Resistance Stats - 5 Slots")]
    [SerializeField] private TMP_Text poisonResistValueText;
    [SerializeField] private TMP_Text bleedResistValueText;
    [SerializeField] private TMP_Text stunResistValueText;
    [SerializeField] private TMP_Text frostResistValueText;
    [SerializeField] private TMP_Text blindResistValueText;
    [Header("Legacy Optional Resistance Texts")]
    [SerializeField] private TMP_Text burnResistValueText;
    [SerializeField] private TMP_Text epitaphText;

    [Header("Enemy Skill Preview - Enemy Uses 3 Skill Slots")]
    [SerializeField] private GameObject[] skillSlotRoots = new GameObject[4];
    [SerializeField] private Button[] skillButtons = new Button[4];
    [SerializeField] private Image[] skillIcons = new Image[4];
    [SerializeField] private TMP_Text[] skillNameTexts = new TMP_Text[4];
    [SerializeField] private Image[] cooldownOverlays = new Image[4];
    [SerializeField] private TMP_Text[] cooldownTexts = new TMP_Text[4];
    [SerializeField] private GameObject[] selectedSkillRoots = new GameObject[4];

    [Header("Last Will Slot")]
    [SerializeField] private Button lastWillButton;
    [SerializeField] private GameObject lastWillSlotRoot;
    [SerializeField] private Image lastWillIconImage;
    [SerializeField] private TMP_Text lastWillButtonLabelText;
    [SerializeField] private GameObject lastWillSelectedRoot;
    [Range(0f, 100f)] [SerializeField] private float lastWillButtonChancePercent = 30f;
    [SerializeField] private BattleLastWillTextTable lastWillTextTable;
    [TextArea(2, 6)] [SerializeField] private string[] fallbackLastWillTexts;
    [Tooltip("켜면 기존처럼 유언장 버튼이 EnemyDetailPopup을 열도록 외부 액션을 호출합니다. 기본값은 꺼짐이며, 새 패널 내부에 유언장을 표시합니다.")]
    [SerializeField] private bool lastWillButtonOpensLegacyDetailPopup = false;

    [Header("Skill Description")]
    [SerializeField] private Image selectedSkillIcon;
    [SerializeField] private TMP_Text selectedSkillNameText;
    [SerializeField] private TMP_Text selectedSkillDescriptionText;
    [SerializeField] private TMP_Text selectedSkillCooldownText;
    [SerializeField] private TMP_Text selectedSkillTargetText;
    [SerializeField] private TMP_Text selectedSkillTargetRanksText;
    [SerializeField] private TMP_Text selectedSkillUsableRanksText;

    [Header("Last Will Description")]
    [SerializeField] private TMP_Text lastWillTitleText;
    [SerializeField] private TMP_Text lastWillBodyText;

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

    private BattleUnit currentEnemy;
    private InfoViewMode viewMode = InfoViewMode.MainStats;
    private InfoViewMode lastStatViewMode = InfoViewMode.MainStats;
    private int selectedSkillIndex = -1;
    private bool buttonsBound;
    private UnityAction legacyLastWillAction;

    public BattleUnit CurrentEnemy => currentEnemy;

    private void Awake()
    {
        BindButtonsOnce();
    }

    public void SetLastWillButtonAction(UnityAction action)
    {
        legacyLastWillAction = action;
        BindButtonsOnce(true);
    }

    public void Show(BattleUnit enemy)
    {
        BindButtonsOnce();

        if (enemy == null)
        {
            Hide();
            return;
        }

        if (currentEnemy != enemy)
        {
            currentEnemy = enemy;
            viewMode = InfoViewMode.MainStats;
            lastStatViewMode = InfoViewMode.MainStats;
            selectedSkillIndex = -1;
        }

        if (root != null)
            root.SetActive(true);

        RefreshIdentity(enemy);
        RefreshStats(enemy);
        RefreshSkillButtons(enemy);
        RefreshLastWillSlot(enemy);
        RefreshViewRoots();
    }

    public void Refresh()
    {
        Show(currentEnemy);
    }

    public void Hide()
    {
        currentEnemy = null;
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
        RefreshSelectedFrames();
    }

    public void ReturnToStatMode()
    {
        viewMode = lastStatViewMode == InfoViewMode.ResistStats ? InfoViewMode.ResistStats : InfoViewMode.MainStats;
        selectedSkillIndex = -1;
        RefreshViewRoots();
        RefreshSelectedFrames();
    }

    private void BindButtonsOnce(bool force = false)
    {
        if (buttonsBound && !force)
            return;

        buttonsBound = true;

        if (infoAreaToggleButton != null)
        {
            infoAreaToggleButton.onClick.RemoveAllListeners();
            infoAreaToggleButton.onClick.AddListener(ToggleStatResistanceMode);
        }

        if (descriptionBackButton != null)
        {
            descriptionBackButton.onClick.RemoveAllListeners();
            descriptionBackButton.onClick.AddListener(ReturnToStatMode);
        }

        for (int i = 0; i < 3; i++)
        {
            int slot = i;
            Button button = GetSkillButton(slot);
            if (button == null)
                continue;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(delegate { OnSkillButtonPressed(slot); });
        }

        if (lastWillButton == null)
            lastWillButton = GetSkillButton(3);

        if (lastWillButton != null)
        {
            lastWillButton.onClick.RemoveAllListeners();
            lastWillButton.onClick.AddListener(HandleLastWillButtonPressed);
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
        if (currentEnemy == null)
            return;

        SkillDefinition skill = currentEnemy.GetActionSkillAt(slotIndex);
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
        RefreshSelectedFrames();
    }

    private void HandleLastWillButtonPressed()
    {
        if (lastWillButtonOpensLegacyDetailPopup)
        {
            legacyLastWillAction?.Invoke();
            return;
        }

        if (currentEnemy == null || !currentEnemy.HasBattleInfoLastWill)
            return;

        if (viewMode == InfoViewMode.LastWill)
        {
            ReturnToStatMode();
            return;
        }

        if (viewMode == InfoViewMode.MainStats || viewMode == InfoViewMode.ResistStats)
            lastStatViewMode = viewMode;

        selectedSkillIndex = -1;
        viewMode = InfoViewMode.LastWill;
        RefreshLastWillDescription(currentEnemy);
        RefreshViewRoots();
        RefreshSelectedFrames();
    }

    private void RefreshIdentity(BattleUnit enemy)
    {
        if (portraitImage != null)
        {
            portraitImage.sprite = enemy.BustPortraitSprite;
            portraitImage.color = enemy.BustPortraitSprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        }

        if (nameValueText != null)
        {
            nameValueText.text = enemy.Name;
            nameValueText.color = enemyNameColor;
        }

        if (levelValueText != null) levelValueText.text = enemy.CurrentLevel.ToString();
        if (hpValueText != null) hpValueText.text = $"{enemy.CurrentHP}/{enemy.MaxHP}";
    }

    private void RefreshStats(BattleUnit enemy)
    {
        if (dmgValueText != null) dmgValueText.text = enemy.DMG.ToString();
        if (defenseValueText != null) defenseValueText.text = "0";
        if (spdValueText != null) spdValueText.text = enemy.SPD.ToString();
        if (hitValueText != null) hitValueText.text = Mathf.RoundToInt(enemy.HIT * 10f).ToString();
        if (acValueText != null) acValueText.text = Mathf.RoundToInt(enemy.AC * 10f).ToString();
        if (criValueText != null) criValueText.text = enemy.CRI.ToString();
        if (crdValueText != null) crdValueText.text = enemy.CRD.ToString();

        if (poisonResistValueText != null) poisonResistValueText.text = BattleStatFormatter.FormatPercent(enemy.PoisonResist);
        if (burnResistValueText != null) burnResistValueText.text = BattleStatFormatter.FormatPercent(enemy.BurnResist);
        if (bleedResistValueText != null) bleedResistValueText.text = BattleStatFormatter.FormatPercent(enemy.BleedResist);
        if (stunResistValueText != null) stunResistValueText.text = BattleStatFormatter.FormatPercent(enemy.StunResist);
        if (frostResistValueText != null) frostResistValueText.text = BattleStatFormatter.FormatPercent(enemy.FrostResist);
        if (blindResistValueText != null) blindResistValueText.text = BattleStatFormatter.FormatPercent(enemy.BlindResist);
        if (epitaphText != null) epitaphText.text = enemy.HasBattleInfoLastWill ? enemy.BattleInfoLastWillText : enemy.Epitaph;
    }

    private void RefreshSkillButtons(BattleUnit enemy)
    {
        for (int i = 0; i < 3; i++)
        {
            SkillDefinition skill = enemy != null ? enemy.GetActionSkillAt(i) : null;
            bool hasSkill = skill != null;

            SetActiveInArray(skillSlotRoots, i, true);

            if (skillIcons != null && i < skillIcons.Length && skillIcons[i] != null)
            {
                skillIcons[i].sprite = hasSkill ? skill.icon : null;
                skillIcons[i].color = hasSkill ? Color.white : new Color(1f, 1f, 1f, 0.2f);
            }

            if (skillNameTexts != null && i < skillNameTexts.Length && skillNameTexts[i] != null)
                skillNameTexts[i].text = hasSkill ? skill.skillName : string.Empty;

            int remaining = hasSkill ? enemy.GetRemainingCooldown(skill) : 0;
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

        RefreshSelectedFrames();
    }

    private void RefreshLastWillSlot(BattleUnit enemy)
    {
        bool hasLastWill = enemy != null && enemy.EnsureBattleInfoLastWill(lastWillButtonChancePercent, lastWillTextTable, fallbackLastWillTexts);

        if (lastWillSlotRoot != null)
            lastWillSlotRoot.SetActive(hasLastWill);
        else
            SetActiveInArray(skillSlotRoots, 3, hasLastWill);

        if (lastWillButton != null)
            lastWillButton.interactable = hasLastWill;

        if (lastWillIconImage != null)
            lastWillIconImage.color = hasLastWill ? Color.white : new Color(1f, 1f, 1f, 0.2f);

        if (lastWillButtonLabelText != null)
            lastWillButtonLabelText.text = hasLastWill ? "유언장" : string.Empty;

        if (skillIcons != null && skillIcons.Length > 3 && skillIcons[3] != null)
        {
            if (!hasLastWill)
                skillIcons[3].sprite = null;
            skillIcons[3].color = hasLastWill ? Color.white : new Color(1f, 1f, 1f, 0.2f);
        }

        if (skillNameTexts != null && skillNameTexts.Length > 3 && skillNameTexts[3] != null)
            skillNameTexts[3].text = hasLastWill ? "유언장" : string.Empty;

        if (cooldownOverlays != null && cooldownOverlays.Length > 3 && cooldownOverlays[3] != null)
            cooldownOverlays[3].gameObject.SetActive(false);
        if (cooldownTexts != null && cooldownTexts.Length > 3 && cooldownTexts[3] != null)
            cooldownTexts[3].text = string.Empty;

        if (viewMode == InfoViewMode.LastWill && !hasLastWill)
            ReturnToStatMode();
    }

    private void RefreshViewRoots()
    {
        bool showMain = viewMode == InfoViewMode.MainStats;
        bool showResist = viewMode == InfoViewMode.ResistStats;
        bool showSkill = viewMode == InfoViewMode.SkillDescription;
        bool showLastWill = viewMode == InfoViewMode.LastWill;

        if (mainStatsRoot != null)
            mainStatsRoot.SetActive(showMain);
        if (resistStatsRoot != null)
            resistStatsRoot.SetActive(showResist);
        if (skillDescriptionRoot != null)
            skillDescriptionRoot.SetActive(showSkill);
        if (lastWillDescriptionRoot != null)
            lastWillDescriptionRoot.SetActive(showLastWill);

        if (infoModeLabelText != null)
        {
            if (showMain) infoModeLabelText.text = "기본 능력치";
            else if (showResist) infoModeLabelText.text = "내성 정보";
            else if (showSkill) infoModeLabelText.text = "스킬 정보";
            else infoModeLabelText.text = "유언장";
        }

        if (showSkill && currentEnemy != null && selectedSkillIndex >= 0)
            RefreshSkillDescription(currentEnemy.GetActionSkillAt(selectedSkillIndex));
        if (showLastWill && currentEnemy != null)
            RefreshLastWillDescription(currentEnemy);
    }

    private void RefreshSelectedFrames()
    {
        for (int i = 0; i < 4; i++)
            SetActiveInArray(selectedSkillRoots, i, viewMode == InfoViewMode.SkillDescription && selectedSkillIndex == i);

        if (lastWillSelectedRoot != null)
            lastWillSelectedRoot.SetActive(viewMode == InfoViewMode.LastWill);
        else
            SetActiveInArray(selectedSkillRoots, 3, viewMode == InfoViewMode.LastWill);
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

    private void RefreshLastWillDescription(BattleUnit enemy)
    {
        if (enemy == null)
            return;

        if (lastWillTitleText != null)
            lastWillTitleText.text = "유언장";

        if (lastWillBodyText != null)
            lastWillBodyText.text = enemy.HasBattleInfoLastWill ? enemy.BattleInfoLastWillText : "";
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
