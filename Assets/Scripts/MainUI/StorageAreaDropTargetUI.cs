using UnityEngine;
using UnityEngine.EventSystems;

public class StorageAreaDropTargetUI : MonoBehaviour, IDropHandler
{
    [SerializeField] private BottomPartySummaryPanelUI bottomPartySummaryPanelUI;

    private void Awake()
    {
        if (bottomPartySummaryPanelUI == null)
            bottomPartySummaryPanelUI = Object.FindFirstObjectByType<BottomPartySummaryPanelUI>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (bottomPartySummaryPanelUI == null)
            return;

        bottomPartySummaryPanelUI.HandleEquipmentDroppedToStorage();
    }
}