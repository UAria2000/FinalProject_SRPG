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

    private RectTransform rectTransform;

    public BattleUnit Unit { get; private set; }

    public RectTransform HoverAnchor
    {
        get { return hoverAnchor != null ? hoverAnchor : rectTransform; }
    }

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
