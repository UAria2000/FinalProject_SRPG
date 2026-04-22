using UnityEngine;

public class WorldMapSceneStartup : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WorldRunManager worldRunManager;
    [SerializeField] private SaveCoordinator saveCoordinator;

    [Header("Difficulty Settings")]
    [SerializeField] private WorldGenerationSettings easySettings;
    [SerializeField] private WorldGenerationSettings normalSettings;
    [SerializeField] private WorldGenerationSettings hardSettings;

    [Header("Fallback")]
    [SerializeField] private string fallbackDifficultyId = "normal";
    [SerializeField] private int fallbackMapRadius = 5;

    private void Start()
    {
        if (saveCoordinator == null)
            saveCoordinator = SaveCoordinator.Instance;

        if (worldRunManager == null)
            worldRunManager = UnityEngine.Object.FindFirstObjectByType<WorldRunManager>();

        if (saveCoordinator == null || worldRunManager == null)
        {
            Debug.LogError("[WorldMapSceneStartup] Missing SaveCoordinator or WorldRunManager.");
            return;
        }

        saveCoordinator.RebindSceneReferences();
        saveCoordinator.LoadProfileIntoCurrentScene();

        if (saveCoordinator.ConsumeQueuedContinueWorld())
        {
            TryContinueWorld();
            return;
        }

        if (saveCoordinator.ConsumeQueuedNewWorldStart(out string newDifficultyId, out int newRadius))
        {
            StartNewWorld(newDifficultyId, newRadius);
            return;
        }

        if (saveCoordinator.HasSavedActiveWorld())
        {
            TryContinueWorld();
        }
        else
        {
            StartNewWorld(fallbackDifficultyId, fallbackMapRadius);
        }
    }

    private void TryContinueWorld()
    {
        ActiveWorldRunSaveData saveData = saveCoordinator.LoadWorldRunData();
        if (saveData == null || !saveData.hasActiveWorld)
        {
            StartNewWorld(fallbackDifficultyId, fallbackMapRadius);
            return;
        }

        WorldGenerationSettings settings = ResolveDifficultySettings(saveData.difficultyId);
        bool restored = worldRunManager.RestoreWorldRunFromSave(
            saveData,
            settings,
            saveCoordinator.ReferenceResolver);

        if (!restored)
        {
            Debug.LogWarning("[WorldMapSceneStartup] Restore failed. Starting a new world instead.");
            StartNewWorld(fallbackDifficultyId, fallbackMapRadius);
        }
    }

    private void StartNewWorld(string difficultyId, int radius)
    {
        WorldGenerationSettings settings = ResolveDifficultySettings(difficultyId);
        if (settings == null)
        {
            Debug.LogError("[WorldMapSceneStartup] Difficulty settings asset is missing.");
            return;
        }

        worldRunManager.StartNewWorldFromSetup(settings, difficultyId, radius);
        saveCoordinator.SaveAll();
    }

    private WorldGenerationSettings ResolveDifficultySettings(string difficultyId)
    {
        string key = string.IsNullOrWhiteSpace(difficultyId) ? fallbackDifficultyId : difficultyId.Trim().ToLowerInvariant();

        switch (key)
        {
            case "easy":
            case "쉬움":
                return easySettings != null ? easySettings : normalSettings;

            case "hard":
            case "어려움":
                return hardSettings != null ? hardSettings : normalSettings;

            case "normal":
            case "medium":
            case "보통":
            default:
                return normalSettings != null ? normalSettings : easySettings;
        }
    }
}