using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public enum BattleActionWheelDepth
{
    Root,
    Attack,
    Mana,
    Item,
    Move,
    Custom
}

[Serializable]
public class BattleActionWheelActionSlotBinding
{
    [Tooltip("행동 슬롯 인덱스. 기본값: 0=Center, 1=Top, 2=Right, 3=Bottom")]
    public int actionSlotIndex;

    [Tooltip("인스펙터 식별용 이름.")]
    public string slotName;

    [Tooltip("이 위치에 표시될 공용 행동 버튼 UI.")]
    public BattleActionWheelButtonUI buttonUI;

    [Tooltip("Auto Layout 사용 시 중앙 버튼인지 여부입니다.")]
    public bool isCenter;

    [Tooltip("Auto Layout 사용 시 중앙 기준 각도입니다. 0=오른쪽, 90=위, 180=왼쪽, 270=아래.")]
    public float angleDegrees;

    public BattleActionWheelActionSlotBinding()
    {
        actionSlotIndex = 0;
        slotName = "Slot";
        isCenter = false;
        angleDegrees = 0f;
    }

    public BattleActionWheelActionSlotBinding(int actionSlotIndex, string slotName, bool isCenter, float angleDegrees)
    {
        this.actionSlotIndex = actionSlotIndex;
        this.slotName = slotName;
        this.isCenter = isCenter;
        this.angleDegrees = angleDegrees;
    }
}

/// <summary>
/// 단일 패널 / 유연 슬롯 기반 액션휠.
/// 기본 구조는 중앙 행동 버튼 1개 + 90도 각도의 Top/Right/Bottom 행동 버튼 3개 + 왼쪽 고유 마나 버튼이다.
/// 마나 버튼은 공용 행동 버튼과 분리되어 항상 표시되며, 대상 선택 중에는 클릭만 비활성화된다.
/// </summary>
public class BattleActionWheelUI : MonoBehaviour
{
    private enum PointerButtonKind
    {
        None,
        Right,
        Middle
    }

    [Header("References")]
    [SerializeField] private RectTransform wheelRoot;
    [SerializeField] private CanvasGroup wheelCanvasGroup;
    [SerializeField] private Camera uiCamera;
    [SerializeField] private Image manaGaugeFill;
    [SerializeField] private BattleActionWheelManaButtonUI manaButtonUI;
    [Tooltip("바텀 파티 서머리에서 지정한 공유 소모품을 읽어오기 위한 월드 런 매니저. 비워두면 자동 탐색합니다.")]
    [SerializeField] private WorldRunManager worldRunManager;

    [Header("Action Slots")]
    [SerializeField]
    private List<BattleActionWheelActionSlotBinding> actionSlots = new List<BattleActionWheelActionSlotBinding>
    {
        new BattleActionWheelActionSlotBinding(0, "Center", true, 0f),
        new BattleActionWheelActionSlotBinding(1, "Top", false, 90f),
        new BattleActionWheelActionSlotBinding(2, "Right", false, 0f),
        new BattleActionWheelActionSlotBinding(3, "Bottom", false, 270f),
    };

    [Header("Auto Layout")]
    [SerializeField] private bool autoLayoutSlots = true;
    [SerializeField] private float outerButtonRadius = 135f;
    [SerializeField] private Vector2 centerButtonPosition = Vector2.zero;
    [SerializeField] private bool autoSetButtonSize = false;
    [SerializeField] private Vector2 buttonSize = new Vector2(110f, 110f);

    [Header("Behavior")]
    [SerializeField] private bool openAtLastPosition = true;
    [SerializeField] private Vector2 initialAnchoredPosition = new Vector2(0f, -220f);
    [SerializeField] private bool autoOpenOnPlayerTurn = true;
    [SerializeField] private bool closeOnBlankLeftClick = true;
    [Tooltip("WorldRunManager의 공유 소모품을 찾지 못했을 때만 사용할 예비 인벤토리 슬롯입니다.")]
    [SerializeField] private int fallbackConsumableInventoryIndex = 0;
    [Tooltip("공유 소모품이 지정되어 있지 않을 때도 예비 슬롯 아이템을 표시/사용할지 여부입니다. 기본값 Off를 권장합니다.")]
    [SerializeField] private bool allowFallbackInventoryItemWhenNoSharedConsumable = false;
    [SerializeField] private bool cancelSelectionWhenClosed = true;
    [SerializeField] private bool hideWheelRootGameObject = false;
    [SerializeField] private bool disableOtherActionButtonsDuringTargetSelection = true;

    [Header("Input")]
    [SerializeField] private bool rightClickContextAction = true;
    [SerializeField] private bool rightDragScaleEnabled = false;
    [SerializeField] private bool middleClickMovesWheel = true;
    [SerializeField] private bool middleDragMovesWheel = true;
    [SerializeField] private float pointerDragThreshold = 30f;

    [Header("Scale")]
    [SerializeField] private float[] scaleSteps = new float[] { 1f, 1.25f, 1.5f };
    [SerializeField] private int defaultScaleIndex = 0;

    [Header("Mana Values")]
    [Tooltip("아직 실제 마나 크리스탈 시스템 API가 연결되지 않았을 때 표시할 현재 마나값입니다. 외부에서 SetManaValues를 호출하면 덮어씁니다.")]
    [SerializeField] private int currentManaValue = 120;
    [SerializeField] private int maxManaValue = 120;

