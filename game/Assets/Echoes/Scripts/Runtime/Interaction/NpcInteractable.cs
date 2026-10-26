using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// Mira, the villager. Before the opening objective is finished she guides it (Phase 1). Afterwards she gives and
    /// follows up the "Restore the Stream" quest, points to the next two quests as they unlock, and, once the windmill
    /// turns, holds the closing conversation that starts the ending. She reads the real GameSession state throughout.
    /// </summary>
    public class NpcInteractable : DialogueInteractable
    {
        [SerializeField] EndingPanel endingPanel;

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
            "Wren says the village garden is stirring too. Look for her south-east of the Heart, beside Clover's yard. She may need your help."
        };

        [SerializeField, TextArea] string[] windmillHintLines =
        {
            "The garden is glorious! I could hear it blooming from here.",
            "Alder is at the old windmill on the north hill. The beams have been waiting for someone with Echo Ore."
        };

        [SerializeField, TextArea] string[] closingLines =
        {
            "You can feel it, can't you? The stream, the garden... even the old windmill.",
            "When you arrived, this valley had forgotten how to sing.",
            "You listened. And now it sings back.",
            "Come, stand here with me a moment. Let us hear it together."
        };

        [SerializeField, TextArea] string[] afterEndingLines =
        {
            "Hear that? The whole valley is humming.",
            "Stay as long as you like. The meadows will grow back, and Clover is always glad of company."
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

            // Once the windmill turns she holds the closing conversation, once; afterwards she is simply content.
            if (session.Resonance == ResonanceStage.RestoredValley)
            {
                if (session.EndingSeen) Dialogue.Open(Speaker, afterEndingLines);
                else Dialogue.Open(Speaker, closingLines, () => BeginEnding(session));
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
                    Dialogue.Open(Speaker, session.Resonance == ResonanceStage.BloomingGarden ? windmillHintLines : thanksLines);
                    break;
            }
        }

        /// <summary>Records the ending as seen (and saves) and then shows it. Without a wired screen nothing is recorded.</summary>
        void BeginEnding(GameSession session)
        {
            if (endingPanel == null) return;
            if (session.TryCompleteEnding() == Outcome.Ok) endingPanel.Show();
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
