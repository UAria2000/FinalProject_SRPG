using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GraveyardUnitCardUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Portrait")]
    [SerializeField] private Image portraitImage;

    [Header("Texts")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;

    private void Awake()
    {
        if (root == null)
            root = gameObject;
    }

    public void Bind(PersistentRosterUnitData unit)
    {
        if (root == null)
            root = gameObject;

        bool hasUnit = unit != null;
        root.SetActive(hasUnit);
        if (!hasUnit)
            return;

        Sprite portrait = unit.unitViewDefinition != null
            ? unit.unitViewDefinition.GetBustPortraitSprite(true)
            : null;

        if (portraitImage != null)
        {
            portraitImage.sprite = portrait;
            portraitImage.enabled = portrait != null;
        }

        if (nameText != null)
            nameText.text = unit.GetDisplayName();

        if (levelText != null)
            levelText.text = $"Lv.{Mathf.Max(1, unit.currentLevel)}";
    }

    public void Clear()
    {
        if (root == null)
            root = gameObject;

        root.SetActive(false);
    }
}
