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

    private void Awake()
    {
        if (generateOnAwake)
            GenerateAndApplyEnemyParty();
    }

    private void OnDestroy()
    {
        if (runtimeGeneratedEnemyParty != null)
            Destroy(runtimeGeneratedEnemyParty);
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

        runtimeGeneratedEnemyParty = generated;
        battleManager.SetEnemyPartyDefinition(runtimeGeneratedEnemyParty);

        if (logGeneratedParty)
            Debug.Log(BuildPartySummary(runtimeGeneratedEnemyParty));
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

        PartyDefinition party = ScriptableObject.CreateInstance<PartyDefinition>();
        party.name = "RuntimeEnemyParty";
        party.partyName = "Random Encounter";
        party.members = new List<PartyMemberData>();
        party.inventory = new List<InventoryStackData>();

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
        member.currentLevel = 1;
        member.originalLevel = 1;
        member.instanceId = BuildInstanceId(entry.unitDefinition, slotIndex);
        member.instanceDisplayNameOverride = entry.instanceDisplayNameOverride;
        member.fixedEpitaph = entry.fixedEpitaph;
        member.statVariance = RollVariance(entry.unitDefinition.varianceRules);
        member.learnedSkills = CopySkills(entry.learnedSkills);
        return member;
    }

    private UnitInstanceStatVariance RollVariance(StatVarianceRules rules)
    {
        UnitInstanceStatVariance variance = new UnitInstanceStatVariance();
        if (rules == null)
            return variance;

        variance.maxHpDelta = RollRange(rules.maxHpRange);
        variance.dmgDelta = RollRange(rules.dmgRange);
        variance.spdDelta = RollRange(rules.spdRange);
        variance.hitDeltaX10 = RollRange(rules.hitRangeX10);
        variance.acDeltaX10 = RollRange(rules.acRangeX10);
        variance.criDelta = RollRange(rules.criRange);
        variance.crdDelta = RollRange(rules.crdRange);
        variance.poisonResistDelta = RollRange(rules.poisonResistRange);
        variance.bleedResistDelta = RollRange(rules.bleedResistRange);
        variance.stunResistDelta = RollRange(rules.stunResistRange);
        return variance;
    }

    private int RollRange(Vector2Int range)
    {
        int min = Mathf.Min(range.x, range.y);
        int max = Mathf.Max(range.x, range.y);
        return UnityEngine.Random.Range(min, max + 1);
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
            sb.Append("@slot ");
            sb.Append(member.startSlotIndex);
        }

        return sb.ToString();
    }
}
