using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "World Map/Generation Settings", fileName = "WorldGenerationSettings")]
public class WorldGenerationSettings : ScriptableObject
{
    [Header("World")]
    [Range(3, 6)] public int radius = 3;
    public WorldDifficulty difficulty = WorldDifficulty.Normal;
    [Min(1)] public int maxGenerationAttempts = 250;
    [Min(1)] public int enemyPortraitMinCount = 1;
    [Range(1, 6)] public int enemyPortraitMaxCount = 6;

    [Header("Factions")]
    public List<FactionType> enemyFactions = new List<FactionType> { FactionType.FactionA, FactionType.FactionB };
    public List<FactionPresentation> factionPresentations = new List<FactionPresentation>();

    [Header("Events")]
    public WorldEventWeightSettings eventWeightSettings;
    public List<WorldEventPresentation> eventPresentations = new List<WorldEventPresentation>();
    [SerializeField] private Sprite startTileIcon;
    public Sprite StartTileIcon => startTileIcon;

    [Header("Battle Event Config")]
    public List<FactionBattleConfig> factionBattleConfigs = new List<FactionBattleConfig>();

    [Header("Settlement & Victory")]
    [Range(0,100)] public int conquestRequiredPercentSmall = 50;
    [Range(0,100)] public int conquestRequiredPercentMedium = 55;
    [Range(0,100)] public int conquestRequiredPercentLarge = 60;
    [Range(0,100)] public int conquestRequiredPercentXLarge = 65;
    public int sizeBonusPercentSmall = 0;
    public int sizeBonusPercentMedium = 10;
    public int sizeBonusPercentLarge = 20;
    public int sizeBonusPercentXLarge = 30;
    public int difficultyBonusPercentEasy = 0;
    public int difficultyBonusPercentNormal = 10;
    public int difficultyBonusPercentHard = 20;
    public int worldVictoryBonusPercent = 20;

    [Header("Rules")]
    public bool forbidBossNearCenter = true;
    public bool forbidEliteNearCenter = true;

    public Sprite GetFactionTileSprite(FactionType faction)
    {
        FactionPresentation presentation = GetFactionPresentation(faction);
        return presentation != null ? presentation.tileSprite : null;
    }

    public Color GetFactionFallbackColor(FactionType faction)
    {
        FactionPresentation presentation = GetFactionPresentation(faction);
        return presentation != null ? presentation.fallbackColor : Color.white;
    }

    public string GetFactionDisplayName(FactionType faction)
    {
        FactionPresentation presentation = GetFactionPresentation(faction);
        if (presentation != null && !string.IsNullOrWhiteSpace(presentation.displayName))
            return presentation.displayName;
        return faction.ToString();
    }

    public Sprite GetFactionUnknownSprite(FactionType faction)
    {
        FactionPresentation presentation = GetFactionPresentation(faction);
        return presentation != null ? presentation.unknownSprite : null;
    }

    public IReadOnlyList<Sprite> GetFactionEnemyPortraitPool(FactionType faction)
    {
        FactionPresentation presentation = GetFactionPresentation(faction);
        return presentation != null ? presentation.enemyPortraitPool : Array.Empty<Sprite>();
    }

    public Sprite GetTileDisplayIcon(WorldTileData tile)
    {
        if (tile == null)
            return null;

        if (tile.isPlayerStart && StartTileIcon != null)
            return StartTileIcon;

        WorldEventPresentation presentation = GetEventPresentation(tile.eventType);
        if (presentation == null)
            return null;

        if (tile.currentOwner == FactionType.Player && presentation.iconDark != null)
            return presentation.iconDark;

        return presentation.icon;
    }

    public Sprite GetQuestionMarkSprite(WorldTileData tile)
    {
        if (tile == null)
            return null;

        FactionType questionFaction = tile.nativeFaction != FactionType.None ? tile.nativeFaction : tile.currentOwner;
        return GetFactionUnknownSprite(questionFaction);
    }

