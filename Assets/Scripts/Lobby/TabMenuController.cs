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
        // 상단 메뉴 버튼들의 이벤트를 연결합니다.
        for (int i = 0; i < tabMappings.Count; i++)
        {
            int index = i; // 클로저 문제 방지를 위한 로컬 변수
            if (tabMappings[i].tabButton != null)
            {
                tabMappings[i].tabButton.onClick.AddListener(() => OnTabSelected(index));
            }
        }
    }

    void Update()
    {
        // ESC 키 입력 시 활성화된 팝업을 닫습니다.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (popupRoot != null && popupRoot.activeSelf)
            {
                ClosePopup();
            }
        }
    }

    // 메인 화면 버튼 등에서 팝업을 열 때 호출합니다.
    public void OpenPopup(int startTabIndex)
    {
        if (popupRoot != null)
        {
            popupRoot.SetActive(true);
            OnTabSelected(startTabIndex);
        }
    }

    // X 버튼 또는 ESC 키를 통해 팝업을 닫을 때 호출합니다.
    public void ClosePopup()
    {
        if (popupRoot != null)
        {
            popupRoot.SetActive(false);

            // 팝업 종료 시 선택되어 있던 파수꾼 정보를 초기화합니다.
            if (KeeperInfoUI.Instance != null)
            {
                KeeperInfoUI.Instance.ClearDisplay();
            }
        }
    }

    // 상단 탭 버튼을 눌렀을 때 실행되는 전환 로직입니다.
    public void OnTabSelected(int index)
    {
        if (index < 0 || index >= tabMappings.Count) return;

        // 탭이 바뀔 때마다 기존에 표시되던 상세 정보를 소거합니다.
        if (KeeperInfoUI.Instance != null)
        {
            KeeperInfoUI.Instance.ClearDisplay();
        }

        for (int i = 0; i < tabMappings.Count; i++)
        {
            if (tabMappings[i].tabButton == null || tabMappings[i].contentPanel == null) continue;

            bool isSelected = (i == index);

            // 1. 패널 활성화 상태 제어
            tabMappings[i].contentPanel.SetActive(isSelected);

            // 2. 버튼 시각적 효과 적용 (색상)
            Image btnImage = tabMappings[i].tabButton.GetComponent<Image>();
            if (btnImage != null) btnImage.color = isSelected ? selectedColor : defaultColor;

            // 3. 텍스트 시각적 효과 적용 (글꼴 스타일)
            Text btnText = tabMappings[i].tabButton.GetComponentInChildren<Text>();
            if (btnText != null) btnText.fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal;
        }
    }
}