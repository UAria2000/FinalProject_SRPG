using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleUnitView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image unitBodyImage;
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private Image hpFillImage;
    [SerializeField] private GameObject currentTurnMarker;
    [SerializeField] private GameObject targetMarker;
    [SerializeField] private Image highlightImage;

    private RectTransform rectTransform;

    public BattleUnit Unit { get; private set; }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void Initialize(BattleUnit unit, string label)
    {
        Unit = unit;

        if (unitBodyImage != null)
        {
            if (unit != null && unit.BodySprite != null)
            {
                unitBodyImage.sprite = unit.BodySprite;
                unitBodyImage.color = Color.white;
                unitBodyImage.preserveAspect = true;
            }
            else
            {
                unitBodyImage.sprite = null;
                unitBodyImage.color = Color.white;
            }
        }

        if (labelText != null)
            labelText.text = label;

        SetCurrentTurnMarker(false);
        SetTargetMarker(false);
        SetHighlighted(false);
        RefreshHPInstant();

        gameObject.name = unit != null
            ? $"View_{label}_{unit.Name}"
            : $"View_{label}_Null";
    }

    public void RefreshHPInstant()
    {
        if (hpFillImage == null || Unit == null)
            return;

        float ratio = 0f;
        if (Unit.MaxHP > 0)
            ratio = (float)Unit.CurrentHP / Unit.MaxHP;

        hpFillImage.fillAmount = Mathf.Clamp01(ratio);
    }

    public IEnumerator AnimateHPChange(float duration)
    {
        if (hpFillImage == null || Unit == null)
            yield break;

        float start = hpFillImage.fillAmount;

        float target = 0f;
        if (Unit.MaxHP > 0)
            target = (float)Unit.CurrentHP / Unit.MaxHP;

        target = Mathf.Clamp01(target);

        if (duration <= 0f)
        {
            hpFillImage.fillAmount = target;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            hpFillImage.fillAmount = Mathf.Lerp(start, target, t);
            yield return null;
        }

        hpFillImage.fillAmount = target;
    }

    public void SetCurrentTurnMarker(bool active)
    {
        if (currentTurnMarker != null)
            currentTurnMarker.SetActive(active);
    }

    public void SetTargetMarker(bool active)
    {
        if (targetMarker != null)
            targetMarker.SetActive(active);
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

        if (duration <= 0f)
        {
            rectTransform.position = worldPosition;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            rectTransform.position = Vector3.Lerp(start, worldPosition, t);
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

        if (distance > 0.001f)
            dir.Normalize();
        else
            dir = Vector3.zero;

        float moveDistance = Mathf.Min(distance * moveRatio, maxDistance);
        Vector3 attackPos = originalPos + dir * moveDistance;

        float halfDuration = duration * 0.5f;
        if (halfDuration <= 0f)
        {
            rectTransform.position = originalPos;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            rectTransform.position = Vector3.Lerp(originalPos, attackPos, t);
            yield return null;
        }

        rectTransform.position = attackPos;

        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            rectTransform.position = Vector3.Lerp(attackPos, originalPos, t);
            yield return null;
        }

        rectTransform.position = originalPos;
    }
}