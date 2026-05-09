using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleResultPopupUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;
    [SerializeField] private Image dimImage;

    [Header("Title")]
    [SerializeField] private TMP_Text titleText;

    [Header("Soul")]
    [SerializeField] private TMP_Text soulValueText;
    [SerializeField] private TMP_Text soulBonusText;

    [Header("Experience")]
    [SerializeField] private TMP_Text expValueText;
    [SerializeField] private TMP_Text defeatedEnemyCountText;

    [Header("Captured Prisoners")]
    [SerializeField] private GameObject capturedRoot;
    [SerializeField] private Image[] capturedPrisonerIcons = new Image[4];
    [SerializeField] private TMP_Text capturedCountText;

    [Header("Party Cards")]
    [SerializeField] private BattleResultPartyCardUI[] partyCards = new BattleResultPartyCardUI[4];
    [SerializeField, Min(0f)] private float expGaugeAnimationDuration = 1f;

    [Header("Close")]
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text closeButtonText;

    private Action onClose;
    private Coroutine animationRoutine;
    private bool initialized;
    private bool opening;

    private void Awake()
    {
        EnsureInitialized();

        // Open()이 비활성 Panel 오브젝트를 켜면서 Awake가 처음 실행될 수 있다.
        // 그 순간 CloseSilently()를 호출하면 콜백이 날아가 결과창이 먹통처럼 된다.
        if (!opening)
            CloseSilently();
    }

    private void EnsureInitialized()
    {
        if (initialized)
            return;

        initialized = true;

        if (root == null)
            root = gameObject;

        if (closeButton == null)
            closeButton = root != null ? root.GetComponentInChildren<Button>(true) : GetComponentInChildren<Button>(true);

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(HandleClose);
        }
        else
        {
            Debug.LogWarning("[BattleResultPopupUI] Close Button is not assigned.", this);
        }
    }

    public void Open(BattleResultPopupData data, Action closeAction)
    {
        opening = true;

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        EnsureInitialized();

        onClose = closeAction;

        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }

        if (titleText != null)
            titleText.text = data != null ? data.GetTitleOrDefault() : "전투 결과";

        if (soulValueText != null)
            soulValueText.text = data != null ? data.soulReward.ToString("N0") : "0";

        if (soulBonusText != null)
        {
            int bonus = data != null ? data.totalBonusPercent : 0;
            soulBonusText.text = bonus > 0 ? $"(+{bonus}% 월드 보너스)" : string.Empty;
            soulBonusText.gameObject.SetActive(bonus > 0);
        }

        if (expValueText != null)
            expValueText.text = data != null ? data.expRewardTotal.ToString("N0") : "0";

        if (defeatedEnemyCountText != null)
        {
            int count = data != null ? data.defeatedOrCapturedEnemyCount : 0;
            defeatedEnemyCountText.text = $"처치한 적 {count:N0}";
        }

        if (closeButtonText != null)
            closeButtonText.text = "전투완료";

        BindCapturedPrisoners(data);
        BindPartyCards(data);

        SetVisible(true);
        opening = false;

        if (isActiveAndEnabled && gameObject.activeInHierarchy)
        {
            animationRoutine = StartCoroutine(AnimateExperienceRoutine());
        }
        else
        {
            Debug.LogWarning("[BattleResultPopupUI] Popup GameObject is inactive after Open(). EXP animation was skipped.", this);
            UpdatePartyCardProgress(1f);
        }
    }

    public void CloseSilently()
    {
        opening = false;
        onClose = null;

        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }

        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        if (root != null)
            root.SetActive(visible);
        else
            gameObject.SetActive(visible);
    }

    private void BindCapturedPrisoners(BattleResultPopupData data)
    {
        int capturedCount = data != null && data.capturedPrisoners != null ? data.capturedPrisoners.Count : 0;
        bool hasCaptured = capturedCount > 0;

        if (capturedRoot != null)
            capturedRoot.SetActive(hasCaptured);

        if (capturedCountText != null)
        {
            capturedCountText.text = $"포획한 적 {capturedCount:N0}";
            capturedCountText.gameObject.SetActive(hasCaptured);
        }

        if (capturedPrisonerIcons == null)
            return;

        for (int i = 0; i < capturedPrisonerIcons.Length; i++)
        {
            Image image = capturedPrisonerIcons[i];
            if (image == null)
                continue;

            bool active = data != null && data.capturedPrisoners != null && i < data.capturedPrisoners.Count && i < 4;
            image.gameObject.SetActive(active);
            if (!active)
                continue;

            CapturedPrisonerRewardEntry reward = data.capturedPrisoners[i];
            Sprite icon = reward != null ? reward.GetIcon() : null;
            image.sprite = icon;
            image.enabled = icon != null;
        }
    }

    private void BindPartyCards(BattleResultPopupData data)
    {
        if (partyCards == null)
            return;

        for (int i = 0; i < partyCards.Length; i++)
        {
            BattleResultPartyCardUI card = partyCards[i];
            if (card == null)
                continue;

            BattleResultPartyMemberSnapshot snapshot = data != null && data.partyMembers != null && i < data.partyMembers.Count
                ? data.partyMembers[i]
                : null;

            card.Bind(snapshot);
        }
    }

    private IEnumerator AnimateExperienceRoutine()
    {
        float duration = Mathf.Max(0.01f, expGaugeAnimationDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            UpdatePartyCardProgress(t);
            yield return null;
        }

        UpdatePartyCardProgress(1f);
        animationRoutine = null;
    }

    private void UpdatePartyCardProgress(float t)
    {
        if (partyCards == null)
            return;

        for (int i = 0; i < partyCards.Length; i++)
        {
            if (partyCards[i] != null)
                partyCards[i].UpdateProgress(t);
        }
    }

    private void HandleClose()
    {
        Action callback = onClose;
        CloseSilently();
        callback?.Invoke();
    }
}
