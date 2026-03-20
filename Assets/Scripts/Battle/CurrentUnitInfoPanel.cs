using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CurrentUnitInfoPanel : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panelRoot;

    [Header("Basic Info")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;

    [Header("Main Stats")]
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text dmgText;
    [SerializeField] private TMP_Text spdText;
    [SerializeField] private TMP_Text hitText;
    [SerializeField] private TMP_Text acText;
    [SerializeField] private TMP_Text criText;
    [SerializeField] private TMP_Text crdText;

    [Header("Resistances")]
    [SerializeField] private TMP_Text poisonResistText;
    [SerializeField] private TMP_Text bleedResistText;
    [SerializeField] private TMP_Text stunResistText;

    [Header("Optional Actions")]
    [SerializeField] private GameObject basicAttackRoot;
    [SerializeField] private GameObject skillListRoot;
    [SerializeField] private TMP_Text basicAttackText;
    [SerializeField] private TMP_Text skillListText;

    public void Show(BattleUnit unit)
    {
        if (unit == null)
        {
            Hide();
            return;
        }

        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (portraitImage != null)
        {
            portraitImage.sprite = unit.PortraitSprite;
            portraitImage.color = unit.PortraitSprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        }

        if (nameText != null)
            nameText.text = unit.Name;

        if (levelText != null)
            levelText.text = unit.Level.ToString();

        // 숫자만 표시
        if (hpText != null)
            hpText.text = $"{unit.CurrentHP}/{unit.MaxHP}";

        if (dmgText != null)
            dmgText.text = unit.DMG.ToString();

        if (spdText != null)
            spdText.text = unit.SPD.ToString();

        // HIT / AC는 UI에서 10배 정수 표기
        if (hitText != null)
            hitText.text = Mathf.RoundToInt(unit.HIT * 10f).ToString();

        if (acText != null)
            acText.text = Mathf.RoundToInt(unit.AC * 10f).ToString();

        if (criText != null)
            criText.text = Mathf.RoundToInt(unit.CRI).ToString();

        if (crdText != null)
            crdText.text = Mathf.RoundToInt(unit.CRD).ToString();

        if (poisonResistText != null)
            poisonResistText.text = Mathf.RoundToInt(unit.PoisonResist).ToString();

        if (bleedResistText != null)
            bleedResistText.text = Mathf.RoundToInt(unit.BleedResist).ToString();

        if (stunResistText != null)
            stunResistText.text = Mathf.RoundToInt(unit.StunResist).ToString();

        // 평타 / 스킬은 보류
        if (basicAttackRoot != null)
            basicAttackRoot.SetActive(false);

        if (skillListRoot != null)
            skillListRoot.SetActive(false);

        if (basicAttackText != null)
            basicAttackText.text = "";

        if (skillListText != null)
            skillListText.text = "";
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }
}