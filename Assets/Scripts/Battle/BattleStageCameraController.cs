using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 24:9 battle stage controller.
/// Keeps the visible play window at 16:9 while the battle stage can be wider, e.g. 3840x1440.
/// It can pan either a world camera, a RectTransform stage content root, or both.
/// </summary>
[DisallowMultipleComponent]
public class BattleStageCameraController : MonoBehaviour
{
    public enum BattleStagePanMode
    {
        RectTransformStage = 0,
        CameraTransform = 1,
        Both = 2,
    }

    [Header("Mode")]
    [SerializeField] private BattleStagePanMode panMode = BattleStagePanMode.RectTransformStage;
    [SerializeField] private bool edgeScrollEnabled = true;
    [SerializeField] private bool focusEnabled = true;

    [Header("References")]
    [Tooltip("Usually the root RectTransform containing battle background and unit views. This is the 3840x1440 moving content.")]
    [SerializeField] private RectTransform stageContentRoot;
    [Tooltip("Optional viewport root. Used only for inspector clarity; not required for math.")]
    [SerializeField] private RectTransform viewportRoot;
    [SerializeField] private Camera stageCamera;
    [SerializeField] private BattleViewManager viewManager;

    [Header("Reference Size")]
    [Tooltip("Visible 16:9 window width at reference resolution. For your project this should be 2560.")]
    [Min(1f)] [SerializeField] private float visibleReferenceWidth = 2560f;
    [Tooltip("Full 24:9 battle stage width at reference resolution. For your project this should be 3840.")]
    [Min(1f)] [SerializeField] private float stageReferenceWidth = 3840f;
    [Tooltip("Reference height. For your project this should be 1440.")]
    [Min(1f)] [SerializeField] private float referenceHeight = 1440f;

    [Header("RectTransform Stage Coordinates")]
    [Tooltip("Local X of the far left edge of the 24:9 stage. Default is -3840/2.")]
    [SerializeField] private float stageLocalLeftX = -1920f;
    [Tooltip("Local X of the far right edge of the 24:9 stage. Default is +3840/2.")]
    [SerializeField] private float stageLocalRightX = 1920f;

    [Header("Camera Bounds")]
    [Tooltip("Camera X at the leftmost view. Used when pan mode moves a Camera transform.")]
    [SerializeField] private float cameraLeftX = -640f;
    [Tooltip("Camera X at the rightmost view. Used when pan mode moves a Camera transform.")]
    [SerializeField] private float cameraRightX = 640f;

    [Header("Edge Scroll")]
    [Tooltip("At 2560 reference width, how close the mouse must be to the left/right edge to scroll.")]
    [Min(1f)] [SerializeField] private float edgeScrollZoneReferencePixels = 90f;
    [Tooltip("Normalized pan amount per second while the mouse stays on the screen edge.")]
    [Min(0f)] [SerializeField] private float edgeScrollNormalizedSpeed = 0.65f;
    [SerializeField] private bool blockEdgeScrollWhenPointerOverUI = true;
    [Tooltip("UI raycast hits under these roots are ignored when deciding whether UI blocks edge-scroll. Put the battle stage/background root here if it is also UI.")]
    [SerializeField] private RectTransform[] pointerUIBlockExclusionRoots;

    [Header("Smoothing")]
    [Tooltip("SmoothDamp time for normal focus and edge-scroll movement.")]
    [Min(0.01f)] [SerializeField] private float smoothTime = 0.18f;
    [Tooltip("When a turn starts, the camera focuses the acting unit using this smooth time. 0 or below means use Smooth Time.")]
    [SerializeField] private float turnStartFocusSmoothTime = 0.22f;
    [Tooltip("When action focus is requested, this smooth time is used. 0 or below means use Smooth Time.")]
    [SerializeField] private float actionFocusSmoothTime = 0.16f;

    [Header("Initial Position")]
    [Range(0f, 1f)] [SerializeField] private float initialNormalizedPan = 0.5f;
    [SerializeField] private bool centerOnStart = true;

    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();
    private readonly List<BattleUnit> scratchUnits = new List<BattleUnit>();

