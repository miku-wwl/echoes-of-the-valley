using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Echoes
{
    /// <summary>
    /// Makes a button feel alive: it swells a little when the mouse is over it or the keyboard has selected it, ticks softly,
    /// and clicks. The click is heard for any onClick, so keyboard and script activations sound the same as a mouse click.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UiButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] float hoverScale = 1.05f;

        Button button;
        bool hovered;
        bool selected;
        float scale = 1f;

        public float CurrentScale => scale;
        public bool Highlighted => hovered || selected;

        void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(() => AudioDirector.PlayStatic(SoundCue.UiClick));
        }

        void OnDisable()
        {
            hovered = false;
            selected = false;
            scale = 1f;
            transform.localScale = Vector3.one;
        }

        void Update()
        {
            float goal = button != null && button.interactable && Highlighted ? hoverScale : 1f;
            scale = Mathf.MoveTowards(scale, goal, Time.unscaledDeltaTime * 0.6f);
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = true;
            if (button != null && button.interactable) AudioDirector.PlayStatic(SoundCue.UiHover);
        }

        public void OnPointerExit(PointerEventData eventData) => hovered = false;

        public void OnSelect(BaseEventData eventData)
        {
            selected = true;
            if (button != null && button.interactable) AudioDirector.PlayStatic(SoundCue.UiHover);
        }

        public void OnDeselect(BaseEventData eventData) => selected = false;
    }
}
