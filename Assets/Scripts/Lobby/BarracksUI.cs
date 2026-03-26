using UnityEngine;
using System.Collections.Generic;

public class BarracksUI : MonoBehaviour
{
    [Header("Barracks Settings")]
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform contentParent;
    [SerializeField] private int totalSlots = 32;

    void OnEnable()
    {
        if (KeeperManager.Instance != null)
        {
            KeeperManager.Instance.OnKeeperListChanged += RefreshUI;
        }
        RefreshUI();
    }

    void OnDisable()
    {
        if (KeeperManager.Instance != null)
        {
            KeeperManager.Instance.OnKeeperListChanged -= RefreshUI;
        }
    }

    public void RefreshUI()
    {
        foreach (Transform child in contentParent) Destroy(child.gameObject);

        for (int i = 0; i < totalSlots; i++)
        {
            GameObject newSlotObj = Instantiate(slotPrefab, contentParent);
            ItemSlot slotScript = newSlotObj.GetComponent<ItemSlot>();

            if (slotScript != null)
            {
                slotScript.SetLocked(i >= 8);
                slotScript.canDrag = false;

                if (i < KeeperManager.Instance.allKeepers.Count)
                {
                    var data = KeeperManager.Instance.allKeepers[i];
                    slotScript.myKeeperData = data;
                    slotScript.SetItem(data.portrait);
                }
                else
                {
                    slotScript.myKeeperData = null;
                    slotScript.SetItem(null);
                }
            }
        }
    }
}