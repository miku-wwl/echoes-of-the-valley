using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// Wren, the Garden Keeper: gives "Restore the Garden" once the stream is restored and takes one of Clover's eggs.
    /// The quest only completes through GameSession.TryDeliverEgg. An egg already in the satchel counts, however it got there.
    /// </summary>
    public class GardenKeeperInteractable : DialogueInteractable
    {
        [SerializeField, TextArea] string[] sleepingLines =
        {
            "Shh... the garden is asleep. Nothing here will stir without clear water.",
            "Wake the stream first, then come and find me. Mira will know how."
        };

        [SerializeField, TextArea] string[] offerLines =
        {
            "Do you hear that? The stream is running! My roots have been waiting for this.",
            "But a garden needs more than water. It needs something living to answer it: a fresh egg from Clover.",
            "Mix Chicken Feed from a Dawn Blossom at the workshop, feed her, and bring me the egg."
        };

        [SerializeField, TextArea] string[] offerWithEggLines =
        {
            "Do you hear that? The stream is running! My roots have been waiting for this.",
            "But a garden needs something living to answer it: a fresh egg from Clover.",
            "Wait... is that one of her eggs in your satchel already? Speak to me again and I will gladly take it."
        };

        [SerializeField, TextArea] string[] deliveredLines =
        {
            "A fresh egg, still warm. Thank you.",
            "I will tuck it under the old rose... and now, listen.",
            "Oh! They are waking. Look at the beds!"
        };

        [SerializeField, TextArea] string[] gardenLines =
        {
            "Look at it! Every bed is singing.",
            "The windmill on the north hill is next. Alder has been fretting over its beams all season."
        };

        [SerializeField, TextArea] string[] restoredLines =
        {
            "The sails are turning! I can feel the whole garden swaying along with them.",
            "Clover is laying like never before. It seems everything wants to sing."
        };

        public override string Prompt
        {
            get
            {
                GameSession session = GameSession.Current;
                if (session != null && session.Resonance == ResonanceStage.AwakeningStream
                    && session.GardenQuest == QuestStage.Active && session.Inventory.Has(ItemIds.Egg, 1))
                    return "Give Clover's egg to Wren";
                return base.Prompt;
            }
        }

        public override void Interact(InteractionDetector interactor)
        {
            GameSession session = GameSession.Current;
            if (session == null)
            {
                Dialogue.Open(Speaker, sleepingLines);
                return;
            }

            switch (session.Resonance)
            {
                case ResonanceStage.SilentValley:
                    Dialogue.Open(Speaker, sleepingLines);
                    return;

                case ResonanceStage.AwakeningStream:
                    if (session.GardenQuest == QuestStage.NotStarted)
                    {
                        Dialogue.Open(Speaker, session.Inventory.Has(ItemIds.Egg, 1) ? offerWithEggLines : offerLines,
                            () => session.TryStartGardenQuest());
                        return;
                    }

                    // The quest is active: hand over the egg if there is one, otherwise explain what is missing.
                    Outcome result = session.TryDeliverEgg();
                    if (result == Outcome.Ok) Dialogue.Open(Speaker, deliveredLines);
                    else Dialogue.Open(Speaker, Reminder(session));
                    return;

                case ResonanceStage.BloomingGarden:
                    Dialogue.Open(Speaker, gardenLines);
                    return;

                default:
                    Dialogue.Open(Speaker, restoredLines);
                    return;
            }
        }

        /// <summary>What Wren says when the quest is active and there is no egg yet: the next step, from the real hen and satchel state.</summary>
        public static string[] Reminder(GameSession session)
        {
            string how;
            switch (session.Chicken)
            {
                case ChickenPhase.EggReady:
                    how = "Her egg is ready in the nest. Collect it from Clover's yard.";
                    break;
                case ChickenPhase.Producing:
                    how = "She is laying one right now. Give her a moment.";
                    break;
                default:
                    how = session.Inventory.Has(ItemIds.ChickenFeed, 1)
                        ? "Feed Clover in her yard, then wait for the egg."
                        : session.Inventory.Has(ItemIds.Flower, 1)
                            ? "Mix Chicken Feed at the workshop (one Dawn Blossom makes three portions), then feed Clover."
                            : "Gather a Dawn Blossom from the meadows, mix Chicken Feed at the workshop, then feed Clover.";
                    break;
            }
            return new[] { "I need a fresh egg from Clover. " + how };
        }
    }
}
