using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class WorldEventController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WorldEventPopupUI eventPopupUI;
    [SerializeField] private WorldQuestController questController;
    [SerializeField] private WorldBattleBridge battleBridge;
    [SerializeField] private BattleManager battleManager;

    [Header("Popup Labels")]
    [SerializeField] private string defaultConfirmText = "확인";
    [SerializeField] private string battleMissingText = "전투 연결이 아직 설정되지 않았습니다.";
    [SerializeField] private string graveyardSuffix = "\n\n묘지는 재사용 가능한 이벤트로 남아 있습니다.";
    [SerializeField] private string merchantSuffix = "\n\n상점 상세 기능은 추후 연결 예정입니다.";
    [SerializeField] private string treasureSuffix = "\n\n보물을 발견했습니다.";

    [Header("Treasure Event")]
    [SerializeField] private string treasureConfirmText = "획득하기";
    [SerializeField] private string treasureRewardHeaderText = "획득 예정 보상";
    [SerializeField] private string treasureEmptyText = "보물 후보 아이템이 없습니다. Treasure Candidate Items를 설정해 주세요.";
    [SerializeField] private string treasureNoRewardText = "획득 가능한 보상이 없습니다.";
    [SerializeField] private List<ItemDefinition> treasureCandidateItems = new List<ItemDefinition>();
    [SerializeField] private List<WorldTreasureTierWeight> treasureTierWeights = new List<WorldTreasureTierWeight>
    {
        new WorldTreasureTierWeight { tier = ItemTier.Tier1, weight = 70f },
        new WorldTreasureTierWeight { tier = ItemTier.Tier2, weight = 25f },
        new WorldTreasureTierWeight { tier = ItemTier.Tier3, weight = 5f },
    };
    [SerializeField, Min(0)] private int treasureMinConsumableTypes = 0;
    [SerializeField, Min(0)] private int treasureMaxConsumableTypes = 1;
    [SerializeField, Min(0)] private int treasureMinEquipmentTypes = 0;
    [SerializeField, Min(0)] private int treasureMaxEquipmentTypes = 3;
    [SerializeField, Min(1)] private int treasureMinTotalItemTypes = 1;
    [SerializeField, Min(1)] private int treasureMaxTotalItemTypes = 4;
    [SerializeField] private Vector2Int treasureConsumableAmountRange = new Vector2Int(1, 3);

    [Header("Rest Event")]
    [SerializeField] private string restConfirmText = "휴식하기";
    [SerializeField] private string restEffectHeaderText = "휴식 효과";
    [SerializeField] private string restPartyPreviewHeaderText = "파티 상태";
    [TextArea(2, 4)]
    [SerializeField] private string restDescriptionSuffix = "\n\n파티가 휴식을 취해 체력을 회복합니다.";
    [SerializeField] private WorldRestHealMode restHealMode = WorldRestHealMode.PercentOfMaxHp;
    [Range(0f, 100f)]
    [SerializeField] private float restHealPercentOfMaxHp = 50f;
    [Min(0)]
    [SerializeField] private int restFlatHealAmount = 0;
    [Tooltip("기본값은 Off. 사망 유닛은 휴식으로 부활하지 않고, 차후 묘지 이벤트에서 부활시키는 흐름을 권장합니다.")]
    [SerializeField] private bool restCanReviveDeadUnits = false;
    [SerializeField] private string restNoPartyText = "휴식할 파티원이 없습니다.";
    [SerializeField] private string restDeadUnitText = "사망 - 휴식 불가";

    private WorldRunManager runManager;
    private WorldGenerationSettings settings;
    private bool popupOpen;
    private readonly Dictionary<int, WorldTreasureResult> pendingTreasureByTileId = new Dictionary<int, WorldTreasureResult>();

    public bool IsBusy =>
        popupOpen ||
        (eventPopupUI != null && eventPopupUI.IsOpen) ||
        (questController != null && questController.IsPopupOpen) ||
        (battleBridge != null && battleBridge.IsBattleRunning);

    public void Initialize(WorldRunManager manager, WorldGenerationSettings generationSettings)
    {
        runManager = manager;
        settings = generationSettings;

        if (questController == null)
            questController = Object.FindFirstObjectByType<WorldQuestController>();

        if (battleBridge != null)
            battleBridge.Initialize(manager, generationSettings);
    }

    public bool TryHandleArrival(WorldTileData tile)
    {
        if (tile == null || !tile.ShouldTriggerEventOnArrival)
            return false;

        if (tile.IsCombatEvent)
            return TryStartCombatEvent(tile);

        if (tile.eventType == WorldTileEventType.Quest)
            return TryOpenQuestEvent(tile);

        if (tile.eventType == WorldTileEventType.Rest)
            return TryOpenRestEvent(tile);

        if (tile.eventType == WorldTileEventType.Treasure)
            return TryOpenTreasureEvent(tile);

        return TryOpenSimpleEvent(tile);
    }

    public void OpenWorldSettlementFromMap()
    {
        if (battleBridge == null || runManager == null || !runManager.IsWorldConquestAvailable())
            return;

        battleBridge.OpenSettlementFromWorldMap(true);
    }

    private bool TryStartCombatEvent(WorldTileData tile)
    {
        if (battleBridge != null && battleBridge.StartBattleForTile(tile))
            return true;

        OpenFallbackPopup(tile, battleMissingText, () =>
        {
            popupOpen = false;
            if (runManager != null)
                runManager.ResolveCombatDefeat(tile, true);
        });

        return false;
    }

    private bool TryOpenQuestEvent(WorldTileData tile)
    {
        if (questController != null && questController.TryOpenQuestOfferFromTile(tile))
            return true;

        return TryOpenSimpleEvent(tile);
    }

    private bool TryOpenRestEvent(WorldTileData tile)
    {
        if (eventPopupUI == null)
        {
            Debug.LogWarning("[WorldEventController] WorldEventPopupUI reference is missing.");
            return false;
        }

        popupOpen = true;
        string title = settings != null ? settings.GetEventDisplayName(tile.eventType) : tile.eventType.ToString();
        string body = BuildRestEventBody(tile);

        eventPopupUI.Open(
            title,
            body,
            string.IsNullOrWhiteSpace(restConfirmText) ? defaultConfirmText : restConfirmText,
            () => ConfirmRestEvent(tile),
            () => popupOpen = false);

        return true;
    }

    private bool TryOpenTreasureEvent(WorldTileData tile)
    {
        if (eventPopupUI == null)
        {
            Debug.LogWarning("[WorldEventController] WorldEventPopupUI reference is missing.");
            return false;
        }

        popupOpen = true;
        WorldTreasureResult treasure = GetOrCreateTreasureForTile(tile);
        string title = settings != null ? settings.GetEventDisplayName(tile.eventType) : tile.eventType.ToString();
        string body = BuildTreasureEventBody(tile, treasure);

        eventPopupUI.OpenTreasure(
            title,
            body,
            string.IsNullOrWhiteSpace(treasureConfirmText) ? defaultConfirmText : treasureConfirmText,
            treasure,
            runManager,
            () => ConfirmTreasureEvent(tile),
            () => popupOpen = false);

        return true;
    }

    private void ConfirmTreasureEvent(WorldTileData tile)
    {
        popupOpen = false;

        // 보물 보상은 WorldEventPopupUI의 슬롯 상호작용에서 지급된다.
        // 슬롯을 연결하지 않은 구형 UI에서는 WorldEventPopupUI가 확인 시 자동 지급한다.

        if (tile != null)
            pendingTreasureByTileId.Remove(tile.tileId);

        bool isReusable = tile != null && tile.IsReusableEvent;
        bool disableIcon = !isReusable;
        bool markResolved = !isReusable;

        if (runManager != null)
            runManager.ResolveMapEvent(tile, true, markResolved, disableIcon);
    }

    private WorldTreasureResult GetOrCreateTreasureForTile(WorldTileData tile)
    {
        int key = tile != null ? tile.tileId : -1;
        if (pendingTreasureByTileId.TryGetValue(key, out WorldTreasureResult cached) && cached != null)
            return cached;

        WorldTreasureResult generated = GenerateTreasureReward();
        pendingTreasureByTileId[key] = generated;
        return generated;
    }

    private WorldTreasureResult GenerateTreasureReward()
    {
        WorldTreasureResult result = new WorldTreasureResult();

        List<ItemDefinition> consumables = BuildTreasureCandidateList(MainUIItemCategory.Consumable);
        List<ItemDefinition> equipment = BuildTreasureCandidateList(MainUIItemCategory.Equipment);

        int maxConsumable = Mathf.Clamp(treasureMaxConsumableTypes, 0, 1);
        int minConsumable = Mathf.Clamp(treasureMinConsumableTypes, 0, maxConsumable);
        int maxEquipment = Mathf.Clamp(treasureMaxEquipmentTypes, 0, 3);
        int minEquipment = Mathf.Clamp(treasureMinEquipmentTypes, 0, maxEquipment);
        int maxTotal = Mathf.Clamp(treasureMaxTotalItemTypes, 1, 4);
        int minTotal = Mathf.Clamp(treasureMinTotalItemTypes, 1, maxTotal);

        if (consumables.Count == 0)
        {
            minConsumable = 0;
            maxConsumable = 0;
        }

        if (equipment.Count == 0)
        {
            minEquipment = 0;
            maxEquipment = 0;
        }

        int consumableCount = maxConsumable > 0 ? Random.Range(minConsumable, maxConsumable + 1) : 0;
        int equipmentCount = maxEquipment > 0 ? Random.Range(minEquipment, maxEquipment + 1) : 0;

        int total = consumableCount + equipmentCount;
        while (total < minTotal && total < maxTotal)
        {
            bool canAddConsumable = consumableCount < maxConsumable && consumables.Count > consumableCount;
            bool canAddEquipment = equipmentCount < maxEquipment && equipment.Count > equipmentCount;

            if (!canAddConsumable && !canAddEquipment)
                break;

            if (canAddConsumable && canAddEquipment)
            {
                if (Random.value < 0.5f)
                    consumableCount++;
                else
                    equipmentCount++;
            }
            else if (canAddConsumable)
            {
                consumableCount++;
            }
            else
            {
                equipmentCount++;
            }

            total = consumableCount + equipmentCount;
        }

        while (total > maxTotal)
        {
            if (equipmentCount > minEquipment)
                equipmentCount--;
            else if (consumableCount > minConsumable)
                consumableCount--;
            else
                break;

            total = consumableCount + equipmentCount;
        }

        AddRolledTreasureItems(result, consumables, consumableCount, true);
        AddRolledTreasureItems(result, equipment, equipmentCount, false);

        return result;
    }

    private List<ItemDefinition> BuildTreasureCandidateList(MainUIItemCategory category)
    {
        List<ItemDefinition> result = new List<ItemDefinition>();
        if (treasureCandidateItems == null)
            return result;

        for (int i = 0; i < treasureCandidateItems.Count; i++)
        {
            ItemDefinition item = treasureCandidateItems[i];
            if (item == null)
                continue;
            if (item.mainUICategory != category)
                continue;
            if (result.Contains(item))
                continue;

            result.Add(item);
        }

        return result;
    }

    private void AddRolledTreasureItems(WorldTreasureResult result, List<ItemDefinition> pool, int count, bool isConsumable)
    {
        if (result == null || pool == null || count <= 0)
            return;

        int safeCount = Mathf.Min(count, pool.Count);
        for (int i = 0; i < safeCount; i++)
        {
            ItemDefinition selected = PickWeightedTreasureItem(pool);
            if (selected == null)
                break;

            pool.Remove(selected);
            int amount = isConsumable ? RollTreasureConsumableAmount() : 1;
            result.Add(selected, amount);
        }
    }

    private int RollTreasureConsumableAmount()
    {
        int min = Mathf.Max(1, Mathf.Min(treasureConsumableAmountRange.x, treasureConsumableAmountRange.y));
        int max = Mathf.Max(min, Mathf.Max(treasureConsumableAmountRange.x, treasureConsumableAmountRange.y));
        return Random.Range(min, max + 1);
    }

    private ItemDefinition PickWeightedTreasureItem(List<ItemDefinition> pool)
    {
        if (pool == null || pool.Count == 0)
            return null;

        List<ItemTier> availableTiers = new List<ItemTier>();
        List<float> availableWeights = new List<float>();
        float totalWeight = 0f;

        for (int i = 0; i < treasureTierWeights.Count; i++)
        {
            WorldTreasureTierWeight tierWeight = treasureTierWeights[i];
            if (tierWeight == null || tierWeight.weight <= 0f)
                continue;

            if (!PoolHasTier(pool, tierWeight.tier))
                continue;

            availableTiers.Add(tierWeight.tier);
            availableWeights.Add(tierWeight.weight);
            totalWeight += tierWeight.weight;
        }

        if (availableTiers.Count == 0 || totalWeight <= 0f)
            return pool[Random.Range(0, pool.Count)];

        float roll = Random.value * totalWeight;
        float cursor = 0f;
        ItemTier selectedTier = availableTiers[availableTiers.Count - 1];

        for (int i = 0; i < availableTiers.Count; i++)
        {
            cursor += availableWeights[i];
            if (roll <= cursor)
            {
                selectedTier = availableTiers[i];
                break;
            }
        }

        List<ItemDefinition> tierItems = new List<ItemDefinition>();
        for (int i = 0; i < pool.Count; i++)
        {
            ItemDefinition item = pool[i];
            if (item != null && item.itemTier == selectedTier)
                tierItems.Add(item);
        }

        if (tierItems.Count == 0)
            return pool[Random.Range(0, pool.Count)];

        return tierItems[Random.Range(0, tierItems.Count)];
    }

    private bool PoolHasTier(List<ItemDefinition> pool, ItemTier tier)
    {
        if (pool == null)
            return false;

        for (int i = 0; i < pool.Count; i++)
        {
            ItemDefinition item = pool[i];
            if (item != null && item.itemTier == tier)
                return true;
        }

        return false;
    }

    private string BuildTreasureEventBody(WorldTileData tile, WorldTreasureResult treasure)
    {
        StringBuilder sb = new StringBuilder();
        if (settings != null)
            sb.Append(settings.GetEventDescription(tile.eventType));

        if (!string.IsNullOrWhiteSpace(treasureSuffix))
            sb.Append(treasureSuffix);

        sb.Append("\n\n");
        sb.Append(string.IsNullOrWhiteSpace(treasureRewardHeaderText) ? "획득 예정 보상" : treasureRewardHeaderText);
        sb.Append("\n");
        AppendTreasureRewardLines(sb, treasure);

        return sb.ToString();
    }

    private void AppendTreasureRewardLines(StringBuilder sb, WorldTreasureResult treasure)
    {
        if (treasureCandidateItems == null || treasureCandidateItems.Count == 0)
        {
            sb.Append(string.IsNullOrWhiteSpace(treasureEmptyText) ? "보물 후보 아이템이 없습니다." : treasureEmptyText);
            return;
        }

        if (treasure == null || !treasure.HasAnyReward)
        {
            sb.Append(string.IsNullOrWhiteSpace(treasureNoRewardText) ? "획득 가능한 보상이 없습니다." : treasureNoRewardText);
            return;
        }

        for (int i = 0; i < treasure.rewards.Count; i++)
        {
            WorldTreasureRewardItemEntry reward = treasure.rewards[i];
            if (reward == null || reward.item == null)
                continue;

            sb.Append("- ");
            sb.Append(GetItemTierLabel(reward.item.itemTier));
            sb.Append(" ");
            sb.Append(reward.GetDisplayName());
            sb.Append(" x");
            sb.Append(Mathf.Max(1, reward.amount));

            if (i < treasure.rewards.Count - 1)
                sb.Append("\n");
        }
    }

    private string GetItemTierLabel(ItemTier tier)
    {
        switch (tier)
        {
            case ItemTier.Tier3:
                return "[3티어]";
            case ItemTier.Tier2:
                return "[2티어]";
            case ItemTier.Tier1:
            default:
                return "[1티어]";
        }
    }

    private bool TryOpenSimpleEvent(WorldTileData tile)
    {
        if (eventPopupUI == null)
        {
            Debug.LogWarning("[WorldEventController] WorldEventPopupUI reference is missing.");
            return false;
        }

        popupOpen = true;
        string title = settings != null ? settings.GetEventDisplayName(tile.eventType) : tile.eventType.ToString();
        string body = BuildEventBody(tile);

        eventPopupUI.Open(title, body, defaultConfirmText, () => ConfirmSimpleEvent(tile), () => popupOpen = false);
        return true;
    }

    private void ConfirmSimpleEvent(WorldTileData tile)
    {
        popupOpen = false;
        ApplyImmediateEventEffects(tile);

        bool isReusable = tile != null && tile.IsReusableEvent;
        bool disableIcon = !isReusable;
        bool markResolved = !isReusable;

        if (runManager != null)
            runManager.ResolveMapEvent(tile, true, markResolved, disableIcon);
    }

    private void ConfirmRestEvent(WorldTileData tile)
    {
        popupOpen = false;

        if (runManager != null)
        {
            runManager.ApplyRestToActiveParty(
                restHealMode,
                restHealPercentOfMaxHp,
                restFlatHealAmount,
                restCanReviveDeadUnits);
        }
        else
        {
            RestorePartyToFullFallback();
        }

        bool isReusable = tile != null && tile.IsReusableEvent;
        bool disableIcon = !isReusable;
        bool markResolved = !isReusable;

        if (runManager != null)
            runManager.ResolveMapEvent(tile, true, markResolved, disableIcon);
    }

    private void ApplyImmediateEventEffects(WorldTileData tile)
    {
        if (tile == null)
            return;

        // Rest는 TryOpenRestEvent/ConfirmRestEvent에서 별도로 처리된다.
    }

    private void RestorePartyToFullFallback()
    {
        BattlePartyRuntimeState partyState = null;
        if (runManager != null)
            partyState = runManager.GetOrCreatePlayerPartyRuntimeState();
        if (partyState == null && battleManager != null)
            partyState = battleManager.AllyRuntimePartyState;
        partyState?.ResetPersistentHPToFull();
    }

    private string BuildEventBody(WorldTileData tile)
    {
        StringBuilder sb = new StringBuilder();
        if (settings != null)
            sb.Append(settings.GetEventDescription(tile.eventType));

        switch (tile.eventType)
        {
            case WorldTileEventType.Treasure:
                sb.Append(treasureSuffix);
                break;

            case WorldTileEventType.Merchant:
                sb.Append(merchantSuffix);
                break;

            case WorldTileEventType.Graveyard:
                sb.Append(graveyardSuffix);
                break;
        }

        return sb.ToString();
    }

    private string BuildRestEventBody(WorldTileData tile)
    {
        StringBuilder sb = new StringBuilder();
        if (settings != null)
            sb.Append(settings.GetEventDescription(tile.eventType));

        if (!string.IsNullOrWhiteSpace(restDescriptionSuffix))
            sb.Append(restDescriptionSuffix);

        sb.Append("\n\n");
        sb.Append(string.IsNullOrWhiteSpace(restEffectHeaderText) ? "휴식 효과" : restEffectHeaderText);
        sb.Append(": ");
        sb.Append(GetRestEffectDescription());

        WorldRestResult preview = runManager != null
            ? runManager.PreviewRestForActiveParty(restHealMode, restHealPercentOfMaxHp, restFlatHealAmount, restCanReviveDeadUnits)
            : null;

        sb.Append("\n\n");
        sb.Append(string.IsNullOrWhiteSpace(restPartyPreviewHeaderText) ? "파티 상태" : restPartyPreviewHeaderText);
        sb.Append("\n");
        AppendRestPreviewLines(sb, preview);

        return sb.ToString();
    }

    private string GetRestEffectDescription()
    {
        float percent = Mathf.Max(0f, restHealPercentOfMaxHp);
        int flat = Mathf.Max(0, restFlatHealAmount);

        switch (restHealMode)
        {
            case WorldRestHealMode.FullHeal:
                return restCanReviveDeadUnits
                    ? "파티원의 체력을 전부 회복"
                    : "생존 파티원의 체력을 전부 회복";

            case WorldRestHealMode.FlatAmount:
                return restCanReviveDeadUnits
                    ? $"파티원의 체력을 {flat} 회복"
                    : $"생존 파티원의 체력을 {flat} 회복";

            case WorldRestHealMode.FlatAndPercentOfMaxHp:
                return restCanReviveDeadUnits
                    ? $"파티원의 체력을 최대 체력의 {percent:0.#}% + {flat} 회복"
                    : $"생존 파티원의 체력을 최대 체력의 {percent:0.#}% + {flat} 회복";

            case WorldRestHealMode.PercentOfMaxHp:
            default:
                return restCanReviveDeadUnits
                    ? $"파티원의 체력을 최대 체력의 {percent:0.#}% 회복"
                    : $"생존 파티원의 체력을 최대 체력의 {percent:0.#}% 회복";
        }
    }

    private void AppendRestPreviewLines(StringBuilder sb, WorldRestResult preview)
    {
        if (preview == null || !preview.HasParty)
        {
            sb.Append(string.IsNullOrWhiteSpace(restNoPartyText) ? "휴식할 파티원이 없습니다." : restNoPartyText);
            return;
        }

        for (int i = 0; i < preview.members.Count; i++)
        {
            WorldRestMemberResult member = preview.members[i];
            if (member == null)
                continue;

            sb.Append("- ");
            sb.Append(string.IsNullOrWhiteSpace(member.displayName) ? "Unit" : member.displayName);
            sb.Append(": ");

            if (member.skipped && member.wasDead)
            {
                sb.Append(string.IsNullOrWhiteSpace(restDeadUnitText) ? "사망 - 휴식 불가" : restDeadUnitText);
            }
            else
            {
                sb.Append(member.beforeHP);
                sb.Append("/");
                sb.Append(member.maxHP);
                sb.Append(" → ");
                sb.Append(member.afterHP);
                sb.Append("/");
                sb.Append(member.maxHP);

                if (member.healedAmount > 0)
                {
                    sb.Append(" (+");
                    sb.Append(member.healedAmount);
                    sb.Append(")");
                }
            }

            if (i < preview.members.Count - 1)
                sb.Append("\n");
        }
    }

    private void OpenFallbackPopup(WorldTileData tile, string body, System.Action onConfirm)
    {
        if (eventPopupUI == null)
        {
            Debug.LogWarning("[WorldEventController] Fallback popup could not open because WorldEventPopupUI is missing.");
            onConfirm?.Invoke();
            return;
        }

        popupOpen = true;
        string title = settings != null && tile != null ? settings.GetEventDisplayName(tile.eventType) : "Event";
        eventPopupUI.Open(title, body, defaultConfirmText, onConfirm, () => popupOpen = false);
    }
}
