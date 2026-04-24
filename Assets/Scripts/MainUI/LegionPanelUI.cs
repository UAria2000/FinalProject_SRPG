using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum LegionSortKey
{
    Obtained,
    Name,
    Level,
}

public class LegionPanelUI : MainUIPanelBase
{
    private const int UnitsPerPage = 10;

    [Header("References")]
    [SerializeField] private PersistentProfileController persistentProfileController;
    [SerializeField] private LegionDetailPanelUI detailPanelUI;
    [SerializeField] private LegionRenamePopupUI renamePopupUI;
    [SerializeField] private LegionDecomposeConfirmPopupUI decomposeConfirmPopupUI;

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

    [Header("State")]
    [SerializeField] private LegionSortKey sortKey = LegionSortKey.Obtained;
    [SerializeField] private bool sortAscending;
    [SerializeField] private bool filterExchangeableOnly;
    [SerializeField] private bool filterFavoriteOnly;
    [SerializeField] private CharacterRangeType? filterRange;

    private readonly List<LegionUnitCardUI> runtimeCards = new();
    private readonly HashSet<string> decomposeSelectedIds = new();

    private int pageIndex;
    private PersistentRosterUnitData selectedUnit;
    private bool decomposeSelectionMode;

    public PersistentProfileController ProfileController => persistentProfileController;
    public WorldRunManager RuntimeWorldRunManager => worldRunManager;
    public bool IsDecomposeSelectionMode => decomposeSelectionMode;

    protected override void Awake()
    {
        base.Awake();

        if (persistentProfileController == null)
            persistentProfileController = UnityEngine.Object.FindFirstObjectByType<PersistentProfileController>();

        EnsureRuntimeCards();
        BindButton(prevButton, PrevPage);
        BindButton(nextButton, NextPage);
        BindButton(multiSelectButton, ToggleMultiSelectMode);
        BindButton(decomposeButton, HandleDecomposeButtonClicked);
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

        RefreshAll();
    }

    protected override void OnPanelClosed()
    {
        if (persistentProfileController != null)
            persistentProfileController.OnProfileChanged -= RefreshAll;
        if (worldRunManager != null)
            worldRunManager.OnStorageChanged -= RefreshAll;

        decomposeSelectionMode = false;
        decomposeSelectedIds.Clear();
    }

    public void RefreshAll()
    {
        EnsureRuntimeCards();

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
    }

    private void RefreshBottomActionUI()
    {
        if (multiSelectButtonText != null)
            multiSelectButtonText.text = decomposeSelectionMode ? "일괄선택 해제" : "일괄선택";

        int soulGain = 0;
        int shardGain = 0;
        if (persistentProfileController != null)
            persistentProfileController.GetDecomposePreview(GetSelectedUnitsForDecompose(), out soulGain, out shardGain);

        if (decomposeCountText != null)
            decomposeCountText.text = $"{decomposeSelectedIds.Count}분해시 획득";
        if (decomposeShardPreviewText != null)
            decomposeShardPreviewText.text = $"x {shardGain:N0}";
        if (decomposeSoulPreviewText != null)
            decomposeSoulPreviewText.text = $"x {soulGain:N0}";
        if (decomposeButton != null)
            decomposeButton.interactable = decomposeSelectionMode && decomposeSelectedIds.Count > 0;
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

        persistentProfileController.TryLevelUp(selectedUnit);
        RefreshAll();
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
                $"선택한 {selectedUnits.Count}개의 유닛을 분해하시겠습니까?\n획득 예정: 승급 파편 {shardGain:N0}, 소울 {soulGain:N0}",
                () =>
                {
                    persistentProfileController.TryBatchDecompose(selectedUnits);
                    decomposeSelectedIds.Clear();
                    decomposeSelectionMode = false;
                    selectedUnit = null;
                    RefreshAll();
                });
        }
        else
        {
            persistentProfileController.TryBatchDecompose(selectedUnits);
            decomposeSelectedIds.Clear();
            decomposeSelectionMode = false;
            selectedUnit = null;
            RefreshAll();
        }
    }

    public void ToggleSortName() => ToggleSort(LegionSortKey.Name);
    public void ToggleSortLevel() => ToggleSort(LegionSortKey.Level);

    public void SetSortObtainedNewest()
    {
        sortKey = LegionSortKey.Obtained;
        sortAscending = false;
        pageIndex = 0;
        RefreshAll();
    }

    public void ToggleFilterExchangeable()
    {
        filterExchangeableOnly = !filterExchangeableOnly;
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
        filterRange = null;
        pageIndex = 0;
        RefreshAll();
    }

    public void SetFilterMelee()
    {
        filterRange = filterRange == CharacterRangeType.Melee ? (CharacterRangeType?)null : CharacterRangeType.Melee;
        pageIndex = 0;
        RefreshAll();
    }

    public void SetFilterMid()
    {
        filterRange = filterRange == CharacterRangeType.Mid ? (CharacterRangeType?)null : CharacterRangeType.Mid;
        pageIndex = 0;
        RefreshAll();
    }

    public void SetFilterRanged()
    {
        filterRange = filterRange == CharacterRangeType.Ranged ? (CharacterRangeType?)null : CharacterRangeType.Ranged;
        pageIndex = 0;
        RefreshAll();
    }

    private void ToggleSort(LegionSortKey key)
    {
        if (sortKey == key)
            sortAscending = !sortAscending;
        else
        {
            sortKey = key;
            sortAscending = true;
        }

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
            ? persistentProfileController.GetRosterUnits().Where(u => u != null).ToList()
            : new List<PersistentRosterUnitData>();

        units = units.Where(PassesFilter).ToList();

        switch (sortKey)
        {
            case LegionSortKey.Name:
                units = sortAscending
                    ? units.OrderBy(u => u.GetDisplayName()).ThenByDescending(u => u.obtainedOrder).ToList()
                    : units.OrderByDescending(u => u.GetDisplayName()).ThenByDescending(u => u.obtainedOrder).ToList();
                break;
            case LegionSortKey.Level:
                units = sortAscending
                    ? units.OrderBy(u => u.currentLevel).ThenByDescending(u => u.obtainedOrder).ToList()
                    : units.OrderByDescending(u => u.currentLevel).ThenByDescending(u => u.obtainedOrder).ToList();
                break;
            default:
                units = units.OrderByDescending(u => u.obtainedOrder).ToList();
                break;
        }

        return units;
    }

    private bool PassesFilter(PersistentRosterUnitData unit)
    {
        if (unit == null)
            return false;

        if (filterExchangeableOnly && !unit.isExchangeable)
            return false;

        if (filterFavoriteOnly && !unit.isFavorite)
            return false;

        if (filterRange.HasValue)
        {
            CharacterRangeType range = unit.unitDefinition != null ? unit.unitDefinition.rangeType : CharacterRangeType.Melee;
            if (range != filterRange.Value)
                return false;
        }

        return true;
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

    private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }
}
