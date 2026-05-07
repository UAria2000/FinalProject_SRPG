using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Battle/Skill Learn Pool Table")]
public class SkillLearnPoolTable : ScriptableObject
{
    [Header("Class Skill Pools")]
    public List<SkillDefinition> meleeSkills = new List<SkillDefinition>();
    public List<SkillDefinition> midSkills = new List<SkillDefinition>();
    public List<SkillDefinition> rangedSkills = new List<SkillDefinition>();
    public List<SkillDefinition> commonSkills = new List<SkillDefinition>();

    public IReadOnlyList<SkillDefinition> GetClassSkills(CharacterRangeType rangeType)
    {
        switch (rangeType)
        {
            case CharacterRangeType.Mid:
                return midSkills;
            case CharacterRangeType.Ranged:
                return rangedSkills;
            default:
                return meleeSkills;
        }
    }
}
