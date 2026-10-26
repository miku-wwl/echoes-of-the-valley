using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// Slow clouds for the sky. Each child group drifts along +X at its own pace and wraps around, and the whole layer takes the
    /// colour of the current ResonanceStage (pale grey-white, then white, peach, and finally golden). Transform animation only.
    /// </summary>
    public class CloudDrift : MonoBehaviour
    {
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        [SerializeField] float limit = 110f;
        [SerializeField] Color[] stageTints =
        {
            new Color(0.90f, 0.92f, 0.96f), new Color(0.96f, 0.96f, 0.98f), new Color(1.00f, 0.95f, 0.91f), new Color(1.00f, 0.86f, 0.68f)
        };

        Transform[] groups = new Transform[0];
        Renderer[] renderers = new Renderer[0];
        MaterialPropertyBlock block;
        Color current;
        bool tinted;

        /// <summary>Distance travelled by the first cloud since the scene loaded (tests check that the sky moves).</summary>
        public float Travelled { get; private set; }
        public int CloudCount => groups.Length;
        public Color Tint => current;

        void Awake()
        {
            groups = new Transform[transform.childCount];
            for (int i = 0; i < groups.Length; i++) groups[i] = transform.GetChild(i);
            renderers = GetComponentsInChildren<Renderer>();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < groups.Length; i++)
            {
                float pace = 0.55f + 0.14f * (i % 5);
                Vector3 p = groups[i].localPosition;
                p.x += pace * dt;
                if (p.x > limit) p.x -= limit * 2f;
                groups[i].localPosition = p;
                if (i == 0) Travelled += pace * dt;
            }

            GameSession session = GameSession.Current;
            int stage = session != null ? (int)session.Resonance : 0;
            Color goal = stageTints[Mathf.Clamp(stage, 0, stageTints.Length - 1)];
            current = tinted ? Color.Lerp(current, goal, 1f - Mathf.Exp(-0.8f * dt)) : goal;
            tinted = true;

            if (block == null) block = new MaterialPropertyBlock();
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].GetPropertyBlock(block);
                block.SetColor(BaseColor, current);
                renderers[i].SetPropertyBlock(block);
            }
        }
    }
}
