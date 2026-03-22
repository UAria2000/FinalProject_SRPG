using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleLogController : MonoBehaviour
{
    [Header("Battle Log UI")]
    [SerializeField] private TMP_Text battleLogText;

    [Header("Popup Log UI")]
    [SerializeField] private TMP_Text popupLogText;
    [SerializeField] private ScrollRect popupLogScrollRect;

    [Header("Colors")]
    [SerializeField] private string unitNameColor = "#817F7F";
    [SerializeField] private string defaultTextColor = "#FFFFFF";
    [SerializeField] private string damageColor = "#DA7332";
    [SerializeField] private string healColor = "#0EE01C";
    [SerializeField] private string buffColor = "#4D4D4D";
    [SerializeField] private string turnColor = "#FFD966";

    private string latestBattleLog = "";
    private readonly List<string> fullBattleLogs = new List<string>();

    public string LatestBattleLog => latestBattleLog;
    public IReadOnlyList<string> FullBattleLogs => fullBattleLogs;

    public void ClearBattleLog()
    {
        latestBattleLog = "";
        fullBattleLogs.Clear();
        RefreshBattleLogUI();
        RefreshPopupBattleLogUI();
    }

    public void AppendBattleLog(string message)
    {
        if (string.IsNullOrEmpty(message))
            return;

        latestBattleLog = message;
        fullBattleLogs.Add(message);

        RefreshBattleLogUI();
        RefreshPopupBattleLogUI();
    }

    public void RefreshBattleLogUI()
    {
        if (battleLogText != null)
            battleLogText.text = latestBattleLog;
    }

    public void RefreshPopupBattleLogUI()
    {
        if (popupLogText != null)
            popupLogText.text = string.Join("\n", fullBattleLogs);

        Canvas.ForceUpdateCanvases();

        if (popupLogScrollRect != null)
            popupLogScrollRect.verticalNormalizedPosition = 0f;
    }

    public string FormatTurnLog(int round)
    {
        return $"<color={turnColor}>Turn{round}</color>";
    }

    public string FormatUnitName(string unitName)
    {
        return $"<color={unitNameColor}>{unitName}</color>";
    }

    public string FormatDefaultText(string text)
    {
        return $"<color={defaultTextColor}>{text}</color>";
    }

    public string FormatDamageValueOnlyNumber(int value)
    {
        return $"<color={damageColor}>{value}</color>";
    }

    public string FormatHealValueOnlyNumber(int value)
    {
        return $"<color={healColor}>{value}</color>";
    }

    public string FormatBuffValueOnlyNumber(int value)
    {
        return $"<color={buffColor}>{value}</color>";
    }

    public string FormatDamageKeyword()
    {
        return $"<color={damageColor}>데미지</color>";
    }

    public string FormatHealKeyword()
    {
        return $"<color={healColor}>회복</color>";
    }

    public string FormatShieldKeyword()
    {
        return $"<color={buffColor}>보호막</color>";
    }

    public string FormatBuffKeyword(string buffName)
    {
        return $"<color={buffColor}>{buffName}</color>";
    }

    public string BuildAttackLog(BattleUnit attacker, BattleUnit target, string skillName, AttackResult result)
    {
        string attackerName = FormatUnitName(attacker.Name);
        string targetName = FormatUnitName(target.Name);

        string actionText = string.IsNullOrEmpty(skillName)
            ? ""
            : $"{FormatDefaultText(skillName)} ";

        switch (result.ResultType)
        {
            case AttackResultType.Crit:
                return $"{attackerName}이 {targetName}에게 {actionText}{FormatDefaultText("치명타로")} {FormatDamageValueOnlyNumber(result.Damage)} {FormatDamageKeyword()}를 {FormatDefaultText("입혔습니다")}";

            case AttackResultType.Hit:
                return $"{attackerName}이 {targetName}에게 {actionText}{FormatDamageValueOnlyNumber(result.Damage)} {FormatDamageKeyword()}를 {FormatDefaultText("입혔습니다")}";

            case AttackResultType.Graze:
                return $"{attackerName}이 {targetName}에게 {actionText}{FormatDefaultText("스침으로")} {FormatDamageValueOnlyNumber(result.Damage)} {FormatDamageKeyword()}를 {FormatDefaultText("입혔습니다")}";

            case AttackResultType.Miss:
                return $"{attackerName}이 {targetName}에게 {actionText}{FormatDefaultText("공격했지만 빗나갔습니다")}";
        }

        return $"{attackerName}이 {targetName}에게 {FormatDefaultText("공격했습니다")}";
    }

    public string BuildItemHealLog(BattleUnit user, BattleUnit target, string actionText, int value)
    {
        return $"{FormatUnitName(user.Name)}이 {FormatUnitName(target.Name)}에게 {FormatDefaultText(actionText)} {FormatHealValueOnlyNumber(value)} {FormatHealKeyword()}을 {FormatDefaultText("회복시켰습니다")}";
    }

    public string BuildBuffLog(BattleUnit user, BattleUnit target, string actionText, int value, string buffText)
    {
        return $"{FormatUnitName(user.Name)}이 {FormatUnitName(target.Name)}에게 {FormatDefaultText(actionText)} {FormatBuffValueOnlyNumber(value)} {FormatBuffKeyword(buffText)}을 {FormatDefaultText("부여했습니다")}";
    }

    public string BuildShieldLog(BattleUnit user, BattleUnit target, string actionText, int value)
    {
        return $"{FormatUnitName(user.Name)}이 {FormatUnitName(target.Name)}에게 {FormatDefaultText(actionText)} {FormatBuffValueOnlyNumber(value)} {FormatShieldKeyword()}을 {FormatDefaultText("부여했습니다")}";
    }

    public string BuildMoveLog(BattleUnit user, BattleUnit target)
    {
        return $"{FormatUnitName(user.Name)}이 {FormatUnitName(target.Name)}과 {FormatDefaultText("위치를 교체했습니다")}";
    }

    public string BuildAutoMoveLog(BattleUnit user)
    {
        return $"{FormatUnitName(user.Name)}이 {FormatDefaultText("위치를 이동했습니다")}";
    }

    public string BuildDeathLog(BattleUnit target)
    {
        return $"{FormatUnitName(target.Name)}이 {FormatDefaultText("사망했습니다")}";
    }

    public string BuildBattleStartLog()
    {
        return FormatDefaultText("전투가 시작되었습니다");
    }

    public string BuildVictoryLog()
    {
        return FormatDefaultText("전투에서 승리했습니다");
    }

    public string BuildDefeatLog()
    {
        return FormatDefaultText("전투에서 패배했습니다");
    }
}