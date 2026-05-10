using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public class GameCursorManager : MonoBehaviour
{
    private static GameCursorManager instance;

    [Header("Cursor Textures")]
    [SerializeField] private Texture2D defaultCursor;
    [SerializeField] private Texture2D clickCursor;
    [SerializeField] private Texture2D busyCursor;

    [Header("Hotspots")]
    [SerializeField] private Vector2 defaultHotspot = Vector2.zero;
    [SerializeField] private Vector2 clickHotspot = Vector2.zero;
    [SerializeField] private Vector2 busyHotspot = Vector2.zero;

    [Header("Options")]
    [SerializeField] private CursorMode cursorMode = CursorMode.Auto;
    [SerializeField] private bool persistAcrossScenes = true;
    [SerializeField] private bool validateCursorTextures = true;

    private readonly HashSet<string> busyKeys = new HashSet<string>();

    private bool pointerDown;
    private CursorVisualState currentState = CursorVisualState.Unset;

    private enum CursorVisualState
    {
        Unset,
        Default,
        Click,
        Busy
    }

    private bool IsBusy => busyKeys.Count > 0;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        if (persistAcrossScenes)
            DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += HandleSceneLoaded;
        RefreshCursor(true);
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
        RefreshCursor(true);
    }

    private void Update()
    {
        bool nextPointerDown = IsPrimaryPointerPressed();

        if (pointerDown != nextPointerDown)
        {
            pointerDown = nextPointerDown;
            RefreshCursor(false);
        }

        if (IsBusy && currentState != CursorVisualState.Busy)
            RefreshCursor(false);
    }

    public static void SetBusy(string key, bool busy)
    {
        if (string.IsNullOrWhiteSpace(key))
            key = "Default";

        if (instance == null)
            return;

        if (busy)
            instance.busyKeys.Add(key);
        else
            instance.busyKeys.Remove(key);

        instance.RefreshCursor(false);
    }

    public static void SetBusy(bool busy)
    {
        SetBusy("Default", busy);
    }

    public static void ClearBusy(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            key = "Default";

        if (instance == null)
            return;

        instance.busyKeys.Remove(key);
        instance.RefreshCursor(false);
    }

    public static void ClearAllBusy()
    {
        if (instance == null)
            return;

        instance.busyKeys.Clear();
        instance.RefreshCursor(false);
    }

    private bool IsPrimaryPointerPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            return Mouse.current.leftButton.isPressed;

        if (Pointer.current != null)
            return Pointer.current.press.isPressed;

        return false;
#else
        return false;
#endif
    }

    private void RefreshCursor(bool force)
    {
        CursorVisualState next = IsBusy
            ? CursorVisualState.Busy
            : (pointerDown ? CursorVisualState.Click : CursorVisualState.Default);

        if (!force && next == currentState)
            return;

        currentState = next;

        switch (next)
        {
            case CursorVisualState.Busy:
                ApplyCursor(
                    busyCursor != null ? busyCursor : defaultCursor,
                    busyCursor != null ? busyHotspot : defaultHotspot
                );
                break;

            case CursorVisualState.Click:
                ApplyCursor(
                    clickCursor != null ? clickCursor : defaultCursor,
                    clickCursor != null ? clickHotspot : defaultHotspot
                );
                break;

            case CursorVisualState.Default:
            default:
                ApplyCursor(defaultCursor, defaultHotspot);
                break;
        }
    }

    private void ApplyCursor(Texture2D texture, Vector2 hotspot)
    {
        if (texture == null)
        {
            Cursor.SetCursor(null, Vector2.zero, cursorMode);
            Cursor.visible = true;
            return;
        }

        if (validateCursorTextures && !IsValidCursorTexture(texture))
        {
            Debug.LogWarning(
                $"[GameCursorManager] Invalid cursor texture '{texture.name}'. " +
                "Import Settings must be: Texture Type=Cursor, Read/Write=On, " +
                "Alpha Is Transparency=On, Generate Mip Maps=Off, Compression=None, Format=RGBA32."
            );

            Cursor.SetCursor(null, Vector2.zero, cursorMode);
            Cursor.visible = true;
            return;
        }

        Cursor.SetCursor(texture, hotspot, cursorMode);
        Cursor.visible = true;
    }

    private bool IsValidCursorTexture(Texture2D texture)
    {
        if (texture == null)
            return false;

        if (!texture.isReadable)
            return false;

        if (texture.mipmapCount > 1)
            return false;

        if (texture.format != TextureFormat.RGBA32)
            return false;

        return true;
    }
}