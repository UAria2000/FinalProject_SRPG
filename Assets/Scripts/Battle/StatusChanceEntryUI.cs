using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatusChanceEntryUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text chanceText;

    public void Bind(StatusChancePreviewData data)
    {
        if (iconImage != null)
        {
            iconImage.sprite = data.icon;
            iconImage.color = data.icon != null ? UnityEngine.Color.white : new UnityEngine.Color(1f, 1f, 1f, 0f);
        }

        if (chanceText != null)
            chanceText.text = string.Format("{0}%", data.successPercent);
    }
}
