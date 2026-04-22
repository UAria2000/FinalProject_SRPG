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

    [Header("Promotion")]
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

    public int GetMainCharacterLevelCap()
    {
        if (worldRunManager == null)
            return 1;

        BattlePartyRuntimeState runtime = worldRunManager.GetOrCreatePlayerPartyRuntimeState();
        if (runtime != null && runtime.members != null)
        {
            for (int i = 0; i < runtime.members.Count; i++)
            {
                PartyMemberData member = runtime.members[i];
                if (member != null && IsMainCharacterPartyMember(member))
                    return Mathf.Max(1, member.currentLevel);
            }
        }

        return 1;
    }

    public bool TryAssignRosterUnitToPartyAuto(PersistentRosterUnitData unit)
    {
        EnsureInitialized();
        if (unit == null || worldRunManager == null)
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

    public bool CanLevelUp(PersistentRosterUnitData unit, out int requiredSoul)
    {
        requiredSoul = 0;
        if (unit == null || worldRunManager == null)
            return false;

        int cap = GetMainCharacterLevelCap();
        requiredSoul = BarracksFormula.GetRemainingSoulCostToNextLevel(unit, cap);
        if (requiredSoul <= 0)
            return false;

        return worldRunManager.PersistentSoul >= requiredSoul;
    }

    public bool TryLevelUp(PersistentRosterUnitData unit)
    {
        if (!CanLevelUp(unit, out int requiredSoul))
            return false;

        if (!worldRunManager.TrySpendPersistentSoul(requiredSoul))
            return false;

        unit.currentLevel = Mathf.Min(unit.currentLevel + 1, GetMainCharacterLevelCap());
        unit.currentExp = 0;
        ApplyRosterUnitToActivePartyIfPresent(unit);
        RaiseProfileChanged();
        return true;
    }

    public int GetClassShardCount(ClassShardType type)
    {
        EnsureInitialized();
        persistentProfile.accountCurrencies.EnsureDefaults();
        return persistentProfile.accountCurrencies.GetShardCount(type);
    }

    public bool CanPromote(PersistentRosterUnitData unit, out int requiredShards)
    {
        requiredShards = 0;
        if (unit == null)
            return false;

        ClassShardType shardType = BarracksFormula.ResolveClassShardType(unit.unitDefinition);
        requiredShards = BarracksFormula.GetPromotionCost(unit.promotionRank);
        return GetClassShardCount(shardType) >= requiredShards;
    }

    public bool TryPromote(PersistentRosterUnitData unit)
    {
        EnsureInitialized();
        if (unit == null)
            return false;

        ClassShardType shardType = BarracksFormula.ResolveClassShardType(unit.unitDefinition);
        if (!CanPromote(unit, out int requiredShards))
            return false;

        if (!persistentProfile.accountCurrencies.TrySpendShards(shardType, requiredShards))
            return false;

        unit.promotionRank = Mathf.Max(0, unit.promotionRank) + 1;
        ApplyRosterUnitToActivePartyIfPresent(unit);
        RaiseProfileChanged();
        return true;
    }

    public bool CanDecompose(PersistentRosterUnitData unit)
    {
        if (unit == null)
            return false;

        if (unit.unitDefinition != null && unit.unitDefinition.isMainPlayerCharacter)
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

        int soulGain = Mathf.Max(0, unit.unitDefinition != null ? unit.unitDefinition.baseSoulReward : 0);
        if (soulGain > 0)
            worldRunManager.AddPersistentSoul(soulGain);

        ClassShardType type = BarracksFormula.ResolveClassShardType(unit.unitDefinition);
        int shardGain = Mathf.Max(1, BarracksFormula.GetDecomposeRefundPromotionShards(unit.promotionRank));
        persistentProfile.accountCurrencies.AddShards(type, shardGain);

        persistentProfile.rosterUnits.Remove(unit);
        RaiseProfileChanged();
        return true;
    }

    public BarracksEquipmentBonusSummary GetEquipmentBonusSummary(PersistentRosterUnitData unit)
    {
        BarracksEquipmentBonusSummary summary = new BarracksEquipmentBonusSummary();
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
        EnsureInitialized();
        persistentProfile.accountCurrencies.AddShards(type, amount);
        RaiseProfileChanged();
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
                if (rosterUnit == null)
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
                rosterUnit.promotionRank = Mathf.Max(0, member.promotionRank);
                rosterUnit.statVariance = member.statVariance != null ? member.statVariance.CloneRuntime() : new UnitInstanceStatVariance();
                rosterUnit.learnedSkills = member.learnedSkills != null ? new List<SkillDefinition>(member.learnedSkills) : new List<SkillDefinition>();
                rosterUnit.battleLootDrops = member.battleLootDrops != null ? new List<ItemDropDefinition>(member.battleLootDrops) : new List<ItemDropDefinition>();
                rosterUnit.persistentCurrentHP = member.persistentCurrentHP;
                rosterUnit.EnsureDefaults();
            }
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

    private void ApplyItemBonusToSummary(ItemDefinition item, ref BarracksEquipmentBonusSummary summary)
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
    }

    private void RaiseProfileChanged()
    {
        OnProfileChanged?.Invoke();
        saveCoordinator?.SaveProfile();
    }
}