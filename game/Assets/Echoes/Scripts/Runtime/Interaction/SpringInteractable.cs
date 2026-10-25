using UnityEngine;

namespace Echoes
{
    /// <summary>The ruined spring. Always shows its lore; examining it after meeting the villager advances the objective.</summary>
    public class SpringInteractable : DialogueInteractable
    {
        [SerializeField, TextArea] string[] lines =
        {
            "A ring of cracked stones. The basin is dry and silted, and only a thin trickle still creeps into the stream.",
            "Once, clear water rose here and carried its song through the whole valley. Now the valley is quiet.",
            "A faint warmth hums beneath the stones, as if something is waiting to be heard."
        };

        public override void Interact(InteractionDetector interactor)
        {
            Dialogue.Open(Speaker, lines, Objective.NotifySpringInspected);
        }
    }
}
