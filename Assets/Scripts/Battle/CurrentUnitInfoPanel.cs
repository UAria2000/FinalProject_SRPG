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
        // 전투 중에는 패널 틀은 항상 보이게 유지
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
        if (epitaphText != null) epitaphText.text = unit.Epitaph;
    }

    private void Clear()
    {
        if (portraitImage != null)
        {
            portraitImage.sprite = null;
            portraitImage.color = new Color(1f, 1f, 1f, 0f);
        }

        if (nameValueText != null) nameValueText.text = string.Empty;
        if (currentLevelValueText != null) currentLevelValueText.text = string.Empty;
        if (originalLevelValueText != null) originalLevelValueText.text = string.Empty;
        if (hpValueText != null) hpValueText.text = string.Empty;
        if (dmgValueText != null) dmgValueText.text = string.Empty;
        if (spdValueText != null) spdValueText.text = string.Empty;
        if (hitValueText != null) hitValueText.text = string.Empty;
        if (acValueText != null) acValueText.text = string.Empty;
        if (criValueText != null) criValueText.text = string.Empty;
        if (crdValueText != null) crdValueText.text = string.Empty;
        if (poisonResistValueText != null) poisonResistValueText.text = string.Empty;
        if (bleedResistValueText != null) bleedResistValueText.text = string.Empty;
        if (stunResistValueText != null) stunResistValueText.text = string.Empty;
        if (epitaphText != null) epitaphText.text = string.Empty;
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
    }
}