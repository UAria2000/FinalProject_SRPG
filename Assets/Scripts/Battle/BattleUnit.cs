using UnityEngine;

public class BattleUnit
{
    public UnitDefinition Definition { get; private set; }
    public UnitViewDefinition ViewDefinition { get; private set; }
    public TeamType Team { get; private set; }

    public int CurrentHP { get; private set; }
    public int SlotIndex { get; set; }

    public bool IsDead => CurrentHP <= 0;

    public BattleUnit(UnitDefinition definition, UnitViewDefinition viewDefinition, TeamType team, int slotIndex)
    {
        Definition = definition;
        ViewDefinition = viewDefinition;
        Team = team;
        SlotIndex = slotIndex;
        CurrentHP = definition != null ? definition.maxHP : 0;
    }

    public string Name
    {
        get
        {
            if (ViewDefinition != null && !string.IsNullOrWhiteSpace(ViewDefinition.displayNameOverride))
                return ViewDefinition.displayNameOverride;

            return Definition != null ? Definition.unitName : "Unknown";
        }
    }

    public CharacterRangeType RangeType => Definition != null ? Definition.rangeType : CharacterRangeType.Melee;

    public int Level => Definition != null ? Definition.level : 1;
    public int Exp => Definition != null ? Definition.exp : 0;

    public int MaxHP => Definition != null ? Definition.maxHP : 0;
    public int DMG => Definition != null ? Definition.dmg : 0;
    public int SPD => Definition != null ? Definition.spd : 0;

    public float HIT => Definition != null ? Definition.hit : 0f;
    public float AC => Definition != null ? Definition.ac : 0f;
    public float CRI => Definition != null ? Definition.cri : 0f;
    public float CRD => Definition != null ? Definition.crd : 0f;

    public float PoisonResist => Definition != null ? Definition.poisonResist : 0f;
    public float BleedResist => Definition != null ? Definition.bleedResist : 0f;
    public float StunResist => Definition != null ? Definition.stunResist : 0f;

    public BattleUnitView ViewPrefab => ViewDefinition != null ? ViewDefinition.unitViewPrefab : null;
    public Sprite PortraitSprite => ViewDefinition != null ? ViewDefinition.portraitSprite : null;
    public Sprite BodySprite => ViewDefinition != null ? ViewDefinition.bodySprite : null;

    public void TakeDamage(int amount)
    {
        CurrentHP = Mathf.Max(0, CurrentHP - amount);
    }

    public void Heal(int amount)
    {
        CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
    }

    public override string ToString()
    {
        return $"{Name}({Team}) [Slot:{SlotIndex + 1}, HP:{CurrentHP}/{MaxHP}]";
    }
}