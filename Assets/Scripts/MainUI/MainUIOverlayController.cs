using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainUIOverlayController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WorldRunManager worldRunManager;
    [SerializeField] private WorldMapDragPan worldMapDragPan;

    [Header("Buttons")]
    [SerializeField] private Button marketButton;
    [SerializeField] private Button storageButton;
    [SerializeField] private Button barracksButton;
    [SerializeField] private Button portraitButton;
    [SerializeField] private Button settingsButton;

    [Header("Catchers")]
    [SerializeField] private GameObject mainPanelDismissRoot;
    [SerializeField] private Button mainPanelDismissButton;
    [SerializeField] private GameObject settingsDimRoot;
    [SerializeField] private Button settingsDimButton;

    [Header("Panels")]
    [SerializeField] private List<MainUIPanelBase> mainPanels = new List<MainUIPanelBase>();
    [SerializeField] private MainUIPanelBase settingsPanel;
    [SerializeField] private BottomPartySummaryPanelUI bottomPartySummaryPanelUI;
    [SerializeField] private GameObject rightInfoPanel;

    private readonly Dictionary<MainUIPanelType, MainUIPanelBase> panelLookup = new Dictionary<MainUIPanelType, MainUIPanelBase>();
    private MainUIPanelBase currentMainPanel;

    private void Awake()
    {
        if (worldRunManager == null)
            worldRunManager = Object.FindFirstObjectByType<WorldRunManager>();

        if (worldMapDragPan == null)
            worldMapDragPan = Object.FindFirstObjectByType<WorldMapDragPan>();

        panelLookup.Clear();
        for (int i = 0; i < mainPanels.Count; i++)
        {
            MainUIPanelBase panel = mainPanels[i];
            if (panel == null)
                continue;

            panel.Setup(this, worldRunManager);
            panel.ClosePanel();
            panelLookup[panel.PanelType] = panel;
        }

        if (settingsPanel != null)
        {
            settingsPanel.Setup(this, worldRunManager);
            settingsPanel.ClosePanel();
        }

        BindButton(marketButton, () => OpenMainPanel(MainUIPanelType.Market));
        BindButton(storageButton, () => OpenMainPanel(MainUIPanelType.Storage));
        BindButton(barracksButton, () => OpenMainPanel(MainUIPanelType.Barracks));
        BindButton(portraitButton, () => OpenMainPanel(MainUIPanelType.Portrait));
        BindButton(settingsButton, OpenSettingsPanel);
        BindButton(mainPanelDismissButton, CloseCurrentMainPanel);
        BindButton(settingsDimButton, CloseSettingsPanel);

        if (mainPanelDismissRoot != null)
            mainPanelDismissRoot.SetActive(false);
        if (settingsDimRoot != null)
            settingsDimRoot.SetActive(false);

        UpdateWorldMapInputLock();
    }

    public void OpenMainPanel(MainUIPanelType panelType)
    {
        if (IsSettingsOpen())
            return;

        if (!panelLookup.TryGetValue(panelType, out MainUIPanelBase nextPanel) || nextPanel == null)
            return;

        bool isStorage = panelType == MainUIPanelType.Storage;
        bool isBarracks = panelType == MainUIPanelType.Barracks;

        if (currentMainPanel != null && currentMainPanel.IsOpen)
            currentMainPanel.ClosePanel();

        currentMainPanel = nextPanel;
        currentMainPanel.OpenPanel();

        if (mainPanelDismissRoot != null)
            mainPanelDismissRoot.SetActive(true);

        if (bottomPartySummaryPanelUI != null)
        {
            bottomPartySummaryPanelUI.SetStorageMode(isStorage);
            bottomPartySummaryPanelUI.SetBarracksMode(isBarracks);
        }

        if (rightInfoPanel != null)
            rightInfoPanel.SetActive(false);

        UpdateWorldMapInputLock();
    }

    public void CloseCurrentMainPanel()
    {
        if (currentMainPanel != null && currentMainPanel.IsOpen)
            currentMainPanel.ClosePanel();

        currentMainPanel = null;

        if (mainPanelDismissRoot != null)
            mainPanelDismissRoot.SetActive(false);

        if (bottomPartySummaryPanelUI != null)
        {
            bottomPartySummaryPanelUI.SetStorageMode(false);
            bottomPartySummaryPanelUI.SetBarracksMode(false);
        }

        UpdateWorldMapInputLock();
    }

    public void OpenSettingsPanel()
    {
        if (settingsPanel == null || IsSettingsOpen())
            return;

        settingsPanel.OpenPanel();
        if (settingsDimRoot != null)
            settingsDimRoot.SetActive(true);

        UpdateWorldMapInputLock();
    }

    public void CloseSettingsPanel()
    {
        if (settingsPanel != null && settingsPanel.IsOpen)
            settingsPanel.ClosePanel();

        if (settingsDimRoot != null)
            settingsDimRoot.SetActive(false);

        UpdateWorldMapInputLock();
    }

    public void CloseTopLayer()
    {
        if (IsSettingsOpen())
        {
            CloseSettingsPanel();
            return;
        }

        CloseCurrentMainPanel();
    }

    public bool IsSettingsOpen()
    {
        return settingsPanel != null && settingsPanel.IsOpen;
    }

    private void UpdateWorldMapInputLock()
    {
        bool shouldLock =
            (currentMainPanel != null && currentMainPanel.IsOpen) ||
            (settingsPanel != null && settingsPanel.IsOpen);

        if (worldMapDragPan != null)
            worldMapDragPan.SetInputLocked(shouldLock);
    }

    private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }
}