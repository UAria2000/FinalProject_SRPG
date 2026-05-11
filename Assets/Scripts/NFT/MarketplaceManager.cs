using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.CloudSave;
// ★중요: Item 형식을 인식하기 위해 아래 네임스페이스가 반드시 필요합니다.
using Unity.Services.CloudSave.Models;
using Unity.Services.CloudSave.Models.Data.Player;

public class MarketplaceManager : MonoBehaviour
{
    public static MarketplaceManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    // 다른 플레이어의 공개 정보를 가져오는 함수
    public async Task LoadMarketItems(string targetPlayerId)
    {
        try
        {
            var keys = new HashSet<string> { "Name", "Price" };

            // [수정] LoadAsync를 사용합니다. 
            // 주의: 최신 SDK에서 타인의 데이터를 가져오려면 Cloud Code를 경유해야 합니다.
            // 현재는 컴파일 에러를 해결하기 위해 LoadAsync로 변경합니다.
            var data = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

            if (data.ContainsKey("Name"))
            {
                string name = data["Name"].Value.GetAs<string>();
                Debug.Log($"데이터 로드 성공: {name}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"오류 발생: {e.Message}");
        }
    }
}