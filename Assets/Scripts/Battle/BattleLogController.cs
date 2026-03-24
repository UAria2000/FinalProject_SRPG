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
                if (i > 0) sb.AppendLine();
                sb.Append(entries[i].text);
            }
            popupLogText.text = sb.ToString();
        }

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
        string skillName = skill != null ? skill.skillName : "공격";

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

    public string BuildEndTurnGuardLog(BattleUnit actor, int guardPercent)
    {
        return string.Format("{0} 턴 종료 → 다음 자기 턴까지 받는 공격 피해 {1}% 감소", actor.Name, guardPercent);
    }

    public string BuildGuardReductionLog(BattleUnit target, int originalDamage, int reducedDamage)
    {
        return string.Format("{0} 방어 태세: 피해 감소 {1} → {2}", target.Name, originalDamage, reducedDamage);
    }

    public string BuildVictoryLog()
    {
        return "전투 승리";
    }

    public string BuildDefeatLog()
    {
        return "전투 패배";
    }
}
