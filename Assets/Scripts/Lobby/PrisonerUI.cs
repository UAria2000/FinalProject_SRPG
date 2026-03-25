using UnityEngine;
using System.Collections.Generic;

public class PrisonerUI : MonoBehaviour
{
    [Header("Prisoner Settings")]
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform contentParent;
    [SerializeField] private int totalSlots = 32;

    private List<ItemSlot> _spawnedSlots = new List<ItemSlot>();

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
                slotScript.SetLocked(i >= 8); // 첫 줄 해금 로직

                // ★ 중요: 스크립트는 켜두고(Enabled), 드래그만 금지(canDrag = false) 하세요!
                slotScript.enabled = true;
                slotScript.canDrag = false;

                if (i < PrisonerManager.Instance.allPrisoners.Count)
                {
                    var data = PrisonerManager.Instance.allPrisoners[i];
                    slotScript.myData = data;
                    slotScript.SetItem(data.portrait);
                }
            }
        }
    }
}