using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public enum BattleActionWheelButtonFrameType
{
    Hex,
    Circle
}

public enum BattleActionWheelButtonDisabledVisualMode
{
    None,
    Cooldown,
    Unusable
}

public readonly struct BattleActionWheelButtonViewData
{
    public readonly string label;
    public readonly Sprite icon;
    public readonly bool visible;
    public readonly bool interactable;
    public readonly bool isEmpty;
    public readonly BattleActionWheelButtonFrameType frameType;
    public readonly UnityAction onClick;
    public readonly int cooldownRemaining;
    public readonly int cooldownTotal;
    public readonly string disabledReason;
    public readonly bool showUnusableDim;

    public BattleActionWheelButtonViewData(
        string label,
        Sprite icon,
        bool visible,
        bool interactable,
        bool isEmpty,
        BattleActionWheelButtonFrameType frameType,
        UnityAction onClick,
        int cooldownRemaining = 0,
        int cooldownTotal = 0,
        string disabledReason = null,
        bool showUnusableDim = false)
    {
        this.label = label;
        this.icon = icon;
        this.visible = visible;
        this.interactable = interactable;
        this.isEmpty = isEmpty;
        this.frameType = frameType;
        this.onClick = onClick;
        this.cooldownRemaining = Mathf.Max(0, cooldownRemaining);
        this.cooldownTotal = Mathf.Max(0, cooldownTotal);
        this.disabledReason = disabledReason;
        this.showUnusableDim = showUnusableDim;
    }

    public static BattleActionWheelButtonViewData Empty()
    {
        return new BattleActionWheelButtonViewData(
            string.Empty,
            null,
            true,
            false,
            true,
            BattleActionWheelButtonFrameType.Hex,
            null);
    }

    public static BattleActionWheelButtonViewData Hidden()
    {
        return new BattleActionWheelButtonViewData(
            string.Empty,
            null,
            false,
            false,
            true,
            BattleActionWheelButtonFrameType.Hex,
            null);
    }
}

