using UnityEngine;
using TMPro; // 텍스트메쉬 프로를 써야 글자가 안 깨지죠!

public class BottomUIController : MonoBehaviour
{
    [Header("Resource UI References")]
    [SerializeField] private TextMeshProUGUI soulEnergyText;
    [SerializeField] private TextMeshProUGUI dainText;

    // 내부 데이터 (나중에 선배가 만든 게임 매니저에서 넘겨받게 수정하세요)
    private int _currentSoulEnergy = 0;
    private int _currentDain = 0;

    void Start()
    {
        // 초기화
        UpdateUI();
    }

    /// <summary>
    /// 소울 에너지 수치를 업데이트합니다.
    /// </summary>
    public void SetSoulEnergy(int amount)
    {
        _currentSoulEnergy = amount;
        UpdateUI();
    }

    /// <summary>
    /// 데인 수치를 업데이트합니다.
    /// </summary>
    public void SetDain(int amount)
    {
        _currentDain = amount;
        UpdateUI();
    }

    /// <summary>
    /// 실제 UI 텍스트에 반영하는 부분이에요. 
    /// </summary>
    private void UpdateUI()
    {
        // "N0"는 천 단위로 쉼표(,)를 찍어주는 서식이에요. 선배도 이 정돈 알죠?
        if (soulEnergyText != null)
            soulEnergyText.text = _currentSoulEnergy.ToString("N0");

        if (dainText != null)
            dainText.text = _currentDain.ToString("N0");
    }

    // 테스트용 (에디터에서 자원 바뀌는 거 보고 싶을 때 쓰세요)
    [ContextMenu("Test Update")]
    public void TestUpdate()
    {
        SetSoulEnergy(1500);
        SetDain(50);
    }
}