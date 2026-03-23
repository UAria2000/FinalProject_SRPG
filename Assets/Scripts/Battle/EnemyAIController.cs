using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAIController : MonoBehaviour
{
    private struct AiCandidate
    {
        public int priority;
        public float score;
        public SkillDefinition skill;
        public BattleUnit target;
        public bool isMove;
        public string debugText;
    }

    private const int PRIORITY_ATTACK_SKILL = 4000;
    private const int PRIORITY_UTILITY_SKILL = 3000;
    private const int PRIORITY_BASIC_ATTACK = 2000;
    private const int PRIORITY_MOVE = 1000;

    [SerializeField] private float thinkDelay = 0.35f;
    [SerializeField] private bool debugLog = false;

    private BattleManager battleManager;
    private BattleActionController actionController;

    public void Initialize(BattleManager manager)
    {
        battleManager = manager;
        actionController = manager != null ? manager.ActionController : null;
    }

    public IEnumerator ExecuteTurn(BattleUnit attacker)
    {
        if (battleManager == null || actionController == null || attacker == null || attacker.IsDead)
            yield break;

        yield return new WaitForSeconds(thinkDelay);

        List<AiCandidate> candidates = BuildCandidates(attacker);

        if (candidates.Count <= 0)
        {
            if (debugLog)
                Debug.Log($"[EnemyAI] {attacker.Name} has no valid action.");
            battleManager.OnActionExecutionFinished(true);
            yield break;
        }

        AiCandidate best = ChooseBestCandidate(candidates);

        if (debugLog)
            Debug.Log($"[EnemyAI] {attacker.Name} -> {best.debugText}");

        if (best.isMove)
        {
            yield return StartCoroutine(actionController.ExecuteMove(attacker, best.target));
        }
        else
        {
            yield return StartCoroutine(actionController.ExecuteSkill(attacker, best.skill, best.target));
        }
    }

    private List<AiCandidate> BuildCandidates(BattleUnit attacker)
    {
        List<AiCandidate> attackSkillCandidates = new List<AiCandidate>();
        List<AiCandidate> utilitySkillCandidates = new List<AiCandidate>();
        List<AiCandidate> basicAttackCandidates = new List<AiCandidate>();
        List<AiCandidate> moveCandidates = new List<AiCandidate>();

        BattleFormation ownFormation = attacker.Team == TeamType.Ally
            ? battleManager.AllyFormation
            : battleManager.EnemyFormation;

        BattleFormation opponentFormation = attacker.Team == TeamType.Ally
            ? battleManager.EnemyFormation
            : battleManager.AllyFormation;

        for (int slot = 0; slot < attacker.GetActionSkillSlotCount(); slot++)
        {
            SkillDefinition skill = attacker.GetActionSkillAt(slot);
            if (skill == null)
                continue;

            if (!attacker.CanUseSkill(skill))
                continue;

            List<BattleUnit> validTargets = BattleTargeting.GetValidSkillTargets(
                attacker,
                skill,
                ownFormation,
                opponentFormation);

            if (validTargets == null || validTargets.Count == 0)
                continue;

            if (skill.isBasicAttack)
            {
                BattleUnit target = ChooseLowestHpTarget(validTargets);
                if (target != null)
                {
                    float score = ScoreAttack(attacker, target, skill);
                    basicAttackCandidates.Add(new AiCandidate
                    {
                        priority = PRIORITY_BASIC_ATTACK,
                        score = score,
                        skill = skill,
                        target = target,
                        isMove = false,
                        debugText = $"Basic [{skill.skillName}] -> {target.Name} score={score:0.##}"
                    });
                }

                continue;
            }

            if (IsAttackSkill(skill))
            {
                BattleUnit target = ChooseLowestHpTarget(validTargets);
                if (target != null)
                {
                    float score = ScoreAttack(attacker, target, skill);
                    attackSkillCandidates.Add(new AiCandidate
                    {
                        priority = PRIORITY_ATTACK_SKILL,
                        score = score,
                        skill = skill,
                        target = target,
                        isMove = false,
                        debugText = $"AttackSkill [{skill.skillName}] -> {target.Name} score={score:0.##}"
                    });
                }
            }
            else
            {
                BattleUnit target = ChooseUtilityTarget(attacker, skill, validTargets);
                if (target != null)
                {
                    float score = ScoreUtility(attacker, target, skill);
                    utilitySkillCandidates.Add(new AiCandidate
                    {
                        priority = PRIORITY_UTILITY_SKILL,
                        score = score,
                        skill = skill,
                        target = target,
                        isMove = false,
                        debugText = $"UtilitySkill [{skill.skillName}] -> {target.Name} score={score:0.##}"
                    });
                }
            }
        }

        if (attackSkillCandidates.Count > 0)
            return attackSkillCandidates;

        if (utilitySkillCandidates.Count > 0)
            return utilitySkillCandidates;

        if (basicAttackCandidates.Count > 0)
            return basicAttackCandidates;

        List<BattleUnit> movableTargets = BattleTargeting.GetMovableTargets(attacker, ownFormation);
        if (movableTargets != null && movableTargets.Count > 0)
        {
            BattleUnit moveTarget = ChooseMoveTarget(attacker, movableTargets);
            if (moveTarget != null)
            {
                moveCandidates.Add(new AiCandidate
                {
                    priority = PRIORITY_MOVE,
                    score = 1f,
                    skill = null,
                    target = moveTarget,
                    isMove = true,
                    debugText = $"Move -> {moveTarget.Name}"
                });
            }
        }

        return moveCandidates;
    }

    private AiCandidate ChooseBestCandidate(List<AiCandidate> candidates)
    {
        int bestPriority = int.MinValue;
        for (int i = 0; i < candidates.Count; i++)
        {
            if (candidates[i].priority > bestPriority)
                bestPriority = candidates[i].priority;
        }

        List<AiCandidate> samePriority = new List<AiCandidate>();
        for (int i = 0; i < candidates.Count; i++)
        {
            if (candidates[i].priority == bestPriority)
                samePriority.Add(candidates[i]);
        }

        float bestScore = float.MinValue;
        for (int i = 0; i < samePriority.Count; i++)
        {
            if (samePriority[i].score > bestScore)
                bestScore = samePriority[i].score;
        }

        List<AiCandidate> bestCandidates = new List<AiCandidate>();
        for (int i = 0; i < samePriority.Count; i++)
        {
            if (Mathf.Abs(samePriority[i].score - bestScore) < 0.01f)
                bestCandidates.Add(samePriority[i]);
        }

        return bestCandidates[Random.Range(0, bestCandidates.Count)];
    }

    private BattleUnit ChooseLowestHpTarget(List<BattleUnit> targets)
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
            else if (unit.CurrentHP == lowestHp && best != null && unit.SlotIndex < best.SlotIndex)
            {
                best = unit;
            }
        }

        return best;
    }

    private BattleUnit ChooseUtilityTarget(BattleUnit attacker, SkillDefinition skill, List<BattleUnit> validTargets)
    {
        bool hasShield = HasEffectKind(skill, BattleEffectKind.Shield);
        bool hasHeal = HasEffectKind(skill, BattleEffectKind.Heal);
        bool hasBuff = HasEffectKind(skill, BattleEffectKind.Buff);
        bool hasDebuff = HasEffectKind(skill, BattleEffectKind.Debuff);
        bool hasStatus = HasEffectKind(skill, BattleEffectKind.ApplyStatus);

        if (hasShield || hasHeal)
            return ChooseLowestHpTarget(validTargets);

        if (hasBuff)
        {
            StatModifierType statType = GetPrimaryStatModifierType(skill);

            BattleUnit best = null;
            float bestValue = float.MinValue;

            for (int i = 0; i < validTargets.Count; i++)
            {
                BattleUnit target = validTargets[i];
                if (target == null || target.IsDead)
                    continue;

                float value = GetStatValue(target, statType);
                if (value > bestValue)
                {
                    bestValue = value;
                    best = target;
                }
            }

            if (best != null)
                return best;
        }

        if (hasDebuff || hasStatus)
        {
            BattleUnit best = null;
            int lowestHp = int.MaxValue;

            for (int i = 0; i < validTargets.Count; i++)
            {
                BattleUnit target = validTargets[i];
                if (target == null || target.IsDead)
                    continue;

                if (hasStatus && HasAnyAppliedStatusAlready(skill, target))
                    continue;

                if (target.CurrentHP < lowestHp)
                {
                    lowestHp = target.CurrentHP;
                    best = target;
                }
                else if (target.CurrentHP == lowestHp && best != null && target.SlotIndex < best.SlotIndex)
                {
                    best = target;
                }
            }

            if (best != null)
                return best;
        }

        return validTargets[0];
    }

    private BattleUnit ChooseMoveTarget(BattleUnit attacker, List<BattleUnit> movableTargets)
    {
        if (movableTargets == null || movableTargets.Count == 0)
            return null;

        int preferredDirection = GetPreferredMoveDirection(attacker);
        int preferredSlot = attacker.SlotIndex + preferredDirection;

        if (preferredDirection != 0)
        {
            for (int i = 0; i < movableTargets.Count; i++)
            {
                if (movableTargets[i] != null && movableTargets[i].SlotIndex == preferredSlot)
                    return movableTargets[i];
            }
        }

        if (attacker.RangeType == CharacterRangeType.Melee)
        {
            BattleUnit best = movableTargets[0];
            for (int i = 1; i < movableTargets.Count; i++)
            {
                if (movableTargets[i].SlotIndex < best.SlotIndex)
                    best = movableTargets[i];
            }
            return best;
        }

        if (attacker.RangeType == CharacterRangeType.Ranged)
        {
            BattleUnit best = movableTargets[0];
            for (int i = 1; i < movableTargets.Count; i++)
            {
                if (movableTargets[i].SlotIndex > best.SlotIndex)
                    best = movableTargets[i];
            }
            return best;
        }

        return movableTargets[0];
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

    private float ScoreAttack(BattleUnit attacker, BattleUnit target, SkillDefinition skill)
    {
        TargetPreviewData preview = BattleCalculator.BuildSkillPreview(attacker, target, skill);

        float avgDamage = (preview.damageMin + preview.damageMax) * 0.5f;
        float hitFactor = preview.showHitChance ? preview.hitChancePercent / 100f : 1f;
        float expectedDamage = avgDamage * hitFactor;

        if (expectedDamage >= target.CurrentHP)
            expectedDamage += 10000f;

        return expectedDamage;
    }

    private float ScoreUtility(BattleUnit attacker, BattleUnit target, SkillDefinition skill)
    {
        float score = 100f;

        if (HasEffectKind(skill, BattleEffectKind.Shield) || HasEffectKind(skill, BattleEffectKind.Heal))
        {
            int missingHp = Mathf.Max(0, target.MaxHP - target.CurrentHP);
            score += missingHp;
        }

        if (HasEffectKind(skill, BattleEffectKind.Buff))
        {
            StatModifierType statType = GetPrimaryStatModifierType(skill);
            score += GetStatValue(target, statType) * 0.5f;
        }

        if (HasEffectKind(skill, BattleEffectKind.Debuff) || HasEffectKind(skill, BattleEffectKind.ApplyStatus))
        {
            score += (1000 - target.CurrentHP) * 0.01f;
            score += GetHighestSuccessChance(skill) * 0.25f;
        }

        return score;
    }

    private bool IsAttackSkill(SkillDefinition skill)
    {
        return skill != null &&
               skill.castType == SkillCastType.Active &&
               skill.resolutionMode == SkillResolutionMode.Attack &&
               skill.HasDamageEffect();
    }

    private bool HasEffectKind(SkillDefinition skill, BattleEffectKind kind)
    {
        if (skill == null || skill.effects == null)
            return false;

        for (int i = 0; i < skill.effects.Count; i++)
        {
            BattleEffectBlock block = skill.effects[i];
            if (block != null && block.kind == kind)
                return true;
        }

        return false;
    }

    private int GetHighestSuccessChance(SkillDefinition skill)
    {
        if (skill == null || skill.effects == null)
            return 0;

        int best = 0;
        for (int i = 0; i < skill.effects.Count; i++)
        {
            BattleEffectBlock block = skill.effects[i];
            if (block == null)
                continue;

            int chance = Mathf.RoundToInt(block.successChancePercent);
            if (chance > best)
                best = chance;
        }

        return best;
    }

    private StatModifierType GetPrimaryStatModifierType(SkillDefinition skill)
    {
        if (skill == null || skill.effects == null)
            return StatModifierType.DMG;

        for (int i = 0; i < skill.effects.Count; i++)
        {
            BattleEffectBlock block = skill.effects[i];
            if (block == null)
                continue;

            if ((block.kind == BattleEffectKind.Buff || block.kind == BattleEffectKind.Debuff) &&
                block.statModifierType != StatModifierType.None)
                return block.statModifierType;
        }

        return StatModifierType.DMG;
    }

    private float GetStatValue(BattleUnit unit, StatModifierType statType)
    {
        if (unit == null)
            return 0f;

        switch (statType)
        {
            case StatModifierType.DMG: return unit.DMG;
            case StatModifierType.SPD: return unit.SPD;
            case StatModifierType.HIT: return unit.HIT;
            case StatModifierType.AC: return unit.AC;
            case StatModifierType.CRI: return unit.CRI;
            case StatModifierType.CRD: return unit.CRD;
            default: return unit.DMG;
        }
    }

    private bool HasAnyAppliedStatusAlready(SkillDefinition skill, BattleUnit target)
    {
        if (skill == null || target == null || skill.effects == null)
            return false;

        for (int i = 0; i < skill.effects.Count; i++)
        {
            BattleEffectBlock block = skill.effects[i];
            if (block == null)
                continue;

            if (block.kind == BattleEffectKind.ApplyStatus &&
                block.statusType != StatusEffectType.None &&
                target.HasStatus(block.statusType))
                return true;
        }

        return false;
    }
}