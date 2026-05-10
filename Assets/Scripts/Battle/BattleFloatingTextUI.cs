using System.Collections;
using TMPro;
using UnityEngine;

public class BattleFloatingTextUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text text;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Defaults")]
    [SerializeField] private float defaultDuration = 1f;
    [SerializeField] private float defaultRiseDistance = 40f;

    private Coroutine playRoutine;

    private void Awake()
    {
        if (text == null)
            text = GetComponentInChildren<TMP_Text>(true);
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void Bind(TMP_Text boundText, CanvasGroup boundCanvasGroup)
    {
        text = boundText;
        canvasGroup = boundCanvasGroup;
    }

    public void Play(string message, Color color, float duration = -1f, float riseDistance = -1f)
    {
        if (playRoutine != null)
            StopCoroutine(playRoutine);

        playRoutine = StartCoroutine(PlayRoutine(
            message,
            color,
            duration > 0f ? duration : defaultDuration,
            riseDistance >= 0f ? riseDistance : defaultRiseDistance));
    }

    private IEnumerator PlayRoutine(string message, Color color, float duration, float riseDistance)
    {
        if (text != null)
        {
            text.text = message ?? string.Empty;
            text.color = color;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        RectTransform rect = GetComponent<RectTransform>();
        Vector2 start = rect != null ? rect.anchoredPosition : Vector2.zero;
        Vector2 end = start + Vector2.up * riseDistance;

        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (rect != null)
                rect.anchoredPosition = Vector2.Lerp(start, end, t);
            if (canvasGroup != null)
                canvasGroup.alpha = 1f - Mathf.SmoothStep(0f, 1f, t);
            yield return null;
        }

        Destroy(gameObject);
    }
}
