using System.Collections.Generic;
using UnityEngine;

namespace Echoes
{
    /// <summary>Base class for scene interactables; enabled instances register themselves so the detector needs no physics queries.</summary>
    public abstract class Interactable : MonoBehaviour, IInteractable
    {
        /// <summary>Largest height difference at which something still counts as "in reach".</summary>
        public const float VerticalTolerance = 2.5f;

        static readonly List<Interactable> active = new List<Interactable>();

        [SerializeField] string prompt = "Interact";
        [SerializeField] float range = 2.5f;
        [SerializeField] Transform interactionPoint;

        public static IReadOnlyList<Interactable> Active => active;

        /// <summary>Text after "[E]". Override for prompts that depend on state (regrowing nodes, the hen).</summary>
        public virtual string Prompt => prompt;
        public float Range => range;
        public Vector3 Position => interactionPoint != null ? interactionPoint.position : transform.position;

        public static bool IsWithinRange(Vector3 from, Vector3 target, float range)
        {
            Vector3 delta = target - from;
            return Mathf.Abs(delta.y) <= VerticalTolerance
                && new Vector2(delta.x, delta.z).sqrMagnitude <= range * range;
        }

        public bool IsInRange(Vector3 from) => IsWithinRange(from, Position, range);

        public abstract void Interact(InteractionDetector interactor);

        protected virtual void OnEnable()
        {
            if (!active.Contains(this)) active.Add(this);
        }

        protected virtual void OnDisable() => active.Remove(this);
    }
}
