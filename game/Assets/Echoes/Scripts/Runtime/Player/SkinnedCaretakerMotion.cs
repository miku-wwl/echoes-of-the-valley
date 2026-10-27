using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// Samples the authored skeletal clips from real displacement and turn rate. The locomotion phase advances with the distance
    /// travelled (one cycle per stride), so a planted foot never slides at the unchanged controller speed. Turn clips are upper-body
    /// only and play on a higher layer. Does not control input, camera or collision.
    /// </summary>
    [DefaultExecutionOrder(90)]
    [RequireComponent(typeof(Animation))]
    public sealed class SkinnedCaretakerMotion : MonoBehaviour
    {
        const float IdleSeconds = 2.4f;
        const float TurnSeconds = 0.9f;
        [SerializeField] float strideMetres = 2.88f;
        Animation clips;
        PlayerController player;
        Vector3 previous;
        float previousYaw;
        float phase;
        float clock;
        float blend;
        float turnWeight;
        float standing;
        bool ready;
        bool layered;

        public float WalkBlend => blend;
        public float WalkPhase => phase;
        public float StrideMetres => strideMetres;
        public float TravelledMetres { get; private set; }
        public float ActualSpeed { get; private set; }
        public string Motion { get; private set; } = "Idle";

        void OnEnable() { ready = false; }

        void Awake() { Configure(); }

        void Configure()
        {
            if (layered) return;
            clips = GetComponent<Animation>();
            Transform spine = FindBone(transform, "Spine");
            foreach (string turn in new[] { "TurnLeft", "TurnRight" })
            {
                AnimationState state = clips[turn];
                if (state == null) continue;
                state.layer = 1;
                state.blendMode = AnimationBlendMode.Blend;
                if (spine != null) state.AddMixingTransform(spine, true);
            }
            layered = true;
        }

        static Transform FindBone(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        void LateUpdate()
        {
            if (clips == null) Configure();
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
            standing = ActualSpeed < 0.2f ? standing + Time.deltaTime : 0f;
            if (standing > 0.25f) phase = 0f; // the next run starts on a heel strike
            else phase = Mathf.Repeat(phase + distance / strideMetres, 1f);
            clock += Time.deltaTime;
            float target = GameFlow.GameplayInputAllowed ? Mathf.Clamp01(ActualSpeed / 1.2f) : 0f;
            blend = Mathf.MoveTowards(blend, target, Time.deltaTime * 9f);
            float turnTarget = Mathf.Clamp01(Mathf.Abs(turning) / 260f) * 0.7f;
            turnWeight = Mathf.MoveTowards(turnWeight, turnTarget, Time.deltaTime * 7f);
            Motion = blend > .15f ? "Walk" : Mathf.Abs(turning) > 20f ? "Turn" : "Idle";
            Set("Idle", Mathf.Repeat(clock / IdleSeconds, 1f), 1f - blend);
            Set("Walk", phase, blend);
            Set("TurnLeft", Mathf.Repeat(clock / TurnSeconds, 1f), turning < 0 ? turnWeight : 0f);
            Set("TurnRight", Mathf.Repeat(clock / TurnSeconds, 1f), turning >= 0 ? turnWeight : 0f);
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
