using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// Soft footsteps on the grass: one every stride of walking on the ground. Teleports and falls make no steps, so a test or
    /// the smoke run that jumps the player around stays quiet.
    /// </summary>
    public class FootstepAudio : MonoBehaviour
    {
        [SerializeField] PlayerController player;
        [SerializeField] float strideMetres = 1.35f;

        Vector3 last;
        float travelled;
        bool initialised;

        public int Steps { get; private set; }

        void Update()
        {
            if (player == null) return;
            Vector3 p = transform.position;
            if (!initialised)
            {
                last = p;
                initialised = true;
                return;
            }

            float step = new Vector2(p.x - last.x, p.z - last.z).magnitude;
            last = p;
            if (step > 1f || !player.IsGrounded) return;   // a teleport, or in the air

            travelled += step;
            if (travelled < strideMetres) return;
            travelled -= strideMetres;
            Steps++;
            AudioDirector.PlayStatic(SoundCue.Footstep);
        }
    }
}
