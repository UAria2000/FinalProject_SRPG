using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleManager : MonoBehaviour
{
    private enum EnemyActionType
    {
        None,
        Skill,
        BasicAttack,
        Move
    }

    private struct EnemyActionChoice
    {
        public EnemyActionType actionType;
        public SkillDefinition skill;
        public BattleUnit target;
        public float expectedDamage;
    }

    [Header("Party Data")]
    [SerializeField] private PartyDefinition allyPartyDefinition;
    [SerializeField] private PartyDefinition enemyPartyDefinition;

    [Header("View")]
    [SerializeField] private BattleViewManager viewManager;

    [Header("Current Unit Info UI")]
    [SerializeField] private CurrentUnitInfoPanel currentUnitInfoPanel;

    [Header("Skill Tooltip UI")]
    [SerializeField] private SkillTooltipUI skillTooltipUI;

    [Header("Buttons")]
    [SerializeField] private Button attackButton;
    [SerializeField] private Button moveButton;
    [SerializeField] private Button[] skillButtons = new Button[3];
    [SerializeField] private Image[] skillIconImages = new Image[3];
    [SerializeField] private Image[] skillCooldownOverlayImages = new Image[3];
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
    private SkillDefinition selectedSkill;
    private BattleUnit selectedSkillTarget;

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

        for (int i = 0; i < skillButtons.Length; i++)
        {
            int skillIndex = i;

            if (skillButtons[i] != null)
            {
                skillButtons[i].onClick.AddListener(() => OnSkillButtonClicked(skillIndex));

                SkillButtonHoverHandler hoverHandler = skillButtons[i].GetComponent<SkillButtonHoverHandler>();
                if (hoverHandler == null)
                    hoverHandler = skillButtons[i].gameObject.AddComponent<SkillButtonHoverHandler>();

                hoverHandler.Initialize(this, skillIndex);
            }
        }

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
        selectedSkill = null;
        selectedSkillTarget = null;
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

        RefreshSkillButtons();
        HideSkillTooltip();

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
        RefreshSkillButtons();
        HideSkillTooltip();

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

        RefreshSkillButtons();

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

        currentActingUnit = null;
        currentState = TurnState.TurnEnding;
        RefreshCancelButtonState();
        RefreshSkillButtons();
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

        SetActionButtonsInteractable(true);
        RefreshActionButtonAvailability(unit);
        RefreshCancelButtonState();
        RefreshSkillButtons();

        while (!playerActionSubmitted)
            yield return null;

        SetActionButtonsInteractable(false);
        ClearAllTargetMarkersAndHighlights();
        inputMode = BattleInputMode.None;
        RefreshCancelButtonState();
        RefreshSkillButtons();
        HideSkillTooltip();
    }

    private IEnumerator ExecuteEnemyTurn(BattleUnit unit)
    {
        currentState = TurnState.EnemyThinking;
        RefreshCancelButtonState();
        RefreshSkillButtons();

        EnemyActionChoice choice = ChooseBestEnemyAction(unit);

        switch (choice.actionType)
        {
            case EnemyActionType.Skill:
                yield return StartCoroutine(ExecuteSkill(unit, choice.target, choice.skill));
                break;

            case EnemyActionType.BasicAttack:
                yield return StartCoroutine(ExecuteBasicAttack(unit, choice.target, null));
                break;

            case EnemyActionType.Move:
                bool moved = TryAutoMove(unit, enemyFormation);

                if (moved)
                {
                    AppendBattleLog(
                        $"{FormatUnitName(unit.Name)}이 {FormatDefaultText("위치를 이동했습니다")}"
                    );

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

    public void OnAttackButtonClicked()
    {
        if (currentState != TurnState.PlayerInput) return;
        if (currentActingUnit == null || currentActingUnit.IsDead) return;

        inputMode = BattleInputMode.WaitingForAttackTarget;
        selectedSkill = null;
        selectedSkillTarget = null;
        HighlightAttackableTargets(currentActingUnit);
        RefreshCancelButtonState();
        RefreshSkillButtons();
        HideSkillTooltip();
    }

    public void OnMoveButtonClicked()
    {
        if (currentState != TurnState.PlayerInput) return;
        if (currentActingUnit == null || currentActingUnit.IsDead) return;

        inputMode = BattleInputMode.WaitingForMoveTarget;
        selectedSkill = null;
        selectedSkillTarget = null;
        HighlightMoveableTargets(currentActingUnit);
        RefreshCancelButtonState();
        RefreshSkillButtons();
        HideSkillTooltip();
    }

    public void OnSkillButtonClicked(int skillIndex)
    {
        if (currentState != TurnState.PlayerInput) return;
        if (currentActingUnit == null || currentActingUnit.IsDead) return;

        SkillDefinition skill = currentActingUnit.GetSkillAt(skillIndex);
        if (skill == null) return;
        if (!currentActingUnit.CanUseSkill(skill)) return;

        List<BattleUnit> validTargets = GetPrimarySkillTargets(currentActingUnit, skill);
        if (validTargets.Count <= 0) return;

        selectedSkill = skill;
        selectedSkillTarget = null;
        inputMode = BattleInputMode.WaitingForSkillTarget;

        HighlightSkillTargets(currentActingUnit, skill);
        RefreshCancelButtonState();
        RefreshSkillButtons();
        HideSkillTooltip();
    }

    public void OnItemButtonClicked()
    {
        if (currentState != TurnState.PlayerInput) return;
        if (currentActingUnit == null || currentActingUnit.IsDead) return;

        inputMode = BattleInputMode.WaitingForItemTarget;
        selectedSkill = null;
        selectedSkillTarget = null;
        HighlightItemTargets(currentActingUnit);
        RefreshCancelButtonState();
        RefreshSkillButtons();
        HideSkillTooltip();
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

        HideSkillTooltip();
    }

    public void OnUnitViewClicked(BattleUnitView clickedView)
    {
        if (clickedView == null || clickedView.Unit == null)
            return;

        BattleUnit clickedUnit = clickedView.Unit;

        if (currentState == TurnState.PlayerInput)
        {
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
                if (selectedSkill == null)
                    return;

                List<BattleUnit> validTargets = GetPrimarySkillTargets(currentActingUnit, selectedSkill);
                if (!validTargets.Contains(clickedUnit))
                    return;

                selectedSkillTarget = clickedUnit;
                StartCoroutine(ResolvePlayerSkillUse());
            }
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
        RefreshSkillButtons();

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
        RefreshSkillButtons();

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
        RefreshSkillButtons();

        yield return StartCoroutine(ExecutePrototypePotionUse(currentActingUnit, selectedItemTarget));

        playerActionSubmitted = true;
    }

    private IEnumerator ResolvePlayerSkillUse()
    {
        if (currentActingUnit == null || selectedSkill == null || selectedSkillTarget == null)
            yield break;

        inputMode = BattleInputMode.None;
        RefreshCancelButtonState();
        ClearAllTargetMarkersAndHighlights();
        SetActionButtonsInteractable(false);
        RefreshSkillButtons();

        yield return StartCoroutine(ExecuteSkill(currentActingUnit, selectedSkillTarget, selectedSkill));

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

    private IEnumerator ExecuteBasicAttack(BattleUnit attacker, BattleUnit target, string skillName)
    {
        currentState = TurnState.ExecutingAction;
        RefreshCancelButtonState();
        RefreshSkillButtons();

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

    private IEnumerator ExecuteSkill(BattleUnit attacker, BattleUnit primaryTarget, SkillDefinition skill)
    {
        currentState = TurnState.ExecutingAction;
        RefreshCancelButtonState();
        RefreshSkillButtons();

        if (attacker == null || primaryTarget == null || skill == null)
            yield break;

        if (attacker.IsDead || primaryTarget.IsDead)
            yield break;

        BattleUnitView attackerView = viewManager != null ? viewManager.GetView(attacker) : null;
        BattleUnitView primaryTargetView = viewManager != null ? viewManager.GetView(primaryTarget) : null;

        if (attackerView != null && primaryTargetView != null)
        {
            yield return StartCoroutine(
                attackerView.PlayAttackMove(
                    primaryTargetView.transform.position,
                    attackMoveRatio,
                    attackMoveMaxDistance,
                    attackMoveDuration
                )
            );
        }

        switch (skill.effectType)
        {
            case SkillEffectType.MultiHitSingleTarget:
                {
                    for (int hitIndex = 0; hitIndex < Mathf.Max(1, skill.hitCount); hitIndex++)
                    {
                        if (primaryTarget == null || primaryTarget.IsDead)
                            break;

                        yield return StartCoroutine(ApplySkillStrike(attacker, primaryTarget, skill, skill.primaryDamageMultiplier));
                    }
                    break;
                }

            case SkillEffectType.FrontAndBackShot:
                {
                    BattleUnit secondaryTarget = GetBackTarget(primaryTarget);

                    if (primaryTarget != null && !primaryTarget.IsDead)
                        yield return StartCoroutine(ApplySkillStrike(attacker, primaryTarget, skill, skill.primaryDamageMultiplier));

                    if (secondaryTarget != null && !secondaryTarget.IsDead)
                        yield return StartCoroutine(ApplySkillStrike(attacker, secondaryTarget, skill, skill.secondaryDamageMultiplier));

                    break;
                }
        }

        attacker.ConsumeSkillCooldown(skill);
        RefreshSkillButtons();
    }

    private IEnumerator ApplySkillStrike(BattleUnit attacker, BattleUnit target, SkillDefinition skill, float damageMultiplier)
    {
        if (attacker == null || target == null || skill == null)
            yield break;

        if (attacker.IsDead || target.IsDead)
            yield break;

        AttackResult result = RollSkillAttack(attacker, target, skill.accuracyMultiplier, damageMultiplier);

        if (result.Damage > 0)
        {
            target.TakeDamage(result.Damage);

            BattleUnitView targetView = viewManager != null ? viewManager.GetView(target) : null;
            if (targetView != null)
                yield return StartCoroutine(targetView.AnimateHPChange(0.2f));
        }

        AppendBattleLog(BuildAttackLog(attacker, target, skill.skillName, result));

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

    private AttackResult RollSkillAttack(BattleUnit attacker, BattleUnit target, float accuracyMultiplier, float damageMultiplier)
    {
        float totalHitChance = BattleCalculator.CalculateTotalHitChance(attacker.HIT, target.AC);
        totalHitChance = Mathf.Clamp(totalHitChance * accuracyMultiplier, 0f, 100f);

        float failChance = 100f - totalHitChance;
        float missRatio = BattleCalculator.CalculateMissRatio(attacker.HIT, target.AC);

        float grazeChance = failChance * (1f - missRatio);
        float missChance = failChance * missRatio;

        float critChance = totalHitChance * (attacker.CRI / 100f);
        float normalHitChance = totalHitChance - critChance;

        int scaledBaseDamage = Mathf.Max(1, Mathf.RoundToInt(attacker.DMG * damageMultiplier));

        float roll = Random.Range(0f, 100f);

        AttackResultType resultType;
        int damage = 0;

        if (roll < critChance)
        {
            resultType = AttackResultType.Crit;
            damage = BattleCalculator.CalculateCritDamage(scaledBaseDamage, attacker.CRD);
        }
        else if (roll < critChance + normalHitChance)
        {
            resultType = AttackResultType.Hit;
            damage = BattleCalculator.CalculateHitDamage(scaledBaseDamage);
        }
        else if (roll < critChance + normalHitChance + grazeChance)
        {
            resultType = AttackResultType.Graze;
            damage = BattleCalculator.CalculateGrazeDamage(scaledBaseDamage);
        }
        else
        {
            resultType = AttackResultType.Miss;
            damage = 0;
        }

        return new AttackResult
        {
            ResultType = resultType,
            Damage = damage,
            CritChance = critChance,
            HitChance = normalHitChance,
            GrazeChance = grazeChance,
            MissChance = missChance
        };
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
        selectedSkill = null;
        selectedSkillTarget = null;

        ClearAllTargetMarkersAndHighlights();

        if (currentActingUnit != null)
            ShowCurrentTurnMarker(currentActingUnit, true);

        SetActionButtonsInteractable(true);
        RefreshActionButtonAvailability(currentActingUnit);
        RefreshCancelButtonState();
        RefreshSkillButtons();
        HideSkillTooltip();
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

    private void HighlightSkillTargets(BattleUnit user, SkillDefinition skill)
    {
        ClearAllTargetMarkersAndHighlights();

        List<BattleUnit> validTargets = GetPrimarySkillTargets(user, skill);

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

    private List<BattleUnit> GetPrimarySkillTargets(BattleUnit user, SkillDefinition skill)
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

        if (itemButton != null)
            itemButton.interactable = GetItemTargets(actingUnit, allyFormation).Count > 0;
    }

    private void RefreshSkillButtons()
    {
        BattleUnit displayUnit = GetSkillButtonDisplayUnit();
        bool allowUse = currentState == TurnState.PlayerInput &&
                        currentActingUnit != null &&
                        !currentActingUnit.IsDead &&
                        currentActingUnit.Team == TeamType.Ally;

        for (int i = 0; i < skillButtons.Length; i++)
        {
            Button button = i < skillButtons.Length ? skillButtons[i] : null;
            Image iconImage = i < skillIconImages.Length ? skillIconImages[i] : null;
            Image overlayImage = i < skillCooldownOverlayImages.Length ? skillCooldownOverlayImages[i] : null;

            UpdateSkillButtonVisual(button, iconImage, overlayImage, displayUnit, i, allowUse);
        }
    }

    private BattleUnit GetSkillButtonDisplayUnit()
    {
        if (currentActingUnit != null && currentActingUnit.Team == TeamType.Ally)
            return currentActingUnit;

        return lastShownAllyUnit;
    }

    private void UpdateSkillButtonVisual(Button button, Image iconImage, Image overlayImage, BattleUnit displayUnit, int slotIndex, bool allowUse)
    {
        if (button == null)
            return;

        SkillDefinition skill = displayUnit != null ? displayUnit.GetSkillAt(slotIndex) : null;
        bool hasSkill = skill != null;

        if (iconImage != null)
        {
            iconImage.sprite = hasSkill ? skill.icon : null;
            iconImage.color = hasSkill ? Color.white : new Color(1f, 1f, 1f, 0.2f);
        }

        bool interactable = false;
        if (allowUse && displayUnit == currentActingUnit && hasSkill)
            interactable = currentActingUnit.CanUseSkill(skill);

        button.interactable = interactable;

        if (overlayImage != null)
        {
            if (!hasSkill)
            {
                overlayImage.gameObject.SetActive(false);
            }
            else
            {
                int remaining = displayUnit.GetRemainingCooldown(skill);
                if (remaining > 0)
                {
                    overlayImage.gameObject.SetActive(true);

                    float divisor = Mathf.Max(1f, skill.cooldownTurns + 1f);
                    overlayImage.fillAmount = Mathf.Clamp01(remaining / divisor);
                }
                else
                {
                    overlayImage.gameObject.SetActive(false);
                    overlayImage.fillAmount = 0f;
                }
            }
        }
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

    private EnemyActionChoice ChooseBestEnemyAction(BattleUnit attacker)
    {
        EnemyActionChoice best = new EnemyActionChoice
        {
            actionType = EnemyActionType.None,
            skill = null,
            target = null,
            expectedDamage = float.MinValue
        };

        if (attacker == null || attacker.IsDead)
            return best;

        for (int i = 0; i < attacker.EquippedSkills.Count; i++)
        {
            SkillDefinition skill = attacker.EquippedSkills[i];
            if (skill == null)
                continue;

            if (!attacker.CanUseSkill(skill))
                continue;

            List<BattleUnit> validTargets = GetPrimarySkillTargets(attacker, skill);
            if (validTargets.Count <= 0)
                continue;

            BattleUnit lowestHpTarget = GetLowestHpTarget(validTargets);
            float expectedDamage = EstimateSkillExpectedDamage(attacker, skill, lowestHpTarget);

            if (expectedDamage > best.expectedDamage)
            {
                best.actionType = EnemyActionType.Skill;
                best.skill = skill;
                best.target = lowestHpTarget;
                best.expectedDamage = expectedDamage;
            }
        }

        if (best.actionType == EnemyActionType.Skill)
            return best;

        BattleFormation enemyFormationRef = attacker.Team == TeamType.Ally ? enemyFormation : allyFormation;
        List<BattleUnit> basicTargets = BattleTargeting.GetBasicAttackTargets(attacker, enemyFormationRef);

        if (basicTargets.Count > 0)
        {
            best.actionType = EnemyActionType.BasicAttack;
            best.target = GetLowestHpTarget(basicTargets);
            best.expectedDamage = EstimateBasicAttackExpectedDamage(attacker, best.target);
            return best;
        }

        best.actionType = EnemyActionType.Move;
        return best;
    }

    private BattleUnit GetLowestHpTarget(List<BattleUnit> targets)
    {
        BattleUnit best = null;
        int lowestHp = int.MaxValue;

        for (int i = 0; i < targets.Count; i++)
        {
            BattleUnit unit = targets[i];
            if (unit == null || unit.IsDead)
                continue;

            if (unit.CurrentHP < lowestHp)
            {
                lowestHp = unit.CurrentHP;
                best = unit;
            }
        }

        return best;
    }

    private float EstimateBasicAttackExpectedDamage(BattleUnit attacker, BattleUnit target)
    {
        if (attacker == null || target == null)
            return 0f;

        return EstimateSingleStrikeExpectedDamage(attacker, target, 1f, 1f);
    }

    private float EstimateSkillExpectedDamage(BattleUnit attacker, SkillDefinition skill, BattleUnit primaryTarget)
    {
        if (attacker == null || skill == null || primaryTarget == null)
            return 0f;

        switch (skill.effectType)
        {
            case SkillEffectType.MultiHitSingleTarget:
                {
                    float oneHit = EstimateSingleStrikeExpectedDamage(
                        attacker,
                        primaryTarget,
                        skill.accuracyMultiplier,
                        skill.primaryDamageMultiplier
                    );

                    return oneHit * Mathf.Max(1, skill.hitCount);
                }

            case SkillEffectType.FrontAndBackShot:
                {
                    float total = EstimateSingleStrikeExpectedDamage(
                        attacker,
                        primaryTarget,
                        skill.accuracyMultiplier,
                        skill.primaryDamageMultiplier
                    );

                    BattleUnit backTarget = GetBackTarget(primaryTarget);
                    if (backTarget != null && !backTarget.IsDead)
                    {
                        total += EstimateSingleStrikeExpectedDamage(
                            attacker,
                            backTarget,
                            skill.accuracyMultiplier,
                            skill.secondaryDamageMultiplier
                        );
                    }

                    return total;
                }
        }

        return 0f;
    }

    private float EstimateSingleStrikeExpectedDamage(BattleUnit attacker, BattleUnit target, float accuracyMultiplier, float damageMultiplier)
    {
        if (attacker == null || target == null)
            return 0f;

        float totalHitChance = BattleCalculator.CalculateTotalHitChance(attacker.HIT, target.AC);
        totalHitChance = Mathf.Clamp(totalHitChance * accuracyMultiplier, 0f, 100f);

        float failChance = 100f - totalHitChance;
        float missRatio = BattleCalculator.CalculateMissRatio(attacker.HIT, target.AC);

        float grazeChance = failChance * (1f - missRatio);
        float critChance = totalHitChance * (attacker.CRI / 100f);
        float normalHitChance = totalHitChance - critChance;

        int scaledBaseDamage = Mathf.Max(1, Mathf.RoundToInt(attacker.DMG * damageMultiplier));

        float expected =
            (critChance / 100f) * BattleCalculator.CalculateCritDamage(scaledBaseDamage, attacker.CRD) +
            (normalHitChance / 100f) * BattleCalculator.CalculateHitDamage(scaledBaseDamage) +
            (grazeChance / 100f) * BattleCalculator.CalculateGrazeDamage(scaledBaseDamage);

        return expected;
    }

    private BattleUnit GetBackTarget(BattleUnit primaryTarget)
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

    public void ShowSkillTooltip(int skillSlotIndex, Vector2 screenPosition)
    {
        if (skillTooltipUI == null)
            return;

        BattleUnit displayUnit = GetSkillButtonDisplayUnit();
        if (displayUnit == null)
        {
            skillTooltipUI.Hide();
            return;
        }

        SkillDefinition skill = displayUnit.GetSkillAt(skillSlotIndex);
        if (skill == null)
        {
            skillTooltipUI.Hide();
            return;
        }

        skillTooltipUI.Show(skill, screenPosition);
    }

    public void MoveSkillTooltip(Vector2 screenPosition)
    {
        if (skillTooltipUI == null)
            return;

        skillTooltipUI.Move(screenPosition);
    }

    public void HideSkillTooltip()
    {
        if (skillTooltipUI == null)
            return;

        skillTooltipUI.Hide();
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