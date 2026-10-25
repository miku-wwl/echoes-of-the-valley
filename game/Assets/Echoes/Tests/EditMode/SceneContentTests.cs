using System.Collections.Generic;
using System.IO;
using System.Linq;
using Echoes.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Echoes.Tests
{
    public class SceneContentTests
    {
        const string MainMenuPath = "Assets/Echoes/Scenes/MainMenu.unity";
        const string ValleyPath = "Assets/Echoes/Scenes/Valley.unity";

        static T First<T>() where T : Object => Object.FindFirstObjectByType<T>();

        [Test]
        public void BothScenesExist()
        {
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuPath), MainMenuPath);
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(ValleyPath), ValleyPath);
        }

        [Test]
        public void BuildSettingsStartWithMainMenuThenValley()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            Assert.AreEqual(2, scenes.Length);
            Assert.AreEqual(MainMenuPath, scenes[0].path);
            Assert.AreEqual(ValleyPath, scenes[1].path);
            Assert.IsTrue(scenes[0].enabled && scenes[1].enabled);
            Assert.AreEqual(SceneNames.MainMenu, Path.GetFileNameWithoutExtension(scenes[0].path));
            Assert.AreEqual(SceneNames.Valley, Path.GetFileNameWithoutExtension(scenes[1].path));
        }

        [Test]
        public void ProjectTargetsWindowsMonoD3D11WithTheNewInputSystem()
        {
            Assert.AreEqual(ScriptingImplementation.Mono2x, PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone));
            CollectionAssert.AreEqual(new[] { GraphicsDeviceType.Direct3D11 }, PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64));

            Object settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset").First();
            int handler = new SerializedObject(settings).FindProperty("activeInputHandler").intValue;
            Assert.That(handler, Is.EqualTo(1).Or.EqualTo(2), "GameInput uses the Input System package, so it must be an active input handler");
        }

        [Test]
        public void Valley_PlayerAndCameraAreConfigured()
        {
            EditorSceneManager.OpenScene(ValleyPath, OpenSceneMode.Single);

            PlayerController player = First<PlayerController>();
            Assert.IsNotNull(player);
            CharacterController controller = player.GetComponent<CharacterController>();
            Assert.IsNotNull(controller);
            Assert.That(controller.height, Is.InRange(1.2f, 2.2f));
            Assert.Greater(controller.stepOffset, 0.1f);
            Assert.That(player.MoveSpeed, Is.InRange(2f, 8f));
            Assert.IsNotNull(player.Animator, "walk feedback");
            Assert.IsNotNull(player.RespawnPoint);
            Assert.IsNotNull(player.CameraTransform);

            ThirdPersonCamera rig = First<ThirdPersonCamera>();
            Assert.IsNotNull(rig);
            Assert.AreSame(player.transform, rig.Target);
            Assert.AreSame(rig.transform, player.CameraTransform, "movement is relative to the follow camera");
            Assert.IsTrue(rig.CompareTag("MainCamera"));
            Assert.IsNotNull(rig.GetComponent<Camera>());
            Assert.That(rig.MinPitch, Is.GreaterThan(0f).And.LessThan(rig.MaxPitch));
            Assert.Less(rig.MaxPitch, 85f);
            Assert.IsNotNull(First<Terrain>(), "ground");
        }

        [Test]
        public void Valley_HasFourInteractablesOnTheSharedFramework()
        {
            EditorSceneManager.OpenScene(ValleyPath, OpenSceneMode.Single);

            DialogueInteractable[] interactables =
            {
                First<NpcInteractable>(), First<WorkshopInteractable>(), First<SpringInteractable>(), First<HeartInteractable>()
            };
            foreach (DialogueInteractable interactable in interactables)
            {
                Assert.IsNotNull(interactable);
                Assert.IsTrue(interactable.IsWired, interactable.name + " needs dialogue and objective");
                Assert.Greater(interactable.Range, 1f, interactable.name);
                Assert.IsFalse(string.IsNullOrEmpty(interactable.Prompt), interactable.name);
            }
            Assert.GreaterOrEqual(Object.FindObjectsByType<Interactable>(FindObjectsSortMode.None).Length, 3);
            Assert.IsNotNull(First<InteractionDetector>().Hud);
        }

        [Test]
        public void Valley_HudObjectiveAndPauseAreWired()
        {
            EditorSceneManager.OpenScene(ValleyPath, OpenSceneMode.Single);

            ExplorationObjective objective = First<ExplorationObjective>();
            Assert.IsNotNull(objective);
            Assert.IsNotNull(objective.Hud);
            Assert.IsNotNull(objective.Feedback);
            Assert.IsNotNull(objective.Feedback.Sun);
            Assert.IsNotNull(objective.Feedback.Sparkles);
            Assert.IsNotNull(objective.Feedback.Crystal);
            Assert.AreEqual(ObjectiveStep.MeetVillager, objective.Step);

            PauseController pause = First<PauseController>();
            Assert.IsNotNull(pause.Panel);
            Assert.IsNotNull(pause.ResumeButton);
            Assert.IsNotNull(pause.MainMenuButton);
            Assert.IsNotNull(pause.QuitButton);
            Assert.IsFalse(pause.Panel.activeSelf, "pause menu starts hidden");

            Assert.IsNotNull(First<DialogueBox>());
            Assert.IsNotNull(First<GameBootstrap>());
            Assert.IsNotNull(First<EventSystem>());
            Assert.IsTrue(Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None).Any(t => t.text.Contains("Move")), "controls hint");
        }

        [Test]
        public void MainMenu_HasTitlePlayAndQuit()
        {
            EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);

            MainMenuController menu = First<MainMenuController>();
            Assert.IsNotNull(menu);
            Assert.IsNotNull(menu.PlayButton);
            Assert.IsNotNull(menu.QuitButton);
            Assert.IsNotNull(First<EventSystem>());
            Assert.IsNotNull(Camera.main);
            Assert.IsTrue(Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t => t.text == "Echoes of the Valley"));
        }

        [TestCase(MainMenuPath)]
        [TestCase(ValleyPath)]
        public void Scene_HasNoMissingScriptsReferencesOrShaders(string path)
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            List<string> problems = new List<string>();

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Component component in root.GetComponentsInChildren<Component>(true))
                {
                    if (component == null)
                    {
                        problems.Add("missing script under " + root.name);
                        continue;
                    }

                    SerializedProperty property = new SerializedObject(component).GetIterator();
                    while (property.NextVisible(true))
                    {
                        if (property.propertyType == SerializedPropertyType.ObjectReference
                            && property.objectReferenceValue == null && property.objectReferenceInstanceIDValue != 0)
                            problems.Add(component.name + " (" + component.GetType().Name + ")." + property.propertyPath);
                    }

                    if (component is Renderer renderer)
                    {
                        foreach (Material material in renderer.sharedMaterials)
                        {
                            if (material == null || material.shader == null || !material.shader.name.StartsWith("Universal Render Pipeline"))
                                problems.Add(renderer.name + " has material '" + (material != null ? material.name : "null") + "' without a URP shader");
                        }
                    }
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void RunningTheGeneratorAgainDoesNotDuplicateContent()
        {
            string before = Describe(ValleyPath) + " | " + Describe(MainMenuPath);
            int scenesBefore = Directory.GetFiles("Assets/Echoes/Scenes", "*.unity").Length;

            ProjectSetup.GenerateAll();

            string after = Describe(ValleyPath) + " | " + Describe(MainMenuPath);
            Assert.AreEqual(before, after);
            Assert.AreEqual(scenesBefore, Directory.GetFiles("Assets/Echoes/Scenes", "*.unity").Length);
            Assert.AreEqual(2, EditorBuildSettings.scenes.Length);
        }

        static string Describe(string path)
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int objects = scene.GetRootGameObjects().Sum(root => root.GetComponentsInChildren<Transform>(true).Length);
            int interactables = Object.FindObjectsByType<Interactable>(FindObjectsSortMode.None).Length;
            int trees = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t => t.name.StartsWith("Tree_"));
            return scene.rootCount + " roots, " + objects + " objects, " + interactables + " interactables, " + trees + " trees";
        }
    }
}
