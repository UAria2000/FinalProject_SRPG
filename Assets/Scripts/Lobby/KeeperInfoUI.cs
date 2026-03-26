using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class KeeperInfoUI : MonoBehaviour
{
    public static KeeperInfoUI Instance;

    [Header("Top Info")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private TextMeshProUGUI statsText;

    [Header("Skills")]
    [SerializeField] private Image basicAttackIcon;
    [SerializeField] private Image skill1Icon;
    [SerializeField] private Image skill2Icon;
    [SerializeField] private Image skill3Icon;

    [Header("Equipment")]
    [SerializeField] private Image weaponIcon;
    [SerializeField] private Image armorIcon;

    [Header("Upgrade Info")]
    [SerializeField] private TextMeshProUGUI effectText;
    [SerializeField] private TextMeshProUGUI costText;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void UpdateDisplay(KeeperData data)
    {
        if (data == null) return;

        if (portraitImage != null)
        {
            portraitImage.sprite = data.portrait;
            portraitImage.gameObject.SetActive(true);
        }

        if (statsText != null) statsText.text = data.statsInfo;

        if (basicAttackIcon != null) basicAttackIcon.sprite = data.basicAttackIcon;
        if (skill1Icon != null) skill1Icon.sprite = data.skill1Icon;
        if (skill2Icon != null) skill2Icon.sprite = data.skill2Icon;
        if (skill3Icon != null) skill3Icon.sprite = data.skill3Icon;

        if (weaponIcon != null) weaponIcon.sprite = data.weaponIcon;
        if (armorIcon != null) armorIcon.sprite = data.armorIcon;

        if (effectText != null) effectText.text = $"{data.currentEffect} / {data.upgradeEffect}";
        if (costText != null) costText.text = $"{data.upgradeCost} 소울";
    }

    public void ClearDisplay()
    {
        // 모든 텍스트 및 이미지 요소를 초기화 상태로 되돌립니다.
        if (portraitImage != null) portraitImage.gameObject.SetActive(false);
        if (statsText != null) statsText.text = "";

        if (basicAttackIcon != null) basicAttackIcon.sprite = null;
        if (skill1Icon != null) skill1Icon.sprite = null;
        if (skill2Icon != null) skill2Icon.sprite = null;
        if (skill3Icon != null) skill3Icon.sprite = null;
        if (weaponIcon != null) weaponIcon.sprite = null;
        if (armorIcon != null) armorIcon.sprite = null;

        if (effectText != null) effectText.text = "";
        if (costText != null) costText.text = "";
    }
}