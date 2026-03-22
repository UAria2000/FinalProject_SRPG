using UnityEngine;
using UnityEngine.EventSystems;

public class EnemySkillButtonHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [SerializeField] private int skillSlotIndex = 0;

    private BattleManager battleManager;
    private bool isHovering = false;

    public void Initialize(BattleManager manager, int slotIndex)
    {
        battleManager = manager;
        skillSlotIndex = slotIndex;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;

        if (battleManager != null)
            battleManager.ShowEnemySkillTooltip(skillSlotIndex, eventData.position);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (!isHovering)
            return;

        if (battleManager != null)
            battleManager.MoveEnemySkillTooltip(eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;

        if (battleManager != null)
            battleManager.HideEnemySkillTooltip();
    }
}