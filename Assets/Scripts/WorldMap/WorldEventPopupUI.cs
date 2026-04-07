using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorldEventPopupUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private TMP_Text confirmButtonText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button closeButton;

    private Action confirmAction;
    private Action closeAction;

    private void Awake()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(HandleConfirmClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(HandleCloseClicked);
        }

        CloseSilently();
    }

    public void Open(string title, string body, string confirmLabel, Action onConfirm, Action onClose = null)
    {
        confirmAction = onConfirm;
        closeAction = onClose;

        if (titleText != null)
            titleText.text = title;

        if (bodyText != null)
            bodyText.text = body;

        if (confirmButtonText != null)
            confirmButtonText.text = string.IsNullOrWhiteSpace(confirmLabel) ? "확인" : confirmLabel;

        if (root != null)
            root.SetActive(true);
        else
            gameObject.SetActive(true);
    }

    public void CloseSilently()
    {
        confirmAction = null;
        closeAction = null;

        if (root != null)
            root.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    private void HandleConfirmClicked()
    {
        Action action = confirmAction;
        CloseSilently();
        action?.Invoke();
    }

    private void HandleCloseClicked()
    {
        Action action = closeAction;
        CloseSilently();
        action?.Invoke();
    }
}
