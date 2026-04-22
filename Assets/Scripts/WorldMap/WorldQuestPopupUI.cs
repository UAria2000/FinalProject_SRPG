using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum WorldQuestPopupMode
{
    None = 0,
    Offer = 1,
    Active = 2,
    Completed = 3
}

public class WorldQuestPopupUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;
    [SerializeField] private Button outsideCloseButton;

    [Header("Texts")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text progressText;

    [Header("Soul Reward")]
    [SerializeField] private GameObject soulRewardRoot;
    [SerializeField] private Image soulIconImage;
    [SerializeField] private TMP_Text soulRewardText;

    [Header("Experience Reward")]
    [SerializeField] private GameObject experienceRewardRoot;
    [SerializeField] private Image experienceIconImage;
    [SerializeField] private TMP_Text experienceRewardText;

    [Header("Reward Slots")]
    [SerializeField] private List<WorldQuestPopupRewardSlotUI> rewardSlots = new List<WorldQuestPopupRewardSlotUI>(4);

    [Header("Buttons")]
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button rejectButton;
    [SerializeField] private Button cancelQuestButton;
    [SerializeField] private Button claimAllButton;
    [SerializeField] private Button closeButton;

    [Header("Tooltip")]
    [SerializeField] private StorageItemTooltipUI itemTooltipUI;

    private WorldQuestController owner;
    private WorldQuestState currentQuest;
    private WorldQuestPopupMode currentMode = WorldQuestPopupMode.None;
    private bool canAcceptCurrentQuest;
    private bool allowOutsideClose;

    public bool IsOpen => root != null && root.activeSelf;
    public WorldQuestState CurrentQuest => currentQuest;
    public WorldQuestPopupMode CurrentMode => currentMode;

    private void Awake()
    {
        if (acceptButton != null)
        {
            acceptButton.onClick.RemoveAllListeners();
            acceptButton.onClick.AddListener(HandleAcceptClicked);
        }

        if (rejectButton != null)
        {
            rejectButton.onClick.RemoveAllListeners();
            rejectButton.onClick.AddListener(HandleRejectClicked);
        }

        if (cancelQuestButton != null)
        {
            cancelQuestButton.onClick.RemoveAllListeners();
            cancelQuestButton.onClick.AddListener(HandleCancelQuestClicked);
        }

        if (claimAllButton != null)
        {
            claimAllButton.onClick.RemoveAllListeners();
            claimAllButton.onClick.AddListener(HandleClaimAllClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(HandleCloseClicked);
        }

        if (outsideCloseButton != null)
        {
            outsideCloseButton.onClick.RemoveAllListeners();
            outsideCloseButton.onClick.AddListener(HandleOutsideClicked);
        }

        Hide();
    }

    public void Initialize(WorldQuestController controller)
    {
        owner = controller;
    }

    public void ShowOffer(WorldQuestState quest, bool canAccept)
    {
        currentQuest = quest;
        currentMode = WorldQuestPopupMode.Offer;
        canAcceptCurrentQuest = canAccept;
        allowOutsideClose = false;

        if (root != null)
            root.SetActive(true);

        RefreshCommonTexts(quest);
        RefreshImmediateRewards(quest);
        RefreshRewardSlots(quest, false);
        RefreshButtons();
    }

    public void ShowActive(WorldQuestState quest)
    {
        currentQuest = quest;
        currentMode = WorldQuestPopupMode.Active;
        canAcceptCurrentQuest = false;
        allowOutsideClose = true;

        if (root != null)
            root.SetActive(true);

        RefreshCommonTexts(quest);
        RefreshImmediateRewards(quest);
        RefreshRewardSlots(quest, false);
        RefreshButtons();
    }

    public void ShowCompleted(WorldQuestState quest)
    {
        currentQuest = quest;
        currentMode = WorldQuestPopupMode.Completed;
        canAcceptCurrentQuest = false;
        allowOutsideClose = false;

        if (root != null)
            root.SetActive(true);

        RefreshCommonTexts(quest);
        RefreshImmediateRewards(quest);
        RefreshRewardSlots(quest, true);
        RefreshButtons();
    }

    public void RefreshCurrent()
    {
        if (currentQuest == null)
            return;

        switch (currentMode)
        {
            case WorldQuestPopupMode.Offer:
                ShowOffer(currentQuest, canAcceptCurrentQuest);
                break;

            case WorldQuestPopupMode.Active:
                ShowActive(currentQuest);
                break;

            case WorldQuestPopupMode.Completed:
                ShowCompleted(currentQuest);
                break;
        }
    }

    public void Hide()
    {
        currentQuest = null;
        currentMode = WorldQuestPopupMode.None;
        canAcceptCurrentQuest = false;
        allowOutsideClose = false;

        if (root != null)
            root.SetActive(false);

        HandleRewardSlotHoverExit();
    }

    public void HandleRewardSlotClicked(WorldQuestState quest, int rewardIndex)
    {
        owner?.ClaimRewardAt(quest, rewardIndex);
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

    private void RefreshCommonTexts(WorldQuestState quest)
    {
        if (quest == null || quest.definition == null)
            return;

        if (titleText != null)
            titleText.text = string.IsNullOrEmpty(quest.definition.displayName)
                ? quest.GetProgressText()
                : quest.definition.displayName;

        if (descriptionText != null)
            descriptionText.text = quest.GetDetailDescription();

        if (progressText != null)
            progressText.text = quest.GetProgressText();
    }

    private void RefreshImmediateRewards(WorldQuestState quest)
    {
        if (quest == null || quest.definition == null)
            return;

        bool showSoul = quest.definition.soulReward > 0;
        bool showExp = quest.definition.experienceReward > 0;

        if (soulRewardRoot != null)
            soulRewardRoot.SetActive(showSoul);

        if (soulRewardText != null)
            soulRewardText.text = showSoul ? quest.definition.soulReward.ToString() : string.Empty;

        if (soulIconImage != null)
            soulIconImage.gameObject.SetActive(showSoul);

        if (experienceRewardRoot != null)
            experienceRewardRoot.SetActive(showExp);

        if (experienceRewardText != null)
            experienceRewardText.text = showExp ? quest.definition.experienceReward.ToString() : string.Empty;

        if (experienceIconImage != null)
            experienceIconImage.gameObject.SetActive(showExp);
    }

    private void RefreshRewardSlots(WorldQuestState quest, bool completedMode)
    {
        for (int i = 0; i < rewardSlots.Count; i++)
        {
            WorldQuestPopupRewardSlotUI slot = rewardSlots[i];
            if (slot == null)
                continue;

            ItemDefinition item = null;
            int amount = 0;
            bool showLocked = false;
            bool canClick = false;

            if (quest != null && quest.definition != null && quest.definition.itemRewards != null && i < quest.definition.itemRewards.Count)
            {
                WorldQuestRewardItemEntry reward = quest.definition.itemRewards[i];
                if (reward != null)
                {
                    item = reward.item;
                    amount = Mathf.Max(1, reward.amount);
                }

                if (item != null)
                {
                    if (completedMode)
                    {
                        showLocked = !quest.CanClaimItemAt(i);
                        canClick = quest.CanClaimItemAt(i);
                    }
                    else
                    {
                        showLocked = true;
                        canClick = false;
                    }
                }
            }

            slot.Bind(this, quest, i, item, amount, showLocked, canClick);
        }
    }

    private void RefreshButtons()
    {
        bool offer = currentMode == WorldQuestPopupMode.Offer;
        bool active = currentMode == WorldQuestPopupMode.Active;
        bool completed = currentMode == WorldQuestPopupMode.Completed;

        if (acceptButton != null)
        {
            acceptButton.gameObject.SetActive(offer);
            acceptButton.interactable = canAcceptCurrentQuest;
        }

        if (rejectButton != null)
            rejectButton.gameObject.SetActive(offer);

        if (cancelQuestButton != null)
            cancelQuestButton.gameObject.SetActive(active);

        if (claimAllButton != null)
            claimAllButton.gameObject.SetActive(completed);

        if (closeButton != null)
            closeButton.gameObject.SetActive(active || completed);

        if (outsideCloseButton != null)
            outsideCloseButton.gameObject.SetActive(active && allowOutsideClose);
    }

    private void HandleAcceptClicked()
    {
        owner?.AcceptCurrentPopupQuest();
    }

    private void HandleRejectClicked()
    {
        owner?.RejectCurrentPopupQuest();
    }

    private void HandleCancelQuestClicked()
    {
        owner?.RequestAbandonFromCurrentPopup();
    }

    private void HandleClaimAllClicked()
    {
        owner?.ClaimAllRewardsForCurrentQuest();
    }

    private void HandleCloseClicked()
    {
        owner?.CloseCurrentPopup();
    }

    private void HandleOutsideClicked()
    {
        if (!allowOutsideClose)
            return;

        owner?.CloseCurrentPopupFromOutsideClick();
    }
}