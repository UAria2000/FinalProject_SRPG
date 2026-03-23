using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Battle/Party Definition")]
public class PartyDefinition : ScriptableObject
{
    public string partyName;

    [Tooltip("전투 외부에서 이미 준비된 파티 상태")]
    public List<PartyMemberData> members = new List<PartyMemberData>();

    [Tooltip("파티 공용 인벤토리. 적 파티는 비워두면 된다.")]
    public List<InventoryStackData> inventory = new List<InventoryStackData>();

    public bool IsValidMemberCount()
    {
        return members != null && members.Count >= 1 && members.Count <= 4;
    }

    public bool HasDuplicateSlotIndex()
    {
        if (members == null) return false;
        HashSet<int> used = new HashSet<int>();
        for (int i = 0; i < members.Count; i++)
        {
            PartyMemberData member = members[i];
            if (member == null) continue;
            if (used.Contains(member.startSlotIndex))
                return true;
            used.Add(member.startSlotIndex);
        }
        return false;
    }

    public bool HasNullDefinitions()
    {
        if (members == null) return true;
        for (int i = 0; i < members.Count; i++)
        {
            PartyMemberData member = members[i];
            if (member == null) return true;
            if (member.unitDefinition == null) return true;
            if (member.unitViewDefinition == null) return true;
        }
        return false;
    }
}
