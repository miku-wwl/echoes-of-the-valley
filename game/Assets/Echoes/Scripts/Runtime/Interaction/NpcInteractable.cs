using UnityEngine;

namespace Echoes
{
    /// <summary>The friendly villager. Talking to her the first time advances the objective; later visits give hints.</summary>
    public class NpcInteractable : DialogueInteractable
    {
        [SerializeField, TextArea] string[] introLines =
        {
            "Oh! A visitor... and just in time, too.",
            "This valley used to hum with life. Then the old spring cracked, and the song went out of everything.",
            "Would you visit the ruined spring beside the pond? See what is left of it, then come back to the Heart of the Valley."
        };

        [SerializeField, TextArea] string[] springHintLines =
        {
            "The spring is past the pond, west of the old plaza. Take your time - the valley isn't going anywhere."
        };

        [SerializeField, TextArea] string[] heartHintLines =
        {
            "You have seen the spring? Then go back to the Heart, the ring of old stones in the middle of the valley. I think it will answer you."
        };

        [SerializeField, TextArea] string[] completedLines =
        {
            "Listen... can you hear it? The valley is stirring again.",
            "There is so much more to mend. Come and find me once you are ready."
        };

        public override void Interact(InteractionDetector interactor)
        {
            switch (Objective.Step)
            {
                case ObjectiveStep.MeetVillager:
                    Dialogue.Open(Speaker, introLines, Objective.NotifyVillagerMet);
                    break;
                case ObjectiveStep.InspectSpring:
                    Dialogue.Open(Speaker, springHintLines);
                    break;
                case ObjectiveStep.ReturnToHeart:
                    Dialogue.Open(Speaker, heartHintLines);
                    break;
                default:
                    Dialogue.Open(Speaker, completedLines);
                    break;
            }
        }
    }
}
