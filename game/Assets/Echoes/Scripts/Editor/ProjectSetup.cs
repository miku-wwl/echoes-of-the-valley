using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Echoes.EditorTools
{
    /// <summary>
    /// One-command project setup. Batch mode: Unity.exe -batchmode -quit -projectPath game
    /// -executeMethod Echoes.EditorTools.ProjectSetup.GenerateAll. Idempotent: assets are updated in place and
    /// both scenes are rebuilt from scratch, so running it twice never duplicates content.
    /// </summary>
    public static class ProjectSetup
    {
        [MenuItem("Echoes/Generate Project Content")]
        public static void GenerateAll()
        {
            try
            {
                Debug.Log("[Echoes] Project generation started.");
                ConfigurePlayerSettings();
                ConfigureRenderPipeline();

                MaterialLibrary materials = new MaterialLibrary();
                PrefabSet prefabs = PrefabLibrary.Build(materials);
                AssetPaths.EnsureFolder(AssetPaths.Scenes);
                MainMenuSceneBuilder.Build(materials, prefabs);
                ValleySceneBuilder.Build(materials, prefabs);
                RegisterScenes();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[Echoes] Project generation finished.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        public static void RegisterScenes()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(AssetPaths.MainMenuScene, true),
                new EditorBuildSettingsScene(AssetPaths.ValleyScene, true)
            };
        }

        static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "EchoesJam";
            PlayerSettings.productName = "Echoes of the Valley";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });
            EditorSettings.serializationMode = SerializationMode.ForceText;
        }

        /// <summary>Trims the template's URP setup for an integrated GPU (Radeon 780M): no HDR, no extra textures, no SSAO, Forward, 2 cascades.</summary>
        static void ConfigureRenderPipeline()
        {
            HashSet<UniversalRenderPipelineAsset> tuned = new HashSet<UniversalRenderPipelineAsset>();
            for (int level = 0; level < QualitySettings.count; level++)
            {
                if (QualitySettings.GetRenderPipelineAssetAt(level) is UniversalRenderPipelineAsset asset && tuned.Add(asset))
                    TuneAsset(asset);
            }
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset defaultAsset && tuned.Add(defaultAsset))
                TuneAsset(defaultAsset);

            foreach (string path in new[] { "Assets/Settings/PC_Renderer.asset", "Assets/Settings/Mobile_Renderer.asset" })
            {
                UniversalRendererData renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
                if (renderer == null) continue;

                foreach (ScriptableRendererFeature feature in renderer.rendererFeatures)
                {
                    if (feature != null && feature.name.Contains("AmbientOcclusion")) feature.SetActive(false);
                }
                SerializedObject serialized = new SerializedObject(renderer);
                SetInt(serialized, "m_RenderingMode", 0);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(renderer);
            }
            Debug.Log("[Echoes] Tuned " + tuned.Count + " URP pipeline asset(s).");
        }

        static void TuneAsset(UniversalRenderPipelineAsset asset)
        {
            SerializedObject serialized = new SerializedObject(asset);
            SetInt(serialized, "m_ShadowCascadeCount", 2);
            SetFloat(serialized, "m_ShadowDistance", 45f);
            SetInt(serialized, "m_MainLightShadowmapResolution", 2048);
            SetInt(serialized, "m_SoftShadowQuality", 1);
            SetInt(serialized, "m_MSAA", 2);
            SetBool(serialized, "m_SupportsHDR", false);
            SetBool(serialized, "m_RequireDepthTexture", false);
            SetBool(serialized, "m_RequireOpaqueTexture", false);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        static void SetInt(SerializedObject serialized, string name, int value)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null) Debug.LogWarning("[Echoes] URP property not found: " + name);
            else property.intValue = value;
        }

        static void SetFloat(SerializedObject serialized, string name, float value)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null) Debug.LogWarning("[Echoes] URP property not found: " + name);
            else property.floatValue = value;
        }

        static void SetBool(SerializedObject serialized, string name, bool value)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null) Debug.LogWarning("[Echoes] URP property not found: " + name);
            else property.boolValue = value;
        }
    }
}
