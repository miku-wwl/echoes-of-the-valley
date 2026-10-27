using UnityEngine;

namespace Echoes
{
    /// <summary>Occasional blink: swaps the painted skin to its closed-eye twin through a material property block. Pure presentation.</summary>
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    public sealed class CaretakerFace : MonoBehaviour
    {
        const float BlinkSeconds = 0.16f;
        static readonly int BlinkId = Shader.PropertyToID("_Blink");
        SkinnedMeshRenderer skin;
        MaterialPropertyBlock block;
        int materialIndex = -1;
        float nextBlink;
        float clock = -1f;

        public float Blink { get; private set; }

        void Awake()
        {
            skin = GetComponent<SkinnedMeshRenderer>();
            Material[] materials = skin.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
                if (materials[i] != null && materials[i].name == "Hero_Skin" && materials[i].HasProperty("_BlinkMap") && materials[i].GetTexture("_BlinkMap") != null) materialIndex = i;
            if (materialIndex < 0) { enabled = false; return; }
            block = new MaterialPropertyBlock();
            nextBlink = Time.time + 1.8f;
        }

        void Update()
        {
            if (clock < 0f)
            {
                if (Time.time < nextBlink) return;
                clock = 0f;
            }
            clock += Time.deltaTime;
            float t = clock / BlinkSeconds;
            Blink = t < 0.45f ? t / 0.45f : t < 1f ? 1f - (t - 0.45f) / 0.55f : 0f;
            if (t >= 1f)
            {
                clock = -1f;
                nextBlink = Time.time + Mathf.Lerp(2.4f, 5.6f, Mathf.Abs(Mathf.Sin(Time.time * 12.9898f)));
            }
            skin.GetPropertyBlock(block, materialIndex);
            block.SetFloat(BlinkId, Blink);
            skin.SetPropertyBlock(block, materialIndex);
        }
    }
}
