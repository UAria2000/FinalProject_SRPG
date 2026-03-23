using UnityEngine;
using UnityEngine.EventSystems;

public class EnemySkillButtonHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private BattleManager battleManager;
    private int slotIndex;

    public void Initialize(BattleManager manager, int index)
    {
        battleManager = manager;
        slotIndex = index;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (battleManager != null)
            battleManager.OnEnemySkillHoverEnter(slotIndex, Input.mousePosition);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (battleManager != null)
            battleManager.OnEnemySkillHoverExit();
    }
}
