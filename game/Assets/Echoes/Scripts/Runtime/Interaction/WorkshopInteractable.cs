using UnityEngine;

namespace Echoes
{
    /// <summary>The alchemy workshop. Phase 1 only explains that alchemy is not available yet.</summary>
    public class WorkshopInteractable : DialogueInteractable
    {
        [SerializeField, TextArea] string[] lines =
        {
            "A cosy workshop crowded with cauldrons, jars and bundles of drying herbs.",
            "The alchemy table is cold and dusty. Alchemy will be unlocked in a future phase - come back once the valley has begun to recover."
        };

        public override void Interact(InteractionDetector interactor)
        {
            Dialogue.Open(Speaker, lines);
        }
    }
}
