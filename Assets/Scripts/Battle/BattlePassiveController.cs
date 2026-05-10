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

        yield return StartCoroutine(ResolveTurnStartAlwaysOnPassives(actingUnit));

        SkillDefinition nextTurnFleeSkill = actingUnit.PeekPendingNextTurnFleeSkill();
        if (nextTurnFleeSkill != null)
        {
            actingUnit.ConsumePendingNextTurnFleeSkill();
            yield return StartCoroutine(ExecuteGuaranteedFlee(actingUnit, nextTurnFleeSkill));
            yield break;
        }

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

        float gainPercent = passiveSkill.GetBlackAuraShieldGainPercentFromHpDamage();
        int flatBonus = passiveSkill.GetBlackAuraShieldFlatBonus();

        int shieldAmount = Mathf.Max(0, Mathf.FloorToInt(hpDamageTaken * (gainPercent * 0.01f)) + flatBonus);
        if (shieldAmount <= 0)
            return;

        int actualShield = defender.AddShield(shieldAmount);
        string skillName = GetPassiveSkillName(passiveSkill);

        AppendLog(string.Format(
            "{0}의 {1} 발동 → 보호막 {2}",
            defender.Name,
            skillName,
            actualShield));
    }

    public void ResolveBeforeDeadUnitsRemoved(List<BattleUnit> deadAllies, List<BattleUnit> deadEnemies)
    {
        if (battleManager == null || battleManager.BattleResult != BattleResultType.None)
            return;

        ResolveLinkedBossDeathPassivesForFormation(battleManager.AllyFormation, deadAllies);
        ResolveLinkedBossDeathPassivesForFormation(battleManager.EnemyFormation, deadEnemies);
    }

    private void ResolveLinkedBossDeathPassivesForFormation(BattleFormation formation, List<BattleUnit> deadUnits)
    {
        if (formation == null || deadUnits == null || deadUnits.Count <= 0)
            return;

        List<BattleUnit> units = formation.GetAllUnits();
        for (int i = 0; i < units.Count; i++)
        {
            BattleUnit unit = units[i];
            if (unit == null || unit.IsDead)
                continue;

            SkillDefinition enrageSkill;
            if (unit.TryGetPassiveSkillByGimmick(PassiveSkillGimmick.HumanJudgeEnrageWhenLinkedBossDies, out enrageSkill) &&
                enrageSkill != null &&
                !unit.IsSkillDisabled(enrageSkill) &&
                ContainsDeadUnitDefinition(deadUnits, enrageSkill.GetLinkedBossUnitDefinition()))
            {
                ApplyHumanJudgeEnrage(unit, enrageSkill);
            }

            SkillDefinition reviveSkill;
            if (unit.TryGetPassiveSkillByGimmick(PassiveSkillGimmick.HumanHighPriestReviveLinkedBossOnDeath, out reviveSkill) &&
                reviveSkill != null &&
                !unit.IsSkillDisabled(reviveSkill))
            {
                BattleUnit deadLinkedBoss = FindDeadUnitByDefinition(deadUnits, reviveSkill.GetLinkedBossUnitDefinition());
                if (deadLinkedBoss != null)
                    ReviveLinkedBoss(unit, deadLinkedBoss, reviveSkill);
            }
        }
    }

    private bool ContainsDeadUnitDefinition(List<BattleUnit> deadUnits, UnitDefinition definition)
    {
        return FindDeadUnitByDefinition(deadUnits, definition) != null;
    }

    private BattleUnit FindDeadUnitByDefinition(List<BattleUnit> deadUnits, UnitDefinition definition)
    {
        if (deadUnits == null || definition == null)
            return null;

        for (int i = 0; i < deadUnits.Count; i++)
        {
            BattleUnit unit = deadUnits[i];
            if (unit == null)
                continue;

            if (unit.Definition == definition && unit.IsDead)
                return unit;
        }

        return null;
    }

    private void ApplyHumanJudgeEnrage(BattleUnit judge, SkillDefinition passiveSkill)
    {
        if (judge == null || judge.IsDead || passiveSkill == null)
            return;

        int dmgPercent = passiveSkill.GetBossEnrageDmgPercent();
        int hitPercent = passiveSkill.GetBossEnrageHitPercent();
        int incomingDamageTakenPercent = passiveSkill.GetBossEnrageIncomingDamageTakenPercent();
        float healPercent = passiveSkill.GetBossEnrageHealMaxHpPercent();

        if (dmgPercent > 0)
            judge.AddPersistentBattleDmgModifierPercent(dmgPercent);

        if (hitPercent > 0)
            judge.AddPersistentBattleHitModifierPercent(hitPercent);

        if (incomingDamageTakenPercent > 0)
            judge.AddPersistentBattleIncomingDamageTakenPercent(incomingDamageTakenPercent);

        int healed = 0;
        if (healPercent > 0f)
        {
            int healAmount = Mathf.Max(1, Mathf.FloorToInt(judge.MaxHP * (healPercent * 0.01f)));
            healed = judge.Heal(healAmount);
        }

        judge.DisableSkill(passiveSkill);

        AppendLog(string.Format(
            "{0}의 {1} 발동 → DMG +{2}%, HIT +{3}%, 받는 피해 +{4}%, {5} 회복",
            judge.Name,
            GetPassiveSkillName(passiveSkill),
            dmgPercent,
            hitPercent,
            incomingDamageTakenPercent,
            healed));
    }

    private void ReviveLinkedBoss(BattleUnit reviver, BattleUnit target, SkillDefinition passiveSkill)
    {
        if (reviver == null || reviver.IsDead || target == null || passiveSkill == null)
            return;

        float revivePercent = passiveSkill.GetLinkedBossReviveHpPercent();
        target.ReviveWithHpPercent(revivePercent);
        battleManager.SuppressUnitUntilNextRound(target);

        int healed = 0;
        float reviverHealPercent = passiveSkill.GetBossReviverHealMaxHpPercent();
        if (reviverHealPercent > 0f)
        {
            int healAmount = Mathf.Max(1, Mathf.FloorToInt(reviver.MaxHP * (reviverHealPercent * 0.01f)));
            healed = reviver.Heal(healAmount);
        }

        reviver.DisableSkill(passiveSkill);

        AppendLog(string.Format(
            "{0}의 {1} 발동 → {2} HP {3}%로 소생, {0} {4} 회복",
            reviver.Name,
            GetPassiveSkillName(passiveSkill),
            target.Name,
            Mathf.RoundToInt(revivePercent),
            healed));
    }

    private IEnumerator ResolveTurnStartAlwaysOnPassives(BattleUnit actingUnit)
    {
        if (actingUnit == null || actingUnit.IsDead)
            yield break;

        SkillDefinition regenSkill;
        if (actingUnit.TryGetPassiveSkillByGimmick(PassiveSkillGimmick.HealSelfMaxHpPercentOnTurnStart, out regenSkill))
        {
            float healPercent = regenSkill.GetTurnStartSelfHealMaxHpPercent();
            if (healPercent > 0f)
            {
                int healAmount = Mathf.Max(1, Mathf.FloorToInt(actingUnit.MaxHP * (healPercent * 0.01f)));
                int healed = actingUnit.Heal(healAmount);
                if (healed > 0)
                {
                    AppendLog(string.Format(
                        "{0}의 {1} 발동 → {2} 회복",
                        actingUnit.Name,
                        GetPassiveSkillName(regenSkill),
                        healed));

                    BattleUnitView view = battleManager != null && battleManager.ViewManager != null
                        ? battleManager.ViewManager.GetView(actingUnit)
                        : null;
                    if (view != null)
                        yield return StartCoroutine(view.AnimateHPChange(0.1f));
                }
            }
        }
    }

    public static int GetActiveStatusResistanceAuraBonus(BattleUnit target)
    {
        if (target == null || target.IsDead)
            return 0;

        BattleManager manager = Object.FindFirstObjectByType<BattleManager>();
        if (manager == null)
            return 0;

        BattleFormation formation = target.Team == TeamType.Ally ? manager.AllyFormation : manager.EnemyFormation;
        if (formation == null)
            return 0;

        List<BattleUnit> allies = formation.GetAliveUnits();
        int totalBonus = 0;

        for (int i = 0; i < allies.Count; i++)
        {
            BattleUnit ally = allies[i];
            if (ally == null || ally.IsDead)
                continue;

            SkillDefinition auraSkill;
            if (!ally.TryGetPassiveSkillByGimmick(PassiveSkillGimmick.TeamStatusResistAuraWhileAlive, out auraSkill))
                continue;

            totalBonus += auraSkill.GetTeamStatusResistAuraPercent();
        }

        return Mathf.Max(0, totalBonus);
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

            ApplyBattleStartEnemyTeamDmgDown(sourceUnit, targetFormation, passiveSkill);
        }
    }

    private void ApplyBattleStartEnemyTeamDmgDown(BattleUnit sourceUnit, BattleFormation targetFormation, SkillDefinition passiveSkill)
    {
        if (sourceUnit == null || targetFormation == null || passiveSkill == null)
            return;

        int percent = passiveSkill.GetBattleStartEnemyTeamDmgDownPercent();
        if (percent <= 0)
            return;

        bool isPermanent = passiveSkill.IsBattleStartEnemyTeamDmgDownPermanent();
        int duration = passiveSkill.GetBattleStartEnemyTeamDmgDownDurationTurns();

        List<BattleUnit> targets = targetFormation.GetAliveUnits();
        bool anyApplied = false;

        for (int i = 0; i < targets.Count; i++)
        {
            BattleUnit target = targets[i];
            if (target == null || target.IsDead)
                continue;

            if (isPermanent)
            {
                target.AddPersistentBattleDmgModifierPercent(-percent);
                anyApplied = true;
            }
            else
            {
                bool applied = target.TryApplyTimedModifier(
                    StatModifierType.DMG,
                    -percent,
                    duration);

                anyApplied |= applied;
            }
        }

        if (!anyApplied)
            return;

        string skillName = GetPassiveSkillName(passiveSkill);
        if (isPermanent)
        {
            AppendLog(string.Format(
                "{0}의 {1} 발동 → 적 전체 DMG {2}% 감소 (전투 종료까지)",
                sourceUnit.Name,
                skillName,
                percent));
        }
        else
        {
            AppendLog(string.Format(
                "{0}의 {1} 발동 → 적 전체 DMG {2}% 감소 ({3}턴)",
                sourceUnit.Name,
                skillName,
                percent,
                duration));
        }
    }

    private void ApplyShieldThornsBleed(BattleUnit attacker, BattleUnit defender, SkillDefinition passiveSkill)
    {
        if (attacker == null || attacker.IsDead || passiveSkill == null)
            return;

        float baseChance = passiveSkill.GetShieldedHitBleedChancePercent();
        int stacks = passiveSkill.GetShieldedHitBleedStacks();

        int resist = attacker.BleedResist;
        int finalChance = Mathf.RoundToInt(baseChance * Mathf.Clamp01((100f - resist) / 100f));
        bool basePassed = Random.Range(0f, 100f) < Mathf.Clamp(baseChance, 0f, 100f);
        bool resistPassed = !basePassed ? false : Random.Range(0f, 100f) < Mathf.Clamp(100 - resist, 0, 100);
        bool success = basePassed && resistPassed;
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

        attacker.ApplyStatus(StatusEffectType.Bleed, stacks);

        AppendLog(string.Format(
            "{0}의 {1} 발동 → {2} 출혈 {3}스택 ({4}%)",
            defender.Name,
            skillName,
            attacker.Name,
            stacks,
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