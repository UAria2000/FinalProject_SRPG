using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleActionController : MonoBehaviour
{
    private const int EndTurnGuardPercent = 20;

    private BattleManager battleManager;
    private BattleViewManager viewManager;
    private BattleLogController logController;
    private int lastResolvedAttackHpDamageDealt;

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

        BattleFormation ownFormation = actor.Team == TeamType.Ally
            ? battleManager.AllyFormation
            : battleManager.EnemyFormation;

        BattleFormation opponentFormation = actor.Team == TeamType.Ally
            ? battleManager.EnemyFormation
            : battleManager.AllyFormation;

        List<BattleUnit> targets = BattleTargeting.ResolveSkillTargets(
            actor,
            skill,
            clickedTarget,
            ownFormation,
            opponentFormation);

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
                yield return StartCoroutine(actorView.PlayAttackMove(
                    targetView.transform.position,
                    battleManager.AttackMoveRatio,
                    battleManager.AttackMoveMaxDistance,
                    battleManager.AttackMoveDuration));
        }

        int rolledPrimaryDamagePercent = BattleCalculator.RollSkillDamagePowerPercent(skill);

        if (skill.resolutionMode == SkillResolutionMode.Attack && skill.HasDamageEffect())
        {
            int totalHpDamageDealt = 0;

            for (int i = 0; i < targets.Count; i++)
            {
                yield return StartCoroutine(ResolveAndApplyAttack(
                    actor,
                    skill,
                    targets[i],
                    rolledPrimaryDamagePercent,
                    -1f,
                    string.Empty,
                    true));
                totalHpDamageDealt += lastResolvedAttackHpDamageDealt;

                if (skill.HasSecondaryHit())
                {
                    BattleUnit secondaryTarget = BattleTargeting.GetSecondaryTarget(
                        actor,
                        skill,
                        targets[i],
                        battleManager.AllyFormation,
                        battleManager.EnemyFormation);

                    if (secondaryTarget != null && !secondaryTarget.IsDead)
                    {
                        yield return StartCoroutine(ResolveAndApplyAttack(
                            actor,
                            skill,
                            secondaryTarget,
                            skill.secondaryDamagePercent,
                            skill.secondaryAccuracyCoefficientPercent,
                            " [추가타격]",
                            skill.secondaryApplyNonDamageEffects));
                        totalHpDamageDealt += lastResolvedAttackHpDamageDealt;
                    }
                }

                if (actor.HasPierceBackOneBuff)
                {
                    BattleUnit pierceTarget = GetBackUnit(targets[i]);
                    if (pierceTarget != null && !pierceTarget.IsDead)
                    {
                        yield return StartCoroutine(ResolveAndApplyAttack(
                            actor,
                            skill,
                            pierceTarget,
                            rolledPrimaryDamagePercent,
                            -1f,
                            " [관통]",
                            false));
                        totalHpDamageDealt += lastResolvedAttackHpDamageDealt;
                    }
                }
            }

            if (skill.activeGimmick == ActiveSkillGimmick.AbyssReboundSelfRecoil20FromTotalDamage)
                yield return StartCoroutine(ApplyAbyssReboundSelfRecoil(actor, skill, totalHpDamageDealt));
        }
        else
        {
            for (int i = 0; i < targets.Count; i++)
            {
                BattleUnit primaryTarget = targets[i];

                ApplySuccessOnlyEffects(actor, primaryTarget, skill.skillName, skill.effects);

                BattleUnitView primaryView = viewManager.GetView(primaryTarget);
                if (primaryView != null)
                    yield return StartCoroutine(primaryView.AnimateHPChange(0.1f));

                if (skill.targetScope == TargetScope.Single &&
                    skill.secondaryTargetRule != SecondaryTargetRule.None)
                {
                    BattleUnit secondaryTarget = BattleTargeting.GetSecondaryTarget(
                        actor,
                        skill,
                        primaryTarget,
                        battleManager.AllyFormation,
                        battleManager.EnemyFormation);

                    if (secondaryTarget != null && !secondaryTarget.IsDead)
                    {
                        ApplySuccessOnlyEffects(actor, secondaryTarget, skill.skillName + " [후열]", skill.effects);

                        BattleUnitView secondaryView = viewManager.GetView(secondaryTarget);
                        if (secondaryView != null)
                            yield return StartCoroutine(secondaryView.AnimateHPChange(0.1f));
                    }
                }
            }
        }

        if (battleManager.SkillGimmickController != null)
            battleManager.SkillGimmickController.OnSkillExecuted(actor, skill);

        ApplySelfStatusAfterSkill(actor, skill);

        if (skill.disableAfterUseInBattle)
            actor.DisableSkill(skill);

        actor.ConsumeSkillCooldown(skill);
        yield return StartCoroutine(battleManager.HandleDeathsAndCompressionRoutine());
        yield return StartCoroutine(HandleSelfMoveAfterSkill(actor, skill));
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

        BattleFormation ownFormation = actor.Team == TeamType.Ally
            ? battleManager.AllyFormation
            : battleManager.EnemyFormation;

        ownFormation.Swap(actor, target);
        logController.AppendBattleLog(logController.BuildMoveLog(actor, target));

        yield return StartCoroutine(viewManager.AnimateRefreshAllPositions(
            battleManager.AllyFormation,
            battleManager.EnemyFormation,
            battleManager.MoveAnimationDuration));

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

        BattleFormation ownFormation = actor.Team == TeamType.Ally
            ? battleManager.AllyFormation
            : battleManager.EnemyFormation;

        BattleFormation opponentFormation = actor.Team == TeamType.Ally
            ? battleManager.EnemyFormation
            : battleManager.AllyFormation;

        List<BattleUnit> targets = BattleTargeting.ResolveItemTargets(
            actor,
            item,
            clickedTarget,
            ownFormation,
            opponentFormation);

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

    public IEnumerator ExecuteCapture(BattleUnit actor, BattleUnit target)
    {
        if (actor == null || target == null)
        {
            battleManager.OnActionExecutionFinished(false);
            yield break;
        }

        if (!battleManager.CanActorUseCaptureCommand(actor) || !battleManager.CanTargetBeCaptured(actor, target))
        {
            battleManager.OnActionExecutionFinished(false);
            yield break;
        }

        battleManager.SetTurnState(TurnState.ExecutingAction);

        if (!battleManager.TryConsumeCaptureAttempt(target))
        {
            battleManager.OnActionExecutionFinished(false);
            yield break;
        }

        int chancePercent = battleManager.GetCaptureChancePercent(target);
        bool success = Random.Range(0f, 100f) < chancePercent;

        if (!success)
        {
            logController.AppendBattleLog(logController.BuildCaptureFailureLog(actor, target, chancePercent));
            battleManager.OnActionExecutionFinished(true);
            yield break;
        }

        ItemDefinition capturedItem;
        if (!battleManager.TryAddCapturedRewardToInventory(target, out capturedItem))
        {
            battleManager.RefundCaptureAttempt(target);
            battleManager.OnActionExecutionFinished(false);
            yield break;
        }

        logController.AppendBattleLog(logController.BuildCaptureSuccessLog(actor, target, chancePercent));
        logController.AppendBattleLog(logController.BuildCaptureAcquiredLog(capturedItem));

        BattleFormation enemyFormation = actor.Team == TeamType.Ally
            ? battleManager.EnemyFormation
            : battleManager.AllyFormation;

        if (enemyFormation != null)
            enemyFormation.RemoveUnit(target);

        battleManager.NotifyUnitLeftBattle(target);
        yield return StartCoroutine(battleManager.HandleDeathsAndCompressionRoutine());
        battleManager.OnActionExecutionFinished(true);
    }

    public IEnumerator ExecuteFlee(BattleUnit actor)
    {
        if (actor == null)
        {
            battleManager.OnActionExecutionFinished(true);
            yield break;
        }

        battleManager.SetTurnState(TurnState.ExecutingAction);

        BattleFormation ownFormation = actor.Team == TeamType.Ally
            ? battleManager.AllyFormation
            : battleManager.EnemyFormation;

        int fleeChancePercent = BattleCalculator.CalculateFleeChancePercent(actor, battleManager.EnemyFormation);
        bool success = Random.Range(0f, 100f) < fleeChancePercent;

        if (success)
        {
            logController.AppendBattleLog(logController.BuildFleeSuccessLog(actor, fleeChancePercent));
            ownFormation.RemoveUnit(actor);
            battleManager.NotifyUnitLeftBattle(actor);
            yield return StartCoroutine(battleManager.HandleDeathsAndCompressionRoutine());
        }
        else
        {
            logController.AppendBattleLog(logController.BuildFleeFailureLog(actor, fleeChancePercent));
        }

        battleManager.OnActionExecutionFinished(true);
    }

    public IEnumerator ExecuteEndTurnGuard(BattleUnit actor)
    {
        if (actor == null)
        {
            battleManager.OnActionExecutionFinished(true);
            yield break;
        }

        battleManager.SetTurnState(TurnState.ExecutingAction);
        actor.ApplyEndTurnGuard(EndTurnGuardPercent);
        logController.AppendBattleLog(logController.BuildEndTurnGuardLog(actor, EndTurnGuardPercent));
        yield return null;
        battleManager.OnActionExecutionFinished(true);
    }

    private IEnumerator ResolveAndApplyAttack(
        BattleUnit actor,
        SkillDefinition skill,
        BattleUnit target,
        float damagePowerPercentOverride,
        float accuracyPercentOverride,
        string logSuffix,
        bool applyNonDamageEffects)
    {
        lastResolvedAttackHpDamageDealt = 0;

        if (actor == null || target == null || skill == null)
            yield break;

        float resolvedDamagePowerPercent = GetResolvedDamagePowerPercentForThisAttack(
            actor,
            target,
            skill,
            damagePowerPercentOverride,
            logSuffix);

        AttackResult result = BattleCalculator.ResolveAttack(
            actor,
            target,
            skill,
            accuracyPercentOverride,
            resolvedDamagePowerPercent);

        if (result.DidHit)
        {
            int originalDamage = result.Damage;
            result.Damage = target.ApplyIncomingAttackDamageReduction(result.Damage);

            int targetShieldBeforeHit = target.CurrentShield;
            int hpDamageDealt = target.ApplyDamage(result.Damage);
            lastResolvedAttackHpDamageDealt = hpDamageDealt;

            if (battleManager != null && battleManager.PassiveController != null)
            {
                battleManager.PassiveController.ResolveAfterDirectAttackHit(actor, target, targetShieldBeforeHit);
                battleManager.PassiveController.ResolveAfterDirectAttackDamageTaken(actor, target, hpDamageDealt);
            }

            if (applyNonDamageEffects)
            {
                if (skill.activeGimmick == ActiveSkillGimmick.BleedDrainStrike)
                    ApplyBleedDrainStrikeEffects(actor, target, skill, hpDamageDealt);
                else
                    ApplyNonDamageEffects(actor, target, skill.skillName, skill.effects, true);
            }

            if (string.IsNullOrEmpty(logSuffix) &&
                skill.activeGimmick == ActiveSkillGimmick.BlackArenaDuel2Turns &&
                actor != null && !actor.IsDead &&
                target != null && !target.IsDead)
            {
                ApplyBlackArenaDuel(actor, target, skill);
            }

            if (result.Damage < originalDamage)
                logController.AppendBattleLog(logController.BuildGuardReductionLog(target, originalDamage, result.Damage));
        }

        logController.AppendBattleLog(logController.BuildAttackLog(actor, target, skill, result, logSuffix));

        BattleUnitView view = viewManager.GetView(target);
        if (view != null)
            yield return StartCoroutine(view.AnimateHPChange(0.15f));

        if (target.IsDead)
        {
            logController.AppendBattleLog(logController.BuildDeathLog(target));
            yield break;
        }

        if (result.DidHit &&
            string.IsNullOrEmpty(logSuffix) &&
            target.HasStatus(StatusEffectType.CounterStance) &&
            actor != null &&
            !actor.IsDead)
        {
            yield return StartCoroutine(ExecuteReactiveCounterAttack(target, actor));
        }

        if (result.DidHit && string.IsNullOrEmpty(logSuffix))
            yield return StartCoroutine(HandleForcedTargetMoveAfterHit(actor, skill, target));
    }

    private float GetResolvedDamagePowerPercentForThisAttack(
        BattleUnit actor,
        BattleUnit target,
        SkillDefinition skill,
        float requestedDamagePowerPercent,
        string logSuffix)
    {
        float resolvedDamagePowerPercent = requestedDamagePowerPercent;

        if (actor == null || target == null || skill == null)
            return resolvedDamagePowerPercent;

        if (skill.HasMissingHpPowerBonus())
            resolvedDamagePowerPercent += skill.GetMissingHpBonusPowerPercent(actor);

        if (!string.IsNullOrEmpty(logSuffix))
            return resolvedDamagePowerPercent;

        if (!skill.HasForcedTargetPushBackAfterHit())
            return resolvedDamagePowerPercent;

        BattleFormation targetFormation = target.Team == TeamType.Ally
            ? battleManager.AllyFormation
            : battleManager.EnemyFormation;

        if (targetFormation == null || !targetFormation.Contains(target))
            return resolvedDamagePowerPercent;

        int steps = skill.GetForcedTargetMoveSteps();
        bool canMove = targetFormation.CanMoveUnitByDelta(target, steps);
        if (canMove)
            return resolvedDamagePowerPercent;

        float failFinalPowerPercent = skill.GetPushBackFailFinalPowerPercent();
        if (failFinalPowerPercent <= 0f)
            return resolvedDamagePowerPercent;

        if (failFinalPowerPercent > resolvedDamagePowerPercent)
            resolvedDamagePowerPercent = failFinalPowerPercent;

        return resolvedDamagePowerPercent;
    }

    private void ApplySelfStatusAfterSkill(BattleUnit actor, SkillDefinition skill)
    {
        if (actor == null || skill == null)
            return;

        if (actor.IsDead)
            return;

        if (!skill.HasSelfStatusAfterUse())
            return;

        actor.ApplyStatus(skill.selfApplyStatusAfterUse, skill.selfApplyStatusDurationTurns);

        if (logController != null)
        {
            logController.AppendBattleLog(
                logController.BuildEffectSuccessLog(
                    actor,
                    actor,
                    skill.skillName,
                    GetStatusDisplayName(skill.selfApplyStatusAfterUse)));
        }
    }

    private IEnumerator HandleSelfMoveAfterSkill(BattleUnit actor, SkillDefinition skill)
    {
        if (actor == null || skill == null || !skill.HasSelfMoveAfterUse())
            yield break;

        if (actor.IsPositionMovementLocked)
            yield break;

        BattleFormation ownFormation = actor.Team == TeamType.Ally
            ? battleManager.AllyFormation
            : battleManager.EnemyFormation;

        if (ownFormation == null || !ownFormation.Contains(actor))
            yield break;

        int fromSlot = actor.SlotIndex;
        int delta = 0;

        switch (skill.selfMoveDirection)
        {
            case SkillSelfMoveDirection.Forward:
                delta = -skill.selfMoveSteps;
                break;
            case SkillSelfMoveDirection.Backward:
                delta = skill.selfMoveSteps;
                break;
        }

        bool moved = ownFormation.MoveUnitByDelta(actor, delta);
        if (!moved)
            yield break;

        logController.AppendBattleLog(logController.BuildSelfSlideLog(actor, fromSlot, actor.SlotIndex));

        if (viewManager != null)
        {
            yield return StartCoroutine(viewManager.AnimateRefreshAllPositions(
                battleManager.AllyFormation,
                battleManager.EnemyFormation,
                battleManager.MoveAnimationDuration));
        }
    }

    private IEnumerator HandleForcedTargetMoveAfterHit(BattleUnit actor, SkillDefinition skill, BattleUnit target)
    {
        if (actor == null || skill == null || target == null)
            yield break;

        if (!skill.HasForcedTargetMoveAfterHit() && !skill.HasForcedTargetPushBackAfterHit())
            yield break;

        if (target.IsDead || !battleManager.IsUnitInBattle(target))
            yield break;

        BattleFormation targetFormation = target.Team == TeamType.Ally
            ? battleManager.AllyFormation
            : battleManager.EnemyFormation;

        if (targetFormation == null || !targetFormation.Contains(target))
            yield break;

        int fromSlotIndex = target.SlotIndex;
        bool moved = false;

        if (skill.HasForcedTargetMoveAfterHit())
        {
            int toSlotIndex = skill.GetForcedTargetMoveTargetSlotIndex();
            if (fromSlotIndex != toSlotIndex)
                moved = targetFormation.MoveUnitTo(target, toSlotIndex);
        }
        else if (skill.HasForcedTargetPushBackAfterHit())
        {
            int steps = skill.GetForcedTargetMoveSteps();
            moved = targetFormation.MoveUnitByDelta(target, steps);
        }

        if (!moved)
            yield break;

        logController.AppendBattleLog(
            logController.BuildForcedTargetMoveLog(actor, skill, target, fromSlotIndex, target.SlotIndex));

        if (viewManager != null)
        {
            yield return StartCoroutine(viewManager.AnimateRefreshAllPositions(
                battleManager.AllyFormation,
                battleManager.EnemyFormation,
                battleManager.MoveAnimationDuration));
        }
    }

    private SkillDefinition FindBasicAttackSkill(BattleUnit unit)
    {
        if (unit == null)
            return null;

        for (int i = 0; i < unit.GetActionSkillSlotCount(); i++)
        {
            SkillDefinition skill = unit.GetActionSkillAt(i);
            if (skill != null && skill.isBasicAttack)
                return skill;
        }

        return null;
    }

    private IEnumerator ExecuteReactiveCounterAttack(BattleUnit counterActor, BattleUnit originalAttacker)
    {
        if (counterActor == null || originalAttacker == null)
            yield break;

        if (counterActor.IsDead || originalAttacker.IsDead)
            yield break;

        SkillDefinition basicAttack = FindBasicAttackSkill(counterActor);
        if (basicAttack == null)
            yield break;

        AttackResult counterResult = BattleCalculator.ResolveAttack(
            counterActor,
            originalAttacker,
            basicAttack,
            100f,
            100f);

        if (counterResult.DidHit)
        {
            int originalDamage = counterResult.Damage;
            counterResult.Damage = originalAttacker.ApplyIncomingAttackDamageReduction(counterResult.Damage);
            originalAttacker.ApplyDamage(counterResult.Damage);

            if (counterResult.Damage < originalDamage)
                logController.AppendBattleLog(
                    logController.BuildGuardReductionLog(originalAttacker, originalDamage, counterResult.Damage));
        }

        logController.AppendBattleLog(
            logController.BuildAttackLog(counterActor, originalAttacker, basicAttack, counterResult, " [반격]"));

        BattleUnitView targetView = viewManager.GetView(originalAttacker);
        if (targetView != null)
            yield return StartCoroutine(targetView.AnimateHPChange(0.15f));

        if (originalAttacker.IsDead)
            logController.AppendBattleLog(logController.BuildDeathLog(originalAttacker));
    }

    private void ApplyBlackArenaDuel(BattleUnit actor, BattleUnit target, SkillDefinition skill)
    {
        if (actor == null || target == null || skill == null)
            return;

        int duration = skill.GetBlackArenaDuelDurationTurns();
        actor.ApplyDuelLock(target, duration);
        target.ApplyDuelLock(actor, duration);

        logController.AppendBattleLog(string.Format(
            "{0}의 {1} → {2}: {3}턴간 결투 격리",
            actor.Name,
            skill.skillName,
            target.Name,
            duration));
    }

    private IEnumerator ApplyAbyssReboundSelfRecoil(BattleUnit actor, SkillDefinition skill, int totalHpDamageDealt)
    {
        if (actor == null || actor.IsDead || skill == null)
            yield break;

        if (totalHpDamageDealt <= 0)
            yield break;

        float recoilPercent = skill.GetAbyssReboundRecoilPercentFromTotalDamage();
        int recoilDamage = Mathf.Max(1, Mathf.FloorToInt(totalHpDamageDealt * (recoilPercent * 0.01f)));
        int actualDamage = actor.ApplyDamage(recoilDamage);

        if (actualDamage <= 0)
            yield break;

        logController.AppendBattleLog(string.Format(
            "{0}의 {1} 반동 → {2} 피해",
            actor.Name,
            skill.skillName,
            actualDamage));

        BattleUnitView actorView = viewManager.GetView(actor);
        if (actorView != null)
            yield return StartCoroutine(actorView.AnimateHPChange(0.15f));

        if (actor.IsDead)
            logController.AppendBattleLog(logController.BuildDeathLog(actor));
    }

    private BattleUnit GetBackUnit(BattleUnit primaryTarget)
    {
        if (primaryTarget == null)
            return null;

        BattleFormation formation = primaryTarget.Team == TeamType.Ally
            ? battleManager.AllyFormation
            : battleManager.EnemyFormation;

        if (formation == null)
            return null;

        BattleUnit back = formation.GetUnit(primaryTarget.SlotIndex + 1);
        if (back == null || back.IsDead)
            return null;

        return back;
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

    private void ApplyBleedDrainStrikeEffects(BattleUnit actor, BattleUnit target, SkillDefinition skill, int hpDamageDealt)
    {
        if (actor == null || target == null || skill == null || skill.effects == null)
            return;

        for (int i = 0; i < skill.effects.Count; i++)
        {
            BattleEffectBlock block = skill.effects[i];
            if (block == null)
                continue;

            if (block.kind == BattleEffectKind.ApplyStatus &&
                block.statusType == StatusEffectType.Bleed)
            {
                int finalChance = BattleCalculator.CalculateEffectSuccessChance(block, target);
                bool success = Random.Range(0f, 100f) < finalChance;

                if (!success)
                {
                    logController.AppendBattleLog(
                        logController.BuildEffectFailureLog(actor, target, skill.skillName, GetStatusDisplayName(StatusEffectType.Bleed)));
                    continue;
                }

                target.ApplyStatus(StatusEffectType.Bleed, block.durationTurns);
                logController.AppendBattleLog(
                    logController.BuildEffectSuccessLog(actor, target, skill.skillName, GetStatusDisplayName(StatusEffectType.Bleed)));
                continue;
            }

            if (block.kind == BattleEffectKind.Heal)
            {
                int drainPercent = GetBleedDrainHealPercent(block);
                if (drainPercent <= 0 || hpDamageDealt <= 0)
                    continue;

                int healAmount = Mathf.Max(0, Mathf.FloorToInt(hpDamageDealt * (drainPercent * 0.01f)));
                int healed = actor.Heal(healAmount);

                if (healed > 0)
                    logController.AppendBattleLog(
                        logController.BuildHealLog(actor, actor, skill.skillName + " [흡혈]", healed));
            }
        }
    }

    private int GetBleedDrainHealPercent(BattleEffectBlock block)
    {
        if (block == null)
            return 0;

        if (block.powerPercent > 0f)
            return Mathf.RoundToInt(block.powerPercent);

        if (block.flatValue > 0)
            return block.flatValue;

        return 0;
    }

    private void ApplyBlock(BattleUnit actor, BattleUnit target, string sourceName, BattleEffectBlock block)
    {
        switch (block.kind)
        {
            case BattleEffectKind.Heal:
                {
                    int amount = ResolveEffectAmount(actor, target, block);
                    int healed = target.Heal(amount);
                    logController.AppendBattleLog(logController.BuildHealLog(actor, target, sourceName, healed));
                    break;
                }
            case BattleEffectKind.Shield:
                {
                    int amount = ResolveEffectAmount(actor, target, block);
                    target.AddShield(amount);
                    logController.AppendBattleLog(logController.BuildShieldLog(actor, target, sourceName, amount));
                    break;
                }
            case BattleEffectKind.Buff:
            case BattleEffectKind.Debuff:
                {
                    ApplyTimedModifierBlock(actor, target, sourceName, block);
                    break;
                }
            case BattleEffectKind.ApplyStatus:
                {
                    target.ApplyStatus(block.statusType, block.durationTurns);
                    logController.AppendBattleLog(logController.BuildEffectSuccessLog(actor, target, sourceName, GetStatusDisplayName(block.statusType)));
                    break;
                }
            case BattleEffectKind.RemoveStatus:
                {
                    target.RemoveStatus(block.statusType);
                    logController.AppendBattleLog(logController.BuildEffectSuccessLog(actor, target, sourceName, GetStatusDisplayName(block.statusType) + " 해제"));
                    break;
                }
        }
    }

    private int ResolveEffectAmount(BattleUnit actor, BattleUnit target, BattleEffectBlock block)
    {
        if (block == null)
            return 0;

        if (block.flatValue > 0)
            return Mathf.Max(0, block.flatValue);

        float baseValue = 0f;
        switch (block.valueReference)
        {
            case EffectValueReference.TargetMaxHP:
                baseValue = target != null ? target.MaxHP : 0f;
                break;

            case EffectValueReference.ActorDMG:
            default:
                baseValue = actor != null ? actor.DMG : 0f;
                break;
        }

        return Mathf.Max(0, Mathf.FloorToInt(baseValue * (block.powerPercent * 0.01f)));
    }

    private string GetStatusDisplayName(StatusEffectType statusType)
    {
        switch (statusType)
        {
            case StatusEffectType.Poison: return "중독";
            case StatusEffectType.Bleed: return "출혈";
            case StatusEffectType.Stun: return "기절";
            case StatusEffectType.Taunt: return "도발";
            case StatusEffectType.CounterStance: return "반격 태세";
            case StatusEffectType.DuelArena: return "결투";
            case StatusEffectType.Stealth: return "은신";
            default: return statusType.ToString();
        }
    }

    private void ApplyTimedModifierBlock(BattleUnit actor, BattleUnit target, string sourceName, BattleEffectBlock block)
    {
        if (block == null || target == null)
            return;

        switch (block.statModifierType)
        {
            case StatModifierType.IncomingDamageTakenPercent:
                {
                    int basePercent = Mathf.Abs(block.flatValue);
                    if (basePercent <= 0 || block.durationTurns <= 0)
                        return;

                    int signedPercent = block.kind == BattleEffectKind.Buff ? -basePercent : basePercent;
                    bool applied = target.TryApplyTimedModifier(block.statModifierType, signedPercent, block.durationTurns);

                    if (applied)
                        logController.AppendBattleLog(logController.BuildIncomingDamageModifierLog(actor, target, sourceName, signedPercent, block.durationTurns));
                    else
                        logController.AppendBattleLog(logController.BuildStrongerEffectMaintainedLog(target, "받는 피해 변조"));

                    break;
                }
            case StatModifierType.PierceBackOne:
                {
                    int magnitude = Mathf.Max(1, Mathf.Abs(block.flatValue));
                    if (block.durationTurns <= 0)
                        return;

                    bool applied = target.TryApplyTimedModifier(block.statModifierType, magnitude, block.durationTurns);
                    if (applied)
                        logController.AppendBattleLog(logController.BuildPierceBuffLog(actor, target, sourceName, block.durationTurns));
                    else
                        logController.AppendBattleLog(logController.BuildStrongerEffectMaintainedLog(target, "관통"));

                    break;
                }
            case StatModifierType.DMG:
                {
                    int basePercent = Mathf.Abs(block.flatValue);
                    if (basePercent <= 0 || block.durationTurns <= 0)
                        return;

                    int signedPercent = block.kind == BattleEffectKind.Buff ? basePercent : -basePercent;

                    bool applied = target.TryApplyTimedModifier(
                        block.statModifierType,
                        signedPercent,
                        block.durationTurns);

                    if (applied)
                        logController.AppendBattleLog(
                            logController.BuildEffectSuccessLog(
                                actor,
                                target,
                                sourceName,
                                signedPercent >= 0
                                    ? $"공격력 {signedPercent}% 증가"
                                    : $"공격력 {Mathf.Abs(signedPercent)}% 감소"));
                    else
                        logController.AppendBattleLog(
                            logController.BuildStrongerEffectMaintainedLog(target, "공격력 변조"));

                    break;
                }
            default:
                {
                    logController.AppendBattleLog(logController.BuildEffectSuccessLog(actor, target, sourceName, block.statModifierType.ToString()));
                    break;
                }
        }
    }
}