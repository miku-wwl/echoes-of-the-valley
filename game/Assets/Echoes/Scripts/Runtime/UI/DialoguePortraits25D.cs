using UnityEngine;
using UnityEngine.UI;

namespace Echoes
{
    /// <summary>
    /// Phase 5.2-B3 dialogue portraits: while a conversation is open the caretaker's bust stands at the left of the dialogue box (she is the
    /// listener) and the villager who speaks appears at the right. Places and objects ("The Ruined Spring") show only the caretaker.
    /// </summary>
    public sealed class DialoguePortraits25D : MonoBehaviour
    {
        [SerializeField] DialogueBox dialogue;
        [SerializeField] Image caretaker;
        [SerializeField] Image speaker;
        [SerializeField] Sprite mira;
        [SerializeField] Sprite wren;
        [SerializeField] Sprite alder;

        public bool CaretakerShown => caretaker != null && caretaker.gameObject.activeSelf;
        public Sprite SpeakerShown => speaker != null && speaker.gameObject.activeSelf ? speaker.sprite : null;

        void LateUpdate()
        {
            bool open = dialogue != null && dialogue.IsOpen;
            if (caretaker != null && caretaker.gameObject.activeSelf != open) caretaker.gameObject.SetActive(open);
            Sprite who = open ? For(dialogue.Speaker) : null;
            if (speaker != null)
            {
                if (speaker.sprite != who && who != null)
                {
                    speaker.sprite = who;
                    // the busts differ in shape: keep the height and the bottom edge, fit the width to the new picture
                    RectTransform rect = speaker.rectTransform;
                    rect.sizeDelta = new Vector2(rect.sizeDelta.y * who.rect.width / who.rect.height, rect.sizeDelta.y);
                }
                if (speaker.gameObject.activeSelf != (who != null)) speaker.gameObject.SetActive(who != null);
            }
        }

        Sprite For(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (name.StartsWith("Mira")) return mira;
            if (name.StartsWith("Wren")) return wren;
            if (name.StartsWith("Alder")) return alder;
            return null;
        }
    }
}
