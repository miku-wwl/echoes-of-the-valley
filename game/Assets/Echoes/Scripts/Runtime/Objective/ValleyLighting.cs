using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// The only writer of the valley's mood (sun, ambient, fog, sky). It captures the muted scene values once and composes
    /// independent contributions every time they change: the Heart payoff (muted -> awakened) and three additive warm shifts,
    /// one per restoration (stream, garden, windmill). Because the shifts are additive, they can happen in any order, they
    /// never undo each other, and a loaded game can set all of them instantly.
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

        [Header("Garden blooming (added on top)")]
        [SerializeField] Color gardenSunShift = new Color(0f, -0.02f, -0.05f, 0f);
        [SerializeField] float gardenSunIntensityShift = 0.06f;
        [SerializeField] Color gardenAmbientShift = new Color(0.03f, 0.02f, -0.02f, 0f);
        [SerializeField] Color gardenFogShift = new Color(0.01f, 0f, -0.03f, 0f);
        [SerializeField] Color gardenSkyTintShift = new Color(0.03f, 0f, -0.03f, 0f);
        [SerializeField] float gardenExposureShift = 0.05f;

        [Header("Windmill turning: the golden hour (added on top)")]
        [SerializeField] Color valleySunShift = new Color(0f, -0.05f, -0.12f, 0f);
        [SerializeField] float valleySunIntensityShift = 0.05f;
        [SerializeField] float valleySunPitchShift = -10f;
        [SerializeField] Color valleyAmbientShift = new Color(0.05f, 0.02f, -0.05f, 0f);
        [SerializeField] Color valleyFogShift = new Color(0.03f, -0.01f, -0.07f, 0f);
        [SerializeField] float valleyFogDensityShift = -0.0008f;
        [SerializeField] Color valleySkyTintShift = new Color(0.06f, 0f, -0.06f, 0f);
        [SerializeField] float valleyExposureShift = 0.10f;

        Color baseSun, baseAmbientSky, baseAmbientEquator, baseAmbientGround, baseFog, baseSkyTint;
        float baseSunIntensity, baseFogDensity, baseExposure;
        Quaternion baseSunRotation;
        Material skyMaterial;
        bool captured;
        bool dirty = true;
        float heartBlend;
        float streamBlend;
        float gardenBlend;
        float valleyBlend;

        public Light Sun => sun;
        public float HeartBlend => heartBlend;
        public float StreamBlend => streamBlend;
        public float GardenBlend => gardenBlend;
        public float ValleyBlend => valleyBlend;

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

        public void SetGardenBlend(float blend)
        {
            gardenBlend = Mathf.Clamp01(blend);
            dirty = true;
        }

        public void SetValleyBlend(float blend)
        {
            valleyBlend = Mathf.Clamp01(blend);
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
                baseSunRotation = sun.transform.rotation;
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
            float g = gardenBlend;
            float v = valleyBlend;

            if (sun != null)
            {
                sun.color = Compose(baseSun, heartSun, h, Shift(streamSunShift, s, gardenSunShift, g, valleySunShift, v));
                sun.intensity = Mathf.Lerp(baseSunIntensity, heartSunIntensity, h)
                    + streamSunIntensityShift * s + gardenSunIntensityShift * g + valleySunIntensityShift * v;
                // The golden hour: the sun sinks a little, so shadows lengthen and the light turns sideways.
                sun.transform.rotation = baseSunRotation * Quaternion.Euler(valleySunPitchShift * v, 0f, 0f);
            }

            Color ambientShift = Shift(streamAmbientShift, s, gardenAmbientShift, g, valleyAmbientShift, v);
            RenderSettings.ambientSkyColor = Compose(baseAmbientSky, heartAmbientSky, h, ambientShift);
            RenderSettings.ambientEquatorColor = Compose(baseAmbientEquator, heartAmbientEquator, h, ambientShift);
            RenderSettings.ambientGroundColor = Compose(baseAmbientGround, heartAmbientGround, h, ambientShift);
            RenderSettings.fogColor = Compose(baseFog, heartFog, h, Shift(streamFogShift, s, gardenFogShift, g, valleyFogShift, v));
            RenderSettings.fogDensity = baseFogDensity + streamFogDensityShift * s + valleyFogDensityShift * v;

            if (skyMaterial != null)
            {
                skyMaterial.SetColor(SkyTintId, Compose(baseSkyTint, heartSkyTint, h, Shift(streamSkyTintShift, s, gardenSkyTintShift, g, valleySkyTintShift, v)));
                skyMaterial.SetFloat(ExposureId, Mathf.Lerp(baseExposure, heartSkyExposure, h)
                    + streamExposureShift * s + gardenExposureShift * g + valleyExposureShift * v);
            }
        }

        static Color Shift(Color stream, float s, Color garden, float g, Color valley, float v) => stream * s + garden * g + valley * v;

        static Color Compose(Color baseColor, Color heartColor, float heart, Color shift)
        {
            Color mixed = Color.Lerp(baseColor, heartColor, heart);
            return new Color(
                Mathf.Clamp01(mixed.r + shift.r),
                Mathf.Clamp01(mixed.g + shift.g),
                Mathf.Clamp01(mixed.b + shift.b),
                1f);
        }
    }
}
