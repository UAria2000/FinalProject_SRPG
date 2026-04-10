using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoragePrisonerCardUI : MonoBehaviour
{
    [SerializeField] private GameObject contentRoot;
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text conditionText;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Button actionButton;
    [SerializeField] private TMP_Text actionButtonText;
    [SerializeField] private GameObject exchangeableBadge;
    [SerializeField] private GameObject normalBadge;

    private StoragePanelUI owner;
    private WorldRunManager worldRunManager;
    private PrisonerRuntimeData prisonerData;

    private void Awake()
    {
        if (actionButton != null)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(HandleActionClicked);
        }
    }

    public void Bind(StoragePanelUI panelOwner, WorldRunManager manager, PrisonerRuntimeData data)
    {
        owner = panelOwner;
        worldRunManager = manager;
        prisonerData = data;

        bool hasData = data != null;

        if (contentRoot != null)
            contentRoot.SetActive(hasData);

        if (!hasData)
        {
            if (portraitImage != null)
                portraitImage.sprite = null;
            if (nameText != null)
                nameText.text = string.Empty;
            if (levelText != null)
                levelText.text = string.Empty;
            if (conditionText != null)
                conditionText.text = string.Empty;
            if (progressSlider != null)
                progressSlider.value = 0f;
            if (actionButton != null)
                actionButton.gameObject.SetActive(false);
            if (actionButtonText != null)
                actionButtonText.text = string.Empty;
            if (exchangeableBadge != null)
                exchangeableBadge.SetActive(false);
            if (normalBadge != null)
                normalBadge.SetActive(false);
            return;
        }

        if (portraitImage != null)
            portraitImage.sprite = data.GetPortrait();

        if (nameText != null)
            nameText.text = data.GetDisplayName();

        if (levelText != null)
            levelText.text = $"Lv.{data.capturedLevel}";

        if (conditionText != null)
            conditionText.text = data.GetConditionLabel();

        if (progressSlider != null)
            progressSlider.value = data.RequiresSoulPayment ? 0f : data.GetProgress01();

        if (exchangeableBadge != null)
            exchangeableBadge.SetActive(data.isExchangeable);

        if (normalBadge != null)
            normalBadge.SetActive(!data.isExchangeable);

        if (actionButton != null)
        {
            bool showButton = data.RequiresSoulPayment || data.IsReadyToCorrupt;
            actionButton.gameObject.SetActive(showButton);
            actionButton.interactable = data.RequiresSoulPayment
                ? (worldRunManager != null && worldRunManager.PersistentSoul >= data.targetValue)
                : data.IsReadyToCorrupt;
        }

        if (actionButtonText != null)
        {
            if (data.RequiresSoulPayment)
                actionButtonText.text = $"소울 {data.targetValue}";
            else if (data.IsReadyToCorrupt)
                actionButtonText.text = "타락";
            else
                actionButtonText.text = string.Empty;
        }
    }

    private void HandleActionClicked()
    {
        if (prisonerData == null)
            return;

        owner?.HandlePrisonerAction(this, prisonerData);
    }
}