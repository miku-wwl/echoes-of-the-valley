using UnityEngine;

namespace Echoes
{
    /// <summary>Shared plumbing for interactables that speak through the DialogueBox and may advance the objective.</summary>
    public abstract class DialogueInteractable : Interactable
    {
        [SerializeField] DialogueBox dialogue;
        [SerializeField] ExplorationObjective objective;
        [SerializeField] string speaker = "";

        protected DialogueBox Dialogue => dialogue;
        protected ExplorationObjective Objective => objective;
        protected string Speaker => speaker;

        public bool IsWired => dialogue != null && objective != null;
    }
}
