using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BattlePersistenceController : MonoBehaviour
{
    private BattleManager battleManager;

    public void Initialize(BattleManager manager)
    {
        battleManager = manager;
    }

    public void SavePersistentAllyPartyHP()
    {
        if (battleManager == null || battleManager.AllyFormation == null)
            return;

        List<BattleUnit> allies = battleManager.AllyFormation.GetAllUnits();
        for (int i = 0; i < allies.Count; i++)
        {
            BattleUnit ally = allies[i];
            if (ally == null)
                continue;

            ally.SavePersistentHPToMemberData();
        }

        PartyDefinition allyPartyDefinition = battleManager.AllyPartyDefinition;
        if (allyPartyDefinition == null || allyPartyDefinition.members == null)
            return;

        for (int i = 0; i < allyPartyDefinition.members.Count; i++)
        {
            PartyMemberData member = allyPartyDefinition.members[i];
            if (member == null)
                continue;

            bool found = false;
            for (int j = 0; j < allies.Count; j++)
            {
                BattleUnit ally = allies[j];
                if (ally != null && ally.MemberData == member)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
                member.persistentCurrentHP = 0;
        }
    }

    public void ResetPersistentAllyPartyHPForNewMap()
    {
        if (battleManager == null)
            return;

        PartyDefinition allyPartyDefinition = battleManager.AllyPartyDefinition;
        if (allyPartyDefinition == null || allyPartyDefinition.members == null)
            return;

        for (int i = 0; i < allyPartyDefinition.members.Count; i++)
        {
            PartyMemberData member = allyPartyDefinition.members[i];
            if (member == null)
                continue;

            member.ResetPersistentHPToFull();
        }
    }
}
