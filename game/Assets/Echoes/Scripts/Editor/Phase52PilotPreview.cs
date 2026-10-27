using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Echoes.EditorTools
{
    /// <summary>
    /// Editor-camera diagnostics of the isolated pilot scene (route, canopy, terrain paths, hero in the valley light). These stills are
    /// model/scene diagnostics, never Windows-player evidence; the sight-line fade is emulated by setting the shader's global focus point.
    /// </summary>
    public static class Phase52PilotPreview
    {
        static string Argument(string name, string fallback)
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
        }

        public static void Capture()
        {
            ShaderUtil.allowAsyncCompilation = false;
            string folder = Path.GetFullPath(Argument("-echoesPreviewDir", Path.Combine(Application.dataPath, "../../artifacts/visual-qa/phase-5.2-b2/unity-preview")));
            Directory.CreateDirectory(folder);
            EditorSceneManager.OpenScene(Phase52PilotBuilder.Valley);
            var pilot = UnityEngine.Object.FindFirstObjectByType<Phase52Pilot>();
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            var camera = Camera.main;
            Vector3 Ground(float x, float z) { var v = new Vector3(x, 0, z); v.y = pilot.Terrain.SampleHeight(v) + pilot.Terrain.transform.position.y + .02f; return v; }
            var walk = AssetDatabase.LoadAssetAtPath<AnimationClip>(Phase52HeroBuilder.Folder + "/Animations/Walk.anim");
            var skin = pilot.SkinnedVisual.GetComponent<SkinnedMeshRenderer>();
            var snapshot = new GameObject("PoseSnapshot", typeof(MeshFilter), typeof(MeshRenderer)); var baked = new Mesh();
            void PoseWalk(float phase)
            {
                walk.SampleAnimation(pilot.SkinnedVisual, walk.length * phase); skin.BakeMesh(baked, true);
                snapshot.GetComponent<MeshFilter>().sharedMesh = baked; var mr = snapshot.GetComponent<MeshRenderer>(); mr.sharedMaterials = skin.sharedMaterials;
                snapshot.transform.SetPositionAndRotation(pilot.SkinnedVisual.transform.position, pilot.SkinnedVisual.transform.rotation); skin.enabled = false;
            }
            void Place(Vector3 at, float yaw) { player.transform.position = at; player.transform.rotation = Quaternion.Euler(0, yaw, 0); snapshot.transform.SetPositionAndRotation(player.transform.position, player.transform.rotation); }
            void Follow(string name, Vector3 at, float yawFacing, float cameraYaw, float pitch, float distance = 6f, bool baseline = false)
            {
                pilot.Apply(baseline); Place(at, yawFacing);
                Quaternion rot = Quaternion.Euler(pitch, cameraYaw, 0); Vector3 pivot = at + Vector3.up * 1.5f;
                Shot(name, pivot + rot * Vector3.back * distance, pivot - Vector3.up * .2f, 1280, 720, at + Vector3.up * 1.05f);
            }
            void Shot(string name, Vector3 from, Vector3 target, int width, int height, Vector3? focus = null)
            {
                camera.transform.SetPositionAndRotation(from, Quaternion.LookRotation(target - from));
                Shader.SetGlobalVector("_EchoesFocus", focus.HasValue ? new Vector4(focus.Value.x, focus.Value.y, focus.Value.z, 1) : Vector4.zero);
                var rt = RenderTexture.GetTemporary(width, height, 24); rt.antiAliasing = 2; camera.targetTexture = rt;
                camera.Render(); camera.Render(); RenderTexture previous = RenderTexture.active; RenderTexture.active = rt;
                var image = new Texture2D(width, height, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
                File.WriteAllBytes(Path.Combine(folder, name + ".png"), image.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(image); RenderTexture.active = previous; camera.targetTexture = null; RenderTexture.ReleaseTemporary(rt);
            }
            pilot.Apply(false);
            PoseWalk(0f); skin.enabled = false;
            // matched pairs: baseline (original terrain and trees) versus pilot
            foreach (bool baseline in new[] { true, false })
            {
                string tag = baseline ? "before-" : "after-";
                Follow(tag + "workshop", Ground(17.5f, 6.8f), 75f, 70f, 20f, 6f, baseline);
                Follow(tag + "windmill", Ground(3.2f, 24f), 10f, 8f, 20f, 6f, baseline);
                Follow(tag + "garden-path", Ground(9.6f, -9.2f), 0f, 5f, 22f, 6f, baseline);
            }
            pilot.Apply(false);
            Shot("route-overview", new Vector3(8, 34, -12), new Vector3(8, 0, 10), 1600, 900);
            // under the canopy: a pilot tree near the route, camera on the tree side
            var trees = pilot.Route.GetComponentsInChildren<MeshFilter>().Where(m => m.name.EndsWith("_Pilot")).OrderBy(m => Phase52EnvironmentBuilder.Distance(new Vector2(m.transform.position.x, m.transform.position.z))).Take(4).ToArray();
            for (int i = 0; i < trees.Length; i++)
            {
                Vector3 tree = trees[i].transform.position;
                Vector3 route = Ground(tree.x + 2.3f, tree.z - 1.2f); Vector3 heading = Vector3.ProjectOnPlane(route - tree, Vector3.up).normalized;
                Place(route, Quaternion.LookRotation(heading).eulerAngles.y);
                Vector3 cam = tree + Vector3.up * 3.0f + Quaternion.Euler(0, 40 * i, 0) * Vector3.forward * 1.2f;
                Shot("under-canopy-" + i, cam, route + Vector3.up * 1.2f, 1280, 720, route + Vector3.up * 1.05f);
                Shot("under-canopy-nofade-" + i, cam, route + Vector3.up * 1.2f, 1280, 720, null);
            }
            Debug.Log("[Echoes][P52B2] editor pilot-scene diagnostics (not player evidence): " + folder);
        }
    }
}
