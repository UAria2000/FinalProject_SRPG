using System.Collections.Generic;
using UnityEngine;

public class EnemyAIController : MonoBehaviour
{
    public struct EnemyActionChoice
    {
        public EnemyActionType actionType;
        public SkillDefinition skill;
        public BattleUnit target;
        public float expectedDamage;
    }

    public enum EnemyActionType
    {
        None,
        Skill,
        BasicAttack,
        Move
    }

    private BattleManager battleManager;

    public void Initialize(BattleManager manager)
    {
        battleManager = manager;
    }

    public EnemyActionChoice ChooseBestEnemyAction(BattleUnit attacker)
    {
        EnemyActionChoice best = new EnemyActionChoice
        {
            actionType = EnemyActionType.None,
            skill = null,
            target = null,
            expectedDamage = float.MinValue
        };

        if (attacker == null || attacker.IsDead || battleManager == null)
            return best;

        // 스킬 우선
        for (int i = 0; i < attacker.EquippedSkills.Count; i++)
        {
            SkillDefinition skill = attacker.EquippedSkills[i];
            if (skill == null) continue;
            if (!attacker.CanUseSkill(skill)) continue;

            List<BattleUnit> validTargets = battleManager.GetPrimarySkillTargets(attacker, skill);
            if (validTargets.Count <= 0) continue;

            BattleUnit lowestHpTarget = GetLowestHpTarget(validTargets);
            float expectedDamage = EstimateSkillExpectedDamage(attacker, skill, lowestHpTarget);

            if (expectedDamage > best.expectedDamage)
            {
                best.actionType = EnemyActionType.Skill;
                best.skill = skill;
                best.target = lowestHpTarget;
                best.expectedDamage = expectedDamage;
            }
        }

        if (best.actionType == EnemyActionType.Skill)
            return best;

        // 평타
        BattleFormation targetFormation = attacker.Team == TeamType.Ally
            ? battleManager.EnemyFormation
            : battleManager.AllyFormation;

        List<BattleUnit> basicTargets = BattleTargeting.GetBasicAttackTargets(attacker, targetFormation);
        if (basicTargets.Count > 0)
        {
            best.actionType = EnemyActionType.BasicAttack;
            best.target = GetLowestHpTarget(basicTargets);
            best.expectedDamage = EstimateBasicAttackExpectedDamage(attacker, best.target);
            return best;
        }

        // 이동
        best.actionType = EnemyActionType.Move;
        return best;
    }

    public bool TryAutoMove(BattleUnit unit, BattleFormation formation)
    {
        if (unit == null || unit.IsDead || formation == null)
            return false;

        int direction = GetPreferredMoveDirection(unit);

        if (direction != 0)
        {
            bool moved = formation.TrySwapAdjacent(unit.SlotIndex, direction);
            if (moved) return true;
        }

        if (direction != -1)
        {
            bool movedForward = formation.TrySwapAdjacent(unit.SlotIndex, -1);
            if (movedForward) return true;
        }

        if (direction != 1)
        {
            bool movedBackward = formation.TrySwapAdjacent(unit.SlotIndex, 1);
            if (movedBackward) return true;
        }

        return false;
    }

    private int GetPreferredMoveDirection(BattleUnit unit)
    {
        int rank = unit.SlotIndex + 1;

        switch (unit.RangeType)
        {
            case CharacterRangeType.Melee:
                if (rank >= 3) return -1;
                return 0;

            case CharacterRangeType.Mid:
                if (rank == 4) return -1;
                return 0;

            case CharacterRangeType.Ranged:
                if (rank == 1) return 1;
                return 0;
        }

        return 0;
    }

    private BattleUnit GetLowestHpTarget(List<BattleUnit> targets)
    {
        BattleUnit best = null;
        int lowestHp = int.MaxValue;

        for (int i = 0; i < targets.Count; i++)
        {
            BattleUnit unit = targets[i];
            if (unit == null || unit.IsDead)
                continue;

            if (unit.CurrentHP < lowestHp)
            {
                lowestHp = unit.CurrentHP;
                best = unit;
            }
        }

        return best;
    }

    private float EstimateBasicAttackExpectedDamage(BattleUnit attacker, BattleUnit target)
    {
        if (attacker == null || target == null)
            return 0f;

        return EstimateSingleStrikeExpectedDamage(attacker, target, 1f, 1f);
    }

    private float EstimateSkillExpectedDamage(BattleUnit attacker, SkillDefinition skill, BattleUnit primaryTarget)
    {
        if (attacker == null || skill == null || primaryTarget == null || battleManager == null)
            return 0f;

        switch (skill.effectType)
        {
            case SkillEffectType.MultiHitSingleTarget:
                {
                    float oneHit = EstimateSingleStrikeExpectedDamage(
                        attacker,
                        primaryTarget,
                        skill.accuracyMultiplier,
                        skill.primaryDamageMultiplier
                    );

                    return oneHit * Mathf.Max(1, skill.hitCount);
                }

            case SkillEffectType.FrontAndBackShot:
                {
                    float total = EstimateSingleStrikeExpectedDamage(
                        attacker,
                        primaryTarget,
                        skill.accuracyMultiplier,
                        skill.primaryDamageMultiplier
                    );

                    BattleUnit backTarget = battleManager.GetBackTarget(primaryTarget);
                    if (backTarget != null && !backTarget.IsDead)
                    {
                        total += EstimateSingleStrikeExpectedDamage(
                            attacker,
                            backTarget,
                            skill.accuracyMultiplier,
                            skill.secondaryDamageMultiplier
                        );
                    }

                    return total;
                }
        }

        return 0f;
    }

    private float EstimateSingleStrikeExpectedDamage(BattleUnit attacker, BattleUnit target, float accuracyMultiplier, float damageMultiplier)
    {
        if (attacker == null || target == null)
            return 0f;

        float totalHitChance = BattleCalculator.CalculateTotalHitChance(attacker.HIT, target.AC);
        totalHitChance = Mathf.Clamp(totalHitChance * accuracyMultiplier, 0f, 100f);

        float failChance = 100f - totalHitChance;
        float missRatio = BattleCalculator.CalculateMissRatio(attacker.HIT, target.AC);

        float grazeChance = failChance * (1f - missRatio);
        float critChance = totalHitChance * (attacker.CRI / 100f);
        float normalHitChance = totalHitChance - critChance;

        int scaledBaseDamage = Mathf.Max(1, Mathf.RoundToInt(attacker.DMG * damageMultiplier));

        float expected =
            (critChance / 100f) * BattleCalculator.CalculateCritDamage(scaledBaseDamage, attacker.CRD) +
            (normalHitChance / 100f) * BattleCalculator.CalculateHitDamage(scaledBaseDamage) +
            (grazeChance / 100f) * BattleCalculator.CalculateGrazeDamage(scaledBaseDamage);

        return expected;
    }
}