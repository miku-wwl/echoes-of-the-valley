using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// Scene-side owner of the exploration objective. A fresh instance (every Valley load) starts at the first
    /// step, so starting a new game resets the objective. Completion feedback fires exactly once.
    /// </summary>
    public class ExplorationObjective : MonoBehaviour
    {
        [SerializeField] HudController hud;
        [SerializeField] CompletionFeedback feedback;

        readonly ObjectiveTracker tracker = new ObjectiveTracker();

        public ObjectiveStep Step => tracker.Step;
        public bool IsComplete => tracker.IsComplete;
        public int CompletionCount { get; private set; }
        public HudController Hud => hud;
        public CompletionFeedback Feedback => feedback;

        void Start()
        {
            Refresh();
            if (hud != null) hud.ShowBanner("New Objective", ObjectiveTracker.Title, 5f);
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
                    hud.ShowBanner("Objective Complete", "You discovered the Heart of the Valley. The valley stirs... its restoration continues in Phase 2.", 9f);
                if (feedback != null) feedback.Play();
            }
            else if (hud != null)
            {
                hud.ShowBanner("New Objective", ObjectiveTracker.Describe(tracker.Step), 5f);
            }
        }

        void Refresh()
        {
            if (hud != null) hud.SetObjective(ObjectiveTracker.Title, ObjectiveTracker.Describe(tracker.Step));
        }
    }
}
