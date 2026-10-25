using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Echoes
{
    /// <summary>
    /// Title screen: Continue loads the saved game (enabled only when a valid save or backup exists), New Game starts clean,
    /// Quit exits. The camera sways gently over the backdrop.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] Button continueButton;
        [SerializeField] Button playButton;   // "New Game"; the name is kept from Phase 1
        [SerializeField] Button quitButton;
        [SerializeField] Text saveStatusText;
        [SerializeField] Transform cameraRig;

        Vector3 cameraBasePosition;

        public Button ContinueButton => continueButton;
        public Button PlayButton => playButton;
        public Button QuitButton => quitButton;
        public string SaveMessage => saveStatusText != null ? saveStatusText.text : string.Empty;

        void Awake()
        {
            GameFlow.EnterMenuMode();
            UiInputSetup.EnsureEventSystem();
            QualitySettings.vSyncCount = 1;

            if (continueButton != null) continueButton.onClick.AddListener(Continue);
            if (playButton != null) playButton.onClick.AddListener(NewGame);
            if (quitButton != null) quitButton.onClick.AddListener(Quit);
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

        public void NewGame()
        {
            GameLaunch.Mode = LaunchMode.New;
            SceneManager.LoadScene(SceneNames.Valley);
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
