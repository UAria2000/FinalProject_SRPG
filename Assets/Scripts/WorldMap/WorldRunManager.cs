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
    [SerializeField] private PersistentProfileController persistentProfileController;

    [Header("Startup")]
    [SerializeField] private bool generateOnStart = true;

    [Header("Player Party")]
    [SerializeField] private PartyDefinition playerPartyTemplate;

    [Header("Prisoner Conversion")]
    [SerializeField] private SkillLearnPoolTable convertedUnitSkillPoolTable;
    [SerializeField, Min(0)] private int convertedUnitRandomSkillCount = 3;

    [Header("Mana Crystal")]
    [SerializeField, Min(0)] private int captureManaCost = 10;
    [SerializeField, Min(0)] private int fleeManaCost = 15;
    [SerializeField, Min(0)] private int preventDeathManaCost = 20;
    [SerializeField, Min(0)] private int teamBuffManaCost = 25;
    [SerializeField, Min(0)] private int teamBuffAllStatsPercent = 10;
    [SerializeField, Min(1)] private int teamBuffDurationTurns = 2;

    [Header("Persistent Currencies")]
    [SerializeField] private int persistentSoul;
    [SerializeField] private int persistentCash;

    [Header("Experience Rewards")]
    [Tooltip("월드 결산 경험치: 점령 타일 1개당 기본 EXP.")]
    [Min(0)] [SerializeField] private int settlementExpPerConqueredTile = 20;
    [Tooltip("월드 결산 경험치: 사라지는 아이템의 환산 소울 대비 EXP 비율.")]
    [Min(0f)] [SerializeField] private float settlementExpPercentOfConvertedItemSoul = 25f;
    [Tooltip("월드 결산 경험치: 사라지는 포로의 환산 소울 대비 EXP 비율.")]
    [Min(0f)] [SerializeField] private float settlementExpPercentOfConvertedPrisonerSoul = 25f;

    [Header("World HUD")]
    [SerializeField] private WorldTopHudUI worldTopHudUI;
    [SerializeField, Range(0,6)] private int revealedEnemyPreviewCount = 4;
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
    public int CurrentMana => GetOrCreateWorldRunState() != null ? Mathf.Max(0, GetOrCreateWorldRunState().currentMana) : 0;
    public int MaxMana => GetOrCreateWorldRunState() != null ? Mathf.Max(0, GetOrCreateWorldRunState().maxMana) : 0;
    public int TeamBuffAllStatsPercent => Mathf.Max(0, teamBuffAllStatsPercent);
    public int TeamBuffDurationTurns => Mathf.Max(1, teamBuffDurationTurns);
    public int RevealedEnemyPreviewCount => Mathf.Clamp(revealedEnemyPreviewCount, 0, 6);
    public string PlayerDisplayName => string.IsNullOrWhiteSpace(playerDisplayName) ? "플레이어" : playerDisplayName;

    public WorldMapData MapData { get; private set; }
    public WorldTileData CurrentTile { get; private set; }
    public WorldTileData SelectedTile { get; private set; }
    public WorldGenerationSettings Settings => generationSettings;
    public bool IsBusy => eventController != null && eventController.IsBusy;

    public event Action OnWorldStateChanged;
    public event Action OnStorageChanged;
    public event Action OnManaChanged;
    public event Action<WorldTileData> OnTileSelectionChanged;
    public event Action<WorldTileData> OnCurrentTileChanged;

    private WorldRevealController revealController;
    private WorldMovementController movementController;
    private WorldTileData previousTileBeforeArrival;

    private string runtimeDifficultyId = "normal";
    public string RuntimeDifficultyId => runtimeDifficultyId;

    private int worldStartMainCharacterLevel = 0;
    public int WorldStartMainCharacterLevel => Mathf.Max(1, worldStartMainCharacterLevel > 0 ? worldStartMainCharacterLevel : ResolveCurrentMainCharacterLevel());
    private void Awake()
    {
        if (revealedEnemyPreviewCount <= 0)
            revealedEnemyPreviewCount = 4;

        if (worldConquestButton != null)
        {
            worldConquestButton.onClick.RemoveAllListeners();
            worldConquestButton.onClick.AddListener(HandleWorldConquestButtonPressed);
        }

        if (saveCoordinator == null)
            saveCoordinator = UnityEngine.Object.FindFirstObjectByType<SaveCoordinator>();

        if (persistentProfileController == null)
            persistentProfileController = UnityEngine.Object.FindFirstObjectByType<PersistentProfileController>();
    }

    private void Start()
    {
        GetOrCreatePlayerPartyRuntimeState();

        if (saveCoordinator == null)
            saveCoordinator = UnityEngine.Object.FindFirstObjectByType<SaveCoordinator>();

        if (persistentProfileController == null)
            persistentProfileController = UnityEngine.Object.FindFirstObjectByType<PersistentProfileController>();

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
        RestoreRosterUnitsForNewWorld();
        ResetWorldRunStateForNewWorld();
        CaptureWorldStartEnemyScalingLevel();

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

    public void ResolveCombatVictory(WorldTileData tile, bool showQueuedQuestCompletionPopup = true)
    {
        if (tile == null)
            return;

        bool wasAlreadyResolvedByPlayer = tile.IsPlayerOwned && tile.isResolved;

        ResolveMapEvent(tile, true, true, true);

        if (!wasAlreadyResolvedByPlayer)
        {
            if (tile.eventType == WorldTileEventType.EliteBattle)
                questController?.NotifyEliteBattleWon();
            else if (tile.eventType == WorldTileEventType.Boss)
                questController?.NotifyBossBattleWon();
        }

        if (showQueuedQuestCompletionPopup)
            questController?.TryShowQueuedCompletionPopup();

        FocusCurrentTile();
        RequestAutoSaveAll();
    }

    public void TryShowQueuedQuestCompletionPopup()
    {
        questController?.TryShowQueuedCompletionPopup();
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

    public int GetManaActionCost(BattleManaActionType actionType)
    {
        switch (actionType)
        {
            case BattleManaActionType.Capture: return Mathf.Max(0, captureManaCost);
            case BattleManaActionType.Flee: return Mathf.Max(0, fleeManaCost);
            case BattleManaActionType.PreventDeath: return Mathf.Max(0, preventDeathManaCost);
            case BattleManaActionType.TeamBuff: return Mathf.Max(0, teamBuffManaCost);
            default: return 0;
        }
    }

    public bool HasManaForAction(BattleManaActionType actionType)
    {
        return CurrentMana >= GetManaActionCost(actionType);
    }

    public bool TrySpendMana(BattleManaActionType actionType)
    {
        int cost = GetManaActionCost(actionType);
        if (cost <= 0)
            return true;

        WorldRunTransientState state = GetOrCreateWorldRunState();
        if (state == null || state.currentMana < cost)
            return false;

        state.currentMana = Mathf.Max(0, state.currentMana - cost);
        RaiseManaChanged();
        // 전투 중 마나 소모는 전투 결과 확정 저장에 포함된다.
        // 여기서 즉시 저장하면 전투는 롤백되는데 마나만 소모되는 강제 종료 문제가 생긴다.
        return true;
    }

    public void AddMana(int amount)
    {
        if (amount <= 0)
            return;

        WorldRunTransientState state = GetOrCreateWorldRunState();
        if (state == null)
            return;

        state.currentMana = Mathf.Clamp(state.currentMana + amount, 0, Mathf.Max(0, state.maxMana));
        RaiseManaChanged();
        RequestAutoSaveAll();
    }

    public void SetManaValues(int current, int max)
    {
        WorldRunTransientState state = GetOrCreateWorldRunState();
        if (state == null)
            return;

        state.maxMana = Mathf.Max(0, max);
        state.currentMana = state.maxMana > 0 ? Mathf.Clamp(current, 0, state.maxMana) : Mathf.Max(0, current);
        RaiseManaChanged();
        RequestAutoSaveAll();
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
        revealedEnemyPreviewCount = Mathf.Clamp(count, 0, 6);
        RaiseWorldStateChanged();
    }


    public void AddWorldSoul(int amount)
    {
        amount = Mathf.Max(0, amount);
        persistentSoul += amount;
        GetOrCreateWorldRunState()?.AddSoulEarnedInWorld(amount);
        RequestAutoSaveAll();
    }

    public int AddPartyExperienceToAllMembers(int amount)
    {
        if (persistentProfileController == null)
            persistentProfileController = UnityEngine.Object.FindFirstObjectByType<PersistentProfileController>();

        int granted = persistentProfileController != null
            ? persistentProfileController.AddExperienceToActivePartyMembers(Mathf.Max(0, amount))
            : 0;

        if (granted > 0)
        {
            RaiseStorageChanged();
            RequestAutoSaveAll();
        }

        return granted;
    }

    public int GrantPartyExperienceReward(int amount) => AddPartyExperienceToAllMembers(amount);
    public int AddPartyExperienceReward(int amount) => AddPartyExperienceToAllMembers(amount);
    public int GainPartyExperience(int amount) => AddPartyExperienceToAllMembers(amount);
    public int AddPartyExp(int amount) => AddPartyExperienceToAllMembers(amount);


    public int GetMainCharacterLevelForEnemyScaling()
    {
        if (worldStartMainCharacterLevel > 0)
            return Mathf.Max(1, worldStartMainCharacterLevel);

        return Mathf.Max(1, ResolveCurrentMainCharacterLevel());
    }

    public void CaptureWorldStartEnemyScalingLevel()
    {
        worldStartMainCharacterLevel = Mathf.Max(1, ResolveCurrentMainCharacterLevel());
    }

    private int ResolveCurrentMainCharacterLevel()
    {
        if (persistentProfileController == null)
            persistentProfileController = UnityEngine.Object.FindFirstObjectByType<PersistentProfileController>();

        int found = 0;
        BattlePartyRuntimeState runtime = GetOrCreatePlayerPartyRuntimeState();
        if (runtime != null && runtime.members != null)
        {
            for (int i = 0; i < runtime.members.Count; i++)
            {
                PartyMemberData member = runtime.members[i];
                if (member == null || member.unitDefinition == null)
                    continue;

                if (member.unitDefinition.isMainPlayerCharacter)
                    found = Mathf.Max(found, member.currentLevel);
            }
        }

        if (found > 0)
            return Mathf.Max(1, found);

        if (persistentProfileController != null)
        {
            IReadOnlyList<PersistentRosterUnitData> roster = persistentProfileController.GetRosterUnits();
            if (roster != null)
            {
                for (int i = 0; i < roster.Count; i++)
                {
                    PersistentRosterUnitData unit = roster[i];
                    if (unit == null || unit.unitDefinition == null)
                        continue;

                    if (unit.unitDefinition.isMainPlayerCharacter)
                        found = Mathf.Max(found, unit.currentLevel);
                }
            }
        }

        if (found > 0)
            return Mathf.Max(1, found);

        if (runtime != null && runtime.members != null)
        {
            for (int i = 0; i < runtime.members.Count; i++)
            {
                PartyMemberData member = runtime.members[i];
                if (member != null)
                    found = Mathf.Max(found, member.currentLevel);
            }
        }

        return Mathf.Max(1, found > 0 ? found : 1);
    }

    public int CountLivingActivePartyMembers()
    {
        BattlePartyRuntimeState runtime = GetOrCreatePlayerPartyRuntimeState();
        if (runtime == null || runtime.members == null)
            return 0;

        int count = 0;
        for (int i = 0; i < runtime.members.Count; i++)
        {
            PartyMemberData member = runtime.members[i];
            if (member == null)
                continue;

            if (member.persistentCurrentHP == 0)
                continue;

            count++;
        }

        return count;
    }

    public List<PersistentRosterUnitData> ConvertCapturedPrisonerRewardsToRoster(IReadOnlyList<CapturedPrisonerRewardEntry> prisonerRewards)
    {
        List<PersistentRosterUnitData> created = new List<PersistentRosterUnitData>();
        if (prisonerRewards == null || prisonerRewards.Count == 0)
            return created;

        if (persistentProfileController == null)
            persistentProfileController = UnityEngine.Object.FindFirstObjectByType<PersistentProfileController>();

        if (persistentProfileController == null)
            return created;

        for (int i = 0; i < prisonerRewards.Count; i++)
        {
            CapturedPrisonerRewardEntry reward = prisonerRewards[i];
            if (reward == null)
                continue;

            UnitDefinition convertedUnit = GetConvertedUnitDefinition(reward);
            if (convertedUnit == null)
                continue;

            UnitViewDefinition convertedView = GetConvertedUnitViewDefinition(reward);

            PersistentRosterUnitData rosterUnit = new PersistentRosterUnitData
            {
                instanceId = Guid.NewGuid().ToString("N"),
                instanceDisplayNameOverride = string.Empty,
                fixedEpitaph = string.Empty,
                unitDefinition = convertedUnit,
                unitViewDefinition = convertedView,
                isExchangeable = reward.isExchangeable,
                isConvertedFromPrisoner = true,
                isNft = reward.isExchangeable || convertedUnit.isNftUnit,
                currentLevel = 1,
                originalLevel = Mathf.Max(1, reward.capturedLevel),
                currentExp = 0,
                levelGrowthMaxHp = 0,
                levelGrowthDmg = 0,
                promotionRank = 1,
                statVariance = RollConvertedUnitVariance(convertedUnit != null ? convertedUnit.varianceRules : null),
                learnedSkills = RollConvertedUnitLearnedSkills(convertedUnit),
                battleLootDrops = new List<ItemDropDefinition>(),
                persistentCurrentHP = -1
            };

            rosterUnit.EnsureDefaults();
            persistentProfileController.AddRosterUnit(rosterUnit);
            created.Add(rosterUnit);
        }

        if (created.Count > 0)
        {
            RaiseStorageChanged();
            RequestAutoSaveAll();
        }

        return created;
    }

    private UnitDefinition GetConvertedUnitDefinition(CapturedPrisonerRewardEntry reward)
    {
        if (reward == null)
            return null;

        if (reward.prisonerItem != null)
            return reward.prisonerItem.GetConvertedAllyUnitDefinition(reward.fallbackUnit);

        return reward.fallbackUnit;
    }

    private UnitViewDefinition GetConvertedUnitViewDefinition(CapturedPrisonerRewardEntry reward)
    {
        if (reward == null)
            return null;

        if (reward.prisonerItem != null)
            return reward.prisonerItem.GetConvertedAllyUnitViewDefinition(reward.fallbackView);

        return reward.fallbackView;
    }

    private UnitInstanceStatVariance RollConvertedUnitVariance(StatVarianceRules rules)
    {
        UnitInstanceStatVariance variance = new UnitInstanceStatVariance();
        if (rules == null)
            return variance;

        variance.maxHpDelta = RollRangeInclusive(rules.maxHpRange);
        variance.dmgDelta = RollRangeInclusive(rules.dmgRange);
        variance.spdDelta = RollRangeInclusive(rules.spdRange);
        variance.idtDelta = RollRangeInclusive(rules.idtRange);
        variance.hitDelta = RollRangeInclusive(rules.hitRange);
        variance.acDelta = RollRangeInclusive(rules.acRange);
        variance.criDelta = RollRangeInclusive(rules.criRange);
        variance.crdDelta = RollRangeInclusive(rules.crdRange);
        variance.burnResistDelta = RollRangeInclusive(rules.burnResistRange);
        variance.bleedResistDelta = RollRangeInclusive(rules.bleedResistRange);
        variance.stunResistDelta = RollRangeInclusive(rules.stunResistRange);
        variance.frostResistDelta = RollRangeInclusive(rules.frostResistRange);
        variance.blindResistDelta = RollRangeInclusive(rules.blindResistRange);
        return variance;
    }

    private int RollRangeInclusive(Vector2Int range)
    {
        int min = Mathf.Min(range.x, range.y);
        int max = Mathf.Max(range.x, range.y);
        return UnityEngine.Random.Range(min, max + 1);
    }

    private List<SkillDefinition> RollConvertedUnitLearnedSkills(UnitDefinition unitDefinition)
    {
        List<SkillDefinition> result = new List<SkillDefinition>();
        if (unitDefinition == null)
            return result;

        int maxSkills = Mathf.Max(0, convertedUnitRandomSkillCount);
        if (maxSkills <= 0)
            return result;

        // 포획/타락으로 생성되는 아군은 기존 적 스킬을 복사하지 않고
        // 역할군별 아군 스킬 풀에서만 무작위 3개를 배정한다.
        if (convertedUnitSkillPoolTable != null && !HasSkillFromPool(result, convertedUnitSkillPoolTable.GetClassSkills(unitDefinition.rangeType)))
        {
            List<SkillDefinition> classCandidates = new List<SkillDefinition>();
            AddCandidateSkills(classCandidates, convertedUnitSkillPoolTable.GetClassSkills(unitDefinition.rangeType), result);
            if (classCandidates.Count > 0 && result.Count < maxSkills)
            {
                int classIndex = UnityEngine.Random.Range(0, classCandidates.Count);
                AddSkillIfValid(result, classCandidates[classIndex], maxSkills, allowUnique: false);
            }
        }

        List<SkillDefinition> candidates = BuildConvertedUnitRandomSkillCandidates(unitDefinition, result);
        while (result.Count < maxSkills && candidates.Count > 0)
        {
            int index = UnityEngine.Random.Range(0, candidates.Count);
            SkillDefinition picked = candidates[index];
            candidates.RemoveAt(index);
            AddSkillIfValid(result, picked, maxSkills, allowUnique: false);
        }

        return result;
    }

    private void AddFixedStartingSkills(List<SkillDefinition> result, List<SkillDefinition> source, int maxSkills, bool uniqueOnly)
    {
        if (result == null || source == null || result.Count >= maxSkills)
            return;

        for (int i = 0; i < source.Count && result.Count < maxSkills; i++)
        {
            SkillDefinition skill = source[i];
            if (skill == null)
                continue;

            bool isUnique = BattleSkillInfoFormatter.GetSkillClass(skill) == SkillClass.Unique;
            if (uniqueOnly != isUnique)
                continue;

            AddSkillIfValid(result, skill, maxSkills, allowUnique: true);
        }
    }

    private List<SkillDefinition> BuildConvertedUnitRandomSkillCandidates(UnitDefinition unitDefinition, List<SkillDefinition> alreadySelected)
    {
        List<SkillDefinition> candidates = new List<SkillDefinition>();
        if (unitDefinition == null)
            return candidates;

        if (convertedUnitSkillPoolTable != null)
        {
            AddCandidateSkills(candidates, convertedUnitSkillPoolTable.GetClassSkills(unitDefinition.rangeType), alreadySelected);
            AddCandidateSkills(candidates, convertedUnitSkillPoolTable.commonSkills, alreadySelected);
        }

        AddCandidateSkills(candidates, unitDefinition.extraLearnableSkills, alreadySelected);
        return candidates;
    }

    private void AddCandidateSkills(List<SkillDefinition> candidates, IEnumerable<SkillDefinition> source, List<SkillDefinition> alreadySelected)
    {
        if (candidates == null || source == null)
            return;

        foreach (SkillDefinition skill in source)
        {
            if (skill == null || skill.isBasicAttack)
                continue;
            if (BattleSkillInfoFormatter.GetSkillClass(skill) == SkillClass.Unique)
                continue;
            if (ContainsSkill(candidates, skill) || ContainsSkill(alreadySelected, skill))
                continue;

            candidates.Add(skill);
        }
    }

    private bool AddSkillIfValid(List<SkillDefinition> list, SkillDefinition skill, int maxSkills, bool allowUnique)
    {
        if (list == null || skill == null || list.Count >= maxSkills)
            return false;
        if (skill.isBasicAttack)
            return false;
        if (!allowUnique && BattleSkillInfoFormatter.GetSkillClass(skill) == SkillClass.Unique)
            return false;
        if (ContainsSkill(list, skill))
            return false;

        list.Add(skill);
        return true;
    }

    private bool HasSkillFromPool(List<SkillDefinition> list, IEnumerable<SkillDefinition> pool)
    {
        if (list == null || pool == null)
            return false;

        foreach (SkillDefinition skill in pool)
        {
            if (ContainsSkill(list, skill))
                return true;
        }

        return false;
    }

    private bool ContainsSkill(List<SkillDefinition> list, SkillDefinition skill)
    {
        if (list == null || skill == null)
            return false;

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == skill)
                return true;
        }

        return false;
    }

    public WorldRestResult PreviewRestForActiveParty(
        WorldRestHealMode healMode,
        float percentOfMaxHp,
        int flatHealAmount,
        bool canReviveDeadUnits)
    {
        return BuildRestResultForActiveParty(
            healMode,
            percentOfMaxHp,
            flatHealAmount,
            canReviveDeadUnits,
            apply: false);
    }

    public WorldRestResult ApplyRestToActiveParty(
        WorldRestHealMode healMode,
        float percentOfMaxHp,
        int flatHealAmount,
        bool canReviveDeadUnits)
    {
        WorldRestResult result = BuildRestResultForActiveParty(
            healMode,
            percentOfMaxHp,
            flatHealAmount,
            canReviveDeadUnits,
            apply: true);

        if (result != null)
        {
            SyncProfileFromActivePartyRuntime();
            RaiseStorageChanged();
            RaiseWorldStateChanged();
            RequestAutoSaveAll();
        }

        return result;
    }

    private WorldRestResult BuildRestResultForActiveParty(
        WorldRestHealMode healMode,
        float percentOfMaxHp,
        int flatHealAmount,
        bool canReviveDeadUnits,
        bool apply)
    {
        WorldRestResult result = new WorldRestResult();
        BattlePartyRuntimeState runtime = GetOrCreatePlayerPartyRuntimeState();
        if (runtime == null || runtime.members == null)
            return result;

        List<PartyMemberData> ordered = new List<PartyMemberData>();
        for (int i = 0; i < runtime.members.Count; i++)
        {
            PartyMemberData member = runtime.members[i];
            if (member != null)
                ordered.Add(member);
        }

        ordered.Sort((a, b) => a.startSlotIndex.CompareTo(b.startSlotIndex));

        for (int i = 0; i < ordered.Count; i++)
        {
            PartyMemberData member = ordered[i];
            int maxHp = member.GetMaxHP();
            int before = member.GetPersistentCurrentHPOrFull();
            bool wasDead = before <= 0;
            bool skipped = wasDead && !canReviveDeadUnits;
            int after = before;
            int healedAmount = 0;

            if (!skipped)
            {
                if (healMode == WorldRestHealMode.FullHeal)
                {
                    after = maxHp;
                }
                else
                {
                    int heal = CalculateRestHealAmount(healMode, maxHp, percentOfMaxHp, flatHealAmount);
                    after = Mathf.Clamp(before + heal, 0, maxHp);
                }

                healedAmount = Mathf.Max(0, after - before);

                if (apply)
                    member.persistentCurrentHP = after;
            }

            result.AddMember(new WorldRestMemberResult
            {
                displayName = member.GetDisplayName(),
                beforeHP = before,
                afterHP = after,
                maxHP = maxHp,
                healedAmount = healedAmount,
                wasDead = wasDead,
                skipped = skipped,
            });
        }

        return result;
    }

    private int CalculateRestHealAmount(
        WorldRestHealMode healMode,
        int maxHp,
        float percentOfMaxHp,
        int flatHealAmount)
    {
        int safeMaxHp = Mathf.Max(0, maxHp);
        float safePercent = Mathf.Max(0f, percentOfMaxHp);
        int safeFlat = Mathf.Max(0, flatHealAmount);

        switch (healMode)
        {
            case WorldRestHealMode.FullHeal:
                return safeMaxHp;

            case WorldRestHealMode.FlatAmount:
                return safeFlat;

            case WorldRestHealMode.FlatAndPercentOfMaxHp:
                return safeFlat + Mathf.RoundToInt(safeMaxHp * safePercent * 0.01f);

            case WorldRestHealMode.PercentOfMaxHp:
            default:
                return Mathf.RoundToInt(safeMaxHp * safePercent * 0.01f);
        }
    }

    public void SyncProfileFromActivePartyRuntime()
    {
        if (persistentProfileController == null)
            persistentProfileController = UnityEngine.Object.FindFirstObjectByType<PersistentProfileController>();

        persistentProfileController?.SyncFromActivePartyRuntimeAndSave();
    }

    public int RemoveDeadPartyMembersFromActiveParty()
    {
        BattlePartyRuntimeState runtime = GetOrCreatePlayerPartyRuntimeState();
        if (runtime == null || runtime.members == null)
            return 0;

        SyncProfileFromActivePartyRuntime();

        int removed = 0;
        for (int i = runtime.members.Count - 1; i >= 0; i--)
        {
            PartyMemberData member = runtime.members[i];
            if (member == null || member.persistentCurrentHP != 0)
                continue;

            if (member.unitDefinition != null && member.unitDefinition.isMainPlayerCharacter)
                continue;

            ClearEquipmentAssignmentsForMember(member.instanceId);
            runtime.members.RemoveAt(i);
            removed++;
        }

        int movedToGraveyard = 0;
        if (persistentProfileController == null)
            persistentProfileController = UnityEngine.Object.FindFirstObjectByType<PersistentProfileController>();
        if (persistentProfileController != null)
            movedToGraveyard = persistentProfileController.MoveDeadNonMainRosterUnitsToGraveyard();

        if (removed > 0)
            NormalizeRuntimePartySlots(runtime.members);

        if (removed > 0 || movedToGraveyard > 0)
        {
            RaiseStorageChanged();
            RequestAutoSaveAll();
        }

        return Mathf.Max(removed, movedToGraveyard);
    }

    public void RestoreRosterUnitsForNewWorld()
    {
        if (persistentProfileController == null)
            persistentProfileController = UnityEngine.Object.FindFirstObjectByType<PersistentProfileController>();

        BattlePartyRuntimeState runtime = GetOrCreatePlayerPartyRuntimeState();
        List<string> currentPartyIds = new List<string>();
        if (runtime != null && runtime.members != null)
        {
            List<PartyMemberData> ordered = new List<PartyMemberData>();
            for (int i = 0; i < runtime.members.Count; i++)
                if (runtime.members[i] != null)
                    ordered.Add(runtime.members[i]);
            ordered.Sort((a, b) => a.startSlotIndex.CompareTo(b.startSlotIndex));
            for (int i = 0; i < ordered.Count; i++)
                currentPartyIds.Add(ordered[i].instanceId);
        }

        if (persistentProfileController != null)
        {
            persistentProfileController.RestoreRosterUnitsForNewWorld();
            persistentProfileController.RebuildActivePartyFromSavedIds(currentPartyIds);
        }
    }

    private void ClearEquipmentAssignmentsForMember(string memberInstanceId)
    {
        if (string.IsNullOrWhiteSpace(memberInstanceId))
            return;

        WorldRunTransientState state = GetOrCreateWorldRunState();
        if (state == null || state.partyEquipmentAssignments == null)
            return;

        for (int i = state.partyEquipmentAssignments.Count - 1; i >= 0; i--)
        {
            PartyEquipmentAssignmentData data = state.partyEquipmentAssignments[i];
            if (data == null || data.memberInstanceId != memberInstanceId)
                continue;

            ConsumeWorldInventoryItem(data.slot0Item, 1);
            ConsumeWorldInventoryItem(data.slot1Item, 1);
            state.partyEquipmentAssignments.RemoveAt(i);
        }
    }

    private bool ConsumeWorldInventoryItem(ItemDefinition item, int amount)
    {
        if (item == null || amount <= 0)
            return false;

        WorldRunTransientState state = GetOrCreateWorldRunState();
        if (state == null || state.inventory == null)
            return false;

        int remaining = Mathf.Max(1, amount);
        for (int i = state.inventory.Count - 1; i >= 0 && remaining > 0; i--)
        {
            InventoryStackData stack = state.inventory[i];
            if (stack == null || stack.item != item || stack.amount <= 0)
                continue;

            int used = Mathf.Min(stack.amount, remaining);
            stack.amount -= used;
            remaining -= used;

            if (stack.amount <= 0)
                state.inventory.RemoveAt(i);
        }

        return remaining <= 0;
    }

    private void NormalizeRuntimePartySlots(List<PartyMemberData> members)
    {
        if (members == null)
            return;

        members.Sort((a, b) => a.startSlotIndex.CompareTo(b.startSlotIndex));
        for (int i = 0; i < members.Count; i++)
        {
            if (members[i] != null)
                members[i].startSlotIndex = i;
        }
    }

    public void AddCapturedPrisoners(IReadOnlyList<UnitDefinition> units)
    {
        WorldRunTransientState state = GetOrCreateWorldRunState();
        if (state == null || units == null)
            return;

        bool addedAny = false;
        for (int i = 0; i < units.Count; i++)
        {
            if (units[i] == null)
                continue;

            state.AddPrisoner(units[i]);
            addedAny = true;
        }

        if (addedAny)
        {
            RaiseStorageChanged();
            RequestAutoSaveAll();
        }
    }

    public void AddCapturedPrisonerItems(IReadOnlyList<ItemDefinition> prisonerItems)
    {
        WorldRunTransientState state = GetOrCreateWorldRunState();
        if (state == null || prisonerItems == null)
            return;

        bool addedAny = false;
        for (int i = 0; i < prisonerItems.Count; i++)
        {
            ItemDefinition item = prisonerItems[i];
            if (item == null)
                continue;

            state.AddPrisonerFromItem(item, 1, item.GetConvertedAllyUnitDefinition());
            addedAny = true;
        }

        if (addedAny)
        {
            RaiseStorageChanged();
            RequestAutoSaveAll();
        }
    }

    public void AddCapturedPrisonerRewards(IReadOnlyList<CapturedPrisonerRewardEntry> prisonerRewards)
    {
        WorldRunTransientState state = GetOrCreateWorldRunState();
        if (state == null || prisonerRewards == null)
            return;

        bool addedAny = false;
        for (int i = 0; i < prisonerRewards.Count; i++)
        {
            CapturedPrisonerRewardEntry reward = prisonerRewards[i];
            if (reward == null)
                continue;

            if (reward.prisonerItem != null)
            {
                state.AddPrisonerFromItem(
                    reward.prisonerItem,
                    Mathf.Max(1, reward.capturedLevel),
                    reward.fallbackUnit,
                    reward.fallbackView,
                    reward.isExchangeable);
                addedAny = true;
            }
            else if (reward.fallbackUnit != null)
            {
                state.AddPrisoner(reward.fallbackUnit, Mathf.Max(1, reward.capturedLevel), reward.fallbackView, reward.isExchangeable);
                addedAny = true;
            }
        }

        if (addedAny)
        {
            RaiseStorageChanged();
            RequestAutoSaveAll();
        }
    }

    public bool TryAddItemToStorage(ItemDefinition item, int amount)
    {
        return AddStorageItem(item, amount);
    }

    public bool AddItemToStorage(ItemDefinition item, int amount)
    {
        return AddStorageItem(item, amount);
    }

    public bool GrantStorageItem(ItemDefinition item, int amount)
    {
        return AddStorageItem(item, amount);
    }

    public bool AddStorageItem(ItemDefinition item, int amount)
    {
        if (item == null || amount <= 0)
            return false;

        WorldRunTransientState state = GetOrCreateWorldRunState();
        if (state == null)
            return false;

        state.AddItem(item, Mathf.Max(1, amount));
        RaiseStorageChanged();
        RequestAutoSaveAll();
        return true;
    }

    public bool GrantTreasureRewards(WorldTreasureResult treasure)
    {
        if (treasure == null)
            return false;

        bool grantedAny = false;

        if (!treasure.soulGranted && treasure.soulAmount > 0)
        {
            AddWorldSoul(treasure.soulAmount);
            treasure.soulGranted = true;
            grantedAny = true;
        }

        if (treasure.rewards != null)
        {
            for (int i = 0; i < treasure.rewards.Count; i++)
            {
                WorldTreasureRewardItemEntry reward = treasure.rewards[i];
                if (reward == null || reward.item == null || reward.amount <= 0)
                    continue;

                if (AddStorageItem(reward.item, Mathf.Max(1, reward.amount)))
                    grantedAny = true;
            }
        }

        return grantedAny;
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
        summary.conqueredTileCount = CountConqueredPlayerTiles();
        summary.conqueredTileExp = Mathf.Max(0, summary.conqueredTileCount * settlementExpPerConqueredTile);

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

        summary.convertedItemExp = Mathf.RoundToInt(summary.convertedItemSoul * Mathf.Max(0f, settlementExpPercentOfConvertedItemSoul) * 0.01f);
        summary.convertedPrisonerExp = Mathf.RoundToInt(summary.convertedPrisonerSoul * Mathf.Max(0f, settlementExpPercentOfConvertedPrisonerSoul) * 0.01f);
        int expBase = summary.conqueredTileExp + summary.convertedItemExp + summary.convertedPrisonerExp;
        int expBonusPercent = summary.difficultyBonusPercent + summary.victoryBonusPercent;
        summary.totalSettlementExpAward = Mathf.Max(0, expBase + Mathf.RoundToInt(expBase * (expBonusPercent / 100f)));

        return summary;
    }

    private int CountConqueredPlayerTiles()
    {
        if (MapData == null || MapData.tiles == null)
            return 0;

        int count = 0;
        for (int i = 0; i < MapData.tiles.Count; i++)
        {
            WorldTileData tile = MapData.tiles[i];
            if (tile == null || tile.isPlayerStart)
                continue;

            if (tile.currentOwner == FactionType.Player)
                count++;
        }

        return count;
    }

    public void FinalizeWorldSettlement(WorldSettlementSummary summary)
    {
        if (summary == null)
            return;

        int conversionOnly = Mathf.Max(0, summary.totalSettlementSoulAward - summary.worldEarnedSoulAlreadyGranted);
        persistentSoul += conversionOnly;

        if (summary.totalSettlementExpAward > 0)
            AddPartyExperienceToAllMembers(summary.totalSettlementExpAward);

        RemoveDeadPartyMembersFromActiveParty();
        RestoreRosterUnitsForNewWorld();

        if (persistentProfileController == null)
            persistentProfileController = UnityEngine.Object.FindFirstObjectByType<PersistentProfileController>();
        if (persistentProfileController != null)
        {
            persistentProfileController.EnsureInitialized();
            if (persistentProfileController.Profile != null)
                persistentProfileController.Profile.lastWorldSettlementResult = summary.wasVictory ? WorldSettlementResultState.Victory : WorldSettlementResultState.Failure;
        }

        ClearRuntimeWorldAfterSettlement();

        // 결산이 끝난 월드는 승리/패배 여부와 무관하게 더 이상 이어하기 대상이 아니다.
        // 프로필(소울, 경험치, 묘지, 회복된 로스터)만 저장하고 active world save는 삭제한다.
        saveCoordinator?.SaveProfile();
        saveCoordinator?.ClearSavedWorldRun();
        RaiseStorageChanged();
        RaiseWorldStateChanged();
    }

    private void ClearRuntimeWorldAfterSettlement()
    {
        MapData = null;
        CurrentTile = null;
        SelectedTile = null;
        previousTileBeforeArrival = null;
        revealController = null;
        movementController = null;
        currentWorldRunState = WorldRunTransientState.CreateForNewWorld(playerPartyTemplate);

        if (selectedTileInfoPanel != null)
            selectedTileInfoPanel.HidePanel();
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
        int initialMaxMana = CalculateInitialMaxManaForNewWorld();

        if (currentWorldRunState == null)
            currentWorldRunState = WorldRunTransientState.CreateForNewWorld(playerPartyTemplate);

        currentWorldRunState.ResetForNewWorld(playerPartyTemplate, initialMaxMana);

        RefreshConquestButtonState();
        RaiseStorageChanged();
        RaiseManaChanged();
        RequestAutoSaveAll();
    }

    private int CalculateInitialMaxManaForNewWorld()
    {
        if (generationSettings == null)
            return 0;

        WorldSettlementResultState previousResult = WorldSettlementResultState.None;
        if (persistentProfileController == null)
            persistentProfileController = UnityEngine.Object.FindFirstObjectByType<PersistentProfileController>();

        if (persistentProfileController != null)
        {
            persistentProfileController.EnsureInitialized();
            if (persistentProfileController.Profile != null)
                previousResult = persistentProfileController.Profile.lastWorldSettlementResult;
        }

        return generationSettings.CalculateMaxMana(previousResult);
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

    public int GetSharedConsumableAmount()
    {
        WorldRunTransientState state = GetOrCreateWorldRunState();
        ItemDefinition item = state != null ? state.sharedConsumableItem : null;
        if (item == null || state.inventory == null)
            return 0;

        int total = 0;
        for (int i = 0; i < state.inventory.Count; i++)
        {
            InventoryStackData stack = state.inventory[i];
            if (stack == null || stack.item != item || stack.amount <= 0)
                continue;

            total += stack.amount;
        }

        return Mathf.Max(0, total);
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

    public bool TryGrantAndAssignSharedConsumable(ItemDefinition item, int amount)
    {
        WorldRunTransientState state = GetOrCreateWorldRunState();
        if (state == null || item == null || amount <= 0)
            return false;

        if (!item.usableInBattle)
            return false;

        bool canAssign = item.canAssignToSharedConsumableSlot || item.mainUICategory == MainUIItemCategory.Consumable;
        if (!canAssign)
            return false;

        state.AddItem(item, Mathf.Max(1, amount));
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
        {
            RaiseStorageChanged();
            RequestAutoSaveAll();
        }

        return removed;
    }

    public bool TryCorruptReadyPrisoner(PrisonerRuntimeData prisoner)
    {
        WorldRunTransientState state = GetOrCreateWorldRunState();
        if (state == null || prisoner == null || state.prisoners == null)
            return false;

        if (!prisoner.IsReadyToCorrupt || prisoner.sourceUnit == null)
            return false;

        if (persistentProfileController == null)
            persistentProfileController = UnityEngine.Object.FindFirstObjectByType<PersistentProfileController>();

        if (persistentProfileController == null)
            return false;

        PersistentRosterUnitData rosterUnit = new PersistentRosterUnitData
        {
            instanceId = Guid.NewGuid().ToString("N"),
            instanceDisplayNameOverride = string.Empty,
            fixedEpitaph = string.Empty,
            unitDefinition = prisoner.sourceUnit,
            unitViewDefinition = prisoner.sourceUnitViewDefinition,
            isExchangeable = prisoner.isExchangeable,
            isConvertedFromPrisoner = true,
            isNft = prisoner.isExchangeable || (prisoner.sourceUnit != null && prisoner.sourceUnit.isNftUnit),
            currentLevel = 1,
            originalLevel = Mathf.Max(1, prisoner.capturedLevel),
            currentExp = 0,
            levelGrowthMaxHp = 0,
            levelGrowthDmg = 0,
            promotionRank = 1,
            statVariance = RollConvertedUnitVariance(prisoner.sourceUnit != null ? prisoner.sourceUnit.varianceRules : null),
            learnedSkills = RollConvertedUnitLearnedSkills(prisoner.sourceUnit),
            persistentCurrentHP = -1
        };

        rosterUnit.EnsureDefaults();
        persistentProfileController.AddRosterUnit(rosterUnit);

        bool removed = state.prisoners.Remove(prisoner);
        if (!removed)
            return false;

        RaiseStorageChanged();
        RequestAutoSaveAll();
        return true;
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

        int inventoryAmount = GetInventoryItemAmount(item);
        if (inventoryAmount <= 0)
            return false;

        ItemDefinition currentItem = slotIndex == 0 ? data.slot0Item : data.slot1Item;
        if (currentItem != item)
        {
            int assignedExceptTarget = CountAssignedEquipmentItem(item, member, slotIndex);

            // 장비는 아직 개별 인스턴스 ID가 없고 ItemDefinition + 수량으로 관리된다.
            // 같은 장비 보유 수량이 남아 있으면 기존 장착은 유지하고,
            // 남는 수량이 없을 때만 기존 장착 위치 중 1개를 새 슬롯으로 이동시킨다.
            if (assignedExceptTarget >= inventoryAmount)
                ClearFirstEquipmentReference(item, member, slotIndex);
        }

        if (slotIndex == 0)
            data.slot0Item = item;
        else
            data.slot1Item = item;

        RaiseStorageChanged();
        RequestAutoSaveAll();
        return true;
    }

    public bool TryGrantAndAssignEquipmentItem(PartyMemberData member, int slotIndex, ItemDefinition item, int amount)
    {
        if (member == null || item == null || amount <= 0)
            return false;

        if (item.mainUICategory != MainUIItemCategory.Equipment)
            return false;

        WorldRunTransientState state = GetOrCreateWorldRunState();
        if (state == null)
            return false;

        slotIndex = Mathf.Clamp(slotIndex, 0, 1);

        PartyEquipmentAssignmentData data = GetEquipmentAssignment(member, true);
        if (data == null)
            return false;

        // 보상 슬롯에서 직접 장착하는 경우에는 먼저 새 보상 아이템을 월드 인벤토리에 등록한다.
        // 기존 같은 종류 장비를 전부 해제하지 않는다. 그래야 같은 장비 여러 개를 정상적으로 유지할 수 있다.
        state.AddItem(item, Mathf.Max(1, amount));

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
        RequestAutoSaveAll();
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

    public bool ShouldSaveAsInterruptedArrival()
    {
        return IsUnresolvedArrivalTile(CurrentTile);
    }

    public WorldTileData GetSafeCurrentTileForSave()
    {
        if (!ShouldSaveAsInterruptedArrival())
            return CurrentTile;

        if (previousTileBeforeArrival != null && previousTileBeforeArrival.IsPlayerOwned)
            return previousTileBeforeArrival;

        WorldTileData adjacent = FindAdjacentPlayerOwnedTile(CurrentTile);
        if (adjacent != null)
            return adjacent;

        return MapData != null ? MapData.GetStartTile() : CurrentTile;
    }

    public bool ShouldRevealTileForInterruptedSave(WorldTileData tile)
    {
        return ShouldTileBeVisibleFromPlayerTerritory(tile);
    }

    private bool IsUnresolvedArrivalTile(WorldTileData tile)
    {
        if (tile == null || tile.isPlayerStart)
            return false;

        if (tile.IsPlayerOwned)
            return false;

        return tile.ShouldTriggerEventOnArrival;
    }

    private WorldTileData FindAdjacentPlayerOwnedTile(WorldTileData tile)
    {
        if (MapData == null || tile == null)
            return null;

        List<WorldTileData> neighbors = MapData.GetNeighbors(tile);
        for (int i = 0; i < neighbors.Count; i++)
        {
            WorldTileData neighbor = neighbors[i];
            if (neighbor != null && neighbor.IsPlayerOwned)
                return neighbor;
        }

        return null;
    }

    private bool ShouldTileBeVisibleFromPlayerTerritory(WorldTileData tile)
    {
        if (tile == null)
            return false;

        if (tile.IsPlayerOwned || tile.isPlayerStart)
            return true;

        if (MapData == null)
            return tile.revealed;

        List<WorldTileData> neighbors = MapData.GetNeighbors(tile);
        for (int i = 0; i < neighbors.Count; i++)
        {
            WorldTileData neighbor = neighbors[i];
            if (neighbor != null && neighbor.IsPlayerOwned)
                return true;
        }

        return false;
    }

    private void NormalizeRevealedTilesToPlayerFrontier()
    {
        if (MapData == null || MapData.tiles == null)
            return;

        for (int i = 0; i < MapData.tiles.Count; i++)
        {
            WorldTileData tile = MapData.tiles[i];
            if (tile == null)
                continue;

            tile.revealed = ShouldTileBeVisibleFromPlayerTerritory(tile);
        }
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
        worldStartMainCharacterLevel = Mathf.Max(0, saveData.worldStartMainCharacterLevel);

        ResetWorldRunStateForNewWorld();

        MapData = BuildMapDataFromSave(saveData);
        if (MapData == null)
            return false;

        revealController = new WorldRevealController(MapData);
        movementController = new WorldMovementController(MapData);

        RestorePartyRuntimeFromSave(saveData);
        if (worldStartMainCharacterLevel <= 0)
            CaptureWorldStartEnemyScalingLevel();
        RestoreTransientWorldStateFromSave(saveData, resolver);

        CurrentTile = MapData.GetTileById(saveData.currentTileId);
        if (CurrentTile == null)
            CurrentTile = MapData.GetStartTile();

        if (IsUnresolvedArrivalTile(CurrentTile))
        {
            WorldTileData interruptedTile = CurrentTile;
            WorldTileData safeTile = FindAdjacentPlayerOwnedTile(interruptedTile);
            if (safeTile == null)
                safeTile = MapData.GetStartTile();

            CurrentTile = safeTile;
            SelectedTile = null;
            previousTileBeforeArrival = safeTile;
            NormalizeRevealedTilesToPlayerFrontier();
        }
        else
        {
            SelectedTile = MapData.GetTileById(saveData.selectedTileId);
        }

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

            tile.previewEnemyPortraits = RestoreSavedEnemyPreviewPortraits(tile, saved);
            if (tile.IsCombatEvent && tile.previewEnemyPortraits.Count == 0)
                tile.previewEnemyPortraits = BuildEnemyPreviewListForTile(tile);

            map.tiles.Add(tile);

            if (tile.isPlayerStart)
                map.startTileId = tile.tileId;
        }

        map.RebuildLookup();
        return map;
    }


    private List<Sprite> RestoreSavedEnemyPreviewPortraits(WorldTileData tile, WorldTileSaveData saved)
    {
        List<Sprite> result = new List<Sprite>();
        if (tile == null || saved == null || saved.previewEnemyPortraitSpriteNames == null)
            return result;

        for (int i = 0; i < saved.previewEnemyPortraitSpriteNames.Count; i++)
        {
            Sprite sprite = FindEnemyPreviewSpriteByName(tile.nativeFaction, saved.previewEnemyPortraitSpriteNames[i]);
            if (sprite != null)
                result.Add(sprite);
        }

        return result;
    }

    private List<Sprite> BuildEnemyPreviewListForTile(WorldTileData tile)
    {
        List<Sprite> sourcePool = new List<Sprite>();
        if (tile == null || generationSettings == null || !tile.IsCombatEvent)
            return sourcePool;

        bool isBoss = tile.eventType == WorldTileEventType.Boss;
        FactionBattleConfig config = generationSettings.GetFactionBattleConfig(tile.nativeFaction);
        if (config != null)
        {
            if (isBoss)
            {
                AddPreviewSpritesFromPartyDefinition(sourcePool, config.bossPartyDefinition);
                AddPreviewSpritesFromEncounterTable(sourcePool, config.bossEncounterTable);
            }
            else
            {
                AddPreviewSpritesFromEncounterTable(sourcePool, config.battleTier1Table);
                AddPreviewSpritesFromEncounterTable(sourcePool, config.battleTier2Table);
                AddPreviewSpritesFromEncounterTable(sourcePool, config.battleTier3Table);
                AddPreviewSpritesFromEncounterTable(sourcePool, config.eliteTier1Table);
                AddPreviewSpritesFromEncounterTable(sourcePool, config.eliteTier2Table);
                AddPreviewSpritesFromEncounterTable(sourcePool, config.eliteTier3Table);
            }
        }

        if (sourcePool.Count == 0)
        {
            IReadOnlyList<Sprite> fallbackPool = generationSettings.GetFactionEnemyPortraitPool(tile.nativeFaction);
            if (fallbackPool != null)
            {
                for (int i = 0; i < fallbackPool.Count; i++)
                {
                    if (fallbackPool[i] != null)
                        sourcePool.Add(fallbackPool[i]);
                }
            }
        }

        List<Sprite> result = new List<Sprite>();
        if (sourcePool.Count == 0)
            return result;

        int minCount = Mathf.Clamp(generationSettings.enemyPortraitMinCount, 1, 6);
        int maxCount = Mathf.Clamp(generationSettings.enemyPortraitMaxCount, minCount, 6);
        int count = isBoss ? 1 : UnityEngine.Random.Range(minCount, maxCount + 1);

        for (int i = 0; i < count; i++)
        {
            Sprite sprite = sourcePool[UnityEngine.Random.Range(0, sourcePool.Count)];
            if (sprite != null)
                result.Add(sprite);
        }

        return result;
    }

    private Sprite FindEnemyPreviewSpriteByName(FactionType faction, string spriteName)
    {
        if (string.IsNullOrWhiteSpace(spriteName) || generationSettings == null)
            return null;

        FactionBattleConfig config = generationSettings.GetFactionBattleConfig(faction);
        if (config != null)
        {
            Sprite sprite = FindPreviewSpriteInPartyDefinition(config.bossPartyDefinition, spriteName);
            if (sprite != null)
                return sprite;

            sprite = FindPreviewSpriteInEncounterTable(config.bossEncounterTable, spriteName);
            if (sprite != null)
                return sprite;

            sprite = FindPreviewSpriteInEncounterTable(config.battleTier1Table, spriteName);
            if (sprite != null)
                return sprite;

            sprite = FindPreviewSpriteInEncounterTable(config.battleTier2Table, spriteName);
            if (sprite != null)
                return sprite;

            sprite = FindPreviewSpriteInEncounterTable(config.battleTier3Table, spriteName);
            if (sprite != null)
                return sprite;

            sprite = FindPreviewSpriteInEncounterTable(config.eliteTier1Table, spriteName);
            if (sprite != null)
                return sprite;

            sprite = FindPreviewSpriteInEncounterTable(config.eliteTier2Table, spriteName);
            if (sprite != null)
                return sprite;

            sprite = FindPreviewSpriteInEncounterTable(config.eliteTier3Table, spriteName);
            if (sprite != null)
                return sprite;
        }

        IReadOnlyList<Sprite> fallbackPool = generationSettings.GetFactionEnemyPortraitPool(faction);
        if (fallbackPool != null)
        {
            for (int i = 0; i < fallbackPool.Count; i++)
            {
                Sprite sprite = fallbackPool[i];
                if (sprite != null && sprite.name == spriteName)
                    return sprite;
            }
        }

        return null;
    }

    private Sprite FindPreviewSpriteInEncounterTable(EnemyEncounterTable table, string spriteName)
    {
        if (table == null || table.entries == null || string.IsNullOrWhiteSpace(spriteName))
            return null;

        for (int i = 0; i < table.entries.Count; i++)
        {
            EnemyEncounterEntry entry = table.entries[i];
            if (entry == null || entry.unitViewDefinition == null)
                continue;

            Sprite sprite = entry.unitViewDefinition.GetSlotFaceSprite();
            if (sprite != null && sprite.name == spriteName)
                return sprite;
        }

        return null;
    }

    private Sprite FindPreviewSpriteInPartyDefinition(PartyDefinition party, string spriteName)
    {
        if (party == null || party.members == null || string.IsNullOrWhiteSpace(spriteName))
            return null;

        for (int i = 0; i < party.members.Count; i++)
        {
            PartyMemberData member = party.members[i];
            if (member == null || member.unitViewDefinition == null)
                continue;

            Sprite sprite = member.unitViewDefinition.GetSlotFaceSprite();
            if (sprite != null && sprite.name == spriteName)
                return sprite;
        }

        return null;
    }

    private void AddPreviewSpritesFromEncounterTable(List<Sprite> target, EnemyEncounterTable table)
    {
        if (target == null || table == null || table.entries == null)
            return;

        for (int i = 0; i < table.entries.Count; i++)
        {
            EnemyEncounterEntry entry = table.entries[i];
            if (entry == null || !entry.enabled || entry.unitViewDefinition == null)
                continue;

            Sprite sprite = entry.unitViewDefinition.GetSlotFaceSprite();
            if (sprite == null)
                continue;

            int repeat = Mathf.Max(1, entry.weight);
            for (int r = 0; r < repeat; r++)
                target.Add(sprite);
        }
    }

    private void AddPreviewSpritesFromPartyDefinition(List<Sprite> target, PartyDefinition party)
    {
        if (target == null || party == null || party.members == null)
            return;

        for (int i = 0; i < party.members.Count; i++)
        {
            PartyMemberData member = party.members[i];
            if (member == null || member.unitViewDefinition == null)
                continue;

            Sprite sprite = member.unitViewDefinition.GetSlotFaceSprite();
            if (sprite != null)
                target.Add(sprite);
        }
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
            member.currentExp = Mathf.Max(0, saved.currentExp);
            member.levelGrowthMaxHp = Mathf.Max(0, saved.levelGrowthMaxHp);
            member.levelGrowthDmg = Mathf.Max(0, saved.levelGrowthDmg);
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
        state.maxMana = Mathf.Max(0, saveData.maxMana);
        state.currentMana = state.maxMana > 0 ? Mathf.Clamp(saveData.currentMana, 0, state.maxMana) : Mathf.Max(0, saveData.currentMana);
        if (state.maxMana <= 0)
        {
            state.maxMana = CalculateInitialMaxManaForNewWorld();
            state.currentMana = state.maxMana;
        }

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
                if (saved == null)
                    continue;

                UnitDefinition unit = !string.IsNullOrWhiteSpace(saved.sourceUnitId)
                    ? resolver.FindUnitDefinition(saved.sourceUnitId)
                    : null;

                UnitViewDefinition view = !string.IsNullOrWhiteSpace(saved.sourceUnitViewDefinitionName)
                    ? resolver.FindUnitViewDefinition(saved.sourceUnitViewDefinitionName)
                    : null;

                ItemDefinition prisonerItem = !string.IsNullOrWhiteSpace(saved.sourcePrisonerItemId)
                    ? resolver.FindItemDefinition(saved.sourcePrisonerItemId)
                    : null;

                if (unit == null && prisonerItem != null)
                    unit = prisonerItem.GetConvertedAllyUnitDefinition();

                if (view == null && prisonerItem != null)
                    view = prisonerItem.GetConvertedAllyUnitViewDefinition();

                if (unit == null && prisonerItem == null)
                    continue;

                PrisonerRuntimeData prisoner = new PrisonerRuntimeData
                {
                    prisonerInstanceId = saved.prisonerInstanceId,
                    sourceUnit = unit,
                    sourceUnitViewDefinition = view,
                    sourcePrisonerItem = prisonerItem,
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
        RaiseManaChanged();
    }
    private bool HasInventoryItem(ItemDefinition item)
    {
        return GetInventoryItemAmount(item) > 0;
    }

    private int GetInventoryItemAmount(ItemDefinition item)
    {
        if (item == null)
            return 0;

        WorldRunTransientState state = GetOrCreateWorldRunState();
        if (state == null || state.inventory == null)
            return 0;

        int amount = 0;
        for (int i = 0; i < state.inventory.Count; i++)
        {
            InventoryStackData stack = state.inventory[i];
            if (stack != null && stack.item == item && stack.amount > 0)
                amount += stack.amount;
        }

        return amount;
    }

    private int CountAssignedEquipmentItem(ItemDefinition item, PartyMemberData excludeMember = null, int excludeSlotIndex = -1)
    {
        if (item == null)
            return 0;

        WorldRunTransientState state = GetOrCreateWorldRunState();
        if (state == null || state.partyEquipmentAssignments == null)
            return 0;

        string excludeMemberId = excludeMember != null ? excludeMember.instanceId : null;
        int excludeSlot = excludeSlotIndex >= 0 ? Mathf.Clamp(excludeSlotIndex, 0, 1) : -1;

        int count = 0;
        for (int i = 0; i < state.partyEquipmentAssignments.Count; i++)
        {
            PartyEquipmentAssignmentData data = state.partyEquipmentAssignments[i];
            if (data == null)
                continue;

            bool sameMember = !string.IsNullOrWhiteSpace(excludeMemberId) && data.memberInstanceId == excludeMemberId;

            if (!(sameMember && excludeSlot == 0) && data.slot0Item == item)
                count++;

            if (!(sameMember && excludeSlot == 1) && data.slot1Item == item)
                count++;
        }

        return count;
    }

    private bool ClearFirstEquipmentReference(ItemDefinition item, PartyMemberData excludeMember = null, int excludeSlotIndex = -1)
    {
        if (item == null)
            return false;

        WorldRunTransientState state = GetOrCreateWorldRunState();
        if (state == null || state.partyEquipmentAssignments == null)
            return false;

        string excludeMemberId = excludeMember != null ? excludeMember.instanceId : null;
        int excludeSlot = excludeSlotIndex >= 0 ? Mathf.Clamp(excludeSlotIndex, 0, 1) : -1;

        for (int i = 0; i < state.partyEquipmentAssignments.Count; i++)
        {
            PartyEquipmentAssignmentData data = state.partyEquipmentAssignments[i];
            if (data == null)
                continue;

            bool sameMember = !string.IsNullOrWhiteSpace(excludeMemberId) && data.memberInstanceId == excludeMemberId;

            if (!(sameMember && excludeSlot == 0) && data.slot0Item == item)
            {
                data.slot0Item = null;
                return true;
            }

            if (!(sameMember && excludeSlot == 1) && data.slot1Item == item)
            {
                data.slot1Item = null;
                return true;
            }
        }

        return false;
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

    private void RaiseManaChanged()
    {
        OnManaChanged?.Invoke();
        OnWorldStateChanged?.Invoke();
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
