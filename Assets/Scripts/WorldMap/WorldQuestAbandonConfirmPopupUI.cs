using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorldQuestAbandonConfirmPopupUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;
    [SerializeField] private Button dimButton;

    [Header("Texts")]
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TMP_Text confirmButtonText;
    [SerializeField] private TMP_Text closeButtonText;

    [Header("Buttons")]
    [SerializeField] private Button abandonButton;
    [SerializeField] private Button closeButton;

    [Header("Defaults")]
    [SerializeField] private string defaultMessage = "정말 이 퀘스트를 포기하시겠습니까?\n진행도는 초기화되며 다시 받을 수 없습니다.";
    [SerializeField] private string defaultConfirmLabel = "퀘스트 포기";
    [SerializeField] private string defaultCloseLabel = "닫기";

    private Action confirmAction;
    private Action closeAction;

    public bool IsOpen => root != null && root.activeSelf;

    private void Awake()
    {
        if (dimButton != null)
        {
            dimButton.onClick.RemoveAllListeners();
            dimButton.onClick.AddListener(HandleCloseClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(HandleCloseClicked);
        }

        if (abandonButton != null)
        {
            abandonButton.onClick.RemoveAllListeners();
            abandonButton.onClick.AddListener(HandleConfirmClicked);
        }

        Hide();
    }

    public void Initialize(WorldQuestController _)
    {
        // 하위 호환용. 현재는 액션 콜백 방식만 사용.
    }

    public void Show(
        string message = null,
        string confirmLabel = null,
        string cancelLabel = null,
        Action onConfirm = null,
        Action onCancel = null)
    {
        confirmAction = onConfirm;
        closeAction = onCancel;

        if (root != null)
            root.SetActive(true);

        if (messageText != null)
            messageText.text = string.IsNullOrEmpty(message) ? defaultMessage : message;

        if (confirmButtonText != null)
            confirmButtonText.text = string.IsNullOrEmpty(confirmLabel) ? defaultConfirmLabel : confirmLabel;

        if (closeButtonText != null)
            closeButtonText.text = string.IsNullOrEmpty(cancelLabel) ? defaultCloseLabel : cancelLabel;
    }

    public void Hide()
    {
        confirmAction = null;
        closeAction = null;

        if (root != null)
            root.SetActive(false);
    }

    private void HandleConfirmClicked()
    {
        Action action = confirmAction;
        Hide();
        action?.Invoke();
    }

    private void HandleCloseClicked()
    {
        Action action = closeAction;
        Hide();
        action?.Invoke();
    }
}