using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleUnitView : MonoBehaviour
{
    [SerializeField] private Image unitBodyImage;
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private Image hpFillImage;
    [SerializeField] private GameObject turnMark;
    [SerializeField] private GameObject targetMark;
    [SerializeField] private Image highlightImage;
    [SerializeField] private RectTransform hoverAnchor;

    [Header("Optional New Overlays")]
    [SerializeField] private GameObject activeRingRoot;
    [SerializeField] private GameObject infoSelectedRingRoot;
    [SerializeField] private Image upcomingGrayOverlayImage;
    [SerializeField] private Image finishedGrayOverlayImage;

    private RectTransform rectTransform;

    public BattleUnit Unit { get; private set; }
    public RectTransform HoverAnchor => hoverAnchor != null ? hoverAnchor : rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void Initialize(BattleUnit unit, string label)
    {
        Unit = unit;

        if (unitBodyImage != null)
        {
            unitBodyImage.sprite = unit != null ? unit.BodySprite : null;
            unitBodyImage.color = unitBodyImage.sprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
            unitBodyImage.preserveAspect = true;
        }

        if (labelText != null)
            labelText.text = label;

        SetTurnMark(false);
        SetTargetMark(false);
        SetHighlighted(false);
        SetActionOwnerRing(false);
        SetInfoSelectedRing(false);
        SetRoundStateOverlay(false, false);
        RefreshHPInstant();
    }

    public void RefreshHPInstant()
    {
        if (hpFillImage == null || Unit == null)
            return;

        float ratio = Unit.MaxHP > 0 ? (float)Unit.CurrentHP / Unit.MaxHP : 0f;
        hpFillImage.fillAmount = Mathf.Clamp01(ratio);
    }

    public IEnumerator AnimateHPChange(float duration)
    {
        if (hpFillImage == null || Unit == null)
            yield break;

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

    public void SetRoundStateOverlay(bool upcoming, bool finished)
    {
        if (upcomingGrayOverlayImage != null)
            upcomingGrayOverlayImage.gameObject.SetActive(upcoming);
        if (finishedGrayOverlayImage != null)
            finishedGrayOverlayImage.gameObject.SetActive(finished);
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
