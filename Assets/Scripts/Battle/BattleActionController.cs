using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleActionController : MonoBehaviour
{
    private BattleManager battleManager;
    private BattleViewManager viewManager;
    private BattleLogController logController;

    public void Initialize(BattleManager manager, BattleViewManager view, BattleLogController log)
    {
        battleManager = manager;
        viewManager = view;
        logController = log;
    }

    public IEnumerator ExecuteSkill(BattleUnit actor, SkillDefinition skill, BattleUnit clickedTarget)
    {
        if (actor == null || skill == null)
        {
            battleManager.OnActionExecutionFinished(true);
            yield break;
        }

        List<BattleUnit> targets = BattleTargeting.ResolveSkillTargets(
            actor,
            skill,
            clickedTarget,
            battleManager.AllyFormation,
            battleManager.EnemyFormation);

        if (targets.Count <= 0)
        {
            battleManager.OnActionExecutionFinished(false);
            yield break;
        }

        battleManager.SetTurnState(TurnState.ExecutingAction);

        if (skill.IsEnemyTargetAttackSkill() && clickedTarget != null)
        {
            BattleUnitView actorView = viewManager.GetView(actor);
            BattleUnitView targetView = viewManager.GetView(clickedTarget);
            if (actorView != null && targetView != null)
                yield return StartCoroutine(actorView.PlayAttackMove(targetView.transform.position, battleManager.AttackMoveRatio, battleManager.AttackMoveMaxDistance, battleManager.AttackMoveDuration));
        }

        if (skill.resolutionMode == SkillResolutionMode.Attack && skill.HasDamageEffect())
        {
            for (int i = 0; i < targets.Count; i++)
            {
                AttackResult result = BattleCalculator.ResolveAttack(actor, targets[i], skill);
                if (result.DidHit)
                {
                    targets[i].ApplyDamage(result.Damage);
                    ApplyNonDamageEffects(actor, targets[i], skill.skillName, skill.effects, true);
                }

                logController.AppendBattleLog(logController.BuildAttackLog(actor, targets[i], skill, result));

                BattleUnitView view = viewManager.GetView(targets[i]);
                if (view != null)
                    yield return StartCoroutine(view.AnimateHPChange(0.15f));

                if (targets[i].IsDead)
                    logController.AppendBattleLog(logController.BuildDeathLog(targets[i]));
            }
        }
        else
        {
            for (int i = 0; i < targets.Count; i++)
            {
                ApplySuccessOnlyEffects(actor, targets[i], skill.skillName, skill.effects);
                BattleUnitView view = viewManager.GetView(targets[i]);
                if (view != null)
                    yield return StartCoroutine(view.AnimateHPChange(0.1f));
            }
        }

        actor.ConsumeSkillCooldown(skill);
        yield return StartCoroutine(battleManager.HandleDeathsAndCompressionRoutine());
        battleManager.OnActionExecutionFinished(true);
    }

    public IEnumerator ExecuteMove(BattleUnit actor, BattleUnit target)
    {
        if (actor == null || target == null)
        {
            battleManager.OnActionExecutionFinished(false);
            yield break;
        }

        battleManager.SetTurnState(TurnState.ExecutingAction);
        battleManager.AllyFormation.Swap(actor, target);
        logController.AppendBattleLog(logController.BuildMoveLog(actor, target));

        yield return StartCoroutine(viewManager.AnimateRefreshAllPositions(battleManager.AllyFormation, battleManager.EnemyFormation, battleManager.MoveAnimationDuration));
        battleManager.OnActionExecutionFinished(true);
    }

    public IEnumerator ExecuteItem(BattleUnit actor, int inventoryIndex, BattleUnit clickedTarget)
    {
        PartyDefinition allyParty = battleManager.AllyPartyDefinition;
        if (allyParty == null || inventoryIndex < 0 || inventoryIndex >= allyParty.inventory.Count)
        {
            battleManager.OnActionExecutionFinished(false);
            yield break;
        }

        InventoryStackData stack = allyParty.inventory[inventoryIndex];
        if (stack == null || stack.item == null || stack.amount <= 0)
        {
            battleManager.OnActionExecutionFinished(false);
            yield break;
        }

        ItemDefinition item = stack.item;
        List<BattleUnit> targets = BattleTargeting.ResolveItemTargets(
            actor,
            item,
            clickedTarget,
            battleManager.AllyFormation,
            battleManager.EnemyFormation);

        if (targets.Count <= 0)
        {
            battleManager.OnActionExecutionFinished(false);
            yield break;
        }

        battleManager.SetTurnState(TurnState.ExecutingAction);

        for (int i = 0; i < targets.Count; i++)
        {
            ApplyItemEffects(actor, targets[i], item);
            BattleUnitView view = viewManager.GetView(targets[i]);
            if (view != null)
                yield return StartCoroutine(view.AnimateHPChange(0.1f));
        }

        if (item.consumeOnUse)
        {
            stack.amount = Mathf.Max(0, stack.amount - 1);
            if (stack.amount <= 0)
                allyParty.inventory.RemoveAt(inventoryIndex);
        }

        yield return StartCoroutine(battleManager.HandleDeathsAndCompressionRoutine());
        battleManager.OnActionExecutionFinished(item.consumeTurnOnUse);
    }

    private void ApplyItemEffects(BattleUnit actor, BattleUnit target, ItemDefinition item)
    {
        if (item == null || item.effects == null)
            return;

        for (int i = 0; i < item.effects.Count; i++)
        {
            BattleEffectBlock block = item.effects[i];
            if (block == null) continue;

            int finalChance = BattleCalculator.CalculateEffectSuccessChance(block, target);
            bool success = Random.Range(0f, 100f) < finalChance;
            if (!success)
            {
                logController.AppendBattleLog(logController.BuildEffectFailureLog(actor, target, item.itemName, block.kind.ToString()));
                continue;
            }

            ApplyBlock(actor, target, item.itemName, block);
        }
    }

    private void ApplySuccessOnlyEffects(BattleUnit actor, BattleUnit target, string sourceName, List<BattleEffectBlock> effects)
    {
        for (int i = 0; i < effects.Count; i++)
        {
            BattleEffectBlock block = effects[i];
            if (block == null) continue;

            int finalChance = BattleCalculator.CalculateEffectSuccessChance(block, target);
            bool success = Random.Range(0f, 100f) < finalChance;
            if (!success)
            {
                logController.AppendBattleLog(logController.BuildEffectFailureLog(actor, target, sourceName, block.kind.ToString()));
                continue;
            }

            ApplyBlock(actor, target, sourceName, block);
        }
    }

    private void ApplyNonDamageEffects(BattleUnit actor, BattleUnit target, string sourceName, List<BattleEffectBlock> effects, bool onlyNonDamage)
    {
        for (int i = 0; i < effects.Count; i++)
        {
            BattleEffectBlock block = effects[i];
            if (block == null) continue;
            if (onlyNonDamage && block.kind == BattleEffectKind.Damage) continue;

            int finalChance = BattleCalculator.CalculateEffectSuccessChance(block, target);
            bool success = Random.Range(0f, 100f) < finalChance;
            if (!success)
            {
                logController.AppendBattleLog(logController.BuildEffectFailureLog(actor, target, sourceName, block.kind.ToString()));
                continue;
            }

            ApplyBlock(actor, target, sourceName, block);
        }
    }

    private void ApplyBlock(BattleUnit actor, BattleUnit target, string sourceName, BattleEffectBlock block)
    {
        switch (block.kind)
        {
            case BattleEffectKind.Heal:
                {
                    int amount = block.flatValue > 0 ? block.flatValue : Mathf.FloorToInt(actor.DMG * (block.powerPercent * 0.01f));
                    int healed = target.Heal(amount);
                    logController.AppendBattleLog(logController.BuildHealLog(actor, target, sourceName, healed));
                    break;
                }
            case BattleEffectKind.Shield:
                {
                    int amount = block.flatValue > 0 ? block.flatValue : Mathf.FloorToInt(actor.DMG * (block.powerPercent * 0.01f));
                    int shield = target.AddShield(amount);
                    logController.AppendBattleLog(logController.BuildShieldLog(actor, target, sourceName, shield));
                    break;
                }
            case BattleEffectKind.ApplyStatus:
                {
                    target.ApplyStatus(block.statusType, block.durationTurns);
                    logController.AppendBattleLog(logController.BuildEffectSuccessLog(actor, target, sourceName, block.statusType.ToString()));
                    break;
                }
            case BattleEffectKind.RemoveStatus:
                {
                    target.RemoveStatus(block.statusType);
                    logController.AppendBattleLog(logController.BuildEffectSuccessLog(actor, target, sourceName, block.statusType.ToString() + " 해제"));
                    break;
                }
            case BattleEffectKind.Buff:
            case BattleEffectKind.Debuff:
                {
                    logController.AppendBattleLog(logController.BuildEffectSuccessLog(actor, target, sourceName, block.kind.ToString()));
                    break;
                }
            case BattleEffectKind.Damage:
                {
                    int amount = block.flatValue > 0 ? block.flatValue : Mathf.FloorToInt(actor.DMG * (block.powerPercent * 0.01f));
                    target.ApplyDamage(amount);
                    logController.AppendBattleLog(string.Format("{0}의 {1} → {2}: {3}", actor.Name, sourceName, target.Name, amount));
                    if (target.IsDead)
                        logController.AppendBattleLog(logController.BuildDeathLog(target));
                    break;
                }
        }
    }
}
