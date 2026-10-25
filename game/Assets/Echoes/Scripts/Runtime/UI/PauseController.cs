using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Echoes
{
    /// <summary>Esc toggles the pause menu. Pausing freezes time and frees the cursor; resuming restores both.</summary>
    public class PauseController : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Button resumeButton;
        [SerializeField] Button mainMenuButton;
        [SerializeField] Button quitButton;

        public bool IsPaused => GameFlow.IsPaused;
        public GameObject Panel => panel;
        public Button ResumeButton => resumeButton;
        public Button MainMenuButton => mainMenuButton;
        public Button QuitButton => quitButton;

        void Awake()
        {
            if (panel != null) panel.SetActive(false);
            if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(GoToMainMenu);
            if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
        }

        void Update()
        {
            if (GameInput.PausePressed) Toggle();
        }

        void OnDestroy()
        {
            // Never leave a frozen clock behind when this scene goes away.
            if (GameFlow.IsPaused) GameFlow.SetPaused(false);
        }

        public void Toggle() => SetPaused(!GameFlow.IsPaused);

        public void Resume() => SetPaused(false);

        public void SetPaused(bool paused)
        {
            GameFlow.SetPaused(paused);
            if (panel != null) panel.SetActive(paused);
            if (paused && resumeButton != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
        }

        public void GoToMainMenu()
        {
            GameFlow.EnterMenuMode();
            SceneManager.LoadScene(SceneNames.MainMenu);
        }

        public void QuitGame()
        {
            GameFlow.SetPaused(false);
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
