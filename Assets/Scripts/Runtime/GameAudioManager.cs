using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class GameAudioManager : MonoBehaviour
{
    private static GameAudioManager instance;
    public static GameAudioManager Instance => instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource uiSource;

    [Header("Volumes")]
    [Range(0f, 1f)] [SerializeField] private float masterVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float bgmVolume = 0.7f;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float uiVolume = 1f;

    [Header("UI SFX")]
    [SerializeField] private AudioClip buttonHoverSfx;
    [SerializeField] private AudioClip buttonClickSfx;
    [SerializeField] private bool autoBindButtons = true;

    [Header("BGM")]
    [SerializeField] private AudioClip bootstrapBgm;
    [SerializeField] private AudioClip worldMapBgm;
    [SerializeField] private AudioClip battleBgm;
    [SerializeField] private float bgmFadeSeconds = 1f;

    private Coroutine bgmRoutine;
    private AudioClip currentBgm;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureSources();
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Start()
    {
        PlayBgmForScene(SceneManager.GetActiveScene().name);
        if (autoBindButtons)
            UIButtonSoundEmitter.BindAllButtonsInScene();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            instance = null;
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayBgmForScene(scene.name);
        if (autoBindButtons)
            StartCoroutine(BindButtonsNextFrame());
    }

    private IEnumerator BindButtonsNextFrame()
    {
        yield return null;
        UIButtonSoundEmitter.BindAllButtonsInScene();
    }

    private void EnsureSources()
    {
        if (bgmSource == null)
            bgmSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();
        if (uiSource == null)
            uiSource = gameObject.AddComponent<AudioSource>();

        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        sfxSource.playOnAwake = false;
        uiSource.playOnAwake = false;
        RefreshVolumes();
    }

    private void RefreshVolumes()
    {
        if (bgmSource != null)
            bgmSource.volume = masterVolume * bgmVolume;
        if (sfxSource != null)
            sfxSource.volume = masterVolume * sfxVolume;
        if (uiSource != null)
            uiSource.volume = masterVolume * uiVolume;
    }

    private void PlayBgmForScene(string sceneName)
    {
        AudioClip clip = ResolveBgm(sceneName);
        if (clip == currentBgm)
            return;

        if (bgmRoutine != null)
            StopCoroutine(bgmRoutine);
        bgmRoutine = StartCoroutine(SwitchBgmRoutine(clip));
    }

    private AudioClip ResolveBgm(string sceneName)
    {
        string key = string.IsNullOrWhiteSpace(sceneName) ? string.Empty : sceneName.ToLowerInvariant();
        if (key.Contains("battle"))
            return battleBgm;
        if (key.Contains("world"))
            return worldMapBgm;
        if (key.Contains("bootstrap") || key.Contains("title"))
            return bootstrapBgm;
        return null;
    }

    private IEnumerator SwitchBgmRoutine(AudioClip next)
    {
        EnsureSources();
        float targetVolume = masterVolume * bgmVolume;
        float fade = Mathf.Max(0f, bgmFadeSeconds);

        if (bgmSource.isPlaying && fade > 0f)
        {
            float startVolume = bgmSource.volume;
            float elapsed = 0f;
            while (elapsed < fade)
            {
                elapsed += Time.unscaledDeltaTime;
                bgmSource.volume = Mathf.Lerp(startVolume, 0f, Mathf.Clamp01(elapsed / fade));
                yield return null;
            }
        }

        currentBgm = next;
        bgmSource.clip = next;

        if (next == null)
        {
            bgmSource.Stop();
            bgmSource.volume = targetVolume;
            yield break;
        }

        bgmSource.volume = fade > 0f ? 0f : targetVolume;
        bgmSource.Play();

        if (fade > 0f)
        {
            float elapsed = 0f;
            while (elapsed < fade)
            {
                elapsed += Time.unscaledDeltaTime;
                bgmSource.volume = Mathf.Lerp(0f, targetVolume, Mathf.Clamp01(elapsed / fade));
                yield return null;
            }
        }

        bgmSource.volume = targetVolume;
    }

    public static void PlaySfx(AudioClip clip)
    {
        if (instance == null || clip == null)
            return;
        instance.EnsureSources();
        instance.sfxSource.PlayOneShot(clip, instance.masterVolume * instance.sfxVolume);
    }

    public static void PlayButtonHover()
    {
        if (instance == null || instance.buttonHoverSfx == null)
            return;
        instance.EnsureSources();
        instance.uiSource.PlayOneShot(instance.buttonHoverSfx, instance.masterVolume * instance.uiVolume);
    }

    public static void PlayButtonClick()
    {
        if (instance == null || instance.buttonClickSfx == null)
            return;
        instance.EnsureSources();
        instance.uiSource.PlayOneShot(instance.buttonClickSfx, instance.masterVolume * instance.uiVolume);
    }
}
