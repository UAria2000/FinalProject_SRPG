using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleManager : MonoBehaviour
{
    [Header("Party Data")]
    [SerializeField] private PartyDefinition allyPartyDefinition;
    [SerializeField] private PartyDefinition enemyPartyDefinition;

    [Header("Controllers")]
    [SerializeField] private BattleViewManager viewManager;
    [SerializeField] private BattleUIController uiController;
    [SerializeField] private BattleLogController logController;
    [SerializeField] private BattleActionController actionController;
    [SerializeField] private BattleInputController inputController;
    [SerializeField] private EnemyAIController enemyAIController;

    [Header("Enemy UI Support")]
    [SerializeField] private Button[] enemySkillButtons = new Button[2];
    [SerializeField] private GameObject popupLogPanel;

    [Header("Settings")]
    [SerializeField] private float turnDelay = 0.4f;
    [SerializeField] private float moveAnimationDuration = 0.4f;
    [SerializeField] private float attackMoveRatio = 0.45f;
    [SerializeField] private float attackMoveMaxDistance = 260f;
    [SerializeField] private float attackMoveDuration = 0.6f;
    [SerializeField] private int potionHealAmount = 8;

    private BattleFormation allyFormation;
    private BattleFormation enemyFormation;
    private TurnManager turnManager;

    private TurnState currentState = TurnState.Waiting;
    private BattleResultType battleResult = BattleResultType.None;
    private BattleInputMode inputMode = BattleInputMode.None;

    private int currentRound = 0;

    private BattleUnit currentActingUnit;
    private BattleUnit lastShownAllyUnit;
    private BattleUnit selectedEnemyInfoUnit;
    private string selectedEnemyEpitaph = "";

    private bool playerActionSubmitted = false;
    private BattleUnit selectedAttackTarget;
    private BattleUnit selectedMoveTarget;
    private BattleUnit selectedItemTarget;
    private SkillDefinition selectedSkill;
    private BattleUnit selectedSkillTarget;

    public BattleFormation AllyFormation => allyFormation;
    public BattleFormation EnemyFormation => enemyFormation;
    public BattleViewManager ViewManager => viewManager;

    public TurnState CurrentState => currentState;
    public BattleInputMode InputMode => inputMode;
    public BattleResultType BattleResult => battleResult;

    public BattleUnit CurrentActingUnit => currentActingUnit;
    public BattleUnit LastShownAllyUnit => lastShownAllyUnit;
    public BattleUnit SelectedEnemyInfoUnit => selectedEnemyInfoUnit;
    public string SelectedEnemyEpitaph => selectedEnemyEpitaph;

    public BattleUnit SelectedAttackTarget => selectedAttackTarget;
    public BattleUnit SelectedMoveTarget => selectedMoveTarget;
    public BattleUnit SelectedItemTarget => selectedItemTarget;
    public SkillDefinition SelectedSkill => selectedSkill;
    public BattleUnit SelectedSkillTarget => selectedSkillTarget;

    public float MoveAnimationDuration => moveAnimationDuration;
    public float AttackMoveRatio => attackMoveRatio;
    public float AttackMoveMaxDistance => attackMoveMaxDistance;
    public float AttackMoveDuration => attackMoveDuration;
    public int PotionHealAmount => potionHealAmount;

    private void Start()
    {
        if (uiController != null)
        {
            uiController.Initialize(this);
            uiController.BindButtonEvents();
            uiController.BindEnemySkillHoverEvents(enemySkillButtons);
        }

        if (actionController != null)
            actionController.Initialize(this, viewManager, logController);

        if (enemyAIController != null)
            enemyAIController.Initialize(this);

        if (inputController != null)
            inputController.Initialize(this, uiController, actionController, logController);

        StartBattle();
    }

    public void StartBattle()
    {
        allyFormation = new BattleFormation();
        enemyFormation = new BattleFormation();
        turnManager = new TurnManager();

        currentRound = 0;
        currentState = TurnState.Waiting;
        battleResult = BattleResultType.None;
        inputMode = BattleInputMode.None;

        currentActingUnit = null;
        lastShownAllyUnit = null;
        selectedEnemyInfoUnit = null;
        selectedEnemyEpitaph = "";

        playerActionSubmitted = false;
        selectedAttackTarget = null;
        selectedMoveTarget = null;
        selectedItemTarget = null;
        selectedSkill = null;
        selectedSkillTarget = null;

        if (popupLogPanel != null)
            popupLogPanel.SetActive(false);

        logController?.ClearBattleLog();

        uiController?.SetActionButtonsInteractable(false);
        uiController?.RefreshCancelButtonState(false);
        uiController?.HideSkillTooltip();
        uiController?.HideEnemySkillTooltip();
        uiController?.HideEnemyDetailPopup();

        SpawnPartyIntoFormation(allyPartyDefinition, TeamType.Ally, allyFormation);
        SpawnPartyIntoFormation(enemyPartyDefinition, TeamType.Enemy, enemyFormation);

        viewManager?.RefreshAllPositionsInstant(allyFormation, enemyFormation);

        ClearAllMarkersAndHighlights();

        lastShownAllyUnit = GetDefaultDisplayedAllyUnit();
        selectedEnemyInfoUnit = GetDefaultDisplayedEnemyUnit();
        selectedEnemyEpitaph = GetEnemyEpitaph(selectedEnemyInfoUnit);

        RefreshAllUI();

        logController?.AppendBattleLog(logController.BuildBattleStartLog());

        StartCoroutine(BattleLoop());
    }

    private void SpawnPartyIntoFormation(PartyDefinition partyDefinition, TeamType team, BattleFormation formation)
    {
        if (partyDefinition == null || formation == null)
            return;

        if (!partyDefinition.IsValidMemberCount())
        {
            Debug.LogWarning($"[BattleManager] {team} party member count must be between 1 and 4.");
            return;
        }

        if (partyDefinition.HasDuplicateSlotIndex())
        {
            Debug.LogWarning($"[BattleManager] {team} party has duplicate startSlotIndex values.");
            return;
        }

        if (partyDefinition.HasNullDefinitions())
        {
            Debug.LogWarning($"[BattleManager] {team} party has null UnitDefinition or UnitViewDefinition.");
            return;
        }

        for (int i = 0; i < partyDefinition.members.Count; i++)
        {
            PartyMemberData member = partyDefinition.members[i];
            if (member == null)
                continue;

            BattleUnit unit = new BattleUnit(member, team);

            formation.SetUnit(member.startSlotIndex, unit);
            viewManager?.CreateView(unit, this);
        }
    }

    private IEnumerator BattleLoop()
    {
        while (battleResult == BattleResultType.None)
        {
            currentRound++;

            logController?.AppendBattleLog(logController.FormatTurnLog(currentRound));

            if (uiController != null)
                yield return StartCoroutine(uiController.ShowTurnStartTextRoutine(currentRound));

            List<BattleUnit> allAlive = new List<BattleUnit>();
            allAlive.AddRange(allyFormation.GetAliveUnits());
            allAlive.AddRange(enemyFormation.GetAliveUnits());

            turnManager.BuildTurnQueue(allAlive);

            while (turnManager.HasNextTurn() && battleResult == BattleResultType.None)
            {
                BattleUnit unit = turnManager.GetNextUnit();
                if (unit == null || unit.IsDead)
                    continue;

                yield return StartCoroutine(ExecuteTurn(unit));

                CheckBattleResult();
                RefreshAllUI();

                if (battleResult != BattleResultType.None)
                    break;

                yield return new WaitForSeconds(turnDelay);
            }
        }

        currentState = TurnState.BattleEnded;

        uiController?.SetActionButtonsInteractable(false);
        uiController?.RefreshCancelButtonState(false);
        uiController?.HideSkillTooltip();
        uiController?.HideEnemySkillTooltip();

        if (battleResult == BattleResultType.Victory)
            logController?.AppendBattleLog(logController.BuildVictoryLog());
        else if (battleResult == BattleResultType.Defeat)
            logController?.AppendBattleLog(logController.BuildDefeatLog());
    }

    private IEnumerator ExecuteTurn(BattleUnit unit)
    {
        currentActingUnit = unit;

        if (unit.Team == TeamType.Ally)
            lastShownAllyUnit = unit;

        RefreshAllUI();
        ClearAllMarkersAndHighlights();

        if (unit.Team == TeamType.Ally)
            ShowCurrentTurnMarker(unit, true);

        if (unit.Team == TeamType.Ally)
            yield return StartCoroutine(ExecutePlayerTurn(unit));
        else
            yield return StartCoroutine(ExecuteEnemyTurn(unit));

        if (unit.Team == TeamType.Ally)
            ShowCurrentTurnMarker(unit, false);

        unit.OnTurnStart();

        allyFormation.RemoveDeadAndCompress();
        enemyFormation.RemoveDeadAndCompress();

        if (viewManager != null)
        {
            yield return StartCoroutine(
                viewManager.AnimateRefreshAllPositions(
                    allyFormation,
                    enemyFormation,
                    moveAnimationDuration
                )
            );
        }

        RefreshEnemySelectionAfterFormationChange();

        currentActingUnit = null;
        currentState = TurnState.TurnEnding;
    }

    private IEnumerator ExecutePlayerTurn(BattleUnit unit)
    {
        currentState = TurnState.PlayerInput;
        inputMode = BattleInputMode.WaitingForAction;

        playerActionSubmitted = false;
        selectedAttackTarget = null;
        selectedMoveTarget = null;
        selectedItemTarget = null;
        selectedSkill = null;
        selectedSkillTarget = null;

        RefreshPlayerActionUI();

        while (!playerActionSubmitted)
            yield return null;

        uiController?.SetActionButtonsInteractable(false);
        uiController?.RefreshCancelButtonState(false);
        uiController?.HideSkillTooltip();

        inputMode = BattleInputMode.None;
    }

    private IEnumerator ExecuteEnemyTurn(BattleUnit unit)
    {
        currentState = TurnState.EnemyThinking;

        if (enemyAIController == null || actionController == null)
            yield break;

        EnemyAIController.EnemyActionChoice choice = enemyAIController.ChooseBestEnemyAction(unit);

        switch (choice.actionType)
        {
            case EnemyAIController.EnemyActionType.Skill:
                yield return StartCoroutine(actionController.ExecuteSkill(unit, choice.target, choice.skill));
                break;

            case EnemyAIController.EnemyActionType.BasicAttack:
                yield return StartCoroutine(actionController.ExecuteBasicAttack(unit, choice.target));
                break;

            case EnemyAIController.EnemyActionType.Move:
                bool moved = enemyAIController.TryAutoMove(unit, enemyFormation);
                if (moved)
                {
                    if (logController != null)
                        logController.AppendBattleLog(logController.BuildAutoMoveLog(unit));

                    if (viewManager != null)
                    {
                        yield return StartCoroutine(
                            viewManager.AnimateRefreshAllPositions(
                                allyFormation,
                                enemyFormation,
                                moveAnimationDuration
                            )
                        );
                    }
                }
                break;
        }
    }

    public void RefreshPlayerActionUI()
    {
        if (uiController == null)
            return;

        uiController.SetActionButtonsInteractable(true);

        uiController.SetAttackButtonInteractable(
            BattleTargeting.GetBasicAttackTargets(currentActingUnit, enemyFormation).Count > 0
        );

        uiController.SetMoveButtonInteractable(
            GetMoveableTargets(currentActingUnit, allyFormation).Count > 0
        );

        uiController.SetItemButtonInteractable(
            GetItemTargets(currentActingUnit, allyFormation).Count > 0
        );

        bool allowUse =
            currentState == TurnState.PlayerInput &&
            currentActingUnit != null &&
            !currentActingUnit.IsDead &&
            currentActingUnit.Team == TeamType.Ally;

        uiController.RefreshPlayerSkillButtons(GetSkillButtonDisplayUnit(), currentActingUnit, allowUse);
        uiController.RefreshCancelButtonState(CanCancelCurrentSelection());
    }

    public void RefreshAllUI()
    {
        if (uiController == null)
            return;

        if (lastShownAllyUnit != null)
            uiController.ShowCurrentUnitInfo(lastShownAllyUnit);
        else
            uiController.HideCurrentUnitInfo();

        bool allowUse =
            currentState == TurnState.PlayerInput &&
            currentActingUnit != null &&
            !currentActingUnit.IsDead &&
            currentActingUnit.Team == TeamType.Ally;

        uiController.RefreshPlayerSkillButtons(GetSkillButtonDisplayUnit(), currentActingUnit, allowUse);
        uiController.RefreshEnemyInfo(selectedEnemyInfoUnit, selectedEnemyEpitaph);
        uiController.RefreshCancelButtonState(CanCancelCurrentSelection());
    }

    private BattleUnit GetSkillButtonDisplayUnit()
    {
        if (currentActingUnit != null && currentActingUnit.Team == TeamType.Ally)
            return currentActingUnit;

        return lastShownAllyUnit;
    }

    private void CheckBattleResult()
    {
        bool allyAlive = allyFormation.HasAliveUnits();
        bool enemyAlive = enemyFormation.HasAliveUnits();

        if (!enemyAlive)
            battleResult = BattleResultType.Victory;
        else if (!allyAlive)
            battleResult = BattleResultType.Defeat;
    }

    private BattleUnit GetDefaultDisplayedAllyUnit()
    {
        if (allyFormation == null)
            return null;

        BattleUnit slot0Unit = allyFormation.GetUnit(0);
        if (slot0Unit != null && !slot0Unit.IsDead)
            return slot0Unit;

        for (int i = 1; i < 4; i++)
        {
            BattleUnit unit = allyFormation.GetUnit(i);
            if (unit != null && !unit.IsDead)
                return unit;
        }

        return null;
    }

    private BattleUnit GetDefaultDisplayedEnemyUnit()
    {
        if (enemyFormation == null)
            return null;

        for (int i = 0; i < 4; i++)
        {
            BattleUnit unit = enemyFormation.GetUnit(i);
            if (unit != null && !unit.IsDead)
                return unit;
        }

        return null;
    }

    private void RefreshEnemySelectionAfterFormationChange()
    {
        if (selectedEnemyInfoUnit == null || selectedEnemyInfoUnit.IsDead)
        {
            selectedEnemyInfoUnit = GetDefaultDisplayedEnemyUnit();
            selectedEnemyEpitaph = GetEnemyEpitaph(selectedEnemyInfoUnit);
        }
    }

    private string GetEnemyEpitaph(BattleUnit enemy)
    {
        if (enemy == null)
            return "";

        if (!string.IsNullOrWhiteSpace(enemy.Epitaph))
            return enemy.Epitaph;

        switch (enemy.Name)
        {
            case "궁수":
                return "끝내 겨눈 화살 하나, 내게 돌아왔군.";
            case "흑기사":
                return "검은 철갑 속에도 마지막 숨은 뜨겁게 남는다.";
            case "다크엘프":
                return "숲의 그림자는 사라져도 증오는 남는다.";
        }

        return "남겨진 말은 바람 속에 흩어진다.";
    }

    public void NotifyUnitDeath(BattleUnit target)
    {
        if (target == null)
            return;

        if (lastShownAllyUnit == target)
            lastShownAllyUnit = GetDefaultDisplayedAllyUnit();

        if (target.Team == TeamType.Enemy && selectedEnemyInfoUnit == target)
        {
            selectedEnemyInfoUnit = GetDefaultDisplayedEnemyUnit();
            selectedEnemyEpitaph = GetEnemyEpitaph(selectedEnemyInfoUnit);
        }

        RefreshAllUI();
    }


    public void NotifyUnitChanged(BattleUnit unit)
    {
        if (unit == null)
            return;

        if (unit.Team == TeamType.Ally && lastShownAllyUnit == unit)
            lastShownAllyUnit = unit;

        if (unit.Team == TeamType.Enemy && selectedEnemyInfoUnit == unit)
            selectedEnemyInfoUnit = unit;

        RefreshAllUI();
    }

    public void ShowCurrentTurnMarker(BattleUnit unit, bool visible)
    {
        if (viewManager == null || unit == null)
            return;

        BattleUnitView view = viewManager.GetView(unit);
        if (view != null)
            view.SetCurrentTurnMarker(visible);
    }

    public void ClearAllTargetMarkersAndHighlights()
    {
        if (viewManager == null)
            return;

        foreach (BattleUnit unit in allyFormation.GetAliveUnits())
        {
            BattleUnitView view = viewManager.GetView(unit);
            if (view != null)
            {
                view.SetHighlighted(false);
                view.SetTargetMarker(false);
            }
        }

        foreach (BattleUnit unit in enemyFormation.GetAliveUnits())
        {
            BattleUnitView view = viewManager.GetView(unit);
            if (view != null)
            {
                view.SetHighlighted(false);
                view.SetTargetMarker(false);
            }
        }
    }

    private void ClearAllMarkersAndHighlights()
    {
        if (viewManager == null)
            return;

        foreach (BattleUnit unit in allyFormation.GetAliveUnits())
        {
            BattleUnitView view = viewManager.GetView(unit);
            if (view != null)
            {
                view.SetHighlighted(false);
                view.SetTargetMarker(false);
                view.SetCurrentTurnMarker(false);
            }
        }

        foreach (BattleUnit unit in enemyFormation.GetAliveUnits())
        {
            BattleUnitView view = viewManager.GetView(unit);
            if (view != null)
            {
                view.SetHighlighted(false);
                view.SetTargetMarker(false);
                view.SetCurrentTurnMarker(false);
            }
        }
    }

    public bool CanCancelCurrentSelection()
    {
        return inputMode == BattleInputMode.WaitingForAttackTarget
            || inputMode == BattleInputMode.WaitingForMoveTarget
            || inputMode == BattleInputMode.WaitingForSkillTarget
            || inputMode == BattleInputMode.WaitingForItemTarget;
    }

    public List<BattleUnit> GetMoveableTargets(BattleUnit mover, BattleFormation formation)
    {
        List<BattleUnit> result = new List<BattleUnit>();

        if (mover == null || mover.IsDead || formation == null)
            return result;

        int slot = mover.SlotIndex;
        int forwardIndex = slot - 1;
        int backwardIndex = slot + 1;

        if (forwardIndex >= 0)
        {
            BattleUnit forwardUnit = formation.GetUnit(forwardIndex);
            if (forwardUnit != null && !forwardUnit.IsDead && forwardUnit.Team == mover.Team)
                result.Add(forwardUnit);
        }

        if (backwardIndex < 4)
        {
            BattleUnit backwardUnit = formation.GetUnit(backwardIndex);
            if (backwardUnit != null && !backwardUnit.IsDead && backwardUnit.Team == mover.Team)
                result.Add(backwardUnit);
        }

        return result;
    }

    public List<BattleUnit> GetItemTargets(BattleUnit user, BattleFormation formation)
    {
        List<BattleUnit> result = new List<BattleUnit>();

        if (user == null || formation == null)
            return result;

        foreach (BattleUnit unit in formation.GetAliveUnits())
            result.Add(unit);

        return result;
    }

    public List<BattleUnit> GetPrimarySkillTargets(BattleUnit user, SkillDefinition skill)
    {
        List<BattleUnit> result = new List<BattleUnit>();

        if (user == null || skill == null)
            return result;

        if (!user.CanUseSkill(skill))
            return result;

        BattleFormation targetFormation = GetSkillTargetFormation(user, skill);
        if (targetFormation == null)
            return result;

        foreach (BattleUnit unit in targetFormation.GetAliveUnits())
        {
            if (unit == null || unit.IsDead)
                continue;

            if (!skill.CanTargetSlot(unit.SlotIndex))
                continue;

            result.Add(unit);
        }

        return result;
    }

    private BattleFormation GetSkillTargetFormation(BattleUnit user, SkillDefinition skill)
    {
        if (user == null || skill == null)
            return null;

        switch (skill.targetTeam)
        {
            case SkillTargetTeam.Enemy:
                return user.Team == TeamType.Ally ? enemyFormation : allyFormation;
            case SkillTargetTeam.Ally:
                return user.Team == TeamType.Ally ? allyFormation : enemyFormation;
            case SkillTargetTeam.Self:
                return user.Team == TeamType.Ally ? allyFormation : enemyFormation;
        }

        return null;
    }

    public BattleUnit GetBackTarget(BattleUnit primaryTarget)
    {
        if (primaryTarget == null)
            return null;

        BattleFormation formation = primaryTarget.Team == TeamType.Ally ? allyFormation : enemyFormation;
        if (formation == null)
            return null;

        int backSlot = primaryTarget.SlotIndex + 1;
        if (backSlot < 0 || backSlot >= 4)
            return null;

        BattleUnit backTarget = formation.GetUnit(backSlot);
        if (backTarget == null || backTarget.IsDead)
            return null;

        return backTarget;
    }

    public void SetInputMode(BattleInputMode mode) => inputMode = mode;

    public void SetSelectedAttackTarget(BattleUnit unit) => selectedAttackTarget = unit;
    public void SetSelectedMoveTarget(BattleUnit unit) => selectedMoveTarget = unit;
    public void SetSelectedItemTarget(BattleUnit unit) => selectedItemTarget = unit;
    public void SetSelectedSkill(SkillDefinition skill) => selectedSkill = skill;
    public void SetSelectedSkillTarget(BattleUnit unit) => selectedSkillTarget = unit;

    public void ClearSelectedSkill()
    {
        selectedSkill = null;
        selectedSkillTarget = null;
    }

    public void ClearSelectedTargetsAndSkill()
    {
        selectedAttackTarget = null;
        selectedMoveTarget = null;
        selectedItemTarget = null;
        selectedSkill = null;
        selectedSkillTarget = null;
    }

    public void SetSelectedEnemyInfoUnit(BattleUnit unit)
    {
        selectedEnemyInfoUnit = unit;
        selectedEnemyEpitaph = GetEnemyEpitaph(unit);
        RefreshAllUI();
    }

    public void MarkPlayerActionSubmitted()
    {
        playerActionSubmitted = true;
    }

    public Coroutine StartManagedCoroutine(IEnumerator routine)
    {
        return StartCoroutine(routine);
    }

    // UI/Button Event Wrappers
    public void OnAttackButtonClicked() => inputController?.OnAttackButtonClicked();
    public void OnMoveButtonClicked() => inputController?.OnMoveButtonClicked();
    public void OnSkillButtonClicked(int skillIndex) => inputController?.OnSkillButtonClicked(skillIndex);
    public void OnItemButtonClicked() => inputController?.OnItemButtonClicked();
    public void OnCancelButtonClicked() => inputController?.OnCancelButtonClicked();

    public void OnPopupLogButtonClicked()
    {
        if (popupLogPanel != null)
            popupLogPanel.SetActive(!popupLogPanel.activeSelf);

        uiController?.HideSkillTooltip();
        uiController?.HideEnemySkillTooltip();
    }

    public void OnEnemyDetailPopupButtonClicked()
    {
        uiController?.ToggleEnemyDetailPopup(selectedEnemyInfoUnit, selectedEnemyEpitaph);
    }

    public void OnUnitViewClicked(BattleUnitView clickedView) => inputController?.OnUnitViewClicked(clickedView);

    public void ShowSkillTooltip(int skillSlotIndex, Vector2 screenPosition)
    {
        uiController?.ShowSkillTooltip(skillSlotIndex, GetSkillButtonDisplayUnit(), screenPosition);
    }

    public void MoveSkillTooltip(Vector2 screenPosition)
    {
        uiController?.MoveSkillTooltip(screenPosition);
    }

    public void HideSkillTooltip()
    {
        uiController?.HideSkillTooltip();
    }

    public void ShowEnemySkillTooltip(int skillSlotIndex, Vector2 screenPosition)
    {
        uiController?.ShowEnemySkillTooltip(skillSlotIndex, selectedEnemyInfoUnit, screenPosition);
    }

    public void MoveEnemySkillTooltip(Vector2 screenPosition)
    {
        uiController?.MoveEnemySkillTooltip(screenPosition);
    }

    public void HideEnemySkillTooltip()
    {
        uiController?.HideEnemySkillTooltip();
    }
}