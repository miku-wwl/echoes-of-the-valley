using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// Scene-side owner of the exploration objective (the opening chapter). A fresh instance (every new Valley load)
    /// starts at the first step; a loaded game restores its saved step without replaying the payoff.
    /// Progress is mirrored into the GameSession so it is saved. Completion feedback fires exactly once.
    /// </summary>
    public class ExplorationObjective : MonoBehaviour
    {
        [SerializeField] HudController hud;
        [SerializeField] CompletionFeedback feedback;

        readonly ObjectiveTracker tracker = new ObjectiveTracker();
        bool restored;

        public ObjectiveStep Step => tracker.Step;
        public bool IsComplete => tracker.IsComplete;
        public int CompletionCount { get; private set; }
        public HudController Hud => hud;
        public CompletionFeedback Feedback => feedback;

        void Start()
        {
            Refresh();
            if (!restored && hud != null) hud.ShowBanner("New Objective", ObjectiveTracker.Title, 5f);
        }

        /// <summary>Applies a saved step: no banner, no new reward, and an already-awakened Heart is shown at once.</summary>
        public void RestoreStep(ObjectiveStep step)
        {
            if (!tracker.Restore((int)step)) return;
            restored = true;
            Refresh();
            if (tracker.IsComplete && feedback != null) feedback.ApplyInstant();
        }

        public void NotifyVillagerMet() => OnAdvance(tracker.TryMarkVillagerMet());

        public void NotifySpringInspected() => OnAdvance(tracker.TryMarkSpringInspected());

        public void NotifyHeartReached() => OnAdvance(tracker.TryMarkHeartReached());

        void OnAdvance(bool advanced)
        {
            if (!advanced) return;
            Refresh();

            if (tracker.IsComplete)
            {
                CompletionCount++;
                if (hud != null)
                    hud.ShowBanner("Objective Complete", "You discovered the Heart of the Valley. The crystal stirs... Mira may know how to wake the stream.", 9f);
                if (feedback != null) feedback.Play();
            }
            else if (hud != null)
            {
                hud.ShowBanner("New Objective", ObjectiveTracker.Describe(tracker.Step), 5f);
            }

            GameSession.Current?.SetExplorationStep((int)tracker.Step);
        }

        void Refresh()
        {
            if (hud != null) hud.SetObjective(ObjectiveTracker.Title, ObjectiveTracker.Describe(tracker.Step));
        }
    }
}