    private float targetNormalizedPan;
    private float currentNormalizedPan;
    private float normalizedPanVelocity;
    private Vector3 cameraInitialPosition;
    private Vector2 stageInitialAnchoredPosition;
    private bool initialized;
    private float activeSmoothTime;

    public float NormalizedPan => currentNormalizedPan;
    public bool IsAtLeftEdge => currentNormalizedPan <= 0.001f;
    public bool IsAtRightEdge => currentNormalizedPan >= 0.999f;

    private float MaxStagePanDistance => Mathf.Max(0f, stageReferenceWidth - visibleReferenceWidth);

    private void Awake()
    {
        CaptureInitialStateIfNeeded();
    }

    private void Start()
    {
        CaptureInitialStateIfNeeded();

        if (centerOnStart)
            SetNormalizedPanInstant(initialNormalizedPan);
    }

    private void Update()
    {
        CaptureInitialStateIfNeeded();

        if (edgeScrollEnabled)
            UpdateEdgeScrollTarget();

        UpdateSmoothPan();
    }

    public void Initialize(BattleViewManager manager)
    {
        viewManager = manager;
        CaptureInitialStateIfNeeded();
    }

    public void SetViewManager(BattleViewManager manager)
    {
        viewManager = manager;
    }

    public void SetNormalizedPanInstant(float normalizedPan)
    {
        targetNormalizedPan = Mathf.Clamp01(normalizedPan);
        currentNormalizedPan = targetNormalizedPan;
        normalizedPanVelocity = 0f;
        ApplyCurrentPan();
    }

    public void SetNormalizedPanSmooth(float normalizedPan)
    {
        targetNormalizedPan = Mathf.Clamp01(normalizedPan);
        activeSmoothTime = smoothTime;
    }

    public void FocusUnitInstant(BattleUnit unit)
    {
        if (!focusEnabled)
            return;

        float normalized;
        if (TryGetNormalizedPanForUnit(unit, out normalized))
            SetNormalizedPanInstant(normalized);
    }

    public void FocusUnitSmooth(BattleUnit unit)
    {
        FocusUnitSmooth(unit, turnStartFocusSmoothTime > 0f ? turnStartFocusSmoothTime : smoothTime);
    }

    public void FocusUnitSmooth(BattleUnit unit, float requestedSmoothTime)
    {
        if (!focusEnabled)
            return;

        float normalized;
        if (!TryGetNormalizedPanForUnit(unit, out normalized))
            return;

        targetNormalizedPan = Mathf.Clamp01(normalized);
        activeSmoothTime = requestedSmoothTime > 0f ? requestedSmoothTime : smoothTime;
    }

    public void FocusUnitsSmooth(BattleUnit a, BattleUnit b)
    {
        FocusUnitsSmooth(a, b, actionFocusSmoothTime > 0f ? actionFocusSmoothTime : smoothTime);
    }

    public void FocusUnitsSmooth(BattleUnit a, BattleUnit b, float requestedSmoothTime)
    {
        if (!focusEnabled)
            return;

        scratchUnits.Clear();
        if (a != null)
            scratchUnits.Add(a);
        if (b != null && b != a)
            scratchUnits.Add(b);

        FocusUnitsSmooth(scratchUnits, requestedSmoothTime);
    }

    public void FocusUnitsSmooth(IReadOnlyList<BattleUnit> units)
    {
        FocusUnitsSmooth(units, actionFocusSmoothTime > 0f ? actionFocusSmoothTime : smoothTime);
    }

    public void FocusUnitsSmooth(IReadOnlyList<BattleUnit> units, float requestedSmoothTime)
    {
        if (!focusEnabled || units == null || units.Count <= 0)
            return;

        float sum = 0f;
        int count = 0;
        for (int i = 0; i < units.Count; i++)
        {
            float unitX;
            if (!TryGetUnitStageLocalX(units[i], out unitX))
                continue;

            sum += unitX;
            count++;
        }

        if (count <= 0)
            return;

        float centerX = sum / count;
        float normalized = GetNormalizedPanForStageLocalX(centerX);
        targetNormalizedPan = Mathf.Clamp01(normalized);
        activeSmoothTime = requestedSmoothTime > 0f ? requestedSmoothTime : smoothTime;
    }

