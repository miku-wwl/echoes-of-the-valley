using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// Mira, the villager. Before the opening objective is finished she guides it (Phase 1). Afterwards she gives and
    /// follows up the "Restore the Stream" quest, reading the real GameSession state.
    /// </summary>
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

        [SerializeField, TextArea] string[] questOfferLines =
        {
            "You woke the Heart! Now the stream: ever since the spring cracked it has run thin and murky.",
            "I can brew a Spring Tonic to wake it. Gather two Moonleaf Herbs and a Dawn Blossom from the meadows, then mix them at the alchemy workshop, my cottage with the cauldron.",
            "Pour the tonic into the ruined spring. Everything grows back here, so pick freely. And if you have a spare blossom, Clover the hen would love some feed."
        };

        [SerializeField, TextArea] string[] tonicReadyLines =
        {
            "That is a Spring Tonic! Take it to the ruined spring by the pond and pour it in. Let's hear the valley sing."
        };

        [SerializeField, TextArea] string[] thanksLines =
        {
            "The stream is running clear again, I can hear it from my door. Thank you.",
            "Clover is laying well too, if you feed her. There is more to restore, but that is for another day."
        };

        public override void Interact(InteractionDetector interactor)
        {
            switch (Objective.Step)
            {
                case ObjectiveStep.MeetVillager:
                    Dialogue.Open(Speaker, introLines, Objective.NotifyVillagerMet);
                    return;
                case ObjectiveStep.InspectSpring:
                    Dialogue.Open(Speaker, springHintLines);
                    return;
                case ObjectiveStep.ReturnToHeart:
                    Dialogue.Open(Speaker, heartHintLines);
                    return;
            }

            GameSession session = GameSession.Current;
            if (session == null)
            {
                Dialogue.Open(Speaker, completedLines);
                return;
            }

            switch (session.StreamQuest)
            {
                case StreamQuestStage.NotStarted:
                    Dialogue.Open(Speaker, questOfferLines, () => session.TryStartStreamQuest());
                    break;
                case StreamQuestStage.Active:
                    Dialogue.Open(Speaker, session.Inventory.Has(ItemIds.SpringTonic, 1) ? tonicReadyLines : Reminder(session));
                    break;
                default:
                    Dialogue.Open(Speaker, thanksLines);
                    break;
            }
        }

        static string[] Reminder(GameSession session)
        {
            int herbs = session.Inventory.Count(ItemIds.Herb);
            int flowers = session.Inventory.Count(ItemIds.Flower);
            return new[]
            {
                "For the Spring Tonic you need 2 Moonleaf Herb (you have " + herbs + ") and 1 Dawn Blossom (you have " + flowers + "). "
                + "Mix it at the workshop, then pour it into the ruined spring."
            };
        }
    }
}