    [Header("Root Labels")]
    [SerializeField] private string attackLabel = "공격";
    [SerializeField] private string itemLabel = "도구";
    [SerializeField] private string moveLabel = "이동";
    [SerializeField] private string endTurnLabel = "턴 넘김";

    [Header("Attack Labels")]
    [SerializeField] private string basicAttackFallbackLabel = "평타";
    [SerializeField] private string emptySkillLabel = "";

    [Header("Mana Labels")]
    [SerializeField] private string captureLabel = "포획";
    [SerializeField] private string fleeLabel = "도주";
    [SerializeField] private string preventDeathLabel = "생존";
    [SerializeField] private string teamBuffLabel = "버프";

    [Header("State Labels")]
    [SerializeField] private string cancelLabel = "취소";
    [SerializeField] private string noTargetLabel = "대상없음";
    [SerializeField] private string invalidPositionLabel = "위치불가";
    [SerializeField] private string passiveLabel = "패시브";
    [SerializeField] private string conditionNotMetLabel = "조건불충족";
    [SerializeField] private string unusableLabel = "사용불가";
    [SerializeField] private string noItemLabel = "미지정";
    [SerializeField] private string noAmountLabel = "수량없음";
    [SerializeField] private string moveUnavailableLabel = "이동불가";
    [SerializeField] private string notImplementedLabel = "미구현";

    [Header("Icons")]
    [SerializeField] private Sprite attackIcon;
    [SerializeField] private Sprite itemIcon;
    [SerializeField] private Sprite moveIcon;
    [SerializeField] private Sprite endTurnIcon;
    [SerializeField] private Sprite cancelIcon;
    [SerializeField] private Sprite basicAttackFallbackIcon;
    [SerializeField] private Sprite captureIcon;
    [SerializeField] private Sprite fleeIcon;
    [SerializeField] private Sprite preventDeathIcon;
    [SerializeField] private Sprite teamBuffIcon;

    private readonly Stack<BattleActionWheelDepth> depthStack = new Stack<BattleActionWheelDepth>();

    private BattleManager battleManager;
    private BattleUnit currentActor;
    private BattleInputMode currentInputMode = BattleInputMode.None;
    private List<InventoryStackData> currentInventory;

    private bool canPlayerAct;
    private bool canAcceptAction;
    private bool isOpen;
    private bool wasWaitingForActionLastRefresh;
    private BattleUnit lastRefreshActor;

    private RectTransform wheelParentRect;
    private Canvas parentCanvas;
    private Vector2 lastAnchoredPosition;
    private int currentScaleIndex;

    private PointerButtonKind pressedPointerButton = PointerButtonKind.None;
    private bool pointerPressed;
    private bool pointerDragged;
    private Vector2 pointerPressScreenPosition;
    private Vector2 pointerLastScreenPosition;
    private float scaleDragReferenceDistance;

    public bool IsOpen => isOpen;
    public BattleActionWheelDepth CurrentDepth => depthStack.Count > 0 ? depthStack.Peek() : BattleActionWheelDepth.Root;
    public IReadOnlyList<BattleActionWheelActionSlotBinding> ActionSlots => actionSlots;

    public void Initialize(BattleManager manager)
    {
        battleManager = manager;

        if (worldRunManager == null)
            worldRunManager = UnityEngine.Object.FindFirstObjectByType<WorldRunManager>();

        if (wheelRoot == null)
            wheelRoot = transform as RectTransform;

        if (wheelRoot != null)
        {
            if (wheelCanvasGroup == null)
                wheelCanvasGroup = wheelRoot.GetComponent<CanvasGroup>();
            if (wheelCanvasGroup == null)
                wheelCanvasGroup = wheelRoot.gameObject.AddComponent<CanvasGroup>();
        }

        if (manaButtonUI == null)
            manaButtonUI = GetComponentInChildren<BattleActionWheelManaButtonUI>(true);

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

        currentScaleIndex = Mathf.Clamp(defaultScaleIndex, 0, Mathf.Max(0, scaleSteps != null ? scaleSteps.Length - 1 : 0));
        lastAnchoredPosition = initialAnchoredPosition;

        EnsureRootDepth();
        ApplyAutoLayout();
        ApplyScale();
        CloseImmediate(false);

        wasWaitingForActionLastRefresh = false;
        lastRefreshActor = null;
    }

