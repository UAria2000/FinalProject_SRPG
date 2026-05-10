#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class EnemyBossSkillGeneratorWindow : EditorWindow
{
    private const string DefaultHumanFolder = "Assets/SkillDefinition/EnemySkill/Boss/Human";
    private const string DefaultDragonFolder = "Assets/SkillDefinition/EnemySkill/Boss/Dragon";

    [SerializeField] private string humanOutputFolder = DefaultHumanFolder;
    [SerializeField] private string dragonOutputFolder = DefaultDragonFolder;
    [SerializeField] private bool overwriteExisting = false;
    [SerializeField] private bool assignToUnitDefinitions = true;

    [Header("Human Boss UnitDefinitions")]
    [SerializeField] private UnitDefinition judge;
    [SerializeField] private UnitDefinition highPriest;

    [Header("Dragon Boss UnitDefinitions")]
    [SerializeField] private UnitDefinition dragon;
    [SerializeField] private UnitDefinition dragonSoldier;

    [Header("Summon ViewDefinition")]
    [SerializeField] private UnitViewDefinition summonedDragonSoldierView;

    [MenuItem("Tools/Battle/Enemy Skills/Generate Boss Enemy Skills")]
    public static void Open()
    {
        GetWindow<EnemyBossSkillGeneratorWindow>("Boss Enemy Skills");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Boss Enemy Skill Generator", EditorStyles.boldLabel);
        humanOutputFolder = EditorGUILayout.TextField("Human Output Folder", string.IsNullOrWhiteSpace(humanOutputFolder) ? DefaultHumanFolder : humanOutputFolder);
        dragonOutputFolder = EditorGUILayout.TextField("Dragon Output Folder", string.IsNullOrWhiteSpace(dragonOutputFolder) ? DefaultDragonFolder : dragonOutputFolder);
        overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing", overwriteExisting);
        assignToUnitDefinitions = EditorGUILayout.Toggle("Assign To UnitDefinitions", assignToUnitDefinitions);

        EditorGUILayout.Space(8f);
        judge = (UnitDefinition)EditorGUILayout.ObjectField("심판관", judge, typeof(UnitDefinition), false);
        highPriest = (UnitDefinition)EditorGUILayout.ObjectField("대사제", highPriest, typeof(UnitDefinition), false);

        EditorGUILayout.Space(8f);
        dragon = (UnitDefinition)EditorGUILayout.ObjectField("드래곤/고룡", dragon, typeof(UnitDefinition), false);
        dragonSoldier = (UnitDefinition)EditorGUILayout.ObjectField("용아병", dragonSoldier, typeof(UnitDefinition), false);
        summonedDragonSoldierView = (UnitViewDefinition)EditorGUILayout.ObjectField("소환 용아병 View", summonedDragonSoldierView, typeof(UnitViewDefinition), false);

        EditorGUILayout.Space(10f);
        if (GUILayout.Button("Generate / Assign Boss Enemy Skills", GUILayout.Height(34f)))
            GenerateAll();
    }

    private void GenerateAll()
    {
        EnsureFolder(humanOutputFolder);
        EnsureFolder(dragonOutputFolder);

        SkillSet judgeSkills = CreateJudgeSkills();
        SkillSet highPriestSkills = CreateHighPriestSkills();
        SkillSet dragonSkills = CreateDragonSkills();
        SkillSet dragonSoldierSkills = CreateDragonSoldierSkills();

        if (assignToUnitDefinitions)
        {
            Assign(judge, judgeSkills, true, "Assign Judge Boss Skills");
            Assign(highPriest, highPriestSkills, true, "Assign High Priest Boss Skills");
            Assign(dragon, dragonSkills, true, "Assign Dragon Boss Skills");
            Assign(dragonSoldier, dragonSoldierSkills, false, "Assign Dragon Soldier Skills");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[EnemyBossSkillGenerator] Boss enemy skills generated.");
    }

    private SkillSet CreateJudgeSkills()
    {
        SkillDefinition basic = CreateAttack(
            humanOutputFolder,
            "human_boss_judge_basic",
            "심판의 철퇴",
            true,
            0,
            1,
            0,
            1,
            0,
            100,
            CharacterRangeType.Melee,
            SkillClass.Common);
        basic.description = "피해 100%.";

        SkillDefinition revenge = CreateSuccessSkill(
            humanOutputFolder,
            "human_boss_judge_righteous_revenge",
            "정의로운 복수",
            0,
            1,
            SkillTargetTeam.Self,
            TargetScope.Single,
            3,
            CharacterRangeType.Melee,
            SkillClass.Melee,
            Status(StatusEffectType.Taunt, 100f, 1, false),
            Status(StatusEffectType.CounterStance, 100f, 1, false));
        revenge.activeRole = ActiveSkillRole.Utility;
        revenge.description = "자신에게 도발 1턴과 반격 태세 1턴을 부여합니다. 반격은 기존 규칙대로 지속 중 여러 번 발동할 수 있습니다.";

        SkillDefinition enrage = CreatePassive(
            humanOutputFolder,
            "human_boss_judge_enrage_when_high_priest_dies",
            "순교의 심판",
            PassiveSkillGimmick.HumanJudgeEnrageWhenLinkedBossDies,
            CharacterRangeType.Melee,
            SkillClass.Unique);
        enrage.linkedBossUnitDefinition = highPriest;
        enrage.bossEnrageDmgPercent = 40;
        enrage.bossEnrageHitPercent = 15;
        enrage.bossEnrageIncomingDamageTakenPercent = 15;
        enrage.bossEnrageHealMaxHpPercent = 25f;
        enrage.description = "패시브: 대사제 사망 시 전투 중 1회, DMG +40%, HIT +15%, 받는 피해 +15%, 최대 체력 25% 회복.";

        return new SkillSet(basic, revenge, enrage);
    }

    private SkillSet CreateHighPriestSkills()
    {
        SkillDefinition basic = CreateAttack(
            humanOutputFolder,
            "human_boss_high_priest_basic",
            "징벌의 빛",
            true,
            1,
            3,
            0,
            3,
            0,
            100,
            CharacterRangeType.Ranged,
            SkillClass.Common);
        basic.description = "피해 100%.";

        SkillDefinition chain = CreateAttack(
            humanOutputFolder,
            "human_boss_high_priest_chain_of_penitence",
            "참회의 사슬",
            false,
            1,
            3,
            0,
            3,
            2,
            70,
            CharacterRangeType.Ranged,
            SkillClass.Ranged,
            StatusEffectType.Bleed,
            70f,
            1);
        chain.effects.Add(Status(StatusEffectType.Frost, 70f, 5, true));
        chain.description = "피해 70%. 출혈 70%, 둔화는 동상 5스택 70%로 대체.";

        SkillDefinition confession = CreateSuccessSkill(
            humanOutputFolder,
            "human_boss_high_priest_confession",
            "고해성사",
            1,
            3,
            SkillTargetTeam.Ally,
            TargetScope.All,
            4,
            CharacterRangeType.Ranged,
            SkillClass.Ranged,
            Shield(EffectValueReference.TargetMaxHP, 20f));
        confession.activeRole = ActiveSkillRole.Buff;
        confession.description = "아군 전체에게 각 대상 최대 체력의 20% 보호막을 부여합니다. 보호막은 영구 수치형으로 누적됩니다.";

        SkillDefinition revive = CreatePassive(
            humanOutputFolder,
            "human_boss_high_priest_revive_judge",
            "신성한 소생",
            PassiveSkillGimmick.HumanHighPriestReviveLinkedBossOnDeath,
            CharacterRangeType.Ranged,
            SkillClass.Unique);
        revive.linkedBossUnitDefinition = judge;
        revive.linkedBossReviveHpPercent = 30f;
        revive.bossReviverHealMaxHpPercent = 25f;
        revive.description = "패시브: 심판관 사망 시 전투 중 1회, 심판관을 원래 슬롯에서 최대 체력 30%로 즉시 소생시키고 자신은 최대 체력 25%를 회복합니다. 소생한 심판관은 다음 라운드부터 행동합니다.";

        return new SkillSet(basic, chain, confession, revive);
    }

    private SkillSet CreateDragonSkills()
    {
        SkillDefinition basic = CreateAttack(
            dragonOutputFolder,
            "dragon_boss_claw",
            "용의 발톱",
            true,
            0,
            2,
            0,
            1,
            0,
            100,
            CharacterRangeType.Melee,
            SkillClass.Common);
        basic.description = "피해 100%.";

        SkillDefinition stomp = CreateAttack(
            dragonOutputFolder,
            "dragon_boss_stomp",
            "짓밟기",
            false,
            0,
            2,
            0,
            1,
            3,
            100,
            CharacterRangeType.Melee,
            SkillClass.Melee);
        stomp.targetScope = TargetScope.FrontTwo;
        stomp.description = "적 1~2열 광역 피해 100%.";

        SkillDefinition summon = CreateSuccessSkill(
            dragonOutputFolder,
            "dragon_boss_summon_dragon_soldier",
            "용아병 소환",
            0,
            3,
            SkillTargetTeam.Self,
            TargetScope.Single,
            4,
            CharacterRangeType.Melee,
            SkillClass.Unique);
        summon.activeRole = ActiveSkillRole.Utility;
        summon.activeGimmick = ActiveSkillGimmick.ImmediateSummonInFront;
        summon.initialCooldownTurns = 4;
        summon.requireOwnTeamLivingCountAtOrBelow = true;
        summon.maxOwnTeamLivingCountToUse = 3;
        summon.maxLivingAlliesForSummon = 3;
        summon.summonUnitDefinition = dragonSoldier;
        summon.summonUnitViewDefinition = summonedDragonSoldierView;
        summon.description = "전투 시작 시 4턴 쿨타임으로 시작합니다. 이후 4턴마다, 같은 팀 생존자가 3명 이하이고 빈 자리가 있으면 자기 앞에 용아병을 소환합니다.";

        return new SkillSet(basic, stomp, summon);
    }

    private SkillSet CreateDragonSoldierSkills()
    {
        SkillDefinition basic = CreateAttack(
            dragonOutputFolder,
            "dragon_soldier_spear",
            "용아병 창격",
            true,
            0,
            2,
            0,
            1,
            0,
            100,
            CharacterRangeType.Melee,
            SkillClass.Common);
        basic.description = "피해 100%.";

        SkillDefinition worship = CreatePassive(
            dragonOutputFolder,
            "dragon_soldier_worship",
            "용아병 숭배",
            PassiveSkillGimmick.DragonIdt99WhileDragonSoldierAlive,
            CharacterRangeType.Melee,
            SkillClass.Unique);
        worship.linkedBossUnitDefinition = dragon;
        worship.dragonSoldierUnitDefinition = dragonSoldier;
        worship.dragonSoldierProtectionIdtPercent = 99;
        worship.description = "패시브: 자신이 살아 있는 동안 같은 팀 드래곤의 IDT를 99%로 보정합니다. 여러 용아병이 있어도 중첩되지 않으며, 피해만 감소하고 상태이상은 정상 적용됩니다.";

        return new SkillSet(basic, worship);
    }

    private SkillDefinition CreateAttack(
        string folder,
        string id,
        string displayName,
        bool basic,
        int usableMin,
        int usableMax,
        int targetMin,
        int targetMax,
        int cooldown,
        int damagePercent,
        CharacterRangeType range,
        SkillClass skillClass,
        StatusEffectType status = StatusEffectType.None,
        float statusChance = 0f,
        int statusDuration = 0)
    {
        SkillDefinition skill = CreateOrLoadSkill(folder, id, displayName);
        skill.skillId = id;
        skill.skillName = displayName;
        skill.isBasicAttack = basic;
        skill.castType = SkillCastType.Active;
        skill.activeRole = ActiveSkillRole.Attack;
        skill.rangeTag = range;
        skill.skillClass = skillClass;
        skill.targetTeam = SkillTargetTeam.Enemy;
        skill.targetScope = TargetScope.Single;
        skill.usableMinSlotIndex = usableMin;
        skill.usableMaxSlotIndex = usableMax;
        skill.targetMinSlotIndex = targetMin;
        skill.targetMaxSlotIndex = targetMax;
        skill.cooldownTurns = cooldown;
        skill.initialCooldownTurns = 0;
        skill.resolutionMode = SkillResolutionMode.Attack;
        skill.allowCrit = true;
        skill.allowGraze = true;
        skill.primaryHitCount = 1;
        skill.activeGimmick = ActiveSkillGimmick.None;
        skill.passiveGimmick = PassiveSkillGimmick.None;
        skill.effects.Clear();
        skill.effects.Add(Damage(damagePercent));
        if (status != StatusEffectType.None && statusChance > 0f && statusDuration > 0)
            skill.effects.Add(Status(status, statusChance, statusDuration, true));
        EditorUtility.SetDirty(skill);
        return skill;
    }

    private SkillDefinition CreateSuccessSkill(
        string folder,
        string id,
        string displayName,
        int usableMin,
        int usableMax,
        SkillTargetTeam targetTeam,
        TargetScope targetScope,
        int cooldown,
        CharacterRangeType range,
        SkillClass skillClass,
        params BattleEffectBlock[] effects)
    {
        SkillDefinition skill = CreateOrLoadSkill(folder, id, displayName);
        skill.skillId = id;
        skill.skillName = displayName;
        skill.isBasicAttack = false;
        skill.castType = SkillCastType.Active;
        skill.activeRole = ActiveSkillRole.Utility;
        skill.rangeTag = range;
        skill.skillClass = skillClass;
        skill.targetTeam = targetTeam;
        skill.targetScope = targetScope;
        skill.usableMinSlotIndex = usableMin;
        skill.usableMaxSlotIndex = usableMax;
        skill.targetMinSlotIndex = 0;
        skill.targetMaxSlotIndex = 3;
        skill.cooldownTurns = cooldown;
        skill.initialCooldownTurns = 0;
        skill.resolutionMode = SkillResolutionMode.SuccessOnly;
        skill.activeGimmick = ActiveSkillGimmick.None;
        skill.passiveGimmick = PassiveSkillGimmick.None;
        skill.effects.Clear();
        if (effects != null)
        {
            for (int i = 0; i < effects.Length; i++)
            {
                if (effects[i] != null)
                    skill.effects.Add(effects[i]);
            }
        }
        EditorUtility.SetDirty(skill);
        return skill;
    }

    private SkillDefinition CreatePassive(
        string folder,
        string id,
        string displayName,
        PassiveSkillGimmick gimmick,
        CharacterRangeType range,
        SkillClass skillClass)
    {
        SkillDefinition skill = CreateOrLoadSkill(folder, id, displayName);
        skill.skillId = id;
        skill.skillName = displayName;
        skill.isBasicAttack = false;
        skill.castType = SkillCastType.Passive;
        skill.activeRole = ActiveSkillRole.Utility;
        skill.rangeTag = range;
        skill.skillClass = skillClass;
        skill.passiveGimmick = gimmick;
        skill.activeGimmick = ActiveSkillGimmick.None;
        skill.cooldownTurns = 0;
        skill.initialCooldownTurns = 0;
        skill.effects.Clear();
        EditorUtility.SetDirty(skill);
        return skill;
    }

    private BattleEffectBlock Damage(int powerPercent)
    {
        return new BattleEffectBlock
        {
            kind = BattleEffectKind.Damage,
            powerPercent = powerPercent,
            valueReference = EffectValueReference.ActorDMG,
            successChancePercent = 100f,
            affectedByResistance = false
        };
    }

    private BattleEffectBlock Shield(EffectValueReference reference, float powerPercent)
    {
        return new BattleEffectBlock
        {
            kind = BattleEffectKind.Shield,
            powerPercent = powerPercent,
            valueReference = reference,
            successChancePercent = 100f,
            affectedByResistance = false
        };
    }

    private BattleEffectBlock Status(StatusEffectType statusType, float chance, int duration, bool affectedByResistance)
    {
        return new BattleEffectBlock
        {
            kind = BattleEffectKind.ApplyStatus,
            statusType = statusType,
            successChancePercent = chance,
            affectedByResistance = affectedByResistance,
            durationTurns = duration
        };
    }

    private SkillDefinition CreateOrLoadSkill(string folder, string id, string displayName)
    {
        EnsureFolder(folder);
        string path = folder.TrimEnd('/') + "/" + id + ".asset";
        SkillDefinition skill = AssetDatabase.LoadAssetAtPath<SkillDefinition>(path);

        if (skill == null)
        {
            skill = CreateInstance<SkillDefinition>();
            skill.name = id;
            AssetDatabase.CreateAsset(skill, path);
        }
        else if (!overwriteExisting)
        {
            return skill;
        }

        skill.name = id;
        skill.skillId = id;
        skill.skillName = displayName;
        return skill;
    }

    private void Assign(UnitDefinition unit, SkillSet skills, bool forceMoveImmune, string undoName)
    {
        if (unit == null || skills == null)
            return;

        Undo.RecordObject(unit, undoName);
        unit.basicAttack = skills.basicAttack;
        unit.forcePositionMoveImmune = forceMoveImmune;
        unit.fixedStartingSkills.Clear();
        for (int i = 0; i < skills.skills.Count; i++)
        {
            SkillDefinition skill = skills.skills[i];
            if (skill != null && !skill.isBasicAttack)
                unit.fixedStartingSkills.Add(skill);
        }
        EditorUtility.SetDirty(unit);
    }

    private void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return;

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
        public readonly List<SkillDefinition> skills = new List<SkillDefinition>();

        public SkillSet(params SkillDefinition[] definitions)
        {
            if (definitions == null)
                return;

            for (int i = 0; i < definitions.Length; i++)
            {
                SkillDefinition skill = definitions[i];
                if (skill == null)
                    continue;

                if (skill.isBasicAttack && basicAttack == null)
                    basicAttack = skill;

                skills.Add(skill);
            }
        }
    }
}
#endif
