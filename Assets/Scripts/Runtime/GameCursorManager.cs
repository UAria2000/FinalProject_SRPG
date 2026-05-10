using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    private readonly HashSet<string> busyKeys = new HashSet<string>();
    private bool mouseDown;
    private CursorVisualState currentState = CursorVisualState.Unset;

    private enum CursorVisualState
    {
        Unset,
        Default,
        Click,
        Busy
    }

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
        if (Input.GetMouseButtonDown(0))
        {
            mouseDown = true;
            RefreshCursor(false);
        }

        if (Input.GetMouseButtonUp(0))
        {
            mouseDown = false;
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

    public static void ClearAllBusy()
    {
        if (instance == null)
            return;

        instance.busyKeys.Clear();
        instance.RefreshCursor(false);
    }

    private bool IsBusy => busyKeys.Count > 0;

    private void RefreshCursor(bool force)
    {
        CursorVisualState next = IsBusy
            ? CursorVisualState.Busy
            : (mouseDown ? CursorVisualState.Click : CursorVisualState.Default);

        if (!force && next == currentState)
            return;

        currentState = next;

        switch (next)
        {
            case CursorVisualState.Busy:
                ApplyCursor(busyCursor != null ? busyCursor : defaultCursor, busyCursor != null ? busyHotspot : defaultHotspot);
                break;
            case CursorVisualState.Click:
                ApplyCursor(clickCursor != null ? clickCursor : defaultCursor, clickCursor != null ? clickHotspot : defaultHotspot);
                break;
            case CursorVisualState.Default:
            default:
                ApplyCursor(defaultCursor, defaultHotspot);
                break;
        }
    }

    private void ApplyCursor(Texture2D texture, Vector2 hotspot)
    {
        Cursor.SetCursor(texture, hotspot, cursorMode);
    }
}
