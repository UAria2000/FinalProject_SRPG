using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Battle/Party Definition")]
public class PartyDefinition : ScriptableObject
{
    public string partyName;

    [Tooltip("파티 템플릿 멤버 목록. 실제 전투 런타임에서는 복제본을 사용한다.")]
    public List<PartyMemberData> members = new List<PartyMemberData>();

    [Tooltip("월드 시작 시 사용할 기본 인벤토리 템플릿. 유닛 상태와 별개로 월드마다 새로 복제된다.")]
    public List<InventoryStackData> inventory = new List<InventoryStackData>();

    public BattlePartyRuntimeState CreateRuntimeState()
    {
        return BattlePartyRuntimeState.CreateFromDefinition(this);
    }

    public List<InventoryStackData> CreateInventoryRuntime()
    {
        List<InventoryStackData> runtimeInventory = new List<InventoryStackData>();
        if (inventory == null)
            return runtimeInventory;

        for (int i = 0; i < inventory.Count; i++)
        {
            InventoryStackData stack = inventory[i];
            if (stack != null)
                runtimeInventory.Add(stack.CloneRuntime());
        }

        return runtimeInventory;
    }

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
