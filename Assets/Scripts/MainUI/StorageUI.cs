using UnityEngine;
using System.Collections.Generic;

public class StorageUI : MonoBehaviour
{
    [Header("Storage Settings")]
    [SerializeField] private GameObject slotPrefab;   // 자물쇠 없는 프리팹 연결!
    [SerializeField] private Transform contentParent;
    [SerializeField] private int totalSlots = 40;

    private List<ItemSlot> _spawnedSlots = new List<ItemSlot>();

    void OnEnable()
    {
        // 선배, 비어있을 때만 새로 만들기로 했죠?
        if (_spawnedSlots.Count == 0) InitStorage();
    }

    public void InitStorage()
    {
        foreach (Transform child in contentParent) Destroy(child.gameObject);
        _spawnedSlots.Clear();

        for (int i = 0; i < totalSlots; i++)
        {
            GameObject newSlotObj = Instantiate(slotPrefab, contentParent);
            ItemSlot slotScript = newSlotObj.GetComponent<ItemSlot>();

            if (slotScript != null)
            {
                // ★ 창고도 마찬가지로 스크립트는 켜두고 canDrag만 끕니다!
                slotScript.enabled = true;
                slotScript.canDrag = false;
                _spawnedSlots.Add(slotScript);
            }
        }
    }
}