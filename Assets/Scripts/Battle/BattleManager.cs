using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BattleManager : MonoBehaviour
{
    [Header("Prepared Battle Data")]
    [SerializeField] private PartyDefinition allyPartyDefinition;
    [SerializeField] private PartyDefinition enemyPartyDefinition;

    [Header("Controllers")]
    [SerializeField] private BattleViewManager viewManager;
    [SerializeField] private BattleUIController uiController;
    [SerializeField] private BattleLogController logController;
    [SerializeField] private BattleActionController actionController;
    [SerializeField] private BattleInputController inputController;
    [SerializeField] private EnemyAIController enemyAIController;
    [SerializeField] private BattlePassiveController passiveController;

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

    private BattleFormation allyFormation;
    private BattleFormation enemyFormation;
    private TurnManager turnManager;

    private bool waitingForPlayerAction;
    private bool battleStarted;
    private int currentRound;
    private bool currentTurnSkippedByStatus;

    private BottomContextType bottomContextType = BottomContextType.Inventory;

    public BattleFormation AllyFormation { get { return allyFormation; } }
    public BattleFormation EnemyFormation { get { return enemyFormation; } }
    public PartyDefinition AllyPartyDefinition { get { return allyPartyDefinition; } }
    public PartyDefinition EnemyPartyDefinition { get { return enemyPartyDefinition; } }
    public BattleActionController ActionController { get { return actionController; } }

    public TurnState CurrentState { get; private set; }
    public BattleResultType BattleResult { get; private set; }
    public BattleInputMode InputMode { get; private set; }

    public BattleUnit CurrentActingUnit { get; private set; }
    public BattleUnit LastShownAllyUnit { get; private set; }
    public BattleUnit SelectedEnemyInfoUnit { get; set; }

    public SkillDefinition SelectedSkill { get; set; }
    public int SelectedSkillSlotIndex { get; set; } = -1;
    public int SelectedInventoryIndex { get; set; } = -1;

    public float MoveAnimationDuration { get { return moveAnimationDuration; } }
    public float AttackMoveRatio { get { return attackMoveRatio; } }
    public float AttackMoveMaxDistance { get { return attackMoveMaxDistance; } }
    public float AttackMoveDuration { get { return attackMoveDuration; } }

    private void Start()
    {
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

        if (passiveController == null)
            passiveController = GetComponent<BattlePassiveController>();
        if (passiveController == null)
            passiveController = gameObject.AddComponent<BattlePassiveController>();
        if (passiveController != null)
            passiveController.Initialize(this, logController);

        StartBattle();
    }

    public void SetEnemyPartyDefinition(PartyDefinition definition)
    {
        enemyPartyDefinition = definition;
    }

    public void StartBattle()
    {
        allyFormation = new BattleFormation();
        enemyFormation = new BattleFormation();
        turnManager = new TurnManager();

        CurrentState = TurnState.Waiting;
        BattleResult = BattleResultType.None;
        InputMode = BattleInputMode.None;
        SelectedSkill = null;
        SelectedInventoryIndex = -1;
        SelectedSkillSlotIndex = -1;
        CurrentActingUnit = null;
        LastShownAllyUnit = null;
        SelectedEnemyInfoUnit = null;
        currentRound = 0;
        currentTurnSkippedByStatus = false;

        if (popupLogPanel != null)
            popupLogPanel.SetActive(false);

        logController.ClearBattleLog();
        SpawnPartyIntoFormation(allyPartyDefinition, TeamType.Ally, allyFormation);
        SpawnPartyIntoFormation(enemyPartyDefinition, TeamType.Enemy, enemyFormation);

        if (viewManager != null)
            viewManager.RefreshAllPositionsInstant(allyFormation, enemyFormation);

        LastShownAllyUnit = GetDefaultShownAllyUnit();
        SelectedEnemyInfoUnit = GetDefaultShownEnemyUnit();

        battleStarted = true;
        RefreshAllUI();
        ClearUISelection();
        StartCoroutine(BattleLoopRoutine());
    }

    private void SpawnPartyIntoFormation(PartyDefinition partyDefinition, TeamType team, BattleFormation formation)
    {
        if (partyDefinition == null || formation == null)
            return;

        if (!partyDefinition.IsValidMemberCount())
        {
            Debug.LogWarning("[BattleManager] Party member count must be 1~4.");
            return;
        }

        if (partyDefinition.HasDuplicateSlotIndex())
        {
            Debug.LogWarning("[BattleManager] Duplicate start slot index.");
            return;
        }

        if (partyDefinition.HasNullDefinitions())
        {
            Debug.LogWarning("[BattleManager] Null unit/view definition found.");
            return;
        }

        for (int i = 0; i < partyDefinition.members.Count; i++)
        {
            PartyMemberData data = partyDefinition.members[i];
            if (data == null) continue;

            BattleUnit unit = new BattleUnit(data, team);
            formation.SetUnit(data.startSlotIndex, unit);

            if (viewManager != null && inputController != null)
                viewManager.CreateView(unit, inputController);
        }
    }

    private IEnumerator BattleLoopRoutine()
    {
        while (battleStarted && BattleResult == BattleResultType.None)
        {
            currentRound++;
            logController.AppendBattleLog(logController.BuildTurnStartLog(currentRound));
            if (uiController != null)
                yield return StartCoroutine(uiController.ShowTurnStartTextRoutine(currentRound));

            List<BattleUnit> alive = new List<BattleUnit>();
            alive.AddRange(allyFormation.GetAliveUnits());
            alive.AddRange(enemyFormation.GetAliveUnits());
            turnManager.BuildTurnQueue(alive);

            while (turnManager.HasNextTurn() && BattleResult == BattleResultType.None)
            {
                BattleUnit unit = turnManager.GetNextUnit();
                if (unit == null || unit.IsDead || !IsUnitInBattle(unit))
                    continue;

                CurrentActingUnit = unit;
                CurrentActingUnit.OnOwnTurnStart();

                if (passiveController != null)
                    yield return StartCoroutine(passiveController.ResolveTurnStartPassive(CurrentActingUnit));

                if (CurrentActingUnit.Team == TeamType.Ally)
                    LastShownAllyUnit = CurrentActingUnit;

                if (viewManager != null)
                {
                    viewManager.ClearAllMarkers();
                    viewManager.SetTurnMarker(CurrentActingUnit);
                }

                currentTurnSkippedByStatus = false;
                yield return StartCoroutine(ResolveTurnStartStatusesRoutine(CurrentActingUnit));
                CheckBattleResult();
                RefreshAllUI();

                if (BattleResult != BattleResultType.None)
                    break;

                if (CurrentActingUnit == null || CurrentActingUnit.IsDead || !IsUnitInBattle(CurrentActingUnit) || currentTurnSkippedByStatus)
                {
                    if (passiveController != null)
                        passiveController.EvaluateAfterTurnEnd(unit);

                    CheckBattleResult();
                    RefreshAllUI();

                    if (BattleResult != BattleResultType.None)
                        break;

                    yield return new WaitForSeconds(turnDelay);
                    continue;
                }

                SetInputMode(BattleInputMode.WaitingForAction);
                SelectedSkill = null;
                SelectedInventoryIndex = -1;
                SelectedSkillSlotIndex = -1;

                if (CurrentActingUnit.Team == TeamType.Ally)
                {
                    CurrentState = TurnState.PlayerInput;
                    waitingForPlayerAction = true;
                }
                else
                {
                    CurrentState = TurnState.EnemyThinking;
                }

                RefreshAllUI();

                if (CurrentActingUnit.Team == TeamType.Ally)
                {
                    while (waitingForPlayerAction && BattleResult == BattleResultType.None)
                        yield return null;
                }
                else
                {
                    yield return StartCoroutine(enemyAIController.ExecuteTurn(CurrentActingUnit));
                }

                CheckBattleResult();
                RefreshAllUI();

                if (BattleResult != BattleResultType.None)
                    break;

                if (passiveController != null)
                    passiveController.EvaluateAfterTurnEnd(unit);

                CheckBattleResult();
                RefreshAllUI();

                if (BattleResult != BattleResultType.None)
                    break;

                yield return new WaitForSeconds(turnDelay);
            }
        }

        CurrentState = TurnState.BattleEnded;
        if (BattleResult == BattleResultType.Victory)
            logController.AppendBattleLog(logController.BuildVictoryLog());
        else if (BattleResult == BattleResultType.Defeat)
            logController.AppendBattleLog(logController.BuildDefeatLog());

        RefreshAllUI();
        ClearUISelection();
    }

    private IEnumerator ResolveTurnStartStatusesRoutine(BattleUnit unit)
    {
        if (unit == null || unit.IsDead || !IsUnitInBattle(unit))
            yield break;

        BattleTurnStartStatusResult result = unit.ResolveTurnStartStatuses();

        if (result.poisonDamage > 0)
            logController.AppendBattleLog(logController.BuildTurnStartPoisonLog(unit, result.poisonDamage));

        if (result.bleedDamage > 0)
            logController.AppendBattleLog(logController.BuildTurnStartBleedLog(unit, result.bleedDamage));

        if (unit.IsDead)
        {
            logController.AppendBattleLog(logController.BuildDeathLog(unit));
            yield return StartCoroutine(HandleDeathsAndCompressionRoutine());
            yield break;
        }

        if (result.wasStunned)
        {
            logController.AppendBattleLog(logController.BuildTurnStartStunLog(unit));
            waitingForPlayerAction = false;
            CurrentState = TurnState.TurnEnding;
            currentTurnSkippedByStatus = true;
        }

        for (int i = 0; i < result.expiredStatuses.Count; i++)
            logController.AppendBattleLog(logController.BuildStatusExpiredLog(unit, result.expiredStatuses[i]));
    }

    public void RefreshAllUI()
    {
        BattleUnit shownAlly = IsUnitInBattle(LastShownAllyUnit) ? LastShownAllyUnit : GetDefaultShownAllyUnit();
        BattleUnit shownEnemy = IsUnitInBattle(SelectedEnemyInfoUnit) ? SelectedEnemyInfoUnit : GetDefaultShownEnemyUnit();
        BattleUnit actionOwner = CurrentActingUnit != null && CurrentActingUnit.Team == TeamType.Ally && IsUnitInBattle(CurrentActingUnit)
            ? CurrentActingUnit
            : shownAlly;
        bool canPlayerAct = CurrentState == TurnState.PlayerInput &&
                            CurrentActingUnit != null &&
                            CurrentActingUnit.Team == TeamType.Ally &&
                            IsUnitInBattle(CurrentActingUnit);

        LastShownAllyUnit = shownAlly;
        SelectedEnemyInfoUnit = shownEnemy;

        if (uiController != null)
        {
            uiController.RefreshCurrentUnitPanel(shownAlly);
            uiController.RefreshEnemyPanels(shownEnemy);
            uiController.RefreshActionButtons(actionOwner, canPlayerAct);
            uiController.RefreshInventory(this, allyPartyDefinition, SelectedInventoryIndex);
            uiController.SetBottomContext(bottomContextType);
        }
    }

    public IEnumerator HandleDeathsAndCompressionRoutine()
    {
        List<BattleUnit> movedAllies = allyFormation.RemoveDeadAndCompress();
        List<BattleUnit> movedEnemies = enemyFormation.RemoveDeadAndCompress();

        for (int i = 0; i < movedAllies.Count; i++)
            logController.AppendBattleLog(logController.BuildAutoMoveLog(movedAllies[i]));

        for (int i = 0; i < movedEnemies.Count; i++)
            logController.AppendBattleLog(logController.BuildAutoMoveLog(movedEnemies[i]));

        if (viewManager != null)
        {
            RemoveDeadViews();
            yield return StartCoroutine(viewManager.AnimateRefreshAllPositions(allyFormation, enemyFormation, moveAnimationDuration));
        }

        if (!IsUnitInBattle(SelectedEnemyInfoUnit))
            SelectedEnemyInfoUnit = GetDefaultShownEnemyUnit();

        if (!IsUnitInBattle(LastShownAllyUnit))
            LastShownAllyUnit = GetDefaultShownAllyUnit();
    }

    private void RemoveDeadViews()
    {
        List<BattleUnit> allyUnits = allyFormation.GetAllUnits();
        List<BattleUnit> enemyUnits = enemyFormation.GetAllUnits();

        HashSet<BattleUnit> aliveSet = new HashSet<BattleUnit>();
        for (int i = 0; i < allyUnits.Count; i++) aliveSet.Add(allyUnits[i]);
        for (int i = 0; i < enemyUnits.Count; i++) aliveSet.Add(enemyUnits[i]);

        List<BattleUnit> removeTargets = new List<BattleUnit>();
        foreach (BattleUnitView view in viewManager.GetAllViews())
        {
            if (view == null || view.Unit == null) continue;
            if (!aliveSet.Contains(view.Unit) || view.Unit.IsDead)
                removeTargets.Add(view.Unit);
        }

        for (int i = 0; i < removeTargets.Count; i++)
            viewManager.RemoveView(removeTargets[i]);
    }

    public void OnActionExecutionFinished(bool consumeTurn)
    {
        SelectedSkill = null;
        SelectedInventoryIndex = -1;
        SelectedSkillSlotIndex = -1;
        SetInputMode(BattleInputMode.WaitingForAction);
        ClearTargetMarkers();

        if (uiController != null)
        {
            uiController.HideTargetPreview();
            uiController.HideSkillTooltip();
            uiController.HideFleeTooltip();
        }

        ClearUISelection();

        CurrentState = TurnState.PlayerInput;
        RefreshAllUI();

        if (CurrentActingUnit == null || CurrentActingUnit.Team == TeamType.Enemy || consumeTurn)
        {
            waitingForPlayerAction = false;
            CurrentState = TurnState.TurnEnding;
        }
    }

    public void NotifyUnitLeftBattle(BattleUnit unit)
    {
        if (unit == null)
            return;

        ClearTargetMarkers();

        if (LastShownAllyUnit == unit)
            LastShownAllyUnit = GetDefaultShownAllyUnit();

        if (SelectedEnemyInfoUnit == unit)
            SelectedEnemyInfoUnit = GetDefaultShownEnemyUnit();

        if (uiController != null)
        {
            uiController.HideTargetPreview();
            uiController.HideSkillTooltip();
            uiController.HideEnemySkillTooltip();
            uiController.HideFleeTooltip();
        }
    }

    public bool IsUnitInBattle(BattleUnit unit)
    {
        if (unit == null)
            return false;

        return (allyFormation != null && allyFormation.Contains(unit)) ||
               (enemyFormation != null && enemyFormation.Contains(unit));
    }

    private void CheckBattleResult()
    {
        bool alliesAlive = allyFormation.HasLivingUnits();
        bool enemiesAlive = enemyFormation.HasLivingUnits();

        if (alliesAlive && enemiesAlive)
            BattleResult = BattleResultType.None;
        else if (alliesAlive)
            BattleResult = BattleResultType.Victory;
        else
            BattleResult = BattleResultType.Defeat;
    }

    private BattleUnit GetDefaultShownAllyUnit()
    {
        List<BattleUnit> allies = allyFormation != null ? allyFormation.GetAliveUnits() : null;
        return allies != null && allies.Count > 0 ? allies[0] : null;
    }

    private BattleUnit GetDefaultShownEnemyUnit()
    {
        List<BattleUnit> enemies = enemyFormation != null ? enemyFormation.GetAliveUnits() : null;
        return enemies != null && enemies.Count > 0 ? enemies[0] : null;
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
        inputController.HandleActionSlotPressed(slotIndex);
    }

    public void OnMoveButtonPressed()
    {
        inputController.HandleMovePressed();
    }

    public void OnFleeButtonPressed()
    {
        inputController.HandleFleePressed();
    }

    public void OnEndTurnButtonPressed()
    {
        inputController.HandleEndTurnPressed();
    }

    public void OnCancelButtonPressed()
    {
        inputController.CancelCurrentInput();
        ClearUISelection();
        RefreshAllUI();
    }

    public void OnInventoryTogglePressed()
    {
        if (bottomContextType == BottomContextType.Inventory)
            bottomContextType = BottomContextType.EnemyInfo;
        else
            bottomContextType = BottomContextType.Inventory;

        if (bottomContextType != BottomContextType.EnemyInfo && uiController != null)
            uiController.HideEnemyDetailPopup();

        ClearUISelection();
        RefreshAllUI();
    }

    public void OnInventorySlotPressed(int slotIndex)
    {
        inputController.HandleInventorySlotPressed(slotIndex);
    }

    public void OnPopupLogButtonPressed()
    {
        if (popupLogPanel != null)
            popupLogPanel.SetActive(!popupLogPanel.activeSelf);

        ClearUISelection();
    }

    public void OnEnemyDetailPopupButtonPressed()
    {
        if (uiController != null)
            uiController.ToggleEnemyDetailPopup(SelectedEnemyInfoUnit);

        ClearUISelection();
    }

    public void OnPlayerSkillButtonHoverEnter(int slotIndex, Vector3 screenPosition)
    {
        BattleUnit unit = CurrentActingUnit != null && CurrentActingUnit.Team == TeamType.Ally
            ? CurrentActingUnit
            : LastShownAllyUnit;

        SkillDefinition skill = unit != null ? unit.GetActionSkillAt(slotIndex) : null;
        if (skill != null && uiController != null)
            uiController.ShowPlayerSkillTooltip(skill, screenPosition);
    }

    public void OnPlayerSkillButtonHoverExit()
    {
        if (uiController != null)
            uiController.HideSkillTooltip();
    }

    public void OnFleeButtonHoverEnter(Vector3 screenPosition)
    {
        if (uiController == null || CurrentState != TurnState.PlayerInput || CurrentActingUnit == null || CurrentActingUnit.Team != TeamType.Ally)
            return;

        int fleeChancePercent = BattleCalculator.CalculateFleeChancePercent(CurrentActingUnit, enemyFormation);
        uiController.ShowFleeTooltip(fleeChancePercent, screenPosition);
    }

    public void OnFleeButtonHoverExit()
    {
        if (uiController != null)
            uiController.HideFleeTooltip();
    }

    public void OnEnemySkillHoverEnter(int slotIndex, Vector3 screenPosition)
    {
        if (SelectedEnemyInfoUnit == null || uiController == null)
            return;

        SkillDefinition skill = SelectedEnemyInfoUnit.GetActionSkillAt(slotIndex);
        if (skill != null)
            uiController.ShowEnemySkillTooltip(skill, screenPosition);
    }

    public void OnEnemySkillHoverExit()
    {
        if (uiController != null)
            uiController.HideEnemySkillTooltip();
    }

    private void ClearUISelection()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }
}
