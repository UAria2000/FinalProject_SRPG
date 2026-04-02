using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BattlePassiveController : MonoBehaviour
{
    private BattleManager battleManager;
    private BattleLogController logController;
    private bool battleStartPassivesResolved;

    public void Initialize(BattleManager manager, BattleLogController log)
    {
        battleManager = manager;
        logController = log;
        battleStartPassivesResolved = false;
    }

    public void ResolveBattleStartPassives()
    {
        if (battleManager == null || battleManager.BattleResult != BattleResultType.None)
            return;

        if (battleStartPassivesResolved)
            return;

        battleStartPassivesResolved = true;

        ResolveBattleStartPassivesForFormation(
            battleManager.AllyFormation,
            battleManager.EnemyFormation);

        ResolveBattleStartPassivesForFormation(
            battleManager.EnemyFormation,
            battleManager.AllyFormation);
    }

    public void EvaluateAfterTurnEnd(BattleUnit endedTurnUnit)
    {
        if (battleManager == null || battleManager.BattleResult != BattleResultType.None || endedTurnUnit == null)
            return;

        EvaluateLonePassiveForFormation(battleManager.AllyFormation, endedTurnUnit);
        EvaluateLonePassiveForFormation(battleManager.EnemyFormation, endedTurnUnit);
    }

    public IEnumerator ResolveTurnStartPassive(BattleUnit actingUnit)
    {
        if (battleManager == null || actingUnit == null || actingUnit.IsDead || !battleManager.IsUnitInBattle(actingUnit))
            yield break;

        SkillDefinition passiveSkill = actingUnit.PeekPendingPassiveSkill();
        if (passiveSkill == null)
            yield break;

        switch (passiveSkill.passiveGimmick)
        {
            case PassiveSkillGimmick.FleeNextTurnWhenAlone:
                actingUnit.ConsumePendingPassiveSkill();
                yield return StartCoroutine(ExecuteGuaranteedFlee(actingUnit, passiveSkill));
                yield break;
        }
    }

    public void ResolveAfterDirectAttackHit(BattleUnit attacker, BattleUnit defender, int defenderShieldBeforeHit)
    {
        if (attacker == null || defender == null)
            return;

        if (defenderShieldBeforeHit <= 0)
            return;

        SkillDefinition passiveSkill;
        if (!defender.TryGetPassiveSkillByGimmick(
                PassiveSkillGimmick.Bleed25ToAttackerWhenShieldedHit,
                out passiveSkill))
            return;

        ApplyShieldThornsBleed(attacker, defender, passiveSkill);
    }

    public void ResolveAfterDirectAttackDamageTaken(BattleUnit attacker, BattleUnit defender, int hpDamageTaken)
    {
        if (attacker == null || defender == null)
            return;

        if (hpDamageTaken <= 0)
            return;

        SkillDefinition passiveSkill;
        if (!defender.TryGetPassiveSkillByGimmick(
                PassiveSkillGimmick.BlackAuraShieldFromDamageTaken,
                out passiveSkill))
            return;

        int shieldAmount = defender.AddShield(hpDamageTaken);
        string skillName = GetPassiveSkillName(passiveSkill);

        AppendLog(string.Format(
            "{0}의 {1} 발동 → 보호막 {2}",
            defender.Name,
            skillName,
            shieldAmount));
    }

    private void ResolveBattleStartPassivesForFormation(BattleFormation sourceFormation, BattleFormation targetFormation)
    {
        if (sourceFormation == null || targetFormation == null)
            return;

        List<BattleUnit> sources = sourceFormation.GetAliveUnits();
        for (int i = 0; i < sources.Count; i++)
        {
            BattleUnit sourceUnit = sources[i];
            if (sourceUnit == null || sourceUnit.IsDead)
                continue;

            SkillDefinition passiveSkill;
            if (!sourceUnit.TryGetPassiveSkillByGimmick(
                    PassiveSkillGimmick.BattleStartEnemyTeamDmgDown10Permanent,
                    out passiveSkill))
                continue;

            ApplyBattleStartEnemyTeamDmgDown10Permanent(sourceUnit, targetFormation, passiveSkill);
        }
    }

    private void ApplyBattleStartEnemyTeamDmgDown10Permanent(BattleUnit sourceUnit, BattleFormation targetFormation, SkillDefinition passiveSkill)
    {
        if (sourceUnit == null || targetFormation == null)
            return;

        List<BattleUnit> targets = targetFormation.GetAliveUnits();
        bool anyApplied = false;

        for (int i = 0; i < targets.Count; i++)
        {
            BattleUnit target = targets[i];
            if (target == null || target.IsDead)
                continue;

            target.AddPersistentBattleDmgModifierPercent(-10);
            anyApplied = true;
        }

        if (anyApplied)
        {
            string skillName = GetPassiveSkillName(passiveSkill);
            AppendLog(string.Format(
                "{0}의 {1} 발동 → 적 전체 DMG 10% 감소 (전투 종료까지, 중첩 가능)",
                sourceUnit.Name,
                skillName));
        }
    }

    private void ApplyShieldThornsBleed(BattleUnit attacker, BattleUnit defender, SkillDefinition passiveSkill)
    {
        if (attacker == null || attacker.IsDead)
            return;

        int baseChance = 100;
        int resist = attacker.BleedResist;
        int finalChance = Mathf.RoundToInt(baseChance * Mathf.Clamp01((100f - resist) / 100f));

        bool success = Random.Range(0f, 100f) < finalChance;
        string skillName = GetPassiveSkillName(passiveSkill);

        if (!success)
        {
            AppendLog(string.Format(
                "{0}의 {1} 발동 → {2} 출혈 실패 ({3}%)",
                defender.Name,
                skillName,
                attacker.Name,
                finalChance));
            return;
        }

        attacker.ApplyStatus(StatusEffectType.Bleed, 1);

        AppendLog(string.Format(
            "{0}의 {1} 발동 → {2} 출혈 1스택 ({3}%)",
            defender.Name,
            skillName,
            attacker.Name,
            finalChance));
    }

    private void EvaluateLonePassiveForFormation(BattleFormation formation, BattleUnit endedTurnUnit)
    {
        if (formation == null)
            return;

        List<BattleUnit> aliveUnits = formation.GetAliveUnits();
        if (aliveUnits.Count != 1)
            return;

        BattleUnit loneUnit = aliveUnits[0];
        if (loneUnit == null || loneUnit == endedTurnUnit || loneUnit.IsDead)
            return;

        if (!battleManager.IsUnitInBattle(loneUnit))
            return;

        SkillDefinition passiveSkill;
        if (!loneUnit.TryGetPassiveSkillByGimmick(PassiveSkillGimmick.FleeNextTurnWhenAlone, out passiveSkill))
            return;

        if (!loneUnit.TryArmPendingPassiveSkill(passiveSkill))
            return;

        string skillName = GetPassiveSkillName(passiveSkill);
        AppendLog(string.Format("{0}의 {1} 발동: 혼자 남아 다음 자기 턴 시작 시 도주", loneUnit.Name, skillName));
    }

    private IEnumerator ExecuteGuaranteedFlee(BattleUnit actor, SkillDefinition passiveSkill)
    {
        if (actor == null || battleManager == null)
        {
            if (battleManager != null)
                battleManager.OnActionExecutionFinished(true);
            yield break;
        }

        battleManager.SetTurnState(TurnState.ExecutingAction);

        BattleFormation ownFormation = actor.Team == TeamType.Ally
            ? battleManager.AllyFormation
            : battleManager.EnemyFormation;

        string skillName = GetPassiveSkillName(passiveSkill);
        AppendLog(string.Format("{0}의 {1} 발동 → 전투에서 이탈", actor.Name, skillName));

        ownFormation.RemoveUnit(actor);
        battleManager.NotifyUnitLeftBattle(actor);
        yield return StartCoroutine(battleManager.HandleDeathsAndCompressionRoutine());

        battleManager.OnActionExecutionFinished(true);
    }

    private string GetPassiveSkillName(SkillDefinition skill)
    {
        if (skill != null && !string.IsNullOrEmpty(skill.skillName))
            return skill.skillName;

        return "패시브";
    }

    private void AppendLog(string text)
    {
        if (logController != null && !string.IsNullOrEmpty(text))
            logController.AppendBattleLog(text);
    }
}