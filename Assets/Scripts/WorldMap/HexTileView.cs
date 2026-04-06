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
    [SerializeField] private Button button;

    [Header("Colors")]
    [SerializeField] private Color iconNormalColor = Color.white;
    [SerializeField] private Color iconDisabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    [SerializeField] private Color hiddenQuestionMarkColor = Color.white;

    private int tileId;
    private Action<int> clickHandler;

    public RectTransform RectTransform => transform as RectTransform;

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
        }

        if (auraImage != null)
        {
            auraImage.gameObject.SetActive(showAura);
            auraImage.sprite = auraSprite;
            auraImage.color = auraColor;
        }

        if (iconImage != null)
        {
            bool showIcon = revealed && iconSprite != null;
            iconImage.gameObject.SetActive(showIcon);
            iconImage.sprite = iconSprite;
            iconImage.color = disableIcon ? iconDisabledColor : iconNormalColor;
        }

        if (questionMarkRoot != null)
        {
            questionMarkRoot.SetActive(showQuestionMark);
            Graphic graphic = questionMarkRoot.GetComponent<Graphic>();
            if (graphic != null)
                graphic.color = hiddenQuestionMarkColor;
        }
    }

    private void HandleClick()
    {
        clickHandler?.Invoke(tileId);
    }
}
