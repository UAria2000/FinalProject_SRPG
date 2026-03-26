using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class KeeperData
{
    public string keeperName;
    public Sprite portrait;
    public int level = 1;

    [Header("Stats")]
    public string statsInfo = "공격력: 10\n방어력: 5\n체력: 100";

    [Header("Skills")]
    public Sprite basicAttackIcon;
    public Sprite skill1Icon;
    public Sprite skill2Icon;
    public Sprite skill3Icon;

    [Header("Equipment")]
    public Sprite weaponIcon;
    public Sprite armorIcon;

    public string currentEffect = "현재 효과: 공격력 +0";
    public string upgradeEffect = "업글 효과: 공격력 +5";
    public int upgradeCost = 1000;
}

public class KeeperManager : MonoBehaviour
{
    public static KeeperManager Instance;

    [Header("Keeper List")]
    public List<KeeperData> allKeepers = new List<KeeperData>();

    public System.Action OnKeeperListChanged;

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

        if (allKeepers.Count == 0)
        {
            AddKeeper("수습 파수꾼", null);
        }
    }

    public void AddKeeper(string name, Sprite portrait)
    {
        KeeperData newData = new KeeperData { keeperName = name, portrait = portrait };
        allKeepers.Add(newData);
        OnKeeperListChanged?.Invoke();
    }
}