using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Echoes
{
    /// <summary>Guarantees one EventSystem whose Input System UI module has its actions bound (mouse clicks, arrows, Enter).</summary>
    public static class UiInputSetup
    {
        public static EventSystem EnsureEventSystem()
        {
            EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
                eventSystem = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();

            InputSystemUIInputModule module = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (module == null)
                module = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();

            if (module.point == null || module.point.action == null)
                module.AssignDefaultActions();

            return eventSystem;
        }
    }
}
