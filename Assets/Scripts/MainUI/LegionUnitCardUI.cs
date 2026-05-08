using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class LegionUnitCardUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Roots")]
    [SerializeField] private GameObject contentRoot;

    [Header("Input")]
    [Tooltip("카드 전체 클릭을 받는 투명 버튼. 비워두면 이 오브젝트의 IPointerClickHandler가 예비 클릭 영역으로 동작한다.")]
    [SerializeField] private Button cardClickButton;

    [Header("Frame")]
    [SerializeField] private Image frameImage;
    [SerializeField] private Image selectedGoldFrameImage;
    [SerializeField] private Image multiSelectSelectedFrameImage;
    [Tooltip("레기온에서 파티 편성 후보로 선택된 카드 표시. 비워도 동작한다.")]
    [SerializeField] private Image partyPendingFrameImage;

    [Header("Portrait")]
    [SerializeField] private Image fullbodyImage;

    [Header("Rank")]
    [SerializeField] private Image rankImage;
    [SerializeField] private Sprite[] rankSprites; // 1~9

    [Header("Badges")]
    [SerializeField] private GameObject exchangeableBadge;
    [SerializeField] private GameObject inPartyRoot;

    [Header("Favorite")]
    [SerializeField] private Button favoriteButton;
    [SerializeField] private GameObject favoriteOnRoot;
    [SerializeField] private GameObject favoriteOffRoot;
    [SerializeField] private GameObject blockFavoriteOverlay;

    [Header("Range")]
    [SerializeField] private GameObject meleeIcon;
    [SerializeField] private GameObject midIcon;
    [SerializeField] private GameObject rangedIcon;

    [Header("Texts")]
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text nameText;

    [Header("EXP")]
    [SerializeField] private Image expFillImage;
    [SerializeField] private Slider expSlider;
    [SerializeField] private TMP_Text expPercentText;

    [Header("HP")]
    [SerializeField] private Image hpIconImage;
    [SerializeField] private Sprite hpAliveIconSprite;
    [SerializeField] private Sprite hpDeadIconSprite;
    [SerializeField] private Image hpFillImage;
    [SerializeField] private TMP_Text hpText;

    private LegionPanelUI owner;
    private CanvasGroup canvasGroup;
    private PersistentRosterUnitData boundUnit;
    private bool isInParty;
    private bool isCurrentSelected;
    private bool isSelectedForDecompose;
    private bool isDecomposeMode;
    private bool isPartyDragInProgress;

    public PersistentRosterUnitData BoundUnit => boundUnit;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        if (cardClickButton != null)
        {
            cardClickButton.onClick.RemoveAllListeners();

            LegionCardClickAreaUI proxy = cardClickButton.GetComponent<LegionCardClickAreaUI>();
            if (proxy == null)
                proxy = cardClickButton.gameObject.AddComponent<LegionCardClickAreaUI>();
            proxy.Bind(this);
        }

        if (favoriteButton != null)
        {
            favoriteButton.onClick.RemoveAllListeners();
            favoriteButton.onClick.AddListener(HandleFavoriteClicked);
        }
    }

    private void OnDisable()
    {
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = true;

        // 패널이 닫히거나 페이지 갱신 중 카드가 비활성화될 때 OnDisable이 호출된다.
        // 이 경로에서 RefreshAll()까지 다시 호출하면 Unity가 자식 GameObject를
        // 비활성화하는 도중 Bind()->SetActive()가 재진입해서
        // "GameObject is already being activated or deactivated" 예외가 난다.
        if (isPartyDragInProgress)
        {
            isPartyDragInProgress = false;
            UIDragGhostUI.HideGhost();
            owner?.EndUnitCardPartyDrag(this, false);
        }
    }

    public void Bind(
        LegionPanelUI ownerPanel,
        PersistentRosterUnitData unit,
        bool inParty,
        bool currentSelected,
        bool selectedForDecompose,
        bool decomposeMode)
    {
        owner = ownerPanel;
        boundUnit = unit;
        isInParty = inParty;
        isCurrentSelected = currentSelected;
        isSelectedForDecompose = selectedForDecompose;
        isDecomposeMode = decomposeMode;

        bool hasUnit = boundUnit != null;

        if (contentRoot != null)
            contentRoot.SetActive(hasUnit);

        gameObject.SetActive(hasUnit);

        if (cardClickButton != null)
            cardClickButton.interactable = hasUnit;

        if (!hasUnit)
            return;

        RefreshFrame();
        RefreshPortrait();
        RefreshRank();
        RefreshFavorite();
        RefreshBadges();
        RefreshRange();
        RefreshTexts();
        RefreshHp();
        RefreshExp();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (cardClickButton != null)
            return;

        HandleCardInputClicked(eventData);
    }

    public void HandleCardInputClicked(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
            return;

        HandleCardClicked();
    }

    public void BeginCardInputDrag(PointerEventData eventData)
    {
        OnBeginDrag(eventData);
    }

    public void HandleCardInputDrag(PointerEventData eventData)
    {
    }

    public void EndCardInputDrag(PointerEventData eventData)
    {
        OnEndDrag(eventData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (boundUnit == null || owner == null || isDecomposeMode)
            return;

        if (!owner.CanBeginUnitCardPartyDrag(boundUnit))
            return;

        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = false;

        isPartyDragInProgress = true;
        owner.BeginUnitCardPartyDrag(this);

        Sprite ghostSprite = fullbodyImage != null ? fullbodyImage.sprite : null;
        RectTransform source = fullbodyImage != null ? fullbodyImage.rectTransform : transform as RectTransform;
        if (ghostSprite != null)
            UIDragGhostUI.Show(ghostSprite, source);
    }

    public void OnDrag(PointerEventData eventData)
    {
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = true;

        if (!isPartyDragInProgress)
            return;

        isPartyDragInProgress = false;
        UIDragGhostUI.HideGhost();
        owner?.EndUnitCardPartyDrag(this, true);
    }

    private void HandleCardClicked()
    {
        if (boundUnit == null || owner == null)
            return;

        if (isDecomposeMode && IsForbiddenForDecompose(boundUnit))
            return;

        owner.HandleUnitCardClicked(this);
    }

    private void HandleFavoriteClicked()
    {
        if (boundUnit == null || owner == null)
            return;

        if (isDecomposeMode)
            return;

        owner.HandleCardFavoriteClicked(this);
    }

    private void RefreshFrame()
    {
        if (selectedGoldFrameImage != null)
            selectedGoldFrameImage.gameObject.SetActive(isCurrentSelected);

        if (multiSelectSelectedFrameImage != null)
            multiSelectSelectedFrameImage.gameObject.SetActive(isDecomposeMode && isSelectedForDecompose);

        if (partyPendingFrameImage != null)
            partyPendingFrameImage.gameObject.SetActive(!isDecomposeMode && owner != null && owner.IsPendingPartyCandidate(boundUnit));

        if (inPartyRoot != null)
            inPartyRoot.SetActive(isInParty);
    }

    private void RefreshPortrait()
    {
        if (fullbodyImage == null)
            return;

        bool isDead = IsDead(boundUnit);
        Sprite sprite = ResolveFullbodySprite(boundUnit, isDead);
        fullbodyImage.sprite = sprite;
        fullbodyImage.enabled = sprite != null;
    }

    private void RefreshRank()
    {
        if (rankImage == null)
            return;

        int rank = boundUnit != null ? boundUnit.GetLegionRank() : 0;

        if (rank <= 0 || rankSprites == null || rankSprites.Length < rank || rankSprites[rank - 1] == null)
        {
            rankImage.gameObject.SetActive(false);
            return;
        }

        rankImage.gameObject.SetActive(true);
        rankImage.sprite = rankSprites[rank - 1];
    }

    private void RefreshFavorite()
    {
        bool favorite = boundUnit != null && boundUnit.isFavorite;

        if (favoriteOnRoot != null)
            favoriteOnRoot.SetActive(favorite);

        if (favoriteOffRoot != null)
            favoriteOffRoot.SetActive(!favorite);

        if (favoriteButton != null)
            favoriteButton.interactable = !isDecomposeMode;

        if (blockFavoriteOverlay != null)
            blockFavoriteOverlay.SetActive(isDecomposeMode);
    }

    private void RefreshBadges()
    {
        if (exchangeableBadge != null)
            exchangeableBadge.SetActive(boundUnit != null && boundUnit.IsNftUnit());
    }

    private void RefreshRange()
    {
        CharacterRangeType range = boundUnit != null && boundUnit.unitDefinition != null
            ? boundUnit.unitDefinition.rangeType
            : CharacterRangeType.Melee;

        if (meleeIcon != null) meleeIcon.SetActive(range == CharacterRangeType.Melee);
        if (midIcon != null) midIcon.SetActive(range == CharacterRangeType.Mid);
        if (rangedIcon != null) rangedIcon.SetActive(range == CharacterRangeType.Ranged);
    }

    private void RefreshTexts()
    {
        if (boundUnit == null)
            return;

        if (levelText != null)
            levelText.text = $"Lv.{Mathf.Max(1, boundUnit.currentLevel)}";

        if (nameText != null)
            nameText.text = boundUnit.GetDisplayName();
    }

    private void RefreshHp()
    {
        if (boundUnit == null)
            return;

        int maxHp = GetMaxHp(boundUnit);
        int currentHp = boundUnit.persistentCurrentHP < 0
            ? maxHp
            : Mathf.Clamp(boundUnit.persistentCurrentHP, 0, maxHp);

        bool isDead = currentHp <= 0;

        if (hpFillImage != null)
        {
            float fill = maxHp <= 0 ? 0f : currentHp / (float)maxHp;
            hpFillImage.fillAmount = fill;
        }

        if (hpText != null)
        {
            hpText.gameObject.SetActive(isCurrentSelected);
            if (isCurrentSelected)
                hpText.text = $"{currentHp}/{maxHp}";
        }

        if (hpIconImage != null)
        {
            if (isDead && hpDeadIconSprite != null)
                hpIconImage.sprite = hpDeadIconSprite;
            else if (!isDead && hpAliveIconSprite != null)
                hpIconImage.sprite = hpAliveIconSprite;
        }
    }


    private void RefreshExp()
    {
        if (boundUnit == null)
            return;

        int need = LegionFormula.GetExpToNextLevel(Mathf.Max(1, boundUnit.currentLevel));
        int current = Mathf.Clamp(boundUnit.currentExp, 0, need);
        float ratio = need > 0 ? Mathf.Clamp01(current / (float)need) : 0f;

        if (expFillImage != null)
            expFillImage.fillAmount = ratio;

        if (expSlider != null)
        {
            expSlider.minValue = 0f;
            expSlider.maxValue = 1f;
            expSlider.value = ratio;
        }

        if (expPercentText != null)
            expPercentText.text = $"{Mathf.RoundToInt(ratio * 100f)}%";
    }

        private bool IsForbiddenForDecompose(PersistentRosterUnitData unit)
    {
        if (unit == null)
            return true;

        if (!unit.CanDefinitionBeDecomposed())
            return true;

        if (unit.isFavorite)
            return true;

        if (isInParty)
            return true;

        if (GetOptionalBool(unit.unitDefinition, "isMainPlayerCharacter", "isMainCharacter"))
            return true;

        return false;
    }

    private bool IsDead(PersistentRosterUnitData unit)
    {
        if (unit == null)
            return false;

        int maxHp = GetMaxHp(unit);
        int currentHp = unit.persistentCurrentHP < 0
            ? maxHp
            : Mathf.Clamp(unit.persistentCurrentHP, 0, maxHp);

        return currentHp <= 0;
    }

    private static Sprite ResolveFullbodySprite(PersistentRosterUnitData unit, bool isDead)
    {
        if (unit == null || unit.unitViewDefinition == null)
            return null;

        object view = unit.unitViewDefinition;
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        if (isDead)
        {
            string[] deadMethodNamesWithBool =
            {
                "GetBattleSprite",
                "GetFullbodySprite",
                "GetFullBodySprite",
                "GetBattlePortraitSprite",
                "GetUnitCardSprite"
            };

            for (int i = 0; i < deadMethodNamesWithBool.Length; i++)
            {
                MethodInfo method = view.GetType().GetMethod(deadMethodNamesWithBool[i], flags, null, new[] { typeof(bool) }, null);
                if (method != null && typeof(Sprite).IsAssignableFrom(method.ReturnType))
                {
                    object result = method.Invoke(view, new object[] { true });
                    if (result is Sprite sprite && sprite != null)
                        return sprite;
                }
            }

            string[] deadFieldNames =
            {
                "deadBattleSprite",
                "deadFullbodySprite",
                "deadFullBodySprite",
                "deadBattlePortraitSprite",
                "deadUnitCardSprite"
            };

            for (int i = 0; i < deadFieldNames.Length; i++)
            {
                FieldInfo field = view.GetType().GetField(deadFieldNames[i], flags);
                if (field != null && typeof(Sprite).IsAssignableFrom(field.FieldType))
                {
                    object result = field.GetValue(view);
                    if (result is Sprite sprite && sprite != null)
                        return sprite;
                }
            }
        }

        string[] methodNames =
        {
            "GetFullbodySprite",
            "GetFullBodySprite",
            "GetBattleSprite",
            "GetBattlePortraitSprite",
            "GetUnitCardSprite"
        };

        for (int i = 0; i < methodNames.Length; i++)
        {
            MethodInfo methodWithBool = view.GetType().GetMethod(methodNames[i], flags, null, new[] { typeof(bool) }, null);
            if (methodWithBool != null && typeof(Sprite).IsAssignableFrom(methodWithBool.ReturnType))
            {
                object result = methodWithBool.Invoke(view, new object[] { false });
                if (result is Sprite sprite && sprite != null)
                    return sprite;
            }

            MethodInfo method = view.GetType().GetMethod(methodNames[i], flags, null, System.Type.EmptyTypes, null);
            if (method != null && typeof(Sprite).IsAssignableFrom(method.ReturnType))
            {
                object result = method.Invoke(view, null);
                if (result is Sprite sprite && sprite != null)
                    return sprite;
            }
        }

        string[] fieldNames =
        {
            "fullbodySprite",
            "fullBodySprite",
            "battleSprite",
            "battlePortraitSprite",
            "unitCardSprite"
        };

        for (int i = 0; i < fieldNames.Length; i++)
        {
            FieldInfo field = view.GetType().GetField(fieldNames[i], flags);
            if (field != null && typeof(Sprite).IsAssignableFrom(field.FieldType))
            {
                object result = field.GetValue(view);
                if (result is Sprite sprite && sprite != null)
                    return sprite;
            }
        }

        return null;
    }

    private int GetMaxHp(PersistentRosterUnitData unit)
    {
        if (unit == null)
            return 1;

        int baseHp = unit.unitDefinition != null ? unit.unitDefinition.maxHP : 1;
        int varianceHp = unit.statVariance != null ? unit.statVariance.maxHpDelta : 0;
        int growthHp = Mathf.Max(0, unit.levelGrowthMaxHp);

        float promoMultiplier = 1f;
        if (owner != null && owner.TryGetPromotionBonusPercentPerRank(out float perRank))
            promoMultiplier = LegionFormula.GetPromotionMultiplier(unit.promotionRank, perRank);

        return Mathf.Max(1, Mathf.RoundToInt((baseHp + varianceHp + growthHp) * promoMultiplier));
    }

    private static bool GetOptionalBool(object target, params string[] candidateNames)
    {
        if (target == null || candidateNames == null)
            return false;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        System.Type type = target.GetType();

        for (int i = 0; i < candidateNames.Length; i++)
        {
            string name = candidateNames[i];
            if (string.IsNullOrWhiteSpace(name))
                continue;

            FieldInfo field = type.GetField(name, flags);
            if (field != null && field.FieldType == typeof(bool))
                return (bool)field.GetValue(target);

            PropertyInfo property = type.GetProperty(name, flags);
            if (property != null && property.CanRead && property.PropertyType == typeof(bool))
                return (bool)property.GetValue(target);
        }

        return false;
    }
}