    private void Reset()
    {
        wheelRoot = transform as RectTransform;
        wheelCanvasGroup = GetComponent<CanvasGroup>();
        if (wheelCanvasGroup == null)
            wheelCanvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void OnValidate()
    {
        EnsureDefaultSlotsIfEmpty();

        if (Application.isPlaying)
            ApplyAutoLayout();
    }

    private void Update()
    {
        HandlePointerInput();
    }

    public void Refresh(BattleUnit actor, bool playerCanAct, BattleInputMode inputMode, List<InventoryStackData> inventory)
    {
        currentActor = actor;
        canPlayerAct = playerCanAct;
        currentInputMode = inputMode;
        currentInventory = inventory;
        canAcceptAction = playerCanAct && actor != null && inputMode == BattleInputMode.WaitingForAction;

        RefreshManaGauge();

        if (!playerCanAct || actor == null)
        {
            CloseImmediate(false);
            wasWaitingForActionLastRefresh = false;
            lastRefreshActor = actor;
            return;
        }

        SyncDepthForTargetSelectionMode();

        bool isWaitingForAction = inputMode == BattleInputMode.WaitingForAction;
        if (isWaitingForAction)
        {
            bool shouldAutoOpen =
                autoOpenOnPlayerTurn &&
                (!wasWaitingForActionLastRefresh || actor != lastRefreshActor);

            if (shouldAutoOpen && !isOpen)
                OpenRoot(false);
        }

        if (isOpen)
            RenderCurrentState();

        wasWaitingForActionLastRefresh = isWaitingForAction;
        lastRefreshActor = actor;
    }

    public void SetManaValues(int current, int max)
    {
        maxManaValue = Mathf.Max(0, max);
        currentManaValue = maxManaValue > 0 ? Mathf.Clamp(current, 0, maxManaValue) : Mathf.Max(0, current);
        RefreshManaGauge();
        if (isOpen)
            RenderManaButton();
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

        if (!isOpen)
            OpenRoot(false);
    }

    public void SetOpenAtLastPosition(bool useLast)
    {
        openAtLastPosition = useLast;
    }

    public void CloseWheel()
    {
        CloseImmediate(cancelSelectionWhenClosed);
    }

    public void OpenWheel()
    {
        OpenRoot(false);
    }

    public void ToggleWheel()
    {
        if (isOpen)
            CloseWheel();
        else
            OpenRoot(false);
    }

    public void SetManaGaugeFill(float normalized)
    {
        if (manaGaugeFill != null)
            manaGaugeFill.fillAmount = Mathf.Clamp01(normalized);
    }

    private void OpenRoot(bool resetPositionToPointer)
    {
        if (!CanOpenForCurrentActor())
            return;

        if (!IsTargetSelectionMode())
            EnsureRootDepth();
        else
            SyncDepthForTargetSelectionMode();

        if (!isOpen)
        {
            OpenAtPreferredPosition();
            SetVisible(true);
        }

        if (resetPositionToPointer)
            MoveWheelToScreenPosition(GetPointerScreenPosition());

        RenderCurrentState();
    }

    private bool CanOpenForCurrentActor()
    {
        return battleManager != null &&
               battleManager.CurrentState == TurnState.PlayerInput &&
               battleManager.CurrentActingUnit != null &&
               battleManager.CurrentActingUnit.Team == TeamType.Ally;
    }

    private void PushDepth(BattleActionWheelDepth depth)
    {
        if (!canAcceptAction)
            return;

        if (CurrentDepth == depth)
        {
            RenderCurrentState();
            return;
        }

        depthStack.Push(depth);
        RenderCurrentState();
    }

    private void PopDepthOrRoot()
    {
        if (IsTargetSelectionMode())
        {
            CancelCurrentTargetSelectionOnly();
            return;
        }

        if (depthStack.Count > 1)
            depthStack.Pop();
        else
            EnsureRootDepth();

        RenderCurrentState();
    }

    private void EnsureRootDepth()
    {
        if (depthStack.Count <= 0)
            depthStack.Push(BattleActionWheelDepth.Root);
    }

    private void SetSingleDepth(BattleActionWheelDepth depth)
    {
        depthStack.Clear();
        depthStack.Push(depth);
    }

    private void ClearDepthToRoot()
    {
        depthStack.Clear();
        depthStack.Push(BattleActionWheelDepth.Root);
    }

    private bool IsTargetSelectionMode()
    {
        return canPlayerAct &&
               currentInputMode != BattleInputMode.None &&
               currentInputMode != BattleInputMode.WaitingForAction;
    }

    private void SyncDepthForTargetSelectionMode()
    {
        if (!IsTargetSelectionMode())
            return;

        switch (currentInputMode)
        {
            case BattleInputMode.WaitingForSkillTarget:
                SetSingleDepth(BattleActionWheelDepth.Attack);
                break;
            case BattleInputMode.WaitingForCaptureTarget:
                SetSingleDepth(BattleActionWheelDepth.Mana);
                break;
            case BattleInputMode.WaitingForMoveTarget:
            case BattleInputMode.WaitingForItemTarget:
                SetSingleDepth(BattleActionWheelDepth.Root);
                break;
        }
    }

    private void RenderCurrentState()
    {
        if (!isOpen)
            return;

        EnsureRootDepth();
        RenderManaButton();

        switch (CurrentDepth)
        {
            case BattleActionWheelDepth.Attack:
                RenderAttackDepth();
                break;
            case BattleActionWheelDepth.Mana:
                RenderManaDepth();
                break;
            case BattleActionWheelDepth.Root:
            default:
                RenderRootDepth();
                break;
        }
    }

    private void RenderRootDepth()
    {
        Dictionary<int, BattleActionWheelButtonViewData> actions = new Dictionary<int, BattleActionWheelButtonViewData>();
        actions[0] = MaybeCancelReplacement(BattleActionWheelDepth.Root, 0) ??
                     MakeButton(attackLabel, attackIcon, canAcceptAction, () => PushDepth(BattleActionWheelDepth.Attack));

        actions[1] = MaybeCancelReplacement(BattleActionWheelDepth.Root, 1) ?? MakeMoveButton();
        actions[2] = MaybeCancelReplacement(BattleActionWheelDepth.Root, 2) ?? MakeItemButton();
        actions[3] = MaybeCancelReplacement(BattleActionWheelDepth.Root, 3) ??
                     MakeButton(endTurnLabel, endTurnIcon, canAcceptAction, OnEndTurnPressed);

        ApplyActions(actions);
    }

    private void RenderAttackDepth()
    {
        Dictionary<int, BattleActionWheelButtonViewData> actions = new Dictionary<int, BattleActionWheelButtonViewData>();

        int maxSlotIndex = GetMaxConfiguredActionSlotIndex();
        for (int slotIndex = 0; slotIndex <= maxSlotIndex; slotIndex++)
        {
            BattleActionWheelButtonViewData? cancel = MaybeCancelReplacement(BattleActionWheelDepth.Attack, slotIndex);
            if (cancel.HasValue)
            {
                actions[slotIndex] = cancel.Value;
                continue;
            }

            SkillDefinition skill = currentActor != null ? currentActor.GetActionSkillAt(slotIndex) : null;
            actions[slotIndex] = skill != null ? MakeSkillButton(slotIndex, skill) : MakeEmpty(emptySkillLabel);
        }

        ApplyActions(actions);
    }

    private void RenderManaDepth()
    {
        Dictionary<int, BattleActionWheelButtonViewData> actions = new Dictionary<int, BattleActionWheelButtonViewData>();
        actions[0] = MaybeCancelReplacement(BattleActionWheelDepth.Mana, 0) ?? MakeCaptureButton();
        actions[1] = MaybeCancelReplacement(BattleActionWheelDepth.Mana, 1) ?? MakeFleeButton();
        actions[2] = MaybeCancelReplacement(BattleActionWheelDepth.Mana, 2) ?? MakeDisabledButton(preventDeathLabel, preventDeathIcon, notImplementedLabel);
        actions[3] = MaybeCancelReplacement(BattleActionWheelDepth.Mana, 3) ?? MakeDisabledButton(teamBuffLabel, teamBuffIcon, notImplementedLabel);

        ApplyActions(actions);
    }

    private BattleActionWheelButtonViewData? MaybeCancelReplacement(BattleActionWheelDepth depth, int actionSlotIndex)
    {
        if (!IsTargetSelectionMode())
            return null;

        if (IsCancelSlot(depth, actionSlotIndex))
            return MakeButton(cancelLabel, cancelIcon, true, CancelCurrentTargetSelectionOnly);

        if (disableOtherActionButtonsDuringTargetSelection)
            return MakeLockedButtonForTargetSelection(depth, actionSlotIndex);

        return null;
    }

    private bool IsCancelSlot(BattleActionWheelDepth depth, int actionSlotIndex)
    {
        switch (currentInputMode)
        {
            case BattleInputMode.WaitingForSkillTarget:
                return depth == BattleActionWheelDepth.Attack && actionSlotIndex == Mathf.Max(0, battleManager != null ? battleManager.SelectedSkillSlotIndex : -1);
            case BattleInputMode.WaitingForMoveTarget:
                return depth == BattleActionWheelDepth.Root && actionSlotIndex == 1;
            case BattleInputMode.WaitingForItemTarget:
                return depth == BattleActionWheelDepth.Root && actionSlotIndex == 2;
            case BattleInputMode.WaitingForCaptureTarget:
                return depth == BattleActionWheelDepth.Mana && actionSlotIndex == 0;
            default:
                return false;
        }
    }

    private BattleActionWheelButtonViewData MakeLockedButtonForTargetSelection(BattleActionWheelDepth depth, int actionSlotIndex)
    {
        // 대상 선택 중에는 눌렀던 버튼만 취소 버튼으로 바꾸고, 나머지는 현재 depth의 버튼을 흐리게 보여준다.
        switch (depth)
        {
            case BattleActionWheelDepth.Attack:
            {
                SkillDefinition skill = currentActor != null ? currentActor.GetActionSkillAt(actionSlotIndex) : null;
                if (skill == null)
                    return MakeEmpty(emptySkillLabel);
                string label = GetSkillLabel(actionSlotIndex, skill);
                Sprite icon = GetSkillIcon(actionSlotIndex, skill);
                return MakeDisabledButton(label, icon, string.Empty);
            }
            case BattleActionWheelDepth.Mana:
            {
                if (actionSlotIndex == 0) return MakeDisabledButton(captureLabel, captureIcon, string.Empty);
                if (actionSlotIndex == 1) return MakeDisabledButton(fleeLabel, fleeIcon, string.Empty);
                if (actionSlotIndex == 2) return MakeDisabledButton(preventDeathLabel, preventDeathIcon, string.Empty);
                if (actionSlotIndex == 3) return MakeDisabledButton(teamBuffLabel, teamBuffIcon, string.Empty);
                return MakeEmpty();
            }
            case BattleActionWheelDepth.Root:
            default:
            {
                if (actionSlotIndex == 0) return MakeDisabledButton(attackLabel, attackIcon, string.Empty);
                if (actionSlotIndex == 1) return MakeDisabledButton(moveLabel, moveIcon, string.Empty);
                if (actionSlotIndex == 2) return MakeDisabledButton(GetItemLabel(), GetItemIcon(), string.Empty);
                if (actionSlotIndex == 3) return MakeDisabledButton(endTurnLabel, endTurnIcon, string.Empty);
                return MakeEmpty();
            }
        }
    }

    private BattleActionWheelButtonViewData MakeSkillButton(int slotIndex, SkillDefinition skill)
    {
        string label = GetSkillLabel(slotIndex, skill);
        Sprite icon = GetSkillIcon(slotIndex, skill);

        int cooldownRemaining = currentActor != null ? currentActor.GetRemainingCooldown(skill) : 0;
        int cooldownTotal = skill != null ? Mathf.Max(1, skill.cooldownTurns) : 1;

        if (cooldownRemaining > 0)
        {
            return new BattleActionWheelButtonViewData(
                label,
                icon,
                true,
                false,
                false,
                BattleActionWheelButtonFrameType.Hex,
                null,
                cooldownRemaining,
                cooldownTotal,
                null,
                false);
        }

        string reason = GetSkillUnusableReason(skill);
        bool usable = string.IsNullOrEmpty(reason);
        int capturedSlotIndex = slotIndex;
        return new BattleActionWheelButtonViewData(
            label,
            icon,
            true,
            usable,
            false,
            BattleActionWheelButtonFrameType.Hex,
            usable ? (UnityAction)(() => OnActionSlotPressed(capturedSlotIndex)) : null,
            0,
            cooldownTotal,
            reason,
            !usable);
    }

    private string GetSkillLabel(int slotIndex, SkillDefinition skill)
    {
        if (skill != null && !string.IsNullOrWhiteSpace(skill.skillName))
            return skill.skillName;

        return slotIndex == 0 ? basicAttackFallbackLabel : $"스킬 {slotIndex}";
    }

    private Sprite GetSkillIcon(int slotIndex, SkillDefinition skill)
    {
        if (skill != null && skill.icon != null)
            return skill.icon;

        return slotIndex == 0 ? basicAttackFallbackIcon : null;
    }

    private string GetSkillUnusableReason(SkillDefinition skill)
    {
        if (!canAcceptAction)
            return unusableLabel;

        if (currentActor == null || skill == null)
            return unusableLabel;

        if (skill.castType == SkillCastType.Passive)
            return passiveLabel;

        if (currentActor.IsSkillDisabled(skill))
            return unusableLabel;

        if (skill.activeGimmick == ActiveSkillGimmick.DelayedReinforcement && !currentActor.IsConditionalSkillArmed(skill))
            return conditionNotMetLabel;

        if (!skill.CanBeUsedFromSlot(currentActor.SlotIndex))
            return invalidPositionLabel;

        if (currentActor.GetRemainingCooldown(skill) > 0)
            return string.Empty;

        List<BattleUnit> validTargets = BattleTargeting.GetValidSkillTargets(
            currentActor,
            skill,
            battleManager != null ? battleManager.AllyFormation : null,
            battleManager != null ? battleManager.EnemyFormation : null);

        if (validTargets == null || validTargets.Count <= 0)
            return noTargetLabel;

        return string.Empty;
    }

    private BattleActionWheelButtonViewData MakeMoveButton()
    {
        if (!canAcceptAction)
            return MakeDisabledButton(moveLabel, moveIcon, unusableLabel);

        List<BattleUnit> validTargets = BattleTargeting.GetMovableTargets(
            currentActor,
            battleManager != null ? battleManager.AllyFormation : null);

        bool usable = validTargets != null && validTargets.Count > 0;
        return usable
            ? MakeButton(moveLabel, moveIcon, true, OnMovePressed)
            : MakeDisabledButton(moveLabel, moveIcon, moveUnavailableLabel);
    }

    private BattleActionWheelButtonViewData MakeItemButton()
    {
        ItemDefinition configuredItem = GetConfiguredSharedConsumableItem();
        string label = GetItemLabel();
        Sprite icon = GetItemIcon();

        if (configuredItem == null && !allowFallbackInventoryItemWhenNoSharedConsumable)
            return MakeDisabledButton(label, icon, noItemLabel);

        if (!TryGetActionWheelItem(out _, out InventoryStackData stack))
            return MakeDisabledButton(label, icon, configuredItem == null ? noItemLabel : noAmountLabel);

        List<BattleUnit> validTargets = BattleTargeting.GetValidItemTargets(
            currentActor,
            stack.item,
            battleManager != null ? battleManager.AllyFormation : null,
            battleManager != null ? battleManager.EnemyFormation : null);

        bool usable = canAcceptAction && validTargets != null && validTargets.Count > 0;
        return usable
            ? MakeButton(label, icon, true, OnItemPressed)
            : MakeDisabledButton(label, icon, noTargetLabel);
    }

    private BattleActionWheelButtonViewData MakeCaptureButton()
    {
        bool canCore = canAcceptAction && battleManager != null && battleManager.IsMainPlayerCharacter(currentActor);
        if (!canCore)
            return MakeDisabledButton(captureLabel, captureIcon, conditionNotMetLabel);

        bool hasTarget = battleManager != null && battleManager.HasAnyCaptureTarget(currentActor);
        bool canCapture = battleManager != null && battleManager.CanActorUseCaptureCommand(currentActor);

        if (!hasTarget || !canCapture)
            return MakeDisabledButton(captureLabel, captureIcon, noTargetLabel);

        return MakeButton(captureLabel, captureIcon, true, OnCapturePressed);
    }

    private BattleActionWheelButtonViewData MakeFleeButton()
    {
        bool usable = canAcceptAction && battleManager != null && battleManager.IsMainPlayerCharacter(currentActor);
        return usable
            ? MakeButton(fleeLabel, fleeIcon, true, OnFleePressed)
            : MakeDisabledButton(fleeLabel, fleeIcon, conditionNotMetLabel);
    }

    private BattleActionWheelButtonViewData MakeButton(string label, Sprite icon, bool interactable, UnityAction onClick)
    {
        return new BattleActionWheelButtonViewData(
            label,
            icon,
            true,
            interactable,
            false,
            BattleActionWheelButtonFrameType.Hex,
            interactable ? onClick : null,
            0,
            0,
            interactable ? null : unusableLabel,
            !interactable);
    }

    private BattleActionWheelButtonViewData MakeDisabledButton(string label, Sprite icon, string reason)
    {
        return new BattleActionWheelButtonViewData(
            label,
            icon,
            true,
            false,
            false,
            BattleActionWheelButtonFrameType.Hex,
            null,
            0,
            0,
            reason,
            true);
    }

    private BattleActionWheelButtonViewData MakeEmpty(string label = "")
    {
        return new BattleActionWheelButtonViewData(
            label,
            null,
            true,
            false,
            true,
            BattleActionWheelButtonFrameType.Hex,
            null);
    }

    private void ApplyActions(Dictionary<int, BattleActionWheelButtonViewData> actions)
    {
        if (actionSlots == null)
            return;

        for (int i = 0; i < actionSlots.Count; i++)
        {
            BattleActionWheelActionSlotBinding slot = actionSlots[i];
            if (slot == null || slot.buttonUI == null)
                continue;

            BattleActionWheelButtonViewData data;
            if (actions != null && actions.TryGetValue(slot.actionSlotIndex, out data))
                slot.buttonUI.Apply(data);
            else
                slot.buttonUI.Apply(BattleActionWheelButtonViewData.Empty());
        }
    }

    private int GetMaxConfiguredActionSlotIndex()
    {
        int max = 0;
        if (actionSlots == null)
            return max;

        for (int i = 0; i < actionSlots.Count; i++)
        {
            if (actionSlots[i] != null)
                max = Mathf.Max(max, actionSlots[i].actionSlotIndex);
        }

        return max;
    }

    private string GetItemLabel()
    {
        ItemDefinition sharedItem = GetConfiguredSharedConsumableItem();
        if (sharedItem != null && !string.IsNullOrWhiteSpace(sharedItem.itemName))
            return sharedItem.itemName;

        if (allowFallbackInventoryItemWhenNoSharedConsumable && TryGetFallbackInventoryStack(out _, out InventoryStackData fallbackStack))
        {
            if (fallbackStack != null && fallbackStack.item != null && !string.IsNullOrWhiteSpace(fallbackStack.item.itemName))
                return fallbackStack.item.itemName;
        }

        return itemLabel;
    }

    private Sprite GetItemIcon()
    {
        ItemDefinition sharedItem = GetConfiguredSharedConsumableItem();
        if (sharedItem != null && sharedItem.icon != null)
            return sharedItem.icon;

        if (allowFallbackInventoryItemWhenNoSharedConsumable && TryGetFallbackInventoryStack(out _, out InventoryStackData fallbackStack))
        {
            if (fallbackStack != null && fallbackStack.item != null && fallbackStack.item.icon != null)
                return fallbackStack.item.icon;
        }

        return itemIcon;
    }

    private ItemDefinition GetConfiguredSharedConsumableItem()
    {
        if (worldRunManager == null)
            worldRunManager = UnityEngine.Object.FindFirstObjectByType<WorldRunManager>();

        return worldRunManager != null ? worldRunManager.GetSharedConsumableItem() : null;
    }

    private bool TryGetActionWheelItem(out int inventoryIndex, out InventoryStackData stack)
    {
        inventoryIndex = -1;
        stack = null;

        ItemDefinition sharedItem = GetConfiguredSharedConsumableItem();
        if (sharedItem != null && TryFindInventoryStackByItem(sharedItem, out inventoryIndex, out stack))
            return true;

        if (sharedItem != null)
            return false;

        if (allowFallbackInventoryItemWhenNoSharedConsumable)
            return TryGetFallbackInventoryStack(out inventoryIndex, out stack);

        return false;
    }

    private bool TryFindInventoryStackByItem(ItemDefinition item, out int inventoryIndex, out InventoryStackData stack)
    {
        inventoryIndex = -1;
        stack = null;

        if (item == null || currentInventory == null)
            return false;

        for (int i = 0; i < currentInventory.Count; i++)
        {
            InventoryStackData candidate = currentInventory[i];
            if (candidate == null || candidate.item == null || candidate.amount <= 0)
                continue;

            if (candidate.item == item)
            {
                inventoryIndex = i;
                stack = candidate;
                return true;
            }
        }

        return false;
    }

    private bool TryGetFallbackInventoryStack(out int inventoryIndex, out InventoryStackData stack)
    {
        inventoryIndex = -1;
        stack = null;

        if (currentInventory == null || currentInventory.Count <= 0)
            return false;

        int idx = Mathf.Clamp(fallbackConsumableInventoryIndex, 0, currentInventory.Count - 1);
        InventoryStackData candidate = currentInventory[idx];
        if (candidate == null || candidate.item == null || candidate.amount <= 0)
            return false;

        inventoryIndex = idx;
        stack = candidate;
        return true;
    }

    private void OnActionSlotPressed(int slotIndex)
    {
        if (!canAcceptAction)
            return;

        battleManager?.OnActionSlotPressed(slotIndex);
    }

    private void OnMovePressed()
    {
        if (!canAcceptAction)
            return;

        battleManager?.OnMoveButtonPressed();
    }

    private void OnItemPressed()
    {
        if (!canAcceptAction)
            return;

        if (!TryGetActionWheelItem(out int inventoryIndex, out _))
            return;

        battleManager?.OnInventorySlotPressed(inventoryIndex);
    }

    private void OnCapturePressed()
    {
        if (!canAcceptAction)
            return;

        battleManager?.OnCaptureButtonPressed();
    }

    private void OnFleePressed()
    {
        if (!canAcceptAction)
            return;

        battleManager?.OnFleeButtonPressed();
    }

    private void OnEndTurnPressed()
    {
        if (!canAcceptAction)
            return;

        battleManager?.OnEndTurnButtonPressed();
    }

    private void CancelCurrentTargetSelectionOnly()
    {
        if (battleManager == null)
            return;

        battleManager.OnCancelButtonPressed();
    }

    private void CancelCurrentSelectionForClose()
    {
        if (!cancelSelectionWhenClosed || battleManager == null)
            return;

        if (battleManager.CurrentState != TurnState.PlayerInput)
            return;

        if (battleManager.InputMode != BattleInputMode.WaitingForAction)
            battleManager.OnCancelButtonPressed();
    }

    private void RenderManaButton()
    {
        if (manaButtonUI == null)
            return;

        bool interactable = isOpen && canAcceptAction && !IsTargetSelectionMode();
        manaButtonUI.SetVisible(isOpen);
        manaButtonUI.Apply(currentManaValue, maxManaValue, interactable, () => PushDepth(BattleActionWheelDepth.Mana));
    }

    private void SetVisible(bool visible)
    {
        isOpen = visible;

        if (wheelRoot != null && hideWheelRootGameObject)
            wheelRoot.gameObject.SetActive(visible);

        if (wheelCanvasGroup != null)
        {
            wheelCanvasGroup.alpha = visible ? 1f : 0f;
            wheelCanvasGroup.blocksRaycasts = visible;
            wheelCanvasGroup.interactable = visible;
        }

        if (manaButtonUI != null)
            manaButtonUI.SetVisible(visible);
    }

    private void CloseImmediate(bool cancelSelection)
    {
        if (cancelSelection)
            CancelCurrentSelectionForClose();

        ClearDepthToRoot();
        ApplyActions(new Dictionary<int, BattleActionWheelButtonViewData>());
        SetVisible(false);
    }

    private void OpenAtPreferredPosition()
    {
        if (wheelRoot == null)
            return;

        wheelRoot.anchoredPosition = openAtLastPosition ? lastAnchoredPosition : initialAnchoredPosition;
    }

    private void RefreshManaGauge()
    {
        if (manaGaugeFill != null)
        {
            float normalized = maxManaValue > 0 ? currentManaValue / (float)maxManaValue : 0f;
            manaGaugeFill.fillAmount = Mathf.Clamp01(normalized);
        }

        if (isOpen)
            RenderManaButton();
    }

    private void ApplyAutoLayout()
    {
        if (!autoLayoutSlots || actionSlots == null)
            return;

        for (int i = 0; i < actionSlots.Count; i++)
        {
            BattleActionWheelActionSlotBinding slot = actionSlots[i];
            if (slot == null || slot.buttonUI == null || slot.buttonUI.RectTransform == null)
                continue;

            if (slot.isCenter)
            {
                slot.buttonUI.RectTransform.anchoredPosition = centerButtonPosition;
            }
            else
            {
                float rad = slot.angleDegrees * Mathf.Deg2Rad;
                Vector2 pos = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * outerButtonRadius;
                slot.buttonUI.RectTransform.anchoredPosition = pos;
            }

            if (autoSetButtonSize)
                slot.buttonUI.RectTransform.sizeDelta = buttonSize;
        }
    }

    private void HandlePointerInput()
    {
        if (battleManager == null || battleManager.CurrentState != TurnState.PlayerInput)
            return;

        if (IsPointerPressedThisFrame(PointerButtonKind.Right) && rightClickContextAction)
        {
            BeginPointerPress(PointerButtonKind.Right);
            return;
        }

        if (IsPointerPressedThisFrame(PointerButtonKind.Middle) && (middleClickMovesWheel || middleDragMovesWheel))
        {
            BeginPointerPress(PointerButtonKind.Middle);
            return;
        }

        if (!pointerPressed)
            return;

        Vector2 currentPos = GetPointerScreenPosition();
        Vector2 deltaFromPress = currentPos - pointerPressScreenPosition;

        bool canDragCurrentButton =
            pressedPointerButton == PointerButtonKind.Middle && middleDragMovesWheel ||
            pressedPointerButton == PointerButtonKind.Right && rightDragScaleEnabled;

        if (canDragCurrentButton && !pointerDragged && deltaFromPress.magnitude >= pointerDragThreshold)
            pointerDragged = true;

        if (pressedPointerButton == PointerButtonKind.Middle && pointerDragged && middleDragMovesWheel)
        {
            MoveWheelByScreenDelta(currentPos - pointerLastScreenPosition);
        }
        else if (pressedPointerButton == PointerButtonKind.Right && pointerDragged && rightDragScaleEnabled)
        {
            HandleRightScaleDrag(currentPos);
        }

        pointerLastScreenPosition = currentPos;

        if (IsPointerReleasedThisFrame(pressedPointerButton))
        {
            EndPointerPress(currentPos);
        }
    }

    private void BeginPointerPress(PointerButtonKind buttonKind)
    {
        pointerPressed = true;
        pointerDragged = false;
        pressedPointerButton = buttonKind;
        pointerPressScreenPosition = GetPointerScreenPosition();
        pointerLastScreenPosition = pointerPressScreenPosition;
        scaleDragReferenceDistance = GetDistanceFromWheelCenter(pointerPressScreenPosition);
    }

    private void EndPointerPress(Vector2 releaseScreenPosition)
    {
        if (pressedPointerButton == PointerButtonKind.Right)
        {
            if (!pointerDragged && rightClickContextAction)
                HandleRightClickContextAction();
        }
        else if (pressedPointerButton == PointerButtonKind.Middle)
        {
            if (!pointerDragged && middleClickMovesWheel)
                MoveWheelToScreenPosition(releaseScreenPosition);
        }

        pointerPressed = false;
        pointerDragged = false;
        pressedPointerButton = PointerButtonKind.None;
    }

    private void HandleRightClickContextAction()
    {
        if (IsTargetSelectionMode())
        {
            CancelCurrentTargetSelectionOnly();
            return;
        }

        if (!isOpen)
        {
            OpenRoot(false);
            return;
        }

        if (CurrentDepth != BattleActionWheelDepth.Root && depthStack.Count > 1)
        {
            PopDepthOrRoot();
            return;
        }

        CloseWheel();
    }

    private void HandleRightScaleDrag(Vector2 mouseScreenPosition)
    {
        float currentDistance = GetDistanceFromWheelCenter(mouseScreenPosition);
        float delta = currentDistance - scaleDragReferenceDistance;

        if (delta >= pointerDragThreshold)
        {
            SetScaleIndex(currentScaleIndex + 1);
            scaleDragReferenceDistance = currentDistance;
        }
        else if (delta <= -pointerDragThreshold)
        {
            SetScaleIndex(currentScaleIndex - 1);
            scaleDragReferenceDistance = currentDistance;
        }
    }

    private void MoveWheelByScreenDelta(Vector2 screenDelta)
    {
        if (wheelRoot == null || wheelParentRect == null)
            return;

        RectTransform targetRect = wheelParentRect != null ? wheelParentRect : wheelRoot.parent as RectTransform;
        if (targetRect == null)
            return;

        Camera eventCamera = parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? (uiCamera != null ? uiCamera : parentCanvas.worldCamera)
            : null;

        Vector2 localBefore;
        Vector2 localAfter;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRect, pointerLastScreenPosition, eventCamera, out localBefore);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRect, pointerLastScreenPosition + screenDelta, eventCamera, out localAfter);

