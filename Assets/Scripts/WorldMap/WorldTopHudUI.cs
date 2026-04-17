using TMPro;
using UnityEngine;

public class WorldTopHudUI : MonoBehaviour
{
    [SerializeField] private WorldRunManager worldRunManager;
    [SerializeField] private WorldGenerationSettings generationSettings;
    [SerializeField] private TMP_Text worldTitleText;
    [SerializeField] private TMP_Text soulText;
    [SerializeField] private TMP_Text cashText;

    private void Awake()
    {
        if (worldRunManager == null)
            worldRunManager = Object.FindFirstObjectByType<WorldRunManager>();
    }

    private void OnEnable()
    {
        if (worldRunManager != null)
        {
            worldRunManager.OnWorldStateChanged += Refresh;
            worldRunManager.OnStorageChanged += Refresh;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (worldRunManager != null)
        {
            worldRunManager.OnWorldStateChanged -= Refresh;
            worldRunManager.OnStorageChanged -= Refresh;
        }
    }

    public void Initialize(WorldRunManager manager, WorldGenerationSettings settings)
    {
        worldRunManager = manager;
        generationSettings = settings;
        Refresh();
    }

    public void Refresh()
    {
        if (worldRunManager != null && generationSettings == null)
            generationSettings = worldRunManager.Settings;

        if (worldTitleText != null)
        {
            string sizeText = generationSettings != null ? GetSizeLabel(generationSettings.radius) : string.Empty;
            string difficultyText = generationSettings != null ? GetDifficultyLabel(generationSettings.difficulty) : string.Empty;
            worldTitleText.text = $"월드맵 {sizeText} - {difficultyText}";
        }

        if (soulText != null)
            soulText.text = worldRunManager != null ? worldRunManager.PersistentSoul.ToString("N0") : "0";

        if (cashText != null)
            cashText.text = worldRunManager != null ? worldRunManager.PersistentCash.ToString("N0") : "0";
    }

    private string GetSizeLabel(int radius)
    {
        switch (radius)
        {
            case 3: return "소형";
            case 4: return "중형";
            case 5: return "대형";
            default: return "초대형";
        }
    }

    private string GetDifficultyLabel(WorldDifficulty difficulty)
    {
        switch (difficulty)
        {
            case WorldDifficulty.Easy: return "쉬움";
            case WorldDifficulty.Hard: return "어려움";
            default: return "보통";
        }
    }
}
