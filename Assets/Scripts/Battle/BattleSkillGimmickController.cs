using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BattleSkillGimmickController : MonoBehaviour
{
    private class PendingReinforcement
    {
        public TeamType team;
        public PartyMemberData sourceData;
        public SkillDefinition sourceSkill;
        public int resolveRound;
        public string sourceUnitName;
        public string skillName;
    }

    private BattleManager battleManager;
    private BattleLogController logController;
    private readonly List<PendingReinforcement> pendingReinforcements = new List<PendingReinforcement>();

    public void Initialize(BattleManager manager, BattleLogController log)
    {
        battleManager = manager;
        logController = log;
        ResetRuntimeState();
    }

    public void ResetRuntimeState()
    {
        pendingReinforcements.Clear();
    }

    public void EvaluateAfterTurnEnd(BattleUnit endedTurnUnit, bool allyDeathOccurredThisTurn, bool enemyDeathOccurredThisTurn)
    {
        if (battleManager == null || battleManager.BattleResult != BattleResultType.None)
            return;

        if (allyDeathOccurredThisTurn)
            ArmReinforcementSkillsForFormation(battleManager.AllyFormation);

        if (enemyDeathOccurredThisTurn)
            ArmReinforcementSkillsForFormation(battleManager.EnemyFormation);
    }

    public IEnumerator ResolveRoundStartGimmicks(int round)
    {
        if (battleManager == null || battleManager.BattleResult != BattleResultType.None)
            yield break;

        List<PendingReinforcement> ready = new List<PendingReinforcement>();
        for (int i = 0; i < pendingReinforcements.Count; i++)
        {
            PendingReinforcement pending = pendingReinforcements[i];
            if (pending != null && pending.resolveRound <= round)
                ready.Add(pending);
        }

        if (ready.Count <= 0)
            yield break;

        for (int i = 0; i < ready.Count; i++)
        {
            PendingReinforcement pending = ready[i];
            pendingReinforcements.Remove(pending);

            if (battleManager.BattleResult != BattleResultType.None)
                yield break;

            yield return StartCoroutine(SpawnPendingReinforcement(pending));
        }
    }

    public void OnSkillExecuted(BattleUnit actor, SkillDefinition skill)
    {
        if (battleManager == null || actor == null || skill == null)
            return;

        switch (skill.activeGimmick)
        {
            case ActiveSkillGimmick.DelayedReinforcement:
                actor.ConsumeConditionalSkillArm(skill);
                QueueDelayedReinforcement(actor, skill);
                break;
        }
    }

    private void ArmReinforcementSkillsForFormation(BattleFormation formation)
    {
        if (formation == null)
            return;

        List<BattleUnit> aliveUnits = formation.GetAliveUnits();
        if (aliveUnits.Count <= 0 || aliveUnits.Count > 3)
            return;

        for (int i = 0; i < aliveUnits.Count; i++)
        {
            BattleUnit unit = aliveUnits[i];
            if (unit == null || unit.IsDead)
                continue;

            SkillDefinition skill;
            if (!unit.TryGetActiveSkillByGimmick(ActiveSkillGimmick.DelayedReinforcement, out skill))
                continue;

            if (skill == null || unit.IsSkillDisabled(skill))
                continue;

            if (!unit.TryArmConditionalSkill(skill))
                continue;

            AppendLog(string.Format("{0}의 {1} 발동 조건 충족 → 다음 턴부터 사용 가능", unit.Name, GetSkillName(skill)));
        }
    }

    private void QueueDelayedReinforcement(BattleUnit actor, SkillDefinition skill)
    {
        if (actor == null || skill == null || battleManager == null)
            return;

        PendingReinforcement pending = new PendingReinforcement();
        pending.team = actor.Team;
        pending.sourceData = ClonePartyMemberData(actor.MemberData);
        pending.sourceSkill = skill;

        int delayRounds = skill.GetDelayedReinforcementDelayRounds();

        // 라운드 시작 시 증원이 처리되므로
        // "N라운드 후"는 현재 라운드 이후 N개 라운드를 모두 지난 다음,
        // 그 다음 라운드 시작에 오도록 + (N + 1)
        pending.resolveRound = Mathf.Max(1, battleManager.CurrentRound + delayRounds + 1);
        pending.sourceUnitName = actor.Name;
        pending.skillName = GetSkillName(skill);
        pendingReinforcements.Add(pending);

        actor.DisableSkill(skill);

        AppendLog(string.Format(
            "{0}의 {1}: {2}라운드 후 증원 예약",
            actor.Name,
            pending.skillName,
            delayRounds));
    }

    private IEnumerator SpawnPendingReinforcement(PendingReinforcement pending)
    {
        if (pending == null || pending.sourceData == null)
            yield break;

        BattleFormation formation = pending.team == TeamType.Ally
            ? battleManager.AllyFormation
            : battleManager.EnemyFormation;

        if (formation == null)
            yield break;

        int emptySlot = FindFirstEmptySlot(formation);
        if (emptySlot < 0)
        {
            AppendLog(string.Format("{0}의 {1}: 증원 취소 (빈 자리 없음)", pending.sourceUnitName, pending.skillName));
            yield break;
        }

        PartyMemberData member = CreateReinforcementMemberData(pending.sourceData, emptySlot, pending.team);
        BattleUnit newUnit = new BattleUnit(member, pending.team);

        newUnit.DisableSkill(pending.sourceSkill);

        formation.SetUnit(emptySlot, newUnit);

        if (battleManager.ViewManager != null && battleManager.InputController != null)
        {
            battleManager.ViewManager.CreateView(newUnit, battleManager.InputController);
            battleManager.ViewManager.RefreshAllPositionsInstant(battleManager.AllyFormation, battleManager.EnemyFormation);
        }

        AppendLog(string.Format("{0}의 {1}: {2} 증원 도착", pending.sourceUnitName, pending.skillName, newUnit.Name));
        battleManager.RefreshAllUI();
        yield return null;
    }

    private PartyMemberData CreateReinforcementMemberData(PartyMemberData source, int slotIndex, TeamType team)
    {
        PartyMemberData member = new PartyMemberData();
        member.unitDefinition = source != null ? source.unitDefinition : null;
        member.unitViewDefinition = source != null ? source.unitViewDefinition : null;
        member.startSlotIndex = Mathf.Clamp(slotIndex, 0, 3);
        member.currentLevel = source != null ? source.currentLevel : 1;
        member.originalLevel = source != null ? source.originalLevel : member.currentLevel;
        member.instanceId = BuildRuntimeInstanceId(member.unitDefinition, team, slotIndex);
        member.instanceDisplayNameOverride = source != null ? source.instanceDisplayNameOverride : string.Empty;
        member.fixedEpitaph = source != null ? source.fixedEpitaph : string.Empty;
        member.statVariance = RollVariance(member.unitDefinition != null ? member.unitDefinition.varianceRules : null);
        member.learnedSkills = CopySkills(source != null ? source.learnedSkills : null);
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
        return Random.Range(min, max + 1);
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
        }

        return copied;
    }

    private PartyMemberData ClonePartyMemberData(PartyMemberData source)
    {
        PartyMemberData clone = new PartyMemberData();
        if (source == null)
            return clone;

        clone.unitDefinition = source.unitDefinition;
        clone.unitViewDefinition = source.unitViewDefinition;
        clone.startSlotIndex = source.startSlotIndex;
        clone.instanceId = source.instanceId;
        clone.instanceDisplayNameOverride = source.instanceDisplayNameOverride;
        clone.fixedEpitaph = source.fixedEpitaph;
        clone.currentLevel = source.currentLevel;
        clone.originalLevel = source.originalLevel;
        clone.statVariance = source.statVariance;
        clone.learnedSkills = CopySkills(source.learnedSkills);
        return clone;
    }

    private int FindFirstEmptySlot(BattleFormation formation)
    {
        if (formation == null)
            return -1;

        for (int i = 0; i < 4; i++)
        {
            if (formation.GetUnit(i) == null)
                return i;
        }

        return -1;
    }

    private string BuildRuntimeInstanceId(UnitDefinition definition, TeamType team, int slotIndex)
    {
        string unitId = definition != null && !string.IsNullOrEmpty(definition.unitId)
            ? definition.unitId
            : (definition != null ? definition.name : "reinforcement");

        return string.Format("reinforcement_{0}_{1}_{2}_{3}", team, unitId, slotIndex, Random.Range(1000, 9999));
    }

    private string GetSkillName(SkillDefinition skill)
    {
        return skill != null && !string.IsNullOrEmpty(skill.skillName)
            ? skill.skillName
            : "스킬";
    }

    private void AppendLog(string text)
    {
        if (logController != null && !string.IsNullOrEmpty(text))
            logController.AppendBattleLog(text);
    }
}