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
    private bool initialized;
    private bool opening;

    private void Awake()
    {
        EnsureInitialized();
        if (!opening)
            Close();
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
            Debug.LogWarning("[BattleOutcomeMessageUI] Confirm Button is not assigned.", this);
        }
    }

    public void Open(string message, string confirmLabel, Action confirm)
    {
        opening = true;

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        EnsureInitialized();

        onConfirm = confirm;
        if (messageText != null) messageText.text = message;
        if (confirmText != null) confirmText.text = string.IsNullOrWhiteSpace(confirmLabel) ? "확인" : confirmLabel;

        SetVisible(true);
        opening = false;
    }

    public void Close()
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
        Close();
        cb?.Invoke();
    }
}
