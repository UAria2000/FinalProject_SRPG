using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CorruptUI : MonoBehaviour
{
    [Header("Left Slots (Status)")]
    [SerializeField] private List<CorruptSlot> statusSlots;

    [Header("Right Inventory Settings")]
    [SerializeField] private GameObject itemSlotPrefab; // Prisoners_Prefab 연결
    [SerializeField] private Transform contentParent;
    [SerializeField] private int totalSlots = 32;

    void OnEnable()
    {
        if (PrisonerManager.Instance != null)
        {
            PrisonerManager.Instance.OnPrisonerListChanged += RefreshInventory;
        }
        RefreshInventory();
    }

    void OnDisable()
    {
        if (PrisonerManager.Instance != null)
        {
            PrisonerManager.Instance.OnPrisonerListChanged -= RefreshInventory;
        }
    }

    public void RefreshInventory()
    {
        // 기존 슬롯 제거
        foreach (Transform child in contentParent) Destroy(child.gameObject);

        // 타락 중이 아닌 포로 데이터 추출
        var availablePrisoners = PrisonerManager.Instance.allPrisoners
            .Where(p => !p.isCorrupting)
            .ToList();

        for (int i = 0; i < totalSlots; i++)
        {
            GameObject slotObj = Instantiate(itemSlotPrefab, contentParent);
            ItemSlot slotScript = slotObj.GetComponent<ItemSlot>();

            if (slotScript != null)
            {
                // 첫 줄(index 0~7)만 자물쇠 해제 및 드래그 허용
                bool isLocked = (i >= 8);
                slotScript.SetLocked(isLocked);
                slotScript.canDrag = !isLocked;
                slotScript.enabled = true;

                // 데이터가 존재하는 경우에만 UI 갱신
                if (i < availablePrisoners.Count)
                {
                    var data = availablePrisoners[i];
                    slotScript.myData = data;
                    slotScript.SetItem(data.portrait);
                    slotObj.name = $"CorruptInvSlot_{i} ({data.prisonerName})";
                }
                else
                {
                    slotScript.myData = null;
                    slotScript.SetItem(null);
                    slotObj.name = $"CorruptInvSlot_{i} (Empty)";
                }
            }
        }
    }
}