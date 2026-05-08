using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class SaveCoordinator : MonoBehaviour
{
    private static SaveCoordinator instance;
    public static SaveCoordinator Instance => instance;

    [Header("Identity")]
    [SerializeField] private string accountId = "default";
    [SerializeField] private string nickname = "Player";

    [Header("References")]
    [SerializeField] private PersistentProfileController persistentProfileController;
    [SerializeField] private WorldRunManager worldRunManager;
    [SerializeField] private WorldQuestController worldQuestController;
    [SerializeField] private SaveReferenceResolver referenceResolver;

    [Header("Persistent Inventory (Prototype)")]
    [SerializeField] private List<PersistentInventoryItemSaveData> persistentInventory = new();

    private bool queuedContinueWorld;
    private bool queuedNewWorld;
    private string queuedDifficultyId;
    private int queuedMapRadius;

    public string AccountId => accountId;
    public string Nickname => nickname;
    public SaveReferenceResolver ReferenceResolver => referenceResolver;
    public PersistentProfileController PersistentProfileController => persistentProfileController;
    public WorldRunManager WorldRunManager => worldRunManager;
    public WorldQuestController WorldQuestController => worldQuestController;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        RebindSceneReferences();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RebindSceneReferences();
    }

    public void RebindSceneReferences()
    {
        if (persistentProfileController == null)
            persistentProfileController = UnityEngine.Object.FindFirstObjectByType<PersistentProfileController>();

        if (worldRunManager == null)
            worldRunManager = UnityEngine.Object.FindFirstObjectByType<WorldRunManager>();

        if (worldQuestController == null)
            worldQuestController = UnityEngine.Object.FindFirstObjectByType<WorldQuestController>();

        if (referenceResolver == null)
            referenceResolver = UnityEngine.Object.FindFirstObjectByType<SaveReferenceResolver>();
    }

    public void SetAccountIdentity(string newAccountId, string newNickname)
    {
        if (!string.IsNullOrWhiteSpace(newAccountId))
            accountId = newAccountId.Trim();

        if (!string.IsNullOrWhiteSpace(newNickname))
            nickname = newNickname.Trim();
    }

    public void SaveAll()
    {
        SaveProfile();
        SaveWorldRun();
    }

    public void SaveProfile()
    {
        RebindSceneReferences();

        AccountProfileSaveData data = SaveDataMapper.CaptureProfile(
            accountId,
            nickname,
            persistentProfileController,
            worldRunManager,
            persistentInventory);

        LocalSaveService.SaveProfile(data);
    }

    public void SaveWorldRun()
    {
        RebindSceneReferences();

        ActiveWorldRunSaveData data = SaveDataMapper.CaptureWorldRun(
            accountId,
            worldRunManager,
            worldQuestController);

        LocalSaveService.SaveWorldRun(data);
    }

    public AccountProfileSaveData LoadProfileData()
    {
        return LocalSaveService.LoadProfile(accountId);
    }

    public ActiveWorldRunSaveData LoadWorldRunData()
    {
        return LocalSaveService.LoadWorldRun(accountId);
    }

    public bool HasSavedActiveWorld()
    {
        ActiveWorldRunSaveData data = LoadWorldRunData();
        return data != null && data.hasActiveWorld;
    }

    public void LoadProfileIntoCurrentScene()
    {
        RebindSceneReferences();

        AccountProfileSaveData data = LoadProfileData();
        if (data == null)
            return;

        nickname = string.IsNullOrWhiteSpace(data.nickname) ? nickname : data.nickname;

        if (data.persistentInventory != null)
            persistentInventory = new List<PersistentInventoryItemSaveData>(data.persistentInventory);

        SaveDataMapper.ApplyProfileToCurrentRuntime(
            data,
            persistentProfileController,
            worldRunManager,
            referenceResolver);

        persistentProfileController?.RebuildActivePartyFromSavedIds(data.activePartyUnitInstanceIds);
    }

    public void ClearSavedWorldRun()
    {
        LocalSaveService.DeleteWorldRun(accountId);
    }

    public void ClearSavedWorldRunAsAbandoned()
    {
        if (HasSavedActiveWorld())
            SetLastWorldSettlementResult(WorldSettlementResultState.Failure);

        LocalSaveService.DeleteWorldRun(accountId);
    }

    public void SetLastWorldSettlementResult(WorldSettlementResultState result)
    {
        RebindSceneReferences();

        if (persistentProfileController != null)
        {
            persistentProfileController.EnsureInitialized();
            if (persistentProfileController.Profile != null)
                persistentProfileController.Profile.lastWorldSettlementResult = result;
            SaveProfile();
            return;
        }

        AccountProfileSaveData data = LoadProfileData();
        if (data == null)
        {
            data = new AccountProfileSaveData
            {
                accountId = accountId,
                nickname = nickname,
            };
        }

        data.lastWorldSettlementResult = result;
        LocalSaveService.SaveProfile(data);
    }

    public void QueueContinueWorld()
    {
        queuedContinueWorld = true;
        queuedNewWorld = false;
        queuedDifficultyId = null;
        queuedMapRadius = 0;
    }

    public void QueueNewWorldStart(string difficultyId, int mapRadius)
    {
        queuedContinueWorld = false;
        queuedNewWorld = true;
        queuedDifficultyId = difficultyId;
        queuedMapRadius = mapRadius;
    }

    public bool ConsumeQueuedContinueWorld()
    {
        bool value = queuedContinueWorld;
        queuedContinueWorld = false;
        return value;
    }

    public bool ConsumeQueuedNewWorldStart(out string difficultyId, out int mapRadius)
    {
        if (!queuedNewWorld)
        {
            difficultyId = null;
            mapRadius = 0;
            return false;
        }

        difficultyId = queuedDifficultyId;
        mapRadius = queuedMapRadius;

        queuedNewWorld = false;
        queuedDifficultyId = null;
        queuedMapRadius = 0;
        return true;
    }
}