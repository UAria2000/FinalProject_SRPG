using TMPro;
using Unity.Services.Economy.Model;
using UnityEngine;
using UnityEngine.AddressableAssets; // Addressables 사용을 위해 필수
using UnityEngine.ResourceManagement.AsyncOperations; // 비동기 작업 처리를 위해 필수
using UnityEngine.UI;
using static UnityEditor.Progress;

public class SoldierCard : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private RawImage portraitImage; // HexMask의 자식인 Portrait 연결

    public void Setup(PlayersInventoryItem item)
    {
        var definition = item.GetItemDefinition();

        // 1. 이름 및 레벨 설정
        nameText.text = definition.Name;
        var customData = item.InstanceData.GetAs<SoldierInstanceData>();
        levelText.text = customData != null ? $"Lv. {customData.level}" : "Lv. 1";

        // 2. Addressables 이미 로드
        // 대시보드 ID(예: ARCHER)를 주소로 사용하여 이미지를 찾습니다.
        string assetAddress = definition.Id;

        LoadPortrait(assetAddress);
    }

    public void OnClickCard()
    {
        // 거래소 관리 스크립트에 이 카드의 정보를 전달
        // item.InstanceId는 고유한 병사 식별 번호입니다.
        MarketplaceUI.Instance.SelectCardForSale(item.InstanceId, definition.Name);
    }

    private void LoadPortrait(string address)
    {
        // 비동기로 이미지를 불러옵니다.
        Addressables.LoadAssetAsync<Texture2D>(address).Completed += (handle) =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                // 불러오기 성공 시 RawImage에 할당
                portraitImage.texture = handle.Result;
            }
            else
            {
                Debug.LogWarning($"이미지를 찾을 수 없습니다: {address}");
                // 이미지가 없을 경우 기본 이미지나 빈 텍스처를 넣는 로직을 추가할 수 있습니다.
            }
        };
    }
}

[System.Serializable]
public class SoldierInstanceData
{
    public int level;
}