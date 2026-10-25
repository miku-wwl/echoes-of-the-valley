using UnityEngine;
using UnityEngine.InputSystem;

namespace Echoes
{
    /// <summary>Keyboard and mouse polling through the Input System (the project's active input handler). All reads are null-safe.</summary>
    public static class GameInput
    {
        /// <summary>WASD / arrow keys as a vector with magnitude of at most 1.</summary>
        public static Vector2 ReadMove()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return Vector2.zero;

            float x = 0f;
            float y = 0f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) y -= 1f;

            Vector2 move = new Vector2(x, y);
            return move.sqrMagnitude > 1f ? move.normalized : move;
        }

        /// <summary>Mouse movement in pixels since the last frame.</summary>
        public static Vector2 ReadLook()
        {
            Mouse mouse = Mouse.current;
            return mouse == null ? Vector2.zero : mouse.delta.ReadValue();
        }

        public static bool InteractPressed => Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;

        public static bool PausePressed => Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;

        public static bool InventoryPressed => Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame;

        /// <summary>Number keys 1-3 (crafting shortcuts).</summary>
        public static bool DigitPressed(int digit)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return false;
            switch (digit)
            {
                case 1: return keyboard.digit1Key.wasPressedThisFrame;
                case 2: return keyboard.digit2Key.wasPressedThisFrame;
                case 3: return keyboard.digit3Key.wasPressedThisFrame;
                default: return false;
            }
        }
    }
}
