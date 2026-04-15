using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum BarracksUnitSortMode
{
    Obtained,
    Name,
    Level,
    Exchangeable,
}

public enum BarracksUnitFilterMode
{
    All,
    InParty,
    NotInParty,
    Exchangeable,
    Favorite,
    Melee,
    Mid,
    Ranged,
}

public class BarracksPanelUI : MainUIPanelBase, IDropHandler
{
    private const int UnitsPerPage = 10;

    [Header("References")]
    [SerializeField] private PersistentProfileController persistentProfileController;
    [SerializeField] private BottomPartySummaryPanelUI bottomPartySummaryPanelUI;
    [SerializeField] private BarracksDetailPanelUI detailPanelUI;

    [Header("Grid")]
    [SerializeField] private RectTransform rosterGridRoot;
    [SerializeField] private BarracksUnitCardUI unitCardPrefab;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_Text pageText;

    [Header("Actions")]
    [SerializeField] private GameObject confirmationRoot;
    [SerializeField] private TMP_Text confirmationText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    [Header("State")]
    [SerializeField] private BarracksUnitSortMode sortMode = BarracksUnitSortMode.Obtained;
    [SerializeField] private BarracksUnitFilterMode filterMode = BarracksUnitFilterMode.All;

    private readonly List<BarracksUnitCardUI> runtimeCards = new List<BarracksUnitCardUI>();

    private int pageIndex;
    private PersistentRosterUnitData selectedUnit;
    private PersistentRosterUnitData draggedUnit;
    private bool waitingDecomposeConfirm;

    protected override void Awake()
    {
        base.Awake();

        if (persistentProfileController == null)
            persistentProfileController = Object.FindFirstObjectByType<PersistentProfileController>();

        EnsureRuntimeCards();
        BindButton(prevButton, PrevPage);
        BindButton(nextButton, NextPage);
        BindButton(confirmButton, ConfirmDangerAction);
        BindButton(cancelButton, CancelDangerAction);
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

        if (bottomPartySummaryPanelUI != null)
            bottomPartySummaryPanelUI.SetBarracksMode(false);
    }

