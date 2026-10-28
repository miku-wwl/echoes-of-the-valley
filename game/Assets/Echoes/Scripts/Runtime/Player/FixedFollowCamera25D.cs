using UnityEngine;
using UnityEngine.InputSystem;

namespace Echoes
{
    /// <summary>
    /// Owner-approved controlled camera for the 2.5D presentation: a fixed yaw, a fixed three-quarter pitch, smooth follow with a little
    /// look-ahead, limited mouse-wheel zoom and a sphere cast that pulls it in front of anything solid. No free orbit.
    /// It runs after the legacy orbit rig, which stays on the camera so the ending's slow drift and the scripted test cameras keep working:
    /// while that rig is switched off for a scripted shot, this camera leaves the transform alone; during the ending it mirrors the rig's
    /// automatic drift as a slow orbit around the player.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public sealed class FixedFollowCamera25D : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] ThirdPersonCamera legacyRig;
        // chosen from Editor renders of four candidates (pitch 38/32/28/24): 30 degrees shows the buildings' fronts and some horizon while
        // keeping canopies and roofs mostly above the line of sight to the character
        [SerializeField] float yaw = 25f;
        [SerializeField] float pitch = 30f;
        [SerializeField] float distance = 8f;
        [SerializeField] float minDistance = 6f;
        [SerializeField] float maxDistance = 11f;
        [SerializeField] float fieldOfView = 35f;
        [SerializeField] Vector3 pivotOffset = new Vector3(0f, 0.9f, 0f);
        [SerializeField] float lookAhead = 0.8f;
        [SerializeField] float followSmoothTime = 0.12f;
        [SerializeField] float collisionRadius = 0.3f;
        [SerializeField] float zoomStep = 0.9f;

        readonly RaycastHit[] hits = new RaycastHit[8];
        Camera view;
        Vector3 pivot, pivotVelocity, ahead, aheadVelocity, lastTarget;
        float zoom, smoothZoom, zoomVelocity, currentDistance, orbit;
        bool initialised;

        public float Yaw => yaw + orbit;
        public float Pitch => pitch;
        /// <summary>The current zoom distance; before the camera has started, the configured default.</summary>
        public float Distance => initialised ? zoom : distance;
        public float MinDistance => minDistance;
        public float MaxDistance => maxDistance;
        public Transform Target => target;

        void Start() => Snap();

        void LateUpdate()
        {
            if (target == null) return;
            if (legacyRig != null && !legacyRig.enabled) return;   // a scripted shot owns the camera this frame
            if (!initialised) Snap();
            float dt = Time.deltaTime;
            if (GameFlow.GameplayInputAllowed && Mouse.current != null)
            {
                float scroll = Mouse.current.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f) zoom = Mathf.Clamp(zoom - Mathf.Sign(scroll) * zoomStep, minDistance, maxDistance);
            }
            float drift = legacyRig != null ? legacyRig.AutoOrbitDegreesPerSecond : 0f;
            orbit = drift != 0f ? Mathf.Repeat(orbit + drift * dt, 360f) : Mathf.MoveTowardsAngle(orbit, 0f, 120f * dt);
            Follow(dt, false);
        }

        /// <summary>Places the camera immediately (scene start, teleports, tests).</summary>
        public void Snap()
        {
            if (target == null) return;
            if (!initialised)
            {
                zoom = Mathf.Clamp(distance, minDistance, maxDistance);
                initialised = true;
            }
            smoothZoom = zoom;
            zoomVelocity = 0f;
            view = GetComponent<Camera>();
            if (view != null) view.fieldOfView = fieldOfView;
            lastTarget = target.position;
            ahead = Vector3.zero;
            Follow(0f, true);
        }

        public void SetZoom(float metres) => zoom = Mathf.Clamp(metres, minDistance, maxDistance);

        void Follow(float dt, bool instant)
        {
            Vector3 velocity = dt > 0f ? (target.position - lastTarget) / dt : Vector3.zero;
            lastTarget = target.position;
            velocity.y = 0f;
            if (velocity.magnitude > 20f) { velocity = Vector3.zero; instant = true; }   // teleport
            Vector3 wantedAhead = Vector3.ClampMagnitude(velocity * (lookAhead / 4.5f), lookAhead);
            Vector3 wantedPivot = target.position + pivotOffset;
            if (instant)
            {
                ahead = wantedAhead;
                pivot = wantedPivot;
                pivotVelocity = aheadVelocity = Vector3.zero;
            }
            else
            {
                ahead = Vector3.SmoothDamp(ahead, wantedAhead, ref aheadVelocity, 0.45f, Mathf.Infinity, dt);
                pivot = Vector3.SmoothDamp(pivot, wantedPivot, ref pivotVelocity, followSmoothTime, Mathf.Infinity, dt);
            }
            smoothZoom = instant ? zoom : Mathf.SmoothDamp(smoothZoom, zoom, ref zoomVelocity, 0.35f, Mathf.Infinity, dt);
            Quaternion rotation = Quaternion.Euler(pitch, yaw + orbit, 0f);
            Vector3 back = rotation * Vector3.back;
            Vector3 focus = pivot + ahead;
            float free = FreeDistance(focus, back, smoothZoom);
            currentDistance = instant || free < currentDistance ? free : Mathf.MoveTowards(currentDistance, free, 10f * dt);
            transform.SetPositionAndRotation(focus + back * currentDistance, rotation);
        }

        float FreeDistance(Vector3 origin, Vector3 direction, float wanted)
        {
            int count = Physics.SphereCastNonAlloc(origin, collisionRadius, direction, hits, wanted, ~0, QueryTriggerInteraction.Ignore);
            float nearest = wanted;
            for (int i = 0; i < count; i++)
            {
                if (hits[i].collider.transform.IsChildOf(target) || hits[i].distance <= 0f) continue;
                nearest = Mathf.Min(nearest, hits[i].distance);
            }
            return Mathf.Max(3.5f, nearest);
        }
    }
}
