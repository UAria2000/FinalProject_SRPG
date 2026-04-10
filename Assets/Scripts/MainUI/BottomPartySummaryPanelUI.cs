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
    [SerializeField] private StorageSharedConsumableSlotUI sharedConsumableSlotUI;

    [Tooltip("왼쪽 -> 오른쪽 순서로 넣어. 즉 3,2,1,0 자리 순서.")]
    [SerializeField] private List<PartyLoadoutUnitEntryUI> unitEntries = new List<PartyLoadoutUnitEntryUI>();

    private bool storageMode;

    private ItemDefinition pendingInventoryItem;
    private PendingLoadoutItemKind pendingItemKind = PendingLoadoutItemKind.None;

    private ItemDefinition draggedInventoryItem;
    private PartyEquipmentSlotUI draggedEquipmentSlot;
    private PartyLoadoutUnitEntryUI draggedUnitEntry;

    private void Awake()
    {
        if (worldRunManager == null)
            worldRunManager = Object.FindFirstObjectByType<WorldRunManager>();
    }

    private void Start()
    {
        SetStorageMode(false);
        RefreshAll();
    }

    private void OnEnable()
    {
        if (worldRunManager != null)
            worldRunManager.OnStorageChanged += RefreshAll;

        RefreshAll();
    }

    private void OnDisable()
    {
        if (worldRunManager != null)
            worldRunManager.OnStorageChanged -= RefreshAll;
    }

    public bool IsStorageMode()
    {
        return storageMode;
    }

    public void SetStorageMode(bool isOpen)
    {
        storageMode = isOpen;

        if (!storageMode)
        {
            ClearPendingSelection();
            draggedInventoryItem = null;
            draggedEquipmentSlot = null;
            draggedUnitEntry = null;
        }

        RefreshAll();
    }

    public void RefreshAll()
    {
        if (worldRunManager == null)
            return;

        // 먼저 파티를 0,1,2...로 압축
        IReadOnlyList<PartyMemberData> orderedMembers = worldRunManager.GetDisplayOrderedPartyMembers();

        Dictionary<int, PartyMemberData> memberByBattleSlot = new Dictionary<int, PartyMemberData>();
        for (int i = 0; i < orderedMembers.Count; i++)
        {
            PartyMemberData member = orderedMembers[i];
            if (member == null)
                continue;

            memberByBattleSlot[member.startSlotIndex] = member;
        }

        for (int i = 0; i < unitEntries.Count; i++)
        {
            if (unitEntries[i] == null)
                continue;

            // 왼쪽->오른쪽 엔트리라면, 표시 슬롯은 3,2,1,0
            int representedBattleSlotIndex = (unitEntries.Count - 1) - i;

            memberByBattleSlot.TryGetValue(representedBattleSlotIndex, out PartyMemberData member);

            unitEntries[i].Bind(this, member, representedBattleSlotIndex, storageMode);
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

        // 1) 창고에서 잡은 장비를 장비 슬롯에 놓기
        if (draggedInventoryItem != null)
        {
            if (draggedInventoryItem.mainUICategory == MainUIItemCategory.Equipment)
            {
                worldRunManager.TryAssignEquipmentItem(targetSlotUI.Member, targetSlotUI.SlotIndex, draggedInventoryItem);
            }

            draggedInventoryItem = null;
            ClearPendingSelection();
            RefreshAll();
            return;
        }

        // 2) 다른 장비 슬롯에서 잡은 장비를 이 슬롯으로 옮기기/교체하기
        if (draggedEquipmentSlot != null &&
            draggedEquipmentSlot != targetSlotUI &&
            draggedEquipmentSlot.Member != null)
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

    // 새 기능: 창고 UI 빈 영역에 드롭하면 장착 해제
    public void HandleEquipmentDroppedToStorage()
    {
        if (!storageMode || worldRunManager == null)
            return;

        if (draggedEquipmentSlot == null || draggedEquipmentSlot.Member == null)
            return;

        worldRunManager.TryAssignEquipmentItem(
            draggedEquipmentSlot.Member,
            draggedEquipmentSlot.SlotIndex,
            null);

        draggedEquipmentSlot = null;
        RefreshAll();
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

    public void HandleUnitEntryDroppedOn(PartyLoadoutUnitEntryUI targetEntry)
    {
        if (worldRunManager == null)
            return;

        if (draggedUnitEntry == null || targetEntry == null || targetEntry.Member == null)
            return;

        if (draggedUnitEntry == targetEntry || draggedUnitEntry.Member == null)
            return;

        worldRunManager.TrySwapPartyOrder(draggedUnitEntry.Member, targetEntry.Member);
        draggedUnitEntry = null;
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

    private void ClearPendingSelection()
    {
        pendingInventoryItem = null;
        pendingItemKind = PendingLoadoutItemKind.None;
    }
}