    public void RefreshAll()
    {
        EnsureRuntimeCards();

        List<PersistentRosterUnitData> filtered = BuildFilteredUnits();
        int totalPages = Mathf.Max(1, Mathf.CeilToInt(filtered.Count / (float)UnitsPerPage));
        pageIndex = Mathf.Clamp(pageIndex, 0, totalPages - 1);

        int start = pageIndex * UnitsPerPage;
        for (int i = 0; i < runtimeCards.Count; i++)
        {
            PersistentRosterUnitData unit = (start + i) < filtered.Count ? filtered[start + i] : null;
            bool inParty = unit != null && persistentProfileController != null && persistentProfileController.IsRosterUnitInParty(unit);
            runtimeCards[i].Bind(this, unit, inParty);
            runtimeCards[i].gameObject.SetActive(true);
        }

        if (pageText != null)
            pageText.text = $"{pageIndex + 1}/{totalPages}";

        if (prevButton != null)
            prevButton.gameObject.SetActive(pageIndex > 0);
        if (nextButton != null)
            nextButton.gameObject.SetActive(pageIndex < totalPages - 1);

        if (selectedUnit != null && persistentProfileController != null)
            selectedUnit = persistentProfileController.FindRosterUnit(selectedUnit.instanceId);

        if (selectedUnit == null && filtered.Count > 0)
            selectedUnit = filtered[0];

        if (detailPanelUI != null)
            detailPanelUI.Bind(this, persistentProfileController, selectedUnit);

        if (confirmationRoot != null)
            confirmationRoot.SetActive(waitingDecomposeConfirm && selectedUnit != null);

        if (bottomPartySummaryPanelUI != null)
            bottomPartySummaryPanelUI.RefreshAll();
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

    public void HandleUnitCardClicked(BarracksUnitCardUI card)
    {
        if (card == null || card.BoundUnit == null)
            return;

        selectedUnit = card.BoundUnit;
        waitingDecomposeConfirm = false;

        if (bottomPartySummaryPanelUI != null)
            bottomPartySummaryPanelUI.TryHandleBarracksUnitClicked(selectedUnit);

        RefreshAll();
    }

    public void BeginUnitCardDrag(BarracksUnitCardUI card)
    {
        if (card == null || card.BoundUnit == null)
            return;

        draggedUnit = card.BoundUnit;
        selectedUnit = card.BoundUnit;
        if (bottomPartySummaryPanelUI != null)
            bottomPartySummaryPanelUI.BeginBarracksUnitDrag(card.BoundUnit);
        RefreshAll();
    }

    public void EndUnitCardDrag(BarracksUnitCardUI card)
    {
        if (card != null && bottomPartySummaryPanelUI != null && card.BoundUnit != null)
            bottomPartySummaryPanelUI.EndBarracksUnitDrag(card.BoundUnit);

        draggedUnit = null;
    }

    public void HandleFavoriteToggleClicked()
    {
        if (selectedUnit == null || persistentProfileController == null)
            return;

        waitingDecomposeConfirm = false;
        persistentProfileController.ToggleFavorite(selectedUnit);
        RefreshAll();
    }

    public void HandleLevelUpClicked()
    {
        if (selectedUnit == null || persistentProfileController == null)
            return;

        waitingDecomposeConfirm = false;
        persistentProfileController.TryLevelUp(selectedUnit);
        RefreshAll();
    }

    public void HandlePromoteClicked()
    {
        if (selectedUnit == null || persistentProfileController == null)
            return;

        waitingDecomposeConfirm = false;
        persistentProfileController.TryPromote(selectedUnit);
        RefreshAll();
    }

    public void HandleDecomposeClicked()
    {
        if (selectedUnit == null || persistentProfileController == null)
            return;

        if (!persistentProfileController.CanDecompose(selectedUnit))
            return;

        waitingDecomposeConfirm = true;
        if (confirmationText != null)
            confirmationText.text = $"{selectedUnit.GetDisplayName()} 을(를) 분해하시겠습니까?";
        RefreshAll();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (bottomPartySummaryPanelUI != null)
            bottomPartySummaryPanelUI.HandleDraggedPartyEntryDroppedToBarracks();
    }

    public void SetSortObtained() => SetSortMode(BarracksUnitSortMode.Obtained);
    public void SetSortName() => SetSortMode(BarracksUnitSortMode.Name);
    public void SetSortLevel() => SetSortMode(BarracksUnitSortMode.Level);
    public void SetSortExchangeable() => SetSortMode(BarracksUnitSortMode.Exchangeable);

    public void SetFilterAll() => SetFilterMode(BarracksUnitFilterMode.All);
    public void SetFilterInParty() => SetFilterMode(BarracksUnitFilterMode.InParty);
    public void SetFilterNotInParty() => SetFilterMode(BarracksUnitFilterMode.NotInParty);
    public void SetFilterExchangeable() => SetFilterMode(BarracksUnitFilterMode.Exchangeable);
    public void SetFilterFavorite() => SetFilterMode(BarracksUnitFilterMode.Favorite);
    public void SetFilterMelee() => SetFilterMode(BarracksUnitFilterMode.Melee);
    public void SetFilterMid() => SetFilterMode(BarracksUnitFilterMode.Mid);
    public void SetFilterRanged() => SetFilterMode(BarracksUnitFilterMode.Ranged);

    private void SetSortMode(BarracksUnitSortMode mode)
    {
        sortMode = mode;
        pageIndex = 0;
        RefreshAll();
    }

    private void SetFilterMode(BarracksUnitFilterMode mode)
    {
        filterMode = mode;
        pageIndex = 0;
        RefreshAll();
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

    private void ConfirmDangerAction()
    {
        if (!waitingDecomposeConfirm || selectedUnit == null || persistentProfileController == null)
            return;

        persistentProfileController.TryDecompose(selectedUnit);
        selectedUnit = null;
        waitingDecomposeConfirm = false;
        RefreshAll();
    }

    private void CancelDangerAction()
    {
        waitingDecomposeConfirm = false;
        RefreshAll();
    }

    private void EnsureRuntimeCards()
    {
        if (rosterGridRoot == null || unitCardPrefab == null)
            return;

        runtimeCards.Clear();
        for (int i = 0; i < rosterGridRoot.childCount; i++)
        {
            BarracksUnitCardUI existing = rosterGridRoot.GetChild(i).GetComponent<BarracksUnitCardUI>();
            if (existing != null)
                runtimeCards.Add(existing);
        }

        while (runtimeCards.Count < UnitsPerPage)
        {
            BarracksUnitCardUI created = Instantiate(unitCardPrefab, rosterGridRoot);
            created.name = $"BarracksUnitCard_{runtimeCards.Count + 1:00}";
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

        switch (sortMode)
        {
            case BarracksUnitSortMode.Name:
                units = units.OrderBy(u => u.GetDisplayName()).ThenBy(u => u.obtainedOrder).ToList();
                break;
            case BarracksUnitSortMode.Level:
                units = units.OrderByDescending(u => u.currentLevel).ThenBy(u => u.obtainedOrder).ToList();
                break;
            case BarracksUnitSortMode.Exchangeable:
                units = units.OrderByDescending(u => u.isExchangeable).ThenBy(u => u.obtainedOrder).ToList();
                break;
            default:
                units = units.OrderBy(u => u.obtainedOrder).ToList();
                break;
        }

        return units;
    }

    private bool PassesFilter(PersistentRosterUnitData unit)
    {
        if (unit == null)
            return false;

        bool inParty = persistentProfileController != null && persistentProfileController.IsRosterUnitInParty(unit);

        switch (filterMode)
        {
            case BarracksUnitFilterMode.InParty:
                return inParty;
            case BarracksUnitFilterMode.NotInParty:
                return !inParty;
            case BarracksUnitFilterMode.Exchangeable:
                return unit.isExchangeable;
            case BarracksUnitFilterMode.Favorite:
                return unit.isFavorite;
            case BarracksUnitFilterMode.Melee:
                return unit.unitDefinition != null && unit.unitDefinition.rangeType == CharacterRangeType.Melee;
            case BarracksUnitFilterMode.Mid:
                return unit.unitDefinition != null && unit.unitDefinition.rangeType == CharacterRangeType.Mid;
            case BarracksUnitFilterMode.Ranged:
                return unit.unitDefinition != null && unit.unitDefinition.rangeType == CharacterRangeType.Ranged;
            default:
                return true;
        }
    }

    private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }
}
