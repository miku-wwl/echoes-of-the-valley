using UnityEditor;
using UnityEditor.SceneManagement;

namespace Echoes.EditorTools
{
    /// <summary>
    /// Rehearses the scripted full-game smoke run inside the Editor's Play Mode (batch mode), so logic mistakes in the scenario or
    /// the game show up before a Windows build is spent on them. Unity.exe -batchmode -projectPath game
    /// -executeMethod Echoes.EditorTools.SmokeDryRun.Run -echoes-smoke -echoes-save-dir dir -echoes-shots dir.
    /// The standalone player run is still the real test: shaders, the player's graphics and screenshots only exist there.
    /// </summary>
    public static class SmokeDryRun
    {
        [MenuItem("Echoes/Rehearse the Smoke Run (needs -echoes-smoke)")]
        public static void Run()
        {
            EditorSceneManager.OpenScene(AssetPaths.MainMenuScene, OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }
    }
}
