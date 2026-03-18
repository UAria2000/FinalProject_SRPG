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

    private RectTransform rectTransform;

    public BattleUnit Unit { get; private set; }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void Initialize(BattleUnit unit, string label, Color color)
    {
        Unit = unit;

        if (unitBodyImage != null)
        {
            if (unit.Definition.sprite != null)
            {
                unitBodyImage.sprite = unit.Definition.sprite;
                unitBodyImage.color = Color.white;
            }
            else
            {
                unitBodyImage.color = color;
            }
        }

        if (labelText != null)
            labelText.text = label;

        RefreshHPInstant();

        gameObject.name = $"View_{label}_{unit.Name}";
    }

    public void SetPositionInstant(Vector3 worldPosition)
    {
        rectTransform.position = worldPosition;
    }

    public IEnumerator MoveToPosition(Vector3 targetPosition, float duration)
    {
        Vector3 start = rectTransform.position;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            t = Mathf.SmoothStep(0f, 1f, t);

            rectTransform.position = Vector3.Lerp(start, targetPosition, t);
            yield return null;
        }

        rectTransform.position = targetPosition;
    }

    public IEnumerator PlayAttackMove(Vector3 targetPosition, float moveRatio, float maxDistance, float moveDuration)
    {
        Vector3 start = rectTransform.position;
        Vector3 toTarget = targetPosition - start;

        float distanceToTarget = toTarget.magnitude;
        if (distanceToTarget <= 0.01f)
            yield break;

        Vector3 dir = toTarget.normalized;

        float dashDistance = distanceToTarget * moveRatio;
        dashDistance = Mathf.Min(dashDistance, maxDistance);

        Vector3 attackPoint = start + dir * dashDistance;

        float goDuration = moveDuration * 0.4f;
        float backDuration = moveDuration * 0.6f;

        yield return StartCoroutine(MoveRoutine(start, attackPoint, goDuration));
        yield return StartCoroutine(MoveRoutine(attackPoint, start, backDuration));
    }

    public void RefreshHPInstant()
    {
        if (Unit == null || hpFillImage == null)
            return;

        float ratio = 0f;

        if (Unit.GetMaxHP() > 0)
            ratio = (float)Unit.CurrentHP / Unit.GetMaxHP();

        ratio = Mathf.Clamp01(ratio);
        hpFillImage.fillAmount = ratio;
    }

    public IEnumerator AnimateHPChange(float duration = 0.2f)
    {
        if (Unit == null || hpFillImage == null)
            yield break;

        float start = hpFillImage.fillAmount;
        float target = 0f;

        if (Unit.GetMaxHP() > 0)
            target = (float)Unit.CurrentHP / Unit.GetMaxHP();

        target = Mathf.Clamp01(target);

        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            t = Mathf.SmoothStep(0f, 1f, t);

            hpFillImage.fillAmount = Mathf.Lerp(start, target, t);
            yield return null;
        }

        hpFillImage.fillAmount = target;
    }

    private IEnumerator MoveRoutine(Vector3 from, Vector3 to, float duration)
    {
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            t = Mathf.SmoothStep(0f, 1f, t);

            rectTransform.position = Vector3.Lerp(from, to, t);
            yield return null;
        }

        rectTransform.position = to;
    }
}