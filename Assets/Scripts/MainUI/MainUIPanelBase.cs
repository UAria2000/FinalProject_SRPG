using UnityEngine;
using UnityEngine.UI;

public class MainUIPanelBase : MonoBehaviour
{
    [SerializeField] private MainUIPanelType panelType = MainUIPanelType.None;
    [SerializeField] private Button closeButton;

    protected MainUIOverlayController overlayController;
    protected WorldRunManager worldRunManager;

    public MainUIPanelType PanelType => panelType;
    public bool IsOpen => gameObject.activeSelf;

    protected virtual void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HandleCloseClicked);
            closeButton.onClick.AddListener(HandleCloseClicked);
        }
    }

    public virtual void Setup(MainUIOverlayController controller, WorldRunManager manager)
    {
        overlayController = controller;
        worldRunManager = manager;
    }

    public virtual void OpenPanel()
    {
        gameObject.SetActive(true);
        OnPanelOpened();
    }

    public virtual void ClosePanel()
    {
        OnPanelClosed();
        gameObject.SetActive(false);
    }

    protected virtual void OnPanelOpened() { }
    protected virtual void OnPanelClosed() { }

    private void HandleCloseClicked()
    {
        if (overlayController != null)
            overlayController.CloseTopLayer();
        else
            ClosePanel();
    }
}
