using UnityEngine;
using Unity.Services.Economy.Model;
using System.Collections.Generic;

public class SoldierListUI : MonoBehaviour
{
    [SerializeField] private GameObject soldierCardPrefab;
    [SerializeField] private Transform contentTransform;

    private async void Start()
    {
        // 서비스 초기화 대기 (EconomyManager가 초기화된 후 실행되어야 함)
        await System.Threading.Tasks.Task.Delay(1500);
        RefreshList();
    }

    public async void RefreshList()
    {
        foreach (Transform child in contentTransform) { Destroy(child.gameObject); }

        // 수정된 매니저 함수 호출
        var myItems = await EconomyManager.Instance.GetMySoldiersAsync();

        foreach (var item in myItems)
        {
            GameObject go = Instantiate(soldierCardPrefab, contentTransform);
            SoldierCard cardScript = go.GetComponent<SoldierCard>();

            if (cardScript != null)
            {
                // 이제 아이템 객체 자체를 넘겨서 상세 정보를 출력합니다.
                cardScript.Setup(item);
            }
        }
    }
}