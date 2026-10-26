using System;

namespace Echoes
{
    public readonly struct QuestLine
    {
        public QuestLine(string heading, string body)
        {
            Heading = heading;
            Body = body;
        }

        public string Heading { get; }
        public string Body { get; }
    }

    /// <summary>
    /// What the objective panel says once the opening exploration is done. It is a pure function of the GameSession, so the
    /// HUD never keeps its own idea of the story: it names who to talk to, what is still needed and where to take it.
    /// </summary>
    public static class QuestLog
    {
        public const string StreamHeading = "Restore the Stream";
        public const string GardenHeading = "Restore the Garden";
        public const string WindmillHeading = "Restore the Windmill";
        public const string ReturnHeading = "Return to the Valley";
        public const string EndedHeading = "Ending Complete";

        public const int BracesOre = 4;   // Windmill Braces = 4 Echo Ore (RecipeBook)

        public static QuestLine Current(GameSession session)
        {
            switch (session.Resonance)
            {
                case ResonanceStage.SilentValley:
                    return new QuestLine(StreamHeading, StreamBody(session));

                case ResonanceStage.AwakeningStream:
                    return new QuestLine(GardenHeading, session.GardenQuest == QuestStage.NotStarted
                        ? "Speak to Wren, the Garden Keeper, at the garden south-east of the Heart."
                        : GardenBody(session));

                case ResonanceStage.BloomingGarden:
                    return new QuestLine(WindmillHeading, session.WindmillQuest == QuestStage.NotStarted
                        ? "Speak to Alder, the Windmill Restorer, at the old windmill north of the Heart."
                        : WindmillBody(session));

                default:
                    return session.EndingSeen
                        ? new QuestLine(EndedHeading, "The valley has found its voice. Explore as long as you like.")
                        : new QuestLine(ReturnHeading, "The windmill turns! Speak to Mira by the workshop cottage.");
            }
        }

        static string StreamBody(GameSession session)
        {
            if (session.StreamQuest == StreamQuestStage.NotStarted) return "Speak to Mira about the dry stream.";

            int herbs = session.Inventory.Count(ItemIds.Herb);
            int flowers = session.Inventory.Count(ItemIds.Flower);
            if (session.Inventory.Has(ItemIds.SpringTonic, 1)) return "Pour the Spring Tonic into the ruined spring.";
            if (herbs >= 2 && flowers >= 1) return "Mix a Spring Tonic at the alchemy workshop.";
            return "Gather Moonleaf Herb " + Math.Min(herbs, 2) + "/2 and Dawn Blossom " + Math.Min(flowers, 1) + "/1.";
        }

        static string GardenBody(GameSession session)
        {
            if (session.Inventory.Has(ItemIds.Egg, 1)) return "Give Clover's egg to the Garden Keeper.";
            switch (session.Chicken)
            {
                case ChickenPhase.EggReady:
                    return "Collect the egg from Clover's nest.";
                case ChickenPhase.Producing:
                    return GameSession.ChickenName + " is laying an egg (" + (int)Math.Ceiling(session.ChickenRemaining) + " s).";
            }
            if (session.Inventory.Has(ItemIds.ChickenFeed, 1)) return "Feed Clover in her yard.";
            if (session.Inventory.Has(ItemIds.Flower, 1)) return "Mix Chicken Feed at the alchemy workshop.";
            return "Gather a Dawn Blossom, mix Chicken Feed at the workshop, then feed Clover.";
        }

        static string WindmillBody(GameSession session)
        {
            if (session.Inventory.Has(ItemIds.WindmillBraces, 1)) return "Give the Windmill Braces to the Windmill Restorer.";

            int ore = session.Inventory.Count(ItemIds.Ore);
            if (ore >= BracesOre) return "Mix Windmill Braces at the alchemy workshop.";
            return "Gather Echo Ore " + ore + "/" + BracesOre + " from the pale outcrops around the valley's edge.";
        }
    }
}
