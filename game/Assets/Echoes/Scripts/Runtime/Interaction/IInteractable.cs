using UnityEngine;

namespace Echoes
{
    /// <summary>Anything the player can use with E. Range is a flat (XZ) radius around Position.</summary>
    public interface IInteractable
    {
        string Prompt { get; }
        Vector3 Position { get; }
        float Range { get; }
        bool IsInRange(Vector3 from);
        void Interact(InteractionDetector interactor);
    }
}
