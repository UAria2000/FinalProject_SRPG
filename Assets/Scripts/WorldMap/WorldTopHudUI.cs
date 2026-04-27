using System.Collections;
using TMPro;
using UnityEngine;

public class WorldTopHudUI : MonoBehaviour
{
    [SerializeField] private WorldRunManager worldRunManager;
    [SerializeField] private PersistentProfileController persistentProfileController;
    [SerializeField] private WorldGenerationSettings generationSettings;
    [SerializeField] private TMP_Text worldTitleText;
    [SerializeField] private TMP_Text soulText;
    [SerializeField] private TMP_Text cashText;

    [Header("Legion Shard HUD")]
    [Tooltip("레기온 패널이 열려 있을 때만 켤 파편 표시 루트. 비워도 동작은 한다.")]
    [SerializeField] private GameObject legionShardRoot;
    [SerializeField] private TMP_Text legionShardText;

    [Header("Gain Feedback")]
    [SerializeField] private TMP_Text soulGainText;
    [SerializeField] private TMP_Text shardGainText;
    [SerializeField] private CanvasGroup soulGainCanvasGroup;
    [SerializeField] private CanvasGroup shardGainCanvasGroup;
    [SerializeField] private float gainFadeDuration = 1.15f;
    [SerializeField] private Color gainTextColor = new Color(0.35f, 1f, 0.35f, 1f);

    private bool legionShardVisible;
    private Coroutine soulGainRoutine;
    private Coroutine shardGainRoutine;

    private void Awake()
    {
        if (worldRunManager == null)
            worldRunManager = Object.FindFirstObjectByType<WorldRunManager>();
        if (persistentProfileController == null)
            persistentProfileController = Object.FindFirstObjectByType<PersistentProfileController>();
    }

    private void OnEnable()
    {
        if (worldRunManager != null)
        {
            worldRunManager.OnWorldStateChanged += Refresh;
            worldRunManager.OnStorageChanged += Refresh;
        }

        if (persistentProfileController != null)
            persistentProfileController.OnProfileChanged += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        if (worldRunManager != null)
        {
            worldRunManager.OnWorldStateChanged -= Refresh;
            worldRunManager.OnStorageChanged -= Refresh;
        }

        if (persistentProfileController != null)
            persistentProfileController.OnProfileChanged -= Refresh;
    }

    public void Initialize(WorldRunManager manager, WorldGenerationSettings settings)
    {
        worldRunManager = manager;
        generationSettings = settings;
        Refresh();
    }

    public void SetLegionShardVisible(bool visible)
    {
        legionShardVisible = visible;
        if (legionShardRoot != null)
            legionShardRoot.SetActive(visible);
        Refresh();
    }

    public void Refresh()
    {
        if (worldRunManager != null && generationSettings == null)
            generationSettings = worldRunManager.Settings;

        if (persistentProfileController == null)
            persistentProfileController = Object.FindFirstObjectByType<PersistentProfileController>();

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

        if (legionShardRoot != null)
            legionShardRoot.SetActive(legionShardVisible);

        if (legionShardText != null)
            legionShardText.text = persistentProfileController != null ? persistentProfileController.GetUnitShardCount().ToString("N0") : "0";
    }

    public void ShowTemporaryGain(int soulGain, int shardGain)
    {
        if (soulGain > 0 && soulGainText != null)
        {
            if (soulGainRoutine != null)
                StopCoroutine(soulGainRoutine);
            soulGainRoutine = StartCoroutine(FadeGainText(soulGainText, soulGainCanvasGroup, soulGain));
        }

        if (shardGain > 0 && shardGainText != null)
        {
            if (shardGainRoutine != null)
                StopCoroutine(shardGainRoutine);
            shardGainRoutine = StartCoroutine(FadeGainText(shardGainText, shardGainCanvasGroup, shardGain));
        }
    }

    private IEnumerator FadeGainText(TMP_Text text, CanvasGroup group, int amount)
    {
        if (text == null)
            yield break;

        text.text = $"(+{amount:N0})";
        text.color = gainTextColor;
        text.gameObject.SetActive(true);

        if (group != null)
            group.alpha = 1f;

        float duration = Mathf.Max(0.05f, gainFadeDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            if (group != null)
                group.alpha = alpha;
            else
            {
                Color c = text.color;
                c.a = alpha;
                text.color = c;
            }
            yield return null;
        }

        if (group != null)
            group.alpha = 0f;
        text.gameObject.SetActive(false);
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
