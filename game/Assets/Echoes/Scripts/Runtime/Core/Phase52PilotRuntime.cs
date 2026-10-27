using System;
using System.IO;
using UnityEngine;

namespace Echoes
{
    /// <summary>Only attached to the pilot menu. Keeps experimental art and saves separate from RC2.</summary>
    public sealed class Phase52PilotRuntime : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void SaveIsolation()
        {
            if (!Application.version.StartsWith("0.5.2-pilot", StringComparison.Ordinal)) return;
            string[] args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args,"-echoes-smoke") >= 0) return; // Existing smoke runner owns its own isolated folder.
            int index = Array.IndexOf(args,"-echoes-save-dir");
            string folder = index >= 0 && index+1 < args.Length ? args[index+1] : Path.Combine(Application.persistentDataPath,"phase52-pilot");
            Directory.CreateDirectory(folder);
            SaveSystem.UseFolder(folder);
            Debug.Log("[Echoes][P52] pilot saves isolated: "+folder);
        }

        void Start()
        {
            var menu = GetComponent<MainMenuController>();
            if (menu == null || menu.CreditsLabel == null) return;
            const string old = "No external art, music, sound or font files are used. Music and sounds are original and synthesised procedurally. The interface uses the font and sprites built into Unity.";
            menu.CreditsLabel.text = GameCredits.Text.Replace(old,
                "Pilot B2: the original Blender-authored caretaker, its code-painted textures, toon shader, animation, foliage and path art were made with Anthropic Claude Code (first pilot: OpenAI Codex). Caretaker portrait: original OpenAI image generation supplied by the owner. Owner visual review pending. Original synthesised audio and Unity built-in font retained.");
        }
    }
}
