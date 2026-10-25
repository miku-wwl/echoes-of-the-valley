using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Echoes.Tests
{
    public class MainMenuPlayTests
    {
        [UnitySetUp]
        public IEnumerator LoadMainMenu()
        {
            yield return SceneManager.LoadSceneAsync(SceneNames.MainMenu);
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator RestoreGlobalState()
        {
            GameFlow.EnterMenuMode();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Menu_StartsWithARunningClockAndAVisibleCursor_AndUiInputIsBound()
        {
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsTrue(Cursor.visible);

            MainMenuController menu = Object.FindFirstObjectByType<MainMenuController>();
            Assert.IsNotNull(menu);
            Assert.IsNotNull(EventSystem.current, "buttons need an EventSystem");
            InputSystemUIInputModule module = EventSystem.current.GetComponent<InputSystemUIInputModule>();
            Assert.IsNotNull(module);
            Assert.IsNotNull(module.point != null ? module.point.action : null, "pointer action bound");
            Assert.IsNotNull(module.leftClick != null ? module.leftClick.action : null, "click action bound");
            Assert.AreSame(menu.PlayButton.gameObject, EventSystem.current.currentSelectedGameObject, "Play has keyboard focus");
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayButton_LoadsTheValleyInGameplayMode()
        {
            Object.FindFirstObjectByType<MainMenuController>().PlayButton.onClick.Invoke();

            float waited = 0f;
            while (Object.FindFirstObjectByType<PlayerController>() == null && waited < 20f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            yield return null;

            Assert.AreEqual(SceneNames.Valley, SceneManager.GetActiveScene().name);
            Assert.IsNotNull(Object.FindFirstObjectByType<PlayerController>());
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsFalse(Cursor.visible, "cursor is captured in the Valley");
            Assert.IsNotNull(EventSystem.current, "pause menu buttons need an EventSystem");
        }
    }
}
