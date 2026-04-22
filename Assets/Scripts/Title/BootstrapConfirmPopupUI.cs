using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BootstrapConfirmPopupUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;
    [SerializeField] private Button dimButton;

    [Header("Texts")]
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TMP_Text confirmButtonText;
    [SerializeField] private TMP_Text cancelButtonText;

    [Header("Buttons")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private Action confirmAction;
    private Action cancelAction;

    public bool IsOpen => root != null && root.activeSelf;

    private void Awake()
    {
        if (dimButton != null)
        {
            dimButton.onClick.RemoveAllListeners();
            dimButton.onClick.AddListener(HandleCancelClicked);
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(HandleConfirmClicked);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(HandleCancelClicked);
        }

        Hide();
    }

    public void Show(
        string message,
        string confirmLabel,
        string cancelLabel,
        Action onConfirm,
        Action onCancel = null)
    {
        confirmAction = onConfirm;
        cancelAction = onCancel;

        if (root != null)
            root.SetActive(true);

        if (messageText != null)
            messageText.text = message ?? string.Empty;

        if (confirmButtonText != null)
            confirmButtonText.text = string.IsNullOrEmpty(confirmLabel) ? "확인" : confirmLabel;

        if (cancelButtonText != null)
            cancelButtonText.text = string.IsNullOrEmpty(cancelLabel) ? "취소" : cancelLabel;
    }

    public void Hide()
    {
        confirmAction = null;
        cancelAction = null;

        if (root != null)
            root.SetActive(false);
    }

    private void HandleConfirmClicked()
    {
        Action action = confirmAction;
        Hide();
        action?.Invoke();
    }

    private void HandleCancelClicked()
    {
        Action action = cancelAction;
        Hide();
        action?.Invoke();
    }
}