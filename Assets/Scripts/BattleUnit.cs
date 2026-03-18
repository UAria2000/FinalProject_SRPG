using UnityEngine;

public class BattleUnit
{
    public UnitDefinition Definition { get; private set; }
    public TeamType Team { get; private set; }

    public int CurrentHP { get; private set; }
    public int SlotIndex { get; set; }

    public bool IsDead => CurrentHP <= 0;

    public BattleUnit(UnitDefinition definition, TeamType team, int slotIndex)
    {
        Definition = definition;
        Team = team;
        SlotIndex = slotIndex;
        CurrentHP = definition.maxHP;
    }

    public string Name => Definition.unitName;
    public CharacterRangeType RangeType => Definition.rangeType;

    public int GetMaxHP() => Definition.maxHP;
    public int GetAtk() => Definition.atk;
    public int GetDef() => Definition.def;
    public int GetSpd() => Definition.spd;
    public int GetCrit() => Definition.crit;
    public int GetAcc() => Definition.acc;
    public int GetEva() => Definition.eva;

    public void TakeDamage(int amount)
    {
        CurrentHP = Mathf.Max(0, CurrentHP - amount);
    }

    public void Heal(int amount)
    {
        CurrentHP = Mathf.Min(GetMaxHP(), CurrentHP + amount);
    }

    public override string ToString()
    {
        return $"{Name}({Team}) [Slot:{SlotIndex + 1}, HP:{CurrentHP}/{GetMaxHP()}]";
    }
}