        wheelRoot.anchoredPosition += localAfter - localBefore;
        lastAnchoredPosition = wheelRoot.anchoredPosition;
    }

    private void MoveWheelToScreenPosition(Vector2 screenPosition)
    {
        if (wheelRoot == null)
            return;

        RectTransform targetRect = wheelParentRect != null ? wheelParentRect : wheelRoot.parent as RectTransform;
        if (targetRect == null)
            return;

        Camera eventCamera = parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? (uiCamera != null ? uiCamera : parentCanvas.worldCamera)
            : null;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRect, screenPosition, eventCamera, out Vector2 localPoint))
            return;

        wheelRoot.anchoredPosition = localPoint;
        lastAnchoredPosition = wheelRoot.anchoredPosition;
    }

    private float GetDistanceFromWheelCenter(Vector2 mouseScreenPosition)
    {
        if (wheelRoot == null)
            return 0f;

        Vector2 centerScreenPosition = RectTransformUtility.WorldToScreenPoint(uiCamera, wheelRoot.position);
        return Vector2.Distance(centerScreenPosition, mouseScreenPosition);
    }

    private void SetScaleIndex(int index)
    {
        if (scaleSteps == null || scaleSteps.Length == 0)
            return;

        currentScaleIndex = Mathf.Clamp(index, 0, scaleSteps.Length - 1);
        ApplyScale();
    }

    private void ApplyScale()
    {
        if (wheelRoot == null || scaleSteps == null || scaleSteps.Length <= 0)
            return;

        float scale = scaleSteps[Mathf.Clamp(currentScaleIndex, 0, scaleSteps.Length - 1)];
        wheelRoot.localScale = new Vector3(scale, scale, 1f);
    }

    private Vector2 GetPointerScreenPosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            return Mouse.current.position.ReadValue();
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.mousePosition;
#else
        return Vector2.zero;
