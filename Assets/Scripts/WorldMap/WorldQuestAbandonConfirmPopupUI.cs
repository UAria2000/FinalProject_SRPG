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

    [Header("Buttons")]
    [SerializeField] private Button abandonButton;
    [SerializeField] private Button closeButton;

    [Header("Message")]
    [SerializeField] private string defaultMessage = "정말 이 퀘스트를 포기하시겠습니까?\n진행도는 초기화되며 다시 받을 수 없습니다.";

    private WorldQuestController owner;
    private WorldQuestState currentQuest;

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
            abandonButton.onClick.AddListener(HandleAbandonClicked);
        }

        Hide();
    }

    public void Initialize(WorldQuestController controller)
    {
        owner = controller;
    }

    public void Show(WorldQuestState quest)
    {
        currentQuest = quest;

        if (root != null)
            root.SetActive(true);

        if (messageText != null)
            messageText.text = defaultMessage;
    }

    public void Hide()
    {
        currentQuest = null;

        if (root != null)
            root.SetActive(false);
    }

    private void HandleAbandonClicked()
    {
        owner?.ConfirmQuestAbandon(currentQuest);
    }

    private void HandleCloseClicked()
    {
        owner?.CloseQuestAbandonConfirmPopup();
    }
}