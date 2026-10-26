using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// Alder, the Windmill Restorer: gives "Restore the Windmill" once the garden blooms and takes one set of Windmill Braces.
    /// The quest only completes through GameSession.TryDeliverWindmillBraces. Braces crafted earlier count.
    /// </summary>
    public class WindmillRestorerInteractable : DialogueInteractable
    {
        [SerializeField, TextArea] string[] lockedLines =
        {
            "Mind the loose beams. The windmill has stood silent since the storm.",
            "I cannot mend it alone, and not before the garden wakes. Things in this valley come back in order."
        };

        [SerializeField, TextArea] string[] offerLines =
        {
            "You woke the garden! Then perhaps you can help me with the windmill.",
            "Its supports have snapped. They need braces made of Echo Ore, the pale stone that rings like a bell. Four pieces will do.",
            "Look along the valley's edge, east and west. Mix the ore into Windmill Braces at Mira's workshop, then bring them to me."
        };

        [SerializeField, TextArea] string[] offerWithBracesLines =
        {
            "You woke the garden! Then perhaps you can help me with the windmill.",
            "Its supports have snapped, and they need braces of Echo Ore.",
            "Is that a set of Windmill Braces you are carrying already? Speak to me again and I will fit them."
        };

        [SerializeField, TextArea] string[] deliveredLines =
        {
            "These are perfect. Hold the beam steady...",
            "There. Now stand back and listen.",
            "The sails are moving! After all these seasons, the windmill is turning.",
            "Go and find Mira by the cottage. She has waited a long time to hear this."
        };

        [SerializeField, TextArea] string[] afterLines =
        {
            "Listen to her turn. It sounds as if the whole valley is humming.",
            "Mira was asking after you. She is by the workshop cottage."
        };

        [SerializeField, TextArea] string[] afterEndingLines =
        {
            "Listen to her turn. It sounds as if the whole valley is humming.",
            "Take as long as you like. The ore will grow back, and so will everything else."
        };

        public override string Prompt
        {
            get
            {
                GameSession session = GameSession.Current;
                if (session != null && session.Resonance == ResonanceStage.BloomingGarden
                    && session.WindmillQuest == QuestStage.Active && session.Inventory.Has(ItemIds.WindmillBraces, 1))
                    return "Give the Windmill Braces to Alder";
                return base.Prompt;
            }
        }

        public override void Interact(InteractionDetector interactor)
        {
            GameSession session = GameSession.Current;
            if (session == null)
            {
                Dialogue.Open(Speaker, lockedLines);
                return;
            }

            switch (session.Resonance)
            {
                case ResonanceStage.SilentValley:
                case ResonanceStage.AwakeningStream:
                    Dialogue.Open(Speaker, lockedLines);
                    return;

                case ResonanceStage.BloomingGarden:
                    if (session.WindmillQuest == QuestStage.NotStarted)
                    {
                        Dialogue.Open(Speaker, session.Inventory.Has(ItemIds.WindmillBraces, 1) ? offerWithBracesLines : offerLines,
                            () => session.TryStartWindmillQuest());
                        return;
                    }

                    // The quest is active: fit the braces if there are any, otherwise explain what is missing.
                    Outcome result = session.TryDeliverWindmillBraces();
                    if (result == Outcome.Ok) Dialogue.Open(Speaker, deliveredLines);
                    else Dialogue.Open(Speaker, Reminder(session));
                    return;

                default:
                    Dialogue.Open(Speaker, session.EndingSeen ? afterEndingLines : afterLines);
                    return;
            }
        }

        /// <summary>What Alder says when the quest is active and there are no braces yet: how many ore are still needed.</summary>
        public static string[] Reminder(GameSession session)
        {
            int ore = session.Inventory.Count(ItemIds.Ore);
            string how = ore >= QuestLog.BracesOre
                ? "You have enough ore. Mix Windmill Braces at the workshop and bring them here."
                : "Gather " + QuestLog.BracesOre + " Echo Ore from the pale outcrops around the valley's edge (you have " + ore
                    + "), mix Windmill Braces at the workshop and bring them here.";
            return new[] { "I need Windmill Braces to mend the supports. " + how };
        }
    }
}
