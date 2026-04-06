using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "World/World Generation Settings", fileName = "WorldGenerationSettings")]
public class WorldGenerationSettings : ScriptableObject
{
    [Header("World")]
    [Range(3, 6)] public int radius = 3;
    public List<FactionType> enemyFactions = new List<FactionType> { FactionType.FactionA, FactionType.FactionB };

    [Header("Fixed Rules")]
    [Min(0)] public int graveyardCount = 1;
    [Min(1)] public int minCombatPreviewCount = 1;
    [Min(1)] public int maxCombatPreviewCount = 6;
    public bool forbidBossNearCenter = true;
    public bool forbidEliteNearCenter = true;

    [Header("Generation Retry")]
    [Min(1)] public int maxGenerationAttempts = 200;

    [Header("Presentation")]
    public List<WorldFactionPresentationEntry> factionPresentationEntries = new List<WorldFactionPresentationEntry>();
    public List<WorldEventPresentationEntry> eventPresentationEntries = new List<WorldEventPresentationEntry>();
    public WorldEventWeightSettings eventWeightSettings;

    public string GetFactionDisplayName(FactionType factionType)
    {
        WorldFactionPresentationEntry entry = FindFactionEntry(factionType);
        return entry != null && !string.IsNullOrWhiteSpace(entry.displayName) ? entry.displayName : factionType.ToString();
    }

    public Sprite GetFactionTileSprite(FactionType factionType)
    {
        WorldFactionPresentationEntry entry = FindFactionEntry(factionType);
        return entry != null ? entry.tileSprite : null;
    }

    public Color GetFactionFallbackColor(FactionType factionType)
    {
        WorldFactionPresentationEntry entry = FindFactionEntry(factionType);
        return entry != null ? entry.fallbackColor : Color.white;
    }

    public string GetEventDisplayName(WorldTileEventType eventType)
    {
        WorldEventPresentationEntry entry = FindEventEntry(eventType);
        return entry != null && !string.IsNullOrWhiteSpace(entry.displayName) ? entry.displayName : eventType.ToString();
    }

    public string GetEventDescription(WorldTileEventType eventType)
    {
        WorldEventPresentationEntry entry = FindEventEntry(eventType);
        return entry != null ? entry.description : string.Empty;
    }

    public Sprite GetEventIcon(WorldTileEventType eventType)
    {
        WorldEventPresentationEntry entry = FindEventEntry(eventType);
        return entry != null ? entry.icon : null;
    }

    public List<Sprite> GetPreviewPortraitPool(FactionType factionType, WorldTileEventType eventType)
    {
        WorldFactionPresentationEntry entry = FindFactionEntry(factionType);
        if (entry == null)
            return null;

        if (eventType == WorldTileEventType.Boss && entry.bossPreviewPortraits != null && entry.bossPreviewPortraits.Count > 0)
            return entry.bossPreviewPortraits;

        if (eventType == WorldTileEventType.EliteBattle && entry.elitePreviewPortraits != null && entry.elitePreviewPortraits.Count > 0)
            return entry.elitePreviewPortraits;

        return entry.battlePreviewPortraits;
    }

    private WorldFactionPresentationEntry FindFactionEntry(FactionType factionType)
    {
        for (int i = 0; i < factionPresentationEntries.Count; i++)
        {
            if (factionPresentationEntries[i] != null && factionPresentationEntries[i].factionType == factionType)
                return factionPresentationEntries[i];
        }

        return null;
    }

    private WorldEventPresentationEntry FindEventEntry(WorldTileEventType eventType)
    {
        for (int i = 0; i < eventPresentationEntries.Count; i++)
        {
            if (eventPresentationEntries[i] != null && eventPresentationEntries[i].eventType == eventType)
                return eventPresentationEntries[i];
        }

        return null;
    }
}
