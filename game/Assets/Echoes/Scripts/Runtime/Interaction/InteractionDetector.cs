using System.Collections.Generic;
using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// Lives on the player. Each frame it picks the nearest interactable that is in range, shows its prompt,
    /// and on E re-checks the range before interacting, so the prompt and the action always agree.
    /// </summary>
    public class InteractionDetector : MonoBehaviour
    {
        [SerializeField] HudController hud;

        public IInteractable Current { get; private set; }
        public HudController Hud => hud;

        void Update()
        {
            Refresh();
            if (Current != null && GameFlow.TryConsumeInteract()) TryInteract();
        }

        public void Refresh()
        {
            Current = GameFlow.GameplayInputAllowed ? SelectBest(Interactable.Active, transform.position) : null;
            if (hud != null) hud.SetPrompt(Current != null ? Current.Prompt : null);
        }

        /// <summary>Interacts with the nearest in-range target. Returns false (and does nothing) when none is in range.</summary>
        public bool TryInteract()
        {
            Refresh();
            if (Current == null || !Current.IsInRange(transform.position)) return false;
            IInteractable target = Current;
            target.Interact(this);
            Refresh();
            return true;
        }

        /// <summary>Nearest candidate within its own range, or null. Ties resolve to the earlier list entry.</summary>
        public static IInteractable SelectBest(IEnumerable<IInteractable> candidates, Vector3 from)
        {
            IInteractable best = null;
            float bestDistance = float.MaxValue;
            foreach (IInteractable candidate in candidates)
            {
                if (candidate == null || !candidate.IsInRange(from)) continue;
                Vector3 delta = candidate.Position - from;
                float distance = new Vector2(delta.x, delta.z).sqrMagnitude;
                if (distance < bestDistance)
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }
            return best;
        }
    }
}
