using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Echoes.EditorTools
{
    /// <summary>
    /// EDITOR PREVIEW of the B3 2.5D pilot scene (diagnostic only, never Windows-player evidence): renders the fixed gameplay camera at a
    /// few places with the sprite presenter stepped by hand, to choose camera and lighting values before a player build.
    /// </summary>
    public static class Phase52B3Preview
    {
        public static void Capture()
        {
            string dir = Argument("-echoesPreviewDir");
            if (string.IsNullOrEmpty(dir)) throw new IOException("-echoesPreviewDir required");
            Directory.CreateDirectory(dir);
            ShaderUtil.allowAsyncCompilation = false;
            EditorSceneManager.OpenScene(Phase52B3Builder.Valley, OpenSceneMode.Single);
            var pilot = UnityEngine.Object.FindFirstObjectByType<Phase52Pilot>();
            pilot.Apply(false);
            UnityEngine.Object.FindFirstObjectByType<Phase52B3Pilot>().Apply(false);
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            var presenter = player.GetComponentInChildren<CharacterVisual2D5D>(true);
            var follow = UnityEngine.Object.FindFirstObjectByType<FixedFollowCamera25D>();
            Camera camera = follow.GetComponent<Camera>();
            Shader.SetGlobalVector("_EchoesFocus", Vector4.zero);
            Physics.SyncTransforms();
            float cy = follow.Yaw;
            var shots = new (string name, Vector2 xz, float yaw, float zoom, bool run)[]
            {
                ("plaza-front", new Vector2(11.0f, -1.6f), cy + 180f, 9.5f, false),
                ("plaza-side-run", new Vector2(11.0f, -1.6f), cy - 90f, 9.5f, true),
                ("plaza-rear-closest", new Vector2(11.0f, -1.6f), cy, 6.5f, false),
                ("mira", new Vector2(12.6f, 3.4f), cy + 60f, 9.5f, false),
                ("workshop", new Vector2(21.0f, 7.0f), 90f, 9.5f, false),
                ("willow", new Vector2(15.4f, 10.2f), cy + 120f, 9.5f, true),
                ("windmill", new Vector2(4.7f, 24.0f), cy + 180f, 9.5f, false),
                ("garden", new Vector2(10.4f, -11.4f), 150f, 9.5f, false),
            };
            foreach (var s in shots)
            {
                Vector3 at = new Vector3(s.xz.x, 0f, s.xz.y);
                at.y = pilot.Terrain.SampleHeight(at) + pilot.Terrain.transform.position.y + 0.05f;
                player.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, s.yaw, 0f));
                follow.SetZoom(s.zoom);
                follow.Snap();
                Physics.SyncTransforms();
                presenter.Step(1f / 60f, camera);
                if (s.run)
                {
                    Vector3 heading = player.transform.forward;
                    for (int i = 0; i < 9; i++) { player.transform.position += heading * (4.5f / 30f); presenter.Step(1f / 30f, camera); }
                    follow.Snap();
                }
                foreach (var npc in UnityEngine.Object.FindObjectsByType<NpcVisual2D5D>(FindObjectsSortMode.None)) npc.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);
                Shader.SetGlobalVector("_EchoesFocus", new Vector4(at.x, at.y + 1.05f, at.z, 1f));
                var card = presenter.GetComponentInChildren<Sprite25DQuad>(true);
                Debug.Log("[Echoes][P52 B3 PREVIEW] " + s.name + " dir=" + presenter.Direction + " shown=" + presenter.ShownDirection + " frame=" + presenter.FrameIndex + " running=" + presenter.Running
                    + " uv=" + card.Uv.ToString("F4") + " rect=" + card.Rect.ToString("F3") + " sheetDirs=" + (presenter.Sheet != null ? presenter.Sheet.directions.Length : -1)
                    + " atlas=" + (presenter.Sheet != null && presenter.Sheet.atlas != null ? presenter.Sheet.atlas.width + "x" + presenter.Sheet.atlas.height : "null")
                    + " material=" + card.Renderer.sharedMaterial.name + " shader=" + card.Renderer.sharedMaterial.shader.name + " supported=" + card.Renderer.sharedMaterial.shader.isSupported
                    + " cardPos=" + card.transform.position.ToString("F2") + " cam=" + camera.transform.position.ToString("F2") + " visibleBounds=" + card.Renderer.bounds);
                Render(camera, Path.Combine(dir, "b3-preview-" + s.name + ".png"));
            }
            // camera candidates at two places, to choose the composition (pitch, distance, field of view)
            foreach (var c in new (float pitch, float distance, float fov)[] { (38f, 9.5f, 38f), (32f, 8.5f, 36f), (28f, 8f, 34f), (24f, 7.5f, 34f) })
            {
                Wire.Set(follow, "pitch", c.pitch);
                Wire.Set(follow, "distance", c.distance);
                Wire.Set(follow, "fieldOfView", c.fov);
                foreach (var s in new (string name, Vector2 xz, float yaw)[] { ("plaza", new Vector2(11.0f, -1.6f), cy + 150f), ("workshop", new Vector2(21.0f, 7.0f), 90f), ("willow", new Vector2(15.4f, 10.2f), cy + 120f) })
                {
                    Vector3 at = new Vector3(s.xz.x, 0f, s.xz.y);
                    at.y = pilot.Terrain.SampleHeight(at) + pilot.Terrain.transform.position.y + 0.05f;
                    player.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, s.yaw, 0f));
                    follow.SetZoom(c.distance);
                    follow.Snap();
                    Physics.SyncTransforms();
                    presenter.Step(1f / 60f, camera);
                    Shader.SetGlobalVector("_EchoesFocus", new Vector4(at.x, at.y + 1.05f, at.z, 1f));
                    Render(camera, Path.Combine(dir, "camera-p" + c.pitch + "-d" + c.distance + "-" + s.name + ".png"));
                }
            }
            File.WriteAllText(Path.Combine(dir, "NOTE.txt"), "EDITOR_PREVIEW renders of the B3 pilot scene (camera " + follow.Yaw + "/" + follow.Pitch + "). Not Windows-player evidence.\n");
        }

        static void Render(Camera camera, string file)
        {
            var rt = RenderTexture.GetTemporary(1920, 1080, 24);
            rt.antiAliasing = 2;
            camera.targetTexture = rt;
            camera.Render();
            camera.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            image.Apply();
            File.WriteAllBytes(file, image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);
            RenderTexture.active = previous;
            camera.targetTexture = null;
            RenderTexture.ReleaseTemporary(rt);
        }

        static string Argument(string name) { string[] args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
    }
}
