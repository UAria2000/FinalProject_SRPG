using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class SaveReferenceResolver : MonoBehaviour
{
    public static SaveReferenceResolver Instance { get; private set; }

    [Header("Definitions")]
    [SerializeField] private List<UnitDefinition> unitDefinitions = new List<UnitDefinition>();
    [SerializeField] private List<UnitViewDefinition> unitViewDefinitions = new List<UnitViewDefinition>();
    [SerializeField] private List<ItemDefinition> itemDefinitions = new List<ItemDefinition>();
    [SerializeField] private List<SkillDefinition> skillDefinitions = new List<SkillDefinition>();

#if UNITY_EDITOR
    [Header("Editor Auto Populate")]
    [SerializeField] private string[] unitDefinitionFolders = { "Assets/UnitDefinition" };
    [SerializeField] private string[] unitViewDefinitionFolders = { "Assets/UnitViewDefinition" };
    [SerializeField] private string[] itemDefinitionFolders = { "Assets/ItemDefinition" };
    [SerializeField] private string[] skillDefinitionFolders = { "Assets/SkillDefinition" };
#endif

    private readonly Dictionary<string, UnitDefinition> unitById = new Dictionary<string, UnitDefinition>();
    private readonly Dictionary<string, UnitViewDefinition> unitViewByName = new Dictionary<string, UnitViewDefinition>();
    private readonly Dictionary<string, ItemDefinition> itemById = new Dictionary<string, ItemDefinition>();
    private readonly Dictionary<string, SkillDefinition> skillById = new Dictionary<string, SkillDefinition>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        RebuildLookup();
    }

    private void OnValidate()
    {
        RebuildLookup();
    }

    public void RebuildLookup()
    {
        unitById.Clear();
        unitViewByName.Clear();
        itemById.Clear();
        skillById.Clear();

        for (int i = 0; i < unitDefinitions.Count; i++)
        {
            UnitDefinition def = unitDefinitions[i];
            if (def == null || string.IsNullOrWhiteSpace(def.unitId))
                continue;

            if (!unitById.ContainsKey(def.unitId))
                unitById.Add(def.unitId, def);
        }

        for (int i = 0; i < unitViewDefinitions.Count; i++)
        {
            UnitViewDefinition def = unitViewDefinitions[i];
            if (def == null || string.IsNullOrWhiteSpace(def.name))
                continue;

            if (!unitViewByName.ContainsKey(def.name))
                unitViewByName.Add(def.name, def);
        }

        for (int i = 0; i < itemDefinitions.Count; i++)
        {
            ItemDefinition def = itemDefinitions[i];
            if (def == null || string.IsNullOrWhiteSpace(def.itemId))
                continue;

            if (!itemById.ContainsKey(def.itemId))
                itemById.Add(def.itemId, def);
        }

        for (int i = 0; i < skillDefinitions.Count; i++)
        {
            SkillDefinition def = skillDefinitions[i];
            if (def == null || string.IsNullOrWhiteSpace(def.skillId))
                continue;

            if (!skillById.ContainsKey(def.skillId))
                skillById.Add(def.skillId, def);
        }
    }

    public UnitDefinition FindUnitDefinition(string unitId)
    {
        if (string.IsNullOrWhiteSpace(unitId))
            return null;

        if (unitById.TryGetValue(unitId, out UnitDefinition result))
            return result;

        return null;
    }

    public UnitViewDefinition FindUnitViewDefinition(string definitionName)
    {
        if (string.IsNullOrWhiteSpace(definitionName))
            return null;

        if (unitViewByName.TryGetValue(definitionName, out UnitViewDefinition result))
            return result;

        return null;
    }

    public ItemDefinition FindItemDefinition(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return null;

        if (itemById.TryGetValue(itemId, out ItemDefinition result))
            return result;

        return null;
    }

    public SkillDefinition FindSkillDefinition(string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId))
            return null;

        if (skillById.TryGetValue(skillId, out SkillDefinition result))
            return result;

        return null;
    }

#if UNITY_EDITOR
    [ContextMenu("Auto Populate From Fixed Folders")]
    public void AutoPopulateFromFixedFolders()
    {
        unitDefinitions = LoadAssets<UnitDefinition>(unitDefinitionFolders);
        unitViewDefinitions = LoadAssets<UnitViewDefinition>(unitViewDefinitionFolders);
        itemDefinitions = LoadAssets<ItemDefinition>(itemDefinitionFolders);
        skillDefinitions = LoadAssets<SkillDefinition>(skillDefinitionFolders);

        RebuildLookup();
        EditorUtility.SetDirty(this);

        if (!Application.isPlaying)
            EditorSceneMarkDirty();
    }

    private static List<T> LoadAssets<T>(string[] folders) where T : UnityEngine.Object
    {
        List<T> results = new List<T>();
        if (folders == null || folders.Length == 0)
            return results;

        string filter = $"t:{typeof(T).Name}";
        string[] guids = AssetDatabase.FindAssets(filter, folders);

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null && !results.Contains(asset))
                results.Add(asset);
        }

        return results;
    }

    private static void EditorSceneMarkDirty()
    {
        if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().IsValid())
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
    }
#endif
}