using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Echoes.EditorTools
{
    /// <summary>Pieces both scenes need: sun, environment, camera, event system and the particle material.</summary>
    internal static class SceneKit
    {
        public static Light CreateSun(Quaternion rotation, Color color, float intensity)
        {
            GameObject go = new GameObject("Sun");
            go.transform.rotation = rotation;
            Light light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.8f;
            light.shadowBias = .02f;
            light.shadowNormalBias = .10f;
            RenderSettings.sun = light;
            return light;
        }

        public static void ConfigureEnvironment(Material skybox, Color sky, Color equator, Color ground, Color fog, float fogDensity)
        {
            RenderSettings.skybox = skybox;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = sky;
            RenderSettings.ambientEquatorColor = equator;
            RenderSettings.ambientGroundColor = ground;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.fogColor = fog;
        }

        public static Camera CreateCamera(Vector3 position, Quaternion rotation, float fieldOfView)
        {
            GameObject go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            go.tag = "MainCamera";
            go.transform.SetPositionAndRotation(position, rotation);

            Camera camera = go.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.fieldOfView = fieldOfView;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 220f;
            camera.allowHDR = false;
            camera.allowMSAA = true;

            UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;   // the global Volume adds a soft bloom, a light vignette and the stage's colour grading
            data.requiresDepthOption = CameraOverrideOption.Off;
            data.requiresColorOption = CameraOverrideOption.Off;
            return camera;
        }

        /// <summary>Runtime code binds the UI module's actions (UiInputSetup), so none are serialized here.</summary>
        public static void CreateEventSystem()
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        /// <summary>Soft, alpha-blended round dots (smoke, pollen, droplets). Created by MaterialLibrary.</summary>
        public static Material ParticleMaterial() => Load("ParticleSoft");

        /// <summary>Additive round dots that glow under the bloom (fireflies, sparkles, rings of light). Created by MaterialLibrary.</summary>
        public static Material GlowMaterial() => Load("ParticleGlow");

        static Material Load(string name)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(AssetPaths.Materials + "/" + name + ".mat");
            if (material == null) Debug.LogWarning("[Echoes] Material " + name + " is missing; particles may render pink.");
            return material;
        }

        /// <summary>
        /// A global post-processing Volume with a light bloom, a soft vignette and gentle colour grading. The profile asset is kept
        /// (same GUID) and its overrides are rebuilt on every run; ValleyLighting moves the values with the restoration stages.
        /// </summary>
        public static Volume CreateVolume(string objectName, string profilePath, float bloomIntensity, float bloomThreshold,
            float vignetteIntensity, float saturation)
        {
            AssetPaths.EnsureFolder(AssetPaths.Settings);
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, profilePath);
            }
            // LoadAllAssetRepresentationsAtPath excludes hidden subassets. Volume overrides are hidden,
            // so using it accumulated orphan components on every generation.
            profile.components.Clear();
            foreach (Object old in AssetDatabase.LoadAllAssetsAtPath(profilePath))
            {
                if (!(old is VolumeComponent)) continue;
                AssetDatabase.RemoveObjectFromAsset(old);
                Object.DestroyImmediate(old, true);
            }

            Bloom bloom = profile.Add<Bloom>(true);
            bloom.intensity.value = bloomIntensity;
            bloom.threshold.value = bloomThreshold;
            bloom.scatter.value = 0.62f;
            bloom.tint.value = new Color(1f, 0.96f, 0.88f);
            bloom.highQualityFiltering.value = false;
            Vignette vignette = profile.Add<Vignette>(true);
            vignette.intensity.value = vignetteIntensity;
            vignette.smoothness.value = 0.45f;
            vignette.color.value = new Color(0.10f, 0.06f, 0.10f);
            ColorAdjustments grading = profile.Add<ColorAdjustments>(true);
            grading.saturation.value = saturation;
            grading.contrast.value = 6f;
            grading.postExposure.value = 0f;
            grading.colorFilter.value = Color.white;
            foreach (VolumeComponent component in new VolumeComponent[] { bloom, vignette, grading })
                AssetDatabase.AddObjectToAsset(component, profile);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            GameObject go = new GameObject(objectName);
            Volume volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 0f;
            volume.sharedProfile = profile;
            return volume;
        }

        public static Gradient FadeInOut(Color color, float peakAlpha)
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f), new GradientAlphaKey(peakAlpha, 0.2f),
                    new GradientAlphaKey(peakAlpha, 0.7f), new GradientAlphaKey(0f, 1f)
                });
            return gradient;
        }
    }
}
