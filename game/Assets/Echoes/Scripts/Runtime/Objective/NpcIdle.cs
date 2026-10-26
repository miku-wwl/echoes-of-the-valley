using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// A villager's mood as body language, set by ResonanceView from the one ResonanceStage: 0 stands slumped and still,
    /// 1 stands upright and sways, 2 bounces and turns about with joy. It only adds a pose through CharacterAnimator.
    /// </summary>
    public class NpcIdle : MonoBehaviour
    {
        [SerializeField] CharacterAnimator animator;
        [SerializeField] int firstAwakeStage = 2;   // the ResonanceStage at which this villager perks up
        [SerializeField] float phaseOffset;
        [SerializeField] float slumpDegrees = 8f;

        Vector3 offset;
        Vector3 euler;

        public int Level { get; private set; }
        public int FirstAwakeStage => firstAwakeStage;
        public CharacterAnimator Animator => animator;

        /// <summary>Mood for a stage: slumped before <c>firstAwakeStage</c>, upright from it, joyful once the valley is restored.</summary>
        public int LevelFor(ResonanceStage stage)
        {
            if ((int)stage < firstAwakeStage) return 0;
            return stage == ResonanceStage.RestoredValley ? 2 : 1;
        }

        public void SetLevel(int level) => Level = Mathf.Clamp(level, 0, 2);

        void Update()
        {
            if (animator == null) return;

            float t = Time.time + phaseOffset;
            Vector3 targetOffset;
            Vector3 targetEuler;
            switch (Level)
            {
                case 0:   // tired: leaning forward, barely moving
                    targetEuler = new Vector3(slumpDegrees, 0f, 0f);
                    targetOffset = new Vector3(0f, -0.03f, 0f);
                    break;
                case 1:   // awake: upright, a slow sway and an easy breath
                    targetEuler = new Vector3(0f, Mathf.Sin(t * 0.9f) * 10f, Mathf.Sin(t * 1.6f) * 2.5f);
                    targetOffset = new Vector3(0f, (0.5f + 0.5f * Mathf.Sin(t * 2.4f)) * 0.03f, 0f);
                    break;
                default:  // joyful: little hops while turning to look around
                    targetEuler = new Vector3(0f, Mathf.Sin(t * 1.7f) * 24f, Mathf.Sin(t * 3.2f) * 4f);
                    targetOffset = new Vector3(0f, Mathf.Abs(Mathf.Sin(t * 3.4f)) * 0.12f, 0f);
                    break;
            }

            float blend = 1f - Mathf.Exp(-6f * Time.deltaTime);
            euler = Vector3.Lerp(euler, targetEuler, blend);
            offset = Vector3.Lerp(offset, targetOffset, blend);
            animator.PoseEuler = euler;
            animator.PoseOffset = offset;
        }
    }
}
