using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleResultPartyCardUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;
    [SerializeField] private CanvasGroup canvasGroup;
    [Range(0f, 1f)] [SerializeField] private float deadAlpha = 0.55f;

    [Header("Portrait")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private GameObject deadRoot;

    [Header("Identity")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private GameObject nftBadgeRoot;
    [SerializeField] private Image nftBadgeImage;

    [Header("Class Badge")]
    [Tooltip("단일 Image 방식으로 클래스 배지를 표시할 때 사용합니다. 비워도 됩니다.")]
    [SerializeField] private GameObject classBadgeRoot;
    [Tooltip("Melee/Mid/Ranged 스프라이트를 교체해서 표시할 Image입니다. 비워도 됩니다.")]
    [SerializeField] private Image classBadgeImage;
    [SerializeField] private Sprite meleeClassBadgeSprite;
    [SerializeField] private Sprite midClassBadgeSprite;
    [SerializeField] private Sprite rangedClassBadgeSprite;
    [Tooltip("레기온 카드처럼 배지 오브젝트 3개를 따로 두는 방식입니다. 연결되어 있으면 해당 타입만 켜집니다.")]
    [SerializeField] private GameObject meleeClassBadgeRoot;
    [SerializeField] private GameObject midClassBadgeRoot;
    [SerializeField] private GameObject rangedClassBadgeRoot;

    [Header("Rank")]
    [SerializeField] private Image rankImage;
    [SerializeField] private Sprite[] rankSprites = new Sprite[9];

    [Header("Level")]
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private GameObject originalLevelRoot;
    [SerializeField] private TMP_Text originalLevelText;

    [Header("Experience")]
    [SerializeField] private TMP_Text expGainText;
    [Tooltip("현재 경험치 숫자만 표시합니다. 예: 6,260")]
    [SerializeField] private TMP_Text expCurrentValueText;
    [Tooltip("다음 레벨까지 필요한 최대 경험치 숫자만 표시합니다. 예: 8,400")]
    [SerializeField] private TMP_Text expMaxValueText;
    [Tooltip("구형 단일 텍스트 방식입니다. 비워도 됩니다. 연결되어 있으면 '현재 / 최대' 형식으로 표시합니다.")]
    [SerializeField] private TMP_Text expValueText;
    [SerializeField] private TMP_Text nextLevelText;
    [SerializeField] private Slider expSlider;
    [SerializeField] private Image expFillImage;

    private BattleResultPartyMemberSnapshot boundData;
    private float startNormalized;
    private float endNormalized;

    public void Bind(BattleResultPartyMemberSnapshot data)
    {
        boundData = data;
        bool hasData = data != null;

        if (root != null)
            root.SetActive(hasData);
        else
            gameObject.SetActive(hasData);

        if (!hasData)
            return;

        startNormalized = data.ExpBeforeNormalized;
        endNormalized = data.ExpAfterNormalized;

        if (canvasGroup != null)
            canvasGroup.alpha = data.isDead ? deadAlpha : 1f;

        if (deadRoot != null)
            deadRoot.SetActive(data.isDead);

        if (portraitImage != null)
        {
            Sprite portrait = data.GetPortraitSprite();
            portraitImage.sprite = portrait;
            portraitImage.enabled = portrait != null;
        }

        if (nameText != null)
            nameText.text = string.IsNullOrWhiteSpace(data.displayName) ? "-" : data.displayName;

        bool showNft = data.isNft || data.isExchangeable;
        if (nftBadgeRoot != null)
            nftBadgeRoot.SetActive(showNft);
        if (nftBadgeImage != null)
            nftBadgeImage.enabled = showNft && nftBadgeImage.sprite != null;

        ApplyClassBadge(data.unitDefinition != null ? data.unitDefinition.rangeType : CharacterRangeType.Melee);
        ApplyRank(data.promotionRank);

        if (levelText != null)
            levelText.text = $"Lv {Mathf.Max(1, data.levelAfter)}";

        bool showOriginal = data.originalLevel > 0 && data.levelAfter < data.originalLevel;
        if (originalLevelRoot != null)
            originalLevelRoot.SetActive(showOriginal);
        if (originalLevelText != null)
            originalLevelText.text = showOriginal ? $"({data.originalLevel})" : string.Empty;

        if (expGainText != null)
            expGainText.text = data.gainedExp > 0 ? $"+{data.gainedExp:N0} 경험치" : "+0 경험치";

        UpdateProgress(0f);
    }

    public void UpdateProgress(float normalizedTime)
    {
        if (boundData == null)
            return;

        float t = Mathf.Clamp01(normalizedTime);

        float value;
        int displayExp;
        int need;

        if (boundData.DidLevelUp)
        {
            if (t < 0.5f)
            {
                float firstT = Mathf.Clamp01(t / 0.5f);
                need = Mathf.Max(1, boundData.expToNextBefore);
                value = Mathf.Lerp(startNormalized, 1f, firstT);
                displayExp = Mathf.RoundToInt(Mathf.Lerp(boundData.expBefore, need, firstT));
            }
            else
            {
                float secondT = Mathf.Clamp01((t - 0.5f) / 0.5f);
                need = Mathf.Max(1, boundData.expToNextAfter);
                value = Mathf.Lerp(0f, endNormalized, secondT);
                displayExp = Mathf.RoundToInt(Mathf.Lerp(0f, boundData.expAfter, secondT));
            }
        }
        else
        {
            value = Mathf.Lerp(startNormalized, endNormalized, t);
            displayExp = Mathf.RoundToInt(Mathf.Lerp(boundData.expBefore, boundData.expAfter, t));
            need = Mathf.Max(1, boundData.expToNextAfter);
        }

        value = Mathf.Clamp01(value);

        if (expSlider != null)
        {
            expSlider.minValue = 0f;
            expSlider.maxValue = 1f;
            expSlider.value = value;
        }

        if (expFillImage != null)
            expFillImage.fillAmount = value;

        int clampedDisplayExp = Mathf.Clamp(displayExp, 0, need);

        if (expCurrentValueText != null)
            expCurrentValueText.text = $"{clampedDisplayExp:N0}";

        if (expMaxValueText != null)
            expMaxValueText.text = $"{need:N0}";

        if (expValueText != null)
            expValueText.text = $"{clampedDisplayExp:N0} / {need:N0}";

        if (nextLevelText != null)
            nextLevelText.text = $"다음 레벨까지 {Mathf.Max(0, need - clampedDisplayExp):N0}";
    }

    private void ApplyClassBadge(CharacterRangeType rangeType)
    {
        bool isMelee = rangeType == CharacterRangeType.Melee;
        bool isMid = rangeType == CharacterRangeType.Mid;
        bool isRanged = rangeType == CharacterRangeType.Ranged;

        if (meleeClassBadgeRoot != null) meleeClassBadgeRoot.SetActive(isMelee);
        if (midClassBadgeRoot != null) midClassBadgeRoot.SetActive(isMid);
        if (rangedClassBadgeRoot != null) rangedClassBadgeRoot.SetActive(isRanged);

        Sprite sprite = GetClassBadgeSprite(rangeType);
        bool showSingleImage = classBadgeImage != null && sprite != null;

        if (classBadgeRoot != null)
            classBadgeRoot.SetActive(showSingleImage);

        if (classBadgeImage != null)
        {
            classBadgeImage.sprite = sprite;
            classBadgeImage.enabled = sprite != null;
        }
    }

    private Sprite GetClassBadgeSprite(CharacterRangeType rangeType)
    {
        switch (rangeType)
        {
            case CharacterRangeType.Mid:
                return midClassBadgeSprite;
            case CharacterRangeType.Ranged:
                return rangedClassBadgeSprite;
            case CharacterRangeType.Melee:
            default:
                return meleeClassBadgeSprite;
        }
    }

    private void ApplyRank(int rank)
    {
        if (rankImage == null)
            return;

        int clamped = LegionFormula.ClampLegionRank(rank);
        Sprite sprite = null;
        int index = clamped - 1;
        if (rankSprites != null && index >= 0 && index < rankSprites.Length)
            sprite = rankSprites[index];

        rankImage.sprite = sprite;
        rankImage.enabled = sprite != null;
    }
}
