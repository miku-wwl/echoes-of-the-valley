using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
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
            data.renderPostProcessing = false;
            data.requiresDepthOption = CameraOverrideOption.Off;
            data.requiresColorOption = CameraOverrideOption.Off;
            return camera;
        }

        /// <summary>Runtime code binds the UI module's actions (UiInputSetup), so none are serialized here.</summary>
        public static void CreateEventSystem()
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        public static Material ParticleMaterial()
        {
            RenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline;
            Material material = pipeline != null ? pipeline.defaultParticleMaterial : null;
            if (material == null)
                Debug.LogWarning("[Echoes] No default particle material from the render pipeline; particles may render pink.");
            return material;
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
