using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WorldRunManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WorldGenerationSettings generationSettings;
    [SerializeField] private HexWorldMapUI worldMapUI;
    [SerializeField] private SelectedTileInfoPanel selectedTileInfoPanel;
    [SerializeField] private WorldEventController eventController;
    [SerializeField] private WorldQuestController questController;
    [SerializeField] private SaveCoordinator saveCoordinator;

    [Header("Startup")]
    [SerializeField] private bool generateOnStart = true;

    [Header("Player Party")]
    [SerializeField] private PartyDefinition playerPartyTemplate;

    [Header("Persistent Currencies")]
    [SerializeField] private int persistentSoul;
    [SerializeField] private int persistentCash;

    [Header("World HUD")]
    [SerializeField] private WorldTopHudUI worldTopHudUI;
    [SerializeField, Range(0,4)] private int revealedEnemyPreviewCount = 0;
    [SerializeField] private string playerDisplayName = "플레이어";

    [Header("Optional Conquest UI")]
    [SerializeField] private GameObject conquestConditionRoot;
    [SerializeField] private Button worldConquestButton;

    private BattlePartyRuntimeState playerPartyRuntimeState;
    private WorldRunTransientState currentWorldRunState;

    public BattlePartyRuntimeState PlayerPartyRuntimeState => playerPartyRuntimeState;
    public PartyDefinition PlayerPartyTemplate => playerPartyTemplate;
    public WorldRunTransientState CurrentWorldRunState => currentWorldRunState;
    public int PersistentSoul => persistentSoul;
    public int PersistentCash => persistentCash;
    public int RevealedEnemyPreviewCount => Mathf.Clamp(revealedEnemyPreviewCount, 0, 4);
    public string PlayerDisplayName => string.IsNullOrWhiteSpace(playerDisplayName) ? "플레이어" : playerDisplayName;

    public WorldMapData MapData { get; private set; }
    public WorldTileData CurrentTile { get; private set; }
    public WorldTileData SelectedTile { get; private set; }
    public WorldGenerationSettings Settings => generationSettings;
    public bool IsBusy => eventController != null && eventController.IsBusy;

    public event Action OnWorldStateChanged;
    public event Action OnStorageChanged;
    public event Action<WorldTileData> OnTileSelectionChanged;
    public event Action<WorldTileData> OnCurrentTileChanged;

    private WorldRevealController revealController;
    private WorldMovementController movementController;
    private WorldTileData previousTileBeforeArrival;

    private string runtimeDifficultyId = "normal";
    public string RuntimeDifficultyId => runtimeDifficultyId;
    private void Awake()
    {
        if (worldConquestButton != null)
        {
            worldConquestButton.onClick.RemoveAllListeners();
            worldConquestButton.onClick.AddListener(HandleWorldConquestButtonPressed);
        }

        if (saveCoordinator == null)
            saveCoordinator = UnityEngine.Object.FindFirstObjectByType<SaveCoordinator>();
    }

    private void Start()
    {
        GetOrCreatePlayerPartyRuntimeState();

        if (saveCoordinator == null)
            saveCoordinator = UnityEngine.Object.FindFirstObjectByType<SaveCoordinator>();

        saveCoordinator?.LoadProfileIntoCurrentScene();

        if (generateOnStart)
            GenerateNewWorld();
    }

    private void RequestAutoSaveAll()
    {
        saveCoordinator?.SaveAll();
    }

    public void GenerateNewWorld()
    {
        ResetWorldRunStateForNewWorld();

        HexWorldGenerator generator = new HexWorldGenerator(generationSettings);
        MapData = generator.Generate();
        if (MapData == null)
            return;

        revealController = new WorldRevealController(MapData);
        movementController = new WorldMovementController(MapData);

        CurrentTile = MapData.GetStartTile();
        SelectedTile = null;
        revealController.RevealAround(CurrentTile);

        if (selectedTileInfoPanel != null)
        {
            selectedTileInfoPanel.Initialize(this, generationSettings);
            selectedTileInfoPanel.HidePanel();
        }

        if (eventController != null)
            eventController.Initialize(this, generationSettings);

        if (questController == null)
            questController = UnityEngine.Object.FindFirstObjectByType<WorldQuestController>();

        if (worldMapUI != null)
            worldMapUI.Initialize(this, MapData, generationSettings);

        if (worldTopHudUI != null)
            worldTopHudUI.Initialize(this, generationSettings);

        RefreshConquestButtonState();
        RaiseSelectionChanged();
        RaiseWorldStateChanged();
        OnCurrentTileChanged?.Invoke(CurrentTile);
        RequestAutoSaveAll();
    }

    public void HandleTileClicked(int tileId)
    {
        if (IsBusy || MapData == null)
            return;

        WorldTileData tile = MapData.GetTileById(tileId);
        HandleTileClicked(tile);
    }

    public void HandleTileClicked(WorldTileData tile)
    {
        if (IsBusy || tile == null || CurrentTile == null || movementController == null)
            return;

        if (tile.tileId == CurrentTile.tileId)
        {
            ClearSelection();
            return;
        }

        if (tile.IsPlayerOwned)
        {
            MoveToTileInternal(tile, true);
            return;
        }

        if (SelectedTile != null && SelectedTile.tileId == tile.tileId)
        {
            if (movementController.CanMoveTo(CurrentTile, tile))
            {
                MoveToTileInternal(tile, true);
                return;
            }
        }

        SelectedTile = tile;
        RaiseSelectionChanged();
        RaiseWorldStateChanged();
    }

    public void HandleBackgroundClicked()
    {
        if (IsBusy)
            return;

        ClearSelection();
    }

    public void ClearSelection()
    {
        if (SelectedTile == null)
            return;

        SelectedTile = null;
        RaiseSelectionChanged();
        RaiseWorldStateChanged();
    }

    public bool CanMoveTo(WorldTileData tile)
    {
        return tile != null && movementController != null && movementController.CanMoveTo(CurrentTile, tile);
    }

    public bool TryMoveToSelectedTile()
    {
        if (IsBusy || SelectedTile == null || !CanMoveTo(SelectedTile))
            return false;

        MoveToTileInternal(SelectedTile, true);
        return true;
    }

    public bool IsCurrentTile(WorldTileData tile) => tile != null && CurrentTile != null && tile.tileId == CurrentTile.tileId;
    public bool IsSelectedTile(WorldTileData tile) => tile != null && SelectedTile != null && tile.tileId == SelectedTile.tileId;
    public bool IsAdjacentReachable(WorldTileData tile) => tile != null && CurrentTile != null && movementController != null && movementController.IsAdjacentReachable(CurrentTile, tile);

    public void ResolveMapEvent(WorldTileData tile, bool conquerTile, bool markResolved, bool disableIcon)
    {
        if (tile == null)
            return;

        tile.revealed = true;

        if (conquerTile)
        {
            tile.currentOwner = FactionType.Player;
            questController?.NotifyTileCaptured(tile);
        }

        tile.isResolved = markResolved;
        tile.isIconDisabled = disableIcon;

        RefreshConquestButtonState();
        RaiseWorldStateChanged();
        RequestAutoSaveAll();
    }

    public void ResolveCombatVictory(WorldTileData tile)
    {
        if (tile == null)
            return;

        ResolveMapEvent(tile, true, true, true);

        if (tile.eventType == WorldTileEventType.EliteBattle)
            questController?.NotifyEliteBattleWon();
        else if (tile.eventType == WorldTileEventType.Boss)
            questController?.NotifyBossBattleWon();

        questController?.TryShowQueuedCompletionPopup();
        FocusCurrentTile();
        RequestAutoSaveAll();
    }

    public void ResolveCombatDefeat(WorldTileData tile, bool returnToStartTile)
    {
        if (returnToStartTile && MapData != null)
        {
            WorldTileData startTile = MapData.GetStartTile();
            if (startTile != null)
                MoveToTileInternal(startTile, false);
        }

        TryRestoreAdjacentFactionTileAsBattle(tile);
        RefreshConquestButtonState();
        RaiseWorldStateChanged();
    }

    public void FocusCurrentTile()
    {
        if (worldMapUI != null)
            worldMapUI.FocusOnCurrentTile(true);
    }

    public BattlePartyRuntimeState GetOrCreatePlayerPartyRuntimeState()
    {
        if (playerPartyRuntimeState == null && playerPartyTemplate != null)
            playerPartyRuntimeState = playerPartyTemplate.CreateRuntimeState();
        return playerPartyRuntimeState;
    }

    public WorldRunTransientState GetOrCreateWorldRunState()
    {
        if (currentWorldRunState == null)
            currentWorldRunState = WorldRunTransientState.CreateForNewWorld(playerPartyTemplate);
        return currentWorldRunState;
    }

    public List<InventoryStackData> GetActiveWorldInventory()
    {
        WorldRunTransientState state = GetOrCreateWorldRunState();
        return state != null ? state.inventory : null;
    }

    public void AddPersistentSoul(int amount)
    {
        persistentSoul += Mathf.Max(0, amount);
        RaiseStorageChanged();
        RequestAutoSaveAll();
    }

    public void AddPersistentCash(int amount)
    {
        persistentCash += Mathf.Max(0, amount);
        RaiseStorageChanged();
    }

    public void SetRevealedEnemyPreviewCount(int count)
    {
        revealedEnemyPreviewCount = Mathf.Clamp(count, 0, 4);
        RaiseWorldStateChanged();
    }


    public void AddWorldSoul(int amount)
    {
        amount = Mathf.Max(0, amount);
        persistentSoul += amount;
        GetOrCreateWorldRunState()?.AddSoulEarnedInWorld(amount);
        RequestAutoSaveAll();
    }

    public void AddCapturedPrisoners(IReadOnlyList<UnitDefinition> units)
    {
        WorldRunTransientState state = GetOrCreateWorldRunState();
        if (state == null || units == null)
            return;

        for (int i = 0; i < units.Count; i++)
            state.AddPrisoner(units[i]);

        RaiseStorageChanged();
        RequestAutoSaveAll();
    }

    public void AddLootToWorldInventory(IReadOnlyList<ItemDefinition> items)
    {
        WorldRunTransientState state = GetOrCreateWorldRunState();
        if (state == null || items == null)
            return;

        for (int i = 0; i < items.Count; i++)
            state.AddItem(items[i], 1);

        RaiseStorageChanged();
        RequestAutoSaveAll();
    }

    public WorldSettlementSummary BuildSettlementSummary(bool wasVictory)
    {
        WorldRunTransientState state = GetOrCreateWorldRunState();
        WorldSettlementSummary summary = new WorldSettlementSummary();
        summary.wasVictory = wasVictory;
        summary.worldEarnedSoulAlreadyGranted = state != null ? state.worldEarnedSoulAlreadyGranted : 0;
        summary.sizeBonusPercent = generationSettings != null ? generationSettings.GetSizeBonusPercent() : 0;
        summary.difficultyBonusPercent = generationSettings != null ? generationSettings.GetDifficultyBonusPercent() : 0;
        summary.victoryBonusPercent = wasVictory && generationSettings != null ? generationSettings.worldVictoryBonusPercent : 0;

        if (state != null)
        {
            if (state.inventory != null)
            {
                for (int i = 0; i < state.inventory.Count; i++)
                {
                    InventoryStackData stack = state.inventory[i];
                    if (stack == null || stack.item == null || stack.amount <= 0)
                        continue;

                    for (int j = 0; j < stack.amount; j++)
                        summary.inventoryItems.Add(stack.item);

                    summary.convertedItemSoul += Mathf.Max(0, stack.item.baseSoulValue) * Mathf.Max(0, stack.amount);
                }
            }

            if (state.prisoners != null)
            {
                for (int i = 0; i < state.prisoners.Count; i++)
                {
                    PrisonerRuntimeData prisoner = state.prisoners[i];
                    if (prisoner == null || prisoner.sourceUnit == null)
                        continue;

                    summary.prisonerUnits.Add(prisoner.sourceUnit);
                    summary.convertedPrisonerSoul += Mathf.Max(0, prisoner.sourceUnit.baseSoulReward);
                }
            }
        }

        int convertedBase = summary.convertedItemSoul + summary.convertedPrisonerSoul;
        int additivePercent = summary.sizeBonusPercent + summary.difficultyBonusPercent + summary.victoryBonusPercent;
        int convertedWithBonus = convertedBase + Mathf.RoundToInt(convertedBase * (additivePercent / 100f));
        summary.totalSettlementSoulAward = summary.worldEarnedSoulAlreadyGranted + convertedWithBonus;
        return summary;
    }

    public void FinalizeWorldSettlement(WorldSettlementSummary summary)
    {
        if (summary == null)
            return;

        int conversionOnly = Mathf.Max(0, summary.totalSettlementSoulAward - summary.worldEarnedSoulAlreadyGranted);
        persistentSoul += conversionOnly;
        ResetWorldRunStateForNewWorld();
        RequestAutoSaveAll();
    }

    public bool TryRestoreAdjacentFactionTileAsBattle(WorldTileData failedTile)
    {
        if (failedTile == null || MapData == null)
            return false;

        List<WorldTileData> neighbors = MapData.GetNeighbors(failedTile);
        List<WorldTileData> candidates = new List<WorldTileData>();
        for (int i = 0; i < neighbors.Count; i++)
        {
            WorldTileData tile = neighbors[i];
            if (tile == null)
                continue;
            if (tile.currentOwner != FactionType.Player)
                continue;
            if (tile.nativeFaction != failedTile.nativeFaction)
                continue;
            if (tile.isPlayerStart)
                continue;
            candidates.Add(tile);
        }

        if (candidates.Count <= 0)
            return false;

        WorldTileData restored = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        restored.currentOwner = restored.nativeFaction;
        restored.eventType = WorldTileEventType.Battle;
        restored.isResolved = false;
        restored.isIconDisabled = false;
        restored.revealed = true;
        return true;
    }

    public bool IsWorldConquestAvailable()
    {
        if (MapData == null || generationSettings == null)
            return false;

        int nonStartTiles = 0;
        int conquered = 0;
        bool allBossesConquered = true;
        for (int i = 0; i < MapData.tiles.Count; i++)
        {
            WorldTileData tile = MapData.tiles[i];
            if (tile == null || tile.isPlayerStart)
                continue;

            nonStartTiles++;
            if (tile.currentOwner == FactionType.Player)
                conquered++;

            if (tile.eventType == WorldTileEventType.Boss && tile.currentOwner != FactionType.Player)
                allBossesConquered = false;
        }

        if (!allBossesConquered || nonStartTiles <= 0)
            return false;

        float percent = conquered / (float)nonStartTiles * 100f;
        return percent >= generationSettings.GetConquestRequiredPercent();
    }

    public void HandleWorldConquestButtonPressed()
    {
        eventController?.OpenWorldSettlementFromMap();
    }

    public void RefreshConquestButtonState()
    {
        if (conquestConditionRoot != null)
            conquestConditionRoot.SetActive(true);
        if (worldConquestButton != null)
            worldConquestButton.gameObject.SetActive(IsWorldConquestAvailable());
    }

    public void ResetWorldRunStateForNewWorld()
    {
        if (currentWorldRunState == null)
            currentWorldRunState = WorldRunTransientState.CreateForNewWorld(playerPartyTemplate);
        else
            currentWorldRunState.ResetForNewWorld(playerPartyTemplate);

        RefreshConquestButtonState();
        RaiseStorageChanged();
        RequestAutoSaveAll();
    }

    public IReadOnlyList<InventoryStackData> GetStorageInventory()
    {
        return GetOrCreateWorldRunState()?.inventory;
    }

    public IReadOnlyList<PrisonerRuntimeData> GetStoragePrisoners()
    {
        return GetOrCreateWorldRunState()?.prisoners;
    }

    public ItemDefinition GetSharedConsumableItem()
    {
        return GetOrCreateWorldRunState()?.sharedConsumableItem;
    }

    public bool IsSharedConsumableAssigned(ItemDefinition item)
    {
        if (item == null)
            return false;

        WorldRunTransientState state = GetOrCreateWorldRunState();
        return state != null && state.sharedConsumableItem == item;
    }

    public bool TryAssignSharedConsumable(ItemDefinition item)
    {
        WorldRunTransientState state = GetOrCreateWorldRunState();
        if (state == null)
            return false;

        if (item == null)
        {
            if (state.sharedConsumableItem == null)
                return false;

            state.sharedConsumableItem = null;
            RaiseStorageChanged();
            RequestAutoSaveAll();
            return true;
        }

        if (!item.usableInBattle)
            return false;

        bool canAssign = item.canAssignToSharedConsumableSlot || item.mainUICategory == MainUIItemCategory.Consumable;
        if (!canAssign)
            return false;

        List<InventoryStackData> inventory = state.inventory;
        bool exists = false;
        if (inventory != null)
        {
            for (int i = 0; i < inventory.Count; i++)
            {
                InventoryStackData stack = inventory[i];
                if (stack != null && stack.item == item && stack.amount > 0)
                {
                    exists = true;
                    break;
                }
            }
        }

        if (!exists)
            return false;

        state.sharedConsumableItem = item;
        RaiseStorageChanged();
        RequestAutoSaveAll();
        return true;
    }

    public bool TrySpendPersistentSoul(int amount)
    {
        int clamped = Mathf.Max(0, amount);
        if (clamped <= 0)
            return true;

        if (persistentSoul < clamped)
            return false;

        persistentSoul -= clamped;
        RaiseStorageChanged();
        RequestAutoSaveAll();
        return true;
    }

    public bool TryPaySoulForPrisoner(PrisonerRuntimeData prisoner)
    {
        if (prisoner == null || !prisoner.RequiresSoulPayment)
            return false;

        if (!TrySpendPersistentSoul(prisoner.targetValue))
            return false;

        prisoner.MarkSoulPaid();
        RaiseStorageChanged();
        RequestAutoSaveAll();
        return true;
    }

    public bool RemovePrisoner(PrisonerRuntimeData prisoner)
    {
        WorldRunTransientState state = GetOrCreateWorldRunState();
        if (state == null || prisoner == null || state.prisoners == null)
            return false;

        bool removed = state.prisoners.Remove(prisoner);
        if (removed)
            RaiseStorageChanged();
            RequestAutoSaveAll();

        return removed;
    }

    public IReadOnlyList<PartyMemberData> GetDisplayOrderedPartyMembers()
    {
        BattlePartyRuntimeState party = GetOrCreatePlayerPartyRuntimeState();
        List<PartyMemberData> ordered = new List<PartyMemberData>();

        if (party == null || party.members == null)
            return ordered;

        for (int i = 0; i < party.members.Count; i++)
        {
            PartyMemberData member = party.members[i];
            if (member != null)
                ordered.Add(member);
        }

        ordered.Sort((a, b) => b.startSlotIndex.CompareTo(a.startSlotIndex));
        return ordered;
    }

    public bool TrySwapPartyOrder(PartyMemberData a, PartyMemberData b)
    {
        if (a == null || b == null || a == b)
            return false;

        int temp = a.startSlotIndex;
        a.startSlotIndex = b.startSlotIndex;
        b.startSlotIndex = temp;

        RaiseStorageChanged();
        RequestAutoSaveAll();
        return true;
    }

    public bool IsAnyLoadoutItemAssigned(ItemDefinition item)
    {
        if (item == null)
            return false;

        return IsSharedConsumableAssigned(item) || IsEquipmentAssigned(item);
    }

    public bool IsEquipmentAssigned(ItemDefinition item)
    {
        if (item == null)
            return false;

        WorldRunTransientState state = GetOrCreateWorldRunState();
        if (state == null || state.partyEquipmentAssignments == null)
            return false;

        for (int i = 0; i < state.partyEquipmentAssignments.Count; i++)
        {
            PartyEquipmentAssignmentData data = state.partyEquipmentAssignments[i];
            if (data == null)
                continue;

            if (data.slot0Item == item || data.slot1Item == item)
                return true;
        }

        return false;
    }

    public ItemDefinition GetAssignedEquipmentItem(PartyMemberData member, int slotIndex)
    {
        PartyEquipmentAssignmentData data = GetEquipmentAssignment(member, false);
        if (data == null)
            return null;

        return slotIndex == 0 ? data.slot0Item : data.slot1Item;
    }

    public bool TryAssignEquipmentItem(PartyMemberData member, int slotIndex, ItemDefinition item)
    {
        if (member == null)
            return false;

        slotIndex = Mathf.Clamp(slotIndex, 0, 1);

        PartyEquipmentAssignmentData data = GetEquipmentAssignment(member, true);
        if (data == null)
            return false;

        if (item == null)
        {
            if (slotIndex == 0)
                data.slot0Item = null;
            else
                data.slot1Item = null;

            RaiseStorageChanged();
            RequestAutoSaveAll();
            return true;
        }

        if (item.mainUICategory != MainUIItemCategory.Equipment)
            return false;

        if (!HasInventoryItem(item))
            return false;

        ClearEquipmentReference(item);

        if (slotIndex == 0)
            data.slot0Item = item;
        else
            data.slot1Item = item;

        RaiseStorageChanged();
        RequestAutoSaveAll();
        return true;
    }

    public bool TryMoveOrSwapEquipment(
        PartyMemberData sourceMember,
        int sourceSlotIndex,
        PartyMemberData targetMember,
        int targetSlotIndex)
    {
        if (sourceMember == null || targetMember == null)
            return false;

        sourceSlotIndex = Mathf.Clamp(sourceSlotIndex, 0, 1);
        targetSlotIndex = Mathf.Clamp(targetSlotIndex, 0, 1);

        PartyEquipmentAssignmentData source = GetEquipmentAssignment(sourceMember, true);
        PartyEquipmentAssignmentData target = GetEquipmentAssignment(targetMember, true);
        if (source == null || target == null)
            return false;

        ItemDefinition sourceItem = sourceSlotIndex == 0 ? source.slot0Item : source.slot1Item;
        ItemDefinition targetItem = targetSlotIndex == 0 ? target.slot0Item : target.slot1Item;

        if (sourceSlotIndex == 0)
            source.slot0Item = targetItem;
        else
            source.slot1Item = targetItem;

        if (targetSlotIndex == 0)
            target.slot0Item = sourceItem;
        else
            target.slot1Item = sourceItem;

        RaiseStorageChanged();
        return true;
    }

    public void HandleQuestAcceptedFromPopup()
    {
        if (CurrentTile == null)
            return;

        // 퀘스트 수락 시 현재 퀘스트 타일 점령
        ResolveMapEvent(CurrentTile, true, true, true);
        FocusCurrentTile();
    }

    public void HandleQuestRejectedFromPopup()
    {
        WorldTileData returnTile = previousTileBeforeArrival;

        if (returnTile == null && MapData != null)
            returnTile = MapData.GetStartTile();

        if (returnTile != null && returnTile != CurrentTile)
            MoveToTileInternal(returnTile, false);
        else
            RaiseWorldStateChanged();
    }

    public void StartNewWorldFromSetup(WorldGenerationSettings templateSettings, string difficultyId, int radius)
    {
        if (templateSettings == null)
        {
            Debug.LogError("[WorldRunManager] templateSettings is null.");
            return;
        }

        WorldGenerationSettings runtimeSettings = Instantiate(templateSettings);
        runtimeSettings.radius = Mathf.Clamp(radius, 1, 64);

        generationSettings = runtimeSettings;
        runtimeDifficultyId = string.IsNullOrWhiteSpace(difficultyId) ? "normal" : difficultyId;

        GenerateNewWorld();
    }

    public bool RestoreWorldRunFromSave(
        ActiveWorldRunSaveData saveData,
        WorldGenerationSettings templateSettings,
        SaveReferenceResolver resolver)
    {
        if (saveData == null || !saveData.hasActiveWorld || resolver == null)
            return false;

        if (templateSettings == null)
        {
            Debug.LogError("[WorldRunManager] templateSettings is null.");
            return false;
        }

        WorldGenerationSettings runtimeSettings = Instantiate(templateSettings);
        runtimeSettings.radius = Mathf.Clamp(saveData.mapRadius, 1, 64);

        generationSettings = runtimeSettings;
        runtimeDifficultyId = string.IsNullOrWhiteSpace(saveData.difficultyId) ? "normal" : saveData.difficultyId;

        ResetWorldRunStateForNewWorld();

        MapData = BuildMapDataFromSave(saveData);
        if (MapData == null)
            return false;

        revealController = new WorldRevealController(MapData);
        movementController = new WorldMovementController(MapData);

        RestorePartyRuntimeFromSave(saveData);
        RestoreTransientWorldStateFromSave(saveData, resolver);

        CurrentTile = MapData.GetTileById(saveData.currentTileId);
        if (CurrentTile == null)
            CurrentTile = MapData.GetStartTile();

        SelectedTile = MapData.GetTileById(saveData.selectedTileId);

        if (selectedTileInfoPanel != null)
        {
            selectedTileInfoPanel.Initialize(this, generationSettings);
            if (SelectedTile == null)
                selectedTileInfoPanel.HidePanel();
            else
                selectedTileInfoPanel.ShowTile(SelectedTile);
        }

        if (eventController != null)
            eventController.Initialize(this, generationSettings);

        if (questController == null)
            questController = UnityEngine.Object.FindFirstObjectByType<WorldQuestController>();

        questController?.LoadFromSave(saveData.activeQuests);

        if (worldMapUI != null)
            worldMapUI.Initialize(this, MapData, generationSettings);

        if (worldTopHudUI != null)
            worldTopHudUI.Initialize(this, generationSettings);

        RefreshConquestButtonState();
        RaiseSelectionChanged();
        RaiseWorldStateChanged();
        OnCurrentTileChanged?.Invoke(CurrentTile);

        return true;
    }

    private WorldMapData BuildMapDataFromSave(ActiveWorldRunSaveData saveData)
    {
        if (saveData == null || saveData.tiles == null || saveData.tiles.Count == 0)
            return null;

        WorldMapData map = new WorldMapData();
        map.radius = saveData.mapRadius;

        for (int i = 0; i < saveData.tiles.Count; i++)
        {
            WorldTileSaveData saved = saveData.tiles[i];
            if (saved == null)
                continue;

            WorldTileData tile = new WorldTileData
            {
                tileId = saved.tileId,
                coord = new HexCoord(saved.q, saved.r),
                nativeFaction = (FactionType)saved.nativeFaction,
                currentOwner = (FactionType)saved.currentOwner,
                eventType = (WorldTileEventType)saved.eventType,
                revealed = saved.revealed,
                isPlayerStart = saved.isPlayerStart,
                isResolved = saved.isResolved,
                isIconDisabled = saved.isIconDisabled,
            };

            map.tiles.Add(tile);

            if (tile.isPlayerStart)
                map.startTileId = tile.tileId;
        }

        map.RebuildLookup();
        return map;
    }

    private void RestorePartyRuntimeFromSave(ActiveWorldRunSaveData saveData)
    {
        BattlePartyRuntimeState runtime = GetOrCreatePlayerPartyRuntimeState();
        if (runtime == null || runtime.members == null || saveData == null || saveData.worldPartyMembers == null)
            return;

        Dictionary<string, PartyMemberData> byId = new Dictionary<string, PartyMemberData>();
        for (int i = 0; i < runtime.members.Count; i++)
        {
            PartyMemberData member = runtime.members[i];
            if (member == null || string.IsNullOrWhiteSpace(member.instanceId))
                continue;

            byId[member.instanceId] = member;
        }

        for (int i = 0; i < saveData.worldPartyMembers.Count; i++)
        {
            WorldPartyMemberRuntimeSaveData saved = saveData.worldPartyMembers[i];
            if (saved == null || string.IsNullOrWhiteSpace(saved.unitInstanceId))
                continue;

            if (!byId.TryGetValue(saved.unitInstanceId, out PartyMemberData member))
                continue;

            member.currentLevel = Mathf.Max(1, saved.currentLevel);
            member.persistentCurrentHP = Mathf.Max(0, saved.persistentCurrentHP);
            member.startSlotIndex = Mathf.Clamp(saved.startSlotIndex, 0, 3);
        }
    }

    private void RestoreTransientWorldStateFromSave(ActiveWorldRunSaveData saveData, SaveReferenceResolver resolver)
    {
        WorldRunTransientState state = GetOrCreateWorldRunState();
        if (state == null || saveData == null || resolver == null)
            return;

        state.inventory.Clear();
        state.prisoners.Clear();
        state.sharedConsumableItem = null;
        state.partyEquipmentAssignments.Clear();
        state.worldEarnedSoulAlreadyGranted = 0;
        state.nextPrisonerSequence = 1;

        if (saveData.worldInventory != null)
        {
            for (int i = 0; i < saveData.worldInventory.Count; i++)
            {
                WorldInventoryItemSaveData saved = saveData.worldInventory[i];
                if (saved == null || string.IsNullOrWhiteSpace(saved.itemId) || saved.amount <= 0)
                    continue;

                ItemDefinition item = resolver.FindItemDefinition(saved.itemId);
                if (item == null)
                    continue;

                state.inventory.Add(new InventoryStackData
                {
                    item = item,
                    amount = Mathf.Max(1, saved.amount)
                });
            }
        }

        if (saveData.prisoners != null)
        {
            long maxSequence = 0;

            for (int i = 0; i < saveData.prisoners.Count; i++)
            {
                CapturedPrisonerSaveData saved = saveData.prisoners[i];
                if (saved == null || string.IsNullOrWhiteSpace(saved.sourceUnitId))
                    continue;

                UnitDefinition unit = resolver.FindUnitDefinition(saved.sourceUnitId);
                if (unit == null)
                    continue;

                PrisonerRuntimeData prisoner = new PrisonerRuntimeData
                {
                    prisonerInstanceId = saved.prisonerInstanceId,
                    sourceUnit = unit,
                    prisonerNameOverride = saved.prisonerNameOverride,
                    capturedLevel = Mathf.Max(1, saved.capturedLevel),
                    isExchangeable = saved.isExchangeable,
                    corruptionConditionType = (PrisonerCorruptionConditionType)saved.corruptionConditionType,
                    targetValue = Mathf.Max(1, saved.targetValue),
                    currentValue = Mathf.Max(0, saved.currentValue),
                    captureSequence = saved.captureSequence
                };

                state.prisoners.Add(prisoner);
                if (prisoner.captureSequence > maxSequence)
                    maxSequence = prisoner.captureSequence;
            }

            state.nextPrisonerSequence = maxSequence + 1;
        }

        if (!string.IsNullOrWhiteSpace(saveData.sharedConsumableItemId))
            state.sharedConsumableItem = resolver.FindItemDefinition(saveData.sharedConsumableItemId);

        if (saveData.equipmentAssignments != null)
        {
            Dictionary<string, PartyEquipmentAssignmentData> byUnit = new Dictionary<string, PartyEquipmentAssignmentData>();

            for (int i = 0; i < saveData.equipmentAssignments.Count; i++)
            {
                WorldEquipmentAssignmentSaveData saved = saveData.equipmentAssignments[i];
                if (saved == null || string.IsNullOrWhiteSpace(saved.unitInstanceId) || string.IsNullOrWhiteSpace(saved.itemId))
                    continue;

                ItemDefinition item = resolver.FindItemDefinition(saved.itemId);
                if (item == null)
                    continue;

                if (!byUnit.TryGetValue(saved.unitInstanceId, out PartyEquipmentAssignmentData assign))
                {
                    assign = new PartyEquipmentAssignmentData
                    {
                        memberInstanceId = saved.unitInstanceId
                    };
                    byUnit[saved.unitInstanceId] = assign;
                    state.partyEquipmentAssignments.Add(assign);
                }

                int slotIndex = Mathf.Clamp(saved.slotIndex, 0, 1);
                if (slotIndex == 0)
                    assign.slot0Item = item;
                else
                    assign.slot1Item = item;
            }
        }

        RaiseStorageChanged();
    }
    private bool HasInventoryItem(ItemDefinition item)
    {
        if (item == null)
            return false;

        WorldRunTransientState state = GetOrCreateWorldRunState();
        if (state == null || state.inventory == null)
            return false;

        for (int i = 0; i < state.inventory.Count; i++)
        {
            InventoryStackData stack = state.inventory[i];
            if (stack != null && stack.item == item && stack.amount > 0)
                return true;
        }

        return false;
    }

    private void ClearEquipmentReference(ItemDefinition item)
    {
        if (item == null)
            return;

        WorldRunTransientState state = GetOrCreateWorldRunState();
        if (state == null || state.partyEquipmentAssignments == null)
            return;

        for (int i = 0; i < state.partyEquipmentAssignments.Count; i++)
        {
            PartyEquipmentAssignmentData data = state.partyEquipmentAssignments[i];
            if (data == null)
                continue;

            if (data.slot0Item == item)
                data.slot0Item = null;

            if (data.slot1Item == item)
                data.slot1Item = null;
        }
    }

    private PartyEquipmentAssignmentData GetEquipmentAssignment(PartyMemberData member, bool createIfMissing)
    {
        if (member == null)
            return null;

        WorldRunTransientState state = GetOrCreateWorldRunState();
        if (state == null)
            return null;

        if (state.partyEquipmentAssignments == null)
            state.partyEquipmentAssignments = new List<PartyEquipmentAssignmentData>();

        string key = EnsureMemberInstanceId(member);

        for (int i = 0; i < state.partyEquipmentAssignments.Count; i++)
        {
            PartyEquipmentAssignmentData data = state.partyEquipmentAssignments[i];
            if (data != null && data.memberInstanceId == key)
                return data;
        }

        if (!createIfMissing)
            return null;

        PartyEquipmentAssignmentData created = new PartyEquipmentAssignmentData
        {
            memberInstanceId = key
        };
        state.partyEquipmentAssignments.Add(created);
        return created;
    }

    private string EnsureMemberInstanceId(PartyMemberData member)
    {
        if (member == null)
            return string.Empty;

        if (string.IsNullOrWhiteSpace(member.instanceId))
            member.instanceId = Guid.NewGuid().ToString("N");

        return member.instanceId;
    }
    private void RaiseStorageChanged()
    {
        OnStorageChanged?.Invoke();
    }

    private void MoveToTileInternal(WorldTileData tile, bool triggerArrivalEvent)
    {
        if (tile == null || !CanMoveTo(tile))
            return;

        WorldTileData previousTile = CurrentTile;
        if (triggerArrivalEvent)
            previousTileBeforeArrival = previousTile;

        CurrentTile = tile;
        SelectedTile = null;
        revealController?.RevealAround(tile);

        if (selectedTileInfoPanel != null)
            selectedTileInfoPanel.HidePanel();

        if (worldMapUI != null)
            worldMapUI.NotifyMovedToTile(tile);

        OnCurrentTileChanged?.Invoke(CurrentTile);
        RaiseSelectionChanged();
        RaiseWorldStateChanged();
        RequestAutoSaveAll();

        if (triggerArrivalEvent && eventController != null)
            eventController.TryHandleArrival(tile);
    }

    private void RaiseWorldStateChanged()
    {
        OnWorldStateChanged?.Invoke();
        RefreshConquestButtonState();

        if (worldMapUI != null && MapData != null)
            worldMapUI.RefreshAll(MapData);

        if (worldTopHudUI != null)
            worldTopHudUI.Refresh();

        questController?.TryShowQueuedCompletionPopup();
    }

    private void RaiseSelectionChanged()
    {
        OnTileSelectionChanged?.Invoke(SelectedTile);

        if (selectedTileInfoPanel == null)
            return;

        if (SelectedTile == null)
            selectedTileInfoPanel.HidePanel();
        else
            selectedTileInfoPanel.ShowTile(SelectedTile);
    }
}
