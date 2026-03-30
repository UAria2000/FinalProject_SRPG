using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    [Header("Prepared Battle Data")]
    [SerializeField] private PartyDefinition allyPartyDefinition;
    [SerializeField] private PartyDefinition enemyPartyDefinition;

    [Header("Exploration")]
    [SerializeField] private bool autoStartBattleOnStart = true;

    [Header("Controllers")]
    [SerializeField] private BattleViewManager viewManager;
    [SerializeField] private BattleUIController uiController;
    [SerializeField] private BattleLogController logController;
    [SerializeField] private BattleActionController actionController;
    [SerializeField] private BattleInputController inputController;
    [SerializeField] private EnemyAIController enemyAIController;
    [SerializeField] private BattlePassiveController passiveController;
    [SerializeField] private BattleSkillGimmickController skillGimmickController;
    [SerializeField] private BattleFlowController flowController;
    [SerializeField] private BattleCaptureController captureController;
    [SerializeField] private BattlePersistenceController persistenceController;
    [SerializeField] private BattlePresentationController presentationController;

    [Header("Enemy Skill Hover Targets")]
    [SerializeField] private GameObject[] enemySkillHoverTargets = new GameObject[4];

    [Header("Animation")]
    [SerializeField] private float turnDelay = 0.25f;
    [SerializeField] private float moveAnimationDuration = 0.35f;
    [SerializeField] private float attackMoveRatio = 0.45f;
    [SerializeField] private float attackMoveMaxDistance = 260f;
    [SerializeField] private float attackMoveDuration = 0.55f;

    [Header("Popup Log")]
    [SerializeField] private GameObject popupLogPanel;

    [Header("Capture")]
    [SerializeField] private int inventoryMaxSlotCount = 8;
    [SerializeField] private int maxCaptureAttemptsPerEnemyInstance = 3;
    [SerializeField] private List<CaptureChanceRange> captureChanceRanges = new List<CaptureChanceRange>()
    {
        new CaptureChanceRange { minHpPercentExclusive = 0f,  maxHpPercentInclusive = 20f,  chancePercent = 70f },
        new CaptureChanceRange { minHpPercentExclusive = 20f, maxHpPercentInclusive = 40f,  chancePercent = 55f },
        new CaptureChanceRange { minHpPercentExclusive = 40f, maxHpPercentInclusive = 60f,  chancePercent = 40f },
        new CaptureChanceRange { minHpPercentExclusive = 60f, maxHpPercentInclusive = 80f,  chancePercent = 25f },
        new CaptureChanceRange { minHpPercentExclusive = 80f, maxHpPercentInclusive = 100f, chancePercent = 10f },
    };

    private BattleFormation allyFormation;
    private BattleFormation enemyFormation;

    private bool waitingForPlayerAction;
    private bool battleStarted;
    private int currentRound;
    private bool currentTurnSkippedByStatus;
    private bool allyDeadUnitPresentThisTurn;
    private bool enemyDeadUnitPresentThisTurn;
    private bool battleEndEventSent;

    public event Action<BattleResultType> BattleEnded;

    public BattleFormation AllyFormation { get { return allyFormation; } }
    public BattleFormation EnemyFormation { get { return enemyFormation; } }
    public PartyDefinition AllyPartyDefinition { get { return allyPartyDefinition; } }
    public PartyDefinition EnemyPartyDefinition { get { return enemyPartyDefinition; } }
    public BattleActionController ActionController { get { return actionController; } }
    public BattleInputController InputController { get { return inputController; } }
    public BattleViewManager ViewManager { get { return viewManager; } }
    public BattleSkillGimmickController SkillGimmickController { get { return skillGimmickController; } }
    public BattlePresentationController PresentationController { get { return presentationController; } }
    public int CurrentRound { get { return currentRound; } }

    public TurnState CurrentState { get; private set; }
    public BattleResultType BattleResult { get; private set; }
    public BattleInputMode InputMode { get; private set; }

    public bool IsBattleInProgress
    {
        get { return battleStarted && BattleResult == BattleResultType.None; }
    }

    public bool WaitingForPlayerAction { get { return waitingForPlayerAction; } }
    public bool CurrentTurnSkippedByStatus { get { return currentTurnSkippedByStatus; } }
    public bool AllyDeadUnitPresentThisTurn { get { return allyDeadUnitPresentThisTurn; } }
    public bool EnemyDeadUnitPresentThisTurn { get { return enemyDeadUnitPresentThisTurn; } }
    public bool BattleEndEventSent { get { return battleEndEventSent; } }

    public BattleUnit CurrentActingUnit { get; private set; }
    public BattleUnit LastShownAllyUnit { get; private set; }
    public BattleUnit SelectedEnemyInfoUnit { get; set; }

    public SkillDefinition SelectedSkill { get; set; }
    public int SelectedSkillSlotIndex { get; set; } = -1;
    public int SelectedInventoryIndex { get; set; } = -1;

    public float TurnDelay { get { return turnDelay; } }
    public float MoveAnimationDuration { get { return moveAnimationDuration; } }
    public float AttackMoveRatio { get { return attackMoveRatio; } }
    public float AttackMoveMaxDistance { get { return attackMoveMaxDistance; } }
    public float AttackMoveDuration { get { return attackMoveDuration; } }

    private void Start()
    {
        if (flowController == null)
            flowController = GetOrAddComponent<BattleFlowController>();
        if (captureController == null)
            captureController = GetOrAddComponent<BattleCaptureController>();
        if (persistenceController == null)
            persistenceController = GetOrAddComponent<BattlePersistenceController>();
        if (presentationController == null)
            presentationController = GetOrAddComponent<BattlePresentationController>();
        if (passiveController == null)
            passiveController = GetOrAddComponent<BattlePassiveController>();
        if (skillGimmickController == null)
            skillGimmickController = GetOrAddComponent<BattleSkillGimmickController>();

        if (uiController != null)
        {
            uiController.Initialize(this);
            uiController.BindButtonEvents();
            uiController.BindEnemySkillHoverEvents(enemySkillHoverTargets);
        }

        if (actionController != null)
            actionController.Initialize(this, viewManager, logController);

        if (inputController != null)
            inputController.Initialize(this, uiController, actionController, logController);

        if (enemyAIController != null)
            enemyAIController.Initialize(this);

        if (passiveController != null)
            passiveController.Initialize(this, logController);

        if (skillGimmickController != null)
            skillGimmickController.Initialize(this, logController);

        if (captureController != null)
        {
            captureController.Initialize(
                this,
                inventoryMaxSlotCount,
                maxCaptureAttemptsPerEnemyInstance,
                captureChanceRanges);
        }

        if (persistenceController != null)
            persistenceController.Initialize(this);

        if (presentationController != null)
            presentationController.Initialize(this, uiController, popupLogPanel);

        if (flowController != null)
        {
            flowController.Initialize(
                this,
                viewManager,
                uiController,
                logController,
                enemyAIController,
                passiveController,
                skillGimmickController,
                captureController,
                persistenceController);
        }

        if (autoStartBattleOnStart)
            StartBattle();
    }

    public void SetEnemyPartyDefinition(PartyDefinition definition)
    {
        enemyPartyDefinition = definition;
    }

    public void StartBattle()
    {
        if (flowController != null)
            flowController.StartBattle();
    }

    public void RefreshAllUI()
    {
        if (presentationController != null)
            presentationController.RefreshAllUI();
    }

    public IEnumerator HandleDeathsAndCompressionRoutine()
    {
        if (flowController != null)
            return flowController.HandleDeathsAndCompressionRoutine();

        return EmptyRoutine();
    }

    public void OnActionExecutionFinished(bool consumeTurn)
    {
        if (flowController != null)
            flowController.OnActionExecutionFinished(consumeTurn);
    }

    public void NotifyUnitLeftBattle(BattleUnit unit)
    {
        if (flowController != null)
            flowController.NotifyUnitLeftBattle(unit);
    }

    public bool IsUnitInBattle(BattleUnit unit)
    {
        return flowController != null && flowController.IsUnitInBattle(unit);
    }

    public void ResetPersistentAllyPartyHPForNewMap()
    {
        if (persistenceController != null)
            persistenceController.ResetPersistentAllyPartyHPForNewMap();
    }

    public bool IsMainPlayerCharacter(BattleUnit unit)
    {
        return captureController != null && captureController.IsMainPlayerCharacter(unit);
    }

    public int GetInventoryCapacity()
    {
        return captureController != null ? captureController.GetInventoryCapacity() : 1;
    }

    public bool HasInventorySpaceForCapture()
    {
        return captureController != null && captureController.HasInventorySpaceForCapture();
    }

    public bool CanActorUseCaptureCommand(BattleUnit actor)
    {
        return captureController != null && captureController.CanActorUseCaptureCommand(actor);
    }

    public List<BattleUnit> GetValidCaptureTargets(BattleUnit actor)
    {
        return captureController != null ? captureController.GetValidCaptureTargets(actor) : new List<BattleUnit>();
    }

    public bool HasAnyCaptureTarget(BattleUnit actor)
    {
        return captureController != null && captureController.HasAnyCaptureTarget(actor);
    }

    public bool CanTargetBeCaptured(BattleUnit actor, BattleUnit target)
    {
        return captureController != null && captureController.CanTargetBeCaptured(actor, target);
    }

    public int GetRemainingCaptureAttempts(BattleUnit target)
    {
        return captureController != null ? captureController.GetRemainingCaptureAttempts(target) : 0;
    }

    public bool TryConsumeCaptureAttempt(BattleUnit target)
    {
        return captureController != null && captureController.TryConsumeCaptureAttempt(target);
    }

    public void RefundCaptureAttempt(BattleUnit target)
    {
        if (captureController != null)
            captureController.RefundCaptureAttempt(target);
    }

    public int GetCaptureChancePercent(BattleUnit target)
    {
        return captureController != null ? captureController.GetCaptureChancePercent(target) : 0;
    }

    public bool TryAddCapturedRewardToInventory(BattleUnit target, out ItemDefinition addedItem)
    {
        addedItem = null;
        return captureController != null && captureController.TryAddCapturedRewardToInventory(target, out addedItem);
    }

    public void SetInputMode(BattleInputMode mode)
    {
        InputMode = mode;
    }

    public void SetTurnState(TurnState state)
    {
        CurrentState = state;
    }

    public void StartManagedCoroutine(IEnumerator routine)
    {
        StartCoroutine(routine);
    }

    public void StopManagedCoroutines()
    {
        StopAllCoroutines();
    }

    public void ShowTargetMarkers(List<BattleUnit> targets)
    {
        if (viewManager != null)
            viewManager.SetTargetMarkers(targets);
    }

    public void ClearTargetMarkers()
    {
        if (viewManager != null)
            viewManager.ClearTargetMarkers();
    }

    public void OnActionSlotPressed(int slotIndex)
    {
        if (inputController != null)
            inputController.HandleActionSlotPressed(slotIndex);
    }

    public void OnMoveButtonPressed()
    {
        if (inputController != null)
            inputController.HandleMovePressed();
    }

    public void OnCaptureButtonPressed()
    {
        if (inputController != null)
            inputController.HandleCapturePressed();
    }

    public void OnFleeButtonPressed()
    {
        if (inputController != null)
            inputController.HandleFleePressed();
    }

    public void OnEndTurnButtonPressed()
    {
        if (inputController != null)
            inputController.HandleEndTurnPressed();
    }

    public void OnCancelButtonPressed()
    {
        if (inputController != null)
            inputController.CancelCurrentInput();

        ClearUISelection();
        RefreshAllUI();
    }

    public void OnInventoryTogglePressed()
    {
        if (presentationController != null)
            presentationController.OnInventoryTogglePressed();
    }

    public void OnMapButtonPressed()
    {
        if (presentationController != null)
            presentationController.OnMapButtonPressed();
    }

    public void OnInventorySlotPressed(int slotIndex)
    {
        if (inputController != null)
            inputController.HandleInventorySlotPressed(slotIndex);
    }

    public void OnPopupLogButtonPressed()
    {
        if (presentationController != null)
            presentationController.OnPopupLogButtonPressed();
    }

    public void OnEnemyDetailPopupButtonPressed()
    {
        if (presentationController != null)
            presentationController.OnEnemyDetailPopupButtonPressed();
    }

    public void OnPlayerSkillButtonHoverEnter(int slotIndex, Vector3 screenPosition)
    {
        if (presentationController != null)
            presentationController.OnPlayerSkillButtonHoverEnter(slotIndex, screenPosition);
    }

    public void OnPlayerSkillButtonHoverExit()
    {
        if (presentationController != null)
            presentationController.OnPlayerSkillButtonHoverExit();
    }

    public void OnFleeButtonHoverEnter(Vector3 screenPosition)
    {
        if (presentationController != null)
            presentationController.OnFleeButtonHoverEnter(screenPosition);
    }

    public void OnFleeButtonHoverExit()
    {
        if (presentationController != null)
            presentationController.OnFleeButtonHoverExit();
    }

    public void OnEnemySkillHoverEnter(int slotIndex, Vector3 screenPosition)
    {
        if (presentationController != null)
            presentationController.OnEnemySkillHoverEnter(slotIndex, screenPosition);
    }

    public void OnEnemySkillHoverExit()
    {
        if (presentationController != null)
            presentationController.OnEnemySkillHoverExit();
    }

    public void ResetRuntimeForBattleStart()
    {
        battleStarted = false;
        battleEndEventSent = false;
        waitingForPlayerAction = false;
        currentRound = 0;
        currentTurnSkippedByStatus = false;
        allyDeadUnitPresentThisTurn = false;
        enemyDeadUnitPresentThisTurn = false;

        CurrentState = TurnState.Waiting;
        BattleResult = BattleResultType.None;
        InputMode = BattleInputMode.None;
        CurrentActingUnit = null;
        LastShownAllyUnit = null;
        SelectedEnemyInfoUnit = null;

        ResetSelections();
    }

    public void ResetSelections()
    {
        SelectedSkill = null;
        SelectedSkillSlotIndex = -1;
        SelectedInventoryIndex = -1;
    }

    public void AssignFormations(BattleFormation ally, BattleFormation enemy)
    {
        allyFormation = ally;
        enemyFormation = enemy;
    }

    public void SetBattleStarted(bool started)
    {
        battleStarted = started;
    }

    public void SetBattleResult(BattleResultType result)
    {
        BattleResult = result;
    }

    public void SetBattleEndEventSent(bool sent)
    {
        battleEndEventSent = sent;
    }

    public void InvokeBattleEnded()
    {
        BattleEnded?.Invoke(BattleResult);
    }

    public void SetWaitingForPlayerAction(bool value)
    {
        waitingForPlayerAction = value;
    }

    public void SetCurrentActingUnit(BattleUnit unit)
    {
        CurrentActingUnit = unit;
    }

    public void SetLastShownAllyUnit(BattleUnit unit)
    {
        LastShownAllyUnit = unit;
    }

    public void SetCurrentTurnSkippedByStatus(bool value)
    {
        currentTurnSkippedByStatus = value;
    }

    public void ClearDeadUnitPresenceFlags()
    {
        allyDeadUnitPresentThisTurn = false;
        enemyDeadUnitPresentThisTurn = false;
    }

    public void MarkDeadUnitPresence(TeamType team, bool hasDeadUnit)
    {
        if (!hasDeadUnit)
            return;

        if (team == TeamType.Ally)
            allyDeadUnitPresentThisTurn = true;
        else
            enemyDeadUnitPresentThisTurn = true;
    }

    public void IncrementCurrentRound()
    {
        currentRound++;
    }

    public BattleUnit GetDefaultShownAllyUnit()
    {
        List<BattleUnit> allies = allyFormation != null ? allyFormation.GetAliveUnits() : null;
        return allies != null && allies.Count > 0 ? allies[0] : null;
    }

    public BattleUnit GetDefaultShownEnemyUnit()
    {
        List<BattleUnit> enemies = enemyFormation != null ? enemyFormation.GetAliveUnits() : null;
        return enemies != null && enemies.Count > 0 ? enemies[0] : null;
    }

    public void ClearUISelection()
    {
        if (presentationController != null)
            presentationController.ClearUISelection();
    }

    private T GetOrAddComponent<T>() where T : Component
    {
        T found = GetComponent<T>();
        if (found == null)
            found = gameObject.AddComponent<T>();
        return found;
    }

    private IEnumerator EmptyRoutine()
    {
        yield break;
    }
}
