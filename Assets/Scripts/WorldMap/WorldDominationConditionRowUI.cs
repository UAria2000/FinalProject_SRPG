using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorldDominationConditionRowUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject root;
    [SerializeField] private Image checkboxImage;
    [SerializeField] private TMP_Text conditionText;

    [Header("Sprites")]
    [SerializeField] private Sprite uncheckedSprite;
    [SerializeField] private Sprite checkedSprite;

    public void Bind(bool visible, bool completed, string text)
    {
        GameObject targetRoot = root != null ? root : gameObject;
        targetRoot.SetActive(visible);

        if (!visible)
            return;

        if (checkboxImage != null)
            checkboxImage.sprite = completed ? checkedSprite : uncheckedSprite;

        if (conditionText != null)
            conditionText.text = text ?? string.Empty;
    }
}