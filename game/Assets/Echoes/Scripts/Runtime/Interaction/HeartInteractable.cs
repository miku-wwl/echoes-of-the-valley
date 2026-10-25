using UnityEngine;

namespace Echoes
{
    /// <summary>The ruined central landmark. Touching it at the end of the objective completes it.</summary>
    public class HeartInteractable : DialogueInteractable
    {
        [SerializeField, TextArea] string[] tooEarlyLines =
        {
            "The Heart of the Valley: a ring of broken pillars around a dull, dormant crystal. It feels like it is listening, but it is not ready yet.",
            "Perhaps someone in the cottage knows its story."
        };

        [SerializeField, TextArea] string[] touchLines =
        {
            "You rest a hand on the old stone. For a moment, everything holds its breath...",
            "Then the crystal wakes. A soft note rings out across the valley, and warm light spreads over the hills."
        };

        [SerializeField, TextArea] string[] afterLines =
        {
            "The crystal glows softly. Mira may know how to wake the stream."
        };

        public override void Interact(InteractionDetector interactor)
        {
            switch (Objective.Step)
            {
                case ObjectiveStep.ReturnToHeart:
                    Dialogue.Open(Speaker, touchLines, Objective.NotifyHeartReached);
                    break;
                case ObjectiveStep.Complete:
                    Dialogue.Open(Speaker, afterLines);
                    break;
                default:
                    Dialogue.Open(Speaker, tooEarlyLines);
                    break;
            }
        }
    }
}
