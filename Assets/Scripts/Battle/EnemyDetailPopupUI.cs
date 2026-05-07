using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class EnemyDetailPopupUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text nameValueText;
    [SerializeField] private TMP_Text levelValueText;
    [SerializeField] private TMP_Text hpValueText;
    [SerializeField] private TMP_Text dmgValueText;
    [SerializeField] private TMP_Text spdValueText;
    [SerializeField] private TMP_Text hitValueText;
    [SerializeField] private TMP_Text acValueText;
    [SerializeField] private TMP_Text idtValueText;
    [SerializeField] private TMP_Text criValueText;
    [SerializeField] private TMP_Text crdValueText;
    [FormerlySerializedAs("poisonResistValueText")]
    [SerializeField] private TMP_Text burnResistValueText;
    [SerializeField] private TMP_Text bleedResistValueText;
    [SerializeField] private TMP_Text stunResistValueText;
    [SerializeField] private TMP_Text frostResistValueText;
    [SerializeField] private TMP_Text blindResistValueText;
    [SerializeField] private TMP_Text epitaphText;

    public void Show(BattleUnit enemy)
    {
        if (root != null)
            root.SetActive(enemy != null);

        if (enemy == null)
            return;

        if (portraitImage != null)
        {
            portraitImage.sprite = enemy.BustPortraitSprite;
            portraitImage.color = enemy.BustPortraitSprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        }

        if (nameValueText != null) nameValueText.text = enemy.Name;
        if (levelValueText != null) levelValueText.text = enemy.CurrentLevel.ToString();
        if (hpValueText != null) hpValueText.text = string.Format("{0}/{1}", enemy.CurrentHP, enemy.MaxHP);
        if (dmgValueText != null) dmgValueText.text = enemy.DMG.ToString();
        if (spdValueText != null) spdValueText.text = enemy.SPD.ToString();
        if (hitValueText != null) hitValueText.text = Mathf.RoundToInt(enemy.HIT).ToString();
        if (acValueText != null) acValueText.text = Mathf.RoundToInt(enemy.AC).ToString();
        if (idtValueText != null) idtValueText.text = BattleStatFormatter.FormatPercent(enemy.IDT);
        if (criValueText != null) criValueText.text = BattleStatFormatter.FormatPercent(enemy.CRI);
        if (crdValueText != null) crdValueText.text = enemy.CRD.ToString();
        if (stunResistValueText != null) stunResistValueText.text = BattleStatFormatter.FormatPercent(enemy.StunResist);
        if (bleedResistValueText != null) bleedResistValueText.text = BattleStatFormatter.FormatPercent(enemy.BleedResist);
        if (burnResistValueText != null) burnResistValueText.text = BattleStatFormatter.FormatPercent(enemy.BurnResist);
        if (frostResistValueText != null) frostResistValueText.text = BattleStatFormatter.FormatPercent(enemy.FrostResist);
        if (blindResistValueText != null) blindResistValueText.text = BattleStatFormatter.FormatPercent(enemy.BlindResist);
        if (epitaphText != null) epitaphText.text = enemy.HasBattleInfoLastWill ? enemy.BattleInfoLastWillText : enemy.Epitaph;
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
    }

    public bool IsOpen()
    {
        return root != null && root.activeSelf;
    }
}
