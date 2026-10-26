using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// Procedural walk and idle feedback for the primitive-built characters (no rig or clips needed): the legs and arms swing
    /// while walking, the body bobs and leans, the scarf, cape and braids trail behind, the head looks about, the eyes blink,
    /// and an idle character breathes. Every part is optional, so simple characters work with just a body. NpcIdle adds a
    /// mood pose on top (PoseOffset, PoseEuler and Wave).
    /// </summary>
    public class CharacterAnimator : MonoBehaviour
    {
        [SerializeField] Transform body;
        [SerializeField] Transform head;
        [SerializeField] Transform leftArm;
        [SerializeField] Transform rightArm;
        [SerializeField] Transform leftLeg;
        [SerializeField] Transform rightLeg;
        [SerializeField] Transform leftFoot;    // the Phase 1 characters moved bare feet; kept for characters without legs
        [SerializeField] Transform rightFoot;
        [SerializeField] Transform[] eyes;
        [SerializeField] Transform[] tails;     // scarf tails, capes and braids that swing behind the body
        [SerializeField] float stepRate = 10f;

        Vector3 bodyBase;
        Vector3 leftBase;
        Vector3 rightBase;
        Vector3[] eyeBase = new Vector3[0];
        float phase;
        float nextBlink = 2.5f;
        float blinkUntil;

        /// <summary>0 = standing, 1 = full walking speed.</summary>
        public float Speed01 { get; set; }

        /// <summary>Extra pose added on top of the walk and idle motion (NpcIdle uses it for slumped and cheerful villagers).</summary>
        public Vector3 PoseOffset { get; set; }

        public Vector3 PoseEuler { get; set; }

        /// <summary>0..1: raises the right arm and waves it (a joyful villager).</summary>
        public float Wave { get; set; }

        /// <summary>The walk cycle's phase in radians; a footstep falls every time it passes a multiple of pi.</summary>
        public float StepPhase => phase;

        public bool HasLimbs => leftArm != null && rightArm != null && leftLeg != null && rightLeg != null;
        public Transform Head => head;
        public bool IsBlinking => Time.time < blinkUntil;

        void Awake()
        {
            if (body != null) bodyBase = body.localPosition;
            if (leftFoot != null) leftBase = leftFoot.localPosition;
            if (rightFoot != null) rightBase = rightFoot.localPosition;
            eyeBase = new Vector3[eyes != null ? eyes.Length : 0];
            for (int i = 0; i < eyeBase.Length; i++) eyeBase[i] = eyes[i].localScale;
            nextBlink = 1.5f + Random.value * 3f;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            float walk = Mathf.Clamp01(Speed01);
            phase += dt * stepRate * walk;
            float swing = Mathf.Sin(phase) * walk;
            float bob = Mathf.Abs(Mathf.Sin(phase)) * 0.06f * walk;
            float breathe = Mathf.Sin(Time.time * 2f) * 0.012f * (1f - walk);
            float idle = 1f - walk;

            if (body != null)
            {
                body.localPosition = bodyBase + Vector3.up * (bob + breathe) + PoseOffset;
                body.localRotation = Quaternion.Euler(PoseEuler + new Vector3(4f * walk, 0f, swing * 2.5f));
            }

            // Legs and arms swing against each other; a legless character still moves its feet
            if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(-swing * 38f, 0f, 0f);
            if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(swing * 38f, 0f, 0f);
            if (leftLeg == null && leftFoot != null) leftFoot.localPosition = leftBase + new Vector3(0f, Mathf.Max(0f, swing) * 0.12f, swing * 0.3f);
            if (rightLeg == null && rightFoot != null) rightFoot.localPosition = rightBase + new Vector3(0f, Mathf.Max(0f, -swing) * 0.12f, -swing * 0.3f);

            float armSway = Mathf.Sin(Time.time * 1.7f) * 2.5f * idle;
            if (leftArm != null) leftArm.localRotation = Quaternion.Euler(swing * 32f + armSway, 0f, -9f);
            if (rightArm != null)
            {
                float raise = Mathf.SmoothStep(0f, 1f, Wave);
                float waving = Mathf.Sin(Time.time * 9f) * 16f * raise;
                rightArm.localRotation = Quaternion.Euler(Mathf.Lerp(-swing * 32f - armSway, 0f, raise), 0f, Mathf.Lerp(9f, 150f + waving, raise));
            }

            // Scarf tails, capes and braids trail behind when walking and drift gently when standing
            if (tails != null)
            {
                for (int i = 0; i < tails.Length; i++)
                {
                    if (tails[i] == null) continue;
                    float trail = walk * (28f + Mathf.Sin(phase * 2f + i) * 8f) + Mathf.Sin(Time.time * 2.1f + i * 1.7f) * 3f;
                    tails[i].localRotation = Quaternion.Euler(trail, Mathf.Sin(Time.time * 1.3f + i) * 4f, 0f);
                }
            }

            // Looking about, and blinking now and then
            if (head != null)
                head.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 0.9f) * 1.5f - walk * 3f, Mathf.Sin(Time.time * 0.55f) * 5f * idle, Mathf.Sin(Time.time * 0.7f) * 1.5f * idle);
            if (eyes != null && eyes.Length > 0)
            {
                if (Time.time >= nextBlink)
                {
                    blinkUntil = Time.time + 0.13f;
                    nextBlink = Time.time + 2.2f + Random.value * 3.5f;
                }
                float open = IsBlinking ? 0.12f : 1f;
                for (int i = 0; i < eyes.Length; i++)
                    if (eyes[i] != null) eyes[i].localScale = new Vector3(eyeBase[i].x, eyeBase[i].y * open, eyeBase[i].z);
            }
        }
    }
}
