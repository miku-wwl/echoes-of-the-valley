using System.Collections;
using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// Payoff for finishing the opening exploration objective: the valley warms up (through ValleyLighting), the Heart
    /// crystal glows and spins, and sparkles rise from the dais. Plays once; a loaded game applies it instantly.
    /// </summary>
    public class CompletionFeedback : MonoBehaviour
    {
        static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        [SerializeField] Light sun;
        [SerializeField] ValleyLighting lighting;
        [SerializeField] ParticleSystem sparkles;
        [SerializeField] Renderer crystal;
        [SerializeField] Transform crystalPivot;
        [SerializeField] float duration = 4f;
        [SerializeField] Color crystalGlow = new Color(0.4f, 1.8f, 1.6f);

        Material crystalMaterial;

        public bool HasPlayed { get; private set; }
        public Light Sun => sun;
        public ParticleSystem Sparkles => sparkles;
        public Renderer Crystal => crystal;
        public ValleyLighting Lighting => lighting;

        public void Play()
        {
            if (HasPlayed) return;
            HasPlayed = true;
            GameSession.Current?.RaiseCue(SoundCue.HeartAwakened);   // only a live completion sounds; a loaded game uses ApplyInstant
            StartCoroutine(Transition());
        }

        /// <summary>Shows the awakened Heart immediately (loading a game whose objective is already complete).</summary>
        public void ApplyInstant()
        {
            if (HasPlayed) return;
            HasPlayed = true;
            Apply(1f);
            if (sparkles != null) sparkles.Play();
        }

        void Update()
        {
            if (!HasPlayed || crystalPivot == null) return;
            crystalPivot.Rotate(0f, 40f * Time.deltaTime, 0f, Space.World);
        }

        IEnumerator Transition()
        {
            if (sparkles != null) sparkles.Play();

            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                Apply(Mathf.SmoothStep(0f, 1f, elapsed / duration));
                yield return null;
            }
            Apply(1f);
        }

        void Apply(float t)
        {
            if (lighting != null) lighting.SetHeartBlend(t);

            if (crystalMaterial == null && crystal != null) crystalMaterial = crystal.material;
            if (crystalMaterial != null)
                crystalMaterial.SetColor(EmissionColor, Color.Lerp(Color.black, crystalGlow, t));
        }
    }
}
