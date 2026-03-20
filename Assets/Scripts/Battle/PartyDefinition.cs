using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Battle/Party Definition")]
public class PartyDefinition : ScriptableObject
{
    [Header("Info")]
    public string partyName;

    [Header("Members")]
    [Tooltip("전투에 참여할 파티 멤버 목록. 최소 1명, 최대 4명")]
    public List<PartyMemberData> members = new List<PartyMemberData>();

    public int Count => members != null ? members.Count : 0;

    public bool IsValidMemberCount()
    {
        return members != null && members.Count >= 1 && members.Count <= 4;
    }

    public bool HasDuplicateSlotIndex()
    {
        if (members == null) return false;

        HashSet<int> usedSlots = new HashSet<int>();

        for (int i = 0; i < members.Count; i++)
        {
            PartyMemberData member = members[i];
            if (member == null) continue;

            if (usedSlots.Contains(member.startSlotIndex))
                return true;

            usedSlots.Add(member.startSlotIndex);
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