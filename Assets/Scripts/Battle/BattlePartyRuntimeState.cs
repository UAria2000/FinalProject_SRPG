using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BattlePartyRuntimeState
{
    public string partyName;
    public PartyDefinition sourceDefinition;
    public List<PartyMemberData> members = new List<PartyMemberData>();

    public static BattlePartyRuntimeState CreateFromDefinition(PartyDefinition definition)
    {
        BattlePartyRuntimeState state = new BattlePartyRuntimeState();
        state.sourceDefinition = definition;
        state.partyName = definition != null ? definition.partyName : string.Empty;

        if (definition != null && definition.members != null)
        {
            for (int i = 0; i < definition.members.Count; i++)
            {
                PartyMemberData member = definition.members[i];
                if (member != null)
                    state.members.Add(member.CloneRuntime());
            }
        }

        return state;
    }

    public bool IsValidMemberCount()
    {
        return members != null && members.Count >= 1 && members.Count <= 4;
    }

    public bool HasDuplicateSlotIndex()
    {
        if (members == null)
            return false;

        HashSet<int> used = new HashSet<int>();
        for (int i = 0; i < members.Count; i++)
        {
            PartyMemberData member = members[i];
            if (member == null)
                continue;

            if (!used.Add(member.startSlotIndex))
                return true;
        }

        return false;
    }

    public bool HasNullDefinitions()
    {
        if (members == null)
            return true;

        for (int i = 0; i < members.Count; i++)
        {
            PartyMemberData member = members[i];
            if (member == null || member.unitDefinition == null || member.unitViewDefinition == null)
                return true;
        }

        return false;
    }

    public void ResetPersistentHPToFull()
    {
        if (members == null)
            return;

        for (int i = 0; i < members.Count; i++)
        {
            PartyMemberData member = members[i];
            if (member != null)
                member.ResetPersistentHPToFull();
        }
    }
}
