using System;
using UnityEngine;

[Serializable]
public class PartyMemberData
{
    [Header("Unit Data")]
    public UnitDefinition unitDefinition;

    [Header("View Data")]
    public UnitViewDefinition unitViewDefinition;

    [Header("Formation")]
    [Tooltip("시작 슬롯 번호. 0=앞열, 3=뒷열")]
    [Range(0, 3)]
    public int startSlotIndex = 0;
}