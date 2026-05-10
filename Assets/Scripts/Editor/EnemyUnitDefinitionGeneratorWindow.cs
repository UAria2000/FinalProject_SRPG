#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class EnemyUnitDefinitionGeneratorWindow : EditorWindow
{
    private const string DefaultHumanUnitFolder = "Assets/UnitDefinition/Enemy/Human";
    private const string DefaultElfUnitFolder = "Assets/UnitDefinition/Enemy/Elf";
    private const string DefaultBossUnitFolder = "Assets/UnitDefinition/Enemy/Boss";

    private const string DefaultHumanViewFolder = "Assets/UnitViewDefinition/Enemy/Human";
    private const string DefaultElfViewFolder = "Assets/UnitViewDefinition/Enemy/Elf";
    private const string DefaultBossViewFolder = "Assets/UnitViewDefinition/Enemy/Boss";

    private const string DefaultEncounterFolder = "Assets/EnemyEncounterTable";
    private const string DefaultBossPartyFolder = "Assets/PartyDefinition/Enemy/Boss";

    private const string HumanSkillFolder = "Assets/SkillDefinition/EnemySkill/Human";
    private const string ElfSkillFolder = "Assets/SkillDefinition/EnemySkill/Elf";
    private const string BossHumanSkillFolder = "Assets/SkillDefinition/EnemySkill/Boss/Human";
    private const string BossDragonSkillFolder = "Assets/SkillDefinition/EnemySkill/Boss/Dragon";

    private enum ExistingAssetPolicy
    {
        CreateMissingOnly,
        UpdateExistingFields
    }

    [SerializeField] private ExistingAssetPolicy existingAssetPolicy = ExistingAssetPolicy.CreateMissingOnly;
    [SerializeField] private bool createUnitViewDefinitions = true;
    [SerializeField] private bool linkExistingSkillAssets = true;
    [SerializeField] private bool createEncounterTables = true;
    [SerializeField] private bool createBossPartyDefinitions = true;
    [SerializeField] private bool applyBossResistanceTable = false;

    [Header("Output Folders")]
    [SerializeField] private string humanUnitFolder = DefaultHumanUnitFolder;
    [SerializeField] private string elfUnitFolder = DefaultElfUnitFolder;
    [SerializeField] private string bossUnitFolder = DefaultBossUnitFolder;
    [SerializeField] private string humanViewFolder = DefaultHumanViewFolder;
    [SerializeField] private string elfViewFolder = DefaultElfViewFolder;
    [SerializeField] private string bossViewFolder = DefaultBossViewFolder;
    [SerializeField] private string encounterFolder = DefaultEncounterFolder;
    [SerializeField] private string bossPartyFolder = DefaultBossPartyFolder;

    [Header("Encounter Defaults")]
    [SerializeField, Min(1)] private int minEnemyCount = 2;
    [SerializeField, Min(1)] private int maxEnemyCount = 4;
    [SerializeField] private bool allowDuplicates = true;

    [MenuItem("Tools/Battle/Enemy Data/Generate Enemy Unit Definitions")]
    public static void Open()
    {
        GetWindow<EnemyUnitDefinitionGeneratorWindow>("Enemy Unit Data");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Enemy UnitDefinition / ViewDefinition Generator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "스킬 문서 기준 이름과 combat_spec 기준 1레벨 스탯으로 적 UnitDefinition을 생성합니다. " +
            "일반 저항은 0으로 생성하며, 보스 저항표는 옵션으로만 적용합니다.",
            MessageType.Info);

        existingAssetPolicy = (ExistingAssetPolicy)EditorGUILayout.EnumPopup("Existing Asset Policy", existingAssetPolicy);
        createUnitViewDefinitions = EditorGUILayout.Toggle("Create UnitViewDefinitions", createUnitViewDefinitions);
        linkExistingSkillAssets = EditorGUILayout.Toggle("Link Existing Skill Assets", linkExistingSkillAssets);
        createEncounterTables = EditorGUILayout.Toggle("Create Encounter Tables", createEncounterTables);
        createBossPartyDefinitions = EditorGUILayout.Toggle("Create Boss PartyDefinitions", createBossPartyDefinitions);
        applyBossResistanceTable = EditorGUILayout.Toggle("Apply Boss Resist Table", applyBossResistanceTable);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Folders", EditorStyles.boldLabel);
        humanUnitFolder = EditorGUILayout.TextField("Human Unit Folder", Fallback(humanUnitFolder, DefaultHumanUnitFolder));
        elfUnitFolder = EditorGUILayout.TextField("Elf Unit Folder", Fallback(elfUnitFolder, DefaultElfUnitFolder));
        bossUnitFolder = EditorGUILayout.TextField("Boss Unit Folder", Fallback(bossUnitFolder, DefaultBossUnitFolder));
        humanViewFolder = EditorGUILayout.TextField("Human View Folder", Fallback(humanViewFolder, DefaultHumanViewFolder));
        elfViewFolder = EditorGUILayout.TextField("Elf View Folder", Fallback(elfViewFolder, DefaultElfViewFolder));
        bossViewFolder = EditorGUILayout.TextField("Boss View Folder", Fallback(bossViewFolder, DefaultBossViewFolder));
        encounterFolder = EditorGUILayout.TextField("Encounter Folder", Fallback(encounterFolder, DefaultEncounterFolder));
        bossPartyFolder = EditorGUILayout.TextField("Boss Party Folder", Fallback(bossPartyFolder, DefaultBossPartyFolder));

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Encounter Defaults", EditorStyles.boldLabel);
        minEnemyCount = EditorGUILayout.IntSlider("Min Enemy Count", minEnemyCount, 1, 4);
        maxEnemyCount = EditorGUILayout.IntSlider("Max Enemy Count", Mathf.Max(minEnemyCount, maxEnemyCount), minEnemyCount, 4);
        allowDuplicates = EditorGUILayout.Toggle("Allow Duplicates", allowDuplicates);

        EditorGUILayout.Space(12f);
        if (GUILayout.Button("Generate Enemy Unit Data", GUILayout.Height(34f)))
            GenerateAll();
    }

    private void GenerateAll()
    {
        EnsureFolder(humanUnitFolder);
        EnsureFolder(elfUnitFolder);
        EnsureFolder(bossUnitFolder);

        if (createUnitViewDefinitions)
        {
            EnsureFolder(humanViewFolder);
            EnsureFolder(elfViewFolder);
            EnsureFolder(bossViewFolder);
        }

        if (createEncounterTables)
            EnsureFolder(encounterFolder);

        if (createBossPartyDefinitions)
            EnsureFolder(bossPartyFolder);

        Dictionary<string, GeneratedUnit> generated = new Dictionary<string, GeneratedUnit>();

        foreach (UnitSpec spec in BuildSpecs())
        {
            GeneratedUnit result = GenerateUnit(spec);
            generated[spec.unitId] = result;
        }

        if (createEncounterTables)
        {
            CreateEncounterTable("Human_Enemy_Encounter_Table", "Human Enemy Encounter Table", generated, BuildHumanEncounterEntries());
            CreateEncounterTable("Elf_Enemy_Encounter_Table", "Elf Enemy Encounter Table", generated, BuildElfEncounterEntries());
        }

        if (createBossPartyDefinitions)
        {
            CreateBossParty("Human_Boss_Judge_HighPriest_Party", "인간 보스 - 심판관 & 대사제", generated,
                new PartySlotSpec("boss_judge", 0, 7),
                new PartySlotSpec("boss_high_priest", 1, 7));

            CreateBossParty("Dragon_Boss_Aidra_Party", "엘프 보스 - 고룡 에이드라", generated,
                new PartySlotSpec("boss_aidra", 0, 9));
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[EnemyUnitDefinitionGenerator] Enemy UnitDefinitions, ViewDefinitions, EncounterTables and Boss Parties generated.");
    }

    private GeneratedUnit GenerateUnit(UnitSpec spec)
    {
        string unitFolder = GetUnitFolder(spec.faction);
        string viewFolder = GetViewFolder(spec.faction);

        UnitDefinition unit = LoadOrCreateAsset<UnitDefinition>(unitFolder, spec.assetName + "_UnitDefinition", out bool unitCreated);
        bool canModifyUnit = unitCreated || existingAssetPolicy == ExistingAssetPolicy.UpdateExistingFields;

        if (canModifyUnit && unit != null)
        {
            Undo.RecordObject(unit, "Generate Enemy UnitDefinition");
            ApplyUnitSpec(unit, spec);
            if (linkExistingSkillAssets)
                LinkSkills(unit, spec);
            EditorUtility.SetDirty(unit);
        }

        UnitViewDefinition view = null;
        if (createUnitViewDefinitions)
        {
            view = LoadOrCreateAsset<UnitViewDefinition>(viewFolder, spec.assetName + "_UnitViewDefinition", out bool viewCreated);
            bool canModifyView = viewCreated || existingAssetPolicy == ExistingAssetPolicy.UpdateExistingFields;
            if (canModifyView && view != null)
            {
                Undo.RecordObject(view, "Generate Enemy UnitViewDefinition");
                // Intentionally empty: sprites and prefab references are art-side data and should be assigned manually.
                EditorUtility.SetDirty(view);
            }
        }
        else
        {
            view = FindViewByAssetName(spec.assetName + "_UnitViewDefinition");
        }

        return new GeneratedUnit(unit, view);
    }

    private void ApplyUnitSpec(UnitDefinition unit, UnitSpec spec)
    {
        unit.unitId = spec.unitId;
        unit.unitName = spec.displayName;
        unit.rangeType = spec.rangeType;

        unit.isNftUnit = false;
        unit.showInLegion = false;
        unit.legionSortPriority = 0;
        unit.legionCategoryLabel = spec.categoryLabel;

        unit.maxHP = spec.maxHP;
        unit.dmg = spec.dmg;
        unit.spd = spec.spd;
        unit.idt = spec.idt;
        unit.hit = spec.hit;
        unit.ac = spec.ac;
        unit.cri = spec.cri;
        unit.crd = 150;

        unit.hpGrowthPerLevel = new Vector2Int(2, 4);
        unit.dmgGrowthPerLevel = new Vector2Int(1, 3);

        unit.burnResist = 0;
        unit.bleedResist = 0;
        unit.stunResist = 0;
        unit.frostResist = 0;
        unit.blindResist = 0;

        if (applyBossResistanceTable)
        {
            unit.burnResist = spec.burnResist;
            unit.bleedResist = spec.bleedResist;
            unit.stunResist = spec.stunResist;
            unit.frostResist = spec.frostResist;
            unit.blindResist = spec.blindResist;
        }

        unit.forcePositionMoveImmune = spec.forcePositionMoveImmune;
        unit.baseSoulReward = spec.baseSoulReward;
        unit.canBeCaptured = spec.canBeCaptured;
        unit.capturableEnemyNftChancePercent = 0f;
        unit.captureRewardItem = null;
        unit.canBeDecomposed = false;
        unit.decomposeShardReward = 1;
        unit.isMainPlayerCharacter = false;
    }

    private void LinkSkills(UnitDefinition unit, UnitSpec spec)
    {
        if (unit == null || spec == null)
            return;

        SkillDefinition basic = LoadSkill(spec.basicSkillId, spec.skillFolder);
        if (basic != null)
            unit.basicAttack = basic;

        unit.fixedStartingSkills = new List<SkillDefinition>();
        for (int i = 0; i < spec.learnedSkillIds.Length && unit.fixedStartingSkills.Count < 3; i++)
        {
            SkillDefinition skill = LoadSkill(spec.learnedSkillIds[i], spec.skillFolder);
            if (skill != null)
                unit.fixedStartingSkills.Add(skill);
        }
    }

    private SkillDefinition LoadSkill(string skillId, string preferredFolder)
    {
        if (string.IsNullOrWhiteSpace(skillId))
            return null;

        List<string> searchFolders = new List<string>();
        if (!string.IsNullOrWhiteSpace(preferredFolder) && AssetDatabase.IsValidFolder(preferredFolder))
            searchFolders.Add(preferredFolder);

        string[] guids = searchFolders.Count > 0
            ? AssetDatabase.FindAssets(skillId + " t:SkillDefinition", searchFolders.ToArray())
            : AssetDatabase.FindAssets(skillId + " t:SkillDefinition");

        SkillDefinition fallback = null;
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            SkillDefinition skill = AssetDatabase.LoadAssetAtPath<SkillDefinition>(path);
            if (skill == null)
                continue;

            if (skill.skillId == skillId)
                return skill;

            if (fallback == null)
                fallback = skill;
        }

        return fallback;
    }

    private UnitViewDefinition FindViewByAssetName(string assetName)
    {
        string[] guids = AssetDatabase.FindAssets(assetName + " t:UnitViewDefinition");
        if (guids == null || guids.Length == 0)
            return null;

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<UnitViewDefinition>(path);
    }

    private void CreateEncounterTable(string assetName, string displayName, Dictionary<string, GeneratedUnit> generated, List<EncounterSpec> entries)
    {
        EnemyEncounterTable table = LoadOrCreateAsset<EnemyEncounterTable>(encounterFolder, assetName, out bool created);
        bool canModify = created || existingAssetPolicy == ExistingAssetPolicy.UpdateExistingFields;
        if (!canModify || table == null)
            return;

        Undo.RecordObject(table, "Generate Enemy Encounter Table");
        table.name = displayName;
        table.minEnemyCount = Mathf.Clamp(minEnemyCount, 1, 4);
        table.maxEnemyCount = Mathf.Clamp(maxEnemyCount, table.minEnemyCount, 4);
        table.allowDuplicates = allowDuplicates;
        table.entries = new List<EnemyEncounterEntry>();

        for (int i = 0; i < entries.Count; i++)
        {
            EncounterSpec spec = entries[i];
            if (!generated.TryGetValue(spec.unitId, out GeneratedUnit unit) || unit.unit == null)
                continue;

            EnemyEncounterEntry entry = new EnemyEncounterEntry();
            entry.unitDefinition = unit.unit;
            entry.unitViewDefinition = unit.view;
            entry.weight = Mathf.Max(1, spec.weight);
            entry.minLevel = 1;
            entry.maxLevel = 1;
            entry.learnedSkills = unit.unit.fixedStartingSkills != null
                ? new List<SkillDefinition>(unit.unit.fixedStartingSkills)
                : new List<SkillDefinition>();
            entry.instanceDisplayNameOverride = string.Empty;
            entry.fixedEpitaph = string.Empty;
            entry.enabled = true;
            table.entries.Add(entry);
        }

        EditorUtility.SetDirty(table);
    }

    private void CreateBossParty(string assetName, string partyName, Dictionary<string, GeneratedUnit> generated, params PartySlotSpec[] slots)
    {
        PartyDefinition party = LoadOrCreateAsset<PartyDefinition>(bossPartyFolder, assetName, out bool created);
        bool canModify = created || existingAssetPolicy == ExistingAssetPolicy.UpdateExistingFields;
        if (!canModify || party == null)
            return;

        Undo.RecordObject(party, "Generate Boss Party Definition");
        party.partyName = partyName;
        party.inventory = new List<InventoryStackData>();
        party.members = new List<PartyMemberData>();

        for (int i = 0; i < slots.Length && party.members.Count < 4; i++)
        {
            PartySlotSpec slot = slots[i];
            if (!generated.TryGetValue(slot.unitId, out GeneratedUnit unit) || unit.unit == null)
                continue;

            PartyMemberData member = new PartyMemberData();
            member.unitDefinition = unit.unit;
            member.unitViewDefinition = unit.view;
            member.startSlotIndex = Mathf.Clamp(slot.slotIndex, 0, 3);
            member.instanceId = slot.unitId + "_boss_slot_" + member.startSlotIndex;
            member.instanceDisplayNameOverride = string.Empty;
            member.fixedEpitaph = string.Empty;
            member.isExchangeable = false;
            member.isNft = false;
            member.currentLevel = Mathf.Max(1, slot.level);
            member.originalLevel = Mathf.Max(1, slot.level);
            member.currentExp = 0;
            member.levelGrowthMaxHp = 0;
            member.levelGrowthDmg = 0;
            member.promotionRank = 1;
            member.promotionBonusPercentPerRank = 1f;
            member.statVariance = new UnitInstanceStatVariance();
            member.learnedSkills = unit.unit.fixedStartingSkills != null
                ? new List<SkillDefinition>(unit.unit.fixedStartingSkills)
                : new List<SkillDefinition>();
            member.equippedItems = new List<ItemDefinition>();
            member.battleLootDrops = new List<ItemDropDefinition>();
            member.persistentCurrentHP = -1;
            party.members.Add(member);
        }

        EditorUtility.SetDirty(party);
    }

    private T LoadOrCreateAsset<T>(string folder, string assetName, out bool created) where T : ScriptableObject
    {
        created = false;
        folder = Fallback(folder, "Assets");
        EnsureFolder(folder);

        string safeName = SanitizeFileName(assetName);
        string path = Path.Combine(folder, safeName + ".asset").Replace("\\", "/");
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null)
            return asset;

        asset = CreateInstance<T>();
        asset.name = safeName;
        AssetDatabase.CreateAsset(asset, path);
        created = true;
        return asset;
    }

    private static string SanitizeFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "GeneratedAsset";

        char[] invalid = Path.GetInvalidFileNameChars();
        string result = value.Trim();
        for (int i = 0; i < invalid.Length; i++)
            result = result.Replace(invalid[i], '_');

        return result.Replace(' ', '_');
    }

    private static string Fallback(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private void EnsureFolder(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder))
            return;

        folder = folder.Replace("\\", "/").Trim('/');
        string[] parts = folder.Split('/');
        if (parts.Length == 0 || parts[0] != "Assets")
        {
            Debug.LogWarning("[EnemyUnitDefinitionGenerator] Folder must start with Assets: " + folder);
            return;
        }

        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private string GetUnitFolder(EnemyFaction faction)
    {
        switch (faction)
        {
            case EnemyFaction.Human: return Fallback(humanUnitFolder, DefaultHumanUnitFolder);
            case EnemyFaction.Elf: return Fallback(elfUnitFolder, DefaultElfUnitFolder);
            default: return Fallback(bossUnitFolder, DefaultBossUnitFolder);
        }
    }

    private string GetViewFolder(EnemyFaction faction)
    {
        switch (faction)
        {
            case EnemyFaction.Human: return Fallback(humanViewFolder, DefaultHumanViewFolder);
            case EnemyFaction.Elf: return Fallback(elfViewFolder, DefaultElfViewFolder);
            default: return Fallback(bossViewFolder, DefaultBossViewFolder);
        }
    }

    private List<UnitSpec> BuildSpecs()
    {
        List<UnitSpec> specs = new List<UnitSpec>();

        specs.Add(new UnitSpec(EnemyFaction.Human, "human_farmer", "농부", "Human_Farmer", CharacterRangeType.Melee, 45, 10, 90, 44f, 28f, 2, 0, 95, false, false, HumanSkillFolder,
            "human_farmer_thrust", "human_farmer_stone_throw", "human_farmer_flee_next_turn"));
        specs.Add(new UnitSpec(EnemyFaction.Human, "human_guard", "경비병", "Human_Guard", CharacterRangeType.Melee, 115, 18, 48, 56f, 38f, 4, 10, 145, false, false, HumanSkillFolder,
            "human_guard_thrust", "human_guard_shield_bash", "human_guard_horn"));
        specs.Add(new UnitSpec(EnemyFaction.Human, "human_priest", "사제", "Human_Priest", CharacterRangeType.Ranged, 75, 12, 52, 58f, 34f, 4, 0, 125, true, false, HumanSkillFolder,
            "human_priest_holy_light", "human_priest_prayer", "human_priest_scripture"));
        specs.Add(new UnitSpec(EnemyFaction.Human, "human_crossbowman", "석궁병", "Human_Crossbowman", CharacterRangeType.Ranged, 70, 22, 60, 64f, 32f, 6, 0, 130, false, false, HumanSkillFolder,
            "human_crossbow_shot", "human_crossbow_piercing_shot", "human_crossbow_retreat_shot"));
        specs.Add(new UnitSpec(EnemyFaction.Human, "human_royal_alchemist", "왕립 연금술사", "Human_Royal_Alchemist", CharacterRangeType.Mid, 68, 14, 56, 60f, 34f, 4, 0, 120, false, false, HumanSkillFolder,
            "human_alchemist_explosive_potion", "human_alchemist_chemical_cloud", "human_alchemist_healing_potion"));
        specs.Add(new UnitSpec(EnemyFaction.Human, "human_paladin", "성기사", "Human_Paladin", CharacterRangeType.Melee, 135, 26, 42, 54f, 40f, 6, 10, 170, true, false, HumanSkillFolder,
            "human_paladin_holy_smite", "human_paladin_brave_charge", "human_paladin_guardian_shield"));

        specs.Add(new UnitSpec(EnemyFaction.Elf, "elf_fairy", "페어리", "Elf_Fairy", CharacterRangeType.Ranged, 38, 9, 102, 50f, 36f, 4, 0, 90, false, false, ElfSkillFolder,
            "elf_fairy_magic_prank"));
        specs.Add(new UnitSpec(EnemyFaction.Elf, "elf_dryad", "드라이어드", "Elf_Dryad", CharacterRangeType.Melee, 125, 20, 40, 52f, 44f, 4, 10, 150, false, false, ElfSkillFolder,
            "elf_dryad_swing", "elf_dryad_root", "elf_dryad_regeneration"));
        specs.Add(new UnitSpec(EnemyFaction.Elf, "elf_sword_dancer", "검의 무희", "Elf_Sword_Dancer", CharacterRangeType.Mid, 75, 28, 78, 66f, 46f, 12, 0, 135, true, false, ElfSkillFolder,
            "elf_sword_dancer_double_attack", "elf_sword_dancer_battle_stance", "elf_sword_dancer_sword_dance"));
        specs.Add(new UnitSpec(EnemyFaction.Elf, "elf_hunter", "사냥꾼", "Elf_Hunter", CharacterRangeType.Mid, 72, 24, 64, 62f, 42f, 8, 0, 130, true, false, ElfSkillFolder,
            "elf_hunter_snipe", "elf_hunter_mark", "elf_hunter_rapid_shot"));
        specs.Add(new UnitSpec(EnemyFaction.Elf, "elf_spirit_deer", "정령 사슴", "Elf_Spirit_Deer", CharacterRangeType.Melee, 155, 24, 36, 52f, 42f, 4, 10, 180, false, false, ElfSkillFolder,
            "elf_spirit_deer_ram", "elf_spirit_deer_stomp", "elf_spirit_deer_purification"));
        specs.Add(new UnitSpec(EnemyFaction.Elf, "elf_druid", "드루이드", "Elf_Druid", CharacterRangeType.Mid, 80, 16, 50, 56f, 32f, 4, 0, 125, false, false, ElfSkillFolder,
            "elf_druid_entangle", "elf_druid_gale", "elf_druid_call_of_forest"));
        specs.Add(new UnitSpec(EnemyFaction.Elf, "elf_mage", "마법사", "Elf_Mage", CharacterRangeType.Ranged, 62, 30, 54, 68f, 28f, 10, 0, 135, false, false, ElfSkillFolder,
            "elf_mage_fireball", "elf_mage_ice_barrier", "elf_mage_arcane_explosion"));

        UnitSpec judge = new UnitSpec(EnemyFaction.Boss, "boss_judge", "심판관", "Boss_Judge", CharacterRangeType.Melee, 580, 43, 38, 62f, 46f, 8, 15, 450, false, true, BossHumanSkillFolder,
            "human_boss_judge_basic", "human_boss_judge_righteous_revenge", "human_boss_judge_enrage_when_high_priest_dies");
        judge.SetBossResists(25, 30, 50, 0, 0); // burn, bleed, stun, frost, blind. 구버전 중독 저항은 현재 화상 저항으로 이관.
        specs.Add(judge);

        UnitSpec highPriest = new UnitSpec(EnemyFaction.Boss, "boss_high_priest", "대사제", "Boss_High_Priest", CharacterRangeType.Ranged, 320, 25, 55, 58f, 36f, 4, 0, 350, false, false, BossHumanSkillFolder,
            "human_boss_high_priest_basic", "human_boss_high_priest_chain_of_penitence", "human_boss_high_priest_confession", "human_boss_high_priest_revive_judge");
        highPriest.SetBossResists(15, 15, 30, 0, 0);
        specs.Add(highPriest);

        UnitSpec dragon = new UnitSpec(EnemyFaction.Boss, "boss_aidra", "고룡 에이드라", "Boss_Aidra", CharacterRangeType.Mid, 720, 48, 30, 64f, 48f, 6, 10, 800, false, true, BossDragonSkillFolder,
            "dragon_boss_claw", "dragon_boss_stomp", "dragon_boss_summon_dragon_soldier");
        dragon.SetBossResists(50, 40, 70, 0, 0);
        specs.Add(dragon);

        UnitSpec dragonSoldier = new UnitSpec(EnemyFaction.Boss, "dragon_soldier", "용아병", "Dragon_Soldier", CharacterRangeType.Melee, 55, 16, 75, 58f, 32f, 0, 0, 90, false, false, BossDragonSkillFolder,
            "dragon_soldier_spear", "dragon_soldier_worship");
        dragonSoldier.SetBossResists(50, 0, 20, 0, 0);
        specs.Add(dragonSoldier);

        return specs;
    }

    private List<EncounterSpec> BuildHumanEncounterEntries()
    {
        return new List<EncounterSpec>
        {
            new EncounterSpec("human_farmer", 3),
            new EncounterSpec("human_guard", 1),
            new EncounterSpec("human_priest", 2),
            new EncounterSpec("human_crossbowman", 2),
            new EncounterSpec("human_royal_alchemist", 2),
            new EncounterSpec("human_paladin", 1)
        };
    }

    private List<EncounterSpec> BuildElfEncounterEntries()
    {
        return new List<EncounterSpec>
        {
            new EncounterSpec("elf_fairy", 3),
            new EncounterSpec("elf_dryad", 1),
            new EncounterSpec("elf_sword_dancer", 2),
            new EncounterSpec("elf_hunter", 2),
            new EncounterSpec("elf_spirit_deer", 1),
            new EncounterSpec("elf_druid", 2),
            new EncounterSpec("elf_mage", 2)
        };
    }

    private enum EnemyFaction
    {
        Human,
        Elf,
        Boss
    }

    private class UnitSpec
    {
        public readonly EnemyFaction faction;
        public readonly string unitId;
        public readonly string displayName;
        public readonly string assetName;
        public readonly CharacterRangeType rangeType;
        public readonly int maxHP;
        public readonly int dmg;
        public readonly int spd;
        public readonly float hit;
        public readonly float ac;
        public readonly int cri;
        public readonly int idt;
        public readonly int baseSoulReward;
        public readonly bool canBeCaptured;
        public readonly bool forcePositionMoveImmune;
        public readonly string skillFolder;
        public readonly string basicSkillId;
        public readonly string[] learnedSkillIds;
        public readonly string categoryLabel;

        public int burnResist;
        public int bleedResist;
        public int stunResist;
        public int frostResist;
        public int blindResist;

        public UnitSpec(
            EnemyFaction faction,
            string unitId,
            string displayName,
            string assetName,
            CharacterRangeType rangeType,
            int maxHP,
            int dmg,
            int spd,
            float hit,
            float ac,
            int cri,
            int idt,
            int baseSoulReward,
            bool canBeCaptured,
            bool forcePositionMoveImmune,
            string skillFolder,
            string basicSkillId,
            params string[] learnedSkillIds)
        {
            this.faction = faction;
            this.unitId = unitId;
            this.displayName = displayName;
            this.assetName = assetName;
            this.rangeType = rangeType;
            this.maxHP = maxHP;
            this.dmg = dmg;
            this.spd = spd;
            this.hit = hit;
            this.ac = ac;
            this.cri = cri;
            this.idt = idt;
            this.baseSoulReward = baseSoulReward;
            this.canBeCaptured = canBeCaptured;
            this.forcePositionMoveImmune = forcePositionMoveImmune;
            this.skillFolder = skillFolder;
            this.basicSkillId = basicSkillId;
            this.learnedSkillIds = learnedSkillIds ?? new string[0];
            this.categoryLabel = faction.ToString();
        }

        public void SetBossResists(int burn, int bleed, int stun, int frost, int blind)
        {
            burnResist = Mathf.Max(0, burn);
            bleedResist = Mathf.Max(0, bleed);
            stunResist = Mathf.Max(0, stun);
            frostResist = Mathf.Max(0, frost);
            blindResist = Mathf.Max(0, blind);
        }
    }

    private readonly struct GeneratedUnit
    {
        public readonly UnitDefinition unit;
        public readonly UnitViewDefinition view;

        public GeneratedUnit(UnitDefinition unit, UnitViewDefinition view)
        {
            this.unit = unit;
            this.view = view;
        }
    }

    private readonly struct EncounterSpec
    {
        public readonly string unitId;
        public readonly int weight;

        public EncounterSpec(string unitId, int weight)
        {
            this.unitId = unitId;
            this.weight = weight;
        }
    }

    private readonly struct PartySlotSpec
    {
        public readonly string unitId;
        public readonly int slotIndex;
        public readonly int level;

        public PartySlotSpec(string unitId, int slotIndex, int level)
        {
            this.unitId = unitId;
            this.slotIndex = slotIndex;
            this.level = level;
        }
    }
}
#endif
