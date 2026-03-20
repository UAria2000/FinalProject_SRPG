using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleManager : MonoBehaviour
{
    [Header("Party Data")]
    [SerializeField] private PartyDefinition allyPartyDefinition;
    [SerializeField] private PartyDefinition enemyPartyDefinition;

    [Header("View")]
    [SerializeField] private BattleViewManager viewManager;

    [Header("Current Unit Info UI")]
    [SerializeField] private CurrentUnitInfoPanel currentUnitInfoPanel;

    [Header("Buttons")]
    [SerializeField] private Button attackButton;
    [SerializeField] private Button moveButton;
    [SerializeField] private Button skillButton;
    [SerializeField] private Button itemButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button popupLogButton;

    [Header("Round UI")]
    [SerializeField] private TMP_Text turnStartText;
    [SerializeField] private float turnStartTextShowTime = 1.0f;

    [Header("Battle Log UI")]
    [SerializeField] private TMP_Text battleLogText;

    [Header("Popup Log UI")]
    [SerializeField] private GameObject popupLogPanel;
    [SerializeField] private TMP_Text popupLogText;
    [SerializeField] private ScrollRect popupLogScrollRect;

    [Header("Timings")]
    [SerializeField] private float turnDelay = 0.4f;
    [SerializeField] private float moveAnimationDuration = 0.4f;

    [Header("Attack Move")]
    [SerializeField] private float attackMoveRatio = 0.45f;
    [SerializeField] private float attackMoveMaxDistance = 260f;
    [SerializeField] private float attackMoveDuration = 0.6f;

    [Header("Cancel Button Colors")]
    [SerializeField] private Color cancelNormalColor = Color.white;
    [SerializeField] private Color cancelEnabledColor = new Color(0.9f, 0.25f, 0.25f, 1f);

    [Header("Prototype Item")]
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

    private bool playerActionSubmitted = false;
    private BattleUnit selectedAttackTarget;
    private BattleUnit selectedMoveTarget;
    private BattleUnit selectedItemTarget;

    private Image cancelButtonImage;
    private ColorBlock cancelButtonColors;

    private string latestBattleLog = "";
    private readonly List<string> fullBattleLogs = new List<string>();

    private const string UNIT_NAME_COLOR = "#817F7F";
    private const string DEFAULT_TEXT_COLOR = "#FFFFFF";
    private const string DAMAGE_COLOR = "#DA7332";
    private const string HEAL_COLOR = "#0EE01C";
    private const string BUFF_COLOR = "#4D4D4D";
    private const string TURN_COLOR = "#FFD966";

    private void Start()
    {
        if (attackButton != null)
            attackButton.onClick.AddListener(OnAttackButtonClicked);

        if (moveButton != null)
            moveButton.onClick.AddListener(OnMoveButtonClicked);

        if (skillButton != null)
            skillButton.onClick.AddListener(OnSkillButtonClicked);

        if (itemButton != null)
            itemButton.onClick.AddListener(OnItemButtonClicked);

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(OnCancelButtonClicked);
            cancelButtonImage = cancelButton.GetComponent<Image>();
            cancelButtonColors = cancelButton.colors;
        }

        if (popupLogButton != null)
            popupLogButton.onClick.AddListener(OnPopupLogButtonClicked);

        StartBattle();
    }

    public void StartBattle()
    {
        allyFormation = new BattleFormation();
        enemyFormation = new BattleFormation();
        turnManager = new TurnManager();

        currentRound = 0;
        inputMode = BattleInputMode.None;
        currentActingUnit = null;
        lastShownAllyUnit = null;
        playerActionSubmitted = false;
        selectedAttackTarget = null;
        selectedMoveTarget = null;
        selectedItemTarget = null;
        battleResult = BattleResultType.None;
        currentState = TurnState.Waiting;

        if (turnStartText != null)
            turnStartText.gameObject.SetActive(false);

        if (popupLogPanel != null)
            popupLogPanel.SetActive(false);

        ClearBattleLog();
        SetActionButtonsInteractable(false);
        RefreshCancelButtonState();

        SpawnPartyIntoFormation(allyPartyDefinition, TeamType.Ally, allyFormation);
        SpawnPartyIntoFormation(enemyPartyDefinition, TeamType.Enemy, enemyFormation);

        if (viewManager != null)
            viewManager.RefreshAllPositionsInstant(allyFormation, enemyFormation);

        ClearAllMarkersAndHighlights();

        lastShownAllyUnit = GetDefaultDisplayedAllyUnit();
        if (currentUnitInfoPanel != null && lastShownAllyUnit != null)
            currentUnitInfoPanel.Show(lastShownAllyUnit);

        AppendBattleLog(FormatDefaultText("전투가 시작되었습니다"));

        StartCoroutine(BattleLoop());
    }

    private void SpawnPartyIntoFormation(PartyDefinition partyDefinition, TeamType team, BattleFormation formation)
    {
        if (partyDefinition == null)
        {
            Debug.LogWarning($"[BattleManager] {team} PartyDefinition is null.");
            return;
        }

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

            BattleUnit unit = new BattleUnit(
                member.unitDefinition,
                member.unitViewDefinition,
                team,
                member.startSlotIndex
            );

            formation.SetUnit(member.startSlotIndex, unit);

            if (viewManager != null)
                viewManager.CreateView(unit, this);
        }
    }

    private IEnumerator BattleLoop()
    {
        while (battleResult == BattleResultType.None)
        {
            currentRound++;

            AppendBattleLog(FormatTurnLog(currentRound));
            yield return StartCoroutine(ShowTurnStartText(currentRound));

            List<BattleUnit> allAlive = new List<BattleUnit>();
            allAlive.AddRange(allyFormation.GetAliveUnits());
            allAlive.AddRange(enemyFormation.GetAliveUnits());

            turnManager.BuildTurnQueue(allAlive);

            while (turnManager.HasNextTurn() && battleResult == BattleResultType.None)
            {
                BattleUnit currentUnit = turnManager.GetNextUnit();

                if (currentUnit == null || currentUnit.IsDead)
                    continue;

                yield return StartCoroutine(ExecuteTurn(currentUnit));

                CheckBattleResult();
                if (battleResult != BattleResultType.None)
                    break;

                yield return new WaitForSeconds(turnDelay);
            }
        }

        currentState = TurnState.BattleEnded;
        SetActionButtonsInteractable(false);
        RefreshCancelButtonState();
        ClearAllMarkersAndHighlights();

        if (battleResult == BattleResultType.Victory)
            AppendBattleLog(FormatDefaultText("전투에서 승리했습니다"));
        else if (battleResult == BattleResultType.Defeat)
            AppendBattleLog(FormatDefaultText("전투에서 패배했습니다"));
    }

    private IEnumerator ExecuteTurn(BattleUnit unit)
    {
        if (unit == null || unit.IsDead)
            yield break;

        currentActingUnit = unit;

        if (unit.Team == TeamType.Ally)
        {
            lastShownAllyUnit = unit;

            if (currentUnitInfoPanel != null)
                currentUnitInfoPanel.Show(lastShownAllyUnit);
        }
        else
        {
            if (currentUnitInfoPanel != null && lastShownAllyUnit != null)
                currentUnitInfoPanel.Show(lastShownAllyUnit);
        }

        ClearAllMarkersAndHighlights();

        if (unit.Team == TeamType.Ally)
            ShowCurrentTurnMarker(unit, true);

        if (unit.Team == TeamType.Ally)
            yield return StartCoroutine(ExecutePlayerTurn(unit));
        else
            yield return StartCoroutine(ExecuteEnemyTurn(unit));

        if (unit.Team == TeamType.Ally)
            ShowCurrentTurnMarker(unit, false);

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

        currentActingUnit = null;
        currentState = TurnState.TurnEnding;
        RefreshCancelButtonState();
    }

    private IEnumerator ExecutePlayerTurn(BattleUnit unit)
    {
        currentState = TurnState.PlayerInput;
        inputMode = BattleInputMode.WaitingForAction;
        playerActionSubmitted = false;
        selectedAttackTarget = null;
        selectedMoveTarget = null;
        selectedItemTarget = null;

        SetActionButtonsInteractable(true);
        RefreshActionButtonAvailability(unit);
        RefreshCancelButtonState();

        while (!playerActionSubmitted)
            yield return null;

        SetActionButtonsInteractable(false);
        ClearAllTargetMarkersAndHighlights();
        inputMode = BattleInputMode.None;
        RefreshCancelButtonState();
    }

    private IEnumerator ExecuteEnemyTurn(BattleUnit unit)
    {
        currentState = TurnState.EnemyThinking;
        RefreshCancelButtonState();

        BattleFormation myFormation = enemyFormation;
        BattleFormation enemyFormationRef = allyFormation;

        List<BattleUnit> targets = BattleTargeting.GetBasicAttackTargets(unit, enemyFormationRef);

        if (targets.Count > 0)
        {
            BattleUnit target = ChooseBestTarget(unit, targets);
            yield return StartCoroutine(ExecuteBasicAttack(unit, target, null));
        }
        else
        {
            bool moved = TryAutoMove(unit, myFormation);

            if (moved)
            {
                AppendBattleLog(
                    $"{FormatUnitName(unit.Name)}이 {FormatDefaultText("위치를 이동했습니다")}"
                );
            }

            if (moved && viewManager != null)
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
    }

    public void OnAttackButtonClicked()
    {
        if (currentState != TurnState.PlayerInput) return;
        if (currentActingUnit == null || currentActingUnit.IsDead) return;

        inputMode = BattleInputMode.WaitingForAttackTarget;
        HighlightAttackableTargets(currentActingUnit);
        RefreshCancelButtonState();
    }

    public void OnMoveButtonClicked()
    {
        if (currentState != TurnState.PlayerInput) return;
        if (currentActingUnit == null || currentActingUnit.IsDead) return;

        inputMode = BattleInputMode.WaitingForMoveTarget;
        HighlightMoveableTargets(currentActingUnit);
        RefreshCancelButtonState();
    }

    public void OnSkillButtonClicked()
    {
        if (currentState != TurnState.PlayerInput) return;
        if (currentActingUnit == null || currentActingUnit.IsDead) return;

        inputMode = BattleInputMode.WaitingForSkillTarget;
        ClearAllTargetMarkersAndHighlights();

        if (currentActingUnit != null)
            ShowCurrentTurnMarker(currentActingUnit, true);

        RefreshCancelButtonState();
    }

    public void OnItemButtonClicked()
    {
        if (currentState != TurnState.PlayerInput) return;
        if (currentActingUnit == null || currentActingUnit.IsDead) return;

        inputMode = BattleInputMode.WaitingForItemTarget;
        HighlightItemTargets(currentActingUnit);
        RefreshCancelButtonState();
    }

    public void OnCancelButtonClicked()
    {
        if (currentState != TurnState.PlayerInput)
            return;

        if (!CanCancelCurrentSelection())
            return;

        CancelCurrentSelection();
    }

    public void OnPopupLogButtonClicked()
    {
        if (popupLogPanel == null)
            return;

        bool nextState = !popupLogPanel.activeSelf;
        popupLogPanel.SetActive(nextState);

        if (nextState)
            RefreshPopupBattleLogUI();
    }

    public void OnUnitViewClicked(BattleUnitView clickedView)
    {
        if (clickedView == null || clickedView.Unit == null)
            return;

        if (currentState != TurnState.PlayerInput)
            return;

        BattleUnit clickedUnit = clickedView.Unit;

        if (inputMode == BattleInputMode.WaitingForAttackTarget)
        {
            if (clickedUnit.Team != TeamType.Enemy)
                return;

            List<BattleUnit> validTargets = BattleTargeting.GetBasicAttackTargets(currentActingUnit, enemyFormation);
            if (!validTargets.Contains(clickedUnit))
                return;

            selectedAttackTarget = clickedUnit;
            StartCoroutine(ResolvePlayerAttack());
        }
        else if (inputMode == BattleInputMode.WaitingForMoveTarget)
        {
            if (clickedUnit.Team != TeamType.Ally)
                return;

            List<BattleUnit> validTargets = GetMoveableTargets(currentActingUnit, allyFormation);
            if (!validTargets.Contains(clickedUnit))
                return;

            selectedMoveTarget = clickedUnit;
            StartCoroutine(ResolvePlayerMove());
        }
        else if (inputMode == BattleInputMode.WaitingForItemTarget)
        {
            if (clickedUnit.Team != TeamType.Ally)
                return;

            List<BattleUnit> validTargets = GetItemTargets(currentActingUnit, allyFormation);
            if (!validTargets.Contains(clickedUnit))
                return;

            selectedItemTarget = clickedUnit;
            StartCoroutine(ResolvePlayerItemUse());
        }
        else if (inputMode == BattleInputMode.WaitingForSkillTarget)
        {
            // 스킬 시스템 추후 구현
        }
    }

    private IEnumerator ResolvePlayerAttack()
    {
        if (currentActingUnit == null || selectedAttackTarget == null)
            yield break;

        inputMode = BattleInputMode.None;
        RefreshCancelButtonState();
        ClearAllTargetMarkersAndHighlights();
        SetActionButtonsInteractable(false);

        yield return StartCoroutine(ExecuteBasicAttack(currentActingUnit, selectedAttackTarget, null));

        playerActionSubmitted = true;
    }

    private IEnumerator ResolvePlayerMove()
    {
        if (currentActingUnit == null || selectedMoveTarget == null)
            yield break;

        inputMode = BattleInputMode.None;
        RefreshCancelButtonState();
        ClearAllTargetMarkersAndHighlights();
        SetActionButtonsInteractable(false);

        bool moved = TrySwapUnits(currentActingUnit, selectedMoveTarget, allyFormation);

        if (moved)
        {
            AppendBattleLog(
                $"{FormatUnitName(currentActingUnit.Name)}이 {FormatUnitName(selectedMoveTarget.Name)}과 {FormatDefaultText("위치를 교체했습니다")}"
            );
        }

        if (moved && viewManager != null)
        {
            yield return StartCoroutine(
                viewManager.AnimateRefreshAllPositions(
                    allyFormation,
                    enemyFormation,
                    moveAnimationDuration
                )
            );
        }

        playerActionSubmitted = true;
    }

    private IEnumerator ResolvePlayerItemUse()
    {
        if (currentActingUnit == null || selectedItemTarget == null)
            yield break;

        inputMode = BattleInputMode.None;
        RefreshCancelButtonState();
        ClearAllTargetMarkersAndHighlights();
        SetActionButtonsInteractable(false);

        yield return StartCoroutine(ExecutePrototypePotionUse(currentActingUnit, selectedItemTarget));

        playerActionSubmitted = true;
    }

    private IEnumerator ExecutePrototypePotionUse(BattleUnit user, BattleUnit target)
    {
        if (user == null || target == null || user.IsDead || target.IsDead)
            yield break;

        BattleUnitView targetView = viewManager != null ? viewManager.GetView(target) : null;

        int beforeHP = target.CurrentHP;
        target.Heal(potionHealAmount);
        int healedAmount = target.CurrentHP - beforeHP;

        if (targetView != null)
            yield return StartCoroutine(targetView.AnimateHPChange(0.25f));

        AppendBattleLog(
            BuildItemHealLog(user, target, "포션을 사용하여", healedAmount, "회복")
        );
    }

    private bool CanCancelCurrentSelection()
    {
        return inputMode == BattleInputMode.WaitingForAttackTarget
            || inputMode == BattleInputMode.WaitingForMoveTarget
            || inputMode == BattleInputMode.WaitingForSkillTarget
            || inputMode == BattleInputMode.WaitingForItemTarget;
    }

    private void CancelCurrentSelection()
    {
        inputMode = BattleInputMode.WaitingForAction;
        selectedAttackTarget = null;
        selectedMoveTarget = null;
        selectedItemTarget = null;

        ClearAllTargetMarkersAndHighlights();

        if (currentActingUnit != null)
            ShowCurrentTurnMarker(currentActingUnit, true);

        SetActionButtonsInteractable(true);
        RefreshActionButtonAvailability(currentActingUnit);
        RefreshCancelButtonState();
    }

    private void HighlightAttackableTargets(BattleUnit attacker)
    {
        ClearAllTargetMarkersAndHighlights();

        List<BattleUnit> validTargets = BattleTargeting.GetBasicAttackTargets(attacker, enemyFormation);

        foreach (BattleUnit target in validTargets)
        {
            BattleUnitView view = viewManager != null ? viewManager.GetView(target) : null;
            if (view != null)
            {
                view.SetHighlighted(true);
                view.SetTargetMarker(true);
            }
        }

        if (attacker != null)
            ShowCurrentTurnMarker(attacker, true);
    }

    private void HighlightMoveableTargets(BattleUnit mover)
    {
        ClearAllTargetMarkersAndHighlights();

        List<BattleUnit> validTargets = GetMoveableTargets(mover, allyFormation);

        foreach (BattleUnit target in validTargets)
        {
            BattleUnitView view = viewManager != null ? viewManager.GetView(target) : null;
            if (view != null)
            {
                view.SetHighlighted(true);
                view.SetTargetMarker(true);
            }
        }

        if (mover != null)
            ShowCurrentTurnMarker(mover, true);
    }

    private void HighlightItemTargets(BattleUnit user)
    {
        ClearAllTargetMarkersAndHighlights();

        List<BattleUnit> validTargets = GetItemTargets(user, allyFormation);

        foreach (BattleUnit target in validTargets)
        {
            BattleUnitView view = viewManager != null ? viewManager.GetView(target) : null;
            if (view != null)
            {
                view.SetHighlighted(true);
                view.SetTargetMarker(true);
            }
        }

        if (user != null)
            ShowCurrentTurnMarker(user, true);
    }

    private List<BattleUnit> GetMoveableTargets(BattleUnit mover, BattleFormation formation)
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

    private List<BattleUnit> GetItemTargets(BattleUnit user, BattleFormation formation)
    {
        List<BattleUnit> result = new List<BattleUnit>();

        if (user == null || formation == null)
            return result;

        foreach (BattleUnit unit in formation.GetAliveUnits())
            result.Add(unit);

        return result;
    }

    private bool TrySwapUnits(BattleUnit a, BattleUnit b, BattleFormation formation)
    {
        if (a == null || b == null || formation == null) return false;
        if (a.IsDead || b.IsDead) return false;
        if (a.Team != b.Team) return false;

        int diff = Mathf.Abs(a.SlotIndex - b.SlotIndex);
        if (diff != 1) return false;

        int direction = b.SlotIndex > a.SlotIndex ? 1 : -1;
        return formation.TrySwapAdjacent(a.SlotIndex, direction);
    }

    private void ShowCurrentTurnMarker(BattleUnit unit, bool visible)
    {
        if (viewManager == null || unit == null)
            return;

        BattleUnitView view = viewManager.GetView(unit);
        if (view != null)
            view.SetCurrentTurnMarker(visible);
    }

    private void ClearAllTargetMarkersAndHighlights()
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

    private void SetActionButtonsInteractable(bool interactable)
    {
        if (attackButton != null) attackButton.interactable = interactable;
        if (moveButton != null) moveButton.interactable = interactable;
        if (skillButton != null) skillButton.interactable = interactable;
        if (itemButton != null) itemButton.interactable = interactable;
    }

    private void RefreshActionButtonAvailability(BattleUnit actingUnit)
    {
        if (currentState != TurnState.PlayerInput || actingUnit == null || actingUnit.IsDead)
            return;

        if (attackButton != null)
            attackButton.interactable = BattleTargeting.GetBasicAttackTargets(actingUnit, enemyFormation).Count > 0;

        if (moveButton != null)
            moveButton.interactable = GetMoveableTargets(actingUnit, allyFormation).Count > 0;

        if (skillButton != null)
            skillButton.interactable = true;

        if (itemButton != null)
            itemButton.interactable = GetItemTargets(actingUnit, allyFormation).Count > 0;
    }

    private void RefreshCancelButtonState()
    {
        bool canCancel = CanCancelCurrentSelection();

        if (cancelButton != null)
            cancelButton.interactable = canCancel;

        if (cancelButtonImage != null)
            cancelButtonImage.color = canCancel ? cancelEnabledColor : cancelNormalColor;

        if (cancelButton != null)
        {
            ColorBlock cb = cancelButtonColors;
            cb.normalColor = canCancel ? cancelEnabledColor : cancelNormalColor;
            cb.highlightedColor = canCancel ? cancelEnabledColor * 1.05f : cancelNormalColor * 1.05f;
            cb.selectedColor = cb.highlightedColor;
            cb.pressedColor = canCancel ? cancelEnabledColor * 0.9f : cancelNormalColor * 0.9f;
            cancelButton.colors = cb;
        }
    }

    private BattleUnit ChooseBestTarget(BattleUnit attacker, List<BattleUnit> targets)
    {
        BattleUnit bestTarget = targets[0];
        float bestScore = float.MinValue;

        foreach (BattleUnit target in targets)
        {
            float totalHitChance = BattleCalculator.CalculateTotalHitChance(attacker.HIT, target.AC);
            float missRatio = BattleCalculator.CalculateMissRatio(attacker.HIT, target.AC);

            float failChance = 100f - totalHitChance;
            float grazeChance = failChance * (1f - missRatio);
            float critChance = totalHitChance * (attacker.CRI / 100f);
            float normalHitChance = totalHitChance - critChance;

            float expectedDamage =
                (critChance / 100f) * BattleCalculator.CalculateCritDamage(attacker.DMG, attacker.CRD) +
                (normalHitChance / 100f) * BattleCalculator.CalculateHitDamage(attacker.DMG) +
                (grazeChance / 100f) * BattleCalculator.CalculateGrazeDamage(attacker.DMG);

            if (expectedDamage > bestScore)
            {
                bestScore = expectedDamage;
                bestTarget = target;
            }
        }

        return bestTarget;
    }

    private IEnumerator ExecuteBasicAttack(BattleUnit attacker, BattleUnit target, string skillName)
    {
        currentState = TurnState.ExecutingAction;
        RefreshCancelButtonState();

        if (attacker == null || target == null || attacker.IsDead || target.IsDead)
            yield break;

        BattleUnitView attackerView = viewManager != null ? viewManager.GetView(attacker) : null;
        BattleUnitView targetView = viewManager != null ? viewManager.GetView(target) : null;

        if (attackerView != null && targetView != null)
        {
            yield return StartCoroutine(
                attackerView.PlayAttackMove(
                    targetView.transform.position,
                    attackMoveRatio,
                    attackMoveMaxDistance,
                    attackMoveDuration
                )
            );
        }

        AttackResult result = BattleCalculator.RollAttack(attacker, target);

        if (result.Damage > 0)
        {
            target.TakeDamage(result.Damage);

            if (targetView != null)
                yield return StartCoroutine(targetView.AnimateHPChange(0.25f));
        }

        AppendBattleLog(BuildAttackLog(attacker, target, skillName, result));

        if (target.IsDead)
        {
            AppendBattleLog($"{FormatUnitName(target.Name)}이 {FormatDefaultText("사망했습니다")}");

            if (viewManager != null)
                viewManager.RemoveView(target);

            if (lastShownAllyUnit == target)
                lastShownAllyUnit = GetDefaultDisplayedAllyUnit();

            if (currentUnitInfoPanel != null && lastShownAllyUnit != null)
                currentUnitInfoPanel.Show(lastShownAllyUnit);
        }
    }

    private bool TryAutoMove(BattleUnit unit, BattleFormation formation)
    {
        if (unit == null || unit.IsDead)
            return false;

        int direction = GetPreferredMoveDirection(unit);

        if (direction != 0)
        {
            bool moved = formation.TrySwapAdjacent(unit.SlotIndex, direction);
            if (moved) return true;
        }

        if (direction != -1)
        {
            bool movedForward = formation.TrySwapAdjacent(unit.SlotIndex, -1);
            if (movedForward) return true;
        }

        if (direction != 1)
        {
            bool movedBackward = formation.TrySwapAdjacent(unit.SlotIndex, 1);
            if (movedBackward) return true;
        }

        return false;
    }

    private int GetPreferredMoveDirection(BattleUnit unit)
    {
        int rank = unit.SlotIndex + 1;

        switch (unit.RangeType)
        {
            case CharacterRangeType.Melee:
                if (rank >= 3) return -1;
                return 0;

            case CharacterRangeType.Mid:
                if (rank == 4) return -1;
                return 0;

            case CharacterRangeType.Ranged:
                if (rank == 1) return 1;
                return 0;
        }

        return 0;
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

    private IEnumerator ShowTurnStartText(int roundNumber)
    {
        if (turnStartText == null)
            yield break;

        turnStartText.text = $"Turn {roundNumber} Start";
        turnStartText.gameObject.SetActive(true);

        yield return new WaitForSeconds(turnStartTextShowTime);

        turnStartText.gameObject.SetActive(false);
    }

    private void AppendBattleLog(string message)
    {
        if (string.IsNullOrEmpty(message))
            return;

        latestBattleLog = message;
        fullBattleLogs.Add(message);

        RefreshBattleLogUI();
        RefreshPopupBattleLogUI();
    }

    private void RefreshBattleLogUI()
    {
        if (battleLogText == null)
            return;

        battleLogText.text = latestBattleLog;
    }

    private void RefreshPopupBattleLogUI()
    {
        if (popupLogText == null)
            return;

        popupLogText.text = string.Join("\n", fullBattleLogs);

        Canvas.ForceUpdateCanvases();
        if (popupLogScrollRect != null)
            popupLogScrollRect.verticalNormalizedPosition = 0f;
    }

    private void ClearBattleLog()
    {
        latestBattleLog = "";
        fullBattleLogs.Clear();
        RefreshBattleLogUI();
        RefreshPopupBattleLogUI();
    }

    private string FormatTurnLog(int round)
    {
        return $"<color={TURN_COLOR}>Turn{round}</color>";
    }

    private string FormatUnitName(string unitName)
    {
        return $"<color={UNIT_NAME_COLOR}>{unitName}</color>";
    }

    private string FormatDefaultText(string text)
    {
        return $"<color={DEFAULT_TEXT_COLOR}>{text}</color>";
    }

    private string FormatDamageValueOnlyNumber(int value)
    {
        return $"<color={DAMAGE_COLOR}>{value}</color>";
    }

    private string FormatHealValueOnlyNumber(int value)
    {
        return $"<color={HEAL_COLOR}>{value}</color>";
    }

    private string FormatBuffValueOnlyNumber(int value)
    {
        return $"<color={BUFF_COLOR}>{value}</color>";
    }

    private string FormatDamageKeyword()
    {
        return $"<color={DAMAGE_COLOR}>데미지</color>";
    }

    private string FormatHealKeyword()
    {
        return $"<color={HEAL_COLOR}>회복</color>";
    }

    private string FormatShieldKeyword()
    {
        return $"<color={BUFF_COLOR}>보호막</color>";
    }

    private string FormatBuffKeyword(string buffName)
    {
        return $"<color={BUFF_COLOR}>{buffName}</color>";
    }

    private string BuildAttackLog(BattleUnit attacker, BattleUnit target, string skillName, AttackResult result)
    {
        string attackerName = FormatUnitName(attacker.Name);
        string targetName = FormatUnitName(target.Name);

        string actionText = string.IsNullOrEmpty(skillName)
            ? ""
            : $"{FormatDefaultText(skillName)} ";

        switch (result.ResultType)
        {
            case AttackResultType.Crit:
                return $"{attackerName}이 {targetName}에게 {actionText}{FormatDefaultText("치명타로")} {FormatDamageValueOnlyNumber(result.Damage)} {FormatDamageKeyword()}를 {FormatDefaultText("입혔습니다")}";

            case AttackResultType.Hit:
                return $"{attackerName}이 {targetName}에게 {actionText}{FormatDamageValueOnlyNumber(result.Damage)} {FormatDamageKeyword()}를 {FormatDefaultText("입혔습니다")}";

            case AttackResultType.Graze:
                return $"{attackerName}이 {targetName}에게 {actionText}{FormatDefaultText("스침으로")} {FormatDamageValueOnlyNumber(result.Damage)} {FormatDamageKeyword()}를 {FormatDefaultText("입혔습니다")}";

            case AttackResultType.Miss:
                return $"{attackerName}이 {targetName}에게 {actionText}{FormatDefaultText("공격했지만 빗나갔습니다")}";
        }

        return $"{attackerName}이 {targetName}에게 {FormatDefaultText("공격했습니다")}";
    }

    private string BuildItemHealLog(BattleUnit user, BattleUnit target, string actionText, int value, string effectText)
    {
        return $"{FormatUnitName(user.Name)}이 {FormatUnitName(target.Name)}에게 {FormatDefaultText(actionText)} {FormatHealValueOnlyNumber(value)} {FormatHealKeyword()}을 {FormatDefaultText("회복시켰습니다")}";
    }

    private string BuildBuffLog(BattleUnit user, BattleUnit target, string actionText, int value, string buffText)
    {
        return $"{FormatUnitName(user.Name)}이 {FormatUnitName(target.Name)}에게 {FormatDefaultText(actionText)} {FormatBuffValueOnlyNumber(value)} {FormatBuffKeyword(buffText)}을 {FormatDefaultText("부여했습니다")}";
    }

    private string BuildShieldLog(BattleUnit user, BattleUnit target, string actionText, int value)
    {
        return $"{FormatUnitName(user.Name)}이 {FormatUnitName(target.Name)}에게 {FormatDefaultText(actionText)} {FormatBuffValueOnlyNumber(value)} {FormatShieldKeyword()}을 {FormatDefaultText("부여했습니다")}";
    }
}