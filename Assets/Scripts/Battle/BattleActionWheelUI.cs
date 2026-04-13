using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BattleActionWheelUI : MonoBehaviour
{
    private enum WheelState
    {
        Closed,
        Root,
        Attack,
        Mana,
        Targeting
    }

    [Header("References")]
    [SerializeField] private RectTransform wheelRoot;
    [SerializeField] private CanvasGroup wheelCanvasGroup;
    [SerializeField] private Camera uiCamera;

    [Header("State Roots")]
    [SerializeField] private GameObject rootStateRoot;
    [SerializeField] private GameObject attackStateRoot;
    [SerializeField] private GameObject manaStateRoot;

    [Header("Root Buttons")]
    [SerializeField] private Button rootManaButton;
    [SerializeField] private Image rootManaGaugeFill;
    [SerializeField] private Button rootAttackButton;
    [SerializeField] private Button rootMoveButton;
    [SerializeField] private Button rootItemButton;
    [SerializeField] private Image rootItemIcon;
    [SerializeField] private Button rootEndTurnButton;
    [SerializeField] private Button rootCenterButton;

    [Header("Attack Depth Buttons")]
    [SerializeField] private Button attackBasicButton;
    [SerializeField] private Image attackBasicIcon;
    [SerializeField] private Button[] attackSkillButtons = new Button[3];
    [SerializeField] private Image[] attackSkillIcons = new Image[3];
    [SerializeField] private GameObject[] attackEmptyFrames = new GameObject[1];
    [SerializeField] private Button attackBackButton;

    [Header("Mana Depth Buttons")]
    [SerializeField] private Button manaCaptureButton;
    [SerializeField] private Button manaFleeButton;
    [SerializeField] private Button manaPreventDeathButton;
    [SerializeField] private Button manaTeamBuffButton;
    [SerializeField] private GameObject[] manaEmptyFrames = new GameObject[1];
    [SerializeField] private Button manaBackButton;

    [Header("Behavior")]
    [SerializeField] private bool openAtLastPosition = true;
    [SerializeField] private Vector2 initialAnchoredPosition = new Vector2(0f, -220f);
    [SerializeField] private float[] scaleSteps = new float[] { 1f, 1.25f, 1.5f };
    [SerializeField] private int defaultScaleIndex = 0;
    [SerializeField] private float rightDragThreshold = 30f;
    [SerializeField] private int sharedConsumableInventoryIndex = 0;
    [SerializeField] private bool closeOnBlankLeftClick = true;

    private BattleManager battleManager;
    private WheelState currentState = WheelState.Closed;
    private WheelState targetingBaseState = WheelState.Root;

    private Vector2 lastAnchoredPosition;
    private int currentScaleIndex;

    private bool rightPressed;
    private bool rightDragged;
    private Vector2 rightPressScreenPosition;
    private float rightDragReferenceDistance;

    private bool canPlayerAct;
    private bool canAcceptRootInteractions;
    private BattleUnit currentActor;

    private RectTransform wheelParentRect;
    private Canvas parentCanvas;

    private bool wasWaitingForActionLastRefresh;
    private BattleUnit lastRefreshActor;

    public void Initialize(BattleManager manager)
    {
        battleManager = manager;

        if (wheelRoot == null)
            wheelRoot = transform as RectTransform;

        parentCanvas = GetComponentInParent<Canvas>();
        wheelParentRect = wheelRoot != null ? wheelRoot.parent as RectTransform : null;

        if (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = null;
        }
        else if (uiCamera == null && parentCanvas != null)
        {
            uiCamera = parentCanvas.worldCamera;
        }

        currentScaleIndex = Mathf.Clamp(defaultScaleIndex, 0, Mathf.Max(0, scaleSteps.Length - 1));
        lastAnchoredPosition = initialAnchoredPosition;

        BindButtons();
        ApplyScale();
        CloseImmediate();

        wasWaitingForActionLastRefresh = false;
        lastRefreshActor = null;
    }

    private void Update()
    {
        HandleRightMouseInput();
    }

    public void Refresh(BattleUnit actor, bool playerCanAct, BattleInputMode inputMode, List<InventoryStackData> inventory)
    {
        currentActor = actor;
        canPlayerAct = playerCanAct;
        canAcceptRootInteractions = playerCanAct && actor != null && inputMode == BattleInputMode.WaitingForAction;

        RefreshRootState(inventory);
        RefreshAttackState();
        RefreshManaState();

        if (!playerCanAct)
        {
            CloseImmediate();
            wasWaitingForActionLastRefresh = false;
            lastRefreshActor = actor;
            return;
        }

        bool isWaitingForAction = inputMode == BattleInputMode.WaitingForAction;

        if (isWaitingForAction)
        {
            bool shouldAutoOpen =
                !wasWaitingForActionLastRefresh ||
                actor != lastRefreshActor;

            if (shouldAutoOpen && currentState == WheelState.Closed)
                OpenRoot();

            if (currentState != WheelState.Closed && currentState != WheelState.Targeting)
            {
                if (currentState == WheelState.Attack || currentState == WheelState.Mana)
                    ShowState(currentState);
                else
                    ShowState(WheelState.Root);
            }
        }
        else
        {
            if (currentState != WheelState.Closed)
                ShowTargetingLock();
        }

        wasWaitingForActionLastRefresh = isWaitingForAction;
        lastRefreshActor = actor;
    }

    public void HandleBlankLeftClick()
    {
        if (!closeOnBlankLeftClick)
            return;

        CloseWheel();
    }

    public void HandleCurrentActorClicked(BattleUnit clickedUnit)
    {
        if (battleManager == null)
            return;

        bool canOpenForCurrentActor =
            battleManager.CurrentState == TurnState.PlayerInput &&
            battleManager.CurrentActingUnit != null &&
            battleManager.CurrentActingUnit.Team == TeamType.Ally &&
            clickedUnit == battleManager.CurrentActingUnit;

        if (!canOpenForCurrentActor)
            return;

        if (currentState == WheelState.Closed)
            OpenRoot();
    }

    public void SetOpenAtLastPosition(bool useLast)
    {
        openAtLastPosition = useLast;
    }

    private void BindButtons()
    {
        Bind(rootManaButton, OpenMana);
        Bind(rootAttackButton, OpenAttack);
        Bind(rootMoveButton, () => battleManager?.OnMoveButtonPressed());
        Bind(rootItemButton, OnRootItemPressed);
        Bind(rootEndTurnButton, () => battleManager?.OnEndTurnButtonPressed());
        Bind(rootCenterButton, OnRootCenterPressed);

        Bind(attackBasicButton, () => battleManager?.OnActionSlotPressed(0));
        for (int i = 0; i < attackSkillButtons.Length; i++)
        {
            int slotIndex = i + 1;
            Bind(attackSkillButtons[i], () => battleManager?.OnActionSlotPressed(slotIndex));
        }
        Bind(attackBackButton, OnAttackBackPressed);

        Bind(manaCaptureButton, () => battleManager?.OnCaptureButtonPressed());
        Bind(manaFleeButton, () => battleManager?.OnFleeButtonPressed());
        Bind(manaPreventDeathButton, () => Debug.Log("[BattleActionWheelUI] 즉시방지버프 액션은 차후 전투 로직과 연결 예정입니다."));
        Bind(manaTeamBuffButton, () => Debug.Log("[BattleActionWheelUI] 아군전체버프 액션은 차후 전투 로직과 연결 예정입니다."));
        Bind(manaBackButton, OnManaBackPressed);
    }

    private void RefreshRootState(List<InventoryStackData> inventory)
    {
        if (rootManaButton != null)
            rootManaButton.interactable = canAcceptRootInteractions;
        if (rootAttackButton != null)
            rootAttackButton.interactable = canAcceptRootInteractions;
        if (rootMoveButton != null)
            rootMoveButton.interactable = canAcceptRootInteractions;
        if (rootItemButton != null)
            rootItemButton.interactable = canAcceptRootInteractions && HasSharedInventoryItem(inventory, out _);
        if (rootEndTurnButton != null)
            rootEndTurnButton.interactable = canAcceptRootInteractions;
        if (rootCenterButton != null)
            rootCenterButton.interactable = true;

        if (rootManaGaugeFill != null)
            rootManaGaugeFill.fillAmount = 0f;

        if (rootItemIcon != null)
        {
            if (HasSharedInventoryItem(inventory, out InventoryStackData stack) && stack != null && stack.item != null)
            {
                rootItemIcon.gameObject.SetActive(true);
                rootItemIcon.sprite = stack.item.icon;
            }
            else
            {
                rootItemIcon.gameObject.SetActive(false);
                rootItemIcon.sprite = null;
            }
        }
    }

    private void RefreshAttackState()
    {
        BattleUnit unit = currentActor;
        SkillDefinition basic = unit != null ? unit.GetActionSkillAt(0) : null;

        if (attackBasicIcon != null)
        {
            attackBasicIcon.gameObject.SetActive(basic != null && basic.icon != null);
            attackBasicIcon.sprite = basic != null ? basic.icon : null;
        }

        if (attackBasicButton != null)
            attackBasicButton.interactable = canAcceptRootInteractions && unit != null && basic != null && unit.CanUseSkill(basic);

        for (int i = 0; i < attackSkillButtons.Length; i++)
        {
            int slot = i + 1;
            SkillDefinition skill = unit != null ? unit.GetActionSkillAt(slot) : null;

            if (i < attackSkillIcons.Length && attackSkillIcons[i] != null)
            {
                attackSkillIcons[i].gameObject.SetActive(skill != null && skill.icon != null);
                attackSkillIcons[i].sprite = skill != null ? skill.icon : null;
            }

            if (attackSkillButtons[i] != null)
                attackSkillButtons[i].interactable = canAcceptRootInteractions && unit != null && skill != null && unit.CanUseSkill(skill);
        }

        for (int i = 0; i < attackEmptyFrames.Length; i++)
        {
            if (attackEmptyFrames[i] != null)
                attackEmptyFrames[i].SetActive(true);
        }
    }

    private void RefreshManaState()
    {
        bool canCapture = canAcceptRootInteractions && battleManager != null && battleManager.CanActorUseCaptureCommand(currentActor);

        if (manaCaptureButton != null) manaCaptureButton.interactable = canCapture;
        if (manaFleeButton != null) manaFleeButton.interactable = canAcceptRootInteractions;
        if (manaPreventDeathButton != null) manaPreventDeathButton.interactable = canAcceptRootInteractions;
        if (manaTeamBuffButton != null) manaTeamBuffButton.interactable = canAcceptRootInteractions;

        for (int i = 0; i < manaEmptyFrames.Length; i++)
        {
            if (manaEmptyFrames[i] != null)
                manaEmptyFrames[i].SetActive(true);
        }
    }

    private void OnRootItemPressed()
    {
        if (battleManager == null)
            return;

        targetingBaseState = WheelState.Root;
        battleManager.OnInventorySlotPressed(sharedConsumableInventoryIndex);
    }

    private bool HasSharedInventoryItem(List<InventoryStackData> inventory, out InventoryStackData stack)
    {
        stack = null;
        if (inventory == null || inventory.Count <= 0)
            return false;

        int idx = Mathf.Clamp(sharedConsumableInventoryIndex, 0, inventory.Count - 1);
        stack = inventory[idx];
        return stack != null && stack.item != null && stack.amount > 0;
    }

    private void OpenRoot()
    {
        if (battleManager == null)
            return;

        bool canOpen =
            battleManager.CurrentState == TurnState.PlayerInput &&
            battleManager.CurrentActingUnit != null &&
            battleManager.CurrentActingUnit.Team == TeamType.Ally;

        if (!canOpen)
            return;

        if (currentState == WheelState.Closed)
            OpenAtPreferredPosition();

        ShowState(WheelState.Root);
    }

    private void OpenAttack()
    {
        if (!canAcceptRootInteractions)
            return;

        if (currentState == WheelState.Closed)
            OpenAtPreferredPosition();

        ShowState(WheelState.Attack);
    }

    private void OpenMana()
    {
        if (!canAcceptRootInteractions)
            return;

        if (currentState == WheelState.Closed)
            OpenAtPreferredPosition();

        ShowState(WheelState.Mana);
    }

    private void OnRootCenterPressed()
    {
        if (currentState == WheelState.Targeting && targetingBaseState == WheelState.Root)
        {
            CancelCurrentSelectionAndReturnTo(WheelState.Root);
            return;
        }

        CloseWheel();
    }

    private void OnAttackBackPressed()
    {
        if (currentState == WheelState.Targeting && targetingBaseState == WheelState.Attack)
        {
            CancelCurrentSelectionAndReturnTo(WheelState.Attack);
            return;
        }

        OpenRoot();
    }

    private void OnManaBackPressed()
    {
        if (currentState == WheelState.Targeting && targetingBaseState == WheelState.Mana)
        {
            CancelCurrentSelectionAndReturnTo(WheelState.Mana);
            return;
        }

        OpenRoot();
    }

    private void CancelCurrentSelectionAndReturnTo(WheelState returnState)
    {
        if (battleManager == null)
            return;

        targetingBaseState = returnState;
        currentState = returnState;

        battleManager.OnCancelButtonPressed();

        if (returnState == WheelState.Attack)
            ShowState(WheelState.Attack);
        else if (returnState == WheelState.Mana)
            ShowState(WheelState.Mana);
        else
            ShowState(WheelState.Root);
    }

    public void CloseWheel()
    {
        CloseImmediate();
    }

    private void OpenAtPreferredPosition()
    {
        if (wheelRoot == null)
            return;

        wheelRoot.anchoredPosition = openAtLastPosition ? lastAnchoredPosition : initialAnchoredPosition;
    }

    private void ShowState(WheelState state)
    {
        currentState = state;
        SetVisible(state != WheelState.Closed);

        if (state == WheelState.Root)
        {
            if (rootStateRoot != null) rootStateRoot.SetActive(true);
            if (attackStateRoot != null) attackStateRoot.SetActive(false);
            if (manaStateRoot != null) manaStateRoot.SetActive(false);
            UnlockInteractiveStates();
            return;
        }

        if (state == WheelState.Attack)
        {
            if (rootStateRoot != null) rootStateRoot.SetActive(false);
            if (attackStateRoot != null) attackStateRoot.SetActive(true);
            if (manaStateRoot != null) manaStateRoot.SetActive(false);
            UnlockInteractiveStates();
            return;
        }

        if (state == WheelState.Mana)
        {
            if (rootStateRoot != null) rootStateRoot.SetActive(false);
            if (attackStateRoot != null) attackStateRoot.SetActive(false);
            if (manaStateRoot != null) manaStateRoot.SetActive(true);
            UnlockInteractiveStates();
            return;
        }

        if (state == WheelState.Targeting)
            ShowTargetingLock();
    }

    private void ShowTargetingLock()
    {
        if (currentState != WheelState.Targeting)
        {
            if (currentState == WheelState.Root || currentState == WheelState.Attack || currentState == WheelState.Mana)
                targetingBaseState = currentState;
        }

        currentState = WheelState.Targeting;

        if (rootStateRoot != null) rootStateRoot.SetActive(targetingBaseState == WheelState.Root);
        if (attackStateRoot != null) attackStateRoot.SetActive(targetingBaseState == WheelState.Attack);
        if (manaStateRoot != null) manaStateRoot.SetActive(targetingBaseState == WheelState.Mana);

        LockForTargeting();
    }

    private void LockForTargeting()
    {
        if (targetingBaseState == WheelState.Root)
        {
            SetButtonInteractable(rootManaButton, false);
            SetButtonInteractable(rootAttackButton, false);
            SetButtonInteractable(rootMoveButton, false);
            SetButtonInteractable(rootItemButton, false);
            SetButtonInteractable(rootEndTurnButton, false);
            SetButtonInteractable(rootCenterButton, true);
        }
        else if (targetingBaseState == WheelState.Attack)
        {
            SetButtonInteractable(attackBasicButton, false);
            for (int i = 0; i < attackSkillButtons.Length; i++)
                SetButtonInteractable(attackSkillButtons[i], false);

            SetButtonInteractable(attackBackButton, true);
        }
        else if (targetingBaseState == WheelState.Mana)
        {
            SetButtonInteractable(manaCaptureButton, false);
            SetButtonInteractable(manaFleeButton, false);
            SetButtonInteractable(manaPreventDeathButton, false);
            SetButtonInteractable(manaTeamBuffButton, false);
            SetButtonInteractable(manaBackButton, true);
        }
    }

    private void UnlockInteractiveStates()
    {
        // Refresh(...)가 각 상태별 interactable을 다시 잡아줌.
    }

    private void SetVisible(bool active)
    {
        if (wheelRoot != null)
            wheelRoot.gameObject.SetActive(active);

        if (wheelCanvasGroup != null)
        {
            wheelCanvasGroup.alpha = active ? 1f : 0f;
            wheelCanvasGroup.blocksRaycasts = active;
            wheelCanvasGroup.interactable = active;
        }
    }

    private void CloseImmediate()
    {
        currentState = WheelState.Closed;
        targetingBaseState = WheelState.Root;

        SetVisible(false);

        if (rootStateRoot != null) rootStateRoot.SetActive(false);
        if (attackStateRoot != null) attackStateRoot.SetActive(false);
        if (manaStateRoot != null) manaStateRoot.SetActive(false);
    }

    private void HandleRightMouseInput()
    {
        if (Mouse.current == null || battleManager == null || battleManager.CurrentState != TurnState.PlayerInput)
            return;

        bool canOpenForActor =
            battleManager.CurrentActingUnit != null &&
            battleManager.CurrentActingUnit.Team == TeamType.Ally;

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            rightPressed = true;
            rightDragged = false;
            rightPressScreenPosition = Mouse.current.position.ReadValue();
            rightDragReferenceDistance = GetDistanceFromWheelCenter(rightPressScreenPosition);
        }

        if (rightPressed && Mouse.current.rightButton.isPressed)
        {
            Vector2 now = Mouse.current.position.ReadValue();
            Vector2 delta = now - rightPressScreenPosition;

            if (!rightDragged && delta.magnitude >= rightDragThreshold)
                rightDragged = true;

            if (rightDragged)
                HandleRightScaleDrag(now);
        }

        if (rightPressed && Mouse.current.rightButton.wasReleasedThisFrame)
        {
            Vector2 releasePos = Mouse.current.position.ReadValue();

            if (!rightDragged)
            {
                TeleportToScreenPosition(releasePos);

                if (currentState == WheelState.Closed && canOpenForActor)
                    OpenRoot();
            }

            rightPressed = false;
            rightDragged = false;
        }
    }

    private void HandleRightScaleDrag(Vector2 mouseScreenPosition)
    {
        float currentDistance = GetDistanceFromWheelCenter(mouseScreenPosition);
        float delta = currentDistance - rightDragReferenceDistance;

        if (delta >= rightDragThreshold)
        {
            StepScaleUp();
            rightDragReferenceDistance = currentDistance;
        }
        else if (delta <= -rightDragThreshold)
        {
            StepScaleDown();
            rightDragReferenceDistance = currentDistance;
        }
    }

    private float GetDistanceFromWheelCenter(Vector2 mouseScreenPosition)
    {
        if (wheelRoot == null)
            return 0f;

        Vector2 centerScreenPosition = RectTransformUtility.WorldToScreenPoint(uiCamera, wheelRoot.position);
        return Vector2.Distance(centerScreenPosition, mouseScreenPosition);
    }

    private void StepScaleUp()
    {
        SetScaleIndex(currentScaleIndex + 1);
    }

    private void StepScaleDown()
    {
        SetScaleIndex(currentScaleIndex - 1);
    }

    private void SetScaleIndex(int index)
    {
        if (scaleSteps == null || scaleSteps.Length == 0)
            return;

        currentScaleIndex = Mathf.Clamp(index, 0, scaleSteps.Length - 1);
        ApplyScale();
    }

    private void TeleportToScreenPosition(Vector2 screenPosition)
    {
        if (wheelRoot == null)
            return;

        RectTransform targetRect = wheelParentRect != null ? wheelParentRect : wheelRoot.parent as RectTransform;
        if (targetRect == null)
            return;

        Camera eventCamera = null;
        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            eventCamera = uiCamera != null ? uiCamera : parentCanvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRect, screenPosition, eventCamera, out Vector2 localPoint))
            return;

        wheelRoot.anchoredPosition = localPoint;
        lastAnchoredPosition = wheelRoot.anchoredPosition;
    }

    private void ApplyScale()
    {
        if (wheelRoot == null || scaleSteps == null || scaleSteps.Length <= 0)
            return;

        float scale = scaleSteps[Mathf.Clamp(currentScaleIndex, 0, scaleSteps.Length - 1)];
        wheelRoot.localScale = new Vector3(scale, scale, 1f);
    }

    private static void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private static void SetButtonInteractable(Button button, bool interactable)
    {
        if (button != null)
            button.interactable = interactable;
    }
}