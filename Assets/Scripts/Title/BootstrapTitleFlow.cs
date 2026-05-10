using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BootstrapTitleFlow : MonoBehaviour
{
    [Header("Roots")]
    [SerializeField] private GameObject titleRoot;
    [SerializeField] private GameObject newWorldSetupRoot;

    [Header("Main Menu Buttons")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button newWorldButton;
    [SerializeField] private Button deleteAccountButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;

    [Header("Difficulty Buttons")]
    [SerializeField] private BootstrapOptionButtonUI easyButton;
    [SerializeField] private BootstrapOptionButtonUI normalButton;
    [SerializeField] private BootstrapOptionButtonUI hardButton;

    [Header("Map Size Slider")]
    [SerializeField] private Slider mapSizeSlider;
    [SerializeField] private TMP_Text mapSizeValueText;
    [SerializeField] private string mapSizeUnselectedText = "맵 크기 선택";
    [SerializeField] private string smallLabel = "소형";
    [SerializeField] private string mediumLabel = "중형";
    [SerializeField] private string largeLabel = "대형";

    [Header("Setup Buttons")]
    [SerializeField] private Button startWorldButton;
    [SerializeField] private Button backButton;

    [Header("Texts")]
    [SerializeField] private TMP_Text accountText;
    [SerializeField] private TMP_Text versionText;

    [Header("Confirm Popup")]
    [SerializeField] private BootstrapConfirmPopupUI overwriteWorldConfirmPopup;
    [SerializeField] private BootstrapConfirmPopupUI deleteAccountConfirmPopup;

    [Header("Confirm Popup Text")]
    [TextArea(3, 8)]
    [SerializeField]
    private string overwriteWorldMessage =
        "기존에 진행 중인 월드가 있습니다.\n\n" +
        "새 월드를 생성하면 기존 월드는 영구 삭제됩니다.\n" +
        "모든 사망한 폴른(유닛), 아이템, 포로가 영구 삭제되며,\n" +
        "다음 월드에서의 최대 마나는 절반이 됩니다.\n\n" +
        "정말 새 월드를 생성하시겠습니까?";

    [SerializeField] private string overwriteWorldConfirmLabel = "동의합니다";
    [SerializeField] private string overwriteWorldCancelLabel = "취소";

    [Header("Delete Account Progress")]
    [TextArea(2, 6)]
    [SerializeField] private string deleteAccountMessage = "Delete all account progress data?\nDeleted progress data cannot be restored.";
    [SerializeField] private string deleteAccountConfirmLabel = "Delete";
    [SerializeField] private string deleteAccountCancelLabel = "Cancel";

    [Header("Scene")]
    [SerializeField] private string worldMapSceneName = "WorldMap";

    private SaveCoordinator saveCoordinator;

    private string selectedDifficultyId;
    private int selectedMapRadius;
    private bool hasSelectedMapSize;

    private void Awake()
    {
        saveCoordinator = SaveCoordinator.Instance;

        WireButtons();
        ConfigureMapSizeSlider();

        ShowTitle();
        RefreshTexts();
        RefreshContinueButton();
        RefreshSetupState();
    }

    private void Start()
    {
        saveCoordinator = SaveCoordinator.Instance;
        RefreshTexts();
        RefreshContinueButton();
        RefreshSetupState();
    }

    private void WireButtons()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(HandleContinueClicked);
        }

        if (newWorldButton != null)
        {
            newWorldButton.onClick.RemoveAllListeners();
            newWorldButton.onClick.AddListener(HandleNewWorldClicked);
        }

        if (deleteAccountButton != null)
        {
            deleteAccountButton.onClick.RemoveAllListeners();
            deleteAccountButton.onClick.AddListener(HandleDeleteAccountClicked);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(HandleSettingsClicked);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(HandleExitClicked);
        }

        if (startWorldButton != null)
        {
            startWorldButton.onClick.RemoveAllListeners();
            startWorldButton.onClick.AddListener(HandleStartWorldClicked);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(HandleBackClicked);
        }

        BindOption(easyButton, () => SelectDifficulty("easy"));
        BindOption(normalButton, () => SelectDifficulty("normal"));
        BindOption(hardButton, () => SelectDifficulty("hard"));
    }

    private void BindOption(BootstrapOptionButtonUI option, UnityEngine.Events.UnityAction action)
    {
        if (option == null || option.Button == null)
            return;

        option.Button.onClick.RemoveAllListeners();
        option.Button.onClick.AddListener(action);
    }

    private void ConfigureMapSizeSlider()
    {
        if (mapSizeSlider == null)
            return;

        mapSizeSlider.wholeNumbers = true;
        mapSizeSlider.minValue = 0f;
        mapSizeSlider.maxValue = 2f;
        mapSizeSlider.SetValueWithoutNotify(1f);

        mapSizeSlider.onValueChanged.RemoveAllListeners();
        mapSizeSlider.onValueChanged.AddListener(HandleMapSizeSliderChanged);

        hasSelectedMapSize = false;
        selectedMapRadius = 0;
        RefreshMapSizeText();
    }

    private void HandleMapSizeSliderChanged(float value)
    {
        hasSelectedMapSize = true;

        int step = Mathf.RoundToInt(value);
        switch (step)
        {
            case 0:
                selectedMapRadius = 4;
                break;
            case 1:
                selectedMapRadius = 5;
                break;
            case 2:
                selectedMapRadius = 6;
                break;
            default:
                selectedMapRadius = 5;
                break;
        }

        RefreshMapSizeText();
        RefreshSetupState();
    }

    private void RefreshMapSizeText()
    {
        if (mapSizeValueText == null)
            return;

        if (!hasSelectedMapSize)
        {
            mapSizeValueText.text = mapSizeUnselectedText;
            return;
        }

        switch (selectedMapRadius)
        {
            case 4:
                mapSizeValueText.text = smallLabel;
                break;
            case 5:
                mapSizeValueText.text = mediumLabel;
                break;
            case 6:
                mapSizeValueText.text = largeLabel;
                break;
            default:
                mapSizeValueText.text = mapSizeUnselectedText;
                break;
        }
    }

    private void RefreshTexts()
    {
        if (accountText != null)
        {
            string nick = saveCoordinator != null ? saveCoordinator.Nickname : "Player";
            accountText.text = $"환영합니다 {nick}님";
        }

        if (versionText != null)
            versionText.text = "v.0.01.a";
    }

    private void RefreshContinueButton()
    {
        bool canContinue = saveCoordinator != null && saveCoordinator.HasSavedActiveWorld();

        if (continueButton != null)
            continueButton.interactable = canContinue;
    }

    private void RefreshSetupState()
    {
        if (easyButton != null) easyButton.SetSelected(selectedDifficultyId == "easy");
        if (normalButton != null) normalButton.SetSelected(selectedDifficultyId == "normal");
        if (hardButton != null) hardButton.SetSelected(selectedDifficultyId == "hard");

        bool canStart = !string.IsNullOrEmpty(selectedDifficultyId) && hasSelectedMapSize;

        if (startWorldButton != null)
            startWorldButton.interactable = canStart;
    }

    private void SelectDifficulty(string difficultyId)
    {
        selectedDifficultyId = difficultyId;
        RefreshSetupState();
    }

    private void ShowTitle()
    {
        if (titleRoot != null)
            titleRoot.SetActive(true);

        if (newWorldSetupRoot != null)
            newWorldSetupRoot.SetActive(false);
    }

    private void ShowNewWorldSetup()
    {
        if (titleRoot != null)
            titleRoot.SetActive(false);

        if (newWorldSetupRoot != null)
            newWorldSetupRoot.SetActive(true);

        RefreshSetupState();
        RefreshMapSizeText();
    }

    private void HandleContinueClicked()
    {
        if (saveCoordinator == null || !saveCoordinator.HasSavedActiveWorld())
            return;

        saveCoordinator.QueueContinueWorld();
        SceneManager.LoadScene(worldMapSceneName);
    }

    private void HandleNewWorldClicked()
    {
        ShowNewWorldSetup();
    }

    private void HandleDeleteAccountClicked()
    {
        BootstrapConfirmPopupUI popup = deleteAccountConfirmPopup != null ? deleteAccountConfirmPopup : overwriteWorldConfirmPopup;
        if (popup != null)
        {
            popup.Show(
                deleteAccountMessage,
                deleteAccountConfirmLabel,
                deleteAccountCancelLabel,
                ConfirmDeleteAccountProgress,
                null);
        }
        else
        {
            ConfirmDeleteAccountProgress();
        }
    }

    private void ConfirmDeleteAccountProgress()
    {
        if (saveCoordinator == null)
            saveCoordinator = SaveCoordinator.Instance;

        saveCoordinator?.DeleteAccountProgressData();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void HandleSettingsClicked()
    {
        Debug.Log("[BootstrapTitleFlow] 환경설정은 추후 연결 예정.");
    }

    private void HandleExitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void HandleBackClicked()
    {
        ShowTitle();
    }

    private void HandleStartWorldClicked()
    {
        if (saveCoordinator == null)
        {
            Debug.LogWarning("[BootstrapTitleFlow] SaveCoordinator is missing.");
            return;
        }

        if (string.IsNullOrEmpty(selectedDifficultyId) || !hasSelectedMapSize)
            return;

        if (saveCoordinator.HasSavedActiveWorld())
        {
            if (overwriteWorldConfirmPopup != null)
            {
                overwriteWorldConfirmPopup.Show(
                    overwriteWorldMessage,
                    overwriteWorldConfirmLabel,
                    overwriteWorldCancelLabel,
                    ConfirmOverwriteAndStartNewWorld,
                    null);
            }
            else
            {
                ConfirmOverwriteAndStartNewWorld();
            }

            return;
        }

        StartQueuedNewWorld();
    }

    private void ConfirmOverwriteAndStartNewWorld()
    {
        if (saveCoordinator == null)
            return;

        saveCoordinator.ClearSavedWorldRunAsAbandoned();
        StartQueuedNewWorld();
    }

    private void StartQueuedNewWorld()
    {
        if (saveCoordinator == null)
            return;

        saveCoordinator.QueueNewWorldStart(selectedDifficultyId, selectedMapRadius);
        SceneManager.LoadScene(worldMapSceneName);
    }
}