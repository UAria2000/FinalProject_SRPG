using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleRewardPopupUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Button closeButton;

    private Action onClose;

    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(HandleClose);
        }
        CloseSilently();
    }

    public void Open(BattleRewardSummary summary, Action closeAction)
    {
        onClose = closeAction;
        if (titleText != null) titleText.text = "전투 보상";
        if (bodyText != null) bodyText.text = BuildBody(summary);
        if (root != null) root.SetActive(true); else gameObject.SetActive(true);
    }

    public void CloseSilently()
    {
        onClose = null;
        if (root != null) root.SetActive(false); else gameObject.SetActive(false);
    }

    private void HandleClose()
    {
        Action cb = onClose;
        CloseSilently();
        cb?.Invoke();
    }

    private string BuildBody(BattleRewardSummary summary)
    {
        if (summary == null) return string.Empty;
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"획득 소울: {summary.soulReward}");
        sb.AppendLine();
        sb.AppendLine("전리품:");
        if (summary.droppedItems.Count == 0) sb.AppendLine("- 없음");
        else foreach (var item in summary.droppedItems) sb.AppendLine($"- {(item != null ? item.itemName : "Unknown")}");
        sb.AppendLine();
        sb.AppendLine("포획한 포로:");
        if (summary.capturedPrisoners.Count == 0) sb.AppendLine("- 없음");
        else foreach (var unit in summary.capturedPrisoners) sb.AppendLine($"- {(unit != null ? unit.unitName : "Unknown")}");
        return sb.ToString();
    }
}
