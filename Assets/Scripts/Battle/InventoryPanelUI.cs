using System.Collections.Generic;
using UnityEngine;

public class InventoryPanelUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private InventorySlotUI[] slots = new InventorySlotUI[8];

    public void Show(bool show)
    {
        if (root != null)
            root.SetActive(show);
    }

    public void Bind(BattleManager manager, List<InventoryStackData> stacks, int selectedIndex)
    {
        if (slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            InventoryStackData stack = null;
            if (stacks != null && i < stacks.Count)
                stack = stacks[i];

            if (slots[i] != null)
                slots[i].Bind(manager, i, stack, i == selectedIndex);
        }
    }
}
