#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class EnemyElfSkillGeneratorWindow : EditorWindow
{
    private const string DefaultFolder = "Assets/SkillDefinition/EnemySkill/Elf";

    [SerializeField] private string outputFolder = DefaultFolder;
    [SerializeField] private bool overwriteExisting = false;
    [SerializeField] private bool assignToUnitDefinitions = true;

    [Header("Elf Enemy UnitDefinitions")]
    [SerializeField] private UnitDefinition fairy;
    [SerializeField] private UnitDefinition dryad;
    [SerializeField] private UnitDefinition swordDancer;
    [SerializeField] private UnitDefinition hunter;
    [SerializeField] private UnitDefinition spiritDeer;
    [SerializeField] private UnitDefinition druid;
    [SerializeField] private UnitDefinition mage;

    [Header("Summon ViewDefinition")]
    [SerializeField] private UnitViewDefinition summonedDryadView;

    [MenuItem("Tools/Battle/Enemy Skills/Generate Elf Enemy Skills")]
    public static void Open()
    {
        GetWindow<EnemyElfSkillGeneratorWindow>("Elf Enemy Skills");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Elf Enemy Skill Generator", EditorStyles.boldLabel);
        outputFolder = EditorGUILayout.TextField("Output Folder", string.IsNullOrWhiteSpace(outputFolder) ? DefaultFolder : outputFolder);
        overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing", overwriteExisting);
        assignToUnitDefinitions = EditorGUILayout.Toggle("Assign To UnitDefinitions", assignToUnitDefinitions);

        EditorGUILayout.Space(8f);
        fairy = (UnitDefinition)EditorGUILayout.ObjectField("페어리", fairy, typeof(UnitDefinition), false);
        dryad = (UnitDefinition)EditorGUILayout.ObjectField("드라이어드", dryad, typeof(UnitDefinition), false);
        swordDancer = (UnitDefinition)EditorGUILayout.ObjectField("검의 무희", swordDancer, typeof(UnitDefinition), false);
        hunter = (UnitDefinition)EditorGUILayout.ObjectField("사냥꾼", hunter, typeof(UnitDefinition), false);
        spiritDeer = (UnitDefinition)EditorGUILayout.ObjectField("정령 사슴", spiritDeer, typeof(UnitDefinition), false);
        druid = (UnitDefinition)EditorGUILayout.ObjectField("드루이드", druid, typeof(UnitDefinition), false);
        mage = (UnitDefinition)EditorGUILayout.ObjectField("마법사", mage, typeof(UnitDefinition), false);

        EditorGUILayout.Space(8f);
        summonedDryadView = (UnitViewDefinition)EditorGUILayout.ObjectField("숲의 부름 소환 드라이어드 View", summonedDryadView, typeof(UnitViewDefinition), false);

        EditorGUILayout.Space(10f);
        if (GUILayout.Button("Generate / Assign Elf Enemy Skills", GUILayout.Height(34f)))
            GenerateAll();
    }

    private void GenerateAll()
    {
        EnsureFolder(outputFolder);

        SkillSet fairySkills = CreateFairySkills();
        SkillSet dryadSkills = CreateDryadSkills();
        SkillSet swordDancerSkills = CreateSwordDancerSkills();
        SkillSet hunterSkills = CreateHunterSkills();
        SkillSet spiritDeerSkills = CreateSpiritDeerSkills();
        SkillSet druidSkills = CreateDruidSkills();
        SkillSet mageSkills = CreateMageSkills();

        if (assignToUnitDefinitions)
        {
            Assign(fairy, fairySkills, "Assign Fairy Skills");
            Assign(dryad, dryadSkills, "Assign Dryad Skills");
            Assign(swordDancer, swordDancerSkills, "Assign Sword Dancer Skills");
            Assign(hunter, hunterSkills, "Assign Hunter Skills");
            Assign(spiritDeer, spiritDeerSkills, "Assign Spirit Deer Skills");
            Assign(druid, druidSkills, "Assign Druid Skills");
            Assign(mage, mageSkills, "Assign Mage Skills");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[EnemyElfSkillGenerator] Elf enemy skills generated.");
    }

    private SkillSet CreateFairySkills()
    {
        SkillDefinition basic = CreateAttack(
            "elf_fairy_magic_prank",
            "마법 장난",
            true,
            0,
            3,
            0,
            3,
            0,
            100,
            CharacterRangeType.Mid,
            SkillClass.Common,
            StatusEffectType.Blind,
            15f,
            1);
        basic.description = "피해 100%. 실명 15% 확률 부여.";
        return new SkillSet(basic);
    }

    private SkillSet CreateDryadSkills()
    {
        SkillDefinition basic = CreateAttack(
            "elf_dryad_swing",
            "휘두르기",
            true,
            0,
            1,
            0,
            0,
            0,
            100,
            CharacterRangeType.Melee,
            SkillClass.Common,
            StatusEffectType.Bleed,
            30f,
            1);
        basic.description = "피해 100%. 출혈 30% 확률 부여.";

        SkillDefinition root = CreateSuccessSkill(
            "elf_dryad_root",
            "뿌리박기",
            0,
            0,
            SkillTargetTeam.Self,
            TargetScope.Single,
            2,
            CharacterRangeType.Melee,
            SkillClass.Melee,
            Status(StatusEffectType.Taunt, 100f, 2, false));
        root.activeRole = ActiveSkillRole.Utility;
        root.description = "자신에게 2턴간 도발을 부여합니다.";

        SkillDefinition regen = CreatePassive(
            "elf_dryad_regeneration",
            "재생",
            PassiveSkillGimmick.HealSelfMaxHpPercentOnTurnStart,
            CharacterRangeType.Melee,
            SkillClass.Unique);
        regen.turnStartSelfHealMaxHpPercent = 5f;
        regen.description = "패시브: 턴 시작마다 자신의 최대 체력 5%를 회복합니다.";

        return new SkillSet(basic, root, regen);
    }

    private SkillSet CreateSwordDancerSkills()
    {
        SkillDefinition basic = CreateAttack(
            "elf_sword_dancer_double_attack",
            "이중 공격",
            true,
            0,
            2,
            0,
            2,
            0,
            70,
            CharacterRangeType.Melee,
            SkillClass.Common);
        basic.primaryHitCount = 2;
        basic.description = "피해 70%로 같은 대상을 2회 공격합니다.";

        SkillDefinition stance = CreateSuccessSkill(
            "elf_sword_dancer_battle_stance",
            "전투 자세",
            0,
            2,
            SkillTargetTeam.Self,
            TargetScope.Single,
            3,
            CharacterRangeType.Melee,
            SkillClass.Melee,
            Buff(StatModifierType.DMG, 20, 3),
            Status(StatusEffectType.BattleStance, 100f, 3, false));
        stance.activeRole = ActiveSkillRole.Buff;
        stance.blockedSelfStatusToUse = StatusEffectType.BattleStance;
        stance.description = "3턴간 DMG 20% 상승. 전투 자세 중에는 중복 사용되지 않으며, 검무 사용 조건인 전투 자세를 부여합니다.";

        SkillDefinition dance = CreateAttack(
            "elf_sword_dancer_sword_dance",
            "검무",
            false,
            0,
            1,
            0,
            3,
            1,
            60,
            CharacterRangeType.Melee,
            SkillClass.Melee);
        dance.targetScope = TargetScope.CenteredThree;
        dance.requiredSelfStatusToUse = StatusEffectType.BattleStance;
        dance.description = "전투 자세 유지 중에만 사용 가능합니다. 지정 기준 123열 또는 234열에 피해 60%.";

        return new SkillSet(basic, stance, dance);
    }

    private SkillSet CreateHunterSkills()
    {
        SkillDefinition basic = CreateAttack(
            "elf_hunter_snipe",
            "저격",
            true,
            1,
            3,
            1,
            3,
            0,
            100,
            CharacterRangeType.Ranged,
            SkillClass.Common);
        basic.description = "피해 100%.";

        SkillDefinition mark = CreateSuccessSkill(
            "elf_hunter_mark",
            "사냥꾼의 표식",
            1,
            3,
            SkillTargetTeam.Enemy,
            TargetScope.Single,
            5,
            CharacterRangeType.Ranged,
            SkillClass.Ranged,
            Status(StatusEffectType.Hunting, 100f, 5, false));
        mark.activeRole = ActiveSkillRole.Debuff;
        mark.description = "사냥 표식(Hunting) 5스택을 부여합니다. 중첩 가능하며, 표식 대상 공격 시 스택당 치명타 확률 +5%. 대상 턴 시작마다 1스택 감소.";

        SkillDefinition rapid = CreateAttack(
            "elf_hunter_rapid_shot",
            "연발 사격",
            false,
            2,
            3,
            0,
            3,
            1,
            60,
            CharacterRangeType.Ranged,
            SkillClass.Ranged);
        rapid.primaryHitCount = 3;
        rapid.description = "지정 대상에게 피해 60%로 3회 연속 공격합니다.";

        return new SkillSet(basic, mark, rapid);
    }

    private SkillSet CreateSpiritDeerSkills()
    {
        SkillDefinition basic = CreateAttack(
            "elf_spirit_deer_ram",
            "들이받기",
            true,
            0,
            3,
            0,
            0,
            0,
            100,
            CharacterRangeType.Melee,
            SkillClass.Common);
        basic.description = "피해 100%.";

        SkillDefinition stomp = CreateAttack(
            "elf_spirit_deer_stomp",
            "발구르기",
            false,
            0,
            1,
            0,
            3,
            3,
            20,
            CharacterRangeType.Melee,
            SkillClass.Melee,
            StatusEffectType.Stun,
            30f,
            1);
        stomp.targetScope = TargetScope.All;
        stomp.description = "적 전체에게 피해 20%. 기절 30% 확률 부여.";

        SkillDefinition purification = CreatePassive(
            "elf_spirit_deer_purification",
            "정화",
            PassiveSkillGimmick.TeamStatusResistAuraWhileAlive,
            CharacterRangeType.Melee,
            SkillClass.Unique);
        purification.teamStatusResistAuraPercent = 30;
        purification.description = "패시브: 생존 중 같은 편 전체의 상태이상 저항률 +30%.";

        return new SkillSet(basic, stomp, purification);
    }

    private SkillSet CreateDruidSkills()
    {
        SkillDefinition basic = CreateAttack(
            "elf_druid_entangle",
            "옭아매기",
            true,
            1,
            3,
            0,
            3,
            0,
            100,
            CharacterRangeType.Ranged,
            SkillClass.Common,
            StatusEffectType.Frost,
            50f,
            5);
        basic.description = "피해 100%. 둔화는 동상 5스택으로 대체합니다.";

        SkillDefinition gale = CreateAttack(
            "elf_druid_gale",
            "강풍",
            false,
            1,
            3,
            0,
            3,
            2,
            50,
            CharacterRangeType.Ranged,
            SkillClass.Ranged);
        gale.activeGimmick = ActiveSkillGimmick.PushTargetBackwardAfterHit;
        gale.forcedTargetMoveSteps = 2;
        gale.forcedTargetMoveChancePercent = 30f;
        gale.description = "피해 50%. 명중 시 30% 확률로 대상을 2칸 뒤로 강제 이동시킵니다.";

        SkillDefinition call = CreateSelfUtility(
            "elf_druid_call_of_forest",
            "숲의 부름",
            ActiveSkillGimmick.ImmediateSummonInFront,
            4,
            CharacterRangeType.Ranged,
            SkillClass.Ranged);
        call.summonUnitDefinition = dryad;
        call.summonUnitViewDefinition = summonedDryadView;
        call.maxLivingAlliesForSummon = 3;
        call.requireOwnTeamLivingCountAtOrBelow = true;
        call.maxOwnTeamLivingCountToUse = 3;
        call.description = "아군이 3개체 이하일 때 본인 앞에 드라이어드 1기를 즉시 소환합니다.";

        return new SkillSet(basic, gale, call);
    }

    private SkillSet CreateMageSkills()
    {
        SkillDefinition basic = CreateAttack(
            "elf_mage_fireball",
            "화염구",
            true,
            1,
            3,
            0,
            3,
            0,
            100,
            CharacterRangeType.Ranged,
            SkillClass.Common,
            StatusEffectType.Burn,
            100f,
            1);
        basic.description = "피해 100%. 화상 100% 확률 부여.";

        SkillDefinition barrier = CreateSuccessSkill(
            "elf_mage_ice_barrier",
            "얼음 방벽",
            1,
            3,
            SkillTargetTeam.Ally,
            TargetScope.Single,
            2,
            CharacterRangeType.Ranged,
            SkillClass.Ranged,
            Effect(BattleEffectKind.Shield, 80, EffectValueReference.ActorDMG));
        barrier.activeRole = ActiveSkillRole.Buff;
        barrier.alsoApplyToSelfWhenTargetingAlly = true;
        barrier.description = "선택한 아군 1명과 자기 자신에게 공격력 80%만큼 영구 보호막을 생성합니다.";

        SkillDefinition arcane = CreateAttack(
            "elf_mage_arcane_explosion",
            "비전 폭발",
            false,
            1,
            3,
            0,
            3,
            1,
            25,
            CharacterRangeType.Ranged,
            SkillClass.Ranged);
        arcane.targetScope = TargetScope.All;
        arcane.shieldedTargetDamagePowerPercent = 100f;
        arcane.description = "적 전체에게 피해 25%. 보호막 보유 대상에게는 피해 100%로 보정됩니다.";

        return new SkillSet(basic, barrier, arcane);
    }

    private SkillDefinition CreateAttack(string id, string displayName, bool isBasic, int usableMin, int usableMax, int targetMin, int targetMax, int cooldown, int damagePercent,
        CharacterRangeType range, SkillClass skillClass, StatusEffectType status = StatusEffectType.None, float statusChance = 0f, int statusDuration = 0)
    {
        SkillDefinition skill = LoadOrCreate(id, displayName);
        ResetSkill(skill);
        skill.skillId = id;
        skill.skillName = displayName;
        skill.description = BuildAttackDescription(damagePercent, status, statusChance);
        skill.isBasicAttack = isBasic;
        skill.castType = SkillCastType.Active;
        skill.activeRole = ActiveSkillRole.Attack;
        skill.skillClass = skillClass;
        skill.rangeTag = range;
        skill.usableMinSlotIndex = usableMin;
        skill.usableMaxSlotIndex = usableMax;
        skill.targetMinSlotIndex = targetMin;
        skill.targetMaxSlotIndex = targetMax;
        skill.targetTeam = SkillTargetTeam.Enemy;
        skill.targetScope = TargetScope.Single;
        skill.resolutionMode = SkillResolutionMode.Attack;
        skill.cooldownTurns = cooldown;
        skill.accuracyCoefficientPercent = 100f;
        skill.allowCrit = true;
        skill.allowGraze = true;
        skill.effects = new List<BattleEffectBlock> { Effect(BattleEffectKind.Damage, damagePercent, EffectValueReference.ActorDMG) };
        if (status != StatusEffectType.None && statusChance > 0f && statusDuration > 0)
            skill.effects.Add(Status(status, statusChance, statusDuration, true));
        EditorUtility.SetDirty(skill);
        return skill;
    }

    private SkillDefinition CreateSuccessSkill(string id, string displayName, int usableMin, int usableMax, SkillTargetTeam team, TargetScope scope, int cooldown,
        CharacterRangeType range, SkillClass skillClass, params BattleEffectBlock[] effects)
    {
        SkillDefinition skill = LoadOrCreate(id, displayName);
        ResetSkill(skill);
        skill.skillId = id;
        skill.skillName = displayName;
        skill.isBasicAttack = false;
        skill.castType = SkillCastType.Active;
        skill.activeRole = ActiveSkillRole.Buff;
        skill.skillClass = skillClass;
        skill.rangeTag = range;
        skill.usableMinSlotIndex = usableMin;
        skill.usableMaxSlotIndex = usableMax;
        skill.targetMinSlotIndex = 0;
        skill.targetMaxSlotIndex = 3;
        skill.targetTeam = team;
        skill.targetScope = scope;
        skill.resolutionMode = SkillResolutionMode.SuccessOnly;
        skill.cooldownTurns = cooldown;
        skill.allowCrit = false;
        skill.allowGraze = false;
        skill.effects = new List<BattleEffectBlock>(effects);
        EditorUtility.SetDirty(skill);
        return skill;
    }

    private SkillDefinition CreateSelfUtility(string id, string displayName, ActiveSkillGimmick gimmick, int cooldown, CharacterRangeType range, SkillClass skillClass)
    {
        SkillDefinition skill = LoadOrCreate(id, displayName);
        ResetSkill(skill);
        skill.skillId = id;
        skill.skillName = displayName;
        skill.isBasicAttack = false;
        skill.castType = SkillCastType.Active;
        skill.activeRole = ActiveSkillRole.Utility;
        skill.skillClass = skillClass;
        skill.rangeTag = range;
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

    private SkillDefinition CreatePassive(string id, string displayName, PassiveSkillGimmick gimmick, CharacterRangeType range, SkillClass skillClass)
    {
        SkillDefinition skill = LoadOrCreate(id, displayName);
        ResetSkill(skill);
        skill.skillId = id;
        skill.skillName = displayName;
        skill.isBasicAttack = false;
        skill.castType = SkillCastType.Passive;
        skill.activeRole = ActiveSkillRole.Utility;
        skill.skillClass = skillClass;
        skill.rangeTag = range;
        skill.targetTeam = SkillTargetTeam.Self;
        skill.targetScope = TargetScope.Single;
        skill.resolutionMode = SkillResolutionMode.SuccessOnly;
        skill.passiveGimmick = gimmick;
        skill.effects = new List<BattleEffectBlock>();
        EditorUtility.SetDirty(skill);
        return skill;
    }

    private void ResetSkill(SkillDefinition skill)
    {
        if (skill == null)
            return;

        skill.passiveGimmick = PassiveSkillGimmick.None;
        skill.activeGimmick = ActiveSkillGimmick.None;
        skill.requiredSelfStatusToUse = StatusEffectType.None;
        skill.blockedSelfStatusToUse = StatusEffectType.None;
        skill.onlyUsableWhenAlone = false;
        skill.requireOwnTeamLivingCountAtOrBelow = false;
        skill.maxOwnTeamLivingCountToUse = 3;
        skill.primaryHitCount = 1;
        skill.alsoApplyToSelfWhenTargetingAlly = false;
        skill.initialCooldownTurns = 0;
        skill.selfMoveDirection = SkillSelfMoveDirection.None;
        skill.selfMoveSteps = 0;
        skill.selfApplyStatusAfterUse = StatusEffectType.None;
        skill.selfApplyStatusDurationTurns = 0;
        skill.useMissingHpPowerBonus = false;
        skill.missingHpPercentStep = 1;
        skill.bonusPowerPerStep = 0f;
        skill.secondaryTargetRule = SecondaryTargetRule.None;
        skill.secondaryAccuracyCoefficientPercent = 100f;
        skill.secondaryDamagePercent = 0f;
        skill.secondaryApplyNonDamageEffects = false;
        skill.forcedTargetMoveToRank = 1;
        skill.forcedTargetMoveSteps = 1;
        skill.forcedTargetMoveChancePercent = 100f;
        skill.pushBackFailFinalPowerPercent = 0f;
        skill.shieldedTargetDamagePowerPercent = 0f;
        skill.randomRepositionChancePercent = 20f;
        skill.delayedReinforcementDelayRounds = 2;
        skill.abyssReboundRecoilPercentFromTotalDamage = 20f;
        skill.blackArenaDuelDurationTurns = 2;
        skill.summonUnitDefinition = null;
        skill.summonUnitViewDefinition = null;
        skill.summonLevelOverride = 0;
        skill.maxLivingAlliesForSummon = 3;
        skill.battleStartEnemyTeamDmgDownPercent = 10;
        skill.battleStartEnemyTeamDmgDownDurationTurns = 0;
        skill.battleStartEnemyTeamDmgDownPermanent = true;
        skill.shieldedHitBleedChancePercent = 100f;
        skill.shieldedHitBleedStacks = 1;
        skill.blackAuraShieldGainPercentFromHpDamage = 100f;
        skill.blackAuraShieldFlatBonus = 0;
        skill.turnStartSelfHealMaxHpPercent = 5f;
        skill.teamStatusResistAuraPercent = 30;
        skill.disableAfterUseInBattle = false;
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

    private BattleEffectBlock Status(StatusEffectType status, float chance, int duration, bool affectedByResistance)
    {
        return new BattleEffectBlock
        {
            kind = BattleEffectKind.ApplyStatus,
            statusType = status,
            successChancePercent = chance,
            affectedByResistance = affectedByResistance,
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
            return string.Format("피해 {0}%", damagePercent);

        return string.Format("피해 {0}%, {1} {2}% 확률", damagePercent, BattleStatusUtility.GetDisplayName(status), Mathf.RoundToInt(chance));
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

            Undo.RecordObject(existing, "Overwrite Elf SkillDefinition");
            return existing;
        }

        SkillDefinition skill = CreateInstance<SkillDefinition>();
        AssetDatabase.CreateAsset(skill, path);
        return skill;
    }

    private void Assign(UnitDefinition unit, SkillSet skills, string undoName)
    {
        if (unit == null || skills == null || skills.basicAttack == null)
            return;

        Undo.RecordObject(unit, undoName);
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
