using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CurrentUnitInfoPanel : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text nameValueText;
    [SerializeField] private TMP_Text currentLevelValueText;
    [SerializeField] private TMP_Text originalLevelValueText;
    [SerializeField] private TMP_Text hpValueText;
    [SerializeField] private TMP_Text dmgValueText;
    [SerializeField] private TMP_Text spdValueText;
    [SerializeField] private TMP_Text hitValueText;
    [SerializeField] private TMP_Text acValueText;
    [SerializeField] private TMP_Text criValueText;
    [SerializeField] private TMP_Text crdValueText;
    [SerializeField] private TMP_Text poisonResistValueText;
    [SerializeField] private TMP_Text bleedResistValueText;
    [SerializeField] private TMP_Text stunResistValueText;
    [SerializeField] private TMP_Text epitaphText;

    public void Show(BattleUnit unit)
    {
        if (root != null)
            root.SetActive(true);

        if (unit == null)
        {
            Clear();
            return;
        }

        if (portraitImage != null)
        {
            portraitImage.sprite = unit.PortraitSprite;
            portraitImage.color = unit.PortraitSprite != null
                ? Color.white
                : new Color(1f, 1f, 1f, 0f);
        }

        UnitInstanceStatVariance variance = unit.GetVariance();

        if (nameValueText != null) nameValueText.text = unit.Name;
        if (currentLevelValueText != null) currentLevelValueText.text = unit.CurrentLevel.ToString();
        if (originalLevelValueText != null) originalLevelValueText.text = unit.OriginalLevel.ToString();
        if (hpValueText != null) hpValueText.text = string.Format("{0}/{1}", unit.CurrentHP, unit.MaxHP);
        if (dmgValueText != null) dmgValueText.text = BattleStatFormatter.FormatIntValueWithDelta(unit.DMG, variance.dmgDelta);
        if (spdValueText != null) spdValueText.text = BattleStatFormatter.FormatIntValueWithDelta(unit.SPD, variance.spdDelta);
        if (hitValueText != null) hitValueText.text = BattleStatFormatter.FormatScaledX10ValueWithDelta(unit.HIT, variance.hitDeltaX10);
        if (acValueText != null) acValueText.text = BattleStatFormatter.FormatScaledX10ValueWithDelta(unit.AC, variance.acDeltaX10);
        if (criValueText != null) criValueText.text = BattleStatFormatter.FormatIntValueWithDelta(unit.CRI, variance.criDelta);
        if (crdValueText != null) crdValueText.text = BattleStatFormatter.FormatIntValueWithDelta(unit.CRD, variance.crdDelta);
        if (poisonResistValueText != null) poisonResistValueText.text = BattleStatFormatter.FormatPercent(unit.PoisonResist);
        if (bleedResistValueText != null) bleedResistValueText.text = BattleStatFormatter.FormatPercent(unit.BleedResist);
        if (stunResistValueText != null) stunResistValueText.text = BattleStatFormatter.FormatPercent(unit.StunResist);
        if (epitaphText != null) epitaphText.text = string.IsNullOrEmpty(unit.Epitaph) ? "-" : unit.Epitaph;
    }

    private void Clear()
    {
        if (portraitImage != null)
        {
            portraitImage.sprite = null;
            portraitImage.color = new Color(1f, 1f, 1f, 0f);
        }

        if (nameValueText != null) nameValueText.text = "-";
        if (currentLevelValueText != null) currentLevelValueText.text = "-";
        if (originalLevelValueText != null) originalLevelValueText.text = "-";
        if (hpValueText != null) hpValueText.text = "-";
        if (dmgValueText != null) dmgValueText.text = "-";
        if (spdValueText != null) spdValueText.text = "-";
        if (hitValueText != null) hitValueText.text = "-";
        if (acValueText != null) acValueText.text = "-";
        if (criValueText != null) criValueText.text = "-";
        if (crdValueText != null) crdValueText.text = "-";
        if (poisonResistValueText != null) poisonResistValueText.text = "-";
        if (bleedResistValueText != null) bleedResistValueText.text = "-";
        if (stunResistValueText != null) stunResistValueText.text = "-";
        if (epitaphText != null) epitaphText.text = "-";
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
    }
}