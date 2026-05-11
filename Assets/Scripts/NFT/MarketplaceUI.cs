using System.Collections.Generic;
using TMPro;
using Unity.Services.CloudCode;
using UnityEngine;

public class MarketplaceUI : MonoBehaviour
{
    public TMP_InputField priceInput;
    private string selectedInstanceId;

    public async void OnClickPostItem()
    {
        if (int.TryParse(priceInput.text, out int price))
        {
            // Cloud Code 함수를 호출하여 판매 등록 요청
            var arguments = new Dictionary<string, object> {
                { "instanceId", selectedInstanceId },
                { "askingPrice", price }
            };

            await CloudCodeService.Instance.CallModuleEndpointAsync("Marketplace", "PostItem", arguments);
            Debug.Log($"{price} 골드에 판매 등록을 요청했습니다.");
        }
    }
}