using TMPro;
using UnityEngine;

public class FleeTooltipUI : HoverPopupUIBase
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text chanceText;

    public void Show(int fleeChancePercent, Vector2 pointerScreenPosition)
    {
        ShowRootAt(root, pointerScreenPosition);

        if (chanceText != null)
            chanceText.text = $"{Mathf.Clamp(fleeChancePercent, 0, 100)}%";
    }

    public void Hide()
    {
        HideRoot(root);
    }
}
