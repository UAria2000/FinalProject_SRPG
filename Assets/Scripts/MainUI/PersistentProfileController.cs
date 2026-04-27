using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PersistentProfileController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WorldRunManager worldRunManager;
    [SerializeField] private SaveCoordinator saveCoordinator;

    [Header("Persistent Profile")]
    [SerializeField] private PersistentProfileState persistentProfile = new PersistentProfileState();

    [Header("Level / Promotion")]
    [Min(1)]
    [SerializeField] private int defaultLevelCap = 999;

    [Tooltip("수동 레벨업 시 부족한 EXP 1을 대체하는 데 필요한 소울. 1이면 부족 EXP 1 = 소울 1.")]
    [Min(0f)]
    [SerializeField] private float soulPerMissingExp = 1f;

    [Range(0f, 20f)]
    [SerializeField] private float promotionBonusPercentPerRank = 1f;

    public event Action OnProfileChanged;

    public PersistentProfileState Profile => persistentProfile;
    public float PromotionBonusPercentPerRank => promotionBonusPercentPerRank;

    private bool isInitializing;

    private void Awake()
    {
        if (worldRunManager == null)
            worldRunManager = GetComponent<WorldRunManager>() ?? UnityEngine.Object.FindFirstObjectByType<WorldRunManager>();

        if (saveCoordinator == null)
            saveCoordinator = UnityEngine.Object.FindFirstObjectByType<SaveCoordinator>();

        EnsureInitialized();
    }

    public void EnsureInitialized()
    {
        if (isInitializing)
            return;

        isInitializing = true;
        try
        {
            persistentProfile.EnsureDefaults();

            if (worldRunManager == null)
                return;

            BattlePartyRuntimeState runtime = worldRunManager.GetOrCreatePlayerPartyRuntimeState();
            if (runtime == null || runtime.members == null)
                return;

            if (persistentProfile.rosterUnits.Count == 0)
            {
                for (int i = 0; i < runtime.members.Count; i++)
                {
                    PartyMemberData member = runtime.members[i];
                    if (member == null || member.unitDefinition == null)
                        continue;

                    EnsureMemberInstanceId(member);

                    PersistentRosterUnitData rosterUnit = PersistentRosterUnitData.CreateFromPartyMember(
                        member,
                        false,
                        persistentProfile.ConsumeObtainedOrder());

                    persistentProfile.rosterUnits.Add(rosterUnit);
                }
            }
            else
            {
                SyncRosterFromActivePartyRuntime();
            }
        }
        finally
        {
            isInitializing = false;
        }
    }

    public IReadOnlyList<PersistentRosterUnitData> GetRosterUnits()
    {
        EnsureInitialized();
        SyncRosterFromActivePartyRuntime();
        return persistentProfile.rosterUnits;
    }

    public PersistentRosterUnitData FindRosterUnit(string instanceId)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            return null;

        EnsureInitialized();
        return FindRosterUnitInternal(instanceId);
    }

    private PersistentRosterUnitData FindRosterUnitInternal(string instanceId)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            return null;

        for (int i = 0; i < persistentProfile.rosterUnits.Count; i++)
        {
            PersistentRosterUnitData unit = persistentProfile.rosterUnits[i];
            if (unit != null && unit.instanceId == instanceId)
                return unit;
        }

        return null;
    }

    public bool IsRosterUnitInParty(PersistentRosterUnitData unit)
    {
        if (unit == null || worldRunManager == null)
            return false;

        if (IsDeadUnit(unit))
            return false;

        BattlePartyRuntimeState runtime = worldRunManager.GetOrCreatePlayerPartyRuntimeState();
        if (runtime == null || runtime.members == null)
            return false;

        for (int i = 0; i < runtime.members.Count; i++)
        {
            PartyMemberData member = runtime.members[i];
            if (member != null && member.instanceId == unit.instanceId)
                return true;
        }

        return false;
    }

    public bool IsMainCharacterPartyMember(PartyMemberData member)
    {
        return member != null && member.unitDefinition != null && member.unitDefinition.isMainPlayerCharacter;
    }

    public bool IsMainCharacter(PersistentRosterUnitData unit)
    {
        return unit != null && unit.unitDefinition != null && unit.unitDefinition.isMainPlayerCharacter;
    }

    public int GetMainCharacterLevelCap()
    {
        int foundLevel = 0;

        if (persistentProfile != null && persistentProfile.rosterUnits != null)
        {
            for (int i = 0; i < persistentProfile.rosterUnits.Count; i++)
            {
                PersistentRosterUnitData unit = persistentProfile.rosterUnits[i];
                if (IsMainCharacter(unit))
                    foundLevel = Mathf.Max(foundLevel, unit.currentLevel);
            }
        }

        if (worldRunManager != null)
        {
            BattlePartyRuntimeState runtime = worldRunManager.GetOrCreatePlayerPartyRuntimeState();
            if (runtime != null && runtime.members != null)
            {
                for (int i = 0; i < runtime.members.Count; i++)
                {
                    PartyMemberData member = runtime.members[i];
                    if (member != null && IsMainCharacterPartyMember(member))
                        foundLevel = Mathf.Max(foundLevel, member.currentLevel);
                }
            }
        }

        return foundLevel > 0 ? Mathf.Max(1, foundLevel) : Mathf.Max(1, defaultLevelCap);
    }

    public bool TryAssignRosterUnitToPartyAuto(PersistentRosterUnitData unit)
    {
        EnsureInitialized();
        if (unit == null || worldRunManager == null)
            return false;

        if (IsDeadUnit(unit))
            return false;

        SyncRosterFromActivePartyRuntime();

        BattlePartyRuntimeState runtime = worldRunManager.GetOrCreatePlayerPartyRuntimeState();
        if (runtime == null)
            return false;

        if (IsRosterUnitInParty(unit))
            return false;

        List<PartyMemberData> ordered = GetOrderedPartyMembers();
        if (ordered.Count >= 4)
            return false;

        ordered.Add(unit.CreateRuntimePartyMember(ordered.Count, promotionBonusPercentPerRank));
        ApplyOrderedPartyMembers(ordered);
        RaiseProfileChanged();
        return true;
    }

    public bool TryAssignRosterUnitToPartySlot(PersistentRosterUnitData unit, int targetBattleSlotIndex)
    {
        EnsureInitialized();
        if (unit == null || worldRunManager == null)
            return false;

        if (IsDeadUnit(unit))
            return false;

        SyncRosterFromActivePartyRuntime();

        List<PartyMemberData> ordered = GetOrderedPartyMembers();
        int targetIndex = Mathf.Clamp(targetBattleSlotIndex, 0, Mathf.Min(ordered.Count, 3));

        int existingIndex = FindPartyMemberIndexByInstanceId(ordered, unit.instanceId);
        PartyMemberData movingMember;

        if (existingIndex >= 0)
        {
            movingMember = ordered[existingIndex];
            ordered.RemoveAt(existingIndex);
            if (existingIndex < targetIndex)
                targetIndex--;
        }
        else
        {
            PartyMemberData occupantAtTarget = targetIndex < ordered.Count ? ordered[targetIndex] : null;
            if (ordered.Count >= 4 && occupantAtTarget == null)
                return false;

            movingMember = unit.CreateRuntimePartyMember(targetIndex, promotionBonusPercentPerRank);
        }

        targetIndex = Mathf.Clamp(targetIndex, 0, ordered.Count);

        if (targetIndex < ordered.Count)
        {
            PartyMemberData occupant = ordered[targetIndex];
            if (occupant != null && IsMainCharacterPartyMember(occupant) && occupant.instanceId != movingMember.instanceId)
                return false;

            if (occupant != null && occupant.instanceId != movingMember.instanceId)
                ordered.RemoveAt(targetIndex);
        }

        targetIndex = Mathf.Clamp(targetIndex, 0, ordered.Count);
        ordered.Insert(targetIndex, movingMember);
        ApplyOrderedPartyMembers(ordered);
        RaiseProfileChanged();
        return true;
    }

    public bool TryReplacePartyMemberWithRosterUnit(PersistentRosterUnitData replacement, PartyMemberData targetMember)
    {
        if (replacement == null || targetMember == null)
            return false;

        if (IsMainCharacterPartyMember(targetMember))
            return false;

        return TryAssignRosterUnitToPartySlot(replacement, targetMember.startSlotIndex);
    }

    public bool TryRemovePartyMemberToRoster(PartyMemberData member)
    {
        EnsureInitialized();
        if (member == null || worldRunManager == null || IsMainCharacterPartyMember(member))
            return false;

        BattlePartyRuntimeState runtime = worldRunManager.GetOrCreatePlayerPartyRuntimeState();
        if (runtime == null || runtime.members == null)
            return false;

        SyncRosterFromActivePartyRuntime();

        bool removed = false;
        for (int i = runtime.members.Count - 1; i >= 0; i--)
        {
            PartyMemberData candidate = runtime.members[i];
            if (candidate != null && candidate.instanceId == member.instanceId)
            {
                runtime.members.RemoveAt(i);
                removed = true;
                break;
            }
        }

        if (!removed)
            return false;

        NormalizePartySlots(runtime.members);
        RaiseProfileChanged();
        return true;
    }

    public bool ToggleFavorite(PersistentRosterUnitData unit)
    {
        if (unit == null)
            return false;

        unit.isFavorite = !unit.isFavorite;
        RaiseProfileChanged();
        return true;
    }

    public bool TryRenameUnit(PersistentRosterUnitData unit, string newName)
    {
        EnsureInitialized();
        if (unit == null)
            return false;

        string trimmed = string.IsNullOrWhiteSpace(newName) ? string.Empty : newName.Trim();
        unit.instanceDisplayNameOverride = trimmed;
        ApplyRosterUnitToActivePartyIfPresent(unit);
        RaiseProfileChanged();
        return true;
    }

    public bool CanLevelUp(PersistentRosterUnitData unit, out int requiredSoul)
    {
        requiredSoul = 0;
        if (unit == null || worldRunManager == null)
            return false;

        if (IsDeadUnit(unit))
            return false;

        unit.EnsureDefaults();
        int cap = GetLevelCapForUnit(unit);
        if (unit.currentLevel >= cap)
            return false;

        requiredSoul = LegionFormula.GetSoulCostToFillMissingExp(unit, cap, soulPerMissingExp);
        return requiredSoul <= 0 || worldRunManager.PersistentSoul >= requiredSoul;
    }

    public bool TryLevelUp(PersistentRosterUnitData unit)
    {
        EnsureInitialized();
        if (!CanLevelUp(unit, out int requiredSoul))
            return false;

        if (requiredSoul > 0 && !worldRunManager.TrySpendPersistentSoul(requiredSoul))
            return false;

        if (!ApplySingleLevelUp(unit, consumeExp: true))
            return false;

        ApplyRosterUnitToActivePartyIfPresent(unit);
        RaiseProfileChanged();
        return true;
    }

    public int GetLevelCapForUnit(PersistentRosterUnitData unit)
    {
        int defaultCap = Mathf.Max(1, defaultLevelCap);
        if (unit == null)
            return defaultCap;

        if (IsMainCharacter(unit))
            return defaultCap;

        return Mathf.Max(1, GetMainCharacterLevelCap());
    }

    public bool IsDeadUnit(PersistentRosterUnitData unit)
    {
        if (unit == null)
            return false;

        return unit.persistentCurrentHP == 0;
    }

    public int AddExperienceToActivePartyMembers(int amount)
    {
        EnsureInitialized();
        SyncRosterFromActivePartyRuntime();

        int exp = Mathf.Max(0, amount);
        if (exp <= 0 || worldRunManager == null)
            return 0;

        BattlePartyRuntimeState runtime = worldRunManager.GetOrCreatePlayerPartyRuntimeState();
        if (runtime == null || runtime.members == null)
            return 0;

        int grantedUnitCount = 0;
        for (int pass = 0; pass < 2; pass++)
        {
            bool mainPass = pass == 0;
            for (int i = 0; i < runtime.members.Count; i++)
            {
                PartyMemberData member = runtime.members[i];
                if (member == null || string.IsNullOrWhiteSpace(member.instanceId))
                    continue;

                if (member.persistentCurrentHP == 0)
                    continue;

                bool isMain = IsMainCharacterPartyMember(member);
                if (mainPass != isMain)
                    continue;

                PersistentRosterUnitData rosterUnit = FindRosterUnitInternal(member.instanceId);
                if (rosterUnit == null || IsDeadUnit(rosterUnit))
                    continue;

                if (AddExperienceToRosterUnit(rosterUnit, exp, true))
                    grantedUnitCount++;
            }
        }

        if (grantedUnitCount > 0)
        {
            SyncRosterToActivePartyRuntime();
            RaiseProfileChanged();
        }

        return grantedUnitCount;
    }

    public bool AddExperienceToRosterUnit(PersistentRosterUnitData unit, int amount, bool autoLevelUp)
    {
        if (unit == null || amount <= 0 || IsDeadUnit(unit))
            return false;

        unit.EnsureDefaults();

        int cap = GetLevelCapForUnit(unit);
        unit.currentExp += Mathf.Max(0, amount);

        if (autoLevelUp)
            ResolveAutoLevelUps(unit, cap);

        ClampExpAtLevelCap(unit, cap);
        return true;
    }

    public void SyncFromActivePartyRuntimeAndSave()
    {
        EnsureInitialized();
        SyncRosterFromActivePartyRuntime();
        RaiseProfileChanged();
    }

    private void ResolveAutoLevelUps(PersistentRosterUnitData unit, int cap)
    {
        if (unit == null)
            return;

        int safeGuard = 0;
        while (unit.currentLevel < cap && safeGuard < 1000)
        {
            int needExp = LegionFormula.GetExpToNextLevel(unit.currentLevel);
            if (unit.currentExp < needExp)
                break;

            ApplySingleLevelUp(unit, consumeExp: true);
            safeGuard++;
        }
    }

    private bool ApplySingleLevelUp(PersistentRosterUnitData unit, bool consumeExp)
    {
        if (unit == null)
            return false;

        int cap = GetLevelCapForUnit(unit);
        if (unit.currentLevel >= cap)
            return false;

        int needExp = LegionFormula.GetExpToNextLevel(unit.currentLevel);
        if (consumeExp)
            unit.currentExp = Mathf.Max(0, unit.currentExp - needExp);
        else
            unit.currentExp = 0;

        int maxHpBefore = GetRosterMaxHp(unit);
        ApplyLevelGrowthRoll(unit);
        unit.currentLevel = Mathf.Min(unit.currentLevel + 1, cap);
        int maxHpAfter = GetRosterMaxHp(unit);

        if (unit.persistentCurrentHP > 0)
            unit.persistentCurrentHP = Mathf.Clamp(unit.persistentCurrentHP + Mathf.Max(0, maxHpAfter - maxHpBefore), 1, maxHpAfter);

        return true;
    }

    private void ApplyLevelGrowthRoll(PersistentRosterUnitData unit)
    {
        if (unit == null || unit.unitDefinition == null)
            return;

        unit.levelGrowthMaxHp += RollInclusive(unit.unitDefinition.hpGrowthPerLevel);
        unit.levelGrowthDmg += RollInclusive(unit.unitDefinition.dmgGrowthPerLevel);
    }

    private int RollInclusive(Vector2Int range)
    {
        int min = Mathf.Min(range.x, range.y);
        int max = Mathf.Max(range.x, range.y);
        return UnityEngine.Random.Range(min, max + 1);
    }

    private void ClampExpAtLevelCap(PersistentRosterUnitData unit, int cap)
    {
        if (unit == null)
            return;

        if (unit.currentLevel >= Mathf.Max(1, cap))
        {
            int maxStoredExp = LegionFormula.GetExpToNextLevel(unit.currentLevel);
            unit.currentExp = Mathf.Clamp(unit.currentExp, 0, maxStoredExp);
        }
        else
        {
            unit.currentExp = Mathf.Max(0, unit.currentExp);
        }
    }

    private int GetRosterMaxHp(PersistentRosterUnitData unit)
    {
        if (unit == null)
            return 1;

        int baseHp = unit.unitDefinition != null ? unit.unitDefinition.maxHP : 1;
        int varianceHp = unit.statVariance != null ? unit.statVariance.maxHpDelta : 0;
        int growthHp = Mathf.Max(0, unit.levelGrowthMaxHp);
        float promo = LegionFormula.GetPromotionMultiplier(unit.promotionRank, promotionBonusPercentPerRank);
        return Mathf.Max(1, Mathf.RoundToInt((baseHp + varianceHp + growthHp) * promo));
    }

    public int GetPromotionShardCount()
    {
        return GetUnitShardCount();
    }

    public int GetUnitShardCount()
    {
        EnsureInitialized();
        persistentProfile.accountCurrencies.EnsureDefaults();
        return persistentProfile.accountCurrencies.GetCommonShardCount();
    }

    public bool CanPromote(PersistentRosterUnitData unit, out int requiredShards)
    {
        requiredShards = 0;
        if (unit == null)
            return false;

        if (IsDeadUnit(unit))
            return false;

        unit.EnsureDefaults();
        if (LegionFormula.IsMaxPromotionRank(unit.promotionRank))
            return false;

        requiredShards = LegionFormula.GetPromotionCost(unit.promotionRank);
        if (requiredShards <= 0)
            return false;

        return GetPromotionShardCount() >= requiredShards;
    }

    public bool TryPromote(PersistentRosterUnitData unit)
    {
        EnsureInitialized();
        if (unit == null)
            return false;

        if (!CanPromote(unit, out int requiredShards))
            return false;

        if (!TrySpendPromotionShards(requiredShards))
            return false;

        unit.promotionRank = LegionFormula.ClampLegionRank(unit.promotionRank + 1);
        ApplyRosterUnitToActivePartyIfPresent(unit);
        RaiseProfileChanged();
        return true;
    }

    private bool TrySpendPromotionShards(int required)
    {
        int clamped = Mathf.Max(0, required);
        if (clamped <= 0)
            return true;

        persistentProfile.accountCurrencies.EnsureDefaults();
        return persistentProfile.accountCurrencies.TrySpendCommonShards(clamped);
    }

    public bool CanDecompose(PersistentRosterUnitData unit)
    {
        if (unit == null)
            return false;

        if (unit.unitDefinition != null && unit.unitDefinition.isMainPlayerCharacter)
            return false;

        if (!unit.CanDefinitionBeDecomposed())
            return false;

        if (unit.isFavorite)
            return false;

        if (IsRosterUnitInParty(unit))
            return false;

        return true;
    }

    public bool TryDecompose(PersistentRosterUnitData unit)
    {
        EnsureInitialized();
        if (!CanDecompose(unit) || worldRunManager == null)
            return false;

        int soulGain = LegionFormula.GetDecomposeSoulReward(unit);
        if (soulGain > 0)
            worldRunManager.AddPersistentSoul(soulGain);

        int shardGain = LegionFormula.GetTotalDecomposeShardReward(unit);
        if (shardGain > 0)
            persistentProfile.accountCurrencies.AddCommonShards(shardGain);

        RemoveRosterUnitByInstanceId(unit.instanceId);
        RaiseProfileChanged();
        return true;
    }

    public bool TryBatchDecompose(IReadOnlyList<PersistentRosterUnitData> units)
    {
        EnsureInitialized();
        if (units == null || units.Count <= 0)
            return false;

        bool changed = false;
        for (int i = units.Count - 1; i >= 0; i--)
        {
            PersistentRosterUnitData unit = units[i];
            if (unit == null)
                continue;
            changed |= TryDecompose(unit);
        }
        return changed;
    }

    public void GetDecomposePreview(IReadOnlyCollection<PersistentRosterUnitData> units, out int soulGain, out int shardGain)
    {
        soulGain = 0;
        shardGain = 0;
        if (units == null)
            return;

        foreach (PersistentRosterUnitData unit in units)
        {
            if (unit == null || !CanDecompose(unit))
                continue;

            soulGain += LegionFormula.GetDecomposeSoulReward(unit);
            shardGain += LegionFormula.GetTotalDecomposeShardReward(unit);
        }
    }

    public LegionEquipmentBonusSummary GetEquipmentBonusSummary(PersistentRosterUnitData unit)
    {
        LegionEquipmentBonusSummary summary = new LegionEquipmentBonusSummary();
        if (unit == null || worldRunManager == null)
            return summary;

        BattlePartyRuntimeState runtime = worldRunManager.GetOrCreatePlayerPartyRuntimeState();
        if (runtime == null || runtime.members == null)
            return summary;

        PartyMemberData runtimeMember = null;
        for (int i = 0; i < runtime.members.Count; i++)
        {
            PartyMemberData member = runtime.members[i];
            if (member != null && member.instanceId == unit.instanceId)
            {
                runtimeMember = member;
                break;
            }
        }

        if (runtimeMember == null)
            return summary;

        ApplyItemBonusToSummary(worldRunManager.GetAssignedEquipmentItem(runtimeMember, 0), ref summary);
        ApplyItemBonusToSummary(worldRunManager.GetAssignedEquipmentItem(runtimeMember, 1), ref summary);
        return summary;
    }

    public int GetNextPageCount(int pageSize)
    {
        IReadOnlyList<PersistentRosterUnitData> units = GetRosterUnits();
        if (units == null || units.Count <= 0)
            return 1;

        return Mathf.Max(1, Mathf.CeilToInt(units.Count / (float)Mathf.Max(1, pageSize)));
    }

    public void AddClassShard(ClassShardType type, int amount)
    {
        // 신규 정책: 파편은 전 유닛 공통이다. 기존 호출부 호환을 위해 type은 무시한다.
        AddPromotionShard(amount);
    }

    public void AddPromotionShard(int amount)
    {
        EnsureInitialized();
        persistentProfile.accountCurrencies.AddCommonShards(amount);
        RaiseProfileChanged();
    }

    public bool TrySpendPromotionShardForDebug(int amount)
    {
        EnsureInitialized();
        bool spent = persistentProfile.accountCurrencies.TrySpendCommonShards(amount);
        if (spent)
            RaiseProfileChanged();
        return spent;
    }

    public void AddRosterUnit(PersistentRosterUnitData unit)
    {
        EnsureInitialized();
        if (unit == null)
            return;

        unit.EnsureDefaults();
        if (unit.obtainedOrder <= 0)
            unit.obtainedOrder = persistentProfile.ConsumeObtainedOrder();

        if (FindRosterUnitInternal(unit.instanceId) == null)
            persistentProfile.rosterUnits.Add(unit);

        RaiseProfileChanged();
    }

    public void RebuildActivePartyFromSavedIds(IReadOnlyList<string> savedInstanceIds)
    {
        EnsureInitialized();
        if (worldRunManager == null)
            return;

        BattlePartyRuntimeState runtime = worldRunManager.GetOrCreatePlayerPartyRuntimeState();
        if (runtime == null)
            return;

        List<PartyMemberData> rebuilt = new List<PartyMemberData>();

        if (savedInstanceIds != null)
        {
            for (int i = 0; i < savedInstanceIds.Count && rebuilt.Count < 4; i++)
            {
                string instanceId = savedInstanceIds[i];
                if (string.IsNullOrWhiteSpace(instanceId))
                    continue;

                PersistentRosterUnitData rosterUnit = FindRosterUnitInternal(instanceId);
                if (rosterUnit == null || IsDeadUnit(rosterUnit))
                    continue;

                if (FindPartyMemberIndexByInstanceId(rebuilt, rosterUnit.instanceId) >= 0)
                    continue;

                rebuilt.Add(rosterUnit.CreateRuntimePartyMember(rebuilt.Count, promotionBonusPercentPerRank));
            }
        }

        if (rebuilt.Count <= 0)
        {
            List<PartyMemberData> fallback = GetOrderedPartyMembers();
            for (int i = 0; i < fallback.Count && rebuilt.Count < 4; i++)
            {
                PartyMemberData member = fallback[i];
                if (member != null)
                    rebuilt.Add(member.CloneRuntime());
            }
        }

        NormalizePartySlots(rebuilt);
        runtime.members = rebuilt;
    }

    private bool RemoveRosterUnitByInstanceId(string instanceId)
    {
        if (string.IsNullOrWhiteSpace(instanceId) || persistentProfile.rosterUnits == null)
            return false;

        for (int i = persistentProfile.rosterUnits.Count - 1; i >= 0; i--)
        {
            PersistentRosterUnitData candidate = persistentProfile.rosterUnits[i];
            if (candidate != null && candidate.instanceId == instanceId)
            {
                persistentProfile.rosterUnits.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    private void SyncRosterFromActivePartyRuntime()
    {
        if (isInitializing)
            return;

        if (worldRunManager == null)
            return;

        BattlePartyRuntimeState runtime = worldRunManager.GetOrCreatePlayerPartyRuntimeState();
        if (runtime == null || runtime.members == null)
            return;

        for (int i = 0; i < runtime.members.Count; i++)
        {
            PartyMemberData member = runtime.members[i];
            if (member == null || member.unitDefinition == null)
                continue;

            EnsureMemberInstanceId(member);

            PersistentRosterUnitData rosterUnit = FindRosterUnitInternal(member.instanceId);
            if (rosterUnit == null)
            {
                rosterUnit = PersistentRosterUnitData.CreateFromPartyMember(
                    member,
                    false,
                    persistentProfile.ConsumeObtainedOrder());
                persistentProfile.rosterUnits.Add(rosterUnit);
            }
            else
            {
                rosterUnit.instanceDisplayNameOverride = member.instanceDisplayNameOverride;
                rosterUnit.fixedEpitaph = member.fixedEpitaph;
                rosterUnit.unitDefinition = member.unitDefinition;
                rosterUnit.unitViewDefinition = member.unitViewDefinition;
                rosterUnit.currentLevel = Mathf.Max(1, member.currentLevel);
                rosterUnit.originalLevel = Mathf.Max(1, member.originalLevel);
                rosterUnit.currentExp = Mathf.Max(0, member.currentExp);
                rosterUnit.levelGrowthMaxHp = Mathf.Max(0, member.levelGrowthMaxHp);
                rosterUnit.levelGrowthDmg = Mathf.Max(0, member.levelGrowthDmg);
                member.promotionRank = LegionFormula.ClampLegionRank(member.promotionRank);
                rosterUnit.promotionRank = LegionFormula.ClampLegionRank(member.promotionRank);
                rosterUnit.statVariance = member.statVariance != null ? member.statVariance.CloneRuntime() : new UnitInstanceStatVariance();
                rosterUnit.learnedSkills = member.learnedSkills != null ? new List<SkillDefinition>(member.learnedSkills) : new List<SkillDefinition>();
                rosterUnit.battleLootDrops = member.battleLootDrops != null ? new List<ItemDropDefinition>(member.battleLootDrops) : new List<ItemDropDefinition>();
                rosterUnit.persistentCurrentHP = member.persistentCurrentHP;
                rosterUnit.EnsureDefaults();
            }
        }
    }

    private void SyncRosterToActivePartyRuntime()
    {
        if (worldRunManager == null)
            return;

        BattlePartyRuntimeState runtime = worldRunManager.GetOrCreatePlayerPartyRuntimeState();
        if (runtime == null || runtime.members == null)
            return;

        for (int i = 0; i < runtime.members.Count; i++)
        {
            PartyMemberData member = runtime.members[i];
            if (member == null || string.IsNullOrWhiteSpace(member.instanceId))
                continue;

            PersistentRosterUnitData rosterUnit = FindRosterUnitInternal(member.instanceId);
            if (rosterUnit == null)
                continue;

            int slot = member.startSlotIndex;
            PartyMemberData updated = rosterUnit.CreateRuntimePartyMember(slot, promotionBonusPercentPerRank);
            runtime.members[i] = updated;
        }
    }

    private void ApplyRosterUnitToActivePartyIfPresent(PersistentRosterUnitData unit)
    {
        if (unit == null || worldRunManager == null)
            return;

        BattlePartyRuntimeState runtime = worldRunManager.GetOrCreatePlayerPartyRuntimeState();
        if (runtime == null || runtime.members == null)
            return;

        for (int i = 0; i < runtime.members.Count; i++)
        {
            PartyMemberData member = runtime.members[i];
            if (member != null && member.instanceId == unit.instanceId)
            {
                int slot = member.startSlotIndex;
                runtime.members[i] = unit.CreateRuntimePartyMember(slot, promotionBonusPercentPerRank);
                return;
            }
        }
    }

    private List<PartyMemberData> GetOrderedPartyMembers()
    {
        List<PartyMemberData> ordered = new List<PartyMemberData>();
        if (worldRunManager == null)
            return ordered;

        BattlePartyRuntimeState runtime = worldRunManager.GetOrCreatePlayerPartyRuntimeState();
        if (runtime == null || runtime.members == null)
            return ordered;

        for (int i = 0; i < runtime.members.Count; i++)
        {
            PartyMemberData member = runtime.members[i];
            if (member != null)
                ordered.Add(member);
        }

        ordered.Sort((a, b) => a.startSlotIndex.CompareTo(b.startSlotIndex));
        return ordered;
    }

    private void ApplyOrderedPartyMembers(List<PartyMemberData> ordered)
    {
        if (worldRunManager == null)
            return;

        BattlePartyRuntimeState runtime = worldRunManager.GetOrCreatePlayerPartyRuntimeState();
        if (runtime == null)
            return;

        NormalizePartySlots(ordered);
        runtime.members = ordered;
    }

    private void NormalizePartySlots(List<PartyMemberData> members)
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

    private int FindPartyMemberIndexByInstanceId(List<PartyMemberData> members, string instanceId)
    {
        if (members == null || string.IsNullOrWhiteSpace(instanceId))
            return -1;

        for (int i = 0; i < members.Count; i++)
        {
            if (members[i] != null && members[i].instanceId == instanceId)
                return i;
        }

        return -1;
    }

    private void ApplyItemBonusToSummary(ItemDefinition item, ref LegionEquipmentBonusSummary summary)
    {
        if (item == null || item.effects == null)
            return;

        for (int i = 0; i < item.effects.Count; i++)
        {
            BattleEffectBlock block = item.effects[i];
            if (block == null)
                continue;

            int amount = block.flatValue;
            switch (block.statModifierType)
            {
                case StatModifierType.DMG:
                    summary.dmg += amount;
                    break;
                case StatModifierType.SPD:
                    summary.spd += amount;
                    break;
                case StatModifierType.HIT:
                    summary.hitX10 += amount;
                    break;
                case StatModifierType.AC:
                    summary.acX10 += amount;
                    break;
                case StatModifierType.CRI:
                    summary.cri += amount;
                    break;
                case StatModifierType.CRD:
                    summary.crd += amount;
                    break;
            }
        }
    }

    private void EnsureMemberInstanceId(PartyMemberData member)
    {
        if (member == null)
            return;

        if (string.IsNullOrWhiteSpace(member.instanceId))
            member.instanceId = Guid.NewGuid().ToString("N");

        member.promotionRank = LegionFormula.ClampLegionRank(member.promotionRank);
    }

    private void RaiseProfileChanged()
    {
        OnProfileChanged?.Invoke();
        saveCoordinator?.SaveProfile();
    }
}
