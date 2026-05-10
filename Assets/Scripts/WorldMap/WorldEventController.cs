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
    [SerializeField] private string treasureConfirmText = "확인";
    [SerializeField] private string treasureRewardHeaderText = "획득 예정 보상";
    [SerializeField] private string treasureEmptyText = "보물 후보 아이템이 없습니다. Treasure Candidate Items를 설정해 주세요.";
    [SerializeField] private string treasureNoRewardText = "획득 가능한 아이템이 없습니다.";
    [Tooltip("{0} 자리에 지급 소울량이 들어갑니다. 예: {0} 소울")]
    [SerializeField] private string treasureSoulTextFormat = "{0} 소울";
    [SerializeField] private List<ItemDefinition> treasureCandidateItems = new List<ItemDefinition>();
    [SerializeField] private List<WorldTreasureTierWeight> treasureTierWeights = new List<WorldTreasureTierWeight>
    {
        new WorldTreasureTierWeight { tier = ItemTier.Tier1, weight = 70f },
        new WorldTreasureTierWeight { tier = ItemTier.Tier2, weight = 25f },
        new WorldTreasureTierWeight { tier = ItemTier.Tier3, weight = 5f },
    };
    [Tooltip("0~4개 아이템 드랍 개수를 뽑는 가중치입니다. 합이 100일 필요는 없습니다.")]
    [SerializeField] private List<WorldTreasureDropCountWeight> treasureDropCountWeights = new List<WorldTreasureDropCountWeight>
    {
        new WorldTreasureDropCountWeight { dropCount = 0, weight = 10f },
        new WorldTreasureDropCountWeight { dropCount = 1, weight = 35f },
        new WorldTreasureDropCountWeight { dropCount = 2, weight = 30f },
        new WorldTreasureDropCountWeight { dropCount = 3, weight = 20f },
        new WorldTreasureDropCountWeight { dropCount = 4, weight = 5f },
    };
    [SerializeField] private Vector2Int treasureConsumableAmountRange = new Vector2Int(1, 3);

    [Header("Treasure Soul Reward")]
    [SerializeField] private Vector2Int treasureBaseSoulRange = new Vector2Int(80, 150);
    [Min(0)] [SerializeField] private int treasureSoulSizePercentSmall = 80;
    [Min(0)] [SerializeField] private int treasureSoulSizePercentMedium = 100;
    [Min(0)] [SerializeField] private int treasureSoulSizePercentLarge = 130;
    [Min(0)] [SerializeField] private int treasureSoulDifficultyPercentEasy = 80;
    [Min(0)] [SerializeField] private int treasureSoulDifficultyPercentNormal = 100;
    [Min(0)] [SerializeField] private int treasureSoulDifficultyPercentHard = 130;

    [Header("Graveyard Event")]
    [SerializeField] private GraveyardPopupUI graveyardPopupUI;

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
        if (graveyardPopupUI == null)
            graveyardPopupUI = Object.FindFirstObjectByType<GraveyardPopupUI>(FindObjectsInactive.Include);

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

        if (tile.eventType == WorldTileEventType.Graveyard)
            return TryOpenGraveyardEvent(tile);

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


    private bool TryOpenGraveyardEvent(WorldTileData tile)
    {
        if (graveyardPopupUI == null)
        {
            Debug.LogWarning("[WorldEventController] GraveyardPopupUI reference is missing.");
            return TryOpenSimpleEvent(tile);
        }

        popupOpen = true;
        string title = settings != null ? settings.GetEventDisplayName(tile.eventType) : "묘지";
        string description = settings != null ? settings.GetEventDescription(tile.eventType) : string.Empty;

        if (runManager != null)
            runManager.ResolveMapEvent(tile, true, true, false);

        graveyardPopupUI.Open(title, description, () => popupOpen = false);
        return true;
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

        // 보물창이 열린 뒤 강제 종료되더라도 같은 보물 타일에서 보상을 반복 획득하지 못하도록
        // 보물 이벤트는 창을 여는 시점에 타일을 먼저 확정한다. 미수령 슬롯은 기존처럼 창을 닫으면 폐기된다.
        ResolveTreasureTileOnOpen(tile);
        GrantTreasureSoulImmediately(treasure);
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

    private void ResolveTreasureTileOnOpen(WorldTileData tile)
    {
        if (tile == null || runManager == null)
            return;

        bool isReusable = tile.IsReusableEvent;
        bool disableIcon = !isReusable;
        bool markResolved = !isReusable;

        if (tile.IsPlayerOwned && tile.isResolved == markResolved && tile.isIconDisabled == disableIcon)
            return;

        runManager.ResolveMapEvent(tile, true, markResolved, disableIcon);
    }

    private void ConfirmTreasureEvent(WorldTileData tile)
    {
        popupOpen = false;

        // 보물 보상은 WorldEventPopupUI의 슬롯 상호작용에서 지급된다.
        // 슬롯을 연결하지 않은 구형 UI에서는 WorldEventPopupUI가 확인 시 자동 지급한다.
        // 타일 점령/해결은 TryOpenTreasureEvent()에서 이미 확정한다.

        if (tile != null)
            pendingTreasureByTileId.Remove(tile.tileId);
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
        result.soulAmount = RollTreasureSoulAmount();

        List<ItemDefinition> pool = BuildTreasureCandidateList();
        int dropCount = Mathf.Clamp(RollTreasureDropCount(), 0, 4);

        for (int i = 0; i < dropCount; i++)
        {
            ItemDefinition selected = PickWeightedTreasureItem(pool);
            if (selected == null)
                break;

            int amount = selected.mainUICategory == MainUIItemCategory.Consumable
                ? RollTreasureConsumableAmount()
                : 1;
            result.Add(selected, amount);
        }

        return result;
    }

    private int RollTreasureDropCount()
    {
        if (treasureDropCountWeights == null || treasureDropCountWeights.Count == 0)
            return 0;

        float total = 0f;
        for (int i = 0; i < treasureDropCountWeights.Count; i++)
        {
            WorldTreasureDropCountWeight weight = treasureDropCountWeights[i];
            if (weight != null && weight.weight > 0f)
                total += weight.weight;
        }

        if (total <= 0f)
            return 0;

        float roll = Random.value * total;
        float cursor = 0f;
        int fallback = 0;
        for (int i = 0; i < treasureDropCountWeights.Count; i++)
        {
            WorldTreasureDropCountWeight weight = treasureDropCountWeights[i];
            if (weight == null || weight.weight <= 0f)
                continue;

            fallback = Mathf.Clamp(weight.dropCount, 0, 4);
            cursor += weight.weight;
            if (roll <= cursor)
                return fallback;
        }

        return fallback;
    }

    private int RollTreasureSoulAmount()
    {
        int min = Mathf.Max(0, Mathf.Min(treasureBaseSoulRange.x, treasureBaseSoulRange.y));
        int max = Mathf.Max(min, Mathf.Max(treasureBaseSoulRange.x, treasureBaseSoulRange.y));
        int baseAmount = Random.Range(min, max + 1);
        int totalPercent = 100 + (GetTreasureSoulSizePercent() - 100) + (GetTreasureSoulDifficultyPercent() - 100);
        totalPercent = Mathf.Max(0, totalPercent);
        return Mathf.Max(0, Mathf.RoundToInt(baseAmount * (totalPercent * 0.01f)));
    }

    private int GetTreasureSoulSizePercent()
    {
        int radius = settings != null ? settings.radius : 4;
        if (radius <= 4)
            return Mathf.Max(0, treasureSoulSizePercentSmall);
        if (radius == 5)
            return Mathf.Max(0, treasureSoulSizePercentMedium);
        return Mathf.Max(0, treasureSoulSizePercentLarge);
    }

    private int GetTreasureSoulDifficultyPercent()
    {
        WorldDifficulty difficulty = settings != null ? settings.difficulty : WorldDifficulty.Normal;
        switch (difficulty)
        {
            case WorldDifficulty.Easy: return Mathf.Max(0, treasureSoulDifficultyPercentEasy);
            case WorldDifficulty.Hard: return Mathf.Max(0, treasureSoulDifficultyPercentHard);
            default: return Mathf.Max(0, treasureSoulDifficultyPercentNormal);
        }
    }

    private void GrantTreasureSoulImmediately(WorldTreasureResult treasure)
    {
        if (treasure == null || treasure.soulGranted || treasure.soulAmount <= 0 || runManager == null)
            return;

        runManager.AddWorldSoul(treasure.soulAmount);
        treasure.soulGranted = true;
    }

    private string FormatTreasureSoulText(int amount)
    {
        string format = string.IsNullOrWhiteSpace(treasureSoulTextFormat) ? "{0} 소울" : treasureSoulTextFormat;
        try
        {
            return string.Format(format, Mathf.Max(0, amount).ToString("N0"));
        }
        catch
        {
            return $"{Mathf.Max(0, amount):N0} 소울";
        }
    }

    private List<ItemDefinition> BuildTreasureCandidateList()
    {
        List<ItemDefinition> result = new List<ItemDefinition>();
        if (treasureCandidateItems == null)
            return result;

        for (int i = 0; i < treasureCandidateItems.Count; i++)
        {
            ItemDefinition item = treasureCandidateItems[i];
            if (item == null)
                continue;

            result.Add(item);
        }

        return result;
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
            if (treasure != null && treasure.soulAmount > 0)
            {
                sb.Append(FormatTreasureSoulText(treasure.soulAmount));
                return;
            }

            sb.Append(string.IsNullOrWhiteSpace(treasureEmptyText) ? "보물 후보 아이템이 없습니다." : treasureEmptyText);
            return;
        }

        if (treasure != null && treasure.soulAmount > 0)
        {
            sb.Append(FormatTreasureSoulText(treasure.soulAmount));
            if (treasure.rewards != null && treasure.rewards.Count > 0)
                sb.Append("\n");
        }

        if (treasure == null || treasure.rewards == null || treasure.rewards.Count == 0)
        {
            if (treasure == null || treasure.soulAmount <= 0)
                sb.Append(string.IsNullOrWhiteSpace(treasureNoRewardText) ? "획득 가능한 아이템이 없습니다." : treasureNoRewardText);
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
