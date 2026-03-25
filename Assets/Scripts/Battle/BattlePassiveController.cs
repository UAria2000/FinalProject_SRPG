using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BattlePassiveController : MonoBehaviour
{
    private BattleManager battleManager;
    private BattleLogController logController;

    public void Initialize(BattleManager manager, BattleLogController log)
    {
        battleManager = manager;
        logController = log;
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
