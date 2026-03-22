using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyDetailPopupUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Portrait")]
    [SerializeField] private Image portraitImage;

    [Header("Basic")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text hpText;

    [Header("Stats")]
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

    [Header("Narrative")]
    [SerializeField] private TMP_Text epitaphText;

    private BattleUnit currentEnemy;
    private string currentEpitaph = "";

    public void Show(BattleUnit enemy, string epitaph)
    {
        currentEnemy = enemy;
        currentEpitaph = epitaph;

        if (root != null)
            root.SetActive(enemy != null);

        if (enemy == null)
            return;

        if (portraitImage != null)
        {
            portraitImage.sprite = enemy.PortraitSprite;
            portraitImage.color = enemy.PortraitSprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        }

        if (nameText != null) nameText.text = enemy.Name;
        if (levelText != null) levelText.text = enemy.Level.ToString();
        if (hpText != null) hpText.text = $"{enemy.CurrentHP}/{enemy.MaxHP}";

        if (dmgText != null) dmgText.text = enemy.DMG.ToString();
        if (spdText != null) spdText.text = enemy.SPD.ToString();
        if (hitText != null) hitText.text = Mathf.RoundToInt(enemy.HIT).ToString();
        if (acText != null) acText.text = Mathf.RoundToInt(enemy.AC).ToString();
        if (criText != null) criText.text = Mathf.RoundToInt(enemy.CRI).ToString();
        if (crdText != null) crdText.text = Mathf.RoundToInt(enemy.CRD).ToString();

        if (poisonResistText != null) poisonResistText.text = Mathf.RoundToInt(enemy.PoisonResist).ToString();
        if (bleedResistText != null) bleedResistText.text = Mathf.RoundToInt(enemy.BleedResist).ToString();
        if (stunResistText != null) stunResistText.text = Mathf.RoundToInt(enemy.StunResist).ToString();

        if (epitaphText != null) epitaphText.text = currentEpitaph;
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