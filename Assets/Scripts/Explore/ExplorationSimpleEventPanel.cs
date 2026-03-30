using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExplorationSimpleEventPanel : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Button closeButton;
    [SerializeField] private ExplorationBattleBridge bridge;

    private void Awake()
    {
        if (root != null)
            root.SetActive(false);

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseAndReturnToMap);
        }
    }

    public void Open(string title, string body)
    {
        if (titleText != null)
            titleText.text = title;

        if (bodyText != null)
            bodyText.text = body;

        if (root != null)
            root.SetActive(true);
    }

    public void CloseAndReturnToMap()
    {
        if (root != null)
            root.SetActive(false);

        if (bridge != null)
            bridge.ReturnToMap();
    }
}
