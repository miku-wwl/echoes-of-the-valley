using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// Makes water move: it scrolls the water texture along the surface through a MaterialPropertyBlock (the shared material,
    /// which ResonanceView swaps between murky and clear, is never touched). While the valley is silent the water only
    /// creeps; once the stream is restored it flows at full speed.
    /// </summary>
    public class WaterFlow : MonoBehaviour
    {
        static readonly int BaseMapScaleOffset = Shader.PropertyToID("_BaseMap_ST");

        [SerializeField] Renderer target;
        [SerializeField] Vector2 tiling = Vector2.one;
        [SerializeField] Vector2 speed = new Vector2(0f, -0.12f);   // texture repeats per second at full flow
        [SerializeField, Range(0f, 1f)] float silentFlow = 0.3f;     // the share of that speed while the valley is silent

        MaterialPropertyBlock block;
        Vector2 offset;
        float flow = -1f;

        /// <summary>How far the texture has scrolled (tests check that it keeps changing).</summary>
        public Vector2 Offset => offset;

        /// <summary>The share of full speed right now: 0..1.</summary>
        public float Flow => Mathf.Max(0f, flow);

        void Update()
        {
            if (target == null) return;
            GameSession session = GameSession.Current;
            float goal = session != null && session.Resonance >= ResonanceStage.AwakeningStream ? 1f : silentFlow;
            flow = flow < 0f ? goal : Mathf.MoveTowards(flow, goal, Time.deltaTime * 0.5f);

            offset += speed * (flow * Time.deltaTime);
            offset = new Vector2(Mathf.Repeat(offset.x, 1f), Mathf.Repeat(offset.y, 1f));

            if (block == null) block = new MaterialPropertyBlock();
            target.GetPropertyBlock(block);
            block.SetVector(BaseMapScaleOffset, new Vector4(tiling.x, tiling.y, offset.x, offset.y));
            target.SetPropertyBlock(block);
        }
    }
}
