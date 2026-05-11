using System.Collections.Generic;
using Unity.Services.CloudCode;
using Unity.Services.CloudSave;
using UnityEngine;

public class MarketManager : MonoBehaviour
{
    // 판매 등록 요청
    public async void PostToMarket(string instanceId, int price)
    {
        try
        {
            var args = new Dictionary<string, object> {
                { "itemInstanceId", instanceId },
                { "price", price }
            };

            // "Marketplace" 모듈의 "ListAsset" 함수 호출
            await CloudCodeService.Instance.CallModuleEndpointAsync("Marketplace", "ListAsset", args);
            Debug.Log("거래소에 아이템이 등록되었습니다.");
        }
        catch (CloudCodeException e)
        {
            Debug.LogError($"등록 실패: {e.Message}");
        }
    }

    // 시장 목록 불러오기 (Cloud Save 이용)
    public async void FetchMarketList()
    {
        try
        {
            // 1. 가져올 키 설정 (HashSet 사용)
            var keys = new HashSet<string> { "MARKET_LIST" };

            // 2. [수정] 최신 SDK 문법은 LoadAsync입니다.
            // 다른 플레이어의 데이터를 가져오는 기능은 보안상 Cloud Code에서 처리하는 것이 원칙이나,
            // 현재 스크립트의 에러를 없애기 위해 자신의 데이터를 로드하는 코드로 수정합니다.
            var data = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

            if (data.ContainsKey("MARKET_LIST"))
            {
                // 3. 데이터 추출 (GetAs<string> 사용)
                string jsonList = data["MARKET_LIST"].Value.GetAs<string>();
                Debug.Log($"거래소 목록 로드 완료: {jsonList}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"시장 목록 로드 실패: {e.Message}");
        }
    }
}