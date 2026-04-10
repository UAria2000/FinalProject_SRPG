using UnityEngine;
using UnityEngine.EventSystems;

public class BattleBackgroundClickCatcherUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private BattleInputController inputController;

    private void Awake()
    {
        if (inputController == null)
            inputController = Object.FindFirstObjectByType<BattleInputController>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (inputController == null)
            return;

        if (eventData.button == PointerEventData.InputButton.Left)
            inputController.OnBattlefieldBackgroundLeftClicked();
    }
}
