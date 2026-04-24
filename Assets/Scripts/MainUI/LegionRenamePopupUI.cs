using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LegionRenamePopupUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private System.Action<string> confirmAction;
    private System.Action cancelAction;

    private void Awake()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(() =>
            {
                var a = confirmAction;
                string value = inputField != null ? inputField.text : string.Empty;
                Hide();
                a?.Invoke(value);
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

    public void Show(string currentName, System.Action<string> onConfirm, System.Action onCancel = null)
    {
        confirmAction = onConfirm;
        cancelAction = onCancel;
        if (root != null) root.SetActive(true); else gameObject.SetActive(true);
        if (inputField != null)
        {
            inputField.text = currentName ?? string.Empty;
            inputField.ActivateInputField();
            inputField.Select();
        }
    }

    public void Hide()
    {
        confirmAction = null;
        cancelAction = null;
        if (root != null) root.SetActive(false); else gameObject.SetActive(false);
    }
}
