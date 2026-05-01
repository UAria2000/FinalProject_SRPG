using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleUnitView : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private Image unitBodyImage;
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private Image hpFillImage;
    [SerializeField] private BattleStatusIconBarUI statusIconBar;

    [Header("Markers")]
    [SerializeField] private GameObject turnMark;
    [SerializeField] private GameObject targetMark;
    [SerializeField] private Image highlightImage;
    [SerializeField] private RectTransform hoverAnchor;

    [Header("Click Area")]
    [Tooltip("유닛 클릭/호버/대상 선택을 담당할 투명 클릭 영역입니다. 프리팹의 ClickableArea를 연결하세요.")]
    [SerializeField] private RectTransform clickableArea;
    [Tooltip("ClickableArea에 붙은 Image/Graphic입니다. 비워두면 자동으로 찾거나 투명 Image를 추가합니다.")]
    [SerializeField] private Graphic clickableAreaGraphic;
    [Tooltip("켜면 ClickableArea를 제외한 하위 Graphic의 Raycast Target을 자동으로 끕니다. 상태 아이콘/HP바가 클릭을 가로막는 것을 방지합니다.")]
    [SerializeField] private bool disableChildGraphicRaycastsExceptClickableArea = true;

    [Header("Selection Rings")]
    [SerializeField] private GameObject activeRingRoot;
    [SerializeField] private GameObject infoSelectedRingRoot;

    [Header("Deprecated - Not Used")]
    [Tooltip("폐기 예정. 더 이상 턴 예정/완료 표시로 사용하지 않습니다.")]
    [SerializeField] private Image upcomingGrayOverlayImage;
    [Tooltip("폐기 예정. 더 이상 턴 완료 표시로 사용하지 않습니다. 턴 완료 표시는 dead battle sprite로 대체됩니다.")]
    [SerializeField] private Image finishedGrayOverlayImage;

    private RectTransform rectTransform;
    private bool currentlyUsingFinishedSprite;

    public BattleUnit Unit { get; private set; }
    public RectTransform HoverAnchor => hoverAnchor != null ? hoverAnchor : rectTransform;
    public RectTransform ClickableArea => clickableArea;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void Initialize(BattleUnit unit, string label)
    {
        Unit = unit;
        currentlyUsingFinishedSprite = false;

        if (labelText != null)
            labelText.text = label;

        DisableDeprecatedOverlays();
        SetTurnMark(false);
        SetTargetMark(false);
        SetHighlighted(false);
        SetActionOwnerRing(false);
        SetInfoSelectedRing(false);
        SetFinishedTurnVisual(false);
        RefreshHPInstant();
        RefreshStatusIcons();
    }

    public void ConfigureClickHandling(BattleInputController inputController)
    {
        // Root에도 핸들러를 붙여 둔다. 다른 자식 Graphic이 Raycast를 먹어도 부모로 이벤트가 전달된다.
        BattleClickable rootClickable = GetComponent<BattleClickable>();
        if (rootClickable == null)
            rootClickable = gameObject.AddComponent<BattleClickable>();
        rootClickable.Initialize(this, inputController);

        RectTransform targetArea = clickableArea != null ? clickableArea : rectTransform;
        if (targetArea == null)
            return;

        BattleClickable areaClickable = targetArea.GetComponent<BattleClickable>();
        if (areaClickable == null)
            areaClickable = targetArea.gameObject.AddComponent<BattleClickable>();
        areaClickable.Initialize(this, inputController);

        EnsureClickableAreaGraphic(targetArea);

        if (disableChildGraphicRaycastsExceptClickableArea)
            DisableChildGraphicRaycastsExcept(targetArea);
    }

    private void EnsureClickableAreaGraphic(RectTransform targetArea)
    {
        if (targetArea == null)
            return;

        if (clickableAreaGraphic == null)
            clickableAreaGraphic = targetArea.GetComponent<Graphic>();

        if (clickableAreaGraphic == null)
        {
            Image image = targetArea.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = true;
            clickableAreaGraphic = image;
        }

        clickableAreaGraphic.raycastTarget = true;
        if (clickableAreaGraphic is Image clickableImage)
        {
            Color c = clickableImage.color;
            c.a = 0f;
            clickableImage.color = c;
        }
    }

    private void DisableChildGraphicRaycastsExcept(RectTransform exceptionRoot)
    {
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic g = graphics[i];
            if (g == null)
                continue;

            if (exceptionRoot != null && (g.transform == exceptionRoot || g.transform.IsChildOf(exceptionRoot)))
                continue;

            g.raycastTarget = false;
        }
    }

    public void RefreshHPInstant()
    {
        if (hpFillImage != null && Unit != null)
        {
            float ratio = Unit.MaxHP > 0 ? (float)Unit.CurrentHP / Unit.MaxHP : 0f;
            hpFillImage.fillAmount = Mathf.Clamp01(ratio);
        }

        RefreshStatusIcons();
    }

    public IEnumerator AnimateHPChange(float duration)
    {
        if (hpFillImage == null || Unit == null)
        {
            RefreshStatusIcons();
            yield break;
        }

        float start = hpFillImage.fillAmount;
        float target = Unit.MaxHP > 0 ? Mathf.Clamp01((float)Unit.CurrentHP / Unit.MaxHP) : 0f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            hpFillImage.fillAmount = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        hpFillImage.fillAmount = target;
        RefreshStatusIcons();
    }

    public void RefreshStatusIcons()
    {
        if (statusIconBar != null)
            statusIconBar.Refresh(Unit);
    }

    public void RefreshBattleVisualState(bool isCurrentActionOwner, bool isInfoSelected, bool isFinishedThisRound)
    {
        SetActionOwnerRing(isCurrentActionOwner);
        SetInfoSelectedRing(isInfoSelected);
        SetFinishedTurnVisual(isFinishedThisRound && !isCurrentActionOwner);
        RefreshHPInstant();
    }

    public void SetTurnMark(bool active)
    {
        if (turnMark != null)
            turnMark.SetActive(active);
    }

    public void SetTargetMark(bool active)
    {
        if (targetMark != null)
            targetMark.SetActive(active);
    }

    public void SetHighlighted(bool active)
    {
        if (highlightImage != null)
            highlightImage.gameObject.SetActive(active);
    }

    public void SetActionOwnerRing(bool active)
    {
        if (activeRingRoot != null)
            activeRingRoot.SetActive(active);
    }

    public void SetInfoSelectedRing(bool active)
    {
        if (infoSelectedRingRoot != null)
            infoSelectedRingRoot.SetActive(active);
    }

    /// <summary>
    /// 기존 코드 호환용. Upcoming/Finished 회색 오버레이는 더 이상 사용하지 않고,
    /// finished 값이 true일 때 dead battle sprite로 교체한다.
    /// </summary>
    public void SetRoundStateOverlay(bool upcoming, bool finished)
    {
        DisableDeprecatedOverlays();
        SetFinishedTurnVisual(finished);
    }

    public void SetFinishedTurnVisual(bool finished)
    {
        if (currentlyUsingFinishedSprite == finished && unitBodyImage != null && unitBodyImage.sprite != null)
            return;

        currentlyUsingFinishedSprite = finished;
        ApplyBodySprite(finished || (Unit != null && Unit.IsDead));
    }

    private void ApplyBodySprite(bool useDeadBattleSprite)
    {
        if (unitBodyImage == null)
            return;

        Sprite sprite = null;
        if (Unit != null && Unit.ViewDefinition != null)
            sprite = Unit.ViewDefinition.GetBattleSprite(useDeadBattleSprite);
        if (sprite == null && Unit != null)
            sprite = Unit.BattleSprite;

        unitBodyImage.sprite = sprite;
        unitBodyImage.color = sprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        unitBodyImage.preserveAspect = true;
        unitBodyImage.raycastTarget = false;
    }

    private void DisableDeprecatedOverlays()
    {
        if (upcomingGrayOverlayImage != null)
            upcomingGrayOverlayImage.gameObject.SetActive(false);
        if (finishedGrayOverlayImage != null)
            finishedGrayOverlayImage.gameObject.SetActive(false);
    }

    public void SetPositionInstant(Vector3 worldPosition)
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();
        rectTransform.position = worldPosition;
    }

    public IEnumerator MoveToPosition(Vector3 worldPosition, float duration)
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        Vector3 start = rectTransform.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            rectTransform.position = Vector3.Lerp(start, worldPosition, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        rectTransform.position = worldPosition;
    }

    public IEnumerator PlayAttackMove(Vector3 targetWorldPosition, float moveRatio, float maxDistance, float duration)
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        Vector3 originalPos = rectTransform.position;
        Vector3 dir = targetWorldPosition - originalPos;
        float distance = dir.magnitude;
        if (distance > 0.001f) dir.Normalize();
        float moveDistance = Mathf.Min(distance * moveRatio, maxDistance);
        Vector3 attackPos = originalPos + dir * moveDistance;

        float half = duration * 0.5f;
        float elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            rectTransform.position = Vector3.Lerp(originalPos, attackPos, Mathf.Clamp01(elapsed / half));
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            rectTransform.position = Vector3.Lerp(attackPos, originalPos, Mathf.Clamp01(elapsed / half));
            yield return null;
        }

        rectTransform.position = originalPos;
    }
}
