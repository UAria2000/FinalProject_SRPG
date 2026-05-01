using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 전투 하단 초상화 슬롯 하나.
/// 클릭 판정은 이 컴포넌트가 붙은 루트 또는 Click Area가 받는다.
/// Button.onClick은 비워두는 것을 권장한다.
/// </summary>
public class BattleBottomPortraitSlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private Image hpFillImage;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text slotText;

    [Header("Roots")]
    [SerializeField] private GameObject contentRoot;
    [SerializeField] private GameObject emptyRoot;
    [SerializeField] private GameObject selectedRoot;
    [SerializeField] private GameObject currentTurnRoot;
    [SerializeField] private GameObject finishedTurnRoot;
    [SerializeField] private GameObject deadRoot;
    [SerializeField] private GameObject hoverRoot;

    [Header("Click Area")]
    [Tooltip("비워두면 자기 자신의 Graphic을 클릭 영역으로 사용합니다. 별도 투명 클릭판이 있으면 연결하세요.")]
    [SerializeField] private Graphic clickAreaGraphic;
    [Tooltip("켜면 클릭 영역을 제외한 하위 Graphic의 Raycast Target을 자동으로 끕니다.")]
    [SerializeField] private bool disableChildRaycastsExceptClickArea = true;

    private BattleBottomPortraitBarUI owner;
    private BattleUnit boundUnit;
    private TeamType team;
    private int slotIndex;
    private bool initialized;

    public BattleUnit BoundUnit => boundUnit;
    public int SlotIndex => slotIndex;
    public TeamType Team => team;

    public void Initialize(BattleBottomPortraitBarUI owner, TeamType team, int slotIndex)
    {
        this.owner = owner;
        this.team = team;
        this.slotIndex = slotIndex;
        initialized = true;

        EnsureClickArea();
        if (disableChildRaycastsExceptClickArea)
            DisableChildGraphicRaycastsExcept(clickAreaGraphic);
    }

    private void Awake()
    {
        EnsureClickArea();
    }

    private void EnsureClickArea()
    {
        if (clickAreaGraphic == null)
            clickAreaGraphic = GetComponent<Graphic>();

        if (clickAreaGraphic == null)
        {
            Image image = gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            clickAreaGraphic = image;
        }

        clickAreaGraphic.raycastTarget = true;

        if (clickAreaGraphic is Image img)
        {
            Color c = img.color;
            c.a = 0f;
            img.color = c;
        }
    }

    private void DisableChildGraphicRaycastsExcept(Graphic exceptionGraphic)
    {
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic g = graphics[i];
            if (g == null)
                continue;

            if (exceptionGraphic != null && g == exceptionGraphic)
                continue;

            g.raycastTarget = false;
        }
    }

    public void Bind(
        BattleUnit unit,
        bool isSelected,
        bool isCurrentTurn,
        bool isFinishedThisRound,
        bool showSlotIndex)
    {
        boundUnit = unit;

        bool hasUnit = unit != null;
        if (contentRoot != null)
            contentRoot.SetActive(hasUnit);
        if (emptyRoot != null)
            emptyRoot.SetActive(!hasUnit);

        if (portraitImage != null)
        {
            Sprite sprite = null;
            if (unit != null && unit.ViewDefinition != null)
                sprite = unit.ViewDefinition.GetSlotFaceSprite(unit.IsDead);
            if (sprite == null && unit != null)
                sprite = unit.SlotFaceSprite;

            portraitImage.sprite = sprite;
            portraitImage.color = sprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
            portraitImage.preserveAspect = true;
            portraitImage.raycastTarget = false;
        }

        if (hpFillImage != null)
        {
            float hpRatio = unit != null && unit.MaxHP > 0 ? (float)unit.CurrentHP / unit.MaxHP : 0f;
            hpFillImage.fillAmount = Mathf.Clamp01(hpRatio);
            hpFillImage.gameObject.SetActive(hasUnit);
            hpFillImage.raycastTarget = false;
        }

        if (hpText != null)
        {
            hpText.text = unit != null ? string.Format("{0}/{1}", Mathf.Max(0, unit.CurrentHP), Mathf.Max(1, unit.MaxHP)) : string.Empty;
            hpText.raycastTarget = false;
        }

        if (levelText != null)
        {
            levelText.text = unit != null ? string.Format("Lv.{0}", Mathf.Max(1, unit.CurrentLevel)) : string.Empty;
            levelText.raycastTarget = false;
        }

        if (slotText != null)
        {
            slotText.text = showSlotIndex ? (slotIndex + 1).ToString() : string.Empty;
            slotText.raycastTarget = false;
        }

        if (selectedRoot != null)
            selectedRoot.SetActive(hasUnit && isSelected);
        if (currentTurnRoot != null)
            currentTurnRoot.SetActive(hasUnit && isCurrentTurn);
        if (finishedTurnRoot != null)
            finishedTurnRoot.SetActive(hasUnit && isFinishedThisRound && !isCurrentTurn);
        if (deadRoot != null)
            deadRoot.SetActive(hasUnit && unit.IsDead);
        if (hoverRoot != null)
            hoverRoot.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!initialized && owner == null)
            return;

        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
            return;

        if (owner != null)
            owner.HandleSlotClicked(this, boundUnit, team, slotIndex);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverRoot != null)
            hoverRoot.SetActive(boundUnit != null);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (hoverRoot != null)
            hoverRoot.SetActive(false);
    }
}