    public Sprite GetEventIcon(WorldTileEventType eventType)
    {
        WorldEventPresentation presentation = GetEventPresentation(eventType);
        return presentation != null ? presentation.icon : null;
    }

    public Sprite GetEventDarkIcon(WorldTileEventType eventType)
    {
        WorldEventPresentation presentation = GetEventPresentation(eventType);
        return presentation != null ? presentation.iconDark : null;
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

    public FactionBattleConfig GetFactionBattleConfig(FactionType faction)
    {
        for (int i = 0; i < factionBattleConfigs.Count; i++)
        {
            FactionBattleConfig config = factionBattleConfigs[i];
            if (config != null && config.faction == faction)
                return config;
        }

        return null;
    }

    public int GetConquestRequiredPercent()
    {
        switch (radius)
        {
            case 3: return conquestRequiredPercentSmall;
            case 4: return conquestRequiredPercentMedium;
            case 5: return conquestRequiredPercentLarge;
            default: return conquestRequiredPercentXLarge;
        }
    }

    public int GetSizeBonusPercent()
    {
        switch (radius)
        {
            case 3: return sizeBonusPercentSmall;
            case 4: return sizeBonusPercentMedium;
            case 5: return sizeBonusPercentLarge;
            default: return sizeBonusPercentXLarge;
        }
    }

    public int GetDifficultyBonusPercent()
    {
        switch (difficulty)
        {
            case WorldDifficulty.Easy: return difficultyBonusPercentEasy;
            case WorldDifficulty.Hard: return difficultyBonusPercentHard;
            default: return difficultyBonusPercentNormal;
        }
    }


    public int GetBattleRewardSizeBonusPercent()
    {
        // radius 3은 초소형 테스트맵으로 취급한다. 실제 보상 기준은 4/5/6이다.
        if (radius <= 4)
            return 0;
        if (radius == 5)
            return 50;
        return 100;
    }

    public int GetBattleRewardCombatBonusPercent(WorldTileEventType eventType)
    {
        if (eventType == WorldTileEventType.EliteBattle)
            return 20;
        if (eventType == WorldTileEventType.Boss)
            return 50;
        return 0;
    }

    private FactionPresentation GetFactionPresentation(FactionType faction)
    {
        for (int i = 0; i < factionPresentations.Count; i++)
        {
            if (factionPresentations[i] != null && factionPresentations[i].faction == faction)
                return factionPresentations[i];
        }
        return null;
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
    public Sprite unknownSprite;
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
    public Sprite iconDark;
}

[Serializable]
public class FactionBattleConfig
{
    public FactionType faction = FactionType.None;

    [Header("Normal Battle Tables")]
    public EnemyEncounterTable battleTier1Table;
    public EnemyEncounterTable battleTier2Table;
    public EnemyEncounterTable battleTier3Table;

    [Header("Elite Battle Tables")]
    public EnemyEncounterTable eliteTier1Table;
    public EnemyEncounterTable eliteTier2Table;
    public EnemyEncounterTable eliteTier3Table;

    [Header("Boss")]
    public PartyDefinition bossPartyDefinition;
    public EnemyEncounterTable bossEncounterTable;

    public EnemyEncounterTable GetEncounterTable(WorldTileEventType eventType, int tierIndex)
    {
        int tier = Mathf.Clamp(tierIndex, 0, 2);

        if (eventType == WorldTileEventType.Battle)
        {
            switch (tier)
            {
                case 0: return battleTier1Table;
                case 1: return battleTier2Table;
                default: return battleTier3Table;
            }
        }

        if (eventType == WorldTileEventType.EliteBattle)
        {
            switch (tier)
            {
                case 0: return eliteTier1Table;
                case 1: return eliteTier2Table;
                default: return eliteTier3Table;
            }
        }

        if (eventType == WorldTileEventType.Boss)
            return bossEncounterTable;

        return null;
    }
}
