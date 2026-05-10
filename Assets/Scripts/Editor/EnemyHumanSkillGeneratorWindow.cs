#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class EnemyHumanSkillGeneratorWindow : EditorWindow
{
    private const string DefaultFolder = "Assets/SkillDefinition/EnemySkill/Human";

    [SerializeField] private string outputFolder = DefaultFolder;
    [SerializeField] private bool overwriteExisting = false;
    [SerializeField] private bool assignToUnitDefinitions = true;

    [Header("Human Enemy UnitDefinitions")]
    [SerializeField] private UnitDefinition farmer;
    [SerializeField] private UnitDefinition guard;
    [SerializeField] private UnitDefinition priest;
    [SerializeField] private UnitDefinition crossbowman;
    [SerializeField] private UnitDefinition royalAlchemist;
    [SerializeField] private UnitDefinition paladin;

    [MenuItem("Tools/Battle/Enemy Skills/Generate Human Enemy Skills")]
    public static void Open()
    {
        GetWindow<EnemyHumanSkillGeneratorWindow>("Human Enemy Skills");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Human Enemy Skill Generator", EditorStyles.boldLabel);
        outputFolder = EditorGUILayout.TextField("Output Folder", string.IsNullOrWhiteSpace(outputFolder) ? DefaultFolder : outputFolder);
        overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing", overwriteExisting);
        assignToUnitDefinitions = EditorGUILayout.Toggle("Assign To UnitDefinitions", assignToUnitDefinitions);

        EditorGUILayout.Space(8f);
        farmer = (UnitDefinition)EditorGUILayout.ObjectField("농부", farmer, typeof(UnitDefinition), false);
        guard = (UnitDefinition)EditorGUILayout.ObjectField("경비병", guard, typeof(UnitDefinition), false);
        priest = (UnitDefinition)EditorGUILayout.ObjectField("사제", priest, typeof(UnitDefinition), false);
        crossbowman = (UnitDefinition)EditorGUILayout.ObjectField("석궁병", crossbowman, typeof(UnitDefinition), false);
        royalAlchemist = (UnitDefinition)EditorGUILayout.ObjectField("왕립 연금술사", royalAlchemist, typeof(UnitDefinition), false);
        paladin = (UnitDefinition)EditorGUILayout.ObjectField("성기사", paladin, typeof(UnitDefinition), false);

        EditorGUILayout.Space(10f);
        if (GUILayout.Button("Generate / Assign Human Enemy Skills", GUILayout.Height(34f)))
            GenerateAll();
    }

    private void GenerateAll()
    {
        EnsureFolder(outputFolder);

        SkillSet farmerSkills = CreateFarmerSkills();
        SkillSet guardSkills = CreateGuardSkills();
        SkillSet priestSkills = CreatePriestSkills();
        SkillSet crossbowSkills = CreateCrossbowmanSkills();
        SkillSet alchemistSkills = CreateRoyalAlchemistSkills();
        SkillSet paladinSkills = CreatePaladinSkills();

        if (assignToUnitDefinitions)
        {
            Assign(farmer, farmerSkills);
            Assign(guard, guardSkills);
            Assign(priest, priestSkills);
            Assign(crossbowman, crossbowSkills);
            Assign(royalAlchemist, alchemistSkills);
            Assign(paladin, paladinSkills);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[EnemyHumanSkillGenerator] Human enemy skills generated.");
    }

    private SkillSet CreateFarmerSkills()
    {
        SkillDefinition basic = CreateAttack("human_farmer_thrust", "내지르기", true, 0, 1, 0, 1, 0, 100);
        SkillDefinition stone = CreateAttack("human_farmer_stone_throw", "투석", false, 2, 3, 0, 2, 1, 70,
            StatusEffectType.Stun, 90f, 1);
        SkillDefinition flee = CreateSelfUtility("human_farmer_flee_next_turn", "줄행랑", ActiveSkillGimmick.FleeOnNextOwnTurn, 0);
        flee.onlyUsableWhenAlone = true;
        flee.description = "아군 진형에 자신만 남았을 때 사용합니다. 다음 자기 턴 시작 시 도주합니다.";
        return new SkillSet(basic, stone, flee);
    }

    private SkillSet CreateGuardSkills()
    {
        SkillDefinition basic = CreateAttack("human_guard_thrust", "찌르기", true, 0, 1, 0, 1, 0, 100);
        SkillDefinition shield = CreateAttack("human_guard_shield_bash", "방패 타격", false, 0, 1, 0, 1, 2, 80,
            StatusEffectType.Stun, 100f, 1);
        SkillDefinition horn = CreateSelfUtility("human_guard_horn", "호각", ActiveSkillGimmick.DelayedReinforcement, 0);
        horn.delayedReinforcementDelayRounds = 2;
        horn.disableAfterUseInBattle = true;
        horn.description = "같은 팀 유닛이 죽어 3명 이하가 된 전투에서 사용 가능합니다. 사용 후 2턴 뒤 경비병 1명을 소환합니다.";
        return new SkillSet(basic, shield, horn);
    }

    private SkillSet CreatePriestSkills()
    {
        SkillDefinition basic = CreateAttack("human_priest_holy_light", "성스러운 빛", true, 1, 3, 1, 3, 0, 100,
            StatusEffectType.Blind, 20f, 1);
        SkillDefinition prayer = CreateSuccessSkill("human_priest_prayer", "신실한 기도", 2, 3, SkillTargetTeam.Ally, TargetScope.All, 2,
            Effect(BattleEffectKind.Heal, 100, EffectValueReference.ActorDMG));
        prayer.description = "공격력의 100%만큼 아군 전체를 치유합니다.";
        SkillDefinition scripture = CreateSuccessSkill("human_priest_scripture", "성서 낭독", 0, 3, SkillTargetTeam.Ally, TargetScope.Single, 2,
            Buff(StatModifierType.IDT, 20, 2));
        scripture.description = "아군 1명에게 2턴간 IDT 20%를 부여합니다.";
        return new SkillSet(basic, prayer, scripture);
    }

    private SkillSet CreateCrossbowmanSkills()
    {
        SkillDefinition basic = CreateAttack("human_crossbow_shot", "사격", true, 1, 3, 0, 3, 0, 100);
        SkillDefinition pierce = CreateAttack("human_crossbow_piercing_shot", "관통 사격", false, 2, 3, 0, 3, 2, 70,
            StatusEffectType.Bleed, 20f, 1);
        pierce.secondaryTargetRule = SecondaryTargetRule.BackOne;
        pierce.secondaryDamagePercent = 70f;
        pierce.secondaryAccuracyCoefficientPercent = 100f;
        pierce.secondaryApplyNonDamageEffects = true;
        pierce.description = "지정 대상과 그 뒤 1열을 공격합니다. 피해 70%, 출혈 20% 확률.";
        SkillDefinition retreat = CreateAttack("human_crossbow_retreat_shot", "후퇴 사격", false, 0, 2, 0, 1, 1, 50);
        retreat.selfMoveDirection = SkillSelfMoveDirection.Backward;
        retreat.selfMoveSteps = 1;
        retreat.description = "피해 50%. 사용 후 1칸 뒤로 이동합니다.";
        return new SkillSet(basic, pierce, retreat);
    }

    private SkillSet CreateRoyalAlchemistSkills()
    {
        SkillDefinition basic = CreateAttack("human_alchemist_explosive_potion", "폭발 물약", true, 0, 2, 0, 2, 0, 100,
            StatusEffectType.Burn, 80f, 1);
        SkillDefinition cloud = CreateAttack("human_alchemist_chemical_cloud", "화학 구름", false, 1, 3, 0, 3, 2, 20);
        cloud.targetScope = TargetScope.All;
        cloud.activeGimmick = ActiveSkillGimmick.RandomRepositionTargetsOnHit;
        cloud.randomRepositionChancePercent = 20f;
        cloud.description = "적 전체에게 피해 20%. 피격한 적을 20% 확률로 무작위 위치에 강제배치합니다.";
        SkillDefinition heal = CreateSuccessSkill("human_alchemist_healing_potion", "치유 물약", 0, 3, SkillTargetTeam.Ally, TargetScope.Single, 2,
            Effect(BattleEffectKind.Heal, 20, EffectValueReference.TargetMaxHP));
        heal.description = "대상의 최대 체력 20%를 회복합니다.";
        return new SkillSet(basic, cloud, heal);
    }

    private SkillSet CreatePaladinSkills()
    {
        SkillDefinition basic = CreateAttack("human_paladin_holy_smite", "신성한 강타", true, 0, 0, 0, 1, 0, 100,
            StatusEffectType.Stun, 30f, 1);
        SkillDefinition charge = CreateAttack("human_paladin_brave_charge", "용감한 돌진", false, 1, 2, 0, 0, 2, 60);
        charge.selfMoveDirection = SkillSelfMoveDirection.Forward;
        charge.selfMoveSteps = 2;
        charge.description = "피해 60%. 사용 후 전방 2칸 전진합니다.";
        SkillDefinition shield = CreateSuccessSkill("human_paladin_guardian_shield", "수호 방패", 0, 2, SkillTargetTeam.Ally, TargetScope.All, 2,
            Effect(BattleEffectKind.Shield, 100, EffectValueReference.ActorDMG));
        shield.description = "공격력의 100%만큼 아군 전체에게 영구 보호막을 생성합니다.";
        return new SkillSet(basic, charge, shield);
    }

    private SkillDefinition CreateAttack(string id, string displayName, bool isBasic, int usableMin, int usableMax, int targetMin, int targetMax, int cooldown, int damagePercent,
        StatusEffectType status = StatusEffectType.None, float statusChance = 0f, int statusDuration = 0)
    {
        SkillDefinition skill = LoadOrCreate(id, displayName);
        skill.skillId = id;
        skill.skillName = displayName;
        skill.description = BuildAttackDescription(damagePercent, status, statusChance);
        skill.isBasicAttack = isBasic;
        skill.castType = SkillCastType.Active;
        skill.activeRole = ActiveSkillRole.Attack;
        skill.skillClass = isBasic ? SkillClass.Common : SkillClass.Melee;
        skill.rangeTag = CharacterRangeType.Melee;
        skill.passiveGimmick = PassiveSkillGimmick.None;
        skill.activeGimmick = ActiveSkillGimmick.None;
        skill.usableMinSlotIndex = usableMin;
        skill.usableMaxSlotIndex = usableMax;
        skill.targetMinSlotIndex = targetMin;
        skill.targetMaxSlotIndex = targetMax;
        skill.targetTeam = SkillTargetTeam.Enemy;
        skill.targetScope = TargetScope.Single;
        skill.resolutionMode = SkillResolutionMode.Attack;
        skill.cooldownTurns = cooldown;
        skill.initialCooldownTurns = 0;
        skill.accuracyCoefficientPercent = 100f;
        skill.allowCrit = true;
        skill.allowGraze = true;
        skill.effects = new List<BattleEffectBlock> { Effect(BattleEffectKind.Damage, damagePercent, EffectValueReference.ActorDMG) };
        if (status != StatusEffectType.None && statusChance > 0f && statusDuration > 0)
            skill.effects.Add(Status(status, statusChance, statusDuration));
        EditorUtility.SetDirty(skill);
        return skill;
    }

    private SkillDefinition CreateSuccessSkill(string id, string displayName, int usableMin, int usableMax, SkillTargetTeam team, TargetScope scope, int cooldown, params BattleEffectBlock[] effects)
    {
        SkillDefinition skill = LoadOrCreate(id, displayName);
        skill.skillId = id;
        skill.skillName = displayName;
        skill.isBasicAttack = false;
        skill.castType = SkillCastType.Active;
        skill.activeRole = ActiveSkillRole.Buff;
        skill.skillClass = SkillClass.Common;
        skill.passiveGimmick = PassiveSkillGimmick.None;
        skill.activeGimmick = ActiveSkillGimmick.None;
        skill.usableMinSlotIndex = usableMin;
        skill.usableMaxSlotIndex = usableMax;
        skill.targetMinSlotIndex = 0;
        skill.targetMaxSlotIndex = 3;
        skill.targetTeam = team;
        skill.targetScope = scope;
        skill.resolutionMode = SkillResolutionMode.SuccessOnly;
        skill.cooldownTurns = cooldown;
        skill.initialCooldownTurns = 0;
        skill.allowCrit = false;
        skill.allowGraze = false;
        skill.effects = new List<BattleEffectBlock>(effects);
        EditorUtility.SetDirty(skill);
        return skill;
    }

    private SkillDefinition CreateSelfUtility(string id, string displayName, ActiveSkillGimmick gimmick, int cooldown)
    {
        SkillDefinition skill = LoadOrCreate(id, displayName);
        skill.skillId = id;
        skill.skillName = displayName;
        skill.isBasicAttack = false;
        skill.castType = SkillCastType.Active;
        skill.activeRole = ActiveSkillRole.Utility;
        skill.skillClass = SkillClass.Common;
        skill.targetTeam = SkillTargetTeam.Self;
        skill.targetScope = TargetScope.Single;
        skill.usableMinSlotIndex = 0;
        skill.usableMaxSlotIndex = 3;
        skill.targetMinSlotIndex = 0;
        skill.targetMaxSlotIndex = 3;
        skill.resolutionMode = SkillResolutionMode.SuccessOnly;
        skill.cooldownTurns = cooldown;
        skill.activeGimmick = gimmick;
        skill.effects = new List<BattleEffectBlock>();
        EditorUtility.SetDirty(skill);
        return skill;
    }

    private BattleEffectBlock Effect(BattleEffectKind kind, int percent, EffectValueReference reference)
    {
        return new BattleEffectBlock
        {
            kind = kind,
            powerPercent = percent,
            valueReference = reference,
            successChancePercent = 100f,
            affectedByResistance = false,
            durationTurns = 0
        };
    }

    private BattleEffectBlock Status(StatusEffectType status, float chance, int duration)
    {
        return new BattleEffectBlock
        {
            kind = BattleEffectKind.ApplyStatus,
            statusType = status,
            successChancePercent = chance,
            affectedByResistance = true,
            durationTurns = duration
        };
    }

    private BattleEffectBlock Buff(StatModifierType stat, int percent, int duration)
    {
        return new BattleEffectBlock
        {
            kind = BattleEffectKind.Buff,
            statModifierType = stat,
            flatValue = percent,
            durationTurns = duration,
            successChancePercent = 100f,
            affectedByResistance = false
        };
    }

    private string BuildAttackDescription(int damagePercent, StatusEffectType status, float chance)
    {
        if (status == StatusEffectType.None || chance <= 0f)
            return $"피해 {damagePercent}%";
        return $"피해 {damagePercent}%, {BattleStatusUtility.GetDisplayName(status)} {Mathf.RoundToInt(chance)}% 확률";
    }

    private SkillDefinition LoadOrCreate(string id, string displayName)
    {
        string safeName = id + "_" + displayName;
        foreach (char c in Path.GetInvalidFileNameChars())
            safeName = safeName.Replace(c, '_');

        string path = outputFolder.TrimEnd('/') + "/" + safeName + ".asset";
        SkillDefinition existing = AssetDatabase.LoadAssetAtPath<SkillDefinition>(path);
        if (existing != null)
        {
            if (!overwriteExisting)
                return existing;

            Undo.RecordObject(existing, "Overwrite SkillDefinition");
            return existing;
        }

        SkillDefinition skill = CreateInstance<SkillDefinition>();
        AssetDatabase.CreateAsset(skill, path);
        return skill;
    }

    private void Assign(UnitDefinition unit, SkillSet skills)
    {
        if (unit == null || skills == null || skills.basicAttack == null)
            return;

        Undo.RecordObject(unit, "Assign Human Enemy Skills");
        unit.basicAttack = skills.basicAttack;
        unit.fixedStartingSkills = new List<SkillDefinition>();
        for (int i = 0; i < skills.learnedSkills.Count && unit.fixedStartingSkills.Count < 3; i++)
        {
            if (skills.learnedSkills[i] != null)
                unit.fixedStartingSkills.Add(skills.learnedSkills[i]);
        }
        EditorUtility.SetDirty(unit);
    }

    private void EnsureFolder(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder))
            folder = DefaultFolder;

        string[] parts = folder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private class SkillSet
    {
        public SkillDefinition basicAttack;
        public List<SkillDefinition> learnedSkills = new List<SkillDefinition>();

        public SkillSet(SkillDefinition basic, params SkillDefinition[] learned)
        {
            basicAttack = basic;
            if (learned != null)
                learnedSkills.AddRange(learned);
        }
    }
}
#endif
