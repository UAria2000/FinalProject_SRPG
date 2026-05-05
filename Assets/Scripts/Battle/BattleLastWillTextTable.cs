using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Battle/Last Will Text Table")]
public class BattleLastWillTextTable : ScriptableObject
{
    [TextArea(2, 6)]
    public List<string> texts = new List<string>();

    public bool HasAnyText()
    {
        if (texts == null)
            return false;

        for (int i = 0; i < texts.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(texts[i]))
                return true;
        }

        return false;
    }

    public string GetRandomText()
    {
        if (texts == null || texts.Count <= 0)
            return string.Empty;

        List<string> candidates = new List<string>();
        for (int i = 0; i < texts.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(texts[i]))
                candidates.Add(texts[i]);
        }

        if (candidates.Count <= 0)
            return string.Empty;

        return candidates[Random.Range(0, candidates.Count)];
    }
}
