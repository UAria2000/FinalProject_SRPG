using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleActionController : MonoBehaviour
{

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

        if (targets.Count > 0 && skill.ShouldAlsoApplyToSelfWhenTargetingAlly() && actor != null && !actor.IsDead && !targets.Contains(actor))
            targets.Add(actor);

        if (targets.Count <= 0)
        {
            battleManager.OnActionExecutionFinished(false);
            yield break;
        }

        battleManager.SetTurnState(TurnState.ExecutingAction);

        // --- [이펙트 추가] 시전자 위치에서 시전 이펙트 재생 ---
        BattleUnitView actorView = viewManager.GetView(actor);
        if (actorView != null && skill.castEffectPrefab != null)
        {
            viewManager.PlayEffect(skill.castEffectPrefab, actorView.transform.position);
        }

        int rolledPrimaryDamagePercent = BattleCalculator.RollSkillDamagePowerPercent(skill);
        Sprite attackSprite = actor != null && actor.ViewDefinition != null ? actor.ViewDefinition.GetAttackBattleSprite() : null;

        if (skill.resolutionMode == SkillResolutionMode.Attack && skill.HasDamageEffect())
        {
            if (skill.IsEnemyTargetAttackSkill() && clickedTarget != null)
            {
                BattleUnitView targetView = viewManager.GetView(clickedTarget);
                if (actorView != null && targetView != null)
                {
                    yield return StartCoroutine(actorView.PlayAttackMoveWithImpact(
                        targetView.AnchoredPosition,
                        battleManager.AttackMoveRatio,
                        battleManager.AttackMoveMaxDistance,
                        battleManager.AttackMoveDuration,
                        attackSprite,
                        () => ResolveAttackSkillImpacts(actor, skill, targets, rolledPrimaryDamagePercent)));
                }
                else
                {
                    yield return StartCoroutine(ResolveAttackSkillImpacts(actor, skill, targets, rolledPrimaryDamagePercent));
                }
            }
            else
            {
                if (actorView != null && attackSprite != null)
                    actorView.SetBodySpriteOverride(attackSprite);

                yield return StartCoroutine(ResolveAttackSkillImpacts(actor, skill, targets, rolledPrimaryDamagePercent));

                if (actorView != null && attackSprite != null)
                    actorView.ClearBodySpriteOverride();
            }
        }
        else
        {
            if (actorView != null && attackSprite != null)
                actorView.SetBodySpriteOverride(attackSprite);

            for (int i = 0; i < targets.Count; i++)
            {
                BattleUnit primaryTarget = targets[i];

                // --- [이펙트 추가] 대상 위치에서 타격 이펙트 재생 (성공판정형) ---
                BattleUnitView targetView = viewManager.GetView(primaryTarget);
                if (targetView != null && skill.hitEffectPrefab != null)
                {
                    viewManager.PlayEffect(skill.hitEffectPrefab, targetView.transform.position);
                }

                PlaySkillHitSfx(skill);
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
                        PlaySkillHitSfx(skill);
                        ApplySuccessOnlyEffects(actor, secondaryTarget, skill.skillName + " [후열]", skill.effects);

                        BattleUnitView secondaryView = viewManager.GetView(secondaryTarget);
                        if (secondaryView != null)
                            yield return StartCoroutine(secondaryView.AnimateHPChange(0.1f));
                    }
                }
            }
            if (actorView != null && attackSprite != null)
                actorView.ClearBodySpriteOverride();
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
        List<InventoryStackData> allyInventory = battleManager.GetActiveAllyInventory();
        if (allyInventory == null || inventoryIndex < 0 || inventoryIndex >= allyInventory.Count)
        {
            battleManager.OnActionExecutionFinished(false);
            yield break;
        }

        InventoryStackData stack = allyInventory[inventoryIndex];
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
                allyInventory.RemoveAt(inventoryIndex);
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

        if (!battleManager.TrySpendManaForAction(BattleManaActionType.Capture))
        {
            battleManager.RefundCaptureAttempt(target);
            battleManager.OnActionExecutionFinished(false);
            yield break;
        }

        int chancePercent = battleManager.GetCaptureChancePercent(target);
        bool success = Random.Range(0f, 100f) < chancePercent;

        if (!success)
        {
            logController.AppendBattleLog(logController.BuildCaptureFailureLog(actor, target, chancePercent));
            battleManager.OnActionExecutionFinished(false);
            yield break;
        }

        ItemDefinition capturedItem;
        if (!battleManager.TryAddCapturedRewardToInventory(target, out capturedItem))
        {
            battleManager.OnActionExecutionFinished(false);
            yield break;
        }

        logController.AppendBattleLog(logController.BuildCaptureSuccessLog(actor, target, chancePercent));
        if (capturedItem != null)
            logController.AppendBattleLog(logController.BuildCaptureAcquiredLog(capturedItem));

        battleManager.RegisterCapturedEnemy(target);

        BattleFormation enemyFormation = actor.Team == TeamType.Ally
            ? battleManager.EnemyFormation
            : battleManager.AllyFormation;

        if (enemyFormation != null)
            enemyFormation.RemoveUnit(target);

        battleManager.NotifyUnitLeftBattle(target);
        yield return StartCoroutine(battleManager.HandleDeathsAndCompressionRoutine());

        if (battleManager.EnemyFormation == null || !battleManager.EnemyFormation.HasLivingUnits())
            battleManager.SetBattleResult(BattleResultType.Victory);

        battleManager.OnActionExecutionFinished(false);
    }

    public IEnumerator ExecuteFlee(BattleUnit actor)
    {
        if (actor == null)
        {
            battleManager.OnActionExecutionFinished(false);
            yield break;
        }

        battleManager.SetTurnState(TurnState.ExecutingAction);

        if (!battleManager.TrySpendManaForAction(BattleManaActionType.Flee))
        {
            battleManager.OnActionExecutionFinished(false);
            yield break;
        }

        logController.AppendBattleLog(logController.BuildFleeSuccessLog(actor, 100));
        battleManager.SetBattleResult(BattleResultType.Flee);
        battleManager.OnActionExecutionFinished(false);
    }

    public IEnumerator ExecuteManaPreventDeath(BattleUnit actor, BattleUnit target)
    {
        if (actor == null || target == null)
        {
            battleManager.OnActionExecutionFinished(false);
            yield break;
        }

        battleManager.SetTurnState(TurnState.ExecutingAction);

        if (!battleManager.TrySpendManaForAction(BattleManaActionType.PreventDeath))
        {
            battleManager.OnActionExecutionFinished(false);
            yield break;
        }

        target.ApplyManaPreventDeathGuard();
        logController.AppendBattleLog($"{actor.Name}의 마나 행동 → {target.Name}: 생존 부여");
        yield return null;
        battleManager.OnActionExecutionFinished(false);
    }

    public IEnumerator ExecuteManaTeamBuff(BattleUnit actor)
    {
        if (actor == null)
        {
            battleManager.OnActionExecutionFinished(false);
            yield break;
        }

        battleManager.SetTurnState(TurnState.ExecutingAction);

        if (!battleManager.TrySpendManaForAction(BattleManaActionType.TeamBuff))
        {
            battleManager.OnActionExecutionFinished(false);
            yield break;
        }

        int percent = Mathf.Max(0, battleManager.TeamBuffAllStatsPercent);
        int duration = Mathf.Max(1, battleManager.TeamBuffDurationTurns);
        List<BattleUnit> allies = battleManager.AllyFormation != null ? battleManager.AllyFormation.GetAliveUnits() : new List<BattleUnit>();
        for (int i = 0; i < allies.Count; i++)
        {
            BattleUnit target = allies[i];
            if (target == null)
                continue;

            target.TryApplyTimedModifier(StatModifierType.DMG, percent, duration);
            target.TryApplyTimedModifier(StatModifierType.SPD, percent, duration);
            target.TryApplyTimedModifier(StatModifierType.HIT, percent, duration);
            target.TryApplyTimedModifier(StatModifierType.AC, percent, duration);
            target.TryApplyTimedModifier(StatModifierType.IDT, percent, duration);
            target.TryApplyTimedModifier(StatModifierType.CRI, percent, duration);
            target.TryApplyTimedModifier(StatModifierType.CRD, percent, duration);
        }

        logController.AppendBattleLog($"{actor.Name}의 마나 행동: 아군 전체 능력치 {percent}% 증가 ({duration}턴)");
        yield return null;
        battleManager.OnActionExecutionFinished(false);
    }

    public IEnumerator ExecuteEndTurn(BattleUnit actor)
    {
        if (actor == null)
        {
            battleManager.OnActionExecutionFinished(true);
            yield break;
        }

        battleManager.SetTurnState(TurnState.ExecutingAction);
        logController.AppendBattleLog(logController.BuildEndTurnLog(actor));
        yield return null;
        battleManager.OnActionExecutionFinished(true);
    }

    private IEnumerator ResolveAttackSkillImpacts(BattleUnit actor, SkillDefinition skill, List<BattleUnit> targets, int rolledPrimaryDamagePercent)
    {
        int totalHpDamageDealt = 0;
        bool killedWithPrimarySkill = false;

        if (targets == null)
            yield break;

        int primaryHitCount = skill != null ? skill.GetPrimaryHitCount() : 1;

        for (int i = 0; i < targets.Count; i++)
        {
            BattleUnit primaryTarget = targets[i];

            for (int hitIndex = 0; hitIndex < primaryHitCount; hitIndex++)
            {
                if (primaryTarget == null || primaryTarget.IsDead)
                    break;

                BattleUnitView targetView = viewManager.GetView(primaryTarget);
                if (targetView != null && skill.hitEffectPrefab != null)
                    viewManager.PlayEffect(skill.hitEffectPrefab, targetView.transform.position);

                PlaySkillHitSfx(skill);

                string primaryLogSuffix = primaryHitCount > 1
                    ? string.Format(" [{0}타]", hitIndex + 1)
                    : string.Empty;

                yield return StartCoroutine(ResolveAndApplyAttack(
                    actor,
                    skill,
                    primaryTarget,
                    rolledPrimaryDamagePercent,
                    -1f,
                    primaryLogSuffix,
                    true,
                    true));
                totalHpDamageDealt += lastResolvedAttackHpDamageDealt;
                if (primaryTarget != null && primaryTarget.IsDead)
                    killedWithPrimarySkill = true;
            }

            if (primaryTarget == null || primaryTarget.IsDead)
                continue;

            if (skill.HasSecondaryHit())
            {
                BattleUnit secondaryTarget = BattleTargeting.GetSecondaryTarget(
                    actor,
                    skill,
                    primaryTarget,
                    battleManager.AllyFormation,
                    battleManager.EnemyFormation);

                if (secondaryTarget != null && !secondaryTarget.IsDead)
                {
                    PlaySkillHitSfx(skill);
                    yield return StartCoroutine(ResolveAndApplyAttack(
                        actor,
                        skill,
                        secondaryTarget,
                        skill.secondaryDamagePercent,
                        skill.secondaryAccuracyCoefficientPercent,
                        " [추가타격]",
                        skill.secondaryApplyNonDamageEffects,
                        false));
                    totalHpDamageDealt += lastResolvedAttackHpDamageDealt;
                    if (secondaryTarget != null && secondaryTarget.IsDead)
                        killedWithPrimarySkill = true;
                }
                else if (skill.HasMissingSecondaryTargetDamageBonus())
                {
                    float bonusPower = Mathf.Max(rolledPrimaryDamagePercent, skill.GetMissingSecondaryTargetDamagePowerPercent());
                    PlaySkillHitSfx(skill);
                    yield return StartCoroutine(ResolveAndApplyAttack(
                        actor,
                        skill,
                        primaryTarget,
                        bonusPower,
                        -1f,
                        " [단독 관통 보정]",
                        false,
                        false));
                    totalHpDamageDealt += lastResolvedAttackHpDamageDealt;
                    if (primaryTarget != null && primaryTarget.IsDead)
                        killedWithPrimarySkill = true;
                }
            }

            if (actor != null && actor.HasPierceBackOneBuff)
            {
                BattleUnit pierceTarget = GetBackUnit(primaryTarget);
                if (pierceTarget != null && !pierceTarget.IsDead)
                {
                    PlaySkillHitSfx(skill);
                    yield return StartCoroutine(ResolveAndApplyAttack(
                        actor,
                        skill,
                        pierceTarget,
                        rolledPrimaryDamagePercent,
                        -1f,
                        " [관통]",
                        false,
                        false));
                    totalHpDamageDealt += lastResolvedAttackHpDamageDealt;
                }
            }
        }

        if (skill.activeGimmick == ActiveSkillGimmick.ChainLightning)
            yield return StartCoroutine(ApplyChainLightningFollowups(actor, skill, targets));

        if (skill.activeGimmick == ActiveSkillGimmick.ChainExecutionOnce && killedWithPrimarySkill)
            yield return StartCoroutine(ApplyChainExecutionOnce(actor, skill, rolledPrimaryDamagePercent));

        if (skill.activeGimmick == ActiveSkillGimmick.ShieldSelfFromDamageDealt)
            ApplySelfShieldFromDamageDealt(actor, skill, totalHpDamageDealt);

        if (skill.activeGimmick == ActiveSkillGimmick.AbyssReboundSelfRecoil20FromTotalDamage)
            yield return StartCoroutine(ApplyAbyssReboundSelfRecoil(actor, skill, totalHpDamageDealt));
    }

    private IEnumerator ResolveAndApplyAttack(
        BattleUnit actor,
        SkillDefinition skill,
        BattleUnit target,
        float damagePowerPercentOverride,
        float accuracyPercentOverride,
        string logSuffix,
        bool applyNonDamageEffects,
        bool allowPrimaryHitGimmicks)
    {
        lastResolvedAttackHpDamageDealt = 0;

        if (actor == null || target == null || skill == null)
            yield break;

        float resolvedDamagePowerPercent = GetResolvedDamagePowerPercentForThisAttack(
            actor,
            target,
            skill,
            damagePowerPercentOverride,
            allowPrimaryHitGimmicks);

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

            ApplyLifeStealFromDirectAttack(actor, skill, hpDamageDealt);

            if (applyNonDamageEffects)
            {
                if (skill.activeGimmick == ActiveSkillGimmick.BleedDrainStrike)
                    ApplyBleedDrainStrikeEffects(actor, target, skill, hpDamageDealt);
                else
                    ApplyNonDamageEffects(actor, target, skill.skillName, skill.effects, true);
            }

            if (allowPrimaryHitGimmicks &&
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

        ShowAttackFloatingFeedback(target, skill, result);

        BattleUnitView view = viewManager.GetView(target);
        if (view != null)
        {
            if (result.DidHit)
                view.PlayHitFlash(Mathf.Max(0.05f, battleManager.AttackMoveDuration * 0.5f));
            yield return StartCoroutine(view.AnimateHPChange(0.15f));
        }

        if (target.IsDead)
        {
            logController.AppendBattleLog(logController.BuildDeathLog(target));
            yield break;
        }

        if (result.DidHit &&
            allowPrimaryHitGimmicks &&
            target.HasStatus(StatusEffectType.CounterStance) &&
            actor != null &&
            !actor.IsDead)
        {
            yield return StartCoroutine(ExecuteReactiveCounterAttack(target, actor));
        }

        if (result.DidHit && allowPrimaryHitGimmicks)
        {
            yield return StartCoroutine(HandleForcedTargetMoveAfterHit(actor, skill, target));
            yield return StartCoroutine(HandleRandomRepositionAfterHit(actor, skill, target));
        }
    }

    private float GetResolvedDamagePowerPercentForThisAttack(
        BattleUnit actor,
        BattleUnit target,
        SkillDefinition skill,
        float requestedDamagePowerPercent,
        bool allowPrimaryHitGimmicks)
    {
        float resolvedDamagePowerPercent = requestedDamagePowerPercent;

        if (actor == null || target == null || skill == null)
            return resolvedDamagePowerPercent;

        if (skill.HasMissingHpPowerBonus())
            resolvedDamagePowerPercent += skill.GetMissingHpBonusPowerPercent(actor);

        if (skill.HasShieldedTargetDamageBonus() && target.CurrentShield > 0)
            resolvedDamagePowerPercent = Mathf.Max(resolvedDamagePowerPercent, skill.GetShieldedTargetDamagePowerPercent());

        if (skill.HasTargetStatusDamageBonus() && target.HasStatus(skill.targetStatusBonusType))
            resolvedDamagePowerPercent += skill.GetTargetStatusBonusPowerAddPercent();

        if (!allowPrimaryHitGimmicks)
            return resolvedDamagePowerPercent;

        if (!skill.HasForcedTargetPushBackAfterHit())
            return resolvedDamagePowerPercent;

        if (target.IsForcedPositionMoveImmune)
        {
            float immuneFailPowerPercent = skill.GetPushBackFailFinalPowerPercent();
            if (immuneFailPowerPercent > resolvedDamagePowerPercent)
                resolvedDamagePowerPercent = immuneFailPowerPercent;
            return resolvedDamagePowerPercent;
        }

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


    private IEnumerator HandleRandomRepositionAfterHit(BattleUnit actor, SkillDefinition skill, BattleUnit target)
    {
        if (actor == null || skill == null || target == null)
            yield break;

        if (skill.activeGimmick != ActiveSkillGimmick.RandomRepositionTargetsOnHit)
            yield break;

        if (target.IsDead || !battleManager.IsUnitInBattle(target) || target.IsPositionMovementLocked || target.IsForcedPositionMoveImmune)
            yield break;

        float chance = skill.GetRandomRepositionChancePercent();
        if (chance <= 0f || Random.Range(0f, 100f) >= chance)
            yield break;

        BattleFormation formation = target.Team == TeamType.Ally
            ? battleManager.AllyFormation
            : battleManager.EnemyFormation;

        if (formation == null || !formation.Contains(target))
            yield break;

        int fromSlot = target.SlotIndex;
        int toSlot = Random.Range(0, 4);
        if (toSlot == fromSlot)
            yield break;

        bool moved = formation.MoveUnitTo(target, toSlot);
        if (!moved)
            yield break;

        logController.AppendBattleLog(
            logController.BuildForcedTargetMoveLog(actor, skill, target, fromSlot, target.SlotIndex));

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

        if (!skill.HasForcedTargetMoveAfterHit() &&
            !skill.HasForcedTargetPushBackAfterHit() &&
            skill.activeGimmick != ActiveSkillGimmick.PullTargetForwardAfterHit)
            yield break;

        if (target.IsDead || !battleManager.IsUnitInBattle(target) || target.IsForcedPositionMoveImmune)
            yield break;

        BattleFormation targetFormation = target.Team == TeamType.Ally
            ? battleManager.AllyFormation
            : battleManager.EnemyFormation;

        if (targetFormation == null || !targetFormation.Contains(target))
            yield break;

        float chance = skill.GetForcedTargetMoveChancePercent();
        if (chance <= 0f || Random.Range(0f, 100f) >= chance)
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
        else if (skill.activeGimmick == ActiveSkillGimmick.PullTargetForwardAfterHit)
        {
            int steps = skill.GetForcedTargetMoveSteps();
            moved = targetFormation.MoveUnitByDelta(target, -steps);
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


    private void ApplyLifeStealFromDirectAttack(BattleUnit actor, SkillDefinition skill, int hpDamageDealt)
    {
        if (actor == null || actor.IsDead || hpDamageDealt <= 0)
            return;

        if (actor.LifeStealStackCount <= 0)
            return;

        int healPercent = BattleStatusUtility.LifeStealHealPercent;
        int healAmount = Mathf.Max(1, Mathf.FloorToInt(hpDamageDealt * (healPercent * 0.01f)));
        int healed = actor.Heal(healAmount);
        if (healed > 0 && logController != null)
            logController.AppendBattleLog(logController.BuildHealLog(actor, actor, skill != null ? skill.skillName + " [흡혈]" : "흡혈", healed));
    }

    private void ApplySelfShieldFromDamageDealt(BattleUnit actor, SkillDefinition skill, int totalHpDamageDealt)
    {
        if (actor == null || actor.IsDead || skill == null || totalHpDamageDealt <= 0)
            return;

        float percent = skill.GetSelfShieldFromDamageDealtPercent();
        if (percent <= 0f)
            return;

        int shield = Mathf.Max(1, Mathf.FloorToInt(totalHpDamageDealt * (percent * 0.01f)));
        int gained = actor.AddShield(shield);
        if (gained > 0 && logController != null)
            logController.AppendBattleLog(logController.BuildShieldLog(actor, actor, skill.skillName, gained));
    }

    private IEnumerator ApplyChainLightningFollowups(BattleUnit actor, SkillDefinition skill, List<BattleUnit> alreadyHit)
    {
        if (actor == null || actor.IsDead || skill == null)
            yield break;

        float[] powers = new float[]
        {
            skill.GetChainLightningFirstJumpPowerPercent(),
            skill.GetChainLightningSecondJumpPowerPercent()
        };

        for (int i = 0; i < powers.Length; i++)
        {
            BattleUnit target = PickRandomAliveEnemy(actor, allowAlreadyHit: true);
            if (target == null)
                yield break;

            PlaySkillHitSfx(skill);
            yield return StartCoroutine(ResolveAndApplyAttack(
                actor,
                skill,
                target,
                powers[i],
                -1f,
                string.Format(" [연쇄 {0}]", i + 1),
                false,
                false));
        }
    }

    private IEnumerator ApplyChainExecutionOnce(BattleUnit actor, SkillDefinition skill, int rolledPrimaryDamagePercent)
    {
        if (actor == null || actor.IsDead || skill == null)
            yield break;

        BattleUnit target = PickRandomAliveEnemy(actor, allowAlreadyHit: true);
        if (target == null)
            yield break;

        int hitCount = Mathf.Max(1, skill.GetPrimaryHitCount());
        for (int i = 0; i < hitCount; i++)
        {
            if (target == null || target.IsDead)
                yield break;

            PlaySkillHitSfx(skill);
            yield return StartCoroutine(ResolveAndApplyAttack(
                actor,
                skill,
                target,
                rolledPrimaryDamagePercent,
                -1f,
                string.Format(" [연쇄 처형 {0}타]", i + 1),
                false,
                false));
        }
    }

    private BattleUnit PickRandomAliveEnemy(BattleUnit actor, bool allowAlreadyHit)
    {
        if (actor == null || battleManager == null)
            return null;

        BattleFormation enemyFormation = actor.Team == TeamType.Ally
            ? battleManager.EnemyFormation
            : battleManager.AllyFormation;

        if (enemyFormation == null)
            return null;

        List<BattleUnit> candidates = enemyFormation.GetAliveUnits();
        if (candidates == null || candidates.Count == 0)
            return null;

        return candidates[Random.Range(0, candidates.Count)];
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

    private bool TryPassEffectRoll(BattleUnit actor, BattleUnit target, string sourceName, BattleEffectBlock block)
    {
        bool basePassed;
        bool resistancePassed;
        int finalChance;
        bool success = BattleCalculator.RollEffectSuccess(block, target, out basePassed, out resistancePassed, out finalChance);
        if (success)
            return true;

        string effectName = GetEffectDisplayName(block);
        if (basePassed && !resistancePassed)
            effectName += " 저항";

        logController.AppendBattleLog(logController.BuildEffectFailureLog(actor, target, sourceName, effectName));
        return false;
    }

    private void PlaySkillHitSfx(SkillDefinition skill)
    {
        if (skill == null || skill.hitSfx == null)
            return;

        GameAudioManager.PlaySfx(skill.hitSfx);
    }

    private void ShowAttackFloatingFeedback(BattleUnit target, SkillDefinition skill, AttackResult result)
    {
        if (target == null || viewManager == null)
            return;

        string skillName = skill != null ? skill.skillName : string.Empty;
        if (result.DidHit)
        {
            string amountText = Mathf.Max(0, result.Damage).ToString();
            if (result.ResultType == AttackResultType.Crit)
                amountText = "<b>" + amountText + "</b>";

            ShowFloatingFeedback(target, string.Format("{0}\n{1}", skillName, amountText), new Color(1f, 0.2f, 0.2f, 1f));
        }
        else
        {
            ShowFloatingFeedback(target, string.Format("{0}\n회피", skillName), new Color(0.7f, 0.7f, 0.7f, 1f));
        }
    }

    private void ShowFloatingFeedback(BattleUnit target, string text, Color color)
    {
        if (viewManager != null)
            viewManager.ShowFloatingText(target, text, color, 1f);
    }

    private string GetEffectDisplayName(BattleEffectBlock block)
    {
        if (block == null)
            return "효과";

        if (block.kind == BattleEffectKind.ApplyStatus || block.kind == BattleEffectKind.RemoveStatus)
            return GetStatusDisplayName(block.statusType);

        return block.kind.ToString();
    }

    private void ApplyItemEffects(BattleUnit actor, BattleUnit target, ItemDefinition item)
    {
        if (item == null || item.effects == null)
            return;

        for (int i = 0; i < item.effects.Count; i++)
        {
            BattleEffectBlock block = item.effects[i];
            if (block == null) continue;
            if (!TryPassEffectRoll(actor, target, item.itemName, block))
                continue;

            ApplyBlock(actor, target, item.itemName, block);
        }
    }

    private void ApplySuccessOnlyEffects(BattleUnit actor, BattleUnit target, string sourceName, List<BattleEffectBlock> effects)
    {
        for (int i = 0; i < effects.Count; i++)
        {
            BattleEffectBlock block = effects[i];
            if (block == null) continue;
            if (!TryPassEffectRoll(actor, target, sourceName, block))
                continue;

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
            if (!TryPassEffectRoll(actor, target, sourceName, block))
                continue;

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
                if (!TryPassEffectRoll(actor, target, skill.skillName, block))
                    continue;

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
                continue;
            }

            if (block.kind == BattleEffectKind.Buff || block.kind == BattleEffectKind.Debuff)
            {
                ApplyBlock(actor, actor, skill.skillName, block);
                continue;
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
                    if (healed > 0)
                        ShowFloatingFeedback(target, string.Format("{0}\n{1}", sourceName, healed), new Color(0.25f, 1f, 0.35f, 1f));
                    break;
                }
            case BattleEffectKind.Shield:
                {
                    int amount = ResolveEffectAmount(actor, target, block);
                    target.AddShield(amount);
                    logController.AppendBattleLog(logController.BuildShieldLog(actor, target, sourceName, amount));
                    if (amount > 0)
                        ShowFloatingFeedback(target, string.Format("{0}\n{1}", sourceName, amount), new Color(0.25f, 1f, 0.35f, 1f));
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
        return BattleStatusUtility.GetDisplayName(statusType);
    }

    private string GetStatModifierDisplayName(StatModifierType statType)
    {
        switch (statType)
        {
            case StatModifierType.DMG: return "DMG";
            case StatModifierType.SPD: return "SPD";
            case StatModifierType.HIT: return "HIT";
            case StatModifierType.AC: return "AC";
            case StatModifierType.IDT: return "IDT";
            case StatModifierType.CRI: return "CRI";
            case StatModifierType.CRD: return "CRD";
            case StatModifierType.IncomingDamageTakenPercent: return "받는 피해";
            case StatModifierType.PierceBackOne: return "관통";
            default: return statType.ToString();
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
                    int basePercent = Mathf.Abs(block.flatValue);
                    if (basePercent <= 0 || block.durationTurns <= 0)
                        return;

                    int signedPercent = block.kind == BattleEffectKind.Buff ? basePercent : -basePercent;
                    bool applied = target.TryApplyTimedModifier(block.statModifierType, signedPercent, block.durationTurns);
                    if (applied)
                    {
                        string label = GetStatModifierDisplayName(block.statModifierType);
                        string text = signedPercent >= 0
                            ? $"{label} {signedPercent}% 증가"
                            : $"{label} {Mathf.Abs(signedPercent)}% 감소";
                        logController.AppendBattleLog(logController.BuildEffectSuccessLog(actor, target, sourceName, text));
                    }
                    break;
                }
        }
    }
}