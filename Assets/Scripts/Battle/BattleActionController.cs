using System.Collections;
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

    public IEnumerator ExecuteBasicAttack(BattleUnit attacker, BattleUnit target, string skillName = null)
    {
        if (attacker == null || target == null || attacker.IsDead || target.IsDead)
            yield break;

        BattleUnitView attackerView = viewManager != null ? viewManager.GetView(attacker) : null;
        BattleUnitView targetView = viewManager != null ? viewManager.GetView(target) : null;

        if (attackerView != null && targetView != null)
        {
            yield return attackerView.PlayAttackMove(
                targetView.transform.position,
                battleManager.AttackMoveRatio,
                battleManager.AttackMoveMaxDistance,
                battleManager.AttackMoveDuration
            );
        }

        AttackResult result = BattleCalculator.RollAttack(attacker, target);

        if (result.Damage > 0)
        {
            target.TakeDamage(result.Damage);
            if (targetView != null)
                yield return targetView.AnimateHPChange(0.25f);
        }

        logController?.AppendBattleLog(logController.BuildAttackLog(attacker, target, skillName, result));
        battleManager.NotifyUnitChanged(attacker);
        battleManager.NotifyUnitChanged(target);

        HandleDeathIfNeeded(target);
    }

    public IEnumerator ExecutePotionUse(BattleUnit user, BattleUnit target, int healAmount)
    {
        if (user == null || target == null || user.IsDead || target.IsDead)
            yield break;

        BattleUnitView targetView = viewManager != null ? viewManager.GetView(target) : null;

        int beforeHP = target.CurrentHP;
        target.Heal(healAmount);
        int healedAmount = target.CurrentHP - beforeHP;

        if (targetView != null)
            yield return targetView.AnimateHPChange(0.25f);

        logController?.AppendBattleLog(logController.BuildItemHealLog(user, target, "포션을 사용하여", healedAmount));
        battleManager.NotifyUnitChanged(user);
        battleManager.NotifyUnitChanged(target);
    }

    public bool TrySwapUnits(BattleUnit a, BattleUnit b, BattleFormation formation)
    {
        if (a == null || b == null || formation == null) return false;
        if (a.IsDead || b.IsDead) return false;
        if (a.Team != b.Team) return false;

        int diff = Mathf.Abs(a.SlotIndex - b.SlotIndex);
        if (diff != 1) return false;

        int direction = b.SlotIndex > a.SlotIndex ? 1 : -1;
        bool moved = formation.TrySwapAdjacent(a.SlotIndex, direction);
        if (moved)
        {
            battleManager.NotifyUnitChanged(a);
            battleManager.NotifyUnitChanged(b);
        }
        return moved;
    }

    public IEnumerator ExecuteSkill(BattleUnit attacker, BattleUnit primaryTarget, SkillDefinition skill)
    {
        if (attacker == null || primaryTarget == null || skill == null)
            yield break;
        if (attacker.IsDead || primaryTarget.IsDead)
            yield break;

        BattleUnitView attackerView = viewManager != null ? viewManager.GetView(attacker) : null;
        BattleUnitView primaryTargetView = viewManager != null ? viewManager.GetView(primaryTarget) : null;

        if (attackerView != null && primaryTargetView != null)
        {
            yield return attackerView.PlayAttackMove(
                primaryTargetView.transform.position,
                battleManager.AttackMoveRatio,
                battleManager.AttackMoveMaxDistance,
                battleManager.AttackMoveDuration
            );
        }

        switch (skill.effectType)
        {
            case SkillEffectType.MultiHitSingleTarget:
                for (int i = 0; i < Mathf.Max(1, skill.hitCount); i++)
                {
                    if (primaryTarget == null || primaryTarget.IsDead)
                        break;
                    yield return ApplySkillStrike(attacker, primaryTarget, skill, skill.primaryDamageMultiplier);
                }
                break;

            case SkillEffectType.FrontAndBackShot:
                BattleUnit secondaryTarget = battleManager.GetBackTarget(primaryTarget);
                if (primaryTarget != null && !primaryTarget.IsDead)
                    yield return ApplySkillStrike(attacker, primaryTarget, skill, skill.primaryDamageMultiplier);
                if (secondaryTarget != null && !secondaryTarget.IsDead)
                    yield return ApplySkillStrike(attacker, secondaryTarget, skill, skill.secondaryDamageMultiplier);
                break;
        }

        attacker.ConsumeSkillCooldown(skill);
        battleManager.NotifyUnitChanged(attacker);
    }

    private IEnumerator ApplySkillStrike(BattleUnit attacker, BattleUnit target, SkillDefinition skill, float damageMultiplier)
    {
        if (attacker == null || target == null || skill == null || attacker.IsDead || target.IsDead)
            yield break;

        AttackResult result = RollSkillAttack(attacker, target, skill.accuracyMultiplier, damageMultiplier);

        if (result.Damage > 0)
        {
            target.TakeDamage(result.Damage);
            BattleUnitView targetView = viewManager != null ? viewManager.GetView(target) : null;
            if (targetView != null)
                yield return targetView.AnimateHPChange(0.2f);
        }

        logController?.AppendBattleLog(logController.BuildAttackLog(attacker, target, skill.skillName, result));
        battleManager.NotifyUnitChanged(attacker);
        battleManager.NotifyUnitChanged(target);
        HandleDeathIfNeeded(target);
    }

    private AttackResult RollSkillAttack(BattleUnit attacker, BattleUnit target, float accuracyMultiplier, float damageMultiplier)
    {
        float totalHitChance = BattleCalculator.CalculateTotalHitChance(attacker.HIT, target.AC);
        totalHitChance = Mathf.Clamp(totalHitChance * accuracyMultiplier, 0f, 100f);

        float failChance = 100f - totalHitChance;
        float missRatio = BattleCalculator.CalculateMissRatio(attacker.HIT, target.AC);
        float grazeChance = failChance * (1f - missRatio);
        float missChance = failChance * missRatio;
        float critChance = totalHitChance * (attacker.CRI / 100f);
        float normalHitChance = totalHitChance - critChance;

        int scaledBaseDamage = Mathf.Max(1, Mathf.RoundToInt(attacker.DMG * damageMultiplier));
        float roll = Random.Range(0f, 100f);

        AttackResultType resultType;
        int damage;

        if (roll < critChance)
        {
            resultType = AttackResultType.Crit;
            damage = BattleCalculator.CalculateCritDamage(scaledBaseDamage, attacker.CRD);
        }
        else if (roll < critChance + normalHitChance)
        {
            resultType = AttackResultType.Hit;
            damage = BattleCalculator.CalculateHitDamage(scaledBaseDamage);
        }
        else if (roll < critChance + normalHitChance + grazeChance)
        {
            resultType = AttackResultType.Graze;
            damage = BattleCalculator.CalculateGrazeDamage(scaledBaseDamage);
        }
        else
        {
            resultType = AttackResultType.Miss;
            damage = 0;
        }

        return new AttackResult
        {
            ResultType = resultType,
            Damage = damage,
            CritChance = critChance,
            HitChance = normalHitChance,
            GrazeChance = grazeChance,
            MissChance = missChance
        };
    }

    private void HandleDeathIfNeeded(BattleUnit target)
    {
        if (target == null || !target.IsDead)
            return;

        logController?.AppendBattleLog(logController.BuildDeathLog(target));
        viewManager?.RemoveView(target);
        battleManager.NotifyUnitDeath(target);
    }
}
