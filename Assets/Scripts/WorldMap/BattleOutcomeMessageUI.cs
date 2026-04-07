using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleOutcomeMessageUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TMP_Text confirmText;
    [SerializeField] private Button confirmButton;

    private Action onConfirm;

    private void Awake()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(HandleConfirm);
        }
        Close();
    }

    public void Open(string message, string confirmLabel, Action confirm)
    {
        onConfirm = confirm;
        if (messageText != null) messageText.text = message;
        if (confirmText != null) confirmText.text = string.IsNullOrWhiteSpace(confirmLabel) ? "확인" : confirmLabel;
        if (root != null) root.SetActive(true); else gameObject.SetActive(true);
    }

    public void Close()
    {
        onConfirm = null;
        if (root != null) root.SetActive(false); else gameObject.SetActive(false);
    }

    private void HandleConfirm()
    {
        Action cb = onConfirm;
        Close();
        cb?.Invoke();
    }
}
