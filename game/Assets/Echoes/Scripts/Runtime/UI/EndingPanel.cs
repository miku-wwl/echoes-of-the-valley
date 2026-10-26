using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Echoes
{
    /// <summary>
    /// The closing screen over the restored valley (the live world is the backdrop; the camera drifts slowly around it).
    /// It is shown by Mira's closing conversation after GameSession.TryCompleteEnding, never by a loaded save.
    /// It always offers a way out: Continue Exploring (button, Enter, E or Esc) returns control to the valley,
    /// Main Menu (button or M) goes back to the title screen. The ending was already saved when it began.
    /// </summary>
    public class EndingPanel : MonoBehaviour
    {
        const float OrbitDegreesPerSecond = 4f;

        [SerializeField] GameObject panel;
        [SerializeField] Button continueButton;
        [SerializeField] Button mainMenuButton;
        [SerializeField] ThirdPersonCamera orbitCamera;

        public bool IsOpen { get; private set; }

        /// <summary>How many times the panel has been shown since the scene loaded (a loaded save never shows it by itself).</summary>
        public int TimesShown { get; private set; }

        public GameObject Panel => panel;
        public Button ContinueButton => continueButton;
        public Button MainMenuButton => mainMenuButton;

        void Awake()
        {
            if (panel != null) panel.SetActive(false);
            if (continueButton != null) continueButton.onClick.AddListener(ContinueExploring);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }

        void Update()
        {
            if (!IsOpen) return;

            if (GameFlow.TryConsumePause() || GameFlow.TryConsumeInteract() || KeyPressed(Key.Enter) || KeyPressed(Key.Space))
            {
                ContinueExploring();
                return;
            }
            if (KeyPressed(Key.M)) ReturnToMainMenu();
        }

        void OnDestroy()
        {
            if (IsOpen) GameFlow.SetPanelOpen(false);
            if (orbitCamera != null) orbitCamera.AutoOrbitDegreesPerSecond = 0f;
        }

        public void Show()
        {
            if (IsOpen) return;
            IsOpen = true;
            TimesShown++;
            if (panel != null) panel.SetActive(true);
            GameFlow.SetPanelOpen(true);
            if (orbitCamera != null) orbitCamera.AutoOrbitDegreesPerSecond = OrbitDegreesPerSecond;
            if (continueButton != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(continueButton.gameObject);
        }

        /// <summary>Closes the screen and gives the player the restored valley back.</summary>
        public void ContinueExploring()
        {
            if (!IsOpen) return;
            IsOpen = false;
            if (panel != null) panel.SetActive(false);
            if (orbitCamera != null) orbitCamera.AutoOrbitDegreesPerSecond = 0f;
            GameFlow.SetPanelOpen(false);
        }

        public void ReturnToMainMenu()
        {
            if (!IsOpen) return;
            IsOpen = false;
            if (orbitCamera != null) orbitCamera.AutoOrbitDegreesPerSecond = 0f;
            GameFlow.EnterMenuMode();
            SceneManager.LoadScene(SceneNames.MainMenu);
        }

        static bool KeyPressed(Key key)
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard[key].wasPressedThisFrame;
        }
    }
}
