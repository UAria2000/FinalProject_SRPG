using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum LegionSortKey
{
    None,
    Obtained,
    Name,
    Level,
    Rank,
    NFT,
}

public enum LegionSortDirection
{
    None,
    Descending,
    Ascending,
}

[System.Serializable]
public class LegionButtonVisualState
{
    public Button button;
    public Image image;
    public Sprite offSprite;
    public Sprite onSprite;
    public GameObject offRoot;
    public GameObject onRoot;

    public void Bind(UnityAction action)
    {
        if (button == null || action == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    public void SetOn(bool on)
    {
        if (image != null)
        {
            Sprite target = on ? onSprite : offSprite;
            if (target != null)
                image.sprite = target;
        }

        if (onRoot != null)
            onRoot.SetActive(on);
        if (offRoot != null)
            offRoot.SetActive(!on);
    }
}

public class LegionPanelUI : MainUIPanelBase, IDropHandler
{
    private const int UnitsPerPage = 10;

    [Header("References")]
    [SerializeField] private PersistentProfileController persistentProfileController;
    [SerializeField] private LegionDetailPanelUI detailPanelUI;
    [SerializeField] private LegionRenamePopupUI renamePopupUI;
    [SerializeField] private LegionDecomposeConfirmPopupUI decomposeConfirmPopupUI;
    [SerializeField] private WorldTopHudUI topHudUI;
    [SerializeField] private BottomPartySummaryPanelUI bottomPartySummaryPanelUI;

    [Header("Party Link")]
    [Tooltip("레기온 카드 클릭 시 하단 파티 슬롯에 배치할 후보로 선택한다. 드래그 배치는 이 값과 무관하게 동작한다.")]
    [SerializeField] private bool clickCardSelectsPartyCandidate = true;

    [Header("Grid")]
    [SerializeField] private RectTransform rosterGridRoot;
    [SerializeField] private LegionUnitCardUI unitCardPrefab;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_Text pageText;

    [Header("Bottom Actions")]
    [SerializeField] private TMP_Text decomposeCountText;
    [SerializeField] private TMP_Text decomposeShardPreviewText;
    [SerializeField] private TMP_Text decomposeSoulPreviewText;
    [SerializeField] private Button multiSelectButton;
    [SerializeField] private TMP_Text multiSelectButtonText;
    [SerializeField] private Button decomposeButton;

    [Header("Top Currency Override (Optional)")]
    [Tooltip("WorldTopHudUI를 쓰지 않고 레기온 패널 안에 직접 표시할 때 연결한다.")]
    [SerializeField] private TMP_Text topSoulText;
    [SerializeField] private TMP_Text topShardText;
    [SerializeField] private TMP_Text topSoulGainText;
    [SerializeField] private TMP_Text topShardGainText;
    [SerializeField] private CanvasGroup topSoulGainCanvasGroup;
    [SerializeField] private CanvasGroup topShardGainCanvasGroup;
    [SerializeField] private float topGainFadeDuration = 1.15f;
    [SerializeField] private Color gainTextColor = new Color(0.35f, 1f, 0.35f, 1f);

    [Header("Sort Button Visuals")]
    [SerializeField] private LegionButtonVisualState sortObtainedButton;
    [SerializeField] private LegionButtonVisualState sortNameButton;
    [SerializeField] private LegionButtonVisualState sortLevelButton;
    [SerializeField] private LegionButtonVisualState sortRankButton;
    [SerializeField] private LegionButtonVisualState sortNftButton;

    [Header("Filter Button Visuals")]
    [SerializeField] private LegionButtonVisualState filterNftButton;
    [SerializeField] private LegionButtonVisualState filterFavoriteButton;
    [SerializeField] private LegionButtonVisualState filterMeleeButton;
    [SerializeField] private LegionButtonVisualState filterMidButton;
    [SerializeField] private LegionButtonVisualState filterRangedButton;

    [Header("State")]
    [SerializeField] private LegionSortKey sortKey = LegionSortKey.None;
    [SerializeField] private LegionSortDirection sortDirection = LegionSortDirection.None;
    [SerializeField] private bool sortAscending; // legacy inspector compatibility
    [SerializeField] private bool filterExchangeableOnly; // legacy inspector field. NFT 필터와 동일하게 처리.
    [SerializeField] private bool filterNftOnly;
    [SerializeField] private bool filterFavoriteOnly;
    [SerializeField] private bool filterMeleeOnly;
    [SerializeField] private bool filterMidOnly;
    [SerializeField] private bool filterRangedOnly;
    [SerializeField] private CharacterRangeType? filterRange; // legacy compatibility only

    private readonly List<LegionUnitCardUI> runtimeCards = new();
    private readonly HashSet<string> decomposeSelectedIds = new();

    private int pageIndex;
    private PersistentRosterUnitData selectedUnit;
    private bool decomposeSelectionMode;
    private Coroutine localSoulGainFadeRoutine;
    private Coroutine localShardGainFadeRoutine;

    public PersistentProfileController ProfileController => persistentProfileController;
    public WorldRunManager RuntimeWorldRunManager => worldRunManager;
    public bool IsDecomposeSelectionMode => decomposeSelectionMode;

    protected override void Awake()
    {
        base.Awake();

        if (persistentProfileController == null)
            persistentProfileController = Object.FindFirstObjectByType<PersistentProfileController>();
        if (topHudUI == null)
            topHudUI = Object.FindFirstObjectByType<WorldTopHudUI>();
        if (bottomPartySummaryPanelUI == null)
            bottomPartySummaryPanelUI = Object.FindFirstObjectByType<BottomPartySummaryPanelUI>();

        EnsureRuntimeCards();
        BindButton(prevButton, PrevPage);
        BindButton(nextButton, NextPage);
        BindButton(multiSelectButton, ToggleMultiSelectMode);
        BindButton(decomposeButton, HandleDecomposeButtonClicked);

        sortObtainedButton?.Bind(ToggleSortObtained);
        sortNameButton?.Bind(ToggleSortName);
        sortLevelButton?.Bind(ToggleSortLevel);
        sortRankButton?.Bind(ToggleSortRank);
        sortNftButton?.Bind(ToggleSortNFT);

        filterNftButton?.Bind(ToggleFilterNft);
        filterFavoriteButton?.Bind(ToggleFilterFavorite);
        filterMeleeButton?.Bind(SetFilterMelee);
        filterMidButton?.Bind(SetFilterMid);
        filterRangedButton?.Bind(SetFilterRanged);
    }

    private void Start()
    {
        RefreshAll();
    }

    protected override void OnPanelOpened()
    {
        EnsureRuntimeCards();

        if (persistentProfileController != null)
            persistentProfileController.OnProfileChanged += RefreshAll;
        if (worldRunManager != null)
            worldRunManager.OnStorageChanged += RefreshAll;

        if (topHudUI != null)
            topHudUI.SetLegionShardVisible(true);
        if (bottomPartySummaryPanelUI != null)
            bottomPartySummaryPanelUI.SetBarracksMode(true);

        RefreshAll();
    }

    protected override void OnPanelClosed()
    {
        if (persistentProfileController != null)
            persistentProfileController.OnProfileChanged -= RefreshAll;
        if (worldRunManager != null)
            worldRunManager.OnStorageChanged -= RefreshAll;

        if (topHudUI != null)
            topHudUI.SetLegionShardVisible(false);
        if (bottomPartySummaryPanelUI != null)
            bottomPartySummaryPanelUI.SetBarracksMode(false);

        decomposeSelectionMode = false;
        decomposeSelectedIds.Clear();
        UIDragGhostUI.HideGhost();
    }

    public void RefreshAll()
    {
        EnsureRuntimeCards();
        NormalizeLegacyFilterState();
        SyncLegacySortState();

        List<PersistentRosterUnitData> filtered = BuildFilteredUnits();
        int totalPages = Mathf.Max(1, Mathf.CeilToInt(filtered.Count / (float)UnitsPerPage));
        pageIndex = Mathf.Clamp(pageIndex, 0, totalPages - 1);

        if (selectedUnit != null && persistentProfileController != null)
            selectedUnit = persistentProfileController.FindRosterUnit(selectedUnit.instanceId);

        if (selectedUnit == null && filtered.Count > 0)
            selectedUnit = filtered[0];
        else if (selectedUnit != null && !filtered.Any(u => u.instanceId == selectedUnit.instanceId))
            selectedUnit = filtered.Count > 0 ? filtered[0] : null;

        int start = pageIndex * UnitsPerPage;
        for (int i = 0; i < runtimeCards.Count; i++)
        {
            PersistentRosterUnitData unit = (start + i) < filtered.Count ? filtered[start + i] : null;
            bool inParty = unit != null && persistentProfileController != null && persistentProfileController.IsRosterUnitInParty(unit);
            bool isCurrent = unit != null && selectedUnit != null && unit.instanceId == selectedUnit.instanceId;
            bool isSelectedForDecompose = unit != null && decomposeSelectedIds.Contains(unit.instanceId);
            runtimeCards[i].Bind(this, unit, inParty, isCurrent, isSelectedForDecompose, decomposeSelectionMode);
        }

        if (pageText != null)
            pageText.text = $"{pageIndex + 1}/{totalPages}";
        if (prevButton != null)
            prevButton.gameObject.SetActive(pageIndex > 0);
        if (nextButton != null)
            nextButton.gameObject.SetActive(pageIndex < totalPages - 1);

        if (detailPanelUI != null)
            detailPanelUI.Bind(this, persistentProfileController, selectedUnit);

        RefreshBottomActionUI();
        RefreshTopCurrencyUI();
        RefreshSortFilterVisuals();
    }

    private void RefreshBottomActionUI()
    {
        if (multiSelectButtonText != null)
            multiSelectButtonText.text = decomposeSelectionMode ? "일괄선택 해제" : "일괄선택";

        int soulGain = 0;
        int shardGain = 0;
        List<PersistentRosterUnitData> selectedUnits = GetSelectedUnitsForDecompose();
        if (persistentProfileController != null)
            persistentProfileController.GetDecomposePreview(selectedUnits, out soulGain, out shardGain);

        if (decomposeCountText != null)
            decomposeCountText.text = $"{selectedUnits.Count}분해시 획득";
        if (decomposeShardPreviewText != null)
            decomposeShardPreviewText.text = $"x {shardGain:N0}";
        if (decomposeSoulPreviewText != null)
            decomposeSoulPreviewText.text = $"x {soulGain:N0}";
        if (decomposeButton != null)
            decomposeButton.interactable = decomposeSelectionMode && selectedUnits.Count > 0;
    }

    private void RefreshTopCurrencyUI()
    {
        if (topSoulText != null)
            topSoulText.text = worldRunManager != null ? worldRunManager.PersistentSoul.ToString("N0") : "0";

        if (topShardText != null)
            topShardText.text = persistentProfileController != null ? persistentProfileController.GetUnitShardCount().ToString("N0") : "0";

        if (topHudUI != null)
            topHudUI.Refresh();
    }

    public bool TryGetPromotionBonusPercentPerRank(out float percent)
    {
        if (persistentProfileController == null)
        {
            percent = 1f;
            return false;
        }

        percent = persistentProfileController.PromotionBonusPercentPerRank;
        return true;
    }

    public bool IsPendingPartyCandidate(PersistentRosterUnitData unit)
    {
        if (unit == null || bottomPartySummaryPanelUI == null || bottomPartySummaryPanelUI.PendingBarracksUnit == null)
            return false;

        return bottomPartySummaryPanelUI.PendingBarracksUnit.instanceId == unit.instanceId;
    }

    public bool CanBeginUnitCardPartyDrag(PersistentRosterUnitData unit)
    {
        if (unit == null || bottomPartySummaryPanelUI == null || persistentProfileController == null)
            return false;

        if (persistentProfileController.IsDeadUnit(unit))
            return false;

        return true;
    }

    public void BeginUnitCardPartyDrag(LegionUnitCardUI card)
    {
        if (card == null || card.BoundUnit == null || bottomPartySummaryPanelUI == null)
            return;

        bottomPartySummaryPanelUI.BeginBarracksUnitDrag(card.BoundUnit);
    }

    public void EndUnitCardPartyDrag(LegionUnitCardUI card, bool refreshAfter = true)
    {
        if (card == null || card.BoundUnit == null || bottomPartySummaryPanelUI == null)
            return;

        bottomPartySummaryPanelUI.EndBarracksUnitDrag(card.BoundUnit);

        // OnDisable/패널 닫힘 경로에서는 Unity가 이미 SetActive(false)를 처리 중이므로
        // 이때 RefreshAll()->Bind()->SetActive()를 다시 호출하면 재진입 예외가 발생한다.
        if (refreshAfter && isActiveAndEnabled && gameObject.activeInHierarchy)
            RefreshAll();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (bottomPartySummaryPanelUI != null && bottomPartySummaryPanelUI.HasDraggedPartyEntry)
            HandlePartyEntryDroppedToLegionPanel();
    }

    public void HandlePartyEntryDroppedToLegionPanel()
    {
        if (bottomPartySummaryPanelUI == null)
            return;

        bool removed = bottomPartySummaryPanelUI.TryRemoveDraggedPartyEntryToBarracks();
        UIDragGhostUI.HideGhost();
        if (removed)
            RefreshAll();
    }

    public void HandleUnitCardClicked(LegionUnitCardUI card)
    {
        if (card == null || card.BoundUnit == null)
            return;

        if (decomposeSelectionMode)
        {
            ToggleDecomposeSelection(card.BoundUnit);
            RefreshAll();
            return;
        }

        selectedUnit = card.BoundUnit;

        if (clickCardSelectsPartyCandidate && bottomPartySummaryPanelUI != null)
            bottomPartySummaryPanelUI.SelectBarracksUnitForParty(card.BoundUnit);

        RefreshAll();
    }

    public void HandleCardFavoriteClicked(LegionUnitCardUI card)
    {
        if (card == null || card.BoundUnit == null || persistentProfileController == null)
            return;

        selectedUnit = card.BoundUnit;
        persistentProfileController.ToggleFavorite(card.BoundUnit);
        RefreshAll();
    }

    public void HandleFavoriteToggleClicked()
    {
        if (selectedUnit == null || persistentProfileController == null)
            return;

        persistentProfileController.ToggleFavorite(selectedUnit);
        RefreshAll();
    }

    public void HandleRenameClicked()
    {
        if (selectedUnit == null || persistentProfileController == null || renamePopupUI == null)
            return;

        renamePopupUI.Show(selectedUnit.GetDisplayName(), newName =>
        {
            persistentProfileController.TryRenameUnit(selectedUnit, newName);
            RefreshAll();
        });
    }

    public void HandleLevelUpClicked()
    {
        if (selectedUnit == null || persistentProfileController == null)
            return;

        int beforeLevel = Mathf.Max(1, selectedUnit.currentLevel);
        int beforeExp = Mathf.Max(0, selectedUnit.currentExp);
        int beforeNeed = LegionFormula.GetExpToNextLevel(beforeLevel);

        if (!persistentProfileController.TryLevelUp(selectedUnit))
        {
            RefreshAll();
            return;
        }

        int afterLevel = Mathf.Max(1, selectedUnit.currentLevel);
        int afterExp = Mathf.Max(0, selectedUnit.currentExp);
        int afterNeed = LegionFormula.GetExpToNextLevel(afterLevel);

        RefreshAll();
        detailPanelUI?.PlayLevelUpExpAnimation(beforeLevel, beforeExp, beforeNeed, afterLevel, afterExp, afterNeed);
    }

    public void HandlePromoteClicked()
    {
        if (selectedUnit == null || persistentProfileController == null)
            return;

        persistentProfileController.TryPromote(selectedUnit);
        RefreshAll();
    }

    public void HandleDecomposeButtonClicked()
    {
        if (!decomposeSelectionMode || persistentProfileController == null)
            return;

        List<PersistentRosterUnitData> selectedUnits = GetSelectedUnitsForDecompose();
        if (selectedUnits.Count <= 0)
            return;

        persistentProfileController.GetDecomposePreview(selectedUnits, out int soulGain, out int shardGain);

        if (decomposeConfirmPopupUI != null)
        {
            decomposeConfirmPopupUI.Show(
                "분해 확인",
                $"선택한 {selectedUnits.Count}개의 유닛을 분해하시겠습니까?\n획득 예정: 유닛 파편 {shardGain:N0}, 소울 {soulGain:N0}",
                () => ExecuteConfirmedDecompose(selectedUnits, soulGain, shardGain));
        }
        else
        {
            ExecuteConfirmedDecompose(selectedUnits, soulGain, shardGain);
        }
    }

    private void ExecuteConfirmedDecompose(IReadOnlyList<PersistentRosterUnitData> selectedUnits, int soulGain, int shardGain)
    {
        if (persistentProfileController == null || selectedUnits == null || selectedUnits.Count <= 0)
            return;

        bool changed = persistentProfileController.TryBatchDecompose(selectedUnits);
        if (changed)
            ShowCurrencyGainFeedback(soulGain, shardGain);

        decomposeSelectedIds.Clear();
        decomposeSelectionMode = false;
        selectedUnit = null;
        RefreshAll();
    }

    public void ToggleSortObtained() => ToggleSort(LegionSortKey.Obtained);
    public void ToggleSortName() => ToggleSort(LegionSortKey.Name);
    public void ToggleSortLevel() => ToggleSort(LegionSortKey.Level);
    public void ToggleSortRank() => ToggleSort(LegionSortKey.Rank);
    public void ToggleSortNFT() => ToggleSort(LegionSortKey.NFT);

    public void SetSortNewest() => SetSortObtainedNewest();

    public void SetSortObtainedNewest()
    {
        sortKey = LegionSortKey.Obtained;
        sortDirection = LegionSortDirection.Descending;
        sortAscending = false;
        pageIndex = 0;
        RefreshAll();
    }

    public void ClearSort()
    {
        sortKey = LegionSortKey.None;
        sortDirection = LegionSortDirection.None;
        sortAscending = false;
        pageIndex = 0;
        RefreshAll();
    }

    public void ToggleFilterExchangeable()
    {
        ToggleFilterNft();
    }

    public void ToggleFilterNft()
    {
        filterNftOnly = !filterNftOnly;
        filterExchangeableOnly = filterNftOnly;
        pageIndex = 0;
        RefreshAll();
    }

    public void SetFilterNFTOnly()
    {
        filterNftOnly = true;
        filterExchangeableOnly = true;
        pageIndex = 0;
        RefreshAll();
    }

    public void ToggleFilterFavorite()
    {
        filterFavoriteOnly = !filterFavoriteOnly;
        pageIndex = 0;
        RefreshAll();
    }

    public void SetFilterAllRange()
    {
        filterMeleeOnly = false;
        filterMidOnly = false;
        filterRangedOnly = false;
        filterRange = null;
        pageIndex = 0;
        RefreshAll();
    }

    public void SetFilterMelee()
    {
        filterMeleeOnly = !filterMeleeOnly;
        filterRange = null;
        pageIndex = 0;
        RefreshAll();
    }

    public void SetFilterMid()
    {
        filterMidOnly = !filterMidOnly;
        filterRange = null;
        pageIndex = 0;
        RefreshAll();
    }

    public void SetFilterRanged()
    {
        filterRangedOnly = !filterRangedOnly;
        filterRange = null;
        pageIndex = 0;
        RefreshAll();
    }

    private void ToggleSort(LegionSortKey key)
    {
        if (sortKey != key)
        {
            sortKey = key;
            sortDirection = LegionSortDirection.Descending;
        }
        else if (sortDirection == LegionSortDirection.Descending)
        {
            sortDirection = LegionSortDirection.Ascending;
        }
        else
        {
            sortKey = LegionSortKey.None;
            sortDirection = LegionSortDirection.None;
        }

        sortAscending = sortDirection == LegionSortDirection.Ascending;
        pageIndex = 0;
        RefreshAll();
    }

    private void ToggleMultiSelectMode()
    {
        decomposeSelectionMode = !decomposeSelectionMode;
        if (!decomposeSelectionMode)
            decomposeSelectedIds.Clear();

        RefreshAll();
    }

    private void ToggleDecomposeSelection(PersistentRosterUnitData unit)
    {
        if (unit == null || persistentProfileController == null)
            return;

        if (!persistentProfileController.CanDecompose(unit))
            return;

        if (!decomposeSelectedIds.Add(unit.instanceId))
            decomposeSelectedIds.Remove(unit.instanceId);
    }

    private void PrevPage()
    {
        pageIndex = Mathf.Max(0, pageIndex - 1);
        RefreshAll();
    }

    private void NextPage()
    {
        List<PersistentRosterUnitData> filtered = BuildFilteredUnits();
        int totalPages = Mathf.Max(1, Mathf.CeilToInt(filtered.Count / (float)UnitsPerPage));
        pageIndex = Mathf.Min(totalPages - 1, pageIndex + 1);
        RefreshAll();
    }

    private void EnsureRuntimeCards()
    {
        if (rosterGridRoot == null || unitCardPrefab == null)
            return;

        runtimeCards.Clear();

        for (int i = 0; i < rosterGridRoot.childCount; i++)
        {
            LegionUnitCardUI existing = rosterGridRoot.GetChild(i).GetComponent<LegionUnitCardUI>();
            if (existing != null)
                runtimeCards.Add(existing);
        }

        while (runtimeCards.Count < UnitsPerPage)
        {
            LegionUnitCardUI created = Object.Instantiate(unitCardPrefab, rosterGridRoot);
            created.name = $"LegionUnitCard_{runtimeCards.Count + 1:00}";
            runtimeCards.Add(created);
        }

        if (runtimeCards.Count > UnitsPerPage)
            runtimeCards.RemoveRange(UnitsPerPage, runtimeCards.Count - UnitsPerPage);
    }

    private List<PersistentRosterUnitData> BuildFilteredUnits()
    {
        List<PersistentRosterUnitData> units = persistentProfileController != null
            ? persistentProfileController.GetRosterUnits().Where(u => u != null && !persistentProfileController.IsDeadUnit(u)).ToList()
            : new List<PersistentRosterUnitData>();

        units = units.Where(PassesFilter).ToList();
        ApplySort(units);
        return units;
    }

    private void ApplySort(List<PersistentRosterUnitData> units)
    {
        if (units == null)
            return;

        bool asc = sortDirection == LegionSortDirection.Ascending;
        bool hasSort = sortKey != LegionSortKey.None && sortDirection != LegionSortDirection.None;

        if (!hasSort)
        {
            units.Sort((a, b) =>
            {
                int priority = GetLegionSortPriority(b).CompareTo(GetLegionSortPriority(a));
                if (priority != 0)
                    return priority;
                return b.obtainedOrder.CompareTo(a.obtainedOrder);
            });
            return;
        }

        switch (sortKey)
        {
            case LegionSortKey.Obtained:
                units.Sort((a, b) => asc ? a.obtainedOrder.CompareTo(b.obtainedOrder) : b.obtainedOrder.CompareTo(a.obtainedOrder));
                break;
            case LegionSortKey.Name:
                units.Sort((a, b) =>
                {
                    int cmp = string.Compare(a.GetDisplayName(), b.GetDisplayName(), System.StringComparison.CurrentCulture);
                    if (!asc) cmp = -cmp;
                    return cmp != 0 ? cmp : b.obtainedOrder.CompareTo(a.obtainedOrder);
                });
                break;
            case LegionSortKey.Level:
                units.Sort((a, b) =>
                {
                    int cmp = a.currentLevel.CompareTo(b.currentLevel);
                    if (!asc) cmp = -cmp;
                    return cmp != 0 ? cmp : b.obtainedOrder.CompareTo(a.obtainedOrder);
                });
                break;
            case LegionSortKey.Rank:
                units.Sort((a, b) =>
                {
                    int cmp = a.GetLegionRank().CompareTo(b.GetLegionRank());
                    if (!asc) cmp = -cmp;
                    if (cmp != 0) return cmp;
                    int priority = GetLegionSortPriority(b).CompareTo(GetLegionSortPriority(a));
                    return priority != 0 ? priority : b.obtainedOrder.CompareTo(a.obtainedOrder);
                });
                break;
            case LegionSortKey.NFT:
                units.Sort((a, b) =>
                {
                    int cmp = a.IsNftUnit().CompareTo(b.IsNftUnit());
                    if (!asc) cmp = -cmp;
                    if (cmp != 0) return cmp;
                    int priority = GetLegionSortPriority(b).CompareTo(GetLegionSortPriority(a));
                    return priority != 0 ? priority : b.obtainedOrder.CompareTo(a.obtainedOrder);
                });
                break;
        }
    }

    private bool PassesFilter(PersistentRosterUnitData unit)
    {
        if (unit == null)
            return false;

        if (unit.unitDefinition != null && !unit.unitDefinition.showInLegion && !unit.isConvertedFromPrisoner)
            return false;

        bool nftFilterActive = filterExchangeableOnly || filterNftOnly;
        bool anyFilterActive = nftFilterActive
                            || filterFavoriteOnly
                            || filterMeleeOnly
                            || filterMidOnly
                            || filterRangedOnly;

        // 필터가 하나도 켜져 있지 않으면 모든 레기온 표시 유닛을 보여준다.
        if (!anyFilterActive)
            return true;

        // 필터끼리는 AND 조건이다.
        // 예: NFT + 즐겨찾기 + 근거리 필터가 켜져 있으면
        //     NFT이면서, 즐겨찾기이면서, 근거리인 유닛만 표시한다.
        if (nftFilterActive && !unit.IsNftUnit())
            return false;

        if (filterFavoriteOnly && !unit.isFavorite)
            return false;

        CharacterRangeType range = unit.unitDefinition != null ? unit.unitDefinition.rangeType : CharacterRangeType.Melee;
        if (filterMeleeOnly && range != CharacterRangeType.Melee)
            return false;
        if (filterMidOnly && range != CharacterRangeType.Mid)
            return false;
        if (filterRangedOnly && range != CharacterRangeType.Ranged)
            return false;

        return true;
    }

    private void NormalizeLegacyFilterState()
    {
        if (filterExchangeableOnly && !filterNftOnly)
            filterNftOnly = true;

        if (filterRange.HasValue)
        {
            filterMeleeOnly |= filterRange.Value == CharacterRangeType.Melee;
            filterMidOnly |= filterRange.Value == CharacterRangeType.Mid;
            filterRangedOnly |= filterRange.Value == CharacterRangeType.Ranged;
            filterRange = null;
        }
    }

    private void SyncLegacySortState()
    {
        if (sortKey == LegionSortKey.None)
        {
            sortDirection = LegionSortDirection.None;
            sortAscending = false;
            return;
        }

        if (sortDirection == LegionSortDirection.None)
            sortDirection = sortAscending ? LegionSortDirection.Ascending : LegionSortDirection.Descending;

        sortAscending = sortDirection == LegionSortDirection.Ascending;
    }

    private void RefreshSortFilterVisuals()
    {
        bool hasSort = sortKey != LegionSortKey.None && sortDirection != LegionSortDirection.None;
        sortObtainedButton?.SetOn(hasSort && sortKey == LegionSortKey.Obtained);
        sortNameButton?.SetOn(hasSort && sortKey == LegionSortKey.Name);
        sortLevelButton?.SetOn(hasSort && sortKey == LegionSortKey.Level);
        sortRankButton?.SetOn(hasSort && sortKey == LegionSortKey.Rank);
        sortNftButton?.SetOn(hasSort && sortKey == LegionSortKey.NFT);

        filterNftButton?.SetOn(filterExchangeableOnly || filterNftOnly);
        filterFavoriteButton?.SetOn(filterFavoriteOnly);
        filterMeleeButton?.SetOn(filterMeleeOnly);
        filterMidButton?.SetOn(filterMidOnly);
        filterRangedButton?.SetOn(filterRangedOnly);
    }

    private void ShowCurrencyGainFeedback(int soulGain, int shardGain)
    {
        if (topHudUI != null)
            topHudUI.ShowTemporaryGain(soulGain, shardGain);

        if (soulGain > 0 && topSoulGainText != null)
        {
            if (localSoulGainFadeRoutine != null)
                StopCoroutine(localSoulGainFadeRoutine);
            localSoulGainFadeRoutine = StartCoroutine(FadeLocalGainText(topSoulGainText, topSoulGainCanvasGroup, soulGain));
        }

        if (shardGain > 0 && topShardGainText != null)
        {
            if (localShardGainFadeRoutine != null)
                StopCoroutine(localShardGainFadeRoutine);
            localShardGainFadeRoutine = StartCoroutine(FadeLocalGainText(topShardGainText, topShardGainCanvasGroup, shardGain));
        }
    }

    private IEnumerator FadeLocalGainText(TMP_Text text, CanvasGroup group, int amount)
    {
        if (text == null)
            yield break;

        text.text = $"(+{amount:N0})";
        text.color = gainTextColor;
        text.gameObject.SetActive(true);

        if (group != null)
            group.alpha = 1f;

        float duration = Mathf.Max(0.05f, topGainFadeDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            if (group != null)
                group.alpha = alpha;
            else
            {
                Color c = text.color;
                c.a = alpha;
                text.color = c;
            }
            yield return null;
        }

        if (group != null)
            group.alpha = 0f;
        text.gameObject.SetActive(false);
    }

    private static int GetLegionSortPriority(PersistentRosterUnitData unit)
    {
        return unit != null && unit.unitDefinition != null ? unit.unitDefinition.legionSortPriority : 0;
    }

    private List<PersistentRosterUnitData> GetSelectedUnitsForDecompose()
    {
        List<PersistentRosterUnitData> result = new();
        if (persistentProfileController == null || decomposeSelectedIds.Count <= 0)
            return result;

        IReadOnlyList<PersistentRosterUnitData> all = persistentProfileController.GetRosterUnits();
        for (int i = 0; i < all.Count; i++)
        {
            PersistentRosterUnitData unit = all[i];
            if (unit != null && decomposeSelectedIds.Contains(unit.instanceId))
                result.Add(unit);
        }

        return result;
    }

    private static void BindButton(Button button, UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }
}
