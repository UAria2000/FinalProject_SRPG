using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillTooltipUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Texts")]
    [SerializeField] private TMP_Text skillNameText;
    [SerializeField] private TMP_Text usablePositionText;
    [SerializeField] private TMP_Text targetPositionText;
    [SerializeField] private TMP_Text damageText;
    [SerializeField] private TMP_Text accuracyText;
    [SerializeField] private TMP_Text cooldownText;

    [Header("Optional")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Vector2 screenOffset = new Vector2(120f, 50f);

    private RectTransform rectTransform;
    private Canvas parentCanvas;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
        Hide();
    }

    public void Show(SkillDefinition skill, Vector2 screenPosition)
    {
        if (skill == null)
        {
            Hide();
            return;
        }

        if (root != null)
            root.SetActive(true);
        else
            gameObject.SetActive(true);

        if (skillNameText != null)
            skillNameText.text = skill.skillName;

        if (usablePositionText != null)
            usablePositionText.text = $"사용 가능 위치: {skill.GetUsablePositionText()}";

        if (targetPositionText != null)
        {
            string targetTeamText = skill.targetTeam switch
            {
                SkillTargetTeam.Enemy => "적",
                SkillTargetTeam.Ally => "아군",
                SkillTargetTeam.Self => "자신",
                _ => "대상"
            };

            targetPositionText.text = $"선택 가능 대상: {targetTeamText} {skill.GetTargetPositionText()}";
        }

        if (damageText != null)
            damageText.text = $"데미지: {skill.GetDamageText()}";

        if (accuracyText != null)
            accuracyText.text = $"명중률: {skill.GetTooltipAccuracyValue()}";

        if (cooldownText != null)
            cooldownText.text = $"쿨타임: {skill.cooldownTurns}";

        if (iconImage != null)
        {
            iconImage.sprite = skill.icon;
            iconImage.color = skill.icon != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        }

        SetScreenPosition(screenPosition);
    }

    public void Move(Vector2 screenPosition)
    {
        SetScreenPosition(screenPosition);
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    private void SetScreenPosition(Vector2 screenPosition)
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        Vector2 finalScreenPos = screenPosition + screenOffset;

        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            Camera cam = parentCanvas.worldCamera;
            RectTransform canvasRect = parentCanvas.transform as RectTransform;

            if (canvasRect != null &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, finalScreenPos, cam, out Vector2 localPos))
            {
                rectTransform.localPosition = localPos;
            }
        }
        else
        {
            rectTransform.position = finalScreenPos;
        }
    }
}