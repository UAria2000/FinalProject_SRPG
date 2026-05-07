using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class RandomEnemyEncounterBootstrapper : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private BattleManager battleManager;

    [Header("Encounter")]
    [SerializeField] private EnemyEncounterTable encounterTable;
    [SerializeField] private bool generateOnAwake = true;
    [SerializeField] private bool logGeneratedParty = false;

    private PartyDefinition runtimeGeneratedEnemyParty;

    private bool useDynamicLevelScaling;
    private int dynamicReferenceLevel = 1;
    private WorldDifficulty dynamicDifficulty = WorldDifficulty.Normal;

    private void Awake()
    {
        if (generateOnAwake)
            GenerateAndApplyEnemyParty();
    }

    private void OnDestroy()
    {
        DestroyRuntimeGeneratedParty();
    }

    public void SetEncounterTable(EnemyEncounterTable table)
    {
        encounterTable = table;
    }

    public void ConfigureDynamicLevelScaling(int mainCharacterLevel, WorldDifficulty difficulty)
    {
        useDynamicLevelScaling = true;
        dynamicReferenceLevel = Mathf.Max(1, mainCharacterLevel);
        dynamicDifficulty = difficulty;
    }

    public void ClearDynamicLevelScaling()
    {
        useDynamicLevelScaling = false;
        dynamicReferenceLevel = 1;
        dynamicDifficulty = WorldDifficulty.Normal;
    }

    public void GenerateAndApplyEnemyPartyFromTable(EnemyEncounterTable table)
    {
        encounterTable = table;
        GenerateAndApplyEnemyParty();
    }

    public void GenerateAndApplyEnemyPartyFromTable(EnemyEncounterTable table, int mainCharacterLevel, WorldDifficulty difficulty)
    {
        ConfigureDynamicLevelScaling(mainCharacterLevel, difficulty);
        GenerateAndApplyEnemyPartyFromTable(table);
    }

    public void GenerateAndApplyEnemyPartyFromPartyDefinition(PartyDefinition sourceParty, int mainCharacterLevel, WorldDifficulty difficulty)
    {
        if (battleManager == null)
            battleManager = GetComponent<BattleManager>();

        if (battleManager == null)
        {
            Debug.LogWarning("[RandomEnemyEncounterBootstrapper] BattleManager reference is missing.");
            return;
        }

        ConfigureDynamicLevelScaling(mainCharacterLevel, difficulty);
        PartyDefinition generated = GenerateRuntimeEnemyPartyFromDefinition(sourceParty);
        if (generated == null)
            return;

        DestroyRuntimeGeneratedParty();
        runtimeGeneratedEnemyParty = generated;
        battleManager.SetEnemyPartyDefinition(runtimeGeneratedEnemyParty);

        if (logGeneratedParty)
            Debug.Log(BuildPartySummary(runtimeGeneratedEnemyParty));
    }

    [ContextMenu("Generate And Apply Enemy Party")]
    public void GenerateAndApplyEnemyParty()
    {
        if (battleManager == null)
            battleManager = GetComponent<BattleManager>();

        if (battleManager == null)
        {
            Debug.LogWarning("[RandomEnemyEncounterBootstrapper] BattleManager reference is missing.");
            return;
        }

        PartyDefinition generated = GenerateRuntimeEnemyParty();
        if (generated == null)
            return;

        DestroyRuntimeGeneratedParty();
        runtimeGeneratedEnemyParty = generated;
        battleManager.SetEnemyPartyDefinition(runtimeGeneratedEnemyParty);

        if (logGeneratedParty)
            Debug.Log(BuildPartySummary(runtimeGeneratedEnemyParty));
    }

    public void DestroyRuntimeGeneratedParty()
    {
        if (runtimeGeneratedEnemyParty != null)
        {
            Destroy(runtimeGeneratedEnemyParty);
            runtimeGeneratedEnemyParty = null;
        }
    }

    public PartyDefinition GenerateRuntimeEnemyParty()
    {
        List<EnemyEncounterEntry> validEntries = CollectValidEntries();
        if (validEntries.Count == 0)
        {
            Debug.LogWarning("[RandomEnemyEncounterBootstrapper] No valid encounter entries found.");
            return null;
        }

        int enemyCount = encounterTable != null ? encounterTable.GetRandomEnemyCount() : 0;
        if (enemyCount <= 0)
        {
            Debug.LogWarning("[RandomEnemyEncounterBootstrapper] Enemy count resolved to 0.");
            return null;
        }

        PartyDefinition party = CreateRuntimePartyAsset("RuntimeEnemyParty", "Random Encounter");
        List<EnemyEncounterEntry> drawPool = new List<EnemyEncounterEntry>(validEntries);

        for (int slot = 0; slot < enemyCount; slot++)
        {
            EnemyEncounterEntry picked = PickRandomEntry(drawPool);
            if (picked == null)
                break;

            PartyMemberData member = CreatePartyMember(picked, slot);
            party.members.Add(member);

            bool allowDuplicates = encounterTable == null || encounterTable.allowDuplicates;
            if (!allowDuplicates)
                drawPool.Remove(picked);

            if (drawPool.Count == 0 && slot + 1 < enemyCount)
                break;
        }

        if (party.members.Count == 0)
        {
            Destroy(party);
            Debug.LogWarning("[RandomEnemyEncounterBootstrapper] Failed to create any enemy members.");
            return null;
        }

        return party;
    }

    public PartyDefinition GenerateRuntimeEnemyPartyFromDefinition(PartyDefinition sourceParty)
    {
        if (sourceParty == null || sourceParty.members == null || sourceParty.members.Count == 0)
        {
            Debug.LogWarning("[RandomEnemyEncounterBootstrapper] Source boss party is empty.");
            return null;
        }

        PartyDefinition party = CreateRuntimePartyAsset("RuntimeBossParty", sourceParty.partyName);

        for (int i = 0; i < sourceParty.members.Count && party.members.Count < 4; i++)
        {
            PartyMemberData source = sourceParty.members[i];
            if (source == null || source.unitDefinition == null)
                continue;

            PartyMemberData member = source.CloneRuntime();
            member.startSlotIndex = Mathf.Clamp(source.startSlotIndex, 0, 3);
            int level = RollLevel(null);
            member.currentLevel = level;
            member.originalLevel = level;
            member.currentExp = 0;
            member.levelGrowthMaxHp = 0;
            member.levelGrowthDmg = 0;
            RollLevelGrowthTotals(member, member.unitDefinition, level);
            member.instanceId = BuildInstanceId(member.unitDefinition, member.startSlotIndex);
            member.isExchangeable = RollCapturableEnemyNft(member.unitDefinition);
            member.isNft = member.isExchangeable;

            if (member.equippedItems == null || member.equippedItems.Count == 0)
                member.equippedItems = RollEnemyEquipment(member.unitDefinition);

            party.members.Add(member);
        }

        if (party.members.Count == 0)
        {
            Destroy(party);
            return null;
        }

        return party;
    }

    private PartyDefinition CreateRuntimePartyAsset(string assetName, string partyName)
    {
        PartyDefinition party = ScriptableObject.CreateInstance<PartyDefinition>();
        party.name = assetName;
        party.partyName = string.IsNullOrWhiteSpace(partyName) ? assetName : partyName;
        party.members = new List<PartyMemberData>();
        party.inventory = new List<InventoryStackData>();
        return party;
    }

    private List<EnemyEncounterEntry> CollectValidEntries()
    {
        List<EnemyEncounterEntry> result = new List<EnemyEncounterEntry>();
        if (encounterTable == null || encounterTable.entries == null)
            return result;

        for (int i = 0; i < encounterTable.entries.Count; i++)
        {
            EnemyEncounterEntry entry = encounterTable.entries[i];
            if (entry == null || !entry.enabled)
                continue;
            if (entry.unitDefinition == null || entry.unitViewDefinition == null)
                continue;
            if (entry.weight <= 0)
                continue;

            result.Add(entry);
        }

        return result;
    }

    private EnemyEncounterEntry PickRandomEntry(List<EnemyEncounterEntry> pool)
    {
        if (pool == null || pool.Count == 0)
            return null;

        int totalWeight = 0;
        for (int i = 0; i < pool.Count; i++)
            totalWeight += Mathf.Max(0, pool[i].weight);

        if (totalWeight <= 0)
            return pool[UnityEngine.Random.Range(0, pool.Count)];

        int roll = UnityEngine.Random.Range(0, totalWeight);
        int cumulative = 0;

        for (int i = 0; i < pool.Count; i++)
        {
            cumulative += Mathf.Max(0, pool[i].weight);
            if (roll < cumulative)
                return pool[i];
        }

        return pool[pool.Count - 1];
    }

    private PartyMemberData CreatePartyMember(EnemyEncounterEntry entry, int slotIndex)
    {
        PartyMemberData member = new PartyMemberData();
        member.unitDefinition = entry.unitDefinition;
        member.unitViewDefinition = entry.unitViewDefinition;
        member.startSlotIndex = Mathf.Clamp(slotIndex, 0, 3);
        int level = RollLevel(entry);
        member.currentLevel = level;
        member.originalLevel = level;
        RollLevelGrowthTotals(member, entry.unitDefinition, level);
        member.instanceId = BuildInstanceId(entry.unitDefinition, slotIndex);
        member.instanceDisplayNameOverride = entry.instanceDisplayNameOverride;
        member.fixedEpitaph = entry.fixedEpitaph;
        member.statVariance = RollVariance(entry.unitDefinition.varianceRules);
        member.learnedSkills = CopySkills(entry.learnedSkills);
        member.isExchangeable = RollCapturableEnemyNft(entry.unitDefinition);
        member.isNft = member.isExchangeable;
        member.equippedItems = RollEnemyEquipment(entry.unitDefinition);
        return member;
    }

    private int RollLevel(EnemyEncounterEntry entry)
    {
        if (useDynamicLevelScaling)
            return RollDynamicLevel();

        if (entry == null)
            return 1;

        int min = Mathf.Max(1, Mathf.Min(entry.minLevel, entry.maxLevel));
        int max = Mathf.Max(min, Mathf.Max(entry.minLevel, entry.maxLevel));
        return UnityEngine.Random.Range(min, max + 1);
    }

    private int RollDynamicLevel()
    {
        int reference = Mathf.Max(1, dynamicReferenceLevel);
        int minOffset;
        int maxOffset;

        if (reference < 100)
        {
            switch (dynamicDifficulty)
            {
                case WorldDifficulty.Easy:
                    minOffset = -20;
                    maxOffset = 0;
                    break;
                case WorldDifficulty.Hard:
                    minOffset = 10;
                    maxOffset = 20;
                    break;
                default:
                    minOffset = -10;
                    maxOffset = 10;
                    break;
            }
        }
        else
        {
            float minPercent;
            float maxPercent;
            switch (dynamicDifficulty)
            {
                case WorldDifficulty.Easy:
                    minPercent = -0.20f;
                    maxPercent = 0f;
                    break;
                case WorldDifficulty.Hard:
                    minPercent = 0f;
                    maxPercent = 0.20f;
                    break;
                default:
                    minPercent = -0.10f;
                    maxPercent = 0.10f;
                    break;
            }

            minOffset = Mathf.RoundToInt(reference * minPercent);
            maxOffset = Mathf.RoundToInt(reference * maxPercent);
        }

        int min = Mathf.Max(1, reference + Mathf.Min(minOffset, maxOffset));
        int max = Mathf.Max(min, reference + Mathf.Max(minOffset, maxOffset));
        return UnityEngine.Random.Range(min, max + 1);
    }

    private void RollLevelGrowthTotals(PartyMemberData member, UnitDefinition definition, int level)
    {
        if (member == null || definition == null)
            return;

        int levelUps = Mathf.Max(0, level - 1);
        for (int i = 0; i < levelUps; i++)
        {
            member.levelGrowthMaxHp += RollRange(definition.hpGrowthPerLevel);
            member.levelGrowthDmg += RollRange(definition.dmgGrowthPerLevel);
        }
    }

    private UnitInstanceStatVariance RollVariance(StatVarianceRules rules)
    {
        UnitInstanceStatVariance variance = new UnitInstanceStatVariance();
        if (rules == null)
            return variance;

        variance.maxHpDelta = RollRange(rules.maxHpRange);
        variance.dmgDelta = RollRange(rules.dmgRange);
        variance.spdDelta = RollRange(rules.spdRange);
        variance.idtDelta = RollRange(rules.idtRange);
        variance.hitDelta = RollRange(rules.hitRange);
        variance.acDelta = RollRange(rules.acRange);
        variance.criDelta = RollRange(rules.criRange);
        variance.crdDelta = RollRange(rules.crdRange);
        variance.burnResistDelta = RollRange(rules.burnResistRange);
        variance.bleedResistDelta = RollRange(rules.bleedResistRange);
        variance.stunResistDelta = RollRange(rules.stunResistRange);
        variance.frostResistDelta = RollRange(rules.frostResistRange);
        variance.blindResistDelta = RollRange(rules.blindResistRange);
        return variance;
    }

    private int RollRange(Vector2Int range)
    {
        int min = Mathf.Min(range.x, range.y);
        int max = Mathf.Max(range.x, range.y);
        return UnityEngine.Random.Range(min, max + 1);
    }

    private bool RollCapturableEnemyNft(UnitDefinition definition)
    {
        if (definition == null || !definition.canBeCaptured)
            return false;

        return UnityEngine.Random.Range(0f, 100f) < Mathf.Clamp(definition.capturableEnemyNftChancePercent, 0f, 100f);
    }

    private List<ItemDefinition> RollEnemyEquipment(UnitDefinition definition)
    {
        List<ItemDefinition> result = new List<ItemDefinition>();
        if (definition == null || definition.randomEnemyEquipment == null)
            return result;

        for (int i = 0; i < definition.randomEnemyEquipment.Count && result.Count < 2; i++)
        {
            ItemDropDefinition roll = definition.randomEnemyEquipment[i];
            if (roll == null || roll.item == null)
                continue;

            if (roll.item.mainUICategory != MainUIItemCategory.Equipment)
                continue;

            float chance = Mathf.Clamp(roll.dropChancePercent, 0f, 100f);
            if (UnityEngine.Random.Range(0f, 100f) < chance)
                result.Add(roll.item);
        }

        return result;
    }

    private List<SkillDefinition> CopySkills(List<SkillDefinition> source)
    {
        List<SkillDefinition> copied = new List<SkillDefinition>();
        if (source == null)
            return copied;

        for (int i = 0; i < source.Count; i++)
        {
            SkillDefinition skill = source[i];
            if (skill == null)
                continue;

            copied.Add(skill);
            if (copied.Count >= 3)
                break;
        }

        return copied;
    }

    private string BuildInstanceId(UnitDefinition definition, int slotIndex)
    {
        string unitId = definition != null && !string.IsNullOrEmpty(definition.unitId)
            ? definition.unitId
            : (definition != null ? definition.name : "enemy");

        return string.Format("enc_{0}_{1}_{2}", unitId, slotIndex, UnityEngine.Random.Range(1000, 9999));
    }

    private string BuildPartySummary(PartyDefinition party)
    {
        if (party == null || party.members == null)
            return "[RandomEnemyEncounterBootstrapper] Generated party is null.";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append("[RandomEnemyEncounterBootstrapper] Generated Enemy Party: ");

        for (int i = 0; i < party.members.Count; i++)
        {
            PartyMemberData member = party.members[i];
            if (member == null)
                continue;

            if (i > 0)
                sb.Append(", ");

            sb.Append(member.GetDisplayName());
            sb.Append("@Lv ");
            sb.Append(member.currentLevel);
            sb.Append(" slot ");
            sb.Append(member.startSlotIndex);
        }

        return sb.ToString();
    }
}
