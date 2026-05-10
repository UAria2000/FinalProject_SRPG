using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorldEventPopupUI : MonoBehaviour
{
    private enum PopupRewardMode
    {
        None = 0,
        Treasure = 1,
    }

    [Header("Root")]
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private TMP_Text confirmButtonText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button closeButton;

    [Header("Dim Close")]
    [Tooltip("패널 뒤에 깔리는 딤 오브젝트. 연결하면 팝업 열림/닫힘과 함께 활성화됩니다.")]
    [SerializeField] private GameObject dimRoot;
    [Tooltip("딤 클릭 닫기용 버튼. 비워두면 dimRoot의 Button을 찾고, 없으면 런타임에 추가합니다.")]
    [SerializeField] private Button dimCloseButton;
    [SerializeField] private bool closeOnDimClick = true;

    [Header("Optional Reward Slots")]
    [Tooltip("보물 이벤트 보상을 슬롯으로 보여줄 때 연결한다. 비워두면 기존처럼 확인 시 전부 자동 지급된다.")]
    [SerializeField] private List<WorldEventRewardSlotUI> rewardSlots = new List<WorldEventRewardSlotUI>(4);
    [SerializeField] private GameObject rewardSlotsRoot;
    [SerializeField] private StorageItemTooltipUI itemTooltipUI;
    [SerializeField] private BottomPartySummaryPanelUI bottomPartySummaryPanelUI;

    private Action confirmAction;
    private Action closeAction;

    private PopupRewardMode rewardMode = PopupRewardMode.None;
    private WorldRunManager rewardRunManager;
    private WorldTreasureResult activeTreasure;
    private bool autoGrantTreasureWhenNoRewardSlots;

    public bool HasUsableRewardSlots => rewardSlots != null && rewardSlots.Count > 0;
    public bool IsOpen => root != null ? root.activeInHierarchy : gameObject.activeInHierarchy;

    private void Awake()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(HandleConfirmClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(HandleCloseClicked);
        }

        InitializeDimCloseButton();

        if (bottomPartySummaryPanelUI == null)
            bottomPartySummaryPanelUI = UnityEngine.Object.FindFirstObjectByType<BottomPartySummaryPanelUI>();

        CloseSilently();
    }

    public void Open(string title, string body, string confirmLabel, Action onConfirm, Action onClose = null)
    {
        ClearRewardMode();
        confirmAction = onConfirm;
        closeAction = onClose;

        ApplyTexts(title, body, confirmLabel);
        SetOpen(true);
    }

    public void OpenTreasure(
        string title,
        string body,
        string confirmLabel,
        WorldTreasureResult treasure,
        WorldRunManager runManager,
        Action onConfirmAfterClose,
        Action onClose = null)
    {
        ClearRewardMode();

        rewardMode = PopupRewardMode.Treasure;
        activeTreasure = treasure;
        rewardRunManager = runManager;
        autoGrantTreasureWhenNoRewardSlots = !HasUsableRewardSlots;

        confirmAction = onConfirmAfterClose;
        closeAction = onClose;

        ApplyTexts(title, body, confirmLabel);
        RefreshTreasureRewardSlots();
        SetOpen(true);
    }

    public void CloseSilently()
    {
        confirmAction = null;
        closeAction = null;
        ClearRewardMode();
        SetOpen(false);
    }

    public void HandleRewardSlotClicked(int rewardIndex)
    {
        if (!TryGetTreasureReward(rewardIndex, out WorldTreasureRewardItemEntry reward))
            return;

        if (bottomPartySummaryPanelUI != null && bottomPartySummaryPanelUI.CanUseAsDirectLoadoutItem(reward.item))
        {
            bool handled = bottomPartySummaryPanelUI.TrySelectExternalLoadoutItem(
                this,
                rewardIndex,
                reward.item,
                Mathf.Max(1, reward.amount),
                () => ConsumeTreasureRewardAt(rewardIndex));

            if (handled)
            {
                RefreshTreasureRewardSlots();
                return;
            }
        }

        if (rewardRunManager == null || !rewardRunManager.AddStorageItem(reward.item, Mathf.Max(1, reward.amount)))
            return;

        ConsumeTreasureRewardAt(rewardIndex);
    }

    public bool HandleRewardSlotDragBegin(int rewardIndex)
    {
        if (!TryGetTreasureReward(rewardIndex, out WorldTreasureRewardItemEntry reward))
            return false;

        if (bottomPartySummaryPanelUI == null || !bottomPartySummaryPanelUI.CanUseAsDirectLoadoutItem(reward.item))
            return false;

        bool started = bottomPartySummaryPanelUI.TryBeginExternalLoadoutItemDrag(
            this,
            rewardIndex,
            reward.item,
            Mathf.Max(1, reward.amount),
            () => ConsumeTreasureRewardAt(rewardIndex));

        if (started)
            RefreshTreasureRewardSlots();

        return started;
    }

    public void HandleRewardSlotDragEnd(int rewardIndex)
    {
        if (bottomPartySummaryPanelUI == null)
            return;

        bottomPartySummaryPanelUI.EndExternalLoadoutItemDrag(this, rewardIndex);
        RefreshTreasureRewardSlots();
    }

    public void HandleRewardSlotHoverEnter(ItemDefinition item)
    {
        if (itemTooltipUI == null || item == null)
            return;

        itemTooltipUI.Show(item, false, 0);
    }

    public void HandleRewardSlotHoverExit()
    {
        if (itemTooltipUI == null)
            return;

        itemTooltipUI.Hide();
    }

    private void ApplyTexts(string title, string body, string confirmLabel)
    {
        if (titleText != null)
            titleText.text = title;

        if (bodyText != null)
            bodyText.text = body;

        if (confirmButtonText != null)
            confirmButtonText.text = string.IsNullOrWhiteSpace(confirmLabel) ? "확인" : confirmLabel;
    }

    private void SetOpen(bool open)
    {
        if (dimRoot != null)
            dimRoot.SetActive(open);

        if (dimCloseButton != null)
            dimCloseButton.interactable = open && closeOnDimClick;

        if (root != null)
            root.SetActive(open);
        else
            gameObject.SetActive(open);

        if (bottomPartySummaryPanelUI != null)
            bottomPartySummaryPanelUI.SetExternalMainPanelOpen(open && rewardMode == PopupRewardMode.Treasure);
    }

    private void InitializeDimCloseButton()
    {
        if (dimCloseButton == null && dimRoot != null)
            dimCloseButton = dimRoot.GetComponent<Button>();

        if (dimCloseButton == null && dimRoot != null)
            dimCloseButton = dimRoot.AddComponent<Button>();

        if (dimCloseButton == null)
            return;

        dimCloseButton.onClick.RemoveAllListeners();
        dimCloseButton.onClick.AddListener(HandleDimClicked);
    }

    private void HandleDimClicked()
    {
        if (!closeOnDimClick)
            return;

        HandleCloseClicked();
    }

    private void HandleConfirmClicked()
    {
        if (rewardMode == PopupRewardMode.Treasure && autoGrantTreasureWhenNoRewardSlots && activeTreasure != null && rewardRunManager != null)
            rewardRunManager.GrantTreasureRewards(activeTreasure);

        Action action = confirmAction;
        CloseSilently();
        action?.Invoke();
    }

    private void HandleCloseClicked()
    {
        // 보물 슬롯에 남아 있는 미수령 아이템은 창을 닫는 순간 폐기된다.
        Action action = closeAction;
        CloseSilently();
        action?.Invoke();
    }

    private bool TryGetTreasureReward(int rewardIndex, out WorldTreasureRewardItemEntry reward)
    {
        reward = null;

        if (rewardMode != PopupRewardMode.Treasure || activeTreasure == null)
            return false;

        if (activeTreasure.rewards == null || rewardIndex < 0 || rewardIndex >= activeTreasure.rewards.Count)
            return false;

        reward = activeTreasure.rewards[rewardIndex];
        return reward != null && reward.item != null && reward.amount > 0;
    }

    private bool ConsumeTreasureRewardAt(int rewardIndex)
    {
        if (!TryGetTreasureReward(rewardIndex, out WorldTreasureRewardItemEntry reward))
            return false;

        reward.item = null;
        reward.amount = 0;
        RefreshTreasureRewardSlots();
        return true;
    }

    private void RefreshTreasureRewardSlots()
    {
        bool showSlots = rewardMode == PopupRewardMode.Treasure && HasUsableRewardSlots;

        if (rewardSlotsRoot != null)
            rewardSlotsRoot.SetActive(showSlots);

        if (rewardSlots == null)
            return;

        for (int i = 0; i < rewardSlots.Count; i++)
        {
            WorldEventRewardSlotUI slot = rewardSlots[i];
            if (slot == null)
                continue;

            ItemDefinition item = null;
            int amount = 0;

            if (activeTreasure != null && activeTreasure.rewards != null && i < activeTreasure.rewards.Count)
            {
                WorldTreasureRewardItemEntry reward = activeTreasure.rewards[i];
                if (reward != null)
                {
                    item = reward.item;
                    amount = reward.amount;
                }
            }

            bool selected = bottomPartySummaryPanelUI != null && bottomPartySummaryPanelUI.IsExternalLoadoutItemPending(this, i);
            slot.Bind(this, i, item, amount, canInteract: showSlots && item != null && amount > 0, selected: selected);
        }
    }

    private void ClearRewardMode()
    {
        HandleRewardSlotHoverExit();

        if (bottomPartySummaryPanelUI != null)
            bottomPartySummaryPanelUI.CancelExternalLoadoutItemSource(this);

        rewardMode = PopupRewardMode.None;
        rewardRunManager = null;
        activeTreasure = null;
        autoGrantTreasureWhenNoRewardSlots = false;

        if (rewardSlotsRoot != null)
            rewardSlotsRoot.SetActive(false);

        if (rewardSlots != null)
        {
            for (int i = 0; i < rewardSlots.Count; i++)
                rewardSlots[i]?.Clear();
        }
    }
}
