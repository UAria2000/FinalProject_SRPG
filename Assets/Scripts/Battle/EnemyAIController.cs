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

    private const int PRIORITY_TAUNT_GUARD_SKILL = 5000;
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

    public IEnumerator ExecuteTurn(BattleUnit actor)
    {
        if (battleManager == null || actionController == null || actor == null || actor.IsDead)
            yield break;

        yield return new WaitForSeconds(thinkDelay);

        List<AiCandidate> candidates = BuildCandidates(actor);

        if (candidates.Count <= 0)
        {
            if (debugLog)
                Debug.Log($"[EnemyAI] {actor.Name} has no valid action.");

            battleManager.OnActionExecutionFinished(true);
            yield break;
        }

        AiCandidate best = ChooseBestCandidate(candidates);

        if (debugLog)
            Debug.Log($"[EnemyAI] {actor.Name} -> {best.debugText}");

        if (best.isMove)
            yield return StartCoroutine(actionController.ExecuteMove(actor, best.target));
        else
            yield return StartCoroutine(actionController.ExecuteSkill(actor, best.skill, best.target));
    }

    private List<AiCandidate> BuildCandidates(BattleUnit actor)
    {
        List<AiCandidate> tauntGuardSkills = new List<AiCandidate>();
        List<AiCandidate> attackSkills = new List<AiCandidate>();
        List<AiCandidate> utilitySkills = new List<AiCandidate>();
        List<AiCandidate> basicAttacks = new List<AiCandidate>();
        List<AiCandidate> moveCandidates = new List<AiCandidate>();

        BattleFormation ownFormation = actor.Team == TeamType.Ally
            ? battleManager.AllyFormation
            : battleManager.EnemyFormation;

        BattleFormation opponentFormation = actor.Team == TeamType.Ally
            ? battleManager.EnemyFormation
            : battleManager.AllyFormation;

        for (int slot = 0; slot < actor.GetActionSkillSlotCount(); slot++)
        {
            SkillDefinition skill = actor.GetActionSkillAt(slot);
            if (skill == null)
                continue;

            if (!actor.CanUseSkill(skill))
                continue;

            List<BattleUnit> validTargets = BattleTargeting.GetValidSkillTargets(
                actor,
                skill,
                ownFormation,
                opponentFormation);

            if (validTargets == null || validTargets.Count == 0)
                continue;

            if (IsTauntGuardSelfSkill(skill) && ShouldPreferTauntGuard(actor))
            {
                BattleUnit target = validTargets[0];
                if (target != null)
                {
                    float score = ScoreTauntGuard(actor);
                    tauntGuardSkills.Add(new AiCandidate
                    {
                        priority = PRIORITY_TAUNT_GUARD_SKILL,
                        score = score,
                        skill = skill,
                        target = target,
                        isMove = false,
                        debugText = $"TauntGuard [{skill.skillName}] -> {target.Name} score={score:0.##}"
                    });
                }

                continue;
            }

            if (skill.isBasicAttack)
            {
                BattleUnit target = ChooseLowestHpTarget(validTargets);
                if (target != null)
                {
                    float score = ScoreAttack(actor, target, skill);
                    basicAttacks.Add(new AiCandidate
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
                    float score = ScoreAttack(actor, target, skill);
                    attackSkills.Add(new AiCandidate
                    {
                        priority = PRIORITY_ATTACK_SKILL,
                        score = score,
                        skill = skill,
                        target = target,
                        isMove = false,
                        debugText = $"Attack [{skill.skillName}] -> {target.Name} score={score:0.##}"
                    });
                }
            }
            else
            {
                BattleUnit target = ChooseUtilityTarget(skill, validTargets);
                if (target != null)
                {
                    float score = ScoreUtility(target, skill);
                    utilitySkills.Add(new AiCandidate
                    {
                        priority = PRIORITY_UTILITY_SKILL,
                        score = score,
                        skill = skill,
                        target = target,
                        isMove = false,
                        debugText = $"Utility [{skill.skillName}] -> {target.Name} score={score:0.##}"
                    });
                }
            }
        }

        if (tauntGuardSkills.Count > 0)
            return tauntGuardSkills;

        if (attackSkills.Count > 0)
            return attackSkills;

        if (utilitySkills.Count > 0)
            return utilitySkills;

        if (basicAttacks.Count > 0)
            return basicAttacks;

        List<BattleUnit> movableTargets = BattleTargeting.GetMovableTargets(actor, ownFormation);
        if (movableTargets != null && movableTargets.Count > 0)
        {
            BattleUnit moveTarget = ChooseMoveTarget(actor, movableTargets);
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

    private BattleUnit ChooseUtilityTarget(SkillDefinition skill, List<BattleUnit> validTargets)
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

            if (statType == StatModifierType.IncomingDamageTakenPercent)
                return ChooseLowestHpTarget(validTargets);

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

    private BattleUnit ChooseMoveTarget(BattleUnit actor, List<BattleUnit> movableTargets)
    {
        if (movableTargets == null || movableTargets.Count == 0)
            return null;

        int preferredDirection = GetPreferredMoveDirection(actor);
        int preferredSlot = actor.SlotIndex + preferredDirection;

        if (preferredDirection != 0)
        {
            for (int i = 0; i < movableTargets.Count; i++)
            {
                if (movableTargets[i] != null && movableTargets[i].SlotIndex == preferredSlot)
                    return movableTargets[i];
            }
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

    private float ScoreUtility(BattleUnit target, SkillDefinition skill)
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

            if (statType == StatModifierType.IncomingDamageTakenPercent)
                score += Mathf.Max(0, target.MaxHP - target.CurrentHP);
            else
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
            case StatModifierType.PierceBackOne: return unit.DMG;
            case StatModifierType.IncomingDamageTakenPercent: return Mathf.Max(0, unit.MaxHP - unit.CurrentHP);
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

            if (block.kind != BattleEffectKind.ApplyStatus)
                continue;

            if (block.statusType == StatusEffectType.None)
                continue;

            // Poison / Bleed 는 스택형이므로 이미 걸려 있어도 재적용 가치가 있음
            if (block.statusType == StatusEffectType.Poison ||
                block.statusType == StatusEffectType.Bleed)
                continue;

            if (target.HasStatus(block.statusType))
                return true;
        }

        return false;
    }

    private bool IsTauntGuardSelfSkill(SkillDefinition skill)
    {
        if (skill == null ||
            skill.castType != SkillCastType.Active ||
            skill.targetTeam != SkillTargetTeam.Self ||
            skill.resolutionMode != SkillResolutionMode.SuccessOnly ||
            skill.effects == null)
            return false;

        bool hasTaunt = false;
        bool hasGuardValue = false;

        for (int i = 0; i < skill.effects.Count; i++)
        {
            BattleEffectBlock block = skill.effects[i];
            if (block == null)
                continue;

            if (block.kind == BattleEffectKind.ApplyStatus &&
                block.statusType == StatusEffectType.Taunt &&
                block.durationTurns > 0)
            {
                hasTaunt = true;
            }

            // 기존 감피형 도발도 허용
            if (block.kind == BattleEffectKind.Buff &&
                block.statModifierType == StatModifierType.IncomingDamageTakenPercent &&
                block.flatValue > 0 &&
                block.durationTurns > 0)
            {
                hasGuardValue = true;
            }

            // 새 보호막형 도발도 허용
            if (block.kind == BattleEffectKind.Shield &&
                (block.flatValue > 0 || block.powerPercent > 0f))
            {
                hasGuardValue = true;
            }
        }

        return hasTaunt && hasGuardValue;
    }

    private bool ShouldPreferTauntGuard(BattleUnit actor)
    {
        if (actor == null || actor.IsDead)
            return false;

        bool hasTaunt = actor.HasStatus(StatusEffectType.Taunt);
        bool hasDamageReduction =
            actor.GetTimedModifierRemainingTurns(StatModifierType.IncomingDamageTakenPercent) > 0 &&
            actor.GetTimedModifierMagnitude(StatModifierType.IncomingDamageTakenPercent) < 0;

        bool hasShield = actor.CurrentShield > 0;

        // 감피형이든 보호막형이든, 둘 중 하나라도 있으면 방어 준비가 된 것으로 본다
        return !hasTaunt || (!hasDamageReduction && !hasShield);
    }

    private float ScoreTauntGuard(BattleUnit actor)
    {
        if (actor == null)
            return 0f;

        float score = 1000f;
        score += Mathf.Max(0, actor.MaxHP - actor.CurrentHP);
        score += Mathf.Max(0, 3 - actor.SlotIndex) * 25f;
        return score;
    }
}