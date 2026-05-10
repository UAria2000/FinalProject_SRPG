#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class AllySkillGeneratorWindow : EditorWindow
{
    private const string DefaultCommonFolder = "Assets/SkillDefinition/AllySkill/Common";
    private const string DefaultMeleeFolder = "Assets/SkillDefinition/AllySkill/Melee";
    private const string DefaultMidFolder = "Assets/SkillDefinition/AllySkill/Mid";
    private const string DefaultRangedFolder = "Assets/SkillDefinition/AllySkill/Ranged";
    private const string DefaultUniqueFolder = "Assets/SkillDefinition/AllySkill/Unique";
    private const string DefaultTableFolder = "Assets/SkillDefinition/AllySkillTables";
    private const string DefaultPoolTablePath = DefaultTableFolder + "/AllySkillLearnPoolTable.asset";

    [SerializeField] private bool overwriteExisting = false;
    [SerializeField] private bool createOrUpdatePoolTable = true;
    [SerializeField] private bool assignMainPlayerSkills = true;
    [SerializeField] private UnitDefinition mainPlayerUnitDefinition;

    [Header("Output Folders")]
    [SerializeField] private string commonFolder = DefaultCommonFolder;
    [SerializeField] private string meleeFolder = DefaultMeleeFolder;
    [SerializeField] private string midFolder = DefaultMidFolder;
    [SerializeField] private string rangedFolder = DefaultRangedFolder;
    [SerializeField] private string uniqueFolder = DefaultUniqueFolder;
    [SerializeField] private string tableFolder = DefaultTableFolder;

    [MenuItem("Tools/Battle/Ally Skills/Generate Ally Skills")]
    public static void Open()
    {
        GetWindow<AllySkillGeneratorWindow>("Ally Skills");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Ally Skill Generator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "allyskill.docx 기준으로 구현 대상 스킬만 생성합니다. 제외된 스킬은 생성하지 않으며, 주인공 스킬은 랜덤 테이블에는 넣지 않고 연결한 UnitDefinition에만 배정할 수 있습니다.",
            MessageType.Info);

        overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing", overwriteExisting);
        createOrUpdatePoolTable = EditorGUILayout.Toggle("Create / Update Pool Table", createOrUpdatePoolTable);
        assignMainPlayerSkills = EditorGUILayout.Toggle("Assign Main Player Skills", assignMainPlayerSkills);
        mainPlayerUnitDefinition = (UnitDefinition)EditorGUILayout.ObjectField("Main Player UnitDefinition", mainPlayerUnitDefinition, typeof(UnitDefinition), false);

        EditorGUILayout.Space(8f);
        commonFolder = EditorGUILayout.TextField("Common Folder", Fallback(commonFolder, DefaultCommonFolder));
        meleeFolder = EditorGUILayout.TextField("Melee Folder", Fallback(meleeFolder, DefaultMeleeFolder));
        midFolder = EditorGUILayout.TextField("Mid Folder", Fallback(midFolder, DefaultMidFolder));
        rangedFolder = EditorGUILayout.TextField("Ranged Folder", Fallback(rangedFolder, DefaultRangedFolder));
        uniqueFolder = EditorGUILayout.TextField("Unique Folder", Fallback(uniqueFolder, DefaultUniqueFolder));
        tableFolder = EditorGUILayout.TextField("Table Folder", Fallback(tableFolder, DefaultTableFolder));

        EditorGUILayout.Space(12f);
        if (GUILayout.Button("Generate Ally Skills", GUILayout.Height(34f)))
            GenerateAll();
    }

    private void GenerateAll()
    {
        EnsureFolder(commonFolder);
        EnsureFolder(meleeFolder);
        EnsureFolder(midFolder);
        EnsureFolder(rangedFolder);
        EnsureFolder(uniqueFolder);
        EnsureFolder(tableFolder);

        List<SkillDefinition> common = CreateCommonSkills();
        List<SkillDefinition> melee = CreateMeleeSkills();
        List<SkillDefinition> mid = CreateMidSkills();
        List<SkillDefinition> ranged = CreateRangedSkills();
        List<SkillDefinition> hero = CreateHeroSkills();

        if (createOrUpdatePoolTable)
            CreateOrUpdatePoolTable(common, melee, mid, ranged);

        if (assignMainPlayerSkills)
            AssignHeroSkills(hero);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[AllySkillGenerator] Ally skills generated.");
    }

    private List<SkillDefinition> CreateMeleeSkills()
    {
        string folder = Fallback(meleeFolder, DefaultMeleeFolder);
        List<SkillDefinition> list = new List<SkillDefinition>();

        SkillDefinition duel = CreateAttack(folder, "ally_melee_duel", "결투", SkillClass.Melee, CharacterRangeType.Melee, 0, 1, 0, 2, 4, 100);
        duel.activeGimmick = ActiveSkillGimmick.BlackArenaDuel2Turns;
        duel.blackArenaDuelDurationTurns = 2;
        duel.description = "피해 100%. 2턴간 자신과 대상에게 결투 상태를 부여합니다.";
        list.Add(duel);

        SkillDefinition destructive = CreateAttack(folder, "ally_melee_destructive_blow", "파괴적인 강타", SkillClass.Melee, CharacterRangeType.Melee, 0, 1, 0, 1, 1, 110);
        destructive.activeGimmick = ActiveSkillGimmick.PushTargetBackwardAfterHit;
        destructive.forcedTargetMoveSteps = 2;
        destructive.pushBackFailFinalPowerPercent = 150f;
        destructive.description = "피해 110%. 대상을 최대 2칸 뒤로 강제 이동합니다. 강제 이동 면역이면 피해 150%.";
        list.Add(destructive);

        SkillDefinition gaze = CreateAttack(folder, "ally_melee_terrifying_gaze", "공포의 시선", SkillClass.Melee, CharacterRangeType.Melee, 0, 1, 0, 1, 1, 110,
            StatusEffectType.Stun, 30f, 1);
        gaze.description = "피해 110%. 기절 30%.";
        list.Add(gaze);

        SkillDefinition armor = CreateSuccessSkill(folder, "ally_melee_armor_of_agony", "고통의 갑옷", SkillClass.Melee, CharacterRangeType.Melee, 0, 1, SkillTargetTeam.Self, TargetScope.Single, 2,
            Effect(BattleEffectKind.Shield, 30, EffectValueReference.TargetMaxHP));
        armor.passiveGimmick = PassiveSkillGimmick.Bleed25ToAttackerWhenShieldedHit;
        armor.shieldedHitBleedChancePercent = 100f;
        armor.shieldedHitBleedStacks = 1;
        armor.description = "자신에게 최대 체력 30% 보호막. 보호막 유지 중 피격 시 공격자에게 출혈 1스택.";
        list.Add(armor);

        SkillDefinition revenge = CreateSuccessSkill(folder, "ally_melee_revenge", "복수", SkillClass.Melee, CharacterRangeType.Melee, 0, 0, SkillTargetTeam.Self, TargetScope.Single, 2,
            Buff(StatModifierType.IncomingDamageTakenPercent, 30, 1), Status(StatusEffectType.CounterStance, 100f, 1, false));
        revenge.description = "1턴간 받는 피해 30% 감소. 피격 시 반격 태세.";
        list.Add(revenge);

        SkillDefinition charge = CreateAttack(folder, "ally_melee_menacing_charge", "위협적인 돌진", SkillClass.Melee, CharacterRangeType.Melee, 0, 3, 0, 0, 2, 100);
        charge.selfMoveDirection = SkillSelfMoveDirection.Forward;
        charge.selfMoveSteps = 2;
        charge.selfApplyStatusAfterUse = StatusEffectType.Taunt;
        charge.selfApplyStatusDurationTurns = 1;
        charge.description = "적 1열 공격. 명중 여부와 무관하게 본인 2칸 전진 후 1턴 도발.";
        list.Add(charge);

        SkillDefinition extinction = CreateAttack(folder, "ally_melee_extinction", "절멸", SkillClass.Melee, CharacterRangeType.Melee, 0, 1, 0, 1, 1, 80);
        extinction.targetScope = TargetScope.FrontTwo;
        extinction.activeGimmick = ActiveSkillGimmick.BleedDrainStrike;
        extinction.effects.Add(Effect(BattleEffectKind.Heal, 25, EffectValueReference.ActorDMG));
        extinction.description = "적 1~2열 광역 피해 80%. 입힌 HP 피해량의 25%를 즉시 회복합니다.";
        list.Add(extinction);

        return list;
    }

    private List<SkillDefinition> CreateMidSkills()
    {
        string folder = Fallback(midFolder, DefaultMidFolder);
        List<SkillDefinition> list = new List<SkillDefinition>();

        SkillDefinition hook = CreateAttack(folder, "ally_mid_hook_netherworld", "저승의 갈고리", SkillClass.Mid, CharacterRangeType.Mid, 0, 2, 0, 3, 2, 80,
            StatusEffectType.Bleed, 100f, 2);
        hook.activeGimmick = ActiveSkillGimmick.PullTargetForwardAfterHit;
        hook.forcedTargetMoveSteps = 2;
        hook.description = "피해 80%. 대상을 앞으로 2칸 당기고 출혈 2스택.";
        list.Add(hook);

        SkillDefinition surge = CreateAttack(folder, "ally_mid_surge_of_calamity", "재앙의 쇄도", SkillClass.Mid, CharacterRangeType.Mid, 0, 3, 0, 2, 1, 100,
            StatusEffectType.Hunting, 100f, 5, false);
        surge.targetStatusBonusType = StatusEffectType.Hunting;
        surge.targetStatusBonusPowerAddPercent = 30f;
        surge.description = "피해 100%. 사냥 표식 5스택 부여. 표식 대상 공격 시 피해 계수 +30%.";
        list.Add(surge);

        SkillDefinition leap = CreateAttack(folder, "ally_mid_shadow_leap", "그림자 도약", SkillClass.Mid, CharacterRangeType.Mid, 0, 3, 0, 1, 1, 120);
        leap.selfMoveDirection = SkillSelfMoveDirection.Forward;
        leap.selfMoveSteps = 1;
        leap.effects.Add(Buff(StatModifierType.AC, 25, 2));
        leap.description = "피해 120%. 자신이 1칸 전진하고 2턴간 AC +25%.";
        list.Add(leap);

        SkillDefinition ashen = CreateAttack(folder, "ally_mid_ashen_veil", "재의 장막", SkillClass.Mid, CharacterRangeType.Mid, 0, 2, 0, 1, 1, 90,
            StatusEffectType.Blind, 80f, 1);
        ashen.selfMoveDirection = SkillSelfMoveDirection.Backward;
        ashen.selfMoveSteps = 1;
        ashen.description = "피해 90%. 자신은 1칸 후퇴하고 대상에게 실명 80%.";
        list.Add(ashen);

        SkillDefinition slaughter = CreateAttack(folder, "ally_mid_slaughter", "살육", SkillClass.Mid, CharacterRangeType.Mid, 0, 2, 0, 3, 2, 70,
            StatusEffectType.Bleed, 70f, 1);
        slaughter.targetScope = TargetScope.All;
        slaughter.description = "적 전체 광역 피해 70%. 출혈 70%.";
        list.Add(slaughter);

        SkillDefinition chain = CreateAttack(folder, "ally_mid_chain_execution", "연쇄 처형", SkillClass.Mid, CharacterRangeType.Mid, 0, 3, 0, 3, 1, 50);
        chain.primaryHitCount = 2;
        chain.activeGimmick = ActiveSkillGimmick.ChainExecutionOnce;
        chain.description = "50% 피해 2회. 이 스킬로 적 처치 시 무작위 적에게 동일 위력으로 1회 재시전.";
        list.Add(chain);

        return list;
    }

    private List<SkillDefinition> CreateRangedSkills()
    {
        string folder = Fallback(rangedFolder, DefaultRangedFolder);
        List<SkillDefinition> list = new List<SkillDefinition>();

        SkillDefinition binding = CreateAttack(folder, "ally_ranged_dark_binding", "어둠의 구속", SkillClass.Ranged, CharacterRangeType.Ranged, 2, 3, 0, 3, 3, 60,
            StatusEffectType.Frost, 70f, 5);
        binding.targetScope = TargetScope.All;
        binding.description = "적 전체 피해 60%. 둔화는 동상 5스택으로 대체하여 70% 확률로 부여합니다.";
        list.Add(binding);

        SkillDefinition spear = CreateAttack(folder, "ally_ranged_abyss_spear", "심연의 창", SkillClass.Ranged, CharacterRangeType.Ranged, 1, 3, 0, 3, 1, 75);
        spear.secondaryTargetRule = SecondaryTargetRule.BackOne;
        spear.secondaryDamagePercent = 75f;
        spear.secondaryAccuracyCoefficientPercent = 100f;
        spear.secondaryApplyNonDamageEffects = false;
        spear.missingSecondaryTargetDamagePowerPercent = 130f;
        spear.description = "타겟과 그 바로 뒷자리 적에게 75% 관통 피해. 뒤 대상이 없으면 주 대상에게 130% 추가 보정 타격.";
        list.Add(spear);

        SkillDefinition harvest = CreateAttack(folder, "ally_ranged_soul_harvest", "영혼 수확", SkillClass.Ranged, CharacterRangeType.Ranged, 0, 3, 0, 2, 2, 80);
        harvest.activeGimmick = ActiveSkillGimmick.ShieldSelfFromDamageDealt;
        harvest.selfShieldFromDamageDealtPercent = 100f;
        harvest.description = "피해 80%. 입힌 HP 피해량의 100%만큼 자신에게 보호막 획득.";
        list.Add(harvest);

        SkillDefinition lightning = CreateAttack(folder, "ally_ranged_chain_lightning", "연쇄 번개", SkillClass.Ranged, CharacterRangeType.Ranged, 0, 3, 0, 3, 2, 100);
        lightning.activeGimmick = ActiveSkillGimmick.ChainLightning;
        lightning.chainLightningFirstJumpPowerPercent = 50f;
        lightning.chainLightningSecondJumpPowerPercent = 25f;
        lightning.description = "지정 대상에게 100% 피해 후 무작위 적에게 50%, 25% 연쇄 피해.";
        list.Add(lightning);

        SkillDefinition apocalypse = CreateAttack(folder, "ally_ranged_apocalypse", "종말", SkillClass.Ranged, CharacterRangeType.Ranged, 2, 3, 0, 3, 4, 70,
            StatusEffectType.Burn, 80f, 1);
        apocalypse.targetScope = TargetScope.All;
        apocalypse.description = "적 전체 피해 70%. 화상 80%.";
        list.Add(apocalypse);

        return list;
    }

    private List<SkillDefinition> CreateCommonSkills()
    {
        string folder = Fallback(commonFolder, DefaultCommonFolder);
        List<SkillDefinition> list = new List<SkillDefinition>();

        SkillDefinition bloodBlessing = CreateSuccessSkill(folder, "ally_common_blood_blessing", "피의 축복", SkillClass.Common, CharacterRangeType.Mid, 0, 3, SkillTargetTeam.Ally, TargetScope.Single, 2,
            Remove(StatusEffectType.Stun), Remove(StatusEffectType.Bleed), Remove(StatusEffectType.Burn), Remove(StatusEffectType.Frost), Remove(StatusEffectType.Blind), Remove(StatusEffectType.Hunting), Status(StatusEffectType.LifeSteal, 100f, 2, false));
        bloodBlessing.description = "대상 아군의 디버프를 해제하고 흡혈 2스택을 부여합니다. 흡혈은 스택 수와 무관하게 30% 고정입니다.";
        list.Add(bloodBlessing);

        SkillDefinition bond = CreateSuccessSkill(folder, "ally_common_ominous_bond", "불길한 결속", SkillClass.Common, CharacterRangeType.Mid, 0, 3, SkillTargetTeam.Ally, TargetScope.Single, 3,
            Effect(BattleEffectKind.Shield, 30, EffectValueReference.TargetMaxHP));
        bond.alsoApplyToSelfWhenTargetingAlly = true;
        bond.description = "자신과 대상 아군 1명에게 최대 체력 30% 보호막.";
        list.Add(bond);

        SkillDefinition wildHunt = CreateSuccessSkill(folder, "ally_common_wild_hunt", "와일드 헌트", SkillClass.Common, CharacterRangeType.Mid, 0, 3, SkillTargetTeam.Ally, TargetScope.All, 3,
            Buff(StatModifierType.HIT, 20, 2));
        wildHunt.description = "아군 전체에게 명중 +10% 2스택(총 +20%) 버프.";
        list.Add(wildHunt);

        return list;
    }

    private List<SkillDefinition> CreateHeroSkills()
    {
        string folder = Fallback(uniqueFolder, DefaultUniqueFolder);
        List<SkillDefinition> list = new List<SkillDefinition>();

        SkillDefinition harvest = CreateAttack(folder, "hero_harvest", "수확", SkillClass.Unique, CharacterRangeType.Mid, 0, 3, 0, 3, 0, 100,
            StatusEffectType.Bleed, 100f, 1);
        harvest.isBasicAttack = true;
        harvest.description = "적 1대상 공격 및 출혈 1스택 부여.";
        list.Add(harvest);

        SkillDefinition meteor = CreateAttack(folder, "hero_collapse_meteor", "붕괴의 유성", SkillClass.Unique, CharacterRangeType.Mid, 0, 3, 0, 3, 1, 40,
            StatusEffectType.Burn, 70f, 1);
        meteor.targetScope = TargetScope.All;
        meteor.description = "적 전체 광역 피해. 현재 구현은 각 대상 40% 피해로 임시 처리. 화상 70%.";
        list.Add(meteor);

        SkillDefinition rift = CreateAttack(folder, "hero_dimensional_rift", "차원 균열", SkillClass.Unique, CharacterRangeType.Mid, 0, 3, 0, 3, 1, 100,
            StatusEffectType.Stun, 70f, 1);
        rift.activeGimmick = ActiveSkillGimmick.RandomRepositionTargetsOnHit;
        rift.randomRepositionChancePercent = 80f;
        rift.description = "적 1개체를 공격하고 80% 확률로 강제 무작위 이동. 이후 기절 70%.";
        list.Add(rift);

        SkillDefinition drain = CreateAttack(folder, "hero_soul_drain", "영혼 착취", SkillClass.Unique, CharacterRangeType.Mid, 0, 3, 0, 3, 2, 120);
        drain.activeGimmick = ActiveSkillGimmick.BleedDrainStrike;
        drain.effects.Add(Effect(BattleEffectKind.Heal, 25, EffectValueReference.ActorDMG));
        drain.effects.Add(Buff(StatModifierType.DMG, 20, 2));
        drain.description = "피해 120%. 준 HP 피해의 25% 회복 및 2턴간 DMG +20%.";
        list.Add(drain);

        return list;
    }

    private void CreateOrUpdatePoolTable(List<SkillDefinition> common, List<SkillDefinition> melee, List<SkillDefinition> mid, List<SkillDefinition> ranged)
    {
        EnsureFolder(tableFolder);
        string path = Fallback(tableFolder, DefaultTableFolder).TrimEnd('/') + "/AllySkillLearnPoolTable.asset";
        SkillLearnPoolTable table = AssetDatabase.LoadAssetAtPath<SkillLearnPoolTable>(path);
        if (table == null)
        {
            table = ScriptableObject.CreateInstance<SkillLearnPoolTable>();
            AssetDatabase.CreateAsset(table, path);
        }

        table.commonSkills = new List<SkillDefinition>(common);
        table.meleeSkills = new List<SkillDefinition>(melee);
        table.midSkills = new List<SkillDefinition>(mid);
        table.rangedSkills = new List<SkillDefinition>(ranged);
        EditorUtility.SetDirty(table);
    }

    private void AssignHeroSkills(List<SkillDefinition> heroSkills)
    {
        if (mainPlayerUnitDefinition == null || heroSkills == null || heroSkills.Count == 0)
            return;

        mainPlayerUnitDefinition.basicAttack = heroSkills[0];
        mainPlayerUnitDefinition.fixedStartingSkills = new List<SkillDefinition>();
        for (int i = 1; i < heroSkills.Count && mainPlayerUnitDefinition.fixedStartingSkills.Count < 3; i++)
            mainPlayerUnitDefinition.fixedStartingSkills.Add(heroSkills[i]);

        mainPlayerUnitDefinition.isMainPlayerCharacter = true;
        EditorUtility.SetDirty(mainPlayerUnitDefinition);
    }

    private SkillDefinition CreateAttack(string folder, string id, string displayName, SkillClass skillClass, CharacterRangeType rangeTag, int usableMin, int usableMax, int targetMin, int targetMax, int cooldown, int damagePercent,
        StatusEffectType status = StatusEffectType.None, float statusChance = 0f, int statusDuration = 0, bool affectedByResistance = true)
    {
        SkillDefinition skill = LoadOrCreate(folder, id, displayName);
        ResetSkill(skill, id, displayName, skillClass, rangeTag);
        skill.isBasicAttack = false;
        skill.castType = SkillCastType.Active;
        skill.activeRole = ActiveSkillRole.Attack;
        skill.usableMinSlotIndex = usableMin;
        skill.usableMaxSlotIndex = usableMax;
        skill.targetMinSlotIndex = targetMin;
        skill.targetMaxSlotIndex = targetMax;
        skill.targetTeam = SkillTargetTeam.Enemy;
        skill.targetScope = TargetScope.Single;
        skill.resolutionMode = SkillResolutionMode.Attack;
        skill.cooldownTurns = cooldown;
        skill.effects.Add(Effect(BattleEffectKind.Damage, damagePercent, EffectValueReference.ActorDMG));
        if (status != StatusEffectType.None && statusChance > 0f && statusDuration > 0)
            skill.effects.Add(Status(status, statusChance, statusDuration, affectedByResistance));
        EditorUtility.SetDirty(skill);
        return skill;
    }

    private SkillDefinition CreateSuccessSkill(string folder, string id, string displayName, SkillClass skillClass, CharacterRangeType rangeTag, int usableMin, int usableMax, SkillTargetTeam targetTeam, TargetScope targetScope, int cooldown, params BattleEffectBlock[] effects)
    {
        SkillDefinition skill = LoadOrCreate(folder, id, displayName);
        ResetSkill(skill, id, displayName, skillClass, rangeTag);
        skill.isBasicAttack = false;
        skill.castType = SkillCastType.Active;
        skill.activeRole = targetTeam == SkillTargetTeam.Enemy ? ActiveSkillRole.Debuff : ActiveSkillRole.Buff;
        skill.usableMinSlotIndex = usableMin;
        skill.usableMaxSlotIndex = usableMax;
        skill.targetMinSlotIndex = 0;
        skill.targetMaxSlotIndex = 3;
        skill.targetTeam = targetTeam;
        skill.targetScope = targetScope;
        skill.resolutionMode = SkillResolutionMode.SuccessOnly;
        skill.cooldownTurns = cooldown;
        skill.allowCrit = false;
        skill.allowGraze = false;
        skill.effects = new List<BattleEffectBlock>(effects);
        EditorUtility.SetDirty(skill);
        return skill;
    }

    private void ResetSkill(SkillDefinition skill, string id, string displayName, SkillClass skillClass, CharacterRangeType rangeTag)
    {
        skill.skillId = id;
        skill.skillName = displayName;
        skill.description = string.Empty;
        skill.icon = null;
        skill.isBasicAttack = false;
        skill.castType = SkillCastType.Active;
        skill.activeRole = ActiveSkillRole.Attack;
        skill.skillClass = skillClass;
        skill.rangeTag = rangeTag;
        skill.passiveGimmick = PassiveSkillGimmick.None;
        skill.activeGimmick = ActiveSkillGimmick.None;
        skill.usableMinSlotIndex = 0;
        skill.usableMaxSlotIndex = 3;
        skill.targetMinSlotIndex = 0;
        skill.targetMaxSlotIndex = 3;
        skill.targetTeam = SkillTargetTeam.Enemy;
        skill.targetScope = TargetScope.Single;
        skill.requiredSelfStatusToUse = StatusEffectType.None;
        skill.blockedSelfStatusToUse = StatusEffectType.None;
        skill.onlyUsableWhenAlone = false;
        skill.requireOwnTeamLivingCountAtOrBelow = false;
        skill.maxOwnTeamLivingCountToUse = 3;
        skill.resolutionMode = SkillResolutionMode.Attack;
        skill.primaryHitCount = 1;
        skill.cooldownTurns = 0;
        skill.initialCooldownTurns = 0;
        skill.accuracyCoefficientPercent = 100f;
        skill.allowCrit = true;
        skill.allowGraze = true;
        skill.alsoApplyToSelfWhenTargetingAlly = false;
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
        skill.targetStatusBonusType = StatusEffectType.None;
        skill.targetStatusBonusPowerAddPercent = 0f;
        skill.missingSecondaryTargetDamagePowerPercent = 0f;
        skill.randomRepositionChancePercent = 20f;
        skill.delayedReinforcementDelayRounds = 2;
        skill.abyssReboundRecoilPercentFromTotalDamage = 20f;
        skill.selfShieldFromDamageDealtPercent = 100f;
        skill.chainLightningFirstJumpPowerPercent = 50f;
        skill.chainLightningSecondJumpPowerPercent = 25f;
        skill.blackArenaDuelDurationTurns = 2;
        skill.summonUnitDefinition = null;
        skill.summonUnitViewDefinition = null;
        skill.summonLevelOverride = 0;
        skill.maxLivingAlliesForSummon = 3;
        skill.effects = new List<BattleEffectBlock>();
        skill.disableAfterUseInBattle = false;
    }

    private BattleEffectBlock Effect(BattleEffectKind kind, float powerPercent, EffectValueReference reference)
    {
        return new BattleEffectBlock
        {
            kind = kind,
            powerPercent = powerPercent,
            valueReference = reference,
            successChancePercent = 100f,
            affectedByResistance = false,
            durationTurns = 0
        };
    }

    private BattleEffectBlock Status(StatusEffectType status, float chance, int duration, bool affectedByResistance = true)
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

    private BattleEffectBlock Remove(StatusEffectType status)
    {
        return new BattleEffectBlock
        {
            kind = BattleEffectKind.RemoveStatus,
            statusType = status,
            successChancePercent = 100f,
            affectedByResistance = false,
            durationTurns = 0
        };
    }

    private BattleEffectBlock Buff(StatModifierType stat, int percent, int duration)
    {
        return new BattleEffectBlock
        {
            kind = BattleEffectKind.Buff,
            statModifierType = stat,
            flatValue = Mathf.Abs(percent),
            durationTurns = duration,
            successChancePercent = 100f,
            affectedByResistance = false
        };
    }

    private SkillDefinition LoadOrCreate(string folder, string id, string displayName)
    {
        EnsureFolder(folder);
        string path = folder.TrimEnd('/') + "/" + id + ".asset";
        SkillDefinition skill = AssetDatabase.LoadAssetAtPath<SkillDefinition>(path);
        if (skill != null)
        {
            if (!overwriteExisting)
                return skill;
            return skill;
        }

        skill = ScriptableObject.CreateInstance<SkillDefinition>();
        skill.name = id;
        AssetDatabase.CreateAsset(skill, path);
        return skill;
    }

    private void EnsureFolder(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder))
            return;

        folder = folder.Replace('\\', '/').Trim('/');
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

    private string Fallback(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}
#endif