    public void FocusWorldPositionSmooth(Vector3 worldPosition)
    {
        float normalized = GetNormalizedPanForWorldPosition(worldPosition);
        targetNormalizedPan = Mathf.Clamp01(normalized);
        activeSmoothTime = actionFocusSmoothTime > 0f ? actionFocusSmoothTime : smoothTime;
    }

    public void ResetToCenterInstant()
    {
        SetNormalizedPanInstant(0.5f);
    }

    public void ResetToCenterSmooth()
    {
        SetNormalizedPanSmooth(0.5f);
    }

    private void CaptureInitialStateIfNeeded()
    {
        if (initialized)
            return;

        if (stageCamera == null)
            stageCamera = Camera.main;

        if (stageContentRoot != null)
            stageInitialAnchoredPosition = stageContentRoot.anchoredPosition;

        if (stageCamera != null)
            cameraInitialPosition = stageCamera.transform.position;

        targetNormalizedPan = Mathf.Clamp01(initialNormalizedPan);
        currentNormalizedPan = targetNormalizedPan;
        activeSmoothTime = smoothTime;
        initialized = true;
        ApplyCurrentPan();
    }

    private void UpdateEdgeScrollTarget()
    {
        Vector2 pointerPosition;
        if (!TryGetPointerPosition(out pointerPosition))
            return;

        if (blockEdgeScrollWhenPointerOverUI && IsPointerOverBlockingUI(pointerPosition))
            return;

        float scale = Screen.width > 0 ? Screen.width / visibleReferenceWidth : 1f;
        float edgeZone = Mathf.Max(1f, edgeScrollZoneReferencePixels * scale);
        float direction = 0f;

        if (pointerPosition.x <= edgeZone)
            direction = -1f;
        else if (pointerPosition.x >= Screen.width - edgeZone)
            direction = 1f;

        if (Mathf.Approximately(direction, 0f))
            return;

        targetNormalizedPan = Mathf.Clamp01(targetNormalizedPan + direction * edgeScrollNormalizedSpeed * Time.unscaledDeltaTime);
        activeSmoothTime = smoothTime;
    }

    private void UpdateSmoothPan()
    {
        float time = activeSmoothTime > 0f ? activeSmoothTime : smoothTime;
        currentNormalizedPan = Mathf.SmoothDamp(
            currentNormalizedPan,
            targetNormalizedPan,
            ref normalizedPanVelocity,
            time,
            Mathf.Infinity,
            Time.unscaledDeltaTime);

        if (Mathf.Abs(currentNormalizedPan - targetNormalizedPan) < 0.0005f)
        {
            currentNormalizedPan = targetNormalizedPan;
            normalizedPanVelocity = 0f;
        }

        currentNormalizedPan = Mathf.Clamp01(currentNormalizedPan);
        ApplyCurrentPan();
    }

    private void ApplyCurrentPan()
    {
        if (panMode == BattleStagePanMode.RectTransformStage || panMode == BattleStagePanMode.Both)
            ApplyRectTransformPan();

        if (panMode == BattleStagePanMode.CameraTransform || panMode == BattleStagePanMode.Both)
            ApplyCameraPan();
    }

    private void ApplyRectTransformPan()
    {
        if (stageContentRoot == null)
            return;

        Vector2 position = stageInitialAnchoredPosition;
        position.x = stageInitialAnchoredPosition.x - currentNormalizedPan * MaxStagePanDistance;
        stageContentRoot.anchoredPosition = position;
    }

    private void ApplyCameraPan()
    {
        if (stageCamera == null)
            return;

        Vector3 position = cameraInitialPosition;
        position.x = Mathf.Lerp(cameraLeftX, cameraRightX, currentNormalizedPan);
        stageCamera.transform.position = position;
    }

    private bool TryGetNormalizedPanForUnit(BattleUnit unit, out float normalized)
    {
        normalized = 0.5f;
        float unitX;
        if (!TryGetUnitStageLocalX(unit, out unitX))
            return false;

        normalized = GetNormalizedPanForStageLocalX(unitX);
        return true;
    }

