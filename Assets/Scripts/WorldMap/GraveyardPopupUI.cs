using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GraveyardPopupUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;
    [SerializeField] private Button dimCloseButton;
    [SerializeField] private Button closeButton;

    [Header("Texts")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text pageText;

    [Header("Cards")]
    [SerializeField] private RectTransform cardGridRoot;
    [SerializeField] private GraveyardUnitCardUI cardPrefab;
    [SerializeField] private List<GraveyardUnitCardUI> cardSlots = new List<GraveyardUnitCardUI>(10);
    [SerializeField, Min(1)] private int cardsPerPage = 10;

    [Header("Paging")]
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;

    private PersistentProfileController profileController;
    private Action closeAction;
    private int pageIndex;

    private void Awake()
    {
        if (profileController == null)
            profileController = UnityEngine.Object.FindFirstObjectByType<PersistentProfileController>();

        BindButton(dimCloseButton, Close);
        BindButton(closeButton, Close);
        BindButton(prevButton, PrevPage);
        BindButton(nextButton, NextPage);

        CloseSilently();
    }

    public void Open(string title, string description, Action onClose = null)
    {
        closeAction = onClose;
        pageIndex = 0;

        if (titleText != null)
            titleText.text = string.IsNullOrWhiteSpace(title) ? "묘지" : title;

        if (descriptionText != null)
            descriptionText.text = description ?? string.Empty;

        SetOpen(true);
        Refresh();
    }

    public void Close()
    {
        Action action = closeAction;
        CloseSilently();
        action?.Invoke();
    }

    public void CloseSilently()
    {
        closeAction = null;
        SetOpen(false);
    }

    private void PrevPage()
    {
        pageIndex = Mathf.Max(0, pageIndex - 1);
        Refresh();
    }

    private void NextPage()
    {
        int totalPages = GetTotalPages();
        pageIndex = Mathf.Min(Mathf.Max(0, totalPages - 1), pageIndex + 1);
        Refresh();
    }

    private void Refresh()
    {
        EnsureCards();

        IReadOnlyList<PersistentRosterUnitData> units = GetGraveyardUnits();
        int count = units != null ? units.Count : 0;
        int perPage = Mathf.Max(1, cardsPerPage);
        int totalPages = Mathf.Max(1, Mathf.CeilToInt(count / (float)perPage));
        pageIndex = Mathf.Clamp(pageIndex, 0, totalPages - 1);
        int start = pageIndex * perPage;

        for (int i = 0; i < cardSlots.Count; i++)
        {
            GraveyardUnitCardUI card = cardSlots[i];
            if (card == null)
                continue;

            int unitIndex = start + i;
            if (units != null && unitIndex >= 0 && unitIndex < units.Count)
                card.Bind(units[unitIndex]);
            else
                card.Clear();
        }

        if (pageText != null)
            pageText.text = $"{pageIndex + 1}/{totalPages}";

        if (prevButton != null)
            prevButton.gameObject.SetActive(pageIndex > 0);
        if (nextButton != null)
            nextButton.gameObject.SetActive(pageIndex < totalPages - 1);
    }

    private int GetTotalPages()
    {
        IReadOnlyList<PersistentRosterUnitData> units = GetGraveyardUnits();
        int count = units != null ? units.Count : 0;
        return Mathf.Max(1, Mathf.CeilToInt(count / (float)Mathf.Max(1, cardsPerPage)));
    }

    private IReadOnlyList<PersistentRosterUnitData> GetGraveyardUnits()
    {
        if (profileController == null)
            profileController = UnityEngine.Object.FindFirstObjectByType<PersistentProfileController>();

        return profileController != null ? profileController.GetGraveyardUnits() : null;
    }

    private void EnsureCards()
    {
        int targetCount = Mathf.Max(1, cardsPerPage);
        if (cardSlots == null)
            cardSlots = new List<GraveyardUnitCardUI>(targetCount);

        for (int i = cardSlots.Count - 1; i >= 0; i--)
        {
            if (cardSlots[i] == null)
                cardSlots.RemoveAt(i);
        }

        if (cardPrefab == null || cardGridRoot == null)
            return;

        while (cardSlots.Count < targetCount)
        {
            GraveyardUnitCardUI card = Instantiate(cardPrefab, cardGridRoot);
            cardSlots.Add(card);
        }
    }

    private void SetOpen(bool open)
    {
        if (root != null)
            root.SetActive(open);
        else
            gameObject.SetActive(open);
    }

    private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null || action == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }
}
