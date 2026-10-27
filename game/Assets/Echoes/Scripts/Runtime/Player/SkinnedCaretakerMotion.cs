using UnityEngine;

namespace Echoes
{
    /// <summary>Samples authored skeletal clips by real displacement. Does not control input, camera or collision.</summary>
    [DefaultExecutionOrder(90)]
    [RequireComponent(typeof(Animation))]
    public sealed class SkinnedCaretakerMotion : MonoBehaviour
    {
        [SerializeField] float strideMetres = 2.4f;
        Animation clips;
        PlayerController player;
        Vector3 previous;
        float previousYaw;
        float phase;
        float clock;
        float blend;
        bool ready;

        public float WalkBlend => blend;
        public float WalkPhase => phase;
        public float TravelledMetres { get; private set; }
        public float ActualSpeed { get; private set; }
        public string Motion { get; private set; } = "Idle";

        void OnEnable() { ready = false; }

        void LateUpdate()
        {
            if (clips == null) clips = GetComponent<Animation>();
            if (player == null) player = GetComponentInParent<PlayerController>();
            if (player == null || Time.deltaTime <= 0f) return;
            if (!ready)
            {
                previous = player.transform.position;
                previousYaw = player.transform.eulerAngles.y;
                ready = true;
            }
            float distance = Vector3.ProjectOnPlane(player.transform.position - previous, Vector3.up).magnitude;
            float yaw = player.transform.eulerAngles.y;
            float turning = Mathf.DeltaAngle(previousYaw, yaw) / Time.deltaTime;
            previousYaw = yaw;
            previous = player.transform.position;
            if (distance > 1f) distance = 0f; // Teleports do not animate a stride or count as walking.
            ActualSpeed = distance / Time.deltaTime;
            TravelledMetres += distance;
            phase = Mathf.Repeat(phase + distance / strideMetres, 1f);
            clock += Time.deltaTime;
            float target = GameFlow.GameplayInputAllowed ? Mathf.Clamp01(ActualSpeed / 1.2f) : 0f;
            blend = Mathf.MoveTowards(blend, target, Time.deltaTime * 7f);
            float turnWeight = Mathf.Clamp01(Mathf.Abs(turning) / 220f) * .24f;
            Motion = blend > .15f ? "Walk" : Mathf.Abs(turning) > 20f ? "Turn" : "Idle";
            Set("Idle", Mathf.Repeat(clock / 2.4f, 1f), (1f - blend) * (1f - turnWeight));
            Set("Walk", phase, blend * (1f - turnWeight));
            Set("TurnLeft", Mathf.Repeat(clock / .9f, 1f), turning < 0 ? turnWeight : 0f);
            Set("TurnRight", Mathf.Repeat(clock / .9f, 1f), turning >= 0 ? turnWeight : 0f);
            clips.Sample();
        }

        void Set(string name, float time, float weight)
        {
            AnimationState state = clips[name];
            if (state == null) return;
            state.enabled = true;
            state.speed = 0f;
            state.weight = weight;
            state.normalizedTime = time;
        }
    }
}
