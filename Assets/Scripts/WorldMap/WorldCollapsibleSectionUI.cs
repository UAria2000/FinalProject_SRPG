using System;
using UnityEngine;
using UnityEngine.UI;

public class WorldCollapsibleSectionUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button toggleButton;
    [SerializeField] private GameObject contentRoot;
    [SerializeField] private Image toggleIconImage;

    [Header("Toggle Sprites")]
    [SerializeField] private Sprite expandedSprite;
    [SerializeField] private Sprite collapsedSprite;

    [Header("State")]
    [SerializeField] private bool startCollapsed = false;

    public bool IsCollapsed { get; private set; }

    public event Action<WorldCollapsibleSectionUI, bool> OnCollapsedChanged;

    private void Awake()
    {
        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveAllListeners();
            toggleButton.onClick.AddListener(Toggle);
        }

        SetCollapsed(startCollapsed, true);
    }

    public void Toggle()
    {
        SetCollapsed(!IsCollapsed);
    }

    public void SetCollapsed(bool collapsed, bool force = false)
    {
        if (!force && IsCollapsed == collapsed)
            return;

        IsCollapsed = collapsed;

        if (contentRoot != null)
            contentRoot.SetActive(!collapsed);

        if (toggleIconImage != null)
            toggleIconImage.sprite = collapsed ? collapsedSprite : expandedSprite;

        OnCollapsedChanged?.Invoke(this, IsCollapsed);
    }
}