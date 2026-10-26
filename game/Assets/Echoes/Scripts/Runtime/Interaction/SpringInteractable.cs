using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// The ruined spring. Without an active quest it shows its lore (and advances the opening objective). With the
    /// "Restore the Stream" quest active it is where the Spring Tonic is delivered; the quest only completes through
    /// GameSession.TryDeliverSpringTonic, which needs a real tonic in the satchel.
    /// </summary>
    public class SpringInteractable : DialogueInteractable
    {
        [SerializeField, TextArea] string[] lines =
        {
            "A ring of cracked stones. The basin is dry and silted, and only a thin trickle still creeps into the stream.",
            "Once, clear water rose here and carried its song through the whole valley. Now the valley is quiet.",
            "A faint warmth hums beneath the stones, as if something is waiting to be heard."
        };

        [SerializeField, TextArea] string[] needTonicLines =
        {
            "The basin is dry and silent. A Spring Tonic could wake it: mix one at the alchemy workshop from 2 Moonleaf Herb and 1 Dawn Blossom."
        };

        [SerializeField, TextArea] string[] restoredLines =
        {
            "You tip the Spring Tonic into the cracked basin. For a heartbeat, nothing happens.",
            "Then the stones hum. Water rises, clear and cold, and the whole stream answers.",
            "Warm light slides down the valley, and somewhere far off a bell-note rings. Mira will want to hear about this."
        };

        [SerializeField, TextArea] string[] afterLines =
        {
            "Clear water murmurs over the old stones. The spring remembers its song."
        };

        public override string Prompt
        {
            get
            {
                GameSession session = GameSession.Current;
                if (session == null) return base.Prompt;
                if (session.StreamQuest == StreamQuestStage.Completed) return "Listen to the spring";
                if (session.StreamQuest == StreamQuestStage.Active && session.Inventory.Has(ItemIds.SpringTonic, 1))
                    return "Pour the Spring Tonic into the spring";
                return base.Prompt;
            }
        }

        public override void Interact(InteractionDetector interactor)
        {
            GameSession session = GameSession.Current;
            if (session != null && session.StreamQuest == StreamQuestStage.Active)
            {
                Outcome result = session.TryDeliverSpringTonic();
                if (result != Outcome.Ok) session.RaiseCue(SoundCue.Denied);
                Dialogue.Open(Speaker, result == Outcome.Ok ? restoredLines : needTonicLines);
                return;
            }
            if (session != null && session.StreamQuest == StreamQuestStage.Completed)
            {
                Dialogue.Open(Speaker, afterLines);
                return;
            }
            Dialogue.Open(Speaker, lines, Objective.NotifySpringInspected);
        }
    }
}
