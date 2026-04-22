using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorldQuestRowUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject root;
    [SerializeField] private Button rowButton;
    [SerializeField] private Image checkboxImage;
    [SerializeField] private TMP_Text questText;
    [SerializeField] private Button cancelButton;

    [Header("Sprites")]
    [SerializeField] private Sprite uncheckedSprite;
    [SerializeField] private Sprite checkedSprite;

    private WorldQuestListPanelUI owner;
    private WorldQuestState boundQuest;

    public void Bind(WorldQuestListPanelUI panelOwner, WorldQuestState quest, bool visible)
    {
        owner = panelOwner;
        boundQuest = quest;

        GameObject targetRoot = root != null ? root : gameObject;
        targetRoot.SetActive(visible);

        if (!visible || quest == null)
            return;

        if (checkboxImage != null)
            checkboxImage.sprite = quest.isCompleted ? checkedSprite : uncheckedSprite;

        if (questText != null)
        {
            questText.richText = true;
            questText.text = quest.GetListProgressTextRich();
        }

        if (cancelButton != null)
        {
            bool canCancel = quest.isAccepted && !quest.isCompleted && !quest.isCancelled;
            cancelButton.gameObject.SetActive(canCancel);
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(HandleCancelClicked);
        }

        if (rowButton != null)
        {
            rowButton.onClick.RemoveAllListeners();
            rowButton.onClick.AddListener(HandleRowClicked);
        }
    }

    private void HandleRowClicked()
    {
        if (owner == null || boundQuest == null)
            return;

        owner.HandleQuestRowClicked(boundQuest);
    }

    private void HandleCancelClicked()
    {
        if (owner == null || boundQuest == null)
            return;

        owner.HandleQuestCancelClicked(boundQuest);
    }
}