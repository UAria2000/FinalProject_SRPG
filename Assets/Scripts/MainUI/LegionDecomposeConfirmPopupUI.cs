using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LegionDecomposeConfirmPopupUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private System.Action confirmAction;
    private System.Action cancelAction;

    private void Awake()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(() =>
            {
                var a = confirmAction;
                Hide();
                a?.Invoke();
            });
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(() =>
            {
                var a = cancelAction;
                Hide();
                a?.Invoke();
            });
        }

        Hide();
    }

    public void Show(string title, string body, System.Action onConfirm, System.Action onCancel = null)
    {
        confirmAction = onConfirm;
        cancelAction = onCancel;
        if (root != null) root.SetActive(true); else gameObject.SetActive(true);
        if (titleText != null) titleText.text = title;
        if (bodyText != null) bodyText.text = body;
    }

    public void Hide()
    {
        confirmAction = null;
        cancelAction = null;
        if (root != null) root.SetActive(false); else gameObject.SetActive(false);
    }
}
