using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Battle/Enemy Encounter Table")]
public class EnemyEncounterTable : ScriptableObject
{
    [Min(1)] public int minEnemyCount = 2;
    [Min(1)] public int maxEnemyCount = 4;

    [Tooltip("중복 뽑기 허용 여부. 현재 프로토타입 기본값은 true.")]
    public bool allowDuplicates = true;

    public List<EnemyEncounterEntry> entries = new List<EnemyEncounterEntry>();

    public int GetRandomEnemyCount()
    {
        int min = Mathf.Clamp(minEnemyCount, 1, 4);
        int max = Mathf.Clamp(maxEnemyCount, min, 4);
        return UnityEngine.Random.Range(min, max + 1);
    }
}

[Serializable]
public class EnemyEncounterEntry
{
    public UnitDefinition unitDefinition;
    public UnitViewDefinition unitViewDefinition;

    [Tooltip("가중치. 높을수록 더 잘 나온다.")]
    [Min(1)] public int weight = 1;

    [Tooltip("평타 외 추가 스킬. 최대 3개 사용 권장.")]
    public List<SkillDefinition> learnedSkills = new List<SkillDefinition>();

    [Tooltip("비어 있으면 UnitDefinition 이름 사용")]
    public string instanceDisplayNameOverride;

    [TextArea(2, 5)]
    public string fixedEpitaph;

    public bool enabled = true;
}