/// <summary>
/// 액션휠의 공용 행동 버튼 1개를 표시한다.
/// 현재 권장 구조:
/// ButtonRoot
/// ├─ FrameImage
/// ├─ IconImage
/// ├─ Text (TMP)
/// ├─ CooldownDim      선택
/// ├─ CooldownText     선택
/// └─ UnusableDim      선택
/// </summary>
public class BattleActionWheelButtonUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button button;
    [Tooltip("현재 하이어라키의 FrameImage를 연결한다.")]
    [SerializeField] private Image frameImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text labelText;

    [Header("Frame Sprites - Optional")]
    [Tooltip("Frame Image에 적용할 기본 프레임 스프라이트. 비워두면 기존 스프라이트를 유지한다.")]
    [SerializeField] private Sprite normalFrameSprite;
    [Tooltip("빈 슬롯에 적용할 프레임 스프라이트. 비워두면 normalFrameSprite 또는 기존 스프라이트를 유지한다.")]
    [SerializeField] private Sprite emptyFrameSprite;
    [SerializeField] private bool hideFrameWhenEmpty = false;

    [Header("Cooldown Visual - Optional")]
    [Tooltip("쿨타임 중 켜질 Dim 이미지. Image Type이 Filled이면 fillAmount도 자동 갱신됩니다.")]
    [SerializeField] private Image cooldownDimImage;
    [SerializeField] private TMP_Text cooldownText;

    [Header("Unusable Visual - Optional")]
    [Tooltip("대상없음/위치불가/패시브/조건 불충족/사용 불가 등 쿨타임 외 사용 불가 상태에서 켜질 Dim 이미지.")]
    [SerializeField] private Image unusableDimImage;
    [Tooltip("사용 불가 사유를 텍스트로 표시하고 싶을 때만 연결합니다.")]
    [SerializeField] private TMP_Text disabledReasonText;

    [Header("State Roots - Optional")]
    [SerializeField] private GameObject lockedRoot;
    [SerializeField] private GameObject emptyRoot;
    [SerializeField] private GameObject contentRoot;

    [Header("Optional")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField, Range(0f, 1f)] private float disabledAlpha = 1f;

    public Button Button => button;
    public RectTransform RectTransform => transform as RectTransform;

    private void Reset()
    {
        AutoWireFromHierarchy();
    }

    private void Awake()
    {
        EnsureReferences();
        DisableChildRaycasts();
    }

    [ContextMenu("Auto Wire From Current Hierarchy")]
    public void AutoWireFromHierarchy()
    {
        button = GetComponent<Button>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        frameImage = FindImage("FrameImage");
        iconImage = FindImage("IconImage");
        labelText = FindText("Text");

        cooldownDimImage = FindImage("CooldownDim");
        cooldownText = FindText("CooldownText");
        unusableDimImage = FindImage("UnusableDim");
        disabledReasonText = FindText("DisabledReasonText");

        Transform locked = FindDeepChild(transform, "LockedRoot");
        if (locked != null)
            lockedRoot = locked.gameObject;

        Transform empty = FindDeepChild(transform, "EmptyRoot");
        if (empty != null)
            emptyRoot = empty.gameObject;

        Transform content = FindDeepChild(transform, "ContentRoot");
        if (content != null)
            contentRoot = content.gameObject;
    }

    public void Apply(BattleActionWheelButtonViewData data)
    {
        EnsureReferences();
        DisableChildRaycasts();

        if (button != null)
            button.onClick.RemoveAllListeners();

        SetVisible(data.visible);
        if (!data.visible)
            return;

        bool canClick = data.interactable && !data.isEmpty && data.onClick != null;
        if (button != null)
        {
            button.interactable = canClick;
            if (canClick)
                button.onClick.AddListener(data.onClick);
        }

        ApplyFrame(data);
        ApplyStateRoots(data, canClick);
        ApplyContent(data);
        ApplyDisabledVisuals(data);
    }

    public void ClearListeners()
    {
        if (button != null)
            button.onClick.RemoveAllListeners();
    }

    private void ApplyFrame(BattleActionWheelButtonViewData data)
    {
        if (frameImage == null)
            return;

        frameImage.gameObject.SetActive(!data.isEmpty || !hideFrameWhenEmpty);

        Sprite targetSprite = data.isEmpty && emptyFrameSprite != null
            ? emptyFrameSprite
            : normalFrameSprite;

        if (targetSprite != null)
            frameImage.sprite = targetSprite;
    }

    private void ApplyStateRoots(BattleActionWheelButtonViewData data, bool canClick)
    {
        if (emptyRoot != null)
            emptyRoot.SetActive(data.isEmpty);

        if (contentRoot != null)
            contentRoot.SetActive(!data.isEmpty);

        if (lockedRoot != null)
            lockedRoot.SetActive(!data.isEmpty && !canClick);

        if (canvasGroup != null)
            canvasGroup.alpha = !data.isEmpty && !canClick ? disabledAlpha : 1f;
    }

    private void ApplyContent(BattleActionWheelButtonViewData data)
    {
        if (iconImage != null)
        {
            bool showIcon = !data.isEmpty && data.icon != null;
            iconImage.gameObject.SetActive(showIcon);
            iconImage.sprite = showIcon ? data.icon : null;
        }

        if (labelText != null)
        {
            bool showLabel = !data.isEmpty && !string.IsNullOrWhiteSpace(data.label);
            labelText.gameObject.SetActive(showLabel);
            labelText.text = showLabel ? data.label : string.Empty;
        }
    }

    private void ApplyDisabledVisuals(BattleActionWheelButtonViewData data)
    {
        bool showCooldown = !data.isEmpty && data.cooldownRemaining > 0;
        bool showUnusable = !data.isEmpty && !showCooldown && data.showUnusableDim && !data.interactable;

        if (cooldownDimImage != null)
        {
            cooldownDimImage.gameObject.SetActive(showCooldown);
            if (showCooldown)
            {
                float total = Mathf.Max(1, data.cooldownTotal);
                cooldownDimImage.fillAmount = Mathf.Clamp01(data.cooldownRemaining / total);
            }
            else
            {
                cooldownDimImage.fillAmount = 0f;
            }
        }

        if (cooldownText != null)
        {
            cooldownText.gameObject.SetActive(showCooldown);
            cooldownText.text = showCooldown ? data.cooldownRemaining.ToString() : string.Empty;
        }

        if (unusableDimImage != null)
            unusableDimImage.gameObject.SetActive(showUnusable);

        if (disabledReasonText != null)
        {
            bool showReason = showUnusable && !string.IsNullOrWhiteSpace(data.disabledReason);
            disabledReasonText.gameObject.SetActive(showReason);
            disabledReasonText.text = showReason ? data.disabledReason : string.Empty;
        }
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? canvasGroup.alpha : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
            if (visible && canvasGroup.alpha <= 0f)
                canvasGroup.alpha = 1f;
        }
        else
        {
            gameObject.SetActive(visible);
        }
    }

    private void EnsureReferences()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (frameImage == null)
            frameImage = FindImage("FrameImage");

        if (iconImage == null)
            iconImage = FindImage("IconImage");

        if (labelText == null)
            labelText = FindText("Text");
    }

    private void DisableChildRaycasts()
    {
        if (frameImage != null)
            frameImage.raycastTarget = false;
        if (iconImage != null)
            iconImage.raycastTarget = false;
        if (cooldownDimImage != null)
            cooldownDimImage.raycastTarget = false;
        if (unusableDimImage != null)
            unusableDimImage.raycastTarget = false;
    }

    private Image FindImage(string childName)
    {
        Transform child = FindDeepChild(transform, childName);
        return child != null ? child.GetComponent<Image>() : null;
    }

    private TMP_Text FindText(string childName)
    {
        Transform child = FindDeepChild(transform, childName);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrWhiteSpace(childName))
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
                return child;

            Transform result = FindDeepChild(child, childName);
            if (result != null)
                return result;
        }

        return null;
    }
}
