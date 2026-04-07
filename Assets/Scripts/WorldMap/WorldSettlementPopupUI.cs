using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorldSettlementPopupUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TMP_Text confirmText;

    private Action onConfirm;

    private void Awake()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(HandleConfirm);
        }
        CloseSilently();
    }

    public void Open(WorldSettlementSummary summary, Action confirm)
    {
        onConfirm = confirm;
        if (titleText != null) titleText.text = summary != null && summary.wasVictory ? "월드 정산 - 승리" : "월드 정산 - 실패";
        if (confirmText != null) confirmText.text = "확인";
        if (bodyText != null) bodyText.text = BuildBody(summary);
        if (root != null) root.SetActive(true); else gameObject.SetActive(true);
    }

    public void CloseSilently()
    {
        onConfirm = null;
        if (root != null) root.SetActive(false); else gameObject.SetActive(false);
    }

    private void HandleConfirm()
    {
        Action cb = onConfirm;
        CloseSilently();
        cb?.Invoke();
    }

    private string BuildBody(WorldSettlementSummary s)
    {
        if (s == null) return string.Empty;
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"월드 중 획득한 소울: {s.worldEarnedSoulAlreadyGranted}");
        sb.AppendLine($"아이템 환산 소울: {s.convertedItemSoul}");
        sb.AppendLine($"포로 환산 소울: {s.convertedPrisonerSoul}");
        sb.AppendLine($"맵 크기 보너스: +{s.sizeBonusPercent}%");
        sb.AppendLine($"난이도 보너스: +{s.difficultyBonusPercent}%");
        sb.AppendLine($"월드 승리 보너스: +{s.victoryBonusPercent}%");
        sb.AppendLine();
        sb.AppendLine($"최종 정산 소울: {s.totalSettlementSoulAward}");
        return sb.ToString();
    }
}
