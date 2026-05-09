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
    private bool initialized;
    private bool opening;

    private void Awake()
    {
        EnsureInitialized();

        // Open()이 비활성 Panel 오브젝트를 켜면서 Awake가 처음 호출되는 경우,
        // 여기서 다시 닫아버리면 결산창이 열린 직후 먹통처럼 사라진다.
        if (!opening)
            CloseSilently();
    }

    private void EnsureInitialized()
    {
        if (initialized)
            return;

        initialized = true;

        if (root == null)
            root = gameObject;

        if (confirmButton == null)
            confirmButton = root != null ? root.GetComponentInChildren<Button>(true) : GetComponentInChildren<Button>(true);

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(HandleConfirm);
        }
        else
        {
            Debug.LogWarning("[WorldSettlementPopupUI] Confirm Button is not assigned.", this);
        }
    }

    public void Open(WorldSettlementSummary summary, Action confirm)
    {
        opening = true;

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        EnsureInitialized();

        onConfirm = confirm;
        if (titleText != null) titleText.text = summary != null && summary.wasVictory ? "월드 정산 - 승리" : "월드 정산 - 실패";
        if (confirmText != null) confirmText.text = "확인";
        if (bodyText != null) bodyText.text = BuildBody(summary);

        SetVisible(true);
        opening = false;
    }

    public void CloseSilently()
    {
        opening = false;
        onConfirm = null;
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        if (root != null)
            root.SetActive(visible);
        else
            gameObject.SetActive(visible);
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
        sb.AppendLine();
        sb.AppendLine("정산 대상 아이템:");
        if (s.inventoryItems.Count == 0) sb.AppendLine("- 없음");
        else foreach (var item in s.inventoryItems) sb.AppendLine($"- {(item != null ? item.itemName : "Unknown")} ({(item != null ? item.baseSoulValue : 0)})");
        sb.AppendLine($"아이템 환산 소울: {s.convertedItemSoul}");
        sb.AppendLine();
        sb.AppendLine("정산 대상 포로:");
        if (s.prisonerUnits.Count == 0) sb.AppendLine("- 없음");
        else foreach (var unit in s.prisonerUnits) sb.AppendLine($"- {(unit != null ? unit.unitName : "Unknown")} ({(unit != null ? unit.baseSoulReward : 0)})");
        sb.AppendLine($"포로 환산 소울: {s.convertedPrisonerSoul}");
        sb.AppendLine();
        sb.AppendLine($"맵 크기 보너스: +{s.sizeBonusPercent}%");
        sb.AppendLine($"난이도 보너스: +{s.difficultyBonusPercent}%");
        sb.AppendLine($"월드 승리 보너스: +{s.victoryBonusPercent}%");
        sb.AppendLine();
        sb.AppendLine($"최종 정산 소울: {s.totalSettlementSoulAward}");
        sb.AppendLine();
        sb.AppendLine("정산 경험치:");
        sb.AppendLine($"점령 타일: {s.conqueredTileCount}개 / EXP {s.conqueredTileExp}");
        sb.AppendLine($"아이템 환산 EXP: {s.convertedItemExp}");
        sb.AppendLine($"포로 환산 EXP: {s.convertedPrisonerExp}");
        sb.AppendLine($"최종 정산 EXP: {s.totalSettlementExpAward}");
        return sb.ToString();
    }
}
