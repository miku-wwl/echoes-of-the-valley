using System.Collections;
using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// Draws the valley for the single authoritative ResonanceStage. Stage 0: a murky trickle, a dry basin, no blooms.
    /// Stage 1: a wide clear stream, a full basin with a fountain, blooms along the banks and a warmer light.
    /// A stage that changes while playing runs a short transition (outward pulse, growing blooms, rising water, warming light);
    /// a stage that is already reached when the scene loads is shown at once, so nothing replays after loading a save.
    /// </summary>
    public class ResonanceView : MonoBehaviour
    {
        [SerializeField] ValleyLighting lighting;
        [SerializeField] GameObject troubledStream;
        [SerializeField] GameObject clearStream;
        [SerializeField] Renderer pond;
        [SerializeField] Renderer springWater;
        [SerializeField] Material murkyWater;
        [SerializeField] Material clearWater;
        [SerializeField] Vector3 springWaterDry = new Vector3(0.9f, 0.006f, 0.9f);
        [SerializeField] Vector3 springWaterFull = new Vector3(2.5f, 0.02f, 2.5f);
        [SerializeField] GameObject bloomRoot;
        [SerializeField] ParticleSystem pulse;
        [SerializeField] ParticleSystem fountain;
        [SerializeField] float transitionSeconds = 3.5f;

        Transform[] bloomChildren = new Transform[0];
        Vector3[] bloomScales = new Vector3[0];
        bool initialised;

        public ResonanceStage ShownStage { get; private set; } = ResonanceStage.SilentValley;
        public bool IsTransitioning { get; private set; }
        public bool ClearStreamShown => clearStream != null && clearStream.activeSelf;
        public bool TroubledStreamShown => troubledStream != null && troubledStream.activeSelf;
        public bool BloomShown => bloomRoot != null && bloomRoot.activeSelf;
        public bool PondIsClear => pond != null && pond.sharedMaterial == clearWater;
        public float StreamBlend => lighting != null ? lighting.StreamBlend : 0f;
        public ValleyLighting Lighting => lighting;

        void Awake()
        {
            if (bloomRoot == null) return;
            int count = bloomRoot.transform.childCount;
            bloomChildren = new Transform[count];
            bloomScales = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                bloomChildren[i] = bloomRoot.transform.GetChild(i);
                bloomScales[i] = bloomChildren[i].localScale;
            }
        }

        void Update()
        {
            GameSession session = GameSession.Current;
            if (session == null) return;

            if (!initialised)
            {
                initialised = true;
                ApplyStage(session.Resonance, true);
                return;
            }
            if (session.Resonance == ShownStage) return;

            if (session.Resonance > ShownStage) PlayTransition(session.Resonance);
            else ApplyStage(session.Resonance, true);
        }

        void ApplyStage(ResonanceStage stage, bool settled)
        {
            bool awake = stage == ResonanceStage.AwakeningStream;
            Material water = awake ? clearWater : murkyWater;
            if (pond != null && water != null) pond.sharedMaterial = water;
            if (springWater != null && water != null) springWater.sharedMaterial = water;
            if (troubledStream != null) troubledStream.SetActive(!awake);
            if (clearStream != null) clearStream.SetActive(awake);
            if (bloomRoot != null) bloomRoot.SetActive(awake);

            if (settled)
            {
                SetBloom(awake ? 1f : 0f);
                SetSpringWater(awake ? 1f : 0f);
                if (lighting != null) lighting.SetStreamBlend(awake ? 1f : 0f);
            }

            if (fountain != null)
            {
                if (awake) fountain.Play();
                else fountain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            ShownStage = stage;
        }

        void PlayTransition(ResonanceStage stage)
        {
            ApplyStage(stage, false);
            StartCoroutine(Animate());
        }

        IEnumerator Animate()
        {
            IsTransitioning = true;
            SetBloom(0f);
            SetSpringWater(0f);
            if (pulse != null) pulse.Play();

            for (float elapsed = 0f; elapsed < transitionSeconds; elapsed += Time.deltaTime)
            {
                float k = Mathf.Clamp01(elapsed / transitionSeconds);
                if (lighting != null) lighting.SetStreamBlend(Mathf.SmoothStep(0f, 1f, k));
                SetSpringWater(Mathf.SmoothStep(0f, 1f, k * 1.5f));
                SetBloom(k);
                yield return null;
            }

            SetBloom(1f);
            SetSpringWater(1f);
            if (lighting != null) lighting.SetStreamBlend(1f);
            IsTransitioning = false;
        }

        void SetSpringWater(float fill)
        {
            if (springWater != null) springWater.transform.localScale = Vector3.Lerp(springWaterDry, springWaterFull, Mathf.Clamp01(fill));
        }

        /// <summary>Blooms pop up one after another: child i starts a little later than child i-1 and overshoots slightly.</summary>
        void SetBloom(float progress)
        {
            int count = bloomChildren.Length;
            for (int i = 0; i < count; i++)
            {
                float local = Mathf.Clamp01((progress - 0.5f * i / Mathf.Max(1, count)) / 0.5f);
                float eased = 1f + 2.70158f * Mathf.Pow(local - 1f, 3f) + 1.70158f * Mathf.Pow(local - 1f, 2f);   // ease-out-back
                bloomChildren[i].localScale = bloomScales[i] * Mathf.Max(0.001f, local <= 0f ? 0f : eased);
            }
        }
    }
}
