using UnityEngine;

namespace Echoes
{
    /// <summary>Entry point of the Valley scene: resets play state, captures the cursor and makes UI input work.</summary>
    public class GameBootstrap : MonoBehaviour
    {
        void Awake()
        {
            GameFlow.EnterGameplayMode();
            UiInputSetup.EnsureEventSystem();
            QualitySettings.vSyncCount = 1;
            Debug.Log("[Echoes] Valley ready.");
        }

        void OnApplicationFocus(bool focused)
        {
            // Alt-Tab releases the cursor on Windows; take it back when play resumes (the pause menu keeps it free).
            if (focused && !GameFlow.IsPaused) GameFlow.SetCursorForGameplay(true);
        }
    }
}
