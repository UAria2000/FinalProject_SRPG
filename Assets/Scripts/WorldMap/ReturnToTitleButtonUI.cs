using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ReturnToTitleButtonUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button titleButton;
    [SerializeField] private GameObject settingsPanelRoot;
    [SerializeField] private SaveCoordinator saveCoordinator;

    [Header("Scene")]
    [SerializeField] private string bootstrapSceneName = "Bootstrap";

    private void Awake()
    {
        if (saveCoordinator == null)
            saveCoordinator = SaveCoordinator.Instance;

        if (titleButton != null)
        {
            titleButton.onClick.RemoveAllListeners();
            titleButton.onClick.AddListener(HandleReturnToTitleClicked);
        }
    }

    private void HandleReturnToTitleClicked()
    {
        if (saveCoordinator == null)
            saveCoordinator = SaveCoordinator.Instance;

        // 설정창 먼저 닫기
        if (settingsPanelRoot != null)
            settingsPanelRoot.SetActive(false);

        // 현재 프로필 + 월드 상태 저장
        saveCoordinator?.SaveAll();

        // 타이틀(bootstrap) 씬으로 이동
        SceneManager.LoadScene(bootstrapSceneName);
    }
}