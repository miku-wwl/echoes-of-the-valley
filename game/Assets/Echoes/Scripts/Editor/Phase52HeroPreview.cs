using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Echoes.EditorTools
{
    /// <summary>
    /// Editor-only model diagnostics: renders the baked caretaker (real shader, textures, outline, shadows) in a tiny scene under the
    /// valley's own light values. These stills are for iteration and review notes; they are NOT Windows-player evidence.
    /// </summary>
    public static class Phase52HeroPreview
    {
        static string Argument(string name, string fallback)
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
        }

        /// <summary>One Unity start for the whole loop: bake the Blender export, then render the stills.</summary>
        public static void BakeAndCapture()
        {
            Phase52HeroBuilder.Bake();
            AssetDatabase.Refresh();
            Capture();
        }

        public static void Capture()
        {
            ShaderUtil.allowAsyncCompilation = false;
            string folder = Path.GetFullPath(Argument("-echoesPreviewDir", Path.Combine(Application.dataPath, "../../artifacts/visual-qa/phase-5.2-b2/model-preview/unity")));
            string preset = Argument("-echoesPreviewLight", "valley");
            Directory.CreateDirectory(folder);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional; sun.shadows = LightShadows.Soft; sun.shadowStrength = 1f;
            var fill = new GameObject("SkyFill").AddComponent<Light>();
            fill.type = LightType.Directional; fill.shadows = LightShadows.None;
            if (preset == "golden")
            {
                sun.color = new Color(1f, .86f, .64f); sun.intensity = 1.25f; sun.transform.rotation = Quaternion.Euler(34, 38, 0);
                fill.color = new Color(.78f, .86f, 1f); fill.intensity = .28f; fill.transform.rotation = Quaternion.Euler(28, 200, 0);
                RenderSettings.ambientSkyColor = new Color(.62f, .68f, .78f); RenderSettings.ambientEquatorColor = new Color(.56f, .52f, .50f); RenderSettings.ambientGroundColor = new Color(.34f, .31f, .28f);
            }
            else
            {
                sun.color = new Color(.86f, .90f, 1f); sun.intensity = 1f; sun.transform.rotation = Quaternion.Euler(48, 38, 0);
                fill.color = new Color(.76f, .86f, 1f); fill.intensity = .22f; fill.transform.rotation = Quaternion.Euler(28, 200, 0);
                RenderSettings.ambientSkyColor = new Color(.5f, .58f, .72f); RenderSettings.ambientEquatorColor = new Color(.46f, .48f, .54f); RenderSettings.ambientGroundColor = new Color(.3f, .3f, .32f);
            }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.fog = false;

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = new Vector3(2, 1, 2);
            var groundMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")); groundMaterial.SetColor("_BaseColor", new Color(.36f, .50f, .30f)); groundMaterial.SetFloat("_Smoothness", 0);
            ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

            var hero = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Phase52HeroBuilder.PrefabPath));
            var cameraObject = new GameObject("Cam");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.62f, .72f, .80f); camera.nearClipPlane = .05f; camera.farClipPlane = 60f;

            AnimationClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AnimationClip>(Phase52HeroBuilder.Folder + "/Animations/" + name + ".anim");
            var skin = hero.GetComponent<SkinnedMeshRenderer>();
            var snapshot = new GameObject("PoseSnapshot", typeof(MeshFilter), typeof(MeshRenderer));
            var baked = new Mesh();
            void Pose(AnimationClip clip, float time)
            {
                clip.SampleAnimation(hero, time);
                skin.BakeMesh(baked, true);
                snapshot.GetComponent<MeshFilter>().sharedMesh = baked;
                var mr = snapshot.GetComponent<MeshRenderer>();
                mr.sharedMaterials = skin.sharedMaterials; mr.shadowCastingMode = ShadowCastingMode.On; mr.receiveShadows = true;
                snapshot.transform.SetPositionAndRotation(hero.transform.position, hero.transform.rotation);
                skin.enabled = false;
            }
            Pose(Clip("Idle"), 0f);
            var focus = new Vector3(0, .88f, 0);
            Shot("full-front", new Vector3(0, 1.0f, 4.4f), focus, 28, 1000, 1300);
            Shot("full-side", new Vector3(4.4f, 1.0f, 0), focus, 28, 1000, 1300);
            Shot("full-back", new Vector3(0, 1.0f, -4.4f), focus, 28, 1000, 1300);
            Shot("full-threeq", new Vector3(3.0f, 1.3f, 3.2f), focus, 28, 1000, 1300);
            Shot("game-view-behind", new Vector3(0, 3.6f, -6.0f), new Vector3(0, 1.0f, 0), 52, 1280, 720);
            Shot("face-front", new Vector3(0, 1.545f, 0.95f), new Vector3(0, 1.525f, 0), 20, 1200, 1200);
            Shot("face-threeq", new Vector3(.55f, 1.58f, .78f), new Vector3(0, 1.52f, 0), 24, 1200, 1200);
            Shot("head-back", new Vector3(0, 1.52f, -1.15f), new Vector3(0, 1.48f, 0), 24, 1200, 1200);
            AnimationClip walk = Clip("Walk");
            for (int k = 0; k < 8; k++)
            {
                Pose(walk, walk.length * k / 8f);
                Shot("run-side-" + k, new Vector3(4.2f, 1.0f, 0f), new Vector3(0, .85f, 0), 30, 760, 900);
            }
            Pose(walk, walk.length * .12f);
            Shot("run-threeq", new Vector3(2.6f, 1.2f, 3.0f), new Vector3(0, .85f, 0), 32, 1000, 1200);
            Shot("run-behind", new Vector3(0, 2.4f, -4.6f), new Vector3(0, .95f, 0), 36, 1000, 1200);
            Debug.Log("[Echoes][P52B2] editor model previews (not player evidence): " + folder);

            void Shot(string name, Vector3 from, Vector3 target, float fov, int width, int height)
            {
                camera.transform.SetPositionAndRotation(from, Quaternion.LookRotation(target - from));
                camera.fieldOfView = fov;
                var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32); rt.antiAliasing = 4;
                camera.targetTexture = rt;
                camera.Render(); camera.Render();
                RenderTexture previous = RenderTexture.active; RenderTexture.active = rt;
                var image = new Texture2D(width, height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
                File.WriteAllBytes(Path.Combine(folder, preset + "-" + name + ".png"), image.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(image); RenderTexture.active = previous; camera.targetTexture = null; RenderTexture.ReleaseTemporary(rt);
            }
        }
    }
}
