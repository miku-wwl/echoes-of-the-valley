using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// Shared play state: pause/dialogue flags, time scale, cursor mode, and a once-per-frame
    /// "E pressed" token so overlapping systems never act on the same key press twice.
    /// Each scene calls EnterGameplayMode / EnterMenuMode on load, which also clears stale statics.
    /// </summary>
    public static class GameFlow
    {
        static int interactConsumedFrame = -1;

        public static bool IsPaused { get; private set; }
        public static bool IsDialogueOpen { get; internal set; }
        public static bool GameplayInputAllowed => !IsPaused && !IsDialogueOpen;

        public static void EnterGameplayMode()
        {
            Reset();
            SetCursorForGameplay(true);
        }

        public static void EnterMenuMode()
        {
            Reset();
            SetCursorForGameplay(false);
        }

        public static void SetPaused(bool paused)
        {
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            SetCursorForGameplay(!paused);
        }

        public static void SetCursorForGameplay(bool gameplay)
        {
            Cursor.lockState = gameplay ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !gameplay;
        }

        /// <summary>True at most once per frame: E was pressed and no other system has used it yet.</summary>
        public static bool TryConsumeInteract()
        {
            if (interactConsumedFrame == Time.frameCount || !GameInput.InteractPressed) return false;
            interactConsumedFrame = Time.frameCount;
            return true;
        }

        static void Reset()
        {
            IsPaused = false;
            IsDialogueOpen = false;
            interactConsumedFrame = -1;
            Time.timeScale = 1f;
        }
    }
}
