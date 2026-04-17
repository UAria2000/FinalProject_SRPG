using System;
using UnityEngine;
using UnityEngine.UI;

public class HexTileView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image tileImage;
    [SerializeField] private Image auraImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject questionMarkRoot;
    [SerializeField] private Image questionMarkImage;
    [SerializeField] private Button button;

    [Header("Colors")]
    [SerializeField] private Color iconNormalColor = Color.white;
    [SerializeField] private Color iconDisabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    private int tileId;
    private Action<int> clickHandler;

    public RectTransform RectTransform => transform as RectTransform;

    private void Awake()
    {
        if (questionMarkImage == null && questionMarkRoot != null)
            questionMarkImage = questionMarkRoot.GetComponentInChildren<Image>(true);
    }

    public void Initialize(int inTileId, Action<int> onClick)
    {
        tileId = inTileId;
        clickHandler = onClick;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
        }
    }

    public void SetVisual(
        Sprite tileSprite,
        Color tileFallbackColor,
        Sprite iconSprite,
        bool revealed,
        Sprite questionMarkSprite,
        bool showQuestionMark,
        bool showAura,
        Sprite auraSprite,
        Color auraColor,
        bool disableIcon)
    {
        if (tileImage != null)
        {
            tileImage.sprite = tileSprite;
            tileImage.color = tileSprite != null ? Color.white : tileFallbackColor;
            tileImage.preserveAspect = true;
        }

        if (auraImage != null)
        {
            auraImage.gameObject.SetActive(showAura && auraSprite != null);
            auraImage.sprite = auraSprite;
            auraImage.color = auraColor;
            auraImage.preserveAspect = true;
        }

        bool shouldShowQuestion = showQuestionMark && questionMarkSprite != null;
        if (questionMarkRoot != null)
            questionMarkRoot.SetActive(shouldShowQuestion);
        if (questionMarkImage != null)
        {
            questionMarkImage.gameObject.SetActive(shouldShowQuestion);
            questionMarkImage.sprite = questionMarkSprite;
            questionMarkImage.color = Color.white;
            questionMarkImage.preserveAspect = true;
        }

        if (iconImage != null)
        {
            bool showIcon = revealed && !shouldShowQuestion && iconSprite != null;
            iconImage.gameObject.SetActive(showIcon);
            iconImage.sprite = iconSprite;
            iconImage.color = disableIcon ? iconDisabledColor : iconNormalColor;
            iconImage.preserveAspect = true;
        }
    }

    private void HandleClick()
    {
        clickHandler?.Invoke(tileId);
    }
}
