using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "World Map/Generation Settings", fileName = "WorldGenerationSettings")]
public class WorldGenerationSettings : ScriptableObject
{
    [Header("World")]
    [Range(3, 6)] public int radius = 3;
    [Min(1)] public int maxGenerationAttempts = 250;
    [Min(1)] public int enemyPortraitMinCount = 1;
    [Range(1, 6)] public int enemyPortraitMaxCount = 6;

    [Header("Factions")]
    public List<FactionType> enemyFactions = new List<FactionType> { FactionType.FactionA, FactionType.FactionB };
    public List<FactionPresentation> factionPresentations = new List<FactionPresentation>();

    [Header("Events")]
    public WorldEventWeightSettings eventWeightSettings;
    public List<WorldEventPresentation> eventPresentations = new List<WorldEventPresentation>();

    [Header("Rules")]
    public bool forbidBossNearCenter = true;
    public bool forbidEliteNearCenter = true;

    public Sprite GetFactionTileSprite(FactionType faction)
    {
        for (int i = 0; i < factionPresentations.Count; i++)
        {
            if (factionPresentations[i] != null && factionPresentations[i].faction == faction)
                return factionPresentations[i].tileSprite;
        }
        return null;
    }

    public Color GetFactionFallbackColor(FactionType faction)
    {
        for (int i = 0; i < factionPresentations.Count; i++)
        {
            if (factionPresentations[i] != null && factionPresentations[i].faction == faction)
                return factionPresentations[i].fallbackColor;
        }
        return Color.white;
    }

    public string GetFactionDisplayName(FactionType faction)
    {
        for (int i = 0; i < factionPresentations.Count; i++)
        {
            if (factionPresentations[i] != null && factionPresentations[i].faction == faction)
            {
                if (!string.IsNullOrWhiteSpace(factionPresentations[i].displayName))
                    return factionPresentations[i].displayName;
            }
        }
        return faction.ToString();
    }

    public IReadOnlyList<Sprite> GetFactionEnemyPortraitPool(FactionType faction)
    {
        for (int i = 0; i < factionPresentations.Count; i++)
        {
            if (factionPresentations[i] != null && factionPresentations[i].faction == faction)
                return factionPresentations[i].enemyPortraitPool;
        }
        return Array.Empty<Sprite>();
    }

    public Sprite GetEventIcon(WorldTileEventType eventType)
    {
        WorldEventPresentation presentation = GetEventPresentation(eventType);
        return presentation != null ? presentation.icon : null;
    }

    public string GetEventDisplayName(WorldTileEventType eventType)
    {
        WorldEventPresentation presentation = GetEventPresentation(eventType);
        if (presentation != null && !string.IsNullOrWhiteSpace(presentation.displayName))
            return presentation.displayName;
        return eventType.ToString();
    }

    public string GetEventDescription(WorldTileEventType eventType)
    {
        WorldEventPresentation presentation = GetEventPresentation(eventType);
        if (presentation != null)
            return presentation.description;
        return string.Empty;
    }

    private WorldEventPresentation GetEventPresentation(WorldTileEventType eventType)
    {
        for (int i = 0; i < eventPresentations.Count; i++)
        {
            if (eventPresentations[i] != null && eventPresentations[i].eventType == eventType)
                return eventPresentations[i];
        }
        return null;
    }
}

[Serializable]
public class FactionPresentation
{
    public FactionType faction = FactionType.None;
    public string displayName;
    public Sprite tileSprite;
    public Color fallbackColor = Color.white;
    public List<Sprite> enemyPortraitPool = new List<Sprite>();
}

[Serializable]
public class WorldEventPresentation
{
    public WorldTileEventType eventType = WorldTileEventType.None;
    public string displayName;
    [TextArea(2, 5)] public string description;
    public Sprite icon;
}
