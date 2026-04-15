using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleTurnOrderPortraitUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image portraitImage;
    [SerializeField] private Image dimOverlayImage;
    [SerializeField] private TMP_Text orderText;

    private BattleTurnOrderStripUI owner;
    private BattleUnit unit;

    public void Bind(BattleTurnOrderStripUI panelOwner, BattleUnit targetUnit, int displayIndex, bool isCurrent, bool isFinished, bool isUpcoming)
    {
        owner = panelOwner;
        unit = targetUnit;

        bool hasUnit = targetUnit != null;
        gameObject.SetActive(hasUnit);
        if (!hasUnit)
            return;

        if (portraitImage != null)
        {
            portraitImage.sprite = targetUnit.SlotFaceSprite;
            portraitImage.color = Color.white;
        }

        if (orderText != null)
            orderText.text = (displayIndex + 1).ToString();

        if (dimOverlayImage != null)
        {
            if (isCurrent)
                dimOverlayImage.color = new Color(1f, 1f, 1f, 0f);
            else if (isFinished)
                dimOverlayImage.color = new Color(0f, 0f, 0f, 0.65f);
            else if (isUpcoming)
                dimOverlayImage.color = new Color(0f, 0f, 0f, 0.35f);
            else
                dimOverlayImage.color = new Color(0f, 0f, 0f, 0.2f);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || owner == null || unit == null)
            return;

        owner.HandlePortraitClicked(unit);
    }
}
