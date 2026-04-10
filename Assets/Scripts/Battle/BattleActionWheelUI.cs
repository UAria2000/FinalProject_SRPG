using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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
    private Vector2 lastAnchoredPosition;
    private int currentScaleIndex;

    private bool rightPressed;
    private bool rightDragged;
    private Vector2 rightPressScreenPosition;
    private int dragScaleDelta;

    private bool canPlayerAct;
    private bool canAcceptRootInteractions;
    private BattleUnit currentActor;
    private RectTransform canvasRect;

    public void Initialize(BattleManager manager)
    {
        battleManager = manager;

        if (wheelRoot == null)
            wheelRoot = transform as RectTransform;

        Canvas canvas = GetComponentInParent<Canvas>();
        canvasRect = canvas != null ? canvas.transform as RectTransform : null;
        if (uiCamera == null && canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = canvas.worldCamera;

        currentScaleIndex = Mathf.Clamp(defaultScaleIndex, 0, Mathf.Max(0, scaleSteps.Length - 1));
        lastAnchoredPosition = initialAnchoredPosition;

        BindButtons();
        ApplyScale();
        CloseImmediate();
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
            return;
        }

        if (inputMode == BattleInputMode.WaitingForAction)
        {
            if (currentState == WheelState.Closed || currentState == WheelState.Targeting)
                OpenRoot();
            else
                ShowState(currentState == WheelState.Attack || currentState == WheelState.Mana ? currentState : WheelState.Root);
        }
        else
        {
            if (currentState != WheelState.Closed)
                ShowTargetingLock();
        }
    }

    public void HandleBlankLeftClick()
    {
        if (!closeOnBlankLeftClick)
            return;

        CloseWheel();
    }

    public void HandleCurrentActorClicked(BattleUnit clickedUnit)
    {
        if (!canPlayerAct || currentActor == null || clickedUnit != currentActor)
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
        Bind(rootCenterButton, CloseWheel);

        Bind(attackBasicButton, () => battleManager?.OnActionSlotPressed(0));
        for (int i = 0; i < attackSkillButtons.Length; i++)
        {
            int slotIndex = i + 1;
            Bind(attackSkillButtons[i], () => battleManager?.OnActionSlotPressed(slotIndex));
        }
        Bind(attackBackButton, OpenRoot);

        Bind(manaCaptureButton, () => battleManager?.OnCaptureButtonPressed());
        Bind(manaFleeButton, () => battleManager?.OnFleeButtonPressed());
        Bind(manaPreventDeathButton, () => Debug.Log("[BattleActionWheelUI] 즉시방지버프 액션은 차후 전투 로직과 연결 예정입니다."));
        Bind(manaTeamBuffButton, () => Debug.Log("[BattleActionWheelUI] 아군전체버프 액션은 차후 전투 로직과 연결 예정입니다."));
        Bind(manaBackButton, OpenRoot);
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
    }

    private void RefreshManaState()
    {
        bool canCapture = canAcceptRootInteractions && battleManager != null && battleManager.CanActorUseCaptureCommand(currentActor);
        if (manaCaptureButton != null) manaCaptureButton.interactable = canCapture;
        if (manaFleeButton != null) manaFleeButton.interactable = canAcceptRootInteractions;
        if (manaPreventDeathButton != null) manaPreventDeathButton.interactable = canAcceptRootInteractions;
        if (manaTeamBuffButton != null) manaTeamBuffButton.interactable = canAcceptRootInteractions;
    }

    private void OnRootItemPressed()
    {
        if (battleManager == null)
            return;

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
        if (!canPlayerAct)
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

        if (rootStateRoot != null)
            rootStateRoot.SetActive(state == WheelState.Root || state == WheelState.Targeting);
        if (attackStateRoot != null)
            attackStateRoot.SetActive(state == WheelState.Attack);
        if (manaStateRoot != null)
            manaStateRoot.SetActive(state == WheelState.Mana);

        if (state == WheelState.Targeting)
            LockForTargeting();
        else
            UnlockInteractiveStates();
    }

    private void ShowTargetingLock()
    {
        if (currentState == WheelState.Closed)
            return;

        currentState = WheelState.Targeting;
        if (rootStateRoot != null)
            rootStateRoot.SetActive(true);
        if (attackStateRoot != null)
            attackStateRoot.SetActive(false);
        if (manaStateRoot != null)
            manaStateRoot.SetActive(false);

        LockForTargeting();
    }

    private void LockForTargeting()
    {
        SetButtonInteractable(rootManaButton, false);
        SetButtonInteractable(rootAttackButton, false);
        SetButtonInteractable(rootMoveButton, false);
        SetButtonInteractable(rootItemButton, false);
        SetButtonInteractable(rootEndTurnButton, false);
        SetButtonInteractable(rootCenterButton, true);
    }

    private void UnlockInteractiveStates()
    {
        // Refresh(...)가 각 상태별 인터랙션을 다시 잡아줌.
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
        SetVisible(false);
        if (rootStateRoot != null) rootStateRoot.SetActive(false);
        if (attackStateRoot != null) attackStateRoot.SetActive(false);
        if (manaStateRoot != null) manaStateRoot.SetActive(false);
    }

    private void HandleRightMouseInput()
    {
        if (Mouse.current == null || battleManager == null || battleManager.CurrentState != TurnState.PlayerInput)
            return;

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            rightPressed = true;
            rightDragged = false;
            rightPressScreenPosition = Mouse.current.position.ReadValue();
            dragScaleDelta = 0;
        }

        if (rightPressed && Mouse.current.rightButton.isPressed)
        {
            Vector2 now = Mouse.current.position.ReadValue();
            Vector2 delta = now - rightPressScreenPosition;
            if (!rightDragged && delta.magnitude >= rightDragThreshold)
                rightDragged = true;

            if (rightDragged)
            {
                if (Mathf.Abs(delta.y) >= rightDragThreshold)
                {
                    dragScaleDelta = delta.y > 0f ? 1 : -1;
                }
            }
        }

        if (rightPressed && Mouse.current.rightButton.wasReleasedThisFrame)
        {
            Vector2 releasePos = Mouse.current.position.ReadValue();
            if (!rightDragged)
            {
                TeleportToScreenPosition(releasePos);
                if (currentState == WheelState.Closed && canPlayerAct)
                    OpenRoot();
            }
            else
            {
                if (dragScaleDelta != 0)
                {
                    currentScaleIndex = Mathf.Clamp(currentScaleIndex + dragScaleDelta, 0, Mathf.Max(0, scaleSteps.Length - 1));
                    ApplyScale();
                }
            }

            rightPressed = false;
            rightDragged = false;
            dragScaleDelta = 0;
        }
    }

    private void TeleportToScreenPosition(Vector2 screenPosition)
    {
        if (wheelRoot == null || canvasRect == null)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, uiCamera, out Vector2 localPoint))
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
