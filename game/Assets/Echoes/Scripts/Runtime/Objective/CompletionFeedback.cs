using System.Collections;
using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// Phase 1 payoff for finishing the objective: the muted valley warms up (sun, ambient, fog, sky),
    /// the Heart crystal glows and spins, and sparkles rise from the dais. Plays once.
    /// </summary>
    public class CompletionFeedback : MonoBehaviour
    {
        static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        static readonly int SkyTint = Shader.PropertyToID("_SkyTint");
        static readonly int Exposure = Shader.PropertyToID("_Exposure");

        [SerializeField] Light sun;
        [SerializeField] ParticleSystem sparkles;
        [SerializeField] Renderer crystal;
        [SerializeField] Transform crystalPivot;
        [SerializeField] float duration = 4f;

        [Header("Awakened look")]
        [SerializeField] Color sunColor = new Color(1f, 0.86f, 0.62f);
        [SerializeField] float sunIntensity = 1.35f;
        [SerializeField] Color ambientSky = new Color(0.62f, 0.78f, 1f);
        [SerializeField] Color ambientEquator = new Color(0.86f, 0.76f, 0.60f);
        [SerializeField] Color ambientGround = new Color(0.46f, 0.40f, 0.30f);
        [SerializeField] Color fog = new Color(0.93f, 0.82f, 0.64f);
        [SerializeField] Color skyTint = new Color(0.62f, 0.50f, 0.42f);
        [SerializeField] float skyExposure = 1.45f;
        [SerializeField] Color crystalGlow = new Color(0.4f, 1.8f, 1.6f);

        Color startSun, startAmbientSky, startAmbientEquator, startAmbientGround, startFog, startSkyTint;
        float startSunIntensity, startExposure;
        Material skyMaterial;
        Material crystalMaterial;

        public bool HasPlayed { get; private set; }
        public Light Sun => sun;
        public ParticleSystem Sparkles => sparkles;
        public Renderer Crystal => crystal;

        public void Play()
        {
            if (HasPlayed) return;
            HasPlayed = true;
            StartCoroutine(Transition());
        }

        void Update()
        {
            if (!HasPlayed || crystalPivot == null) return;
            crystalPivot.Rotate(0f, 40f * Time.deltaTime, 0f, Space.World);
        }

        IEnumerator Transition()
        {
            CaptureStart();
            if (sparkles != null) sparkles.Play();

            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                Apply(Mathf.SmoothStep(0f, 1f, elapsed / duration));
                yield return null;
            }
            Apply(1f);
        }

        void CaptureStart()
        {
            if (sun != null)
            {
                startSun = sun.color;
                startSunIntensity = sun.intensity;
            }
            startAmbientSky = RenderSettings.ambientSkyColor;
            startAmbientEquator = RenderSettings.ambientEquatorColor;
            startAmbientGround = RenderSettings.ambientGroundColor;
            startFog = RenderSettings.fogColor;

            if (RenderSettings.skybox != null)
            {
                skyMaterial = new Material(RenderSettings.skybox);
                RenderSettings.skybox = skyMaterial;
                startSkyTint = skyMaterial.GetColor(SkyTint);
                startExposure = skyMaterial.GetFloat(Exposure);
            }
            if (crystal != null) crystalMaterial = crystal.material;
        }

        void Apply(float t)
        {
            if (sun != null)
            {
                sun.color = Color.Lerp(startSun, sunColor, t);
                sun.intensity = Mathf.Lerp(startSunIntensity, sunIntensity, t);
            }
            RenderSettings.ambientSkyColor = Color.Lerp(startAmbientSky, ambientSky, t);
            RenderSettings.ambientEquatorColor = Color.Lerp(startAmbientEquator, ambientEquator, t);
            RenderSettings.ambientGroundColor = Color.Lerp(startAmbientGround, ambientGround, t);
            RenderSettings.fogColor = Color.Lerp(startFog, fog, t);

            if (skyMaterial != null)
            {
                skyMaterial.SetColor(SkyTint, Color.Lerp(startSkyTint, skyTint, t));
                skyMaterial.SetFloat(Exposure, Mathf.Lerp(startExposure, skyExposure, t));
            }
            if (crystalMaterial != null)
                crystalMaterial.SetColor(EmissionColor, Color.Lerp(Color.black, crystalGlow, t));
        }
    }
}