    private bool TryGetUnitStageLocalX(BattleUnit unit, out float localX)
    {
        localX = 0f;
        if (unit == null || viewManager == null)
            return false;

        BattleUnitView view = viewManager.GetView(unit);
        if (view == null)
            return false;

        RectTransform anchor = view.HoverAnchor;
        Transform sourceTransform = anchor != null ? anchor : view.transform;

        if (stageContentRoot != null)
        {
            Vector3 local = stageContentRoot.InverseTransformPoint(sourceTransform.position);
            localX = local.x;
            return true;
        }

        localX = sourceTransform.position.x;
        return true;
    }

    private float GetNormalizedPanForStageLocalX(float stageLocalX)
    {
        float visibleHalf = visibleReferenceWidth * 0.5f;
        float minCenterX = stageLocalLeftX + visibleHalf;
        float maxCenterX = stageLocalRightX - visibleHalf;

        if (maxCenterX <= minCenterX)
            return 0.5f;

        return Mathf.InverseLerp(minCenterX, maxCenterX, stageLocalX);
    }

    private float GetNormalizedPanForWorldPosition(Vector3 worldPosition)
    {
        if (panMode == BattleStagePanMode.CameraTransform || panMode == BattleStagePanMode.Both)
        {
            if (!Mathf.Approximately(cameraLeftX, cameraRightX))
                return Mathf.InverseLerp(cameraLeftX, cameraRightX, worldPosition.x);
        }

        if (stageContentRoot != null)
        {
            float localX = stageContentRoot.InverseTransformPoint(worldPosition).x;
            return GetNormalizedPanForStageLocalX(localX);
        }

        return 0.5f;
    }

    private bool TryGetPointerPosition(out Vector2 position)
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null)
        {
            position = Vector2.zero;
            return false;
        }

        position = Mouse.current.position.ReadValue();
        return true;
#elif ENABLE_LEGACY_INPUT_MANAGER
        position = UnityEngine.Input.mousePosition;
        return true;
#else
        position = Vector2.zero;
        return false;
#endif
    }

    private bool IsPointerOverBlockingUI(Vector2 pointerPosition)
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
            return false;

        PointerEventData eventData = new PointerEventData(eventSystem);
        eventData.position = pointerPosition;

        raycastResults.Clear();
        eventSystem.RaycastAll(eventData, raycastResults);

        for (int i = 0; i < raycastResults.Count; i++)
        {
            GameObject hitObject = raycastResults[i].gameObject;
            if (hitObject == null || !hitObject.activeInHierarchy)
                continue;

            RectTransform hitRect = hitObject.GetComponent<RectTransform>();
            if (IsUnderAnyExclusionRoot(hitRect))
                continue;

            return true;
        }

        return false;
    }

    private bool IsUnderAnyExclusionRoot(RectTransform rect)
    {
        if (rect == null || pointerUIBlockExclusionRoots == null)
            return false;

        for (int i = 0; i < pointerUIBlockExclusionRoots.Length; i++)
        {
            RectTransform root = pointerUIBlockExclusionRoots[i];
            if (root == null)
                continue;

            if (rect == root || rect.IsChildOf(root))
                return true;
        }

        return false;
    }

#if UNITY_EDITOR
    [ContextMenu("Set 24:9 Defaults")]
    private void SetTwentyFourByNineDefaults()
    {
        visibleReferenceWidth = 2560f;
        stageReferenceWidth = 3840f;
        referenceHeight = 1440f;
        stageLocalLeftX = -1920f;
        stageLocalRightX = 1920f;
        cameraLeftX = -640f;
        cameraRightX = 640f;
        edgeScrollZoneReferencePixels = 90f;
        edgeScrollNormalizedSpeed = 0.65f;
        smoothTime = 0.18f;
        turnStartFocusSmoothTime = 0.22f;
        actionFocusSmoothTime = 0.16f;
        initialNormalizedPan = 0.5f;
    }
#endif
}
