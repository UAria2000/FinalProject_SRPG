using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BootstrapOptionButtonUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button button;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text labelText;

    [Header("Visuals")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite selectedSprite;
    [SerializeField] private Sprite disabledSprite;

    [SerializeField] private Color normalTextColor = Color.white;
    [SerializeField] private Color selectedTextColor = Color.white;
    [SerializeField] private Color disabledTextColor = Color.gray;

    [SerializeField] private Vector3 normalScale = Vector3.one;
    [SerializeField] private Vector3 selectedScale = Vector3.one;
    [SerializeField] private Vector3 disabledScale = Vector3.one;

    private bool isSelected;
    private bool isInteractable = true;

    public Button Button => button;

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        RefreshVisual();
    }

    public void SetInteractable(bool interactable)
    {
        isInteractable = interactable;

        if (button != null)
            button.interactable = interactable;

        RefreshVisual();
    }

    private void Reset()
    {
        button = GetComponentInChildren<Button>();
        if (button != null && backgroundImage == null)
            backgroundImage = button.GetComponent<Image>();
        if (button != null && labelText == null)
            labelText = button.GetComponentInChildren<TMP_Text>();
    }

    private void OnValidate()
    {
        RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (backgroundImage != null)
        {
            if (!isInteractable && disabledSprite != null)
                backgroundImage.sprite = disabledSprite;
            else if (isSelected && selectedSprite != null)
                backgroundImage.sprite = selectedSprite;
            else
                backgroundImage.sprite = normalSprite;
        }

        if (labelText != null)
        {
            if (!isInteractable)
                labelText.color = disabledTextColor;
            else if (isSelected)
                labelText.color = selectedTextColor;
            else
                labelText.color = normalTextColor;
        }

        Transform target = button != null ? button.transform : transform;
        if (target != null)
        {
            if (!isInteractable)
                target.localScale = disabledScale;
            else if (isSelected)
                target.localScale = selectedScale;
            else
                target.localScale = normalScale;
        }
    }
}