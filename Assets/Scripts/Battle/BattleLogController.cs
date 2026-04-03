using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleLogController : MonoBehaviour
{
    [SerializeField] private TMP_Text recentLogText;
    [SerializeField] private TMP_Text popupLogText;
    [SerializeField] private ScrollRect popupLogScrollRect;

    private readonly List<BattleLogEntry> entries = new List<BattleLogEntry>();

    public void ClearBattleLog()
    {
        entries.Clear();
        Refresh();
    }

    public void AppendBattleLog(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        BattleLogEntry entry = new BattleLogEntry();
        entry.text = text;
        entries.Add(entry);
        Refresh();
    }

    public void Refresh()
    {
        if (recentLogText != null)
            recentLogText.text = entries.Count > 0 ? entries[entries.Count - 1].text : string.Empty;

        if (popupLogText != null)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < entries.Count; i++)
            {
                if (i > 0)
                    sb.AppendLine();

                sb.Append(entries[i].text);
            }

            popupLogText.text = sb.ToString();
        }

        ForceScrollPopupToBottomImmediate();
    }

    public void ScrollPopupToBottom()
    {
        StopAllCoroutines();
        StartCoroutine(CoScrollPopupToBottom());
    }

    private IEnumerator CoScrollPopupToBottom()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();

        if (popupLogText != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(popupLogText.rectTransform);

        if (popupLogScrollRect != null && popupLogScrollRect.content != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(popupLogScrollRect.content);

        Canvas.ForceUpdateCanvases();

        if (popupLogScrollRect != null)
            popupLogScrollRect.verticalNormalizedPosition = 0f;
    }

    private void ForceScrollPopupToBottomImmediate()
    {
        Canvas.ForceUpdateCanvases();

        if (popupLogText != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(popupLogText.rectTransform);

        if (popupLogScrollRect != null && popupLogScrollRect.content != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(popupLogScrollRect.content);

        Canvas.ForceUpdateCanvases();

        if (popupLogScrollRect != null)
            popupLogScrollRect.verticalNormalizedPosition = 0f;
    }

    public string BuildTurnStartLog(int round)
    {
        return string.Format("<color=#FFD966>Turn {0}</color>", round);
    }

    public string BuildAttackLog(BattleUnit attacker, BattleUnit target, SkillDefinition skill, AttackResult result)
    {
        return BuildAttackLog(attacker, target, skill, result, string.Empty);
    }

    public string BuildAttackLog(BattleUnit attacker, BattleUnit target, SkillDefinition skill, AttackResult result, string skillNameSuffix)
    {
        string skillName = skill != null ? skill.skillName : "공격";
        if (!string.IsNullOrEmpty(skillNameSuffix))
            skillName += skillNameSuffix;

        if (result.ResultType == AttackResultType.Miss)
            return string.Format("{0}의 {1} → {2}: 빗나감", attacker.Name, skillName, target.Name);
        if (result.ResultType == AttackResultType.Graze)
            return string.Format("{0}의 {1} → {2}: 스침 {3}", attacker.Name, skillName, target.Name, result.Damage);
        if (result.ResultType == AttackResultType.Crit)
            return string.Format("{0}의 {1} → {2}: 치명 {3}", attacker.Name, skillName, target.Name, result.Damage);

        return string.Format("{0}의 {1} → {2}: {3}", attacker.Name, skillName, target.Name, result.Damage);
    }

    public string BuildEffectSuccessLog(BattleUnit user, BattleUnit target, string actionName, string effectName)
    {
        return string.Format("{0}의 {1} → {2}: {3} 성공", user.Name, actionName, target.Name, effectName);
    }

    public string BuildEffectFailureLog(BattleUnit user, BattleUnit target, string actionName, string effectName)
    {
        return string.Format("{0}의 {1} → {2}: {3} 실패", user.Name, actionName, target.Name, effectName);
    }

    public string BuildMoveLog(BattleUnit actor, BattleUnit target)
    {
        return string.Format("{0}이(가) {1}와 위치를 교체", actor.Name, target.Name);
    }

    public string BuildSelfSlideLog(BattleUnit actor, int fromSlotIndex, int toSlotIndex)
    {
        return string.Format("{0} 위치 이동 {1} → {2}", actor.Name, fromSlotIndex + 1, toSlotIndex + 1);
    }

    public string BuildForcedTargetMoveLog(BattleUnit actor, SkillDefinition skill, BattleUnit target, int fromSlotIndex, int toSlotIndex)
    {
        string skillName = skill != null ? skill.skillName : "스킬";
        return string.Format("{0}의 {1} → {2}: 위치 이동 {3} → {4}",
            actor.Name,
            skillName,
            target.Name,
            fromSlotIndex + 1,
            toSlotIndex + 1);
    }

    public string BuildAutoMoveLog(BattleUnit unit)
    {
        return string.Format("{0}이(가) 전열로 당겨짐", unit.Name);
    }

    public string BuildDeathLog(BattleUnit unit)
    {
        return string.Format("{0} 사망", unit.Name);
    }

    public string BuildHealLog(BattleUnit user, BattleUnit target, string sourceName, int amount)
    {
        return string.Format("{0}의 {1} → {2}: 회복 {3}", user.Name, sourceName, target.Name, amount);
    }

    public string BuildShieldLog(BattleUnit user, BattleUnit target, string sourceName, int amount)
    {
        return string.Format("{0}의 {1} → {2}: 보호막 {3}", user.Name, sourceName, target.Name, amount);
    }

    public string BuildFleeSuccessLog(BattleUnit actor, int chancePercent)
    {
        return string.Format("{0} 도주 성공 ({1}%) → 전투에서 이탈", actor.Name, chancePercent);
    }

    public string BuildFleeFailureLog(BattleUnit actor, int chancePercent)
    {
        return string.Format("{0} 도주 실패 ({1}%)", actor.Name, chancePercent);
    }

    public string BuildCaptureSuccessLog(BattleUnit actor, BattleUnit target, int chancePercent)
    {
        return string.Format("{0}의 포획 → {1}: 성공 ({2}%)", actor.Name, target.Name, chancePercent);
    }

    public string BuildCaptureFailureLog(BattleUnit actor, BattleUnit target, int chancePercent)
    {
        return string.Format("{0}의 포획 → {1}: 실패 ({2}%)", actor.Name, target.Name, chancePercent);
    }

    public string BuildCaptureAcquiredLog(ItemDefinition item)
    {
        return string.Format("포획물 획득: {0}", item != null ? item.itemName : "Unknown");
    }

    public string BuildEndTurnGuardLog(BattleUnit actor, int guardPercent)
    {
        return string.Format("{0} 턴 종료 → 다음 자기 턴까지 받는 공격 피해 {1}% 감소", actor.Name, guardPercent);
    }

    public string BuildGuardReductionLog(BattleUnit target, int originalDamage, int reducedDamage)
    {
        return string.Format("{0} 방어/피해변조: 피해 감소 {1} → {2}", target.Name, originalDamage, reducedDamage);
    }

    public string BuildTurnStartPoisonLog(BattleUnit unit, int damage)
    {
        return string.Format("{0} 중독 피해 {1}", unit.Name, damage);
    }

    public string BuildTurnStartBleedLog(BattleUnit unit, int damage)
    {
        return string.Format("{0} 출혈 피해 {1}", unit.Name, damage);
    }

    public string BuildTurnStartStunLog(BattleUnit unit)
    {
        return string.Format("{0} 기절로 행동 불가", unit.Name);
    }

    public string BuildStatusExpiredLog(BattleUnit unit, StatusEffectType statusType)
    {
        string statusName = GetStatusDisplayName(statusType);
        return string.Format("{0}의 {1} 해제", unit.Name, statusName);
    }

    public string BuildIncomingDamageModifierLog(BattleUnit user, BattleUnit target, string sourceName, int signedPercent, int durationTurns)
    {
        string directionText = signedPercent >= 0 ? "증가" : "감소";
        return string.Format("{0}의 {1} → {2}: 받는 피해 {3}% {4} ({5}턴)",
            user.Name,
            sourceName,
            target.Name,
            Mathf.Abs(signedPercent),
            directionText,
            durationTurns);
    }

    public string BuildPierceBuffLog(BattleUnit user, BattleUnit target, string sourceName, int durationTurns)
    {
        return string.Format("{0}의 {1} → {2}: 관통 부여 ({3}턴)", user.Name, sourceName, target.Name, durationTurns);
    }

    public string BuildStrongerEffectMaintainedLog(BattleUnit target, string effectName)
    {
        return string.Format("{0}: 더 강한 {1} 효과 유지", target.Name, effectName);
    }

    public string BuildVictoryLog()
    {
        return "전투 승리";
    }

    public string BuildDefeatLog()
    {
        return "전투 패배";
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
}