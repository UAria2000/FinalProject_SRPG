using UnityEngine;
using System.Collections.Generic;

public class CorruptUI : MonoBehaviour
{
    [Header("Left Slots (Status)")]
    [SerializeField] private List<CorruptSlot> statusSlots; // 왼쪽 타락 진행 슬롯들

    [Header("Right Inventory")]
    [SerializeField] private GameObject itemSlotPrefab; // 자물쇠 있는 프리팹도 괜찮고 없는 것도 괜찮아요
    [SerializeField] private Transform contentParent;

    void OnEnable()
    {
        // 선배! 장부가 바뀔 때마다(예: 타락 시작해서 목록에서 사라질 때) 다시 그려야 해요.
        if (PrisonerManager.Instance != null)
        {
            PrisonerManager.Instance.OnPrisonerListChanged += RefreshInventory;
        }

        RefreshInventory();
    }

    void OnDisable()
    {
        // 꺼질 때 예약 취소 안 하면 에러 나는 거, 이제 선배도 상식이죠?
        if (PrisonerManager.Instance != null)
        {
            PrisonerManager.Instance.OnPrisonerListChanged -= RefreshInventory;
        }
    }

    // 오른쪽 포로 목록을 두뇌(Manager)에서 가져와서 다시 그립니다.
    public void RefreshInventory()
    {
        foreach (Transform child in contentParent) Destroy(child.gameObject);

        foreach (var data in PrisonerManager.Instance.allPrisoners)
        {
            if (!data.isCorrupting)
            {
                GameObject slotObj = Instantiate(itemSlotPrefab, contentParent);
                ItemSlot slotScript = slotObj.GetComponent<ItemSlot>();

                if (slotScript != null)
                {
                    slotScript.myData = data;
                    slotScript.SetItem(data.portrait);

                    // ★ 선배! 타락 창은 드래그를 '진짜로' 허용해줘야죠!
                    slotScript.SetLocked(false);
                    slotScript.canDrag = true;   // 드래그 ON!
                    slotScript.enabled = true;   // 스크립트 ON!
                }
            }
        }
    }
}