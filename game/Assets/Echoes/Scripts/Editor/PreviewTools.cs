using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Echoes.EditorTools
{
    /// <summary>
    /// Renders the scene cameras to PNG files without entering Play Mode, for quick visual review in batch mode.
    /// Unity.exe -batchmode -quit -projectPath game -executeMethod Echoes.EditorTools.PreviewTools.CaptureAll -echoesShots dir
    /// Overlay UI is not part of these shots (the player smoke run captures the full screen).
    /// </summary>
    public static class PreviewTools
    {
        const int Width = 1600;
        const int Height = 900;

        [MenuItem("Echoes/Capture Preview Shots")]
        public static void CaptureAll()
        {
            string directory = ReadArgument("-echoesShots")
                ?? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "artifacts", "logs", "preview-shots"));
            Directory.CreateDirectory(directory);

            EditorSceneManager.OpenScene(AssetPaths.ValleyScene, OpenSceneMode.Single);
            Camera camera = Camera.main;
            Shot(camera, directory, "valley-1-spawn", new Vector3(0f, 3.4f, -38f), new Vector3(0f, 2f, -20f));
            Shot(camera, directory, "valley-2-heart", new Vector3(-6f, 3.5f, -13f), new Vector3(0f, 2.2f, 0f));
            Shot(camera, directory, "valley-3-cottage", new Vector3(11f, 3.4f, 0f), new Vector3(24f, 2.4f, 8f));
            Shot(camera, directory, "valley-4-spring-pond", new Vector3(-8f, 4.5f, 6f), new Vector3(-23f, 0.5f, 10f));
            Shot(camera, directory, "valley-5-overview", new Vector3(0f, 40f, -52f), new Vector3(0f, 0f, 2f));
            Shot(camera, directory, "valley-6-villager", new Vector3(10f, 2.2f, 2f), new Vector3(14.2f, 1.2f, 5.4f));

            EditorSceneManager.OpenScene(AssetPaths.MainMenuScene, OpenSceneMode.Single);
            Shot(Camera.main, directory, "menu-backdrop", Camera.main.transform.position, new Vector3(0f, 4f, 22f));
            Debug.Log("[Echoes] Preview shots written to " + directory);
        }

        static void Shot(Camera camera, string directory, string name, Vector3 position, Vector3 lookAt)
        {
            camera.transform.position = position;
            camera.transform.LookAt(lookAt);

            RenderTexture target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            camera.Render();

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            Texture2D image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            image.Apply();
            File.WriteAllBytes(Path.Combine(directory, name + ".png"), image.EncodeToPNG());

            RenderTexture.active = previous;
            camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(image);
        }

        static string ReadArgument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
    }
}
