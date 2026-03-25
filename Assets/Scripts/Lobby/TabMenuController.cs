using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TabMenuController : MonoBehaviour
{
    [System.Serializable]
    public struct TabMapping
    {
        public Button tabButton;        // 상단 메뉴의 버튼
        public GameObject contentPanel; // 아래에 뜰 내용 패널
    }

    [Header("Main Popup Settings")]
    [SerializeField] private GameObject popupRoot; // Dimmer 포함 최상위 부모

    [Header("Tab Settings")]
    [SerializeField] private List<TabMapping> tabMappings;
    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private Color defaultColor = new Color(0.7f, 0.7f, 0.7f, 1f);

    void Awake()
    {
        // 선배, 상단 메뉴 버튼들은 여기서 한 번에 이벤트를 다 연결해줄게요.
        for (int i = 0; i < tabMappings.Count; i++)
        {
            int index = i; // 클로저 문제 방지용
            if (tabMappings[i].tabButton != null)
            {
                tabMappings[i].tabButton.onClick.AddListener(() => OnTabSelected(index));
            }
        }
    }

    // 메인 화면 버튼(예: 창고 버튼)에서 호출할 함수!
    public void OpenPopup(int startTabIndex)
    {
        if (popupRoot != null)
        {
            popupRoot.SetActive(true);
            OnTabSelected(startTabIndex); // 열자마자 해당 탭을 보여줘요
        }
    }

    // X 버튼 누를 때 호출!
    public void ClosePopup()
    {
        if (popupRoot != null) popupRoot.SetActive(false);
    }

    // 상단 메뉴 버튼을 눌렀을 때 실행되는 탭 전환 로직
    public void OnTabSelected(int index)
    {
        if (index < 0 || index >= tabMappings.Count) return;

        for (int i = 0; i < tabMappings.Count; i++)
        {
            if (tabMappings[i].tabButton == null || tabMappings[i].contentPanel == null) continue;

            bool isSelected = (i == index);

            // 1. 패널 끄고 켜기
            tabMappings[i].contentPanel.SetActive(isSelected);

            // 2. 버튼 강조 (이미지 색상)
            Image btnImage = tabMappings[i].tabButton.GetComponent<Image>();
            if (btnImage != null) btnImage.color = isSelected ? selectedColor : defaultColor;

            // 3. 텍스트 강조 (볼드체)
            Text btnText = tabMappings[i].tabButton.GetComponentInChildren<Text>();
            if (btnText != null) btnText.fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal;
        }
    }
}