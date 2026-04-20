using UnityEngine;

public class WorldLeftPanelsLayoutUI : MonoBehaviour
{
    [Header("Sections")]
    [SerializeField] private WorldCollapsibleSectionUI dominationSection;
    [SerializeField] private RectTransform dominationPanelRoot;
    [SerializeField] private RectTransform questPanelRoot;

    [Header("Quest Panel Position")]
    [SerializeField] private Vector2 questExpandedAnchoredPos = new Vector2(0f, -190f);
    [SerializeField] private Vector2 questWhenDominationCollapsedAnchoredPos = new Vector2(0f, -110f);

    private void Awake()
    {
        if (dominationSection != null)
            dominationSection.OnCollapsedChanged += HandleDominationCollapsedChanged;
    }

    private void OnDestroy()
    {
        if (dominationSection != null)
            dominationSection.OnCollapsedChanged -= HandleDominationCollapsedChanged;
    }

    private void Start()
    {
        RefreshLayout();
    }

    private void HandleDominationCollapsedChanged(WorldCollapsibleSectionUI _, bool __)
    {
        RefreshLayout();
    }

    public void RefreshLayout()
    {
        if (questPanelRoot == null)
            return;

        bool dominationCollapsed = dominationSection != null && dominationSection.IsCollapsed;
        questPanelRoot.anchoredPosition = dominationCollapsed
            ? questWhenDominationCollapsedAnchoredPos
            : questExpandedAnchoredPos;
    }
}