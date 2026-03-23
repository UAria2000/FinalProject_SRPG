using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAIController : MonoBehaviour
{
    private BattleManager battleManager;
    private BattleActionController actionController;

    public void Initialize(BattleManager manager)
    {
        battleManager = manager;
        actionController = manager != null ? manager.ActionController : null;
    }

    public IEnumerator ExecuteTurn(BattleUnit enemy)
    {
        if (battleManager == null || actionController == null || enemy == null)
            yield break;

        SkillDefinition chosenSkill = null;
        BattleUnit chosenTarget = null;

        for (int i = 0; i < 4; i++)
        {
            SkillDefinition skill = enemy.GetActionSkillAt(i);
            if (skill == null || !enemy.CanUseSkill(skill))
                continue;

            List<BattleUnit> validTargets = BattleTargeting.GetValidSkillTargets(
                enemy,
                skill,
                battleManager.EnemyFormation,
                battleManager.AllyFormation);

            if (validTargets.Count > 0)
            {
                chosenSkill = skill;
                chosenTarget = validTargets[0];
                break;
            }
        }

        if (chosenSkill != null && chosenTarget != null)
            yield return StartCoroutine(actionController.ExecuteSkill(enemy, chosenSkill, chosenTarget));
        else
            battleManager.OnActionExecutionFinished(true);
    }
}
