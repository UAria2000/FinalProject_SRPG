using UnityEngine;
using System.Collections.Generic;

// --- 1. 데이터 클래스 (반드시 클래스 밖에 있어야 해요!) ---
[System.Serializable]
public class PrisonerData
{
    public string prisonerName;
    public Sprite portrait;
    public bool isCorrupting;
    public int requiredBattles = 3; // 타락에 필요한 판수
    public int currentBattles = 0;  // 현재 채운 판수
}

// --- 2. 매니저 클래스 ---
public class PrisonerManager : MonoBehaviour
{
    public static PrisonerManager Instance;

    [Header("Prisoner List")]
    public List<PrisonerData> allPrisoners = new List<PrisonerData>();

    // 포로 목록이 바뀔 때 UI에 알려줄 알림벨!
    public System.Action OnPrisonerListChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 테스트 데이터: 선배가 포로 잡는 거 구현 안 했을까 봐 제가 넣어둔 거예요!
        if (allPrisoners.Count == 0)
        {
            AddPrisoner("기사", null);
            AddPrisoner("수녀", null);
        }
    }

    /// <summary>
    /// 새로운 포로를 추가하고 UI에 알립니다!
    /// </summary>
    public void AddPrisoner(string name, Sprite portrait)
    {
        PrisonerData newData = new PrisonerData
        {
            prisonerName = name,
            portrait = portrait,
            isCorrupting = false
        };

        allPrisoners.Add(newData);

        // UI들한테 "새 포로 왔으니까 다시 그려!"라고 말해요.
        OnPrisonerListChanged?.Invoke();

        Debug.Log($"[매니저] {name} 추가 완료! 이제 포로는 총 {allPrisoners.Count}명이에요.");
    }
}