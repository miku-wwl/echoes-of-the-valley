using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// The only writer of the valley's mood (sun, ambient, fog, sky). It captures the muted scene values once and composes
    /// two independent contributions every time they change: the Heart payoff (muted -> awakened) and the restored stream
    /// (an extra golden-hour shift added on top). Because the shift is additive, the two can happen in any order and a
    /// loaded game can set both instantly.
    /// </summary>
    public class ValleyLighting : MonoBehaviour
    {
        static readonly int SkyTintId = Shader.PropertyToID("_SkyTint");
        static readonly int ExposureId = Shader.PropertyToID("_Exposure");

        [SerializeField] Light sun;

        [Header("Heart awakened look (muted -> this)")]
        [SerializeField] Color heartSun = new Color(1f, 0.86f, 0.62f);
        [SerializeField] float heartSunIntensity = 1.35f;
        [SerializeField] Color heartAmbientSky = new Color(0.62f, 0.78f, 1f);
        [SerializeField] Color heartAmbientEquator = new Color(0.86f, 0.76f, 0.60f);
        [SerializeField] Color heartAmbientGround = new Color(0.46f, 0.40f, 0.30f);
        [SerializeField] Color heartFog = new Color(0.93f, 0.82f, 0.64f);
        [SerializeField] Color heartSkyTint = new Color(0.62f, 0.50f, 0.42f);
        [SerializeField] float heartSkyExposure = 1.45f;

        [Header("Stream restored (added on top)")]
        [SerializeField] Color streamSunShift = new Color(0f, -0.05f, -0.10f, 0f);
        [SerializeField] float streamSunIntensityShift = 0.15f;
        [SerializeField] Color streamAmbientShift = new Color(0.05f, 0.02f, -0.05f, 0f);
        [SerializeField] Color streamFogShift = new Color(0.02f, -0.02f, -0.06f, 0f);
        [SerializeField] float streamFogDensityShift = -0.0015f;
        [SerializeField] Color streamSkyTintShift = new Color(0.05f, 0f, -0.05f, 0f);
        [SerializeField] float streamExposureShift = 0.12f;

        Color baseSun, baseAmbientSky, baseAmbientEquator, baseAmbientGround, baseFog, baseSkyTint;
        float baseSunIntensity, baseFogDensity, baseExposure;
        Material skyMaterial;
        bool captured;
        bool dirty = true;
        float heartBlend;
        float streamBlend;

        public Light Sun => sun;
        public float HeartBlend => heartBlend;
        public float StreamBlend => streamBlend;

        public void SetHeartBlend(float blend)
        {
            heartBlend = Mathf.Clamp01(blend);
            dirty = true;
        }

        public void SetStreamBlend(float blend)
        {
            streamBlend = Mathf.Clamp01(blend);
            dirty = true;
        }

        void Start()
        {
            Capture();
            dirty = true;
        }

        void LateUpdate()
        {
            if (!captured || !dirty) return;
            dirty = false;
            Apply();
        }

        void Capture()
        {
            if (captured) return;
            captured = true;
            if (sun != null)
            {
                baseSun = sun.color;
                baseSunIntensity = sun.intensity;
            }
            baseAmbientSky = RenderSettings.ambientSkyColor;
            baseAmbientEquator = RenderSettings.ambientEquatorColor;
            baseAmbientGround = RenderSettings.ambientGroundColor;
            baseFog = RenderSettings.fogColor;
            baseFogDensity = RenderSettings.fogDensity;

            if (RenderSettings.skybox != null)
            {
                skyMaterial = new Material(RenderSettings.skybox);   // never edit the shared asset
                RenderSettings.skybox = skyMaterial;
                baseSkyTint = skyMaterial.GetColor(SkyTintId);
                baseExposure = skyMaterial.GetFloat(ExposureId);
            }
        }

        void Apply()
        {
            float h = heartBlend;
            float s = streamBlend;

            if (sun != null)
            {
                sun.color = Compose(baseSun, heartSun, streamSunShift, h, s);
                sun.intensity = Mathf.Lerp(baseSunIntensity, heartSunIntensity, h) + streamSunIntensityShift * s;
            }
            RenderSettings.ambientSkyColor = Compose(baseAmbientSky, heartAmbientSky, streamAmbientShift, h, s);
            RenderSettings.ambientEquatorColor = Compose(baseAmbientEquator, heartAmbientEquator, streamAmbientShift, h, s);
            RenderSettings.ambientGroundColor = Compose(baseAmbientGround, heartAmbientGround, streamAmbientShift, h, s);
            RenderSettings.fogColor = Compose(baseFog, heartFog, streamFogShift, h, s);
            RenderSettings.fogDensity = baseFogDensity + streamFogDensityShift * s;

            if (skyMaterial != null)
            {
                skyMaterial.SetColor(SkyTintId, Compose(baseSkyTint, heartSkyTint, streamSkyTintShift, h, s));
                skyMaterial.SetFloat(ExposureId, Mathf.Lerp(baseExposure, heartSkyExposure, h) + streamExposureShift * s);
            }
        }

        static Color Compose(Color baseColor, Color heartColor, Color streamShift, float heart, float stream)
        {
            Color mixed = Color.Lerp(baseColor, heartColor, heart);
            return new Color(
                Mathf.Clamp01(mixed.r + streamShift.r * stream),
                Mathf.Clamp01(mixed.g + streamShift.g * stream),
                Mathf.Clamp01(mixed.b + streamShift.b * stream),
                1f);
        }
    }
}