#endif
    }

    private bool IsPointerPressedThisFrame(PointerButtonKind kind)
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            if (kind == PointerButtonKind.Right)
                return Mouse.current.rightButton.wasPressedThisFrame;
            if (kind == PointerButtonKind.Middle)
                return Mouse.current.middleButton.wasPressedThisFrame;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (kind == PointerButtonKind.Right)
            return Input.GetMouseButtonDown(1);
        if (kind == PointerButtonKind.Middle)
            return Input.GetMouseButtonDown(2);
#endif
        return false;
    }

    private bool IsPointerReleasedThisFrame(PointerButtonKind kind)
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            if (kind == PointerButtonKind.Right)
                return Mouse.current.rightButton.wasReleasedThisFrame;
            if (kind == PointerButtonKind.Middle)
                return Mouse.current.middleButton.wasReleasedThisFrame;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (kind == PointerButtonKind.Right)
            return Input.GetMouseButtonUp(1);
        if (kind == PointerButtonKind.Middle)
            return Input.GetMouseButtonUp(2);
#endif
        return false;
    }

    private void EnsureDefaultSlotsIfEmpty()
    {
        if (actionSlots != null && actionSlots.Count > 0)
            return;

        actionSlots = new List<BattleActionWheelActionSlotBinding>
        {
            new BattleActionWheelActionSlotBinding(0, "Center", true, 0f),
            new BattleActionWheelActionSlotBinding(1, "Top", false, 90f),
            new BattleActionWheelActionSlotBinding(2, "Right", false, 0f),
            new BattleActionWheelActionSlotBinding(3, "Bottom", false, 270f),
        };
    }
}
