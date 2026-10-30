using UnityEngine;
using System;
using System.IO;
namespace Echoes
{
    [DefaultExecutionOrder(100)]
    public sealed class Phase53MenuCredits : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void IsolatePilotSaves()
        {
            if(!Application.version.StartsWith("0.5.3-pilot",StringComparison.Ordinal))return;
            string[] args=Environment.GetCommandLineArgs();if(Array.IndexOf(args,"-echoes-smoke")>=0)return;
            int i=Array.IndexOf(args,"-echoes-save-dir");string folder=i>=0&&i+1<args.Length?args[i+1]:Path.Combine(Application.persistentDataPath,"phase53-pilot");
            Directory.CreateDirectory(folder);SaveSystem.UseFolder(folder);Debug.Log("[Echoes][P53] pilot saves isolated: "+folder);
        }
        void Start()
        {
            var menu=FindFirstObjectByType<MainMenuController>();
            if(menu!=null&&menu.CreditsLabel!=null)
            {
                const string old="No external art, music, sound or font files are used. Music and sounds are original and synthesised procedurally. The interface uses the font and sprites built into Unity.";
                menu.CreditsLabel.text=GameCredits.Text.Replace(old,"Phase 5.3 source art: ChatGPT image generation, supplied by the owner; inherited B3/B4/B5 original AI-assisted paintings retained. Original synthesised audio and Unity built-in font retained.")+"\n\nPhase 5.3 alpha processing, derived TWO-contact-pose animation, scene tools, collision and verification: OpenAI Codex. Earlier B5 pipelines: Anthropic Claude Code.\nWren/Alder retain their earlier identity and visuals (PARTIAL). Human playtest and owner approval PENDING.";
            }
        }
    }
}
