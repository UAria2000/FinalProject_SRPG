using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class LegionRenamePopupUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;
    [Tooltip("배경 Dim 전체를 Button으로 만들었다면 연결. 클릭 시 취소/닫기 처리된다.")]
    [SerializeField] private Button backgroundCloseButton;

    [Header("Texts")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text errorText;

    [Header("Input")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField, Min(1)] private int maxNameLength = 12;
    [SerializeField] private bool allowEmptyAsDefaultName = true;

    [Header("Buttons")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [Tooltip("기본 이름으로 되돌리기 버튼. 누르면 이름 Override를 빈 값으로 저장한다.")]
    [SerializeField] private Button resetToDefaultButton;

    private System.Action<string> confirmAction;
    private System.Action cancelAction;
    private bool isShowing;

    private void Awake()
    {
        if (inputField != null)
        {
            inputField.characterLimit = Mathf.Max(1, maxNameLength);
            inputField.onValueChanged.RemoveListener(HandleInputChanged);
            inputField.onValueChanged.AddListener(HandleInputChanged);
            inputField.onSubmit.RemoveListener(HandleInputSubmit);
            inputField.onSubmit.AddListener(HandleInputSubmit);
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(ConfirmCurrentInput);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(CancelAndHide);
        }

        if (backgroundCloseButton != null)
        {
            backgroundCloseButton.onClick.RemoveAllListeners();
            backgroundCloseButton.onClick.AddListener(CancelAndHide);
        }

        if (resetToDefaultButton != null)
        {
            resetToDefaultButton.onClick.RemoveAllListeners();
            resetToDefaultButton.onClick.AddListener(ConfirmDefaultNameReset);
        }

        Hide();
    }

    private void Update()
    {
        if (!isShowing)
            return;

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            CancelAndHide();
            return;
        }

        if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
            ConfirmCurrentInput();
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelAndHide();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            ConfirmCurrentInput();
#endif
    }

    public void Show(string currentName, System.Action<string> onConfirm, System.Action onCancel = null)
    {
        confirmAction = onConfirm;
        cancelAction = onCancel;
        isShowing = true;

        if (root != null)
            root.SetActive(true);
        else
            gameObject.SetActive(true);

        SetText(titleText, "이름 변경");
        SetText(descriptionText, $"새 이름을 입력하세요. 최대 {Mathf.Max(1, maxNameLength)}자");
        SetText(errorText, string.Empty);

        if (inputField != null)
        {
            inputField.characterLimit = Mathf.Max(1, maxNameLength);
            inputField.SetTextWithoutNotify(currentName ?? string.Empty);
            inputField.ActivateInputField();
            inputField.Select();
            inputField.MoveTextEnd(false);
        }

        RefreshValidation();
    }

    public void Hide()
    {
        isShowing = false;
        confirmAction = null;
        cancelAction = null;

        if (inputField != null)
            inputField.DeactivateInputField();

        if (root != null)
            root.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    private void HandleInputSubmit(string _)
    {
        if (isShowing)
            ConfirmCurrentInput();
    }

    private void HandleInputChanged(string _)
    {
        RefreshValidation();
    }

    private void RefreshValidation()
    {
        string value = inputField != null ? inputField.text : string.Empty;
        string trimmed = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

        bool valid = allowEmptyAsDefaultName || !string.IsNullOrWhiteSpace(trimmed);

        if (!valid)
            SetText(errorText, "이름을 입력해야 합니다.");
        else if (trimmed.Length > maxNameLength)
            SetText(errorText, $"이름은 최대 {maxNameLength}자까지 가능합니다.");
        else
            SetText(errorText, string.Empty);

        if (confirmButton != null)
            confirmButton.interactable = valid;
    }

    private void ConfirmCurrentInput()
    {
        string value = inputField != null ? inputField.text : string.Empty;
        string trimmed = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

        if (!allowEmptyAsDefaultName && string.IsNullOrWhiteSpace(trimmed))
        {
            RefreshValidation();
            return;
        }

        System.Action<string> action = confirmAction;
        Hide();
        action?.Invoke(trimmed);
    }

    private void ConfirmDefaultNameReset()
    {
        System.Action<string> action = confirmAction;
        Hide();
        action?.Invoke(string.Empty);
    }

    private void CancelAndHide()
    {
        System.Action action = cancelAction;
        Hide();
        action?.Invoke();
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
            target.text = value ?? string.Empty;
    }
}
