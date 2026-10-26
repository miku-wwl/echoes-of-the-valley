using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// A light source that is a glowing object (the caretaker's lantern, the cottage windows): its emission follows the one
    /// ResonanceStage, dim in the silent valley and bright in the restored one, with an optional flicker. It only writes a
    /// MaterialPropertyBlock, so the shared material asset is never changed.
    /// </summary>
    public class StageGlow : MonoBehaviour
    {
        static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        [SerializeField] Renderer target;
        [SerializeField] Color color = new Color(1f, 0.80f, 0.40f);
        [SerializeField] float[] stageIntensity = { 0.15f, 0.40f, 0.80f, 1.40f };
        [SerializeField, Range(0f, 0.5f)] float flicker = 0.08f;
        [SerializeField] float phase;
        [SerializeField] bool constant;               // ignore the stage (the title screen)
        [SerializeField] float constantIntensity = 1f;

        MaterialPropertyBlock block;
        float current = -1f;

        /// <summary>The emission strength shown right now (before the flicker).</summary>
        public float Intensity => Mathf.Max(0f, current);

        public float GoalForStage(ResonanceStage stage)
        {
            if (constant) return constantIntensity;
            if (stageIntensity == null || stageIntensity.Length == 0) return 1f;
            return stageIntensity[Mathf.Clamp((int)stage, 0, stageIntensity.Length - 1)];
        }

        void Update()
        {
            if (target == null) return;
            GameSession session = GameSession.Current;
            float goal = GoalForStage(session != null ? session.Resonance : ResonanceStage.SilentValley);
            current = current < 0f ? goal : Mathf.MoveTowards(current, goal, Time.deltaTime * 0.5f);   // eases over a couple of seconds, instant on load

            float wobble = 1f + flicker * (Mathf.Sin(Time.time * 7.3f + phase) * 0.6f + Mathf.Sin(Time.time * 12.9f + phase * 2f) * 0.4f);
            if (block == null) block = new MaterialPropertyBlock();
            target.GetPropertyBlock(block);
            block.SetColor(EmissionColor, color * (current * wobble));
            target.SetPropertyBlock(block);
        }
    }
}
