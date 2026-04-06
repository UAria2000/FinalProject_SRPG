using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "World/World Event Weight Settings", fileName = "WorldEventWeightSettings")]
public class WorldEventWeightSettings : ScriptableObject
{
    [SerializeField] private List<WorldEventWeightEntry> entries = new List<WorldEventWeightEntry>
    {
        new WorldEventWeightEntry { eventType = WorldTileEventType.Battle, weight = 50f },
        new WorldEventWeightEntry { eventType = WorldTileEventType.Rest, weight = 10f },
        new WorldEventWeightEntry { eventType = WorldTileEventType.Treasure, weight = 12f },
        new WorldEventWeightEntry { eventType = WorldTileEventType.Merchant, weight = 8f },
        new WorldEventWeightEntry { eventType = WorldTileEventType.Quest, weight = 10f },
        new WorldEventWeightEntry { eventType = WorldTileEventType.EliteBattle, weight = 10f },
    };

    public IReadOnlyList<WorldEventWeightEntry> Entries => entries;

    public float GetWeight(WorldTileEventType eventType)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].eventType == eventType)
                return Mathf.Max(0f, entries[i].weight);
        }

        return 0f;
    }
}
