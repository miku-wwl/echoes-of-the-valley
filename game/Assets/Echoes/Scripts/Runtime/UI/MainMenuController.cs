using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Echoes
{
    /// <summary>Title screen: Play loads the Valley, Quit exits. The camera sways gently over the backdrop.</summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] Button playButton;
        [SerializeField] Button quitButton;
        [SerializeField] Transform cameraRig;

        Vector3 cameraBasePosition;

        public Button PlayButton => playButton;
        public Button QuitButton => quitButton;

        void Awake()
        {
            GameFlow.EnterMenuMode();
            UiInputSetup.EnsureEventSystem();
            QualitySettings.vSyncCount = 1;

            if (playButton != null) playButton.onClick.AddListener(Play);
            if (quitButton != null) quitButton.onClick.AddListener(Quit);
            if (cameraRig != null) cameraBasePosition = cameraRig.position;
            Debug.Log("[Echoes] MainMenu ready.");
        }

        void Start()
        {
            if (playButton != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(playButton.gameObject);
        }

        void Update()
        {
            if (cameraRig == null) return;
            float t = Time.unscaledTime;
            cameraRig.position = cameraBasePosition + new Vector3(Mathf.Sin(t * 0.08f) * 2f, Mathf.Sin(t * 0.11f) * 0.25f, 0f);
        }

        public void Play() => SceneManager.LoadScene(SceneNames.Valley);

        public void Quit()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
