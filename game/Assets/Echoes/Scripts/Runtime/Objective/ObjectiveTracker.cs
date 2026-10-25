namespace Echoes
{
    public enum ObjectiveStep
    {
        MeetVillager = 0,
        InspectSpring = 1,
        ReturnToHeart = 2,
        Complete = 3
    }

    /// <summary>
    /// Plain-C# state machine for the Phase 1 objective. Steps only move forward, one at a time, and only
    /// through the matching Try* call; anything out of order or repeated is rejected without changing state.
    /// </summary>
    public sealed class ObjectiveTracker
    {
        public const string Title = "Discover the Heart of the Valley";

        public ObjectiveStep Step { get; private set; } = ObjectiveStep.MeetVillager;

        public bool IsComplete => Step == ObjectiveStep.Complete;

        public bool TryMarkVillagerMet() => TryAdvance(ObjectiveStep.MeetVillager);

        public bool TryMarkSpringInspected() => TryAdvance(ObjectiveStep.InspectSpring);

        public bool TryMarkHeartReached() => TryAdvance(ObjectiveStep.ReturnToHeart);

        public void Reset() => Step = ObjectiveStep.MeetVillager;

        /// <summary>Sets the step from saved data. Rejects anything outside the known steps.</summary>
        public bool Restore(int step)
        {
            if (step < (int)ObjectiveStep.MeetVillager || step > (int)ObjectiveStep.Complete) return false;
            Step = (ObjectiveStep)step;
            return true;
        }

        bool TryAdvance(ObjectiveStep expected)
        {
            if (Step != expected) return false;
            Step = expected + 1;
            return true;
        }

        public static string Describe(ObjectiveStep step)
        {
            switch (step)
            {
                case ObjectiveStep.MeetVillager: return "Find the villager near the workshop cottage.";
                case ObjectiveStep.InspectSpring: return "Visit the ruined spring beside the pond.";
                case ObjectiveStep.ReturnToHeart: return "Return to the Heart of the Valley.";
                default: return "Complete - the crystal stirs. Ask Mira about the dry stream.";
            }
        }
    }
}
