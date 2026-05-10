using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UIButtonSoundEmitter : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private Button button;
    private bool inside;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (inside)
            return;

        inside = true;
        if (IsPlayable())
            GameAudioManager.PlayButtonHover();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        inside = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (IsPlayable())
            GameAudioManager.PlayButtonClick();
    }

    private bool IsPlayable()
    {
        if (button == null)
            button = GetComponent<Button>();
        return button == null || button.interactable;
    }

    public static void BindAllButtonsInScene()
    {
        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button b = buttons[i];
            if (b == null)
                continue;
            if (b.GetComponent<UIButtonSoundEmitter>() == null)
                b.gameObject.AddComponent<UIButtonSoundEmitter>();
        }
    }
}
