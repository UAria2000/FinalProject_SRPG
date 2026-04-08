using UnityEngine;
using TMPro;

public class ResourceUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI soulEnergyText;
    public TextMeshProUGUI dainText;

    // 자원 데이터 (실제 게임에서는 별도의 Data Manager에서 관리하는 것이 좋습니다)
    private int soulEnergy;
    private int dain;

    public void UpdateResourceUI(int soul, int dainValue)
    {
        soulEnergy = soul;
        dain = dainValue;

        // UI 텍스트 업데이트
        soulEnergyText.text = string.Format("{0:N0}", soulEnergy);
        dainText.text = string.Format("{0:N0}", dain);
    }
}