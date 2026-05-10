using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public static class SaveDataMapper
{
    public static AccountProfileSaveData CaptureProfile(
        string accountId,
        string nickname,
        PersistentProfileController profileController,
        WorldRunManager worldRunManager,
        List<PersistentInventoryItemSaveData> persistentInventory = null)
    {
        AccountProfileSaveData save = new AccountProfileSaveData
        {
            accountId = accountId,
            nickname = nickname,
            lastSavedUnixTime = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        };

        if (persistentInventory != null)
            save.persistentInventory = new List<PersistentInventoryItemSaveData>(persistentInventory);

        if (profileController == null)
            return save;

        profileController.EnsureInitialized();
        PersistentProfileState profile = profileController.Profile;
        if (profile == null)
            return save;

        save.nextObtainedOrder = profile.nextObtainedOrder;
        save.lastWorldSettlementResult = profile.lastWorldSettlementResult;

        if (profile.accountCurrencies != null)
        {
            int commonShard = profile.accountCurrencies.GetCommonShardCount();
            save.currencies.unitShard = commonShard;
            // legacy fields are kept for backward-compatible inspection only.
            save.currencies.meleeShard = 0;
            save.currencies.midShard = 0;
            save.currencies.rangedShard = 0;
            save.currencies.cash = profile.accountCurrencies.cashCurrency;
        }

        if (worldRunManager != null)
        {
            save.currencies.soul = worldRunManager.PersistentSoul;
            if (save.currencies.cash <= 0)
                save.currencies.cash = worldRunManager.PersistentCash;
        }

        if (profile.rosterUnits != null)
        {
            for (int i = 0; i < profile.rosterUnits.Count; i++)
            {
                RosterUnitSaveData unit = RosterUnitSaveData.FromPersistent(profile.rosterUnits[i], profileController.PromotionBonusPercentPerRank);
                if (unit != null)
                    save.rosterUnits.Add(unit);
            }
        }

        if (profile.graveyardUnits != null)
        {
            for (int i = 0; i < profile.graveyardUnits.Count; i++)
            {
                RosterUnitSaveData unit = RosterUnitSaveData.FromPersistent(profile.graveyardUnits[i], profileController.PromotionBonusPercentPerRank);
                if (unit != null)
                    save.graveyardUnits.Add(unit);
            }
        }

        if (worldRunManager != null)
        {
            IReadOnlyList<PartyMemberData> orderedParty = worldRunManager.GetDisplayOrderedPartyMembers();
            if (orderedParty != null)
            {
                Dictionary<int, string> bySlot = new Dictionary<int, string>();
                for (int i = 0; i < orderedParty.Count; i++)
                {
                    PartyMemberData member = orderedParty[i];
                    if (member == null || string.IsNullOrWhiteSpace(member.instanceId))
                        continue;
                    bySlot[member.startSlotIndex] = member.instanceId;
                }

                for (int slot = 0; slot < 4; slot++)
                    save.activePartyUnitInstanceIds.Add(bySlot.TryGetValue(slot, out string id) ? id : string.Empty);
            }
        }

        return save;
    }

    public static ActiveWorldRunSaveData CaptureWorldRun(
        string accountId,
        WorldRunManager worldRunManager,
        WorldQuestController questController)
    {
        ActiveWorldRunSaveData save = new ActiveWorldRunSaveData
        {
            ownerAccountId = accountId,
            createdUnixTime = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        };

        if (worldRunManager == null || worldRunManager.MapData == null)
            return save;

        save.hasActiveWorld = true;
        save.mapRadius = worldRunManager.MapData.radius;
        save.worldStartMainCharacterLevel = Mathf.Max(1, worldRunManager.WorldStartMainCharacterLevel);

        bool interruptedArrival = worldRunManager.ShouldSaveAsInterruptedArrival();
        WorldTileData safeCurrentTile = interruptedArrival
            ? worldRunManager.GetSafeCurrentTileForSave()
            : worldRunManager.CurrentTile;

        save.currentTileId = safeCurrentTile != null ? safeCurrentTile.tileId : -1;
        save.selectedTileId = interruptedArrival
            ? -1
            : (worldRunManager.SelectedTile != null ? worldRunManager.SelectedTile.tileId : -1);
        save.difficultyId = worldRunManager.Settings != null ? worldRunManager.Settings.difficulty.ToString() : string.Empty;

        IReadOnlyList<WorldTileData> tiles = worldRunManager.MapData.Tiles;
        for (int i = 0; i < tiles.Count; i++)
        {
            WorldTileSaveData tile = WorldTileSaveData.FromRuntime(tiles[i]);
            if (tile == null)
                continue;

            if (interruptedArrival)
                tile.revealed = worldRunManager.ShouldRevealTileForInterruptedSave(tiles[i]);

            save.tiles.Add(tile);
        }

        WorldRunTransientState state = worldRunManager.CurrentWorldRunState;
        if (state != null)
        {
            if (state.inventory != null)
            {
                Dictionary<string, int> worldItemAmounts = new Dictionary<string, int>();
                for (int i = 0; i < state.inventory.Count; i++)
                {
                    InventoryStackData stack = state.inventory[i];
                    if (stack == null || stack.item == null || string.IsNullOrWhiteSpace(stack.item.itemId))
                        continue;

                    if (!worldItemAmounts.ContainsKey(stack.item.itemId))
                        worldItemAmounts[stack.item.itemId] = 0;

                    worldItemAmounts[stack.item.itemId] += Mathf.Max(0, stack.amount);
                }

                foreach (KeyValuePair<string, int> pair in worldItemAmounts)
                {
                    save.worldInventory.Add(new WorldInventoryItemSaveData
                    {
                        itemId = pair.Key,
                        amount = pair.Value,
                        ownershipType = InventoryOwnershipType.WorldOnly,
                    });
                }
            }

            if (state.prisoners != null)
            {
                for (int i = 0; i < state.prisoners.Count; i++)
                {
                    CapturedPrisonerSaveData prisoner = CapturedPrisonerSaveData.FromRuntime(state.prisoners[i]);
                    if (prisoner != null)
                        save.prisoners.Add(prisoner);
                }
            }

            save.sharedConsumableItemId = state.sharedConsumableItem != null ? state.sharedConsumableItem.itemId : string.Empty;
            save.currentMana = Mathf.Max(0, state.currentMana);
            save.maxMana = Mathf.Max(0, state.maxMana);

            if (state.partyEquipmentAssignments != null)
            {
                for (int i = 0; i < state.partyEquipmentAssignments.Count; i++)
                {
                    PartyEquipmentAssignmentData assignment = state.partyEquipmentAssignments[i];
                    if (assignment == null || string.IsNullOrWhiteSpace(assignment.memberInstanceId))
                        continue;

                    if (assignment.slot0Item != null)
                    {
                        save.equipmentAssignments.Add(new WorldEquipmentAssignmentSaveData
                        {
                            unitInstanceId = assignment.memberInstanceId,
                            slotIndex = 0,
                            itemId = assignment.slot0Item.itemId,
                            equipmentInstanceId = string.Empty,
                        });
                    }

                    if (assignment.slot1Item != null)
                    {
                        save.equipmentAssignments.Add(new WorldEquipmentAssignmentSaveData
                        {
                            unitInstanceId = assignment.memberInstanceId,
                            slotIndex = 1,
                            itemId = assignment.slot1Item.itemId,
                            equipmentInstanceId = string.Empty,
                        });
                    }
                }
            }
        }

        BattlePartyRuntimeState party = worldRunManager.PlayerPartyRuntimeState;
        if (party != null && party.members != null)
        {
            for (int i = 0; i < party.members.Count; i++)
            {
                WorldPartyMemberRuntimeSaveData member = WorldPartyMemberRuntimeSaveData.FromRuntime(party.members[i]);
                if (member != null)
                    save.worldPartyMembers.Add(member);
            }
        }

        if (questController != null)
        {
            IReadOnlyList<WorldQuestState> quests = questController.ActiveAcceptedQuests;
            if (quests != null)
            {
                for (int i = 0; i < quests.Count; i++)
                {
                    WorldQuestSaveData quest = WorldQuestSaveData.FromRuntime(quests[i]);
                    if (quest != null)
                        save.activeQuests.Add(quest);
                }
            }
        }

        return save;
    }

    public static void ApplyProfileToCurrentRuntime(AccountProfileSaveData saveData, PersistentProfileController profileController, WorldRunManager worldRunManager, SaveReferenceResolver resolver)
    {
        if (saveData == null || profileController == null)
            return;

        profileController.EnsureInitialized();
        PersistentProfileState profile = profileController.Profile;
        if (profile == null)
            return;

        profile.rosterUnits.Clear();
        if (profile.graveyardUnits == null)
            profile.graveyardUnits = new List<PersistentRosterUnitData>();
        profile.graveyardUnits.Clear();
        profile.nextObtainedOrder = saveData.nextObtainedOrder > 0 ? saveData.nextObtainedOrder : 1;
        profile.lastWorldSettlementResult = saveData.lastWorldSettlementResult;

        if (profile.accountCurrencies == null)
            profile.accountCurrencies = new PersistentAccountCurrencyState();
        profile.accountCurrencies.EnsureDefaults();
        profile.accountCurrencies.cashCurrency = Mathf.Max(0, saveData.currencies.cash);

        int commonShard = Mathf.Max(0, saveData.currencies.unitShard);
        // 클래스별/스킬별 샤드는 폐기되었으므로 legacy melee/mid/ranged 값은 이관하지 않는다.
        profile.accountCurrencies.SetCommonShardCount(commonShard);
        profile.accountCurrencies.ClearLegacyClassShards();

        if (saveData.rosterUnits != null)
        {
            for (int i = 0; i < saveData.rosterUnits.Count; i++)
            {
                PersistentRosterUnitData runtime = ToPersistentRosterUnit(saveData.rosterUnits[i], resolver);
                if (runtime != null)
                    profile.rosterUnits.Add(runtime);
            }
        }

        if (saveData.graveyardUnits != null)
        {
            for (int i = 0; i < saveData.graveyardUnits.Count; i++)
            {
                PersistentRosterUnitData runtime = ToPersistentRosterUnit(saveData.graveyardUnits[i], resolver);
                if (runtime != null)
                {
                    runtime.persistentCurrentHP = 0;
                    profile.graveyardUnits.Add(runtime);
                }
            }
        }

        if (worldRunManager != null)
        {
            SetPrivateInt(worldRunManager, "persistentSoul", Mathf.Max(0, saveData.currencies.soul));
            SetPrivateInt(worldRunManager, "persistentCash", Mathf.Max(0, saveData.currencies.cash));
        }
    }

    public static PersistentRosterUnitData ToPersistentRosterUnit(RosterUnitSaveData data, SaveReferenceResolver resolver)
    {
        if (data == null || resolver == null)
            return null;

        UnitDefinition unitDef = resolver.FindUnitDefinition(data.unitDefinitionId);
        if (unitDef == null)
            return null;

        PersistentRosterUnitData runtime = new PersistentRosterUnitData();
        runtime.instanceId = string.IsNullOrWhiteSpace(data.unitInstanceId) ? System.Guid.NewGuid().ToString("N") : data.unitInstanceId;
        runtime.instanceDisplayNameOverride = data.instanceDisplayNameOverride;
        runtime.fixedEpitaph = data.fixedEpitaph;
        runtime.obtainedOrder = data.obtainedOrder;
        runtime.unitDefinition = unitDef;
        runtime.unitViewDefinition = resolver.FindUnitViewDefinition(data.unitViewDefinitionName);
        runtime.isExchangeable = data.isExchangeable;
        runtime.isFavorite = data.isFavorite;
        runtime.isConvertedFromPrisoner = data.isConvertedFromPrisoner;
        runtime.isNft = data.isNft || (unitDef != null && unitDef.isNftUnit);
        runtime.unitRankOverride = Mathf.Clamp(data.unitRankOverride, 0, 9);
        runtime.currentLevel = Mathf.Max(1, data.level);
        runtime.originalLevel = Mathf.Max(1, data.originalLevel);
        runtime.currentExp = Mathf.Max(0, data.currentExp);
        runtime.levelGrowthMaxHp = Mathf.Max(0, data.levelGrowthMaxHp);
        runtime.levelGrowthDmg = Mathf.Max(0, data.levelGrowthDmg);
        runtime.promotionRank = LegionFormula.ClampLegionRank(data.promotionRank);
        runtime.statVariance = data.statVariance != null ? data.statVariance.ToRuntime() : new UnitInstanceStatVariance();
        runtime.persistentCurrentHP = data.persistentCurrentHP;
        runtime.learnedSkills = new List<SkillDefinition>();
        runtime.battleLootDrops = new List<ItemDropDefinition>();

        if (data.learnedSkillIds != null)
        {
            for (int i = 0; i < data.learnedSkillIds.Count; i++)
            {
                SkillDefinition skill = resolver.FindSkillDefinition(data.learnedSkillIds[i]);
                if (skill != null)
                    runtime.learnedSkills.Add(skill);
            }
        }

        if (data.battleLootDrops != null)
        {
            for (int i = 0; i < data.battleLootDrops.Count; i++)
            {
                BattleLootDropSaveData drop = data.battleLootDrops[i];
                if (drop == null)
                    continue;

                ItemDefinition item = resolver.FindItemDefinition(drop.itemId);
                if (item == null)
                    continue;

                runtime.battleLootDrops.Add(new ItemDropDefinition
                {
                    item = item,
                    dropChancePercent = drop.dropChancePercent,
                });
            }
        }

        runtime.EnsureDefaults();
        return runtime;
    }

    private static void SetShard(PersistentAccountCurrencyState currencies, ClassShardType type, int amount)
    {
        // Legacy helper. New shard policy is common/shared, so type is ignored.
        currencies.SetCommonShardCount(Mathf.Max(0, amount));
    }

    private static void SetPrivateInt(object target, string fieldName, int value)
    {
        if (target == null || string.IsNullOrWhiteSpace(fieldName))
            return;

        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field != null && field.FieldType == typeof(int))
            field.SetValue(target, value);
    }
}
