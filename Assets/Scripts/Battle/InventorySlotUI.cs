using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private Image selectedOutline;

    private int boundIndex = -1;
    private BattleManager battleManager;

    public void Bind(BattleManager manager, int index, InventoryStackData stack, bool selected)
    {
        battleManager = manager;
        boundIndex = index;

        if (iconImage != null)
        {
            iconImage.sprite = stack != null && stack.item != null ? stack.item.icon : null;
            iconImage.color = stack != null && stack.item != null ? Color.white : new Color(1f, 1f, 1f, 0.2f);
        }

        if (amountText != null)
            amountText.text = stack != null ? stack.amount.ToString() : string.Empty;

        if (selectedOutline != null)
            selectedOutline.gameObject.SetActive(selected);

        if (button != null)
        {
            button.interactable = stack != null && stack.item != null && stack.amount > 0;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }
    }

    private void OnClick()
    {
        if (battleManager != null && boundIndex >= 0)
            battleManager.OnInventorySlotPressed(boundIndex);
    }
}
