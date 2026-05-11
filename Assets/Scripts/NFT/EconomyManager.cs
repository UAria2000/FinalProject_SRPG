using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Economy;
using Unity.Services.Economy.Model;
using UnityEngine;

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    private async void Awake()
    {
        // 싱글톤 설정
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        await InitializeEconomyAsync();
    }

    // EconomyManager.cs 내부에 추가
    public async Task<List<PlayersInventoryItem>> GetMySoldiersAsync()
    {
        try
        {
            // 최신 SDK 구조: .PlayerInventory.GetInventoryAsync() 사용
            GetInventoryResult inventoryResult = await EconomyService.Instance.PlayerInventory.GetInventoryAsync();

            // 오류 지점 수정: PlayersInventoryItems 속성을 호출해야 합니다.
            return inventoryResult.PlayersInventoryItems;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"인벤토리 로드 실패: {e.Message}");
            return new List<PlayersInventoryItem>();
        }
    }

    private async Task InitializeEconomyAsync()
    {
        try
        {
            // 1. 서비스 초기화
            await UnityServices.InitializeAsync();

            // 2. 익명 로그인 (거래소 이용을 위한 최소 인증)
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"로그인 성공: {AuthenticationService.Instance.PlayerId}");
            }

            // 3. 대시보드 데이터 동기화 (방금 설정한 GOLD, 기사 정보 가져오기)
            await EconomyService.Instance.Configuration.SyncConfigurationAsync();
            Debug.Log("Economy 서비스 동기화 완료");
        }
        catch (Exception e)
        {
            Debug.LogError($"초기화 실패: {e.Message}");
        }
    }

    // [테스트용] 아까 만든 기사 고용(Virtual Purchase) 실행 함수
    public async Task BuyKnight()
    {
        try
        {
            // ID는 대시보드에서 만든 것과 정확히 일치해야 함
            MakeVirtualPurchaseResult result = await EconomyService.Instance.Purchases.MakeVirtualPurchaseAsync("BUY_KNIGHT_01");
            Debug.Log("기사 고용 성공!");
        }
        catch (EconomyException e)
        {
            Debug.LogError($"구매 실패: {e.ErrorCode} - {e.Message}");
        }
    }
}