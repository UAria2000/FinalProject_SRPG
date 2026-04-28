using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 월드맵 휴식 이벤트의 회복 방식.
/// </summary>
public enum WorldRestHealMode
{
    /// <summary>대상 체력을 최대 체력까지 회복.</summary>
    FullHeal = 0,

    /// <summary>최대 체력의 일정 비율만큼 회복.</summary>
    PercentOfMaxHp = 1,

    /// <summary>고정 수치만큼 회복.</summary>
    FlatAmount = 2,

    /// <summary>고정 수치 + 최대 체력 비율만큼 회복.</summary>
    FlatAndPercentOfMaxHp = 3,
}

/// <summary>
/// 휴식 이벤트 적용/미리보기에서 유닛 1명의 결과.
/// </summary>
[Serializable]
public class WorldRestMemberResult
{
    public string displayName;
    public int beforeHP;
    public int afterHP;
    public int maxHP;
    public int healedAmount;
    public bool wasDead;
    public bool skipped;

    public bool WasInjured => !wasDead && beforeHP < maxHP;
}

/// <summary>
/// 휴식 이벤트 전체 결과.
/// </summary>
[Serializable]
public class WorldRestResult
{
    public List<WorldRestMemberResult> members = new List<WorldRestMemberResult>();
    public int totalHealed;
    public int affectedCount;
    public int skippedDeadCount;

    public bool HasParty => members != null && members.Count > 0;

    public bool HasAnyInjured
    {
        get
        {
            if (members == null)
                return false;

            for (int i = 0; i < members.Count; i++)
            {
                WorldRestMemberResult member = members[i];
                if (member != null && member.WasInjured)
                    return true;
            }

            return false;
        }
    }

    public void AddMember(WorldRestMemberResult member)
    {
        if (member == null)
            return;

        if (members == null)
            members = new List<WorldRestMemberResult>();

        members.Add(member);

        int healed = Mathf.Max(0, member.healedAmount);
        totalHealed += healed;

        if (!member.skipped && healed > 0)
            affectedCount++;

        if (member.skipped && member.wasDead)
            skippedDeadCount++;
    }
}