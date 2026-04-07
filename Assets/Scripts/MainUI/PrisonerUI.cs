using UnityEngine;
using System.Collections.Generic;

public class PrisonerUI : MonoBehaviour
{
    [Header("Prisoner Settings")]
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform contentParent;
    [SerializeField] private int totalSlots = 32;

    void OnEnable()
    {
        if (PrisonerManager.Instance != null)
        {
            PrisonerManager.Instance.OnPrisonerListChanged += RefreshUI;
        }
        RefreshUI();
    }

    void OnDisable()
    {
        if (PrisonerManager.Instance != null)
        {
            PrisonerManager.Instance.OnPrisonerListChanged -= RefreshUI;
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
                // 첫 줄 해금 및 드래그 금지 설정
                slotScript.SetLocked(i >= 8);
                slotScript.canDrag = false;
                slotScript.enabled = true;

                if (i < PrisonerManager.Instance.allPrisoners.Count)
                {
                    var data = PrisonerManager.Instance.allPrisoners[i];
                    slotScript.myData = data;
                    slotScript.SetItem(data.portrait);
                }
                else
                {
                    slotScript.myData = null;
                    slotScript.SetItem(null);
                }
            }
        }
    }
}