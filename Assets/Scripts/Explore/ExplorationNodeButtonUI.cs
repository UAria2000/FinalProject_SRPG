using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExplorationNodeButtonUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private GameObject currentMarker;

    private int nodeId;
    private Action<int> clickHandler;

    public RectTransform RectTransform => transform as RectTransform;

    public void Initialize(int inNodeId, Action<int> onClick)
    {
        nodeId = inNodeId;
        clickHandler = onClick;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
        }
    }

    public void SetLabel(string text)
    {
        if (labelText != null)
            labelText.text = text;
    }

    public void SetVisual(Color backgroundColor, Color textColor, bool interactable, bool isCurrent)
    {
        if (backgroundImage != null)
            backgroundImage.color = backgroundColor;

        if (labelText != null)
            labelText.color = textColor;

        if (button != null)
            button.interactable = interactable;

        if (currentMarker != null)
            currentMarker.SetActive(isCurrent);
    }

    private void HandleClick()
    {
        Debug.Log($"[ExplorationNodeButtonUI] Clicked nodeId={nodeId}");
        clickHandler?.Invoke(nodeId);
    }
}
