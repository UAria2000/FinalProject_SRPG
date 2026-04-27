using System.Collections.Generic;
using UnityEngine;

public enum PendingLoadoutItemKind
{
    None,
    Equipment,
    SharedConsumable
}

public class BottomPartySummaryPanelUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WorldRunManager worldRunManager;
    [SerializeField] private PersistentProfileController persistentProfileController;
    [SerializeField] private StorageSharedConsumableSlotUI sharedConsumableSlotUI;
    [SerializeField] private StorageItemTooltipUI sharedConsumableTooltipUI;

    [Tooltip("왼쪽 -> 오른쪽 순서로 넣어. 즉 3,2,1,0 자리 순서.")]
    [SerializeField] private List<PartyLoadoutUnitEntryUI> unitEntries = new List<PartyLoadoutUnitEntryUI>();

    private bool storageMode;
    private bool barracksMode;
    private bool externalMainPanelOpen;

    private ItemDefinition pendingInventoryItem;
    private PendingLoadoutItemKind pendingItemKind = PendingLoadoutItemKind.None;

    private ItemDefinition draggedInventoryItem;
    private PartyEquipmentSlotUI draggedEquipmentSlot;
    private PartyLoadoutUnitEntryUI draggedUnitEntry;

    private PersistentRosterUnitData pendingBarracksUnit;
    private PersistentRosterUnitData draggedBarracksUnit;

    private int expandedWorldSlotIndex = -1;

    private void Awake()
    {
        if (worldRunManager == null)
            worldRunManager = Object.FindFirstObjectByType<WorldRunManager>();

        if (persistentProfileController == null)
            persistentProfileController = Object.FindFirstObjectByType<PersistentProfileController>();
    }

    private void Start()
    {
        SetStorageMode(false);
        SetBarracksMode(false);
        SetExternalMainPanelOpen(false);
        RefreshAll();
    }

    private void OnEnable()
    {
        if (worldRunManager != null)
            worldRunManager.OnStorageChanged += RefreshAll;

        if (persistentProfileController != null)
            persistentProfileController.OnProfileChanged += RefreshAll;

        RefreshAll();
    }

    private void OnDisable()
    {
        if (worldRunManager != null)
            worldRunManager.OnStorageChanged -= RefreshAll;

        if (persistentProfileController != null)
            persistentProfileController.OnProfileChanged -= RefreshAll;
    }

    public bool IsStorageMode() => storageMode;
    public bool IsBarracksMode() => barracksMode;
    public bool IsAnyMainPanelOpen() => storageMode || barracksMode || externalMainPanelOpen;
    public PersistentRosterUnitData PendingBarracksUnit => pendingBarracksUnit;
    public bool HasPendingBarracksUnit => pendingBarracksUnit != null;
    public bool HasDraggedPartyEntry => draggedUnitEntry != null && draggedUnitEntry.Member != null;

    public bool IsDraggingPartyEntry(PartyLoadoutUnitEntryUI entry)
    {
        return entry != null && draggedUnitEntry == entry;
    }

    public void SetStorageMode(bool isOpen)
    {
        storageMode = isOpen;

        if (!storageMode)
        {
            ClearPendingSelection();
            draggedInventoryItem = null;
            draggedEquipmentSlot = null;
        }

        RefreshAll();
    }

    public void SetBarracksMode(bool isOpen)
    {
        barracksMode = isOpen;

        if (!barracksMode)
        {
            pendingBarracksUnit = null;
            draggedBarracksUnit = null;
            draggedUnitEntry = null;
            UIDragGhostUI.HideGhost();
        }

        RefreshAll();
    }

    public void SetExternalMainPanelOpen(bool isOpen)
    {
        externalMainPanelOpen = isOpen;
        RefreshAll();
    }

    public void CollapseExpandedWorldEntry()
    {
        expandedWorldSlotIndex = -1;
        RefreshAll();
    }

    public void RefreshAll()
    {
        if (worldRunManager == null)
            return;

        IReadOnlyList<PartyMemberData> orderedMembers = worldRunManager.GetDisplayOrderedPartyMembers();
        Dictionary<int, PartyMemberData> memberByBattleSlot = new Dictionary<int, PartyMemberData>();

        for (int i = 0; i < orderedMembers.Count; i++)
        {
            PartyMemberData member = orderedMembers[i];
            if (member == null)
                continue;

            memberByBattleSlot[member.startSlotIndex] = member;
        }

        bool anyMainPanelOpen = IsAnyMainPanelOpen();

        for (int i = 0; i < unitEntries.Count; i++)
        {
            if (unitEntries[i] == null)
                continue;

            int representedBattleSlotIndex = (unitEntries.Count - 1) - i;
            memberByBattleSlot.TryGetValue(representedBattleSlotIndex, out PartyMemberData member);

            bool isExpandedWorldEntry = expandedWorldSlotIndex == representedBattleSlotIndex;
            bool showEquipmentSlots = member != null && (anyMainPanelOpen || isExpandedWorldEntry);
            bool showWorldInfo = member != null && isExpandedWorldEntry && !anyMainPanelOpen;

            bool isPendingBarracksMember = IsSameRosterRuntimeUnit(member, pendingBarracksUnit);
            bool canReceivePendingBarracksUnit = barracksMode && pendingBarracksUnit != null && !isPendingBarracksMember;

            unitEntries[i].Bind(
                this,
                member,
                representedBattleSlotIndex,
                showEquipmentSlots,
                showWorldInfo,
                barracksMode,
                canReceivePendingBarracksUnit,
                isPendingBarracksMember);
        }

        if (sharedConsumableSlotUI != null)
            sharedConsumableSlotUI.Bind(this, worldRunManager.GetSharedConsumableItem());
    }

    public ItemDefinition GetAssignedEquipment(PartyMemberData member, int equipmentSlotIndex)
    {
        if (worldRunManager == null || member == null)
            return null;

        return worldRunManager.GetAssignedEquipmentItem(member, equipmentSlotIndex);
    }

    public string GetWorldLevelText(PartyMemberData member)
    {
        if (member == null)
            return string.Empty;

        return $"Lv.{Mathf.Max(1, member.currentLevel)}";
    }

    public string GetWorldHPText(PartyMemberData member)
    {
        if (member == null)
            return string.Empty;

        int maxHP = GetWorldMaxHP(member);
        int currentHP = GetWorldCurrentHP(member, maxHP);
        return $"{currentHP}/{maxHP}";
    }

    public int GetWorldWarningStage(PartyMemberData member)
    {
        if (member == null)
            return 0;

        int maxHP = GetWorldMaxHP(member);
        int currentHP = GetWorldCurrentHP(member, maxHP);
        if (maxHP <= 0)
            return 0;

        float hpRatio = currentHP / (float)maxHP;

        if (hpRatio <= 0.25f)
            return 3;
        if (hpRatio <= 0.50f)
            return 2;
        if (hpRatio <= 0.75f)
            return 1;

        return 0;
    }

    private int GetWorldMaxHP(PartyMemberData member)
    {
        if (member == null || member.unitDefinition == null)
            return 1;

        int varianceBonus = member.statVariance != null ? member.statVariance.maxHpDelta : 0;
        int growthBonus = Mathf.Max(0, member.levelGrowthMaxHp);
        int baseMaxHP = Mathf.Max(1, member.unitDefinition.maxHP + varianceBonus + growthBonus);

        float promotionPercentPerRank = Mathf.Max(0f, member.promotionBonusPercentPerRank);
        float promotionMultiplier = LegionFormula.GetPromotionMultiplier(member.promotionRank, promotionPercentPerRank);

        return Mathf.Max(1, Mathf.RoundToInt(baseMaxHP * promotionMultiplier));
    }

    private int GetWorldCurrentHP(PartyMemberData member, int maxHP)
    {
        if (member == null)
            return 0;

        if (member.persistentCurrentHP < 0)
            return maxHP;

        return Mathf.Clamp(member.persistentCurrentHP, 0, maxHP);
    }

    public bool IsItemPendingSelection(ItemDefinition item)
    {
        return pendingInventoryItem == item && pendingItemKind != PendingLoadoutItemKind.None;
    }

    public bool TryHandleStorageItemClicked(ItemDefinition item)
    {
        if (item == null)
            return false;

        if (item.mainUICategory == MainUIItemCategory.Equipment)
        {
            TogglePendingItem(item, PendingLoadoutItemKind.Equipment);
            return true;
        }

        bool isSharedConsumableCandidate =
            item.canAssignToSharedConsumableSlot ||
            item.mainUICategory == MainUIItemCategory.Consumable;

        if (isSharedConsumableCandidate)
        {
            TogglePendingItem(item, PendingLoadoutItemKind.SharedConsumable);
            return true;
        }

        return false;
    }

    public bool TryHandleBarracksUnitClicked(PersistentRosterUnitData unit)
    {
        // 기존 호출부 호환용: 레기온 카드/막사 카드 클릭은 기본적으로 "파티 후보 선택"으로 처리한다.
        return SelectBarracksUnitForParty(unit);
    }

    public bool SelectBarracksUnitForParty(PersistentRosterUnitData unit)
    {
        if (!barracksMode || persistentProfileController == null || unit == null)
            return false;

        if (persistentProfileController.IsDeadUnit(unit))
            return false;

        if (pendingBarracksUnit != null && pendingBarracksUnit.instanceId == unit.instanceId)
            pendingBarracksUnit = null;
        else
            pendingBarracksUnit = unit;

        RefreshAll();
        return true;
    }

    public bool TryAutoAssignBarracksUnit(PersistentRosterUnitData unit)
    {
        if (!barracksMode || persistentProfileController == null || unit == null)
            return false;

        if (persistentProfileController.IsDeadUnit(unit))
            return false;

        if (persistentProfileController.IsRosterUnitInParty(unit))
        {
            pendingBarracksUnit = unit;
            RefreshAll();
            return true;
        }

        if (persistentProfileController.TryAssignRosterUnitToPartyAuto(unit))
        {
            pendingBarracksUnit = null;
            RefreshAll();
            return true;
        }

        pendingBarracksUnit = unit;
        RefreshAll();
        return true;
    }

    public void HandleSharedConsumableClicked()
    {
        if (!storageMode || worldRunManager == null)
            return;

        if (pendingItemKind == PendingLoadoutItemKind.SharedConsumable && pendingInventoryItem != null)
        {
            worldRunManager.TryAssignSharedConsumable(pendingInventoryItem);
            ClearPendingSelection();
            RefreshAll();
            return;
        }

        if (worldRunManager.GetSharedConsumableItem() != null)
        {
            worldRunManager.TryAssignSharedConsumable(null);
            RefreshAll();
        }
    }

    public void HandleSharedConsumableDropped()
    {
        if (!storageMode || worldRunManager == null)
            return;

        if (draggedInventoryItem == null)
            return;

        bool isSharedConsumableCandidate =
            draggedInventoryItem.canAssignToSharedConsumableSlot ||
            draggedInventoryItem.mainUICategory == MainUIItemCategory.Consumable;

        if (!isSharedConsumableCandidate)
            return;

        worldRunManager.TryAssignSharedConsumable(draggedInventoryItem);
        draggedInventoryItem = null;
        ClearPendingSelection();
        RefreshAll();
    }

    public void HandleEquipmentSlotClicked(PartyEquipmentSlotUI slotUI)
    {
        if (!storageMode || worldRunManager == null || slotUI == null || slotUI.Member == null)
            return;

        if (pendingItemKind == PendingLoadoutItemKind.Equipment && pendingInventoryItem != null)
        {
            worldRunManager.TryAssignEquipmentItem(slotUI.Member, slotUI.SlotIndex, pendingInventoryItem);
            ClearPendingSelection();
            RefreshAll();
            return;
        }

        if (slotUI.AssignedItem != null)
        {
            worldRunManager.TryAssignEquipmentItem(slotUI.Member, slotUI.SlotIndex, null);
            RefreshAll();
        }
    }

    public void HandleEquipmentSlotDropped(PartyEquipmentSlotUI targetSlotUI)
    {
        if (!storageMode || worldRunManager == null || targetSlotUI == null || targetSlotUI.Member == null)
            return;

        if (draggedInventoryItem != null)
        {
            if (draggedInventoryItem.mainUICategory == MainUIItemCategory.Equipment)
                worldRunManager.TryAssignEquipmentItem(targetSlotUI.Member, targetSlotUI.SlotIndex, draggedInventoryItem);

            draggedInventoryItem = null;
            ClearPendingSelection();
            RefreshAll();
            return;
        }

        if (draggedEquipmentSlot != null && draggedEquipmentSlot != targetSlotUI && draggedEquipmentSlot.Member != null)
        {
            worldRunManager.TryMoveOrSwapEquipment(
                draggedEquipmentSlot.Member,
                draggedEquipmentSlot.SlotIndex,
                targetSlotUI.Member,
                targetSlotUI.SlotIndex);

            draggedEquipmentSlot = null;
            RefreshAll();
        }
    }

    public void HandleEquipmentDroppedToStorage()
    {
        if (!storageMode || worldRunManager == null)
            return;

        if (draggedEquipmentSlot == null || draggedEquipmentSlot.Member == null)
            return;

        worldRunManager.TryAssignEquipmentItem(draggedEquipmentSlot.Member, draggedEquipmentSlot.SlotIndex, null);
        draggedEquipmentSlot = null;
        RefreshAll();
    }

    public void HandleUnitEntryClicked(PartyLoadoutUnitEntryUI targetEntry)
    {
        if (targetEntry == null)
            return;

        // Barracks에서 대기 중인 유닛으로 파티 교체/배치
        if (barracksMode && persistentProfileController != null && pendingBarracksUnit != null)
        {
            bool success;
            if (targetEntry.Member == null)
                success = persistentProfileController.TryAssignRosterUnitToPartySlot(pendingBarracksUnit, targetEntry.RepresentedBattleSlotIndex);
            else
                success = persistentProfileController.TryReplacePartyMemberWithRosterUnit(pendingBarracksUnit, targetEntry.Member);

            if (success)
                pendingBarracksUnit = null;

            RefreshAll();
            return;
        }

        // 월드 기본 상태에서만 포트레잇 클릭 상세 토글
        if (!IsAnyMainPanelOpen())
        {
            if (targetEntry.Member == null)
                return;

            if (expandedWorldSlotIndex == targetEntry.RepresentedBattleSlotIndex)
                expandedWorldSlotIndex = -1;
            else
                expandedWorldSlotIndex = targetEntry.RepresentedBattleSlotIndex;

            RefreshAll();
        }
    }

    public void HandleDraggedPartyEntryDroppedToBarracks()
    {
        TryRemoveDraggedPartyEntryToBarracks();
    }

    public bool TryRemoveDraggedPartyEntryToBarracks()
    {
        PartyLoadoutUnitEntryUI sourceEntry = draggedUnitEntry;

        if (!barracksMode || sourceEntry == null || sourceEntry.Member == null)
        {
            UIDragGhostUI.HideGhost();
            sourceEntry?.RestoreDragRaycastState();
            if (draggedUnitEntry == sourceEntry)
                draggedUnitEntry = null;
            return false;
        }

        bool removed = TryRemovePartyEntryFromBarracks(sourceEntry);

        sourceEntry.RestoreDragRaycastState();
        UIDragGhostUI.HideGhost();

        if (draggedUnitEntry == sourceEntry)
            draggedUnitEntry = null;

        RefreshAll();
        return removed;
    }

    public bool TryRemovePartyEntryByDoubleClick(PartyLoadoutUnitEntryUI entry)
    {
        if (!barracksMode || entry == null || entry.Member == null)
            return false;

        bool removed = TryRemovePartyEntryFromBarracks(entry);
        if (removed)
        {
            if (draggedUnitEntry == entry)
                draggedUnitEntry = null;
            UIDragGhostUI.HideGhost();
            entry.RestoreDragRaycastState();
        }
        return removed;
    }

    public bool TryRemovePartyEntryFromBarracks(PartyLoadoutUnitEntryUI entry)
    {
        if (!barracksMode || persistentProfileController == null || entry == null || entry.Member == null)
            return false;

        string removedInstanceId = entry.Member.instanceId;
        bool removed = persistentProfileController.TryRemovePartyMemberToRoster(entry.Member);
        if (!removed)
            return false;

        if (pendingBarracksUnit != null && !string.IsNullOrWhiteSpace(removedInstanceId) && pendingBarracksUnit.instanceId == removedInstanceId)
            pendingBarracksUnit = null;

        RefreshAll();
        return true;
    }

    public void BeginStorageItemDrag(ItemDefinition item)
    {
        draggedInventoryItem = item;
    }

    public void EndStorageItemDrag(ItemDefinition item)
    {
        if (draggedInventoryItem == item)
            draggedInventoryItem = null;
    }

    public void BeginEquipmentDrag(PartyEquipmentSlotUI slotUI)
    {
        if (!storageMode || slotUI == null || slotUI.AssignedItem == null)
            return;

        draggedEquipmentSlot = slotUI;
    }

    public void EndEquipmentDrag(PartyEquipmentSlotUI slotUI)
    {
        if (draggedEquipmentSlot == slotUI)
            draggedEquipmentSlot = null;
    }

    public void BeginUnitEntryDrag(PartyLoadoutUnitEntryUI entryUI)
    {
        if (entryUI == null || entryUI.Member == null)
            return;

        draggedUnitEntry = entryUI;
    }

    public void EndUnitEntryDrag(PartyLoadoutUnitEntryUI entryUI)
    {
        if (draggedUnitEntry == entryUI)
            draggedUnitEntry = null;
    }

    public void BeginBarracksUnitDrag(PersistentRosterUnitData unit)
    {
        if (!barracksMode || unit == null)
            return;

        draggedBarracksUnit = unit;
        pendingBarracksUnit = unit;
    }

    public void EndBarracksUnitDrag(PersistentRosterUnitData unit)
    {
        if (draggedBarracksUnit == unit)
            draggedBarracksUnit = null;
    }

    public void HandleUnitEntryDroppedOn(PartyLoadoutUnitEntryUI targetEntry)
    {
        if (targetEntry == null)
            return;

        if (barracksMode && persistentProfileController != null && draggedBarracksUnit != null)
        {
            bool success;
            if (targetEntry.Member == null)
                success = persistentProfileController.TryAssignRosterUnitToPartySlot(draggedBarracksUnit, targetEntry.RepresentedBattleSlotIndex);
            else
                success = persistentProfileController.TryReplacePartyMemberWithRosterUnit(draggedBarracksUnit, targetEntry.Member);

            if (success)
                pendingBarracksUnit = null;

            draggedBarracksUnit = null;
            UIDragGhostUI.HideGhost();
            RefreshAll();
            return;
        }

        if (draggedUnitEntry == null || draggedUnitEntry == targetEntry || draggedUnitEntry.Member == null)
            return;

        // 빈 슬롯으로 드래그하면 단순 스왑이 아니라 해당 위치로 파티 순서를 이동한다.
        if (targetEntry.Member == null && persistentProfileController != null)
        {
            PersistentRosterUnitData movingUnit = persistentProfileController.FindRosterUnit(draggedUnitEntry.Member.instanceId);
            if (movingUnit != null)
            {
                persistentProfileController.TryAssignRosterUnitToPartySlot(movingUnit, targetEntry.RepresentedBattleSlotIndex);
                draggedUnitEntry.RestoreDragRaycastState();
                draggedUnitEntry = null;
                UIDragGhostUI.HideGhost();
                RefreshAll();
            }
            return;
        }

        if (worldRunManager == null || targetEntry.Member == null)
            return;

        worldRunManager.TrySwapPartyOrder(draggedUnitEntry.Member, targetEntry.Member);
        draggedUnitEntry.RestoreDragRaycastState();
        draggedUnitEntry = null;
        UIDragGhostUI.HideGhost();
        RefreshAll();
    }

    private void TogglePendingItem(ItemDefinition item, PendingLoadoutItemKind kind)
    {
        if (pendingInventoryItem == item && pendingItemKind == kind)
        {
            ClearPendingSelection();
        }
        else
        {
            pendingInventoryItem = item;
            pendingItemKind = kind;
        }

        RefreshAll();
    }
    public void HandleSharedConsumableHoverEnter()
    {
        if (sharedConsumableTooltipUI == null || worldRunManager == null)
            return;

        ItemDefinition item = worldRunManager.GetSharedConsumableItem();
        if (item == null)
            return;

        sharedConsumableTooltipUI.Show(item, true, 0);
    }

    public void HandleSharedConsumableHoverExit()
    {
        if (sharedConsumableTooltipUI == null)
            return;

        sharedConsumableTooltipUI.Hide();
    }
    private bool IsSameRosterRuntimeUnit(PartyMemberData member, PersistentRosterUnitData unit)
    {
        if (member == null || unit == null)
            return false;

        return !string.IsNullOrWhiteSpace(member.instanceId) && member.instanceId == unit.instanceId;
    }

    private void ClearPendingSelection()
    {
        pendingInventoryItem = null;
        pendingItemKind = PendingLoadoutItemKind.None;
    }
}