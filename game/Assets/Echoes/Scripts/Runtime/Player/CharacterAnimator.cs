using UnityEngine;

namespace Echoes
{
    /// <summary>Procedural walk and idle feedback for the primitive-built characters (no rig or clips needed).</summary>
    public class CharacterAnimator : MonoBehaviour
    {
        [SerializeField] Transform body;
        [SerializeField] Transform leftFoot;
        [SerializeField] Transform rightFoot;
        [SerializeField] float stepRate = 10f;

        Vector3 bodyBase;
        Vector3 leftBase;
        Vector3 rightBase;
        float phase;

        /// <summary>0 = standing, 1 = full walking speed.</summary>
        public float Speed01 { get; set; }

        void Awake()
        {
            if (body != null) bodyBase = body.localPosition;
            if (leftFoot != null) leftBase = leftFoot.localPosition;
            if (rightFoot != null) rightBase = rightFoot.localPosition;
        }

        void Update()
        {
            float walk = Mathf.Clamp01(Speed01);
            phase += Time.deltaTime * stepRate * walk;
            float swing = Mathf.Sin(phase) * walk;
            float bob = Mathf.Abs(Mathf.Sin(phase)) * 0.06f * walk;
            float breathe = Mathf.Sin(Time.time * 2f) * 0.012f * (1f - walk);

            if (body != null) body.localPosition = bodyBase + Vector3.up * (bob + breathe);
            if (leftFoot != null) leftFoot.localPosition = leftBase + new Vector3(0f, Mathf.Max(0f, swing) * 0.12f, swing * 0.3f);
            if (rightFoot != null) rightFoot.localPosition = rightBase + new Vector3(0f, Mathf.Max(0f, -swing) * 0.12f, -swing * 0.3f);
        }
    }
}
