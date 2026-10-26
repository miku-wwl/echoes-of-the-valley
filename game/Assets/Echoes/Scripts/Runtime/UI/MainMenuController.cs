using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Echoes
{
    /// <summary>
    /// Title screen: Continue loads the saved game (enabled only when a valid save or backup exists), New Game starts clean,
    /// Quit exits. New Game asks first when a save exists on disk, because the new game's first save replaces it.
    /// The camera sways gently over the backdrop.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] Button continueButton;
        [SerializeField] Button playButton;   // "New Game"; the name is kept from Phase 1
        [SerializeField] Button quitButton;
        [SerializeField] Text saveStatusText;
        [SerializeField] Transform cameraRig;
        [SerializeField] GameObject confirmPanel;
        [SerializeField] Text confirmText;
        [SerializeField] Button confirmButton;
        [SerializeField] Button cancelButton;

        Vector3 cameraBasePosition;

        public Button ContinueButton => continueButton;
        public Button PlayButton => playButton;
        public Button QuitButton => quitButton;
        public Button ConfirmButton => confirmButton;
        public Button CancelButton => cancelButton;
        public bool ConfirmationOpen => confirmPanel != null && confirmPanel.activeSelf;
        public string ConfirmationMessage => confirmText != null ? confirmText.text : string.Empty;
        public string SaveMessage => saveStatusText != null ? saveStatusText.text : string.Empty;

        void Awake()
        {
            GameFlow.EnterMenuMode();
            UiInputSetup.EnsureEventSystem();
            QualitySettings.vSyncCount = 1;

            if (continueButton != null) continueButton.onClick.AddListener(Continue);
            if (playButton != null) playButton.onClick.AddListener(RequestNewGame);
            if (quitButton != null) quitButton.onClick.AddListener(Quit);
            if (confirmButton != null) confirmButton.onClick.AddListener(NewGame);
            if (cancelButton != null) cancelButton.onClick.AddListener(CancelNewGame);
            if (confirmPanel != null) confirmPanel.SetActive(false);
            if (cameraRig != null) cameraBasePosition = cameraRig.position;
            RefreshSaveState();
            Debug.Log("[Echoes] MainMenu ready.");
        }

        void Start()
        {
            if (EventSystem.current == null) return;
            Button first = continueButton != null && continueButton.interactable ? continueButton : playButton;
            if (first != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
        }

        void Update()
        {
            if (ConfirmationOpen && GameInput.PausePressed) CancelNewGame();
            if (cameraRig == null) return;
            float t = Time.unscaledTime;
            cameraRig.position = cameraBasePosition + new Vector3(Mathf.Sin(t * 0.08f) * 2f, Mathf.Sin(t * 0.11f) * 0.25f, 0f);
        }

        /// <summary>Re-reads the save folder and enables Continue only when a usable save exists.</summary>
        public void RefreshSaveState()
        {
            LoadResult peek = SaveSystem.Load();
            if (continueButton != null) continueButton.interactable = peek.Ok;
            if (saveStatusText == null) return;

            if (peek.Ok) saveStatusText.text = peek.UsedBackup ? peek.Message : "Saved game found.";
            else if (peek.Status == SaveStatus.Missing) saveStatusText.text = "No saved game yet.";
            else saveStatusText.text = "Saved game unusable: " + peek.Message;
        }

        /// <summary>
        /// What the New Game button does. With no save on disk it starts at once; otherwise it asks first and leaves the
        /// save alone: nothing is deleted here, and the old file is only replaced when the new game first saves.
        /// </summary>
        public void RequestNewGame()
        {
            if (confirmPanel == null || !SaveSystem.HasSaveFiles)
            {
                NewGame();
                return;
            }

            LoadResult existing = SaveSystem.Load();
            if (confirmText != null)
                confirmText.text = existing.Ok
                    ? "You have a saved game. Starting a new game replaces it the first time the new game saves. Continue keeps it until then."
                    : "A saved game file exists but cannot be loaded. It is kept aside, not deleted, when the new game first saves.";

            SetMainButtonsInteractable(false);
            confirmPanel.SetActive(true);
            // Cancel is the default choice, so a stray Enter never starts over.
            if (EventSystem.current != null && cancelButton != null) EventSystem.current.SetSelectedGameObject(cancelButton.gameObject);
        }

        public void CancelNewGame()
        {
            if (!ConfirmationOpen) return;
            confirmPanel.SetActive(false);
            SetMainButtonsInteractable(true);
            RefreshSaveState();
            Button first = continueButton != null && continueButton.interactable ? continueButton : playButton;
            if (EventSystem.current != null && first != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
        }

        /// <summary>Starts a clean game unconditionally (the confirmation, tests and the smoke run call this).</summary>
        public void NewGame()
        {
            GameLaunch.Mode = LaunchMode.New;
            SceneManager.LoadScene(SceneNames.Valley);
        }

        void SetMainButtonsInteractable(bool enabled)
        {
            if (playButton != null) playButton.interactable = enabled;
            if (quitButton != null) quitButton.interactable = enabled;
            if (continueButton != null) continueButton.interactable = enabled && continueButton.interactable;
        }

        /// <summary>Phase 1 name for starting a new game.</summary>
        public void Play() => NewGame();

        public void Continue()
        {
            if (continueButton != null && !continueButton.interactable) return;
            GameLaunch.Mode = LaunchMode.Continue;
            SceneManager.LoadScene(SceneNames.Valley);
        }

        public void Quit()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
