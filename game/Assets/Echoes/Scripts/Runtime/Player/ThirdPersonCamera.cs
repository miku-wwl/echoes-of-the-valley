using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// Orbit follow camera: mouse yaw/pitch with a clamped vertical angle, smoothed follow, and a sphere cast
    /// that pulls the camera in front of terrain or walls instead of letting it clip through them.
    /// </summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Vector3 pivotOffset = new Vector3(0f, 1.5f, 0f);
        [SerializeField] float distance = 6f;
        [SerializeField] float minDistance = 1.2f;
        [SerializeField] float startPitch = 22f;
        [SerializeField] float minPitch = 8f;
        [SerializeField] float maxPitch = 60f;
        [SerializeField] float degreesPerPixel = 0.12f;
        [SerializeField] float maxLookDeltaPerFrame = 80f;
        [SerializeField] float followSmoothTime = 0.05f;
        [SerializeField] float collisionRadius = 0.25f;

        readonly RaycastHit[] hits = new RaycastHit[8];
        float yaw;
        float pitch;
        float currentDistance;
        Vector3 pivot;
        Vector3 pivotVelocity;
        bool initialised;

        public Transform Target => target;
        public float Yaw => yaw;
        public float Pitch => pitch;
        public float MinPitch => minPitch;
        public float MaxPitch => maxPitch;
        public float Distance => distance;

        void Start() => Snap();

        void LateUpdate()
        {
            if (target == null) return;
            if (!initialised) Snap();

            // Mouse look is ignored while paused. Per-frame deltas are clamped in ApplyLook, so a focus change cannot spin the camera.
            if (!GameFlow.IsPaused) ApplyLook(GameInput.ReadLook());

            Follow(Time.deltaTime, false);
        }

        /// <summary>Turns mouse movement (pixels) into yaw/pitch; per-frame spikes are clamped and pitch stays in range.</summary>
        public void ApplyLook(Vector2 delta)
        {
            delta = Vector2.ClampMagnitude(delta, maxLookDeltaPerFrame);
            yaw = Mathf.Repeat(yaw + delta.x * degreesPerPixel, 360f);
            pitch = Mathf.Clamp(pitch - delta.y * degreesPerPixel, minPitch, maxPitch);
        }

        /// <summary>Places the camera immediately, without smoothing (scene start, teleports).</summary>
        public void Snap()
        {
            if (target == null) return;
            if (!initialised)
            {
                yaw = target.eulerAngles.y;
                pitch = Mathf.Clamp(startPitch, minPitch, maxPitch);
                initialised = true;
            }
            Follow(0f, true);
        }

        void Follow(float dt, bool instant)
        {
            Vector3 wantedPivot = target.position + pivotOffset;
            if (instant)
            {
                pivot = wantedPivot;
                pivotVelocity = Vector3.zero;
            }
            else
            {
                pivot = Vector3.SmoothDamp(pivot, wantedPivot, ref pivotVelocity, followSmoothTime, Mathf.Infinity, dt);
            }

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 back = rotation * Vector3.back;

            // Pull in instantly when something is in the way, ease back out afterwards.
            float free = FreeDistance(pivot, back);
            currentDistance = instant || free < currentDistance
                ? free
                : Mathf.MoveTowards(currentDistance, free, 12f * dt);

            transform.SetPositionAndRotation(pivot + back * currentDistance, rotation);
        }

        float FreeDistance(Vector3 origin, Vector3 direction)
        {
            int count = Physics.SphereCastNonAlloc(origin, collisionRadius, direction, hits, distance, ~0, QueryTriggerInteraction.Ignore);
            float nearest = distance;
            for (int i = 0; i < count; i++)
            {
                if (hits[i].collider.transform.IsChildOf(target)) continue;
                nearest = Mathf.Min(nearest, hits[i].distance);
            }
            return Mathf.Max(minDistance, nearest);
        }
    }
}
