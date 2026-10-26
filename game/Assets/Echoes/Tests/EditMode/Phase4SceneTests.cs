using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Echoes.Tests
{
    /// <summary>
    /// Phase 4 scene hygiene and polish: no overlapping or clipped interface, no missing materials, shaders, scripts or references,
    /// no particle system that logs errors, post-processing and the gradient sky on in both scenes, and the new visual systems
    /// (clouds, water flow, stage glow, character rigs) present.
    /// </summary>
    public class Phase4SceneTests
    {
        const string ValleyPath = "Assets/Echoes/Scenes/Valley.unity";
        const string MainMenuPath = "Assets/Echoes/Scenes/MainMenu.unity";
        static readonly string[] BothScenes = { ValleyPath, MainMenuPath };

        static T First<T>() where T : Object => Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);

        static T[] All<T>() where T : Object => Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        static Canvas CanvasNamed(string name) => All<Canvas>().First(c => c.name == name);

        /// <summary>
        /// A rectangle in the 1920 x 1080 reference space of the canvas, worked out from anchors and offsets alone so it does
        /// not depend on the size of an editor window. Origin bottom-left.
        /// </summary>
        static Rect Reference(RectTransform rect)
        {
            Rect parent = rect.parent is RectTransform up && up.GetComponent<Canvas>() == null ? Reference(up) : new Rect(0f, 0f, 1920f, 1080f);
            float x0 = parent.x + rect.anchorMin.x * parent.width + rect.offsetMin.x;
            float y0 = parent.y + rect.anchorMin.y * parent.height + rect.offsetMin.y;
            float x1 = parent.x + rect.anchorMax.x * parent.width + rect.offsetMax.x;
            float y1 = parent.y + rect.anchorMax.y * parent.height + rect.offsetMax.y;
            return Rect.MinMaxRect(x0, y0, x1, y1);
        }

        static RectTransform Find(Transform root, string path)
        {
            Transform found = root.Find(path);
            Assert.IsNotNull(found, "no '" + path + "' under " + root.name);
            return (RectTransform)found;
        }

        static void AssertNoOverlaps(Transform root, params string[] paths)
        {
            Rect[] rects = paths.Select(p => Reference(Find(root, p))).ToArray();
            for (int i = 0; i < rects.Length; i++)
            {
                Assert.GreaterOrEqual(rects[i].xMin, 0f, paths[i] + " leaves the screen on the left");
                Assert.GreaterOrEqual(rects[i].yMin, 0f, paths[i] + " leaves the screen at the bottom");
                Assert.LessOrEqual(rects[i].xMax, 1920f, paths[i] + " leaves the screen on the right");
                Assert.LessOrEqual(rects[i].yMax, 1080f, paths[i] + " leaves the screen at the top");
                for (int j = i + 1; j < rects.Length; j++)
                    Assert.IsFalse(rects[i].Overlaps(rects[j]), paths[i] + " " + rects[i] + " overlaps " + paths[j] + " " + rects[j]);
            }
        }

        // ---- Interface layout ---------------------------------------------------------------------------------------------------------

        [Test]
        public void Hud_ElementsThatCanShowTogether_NeverOverlap()
        {
            EditorSceneManager.OpenScene(ValleyPath, OpenSceneMode.Single);
            Transform hud = CanvasNamed("HUD").transform;
            AssertNoOverlaps(hud, "ObjectivePanel", "StatusPanel", "Banner", "InteractionPrompt", "Dialogue", "ControlsHint", "Toast0", "Toast1", "Toast2", "Toast3");
        }

        [Test]
        public void EndingCard_ClearsTheObjectiveAndStatusPanels_AndUsesTheCreditLines()
        {
            EditorSceneManager.OpenScene(ValleyPath, OpenSceneMode.Single);
            Transform hud = CanvasNamed("HUD").transform;
            Rect card = Reference(Find(hud, "EndingPanel/Card"));
            foreach (string other in new[] { "ObjectivePanel", "StatusPanel", "ControlsHint" })
                Assert.IsFalse(card.Overlaps(Reference(Find(hud, other))), "the ending card overlaps " + other);

            Text credits = Find(hud, "EndingPanel/Card/Credits").GetComponent<Text>();
            Assert.AreEqual(string.Join("\n", GameCredits.EndingLines), credits.text);
            Assert.LessOrEqual(credits.preferredHeight, credits.rectTransform.rect.height + 0.5f, "the credits fit their box");

            // The banner sits where the card's title would be, so the game hides it when the ending starts (tested in Play Mode).
            Rect banner = Reference(Find(hud, "Banner"));
            Assert.IsTrue(card.Overlaps(banner), "this is why GameplayHud hides the banner at the ending");
        }

        [Test]
        public void MainMenu_HasCreditsAndVolume_WiredAndHidden_AndNothingOverlaps()
        {
            EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);
            MainMenuController menu = First<MainMenuController>();
            SerializedObject serialized = new SerializedObject(menu);
            foreach (string field in new[] { "creditsButton", "creditsPanel", "creditsText", "creditsCloseButton", "versionText" })
                Assert.IsNotNull(serialized.FindProperty(field).objectReferenceValue, field);
            Assert.IsFalse(menu.CreditsOpen, "hidden until Credits is pressed");
            Assert.AreEqual("Credits", menu.CreditsButton.GetComponentInChildren<Text>().text);
            Assert.AreEqual("Close", menu.CreditsCloseButton.GetComponentInChildren<Text>().text);

            Transform root = CanvasNamed("MenuCanvas").transform;
            AssertNoOverlaps(root, "ContinueButton", "PlayButton", "CreditsButton", "QuitButton", "SaveStatus", "Footer", "Version", "VolumePanel");
            Rect plate = Reference(Find(root, "TitlePlate"));
            foreach (string below in new[] { "ContinueButton", "PlayButton", "CreditsButton", "QuitButton", "SaveStatus", "Footer", "Version", "VolumePanel" })
                Assert.IsFalse(plate.Overlaps(Reference(Find(root, below))), "the title plate overlaps " + below);

            VolumeControl volume = root.GetComponentInChildren<VolumeControl>(true);
            Assert.IsNotNull(volume);
            Assert.IsNotNull(volume.MusicSlider);
            Assert.IsNotNull(volume.SoundSlider);
        }

        [Test]
        public void CreditsScreen_FitsItsText_AndItsButtonClearsTheHint()
        {
            EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);
            Transform root = CanvasNamed("MenuCanvas").transform;
            AssertNoOverlaps(root, "CreditsPanel/Box/Title", "CreditsPanel/Box/Body", "CreditsPanel/Box/CloseButton", "CreditsPanel/Box/Hint");

            Text body = Find(root, "CreditsPanel/Box/Body").GetComponent<Text>();
            Assert.AreEqual(GameCredits.Text, body.text);
            Assert.LessOrEqual(body.preferredHeight, body.rectTransform.rect.height + 0.5f, "the whole credits fit (" + body.preferredHeight + " high)");
            Assert.IsFalse(Find(root, "CreditsPanel").gameObject.activeSelf);
        }

        [Test]
        public void PauseMenu_HasVolumeSliders_AndItsPartsDoNotOverlap()
        {
            EditorSceneManager.OpenScene(ValleyPath, OpenSceneMode.Single);
            Transform hud = CanvasNamed("HUD").transform;
            AssertNoOverlaps(hud, "PauseMenu/Panel/Title", "PauseMenu/Panel/ResumeButton", "PauseMenu/Panel/SaveButton", "PauseMenu/Panel/MainMenuButton",
                "PauseMenu/Panel/QuitButton", "PauseMenu/Panel/Volume", "PauseMenu/Panel/Hint");

            VolumeControl volume = hud.GetComponentInChildren<VolumeControl>(true);
            Assert.IsNotNull(volume);
            Assert.IsNotNull(volume.MusicSlider);
            Assert.IsNotNull(volume.SoundSlider);
            Assert.AreNotSame(volume.MusicSlider, volume.SoundSlider);
            Assert.AreEqual(0f, volume.MusicSlider.minValue);
            Assert.AreEqual(1f, volume.MusicSlider.maxValue);
            Assert.IsNotNull(volume.MusicSlider.fillRect);
            Assert.IsNotNull(volume.MusicSlider.handleRect);
        }

        [Test]
        public void TheDialogueNamePlate_ShowsTheLongestSpeakerOnOneLine()
        {
            EditorSceneManager.OpenScene(ValleyPath, OpenSceneMode.Single);
            Transform hud = CanvasNamed("HUD").transform;
            Text speaker = Find(hud, "Dialogue/NamePlate/Speaker").GetComponent<Text>();
            Assert.IsNotNull(new SerializedObject(First<DialogueBox>()).FindProperty("namePlate").objectReferenceValue);

            List<string> names = new List<string>();
            foreach (DialogueInteractable interactable in All<DialogueInteractable>())
                names.Add(new SerializedObject(interactable).FindProperty("speaker").stringValue);
            Assert.GreaterOrEqual(names.Count, 6, "three villagers, the workshop, the spring and the Heart");

            string previous = speaker.text;
            foreach (string name in names)
            {
                speaker.text = name;
                Assert.Greater(speaker.preferredHeight, 0f, name);
                Assert.LessOrEqual(speaker.preferredHeight, speaker.rectTransform.rect.height + 0.5f, "'" + name + "' is taller than the name plate, so it would be hidden");
                Assert.LessOrEqual(speaker.preferredWidth, speaker.rectTransform.rect.width + 0.5f, "'" + name + "' does not fit on the name plate in one line");
            }
            speaker.text = previous;
        }

        [Test]
        public void NoLabelInEitherScene_IsTallerThanItsBox()
        {
            // A Text with vertical overflow set to Truncate draws nothing at all when it is taller than its rectangle.
            foreach (string scene in BothScenes)
            {
                EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
                foreach (Text label in All<Text>())
                {
                    if (string.IsNullOrEmpty(label.text)) continue;
                    Assert.LessOrEqual(label.preferredHeight, label.rectTransform.rect.height + 0.5f,
                        scene + ": '" + label.name + "' (\"" + label.text + "\") is " + label.preferredHeight + " high in a box " + label.rectTransform.rect.height + " high");
                }
            }
        }

        // ---- Nothing missing -----------------------------------------------------------------------------------------------------------

        [Test]
        public void EveryRenderer_HasItsMaterials_AndEveryShaderIsSupported()
        {
            foreach (string scene in BothScenes)
            {
                EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
                foreach (Renderer renderer in All<Renderer>())
                {
                    Material[] materials = renderer.sharedMaterials;
                    Assert.Greater(materials.Length, 0, scene + ": " + renderer.name + " has no material");
                    foreach (Material material in materials)
                    {
                        Assert.IsNotNull(material, scene + ": " + renderer.name + " has a missing material");
                        Assert.IsNotNull(material.shader, scene + ": " + material.name + " has no shader");
                        Assert.AreNotEqual("Hidden/InternalErrorShader", material.shader.name, scene + ": " + material.name + " renders pink");
                        Assert.IsTrue(material.shader.isSupported, scene + ": " + material.name + " uses an unsupported shader (" + material.shader.name + ")");
                    }
                }
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Echoes" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                Assert.IsNotNull(material.shader, path);
                Assert.AreNotEqual("Hidden/InternalErrorShader", material.shader.name, path);
            }
        }

        [Test]
        public void TheTerrainLayers_HaveTheirTextures()
        {
            EditorSceneManager.OpenScene(ValleyPath, OpenSceneMode.Single);
            Terrain terrain = Terrain.activeTerrain;
            Assert.IsNotNull(terrain);
            Assert.GreaterOrEqual(terrain.terrainData.terrainLayers.Length, 3, "grass, path and stone at least");
            foreach (TerrainLayer layer in terrain.terrainData.terrainLayers)
            {
                Assert.IsNotNull(layer, "a terrain layer is missing");
                Assert.IsNotNull(layer.diffuseTexture, layer.name + " has no texture");
            }
        }

        [Test]
        public void NoScript_AndNoReference_IsMissing_InEitherScene()
        {
            foreach (string scene in BothScenes)
            {
                EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
                foreach (GameObject go in All<Transform>().Select(t => t.gameObject))
                    Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go), scene + ": " + go.name + " has a missing script");

                foreach (MonoBehaviour behaviour in All<MonoBehaviour>())
                {
                    if (behaviour == null) continue;   // a missing script, reported above
                    if (behaviour.GetType().Namespace != "Echoes") continue;
                    SerializedProperty property = new SerializedObject(behaviour).GetIterator();
                    while (property.NextVisible(true))
                    {
                        if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                        Assert.IsFalse(property.objectReferenceValue == null && property.objectReferenceInstanceIDValue != 0,
                            scene + ": " + behaviour.GetType().Name + " on " + behaviour.name + " has a missing reference in " + property.propertyPath);
                    }
                }
            }
        }

        [Test]
        public void NoParticleSystem_MixesCurveModesInItsVelocity()
        {
            // Unity logs "Particle Velocity curves must all be in the same mode" every time such a system starts.
            foreach (string scene in BothScenes)
            {
                EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
                foreach (ParticleSystem system in All<ParticleSystem>())
                {
                    ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
                    if (velocity.enabled)
                    {
                        Assert.AreEqual(velocity.x.mode, velocity.y.mode, scene + ": " + system.name + " velocity x and y");
                        Assert.AreEqual(velocity.x.mode, velocity.z.mode, scene + ": " + system.name + " velocity x and z");
                    }
                    ParticleSystem.ForceOverLifetimeModule force = system.forceOverLifetime;
                    if (force.enabled)
                    {
                        Assert.AreEqual(force.x.mode, force.y.mode, scene + ": " + system.name + " force x and y");
                        Assert.AreEqual(force.x.mode, force.z.mode, scene + ": " + system.name + " force x and z");
                    }
                }
            }
        }

        // ---- The look ----------------------------------------------------------------------------------------------------------------------

        [Test]
        public void BothScenes_HavePostProcessing_AndTheGradientSky()
        {
            foreach (string scene in BothScenes)
            {
                EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
                Volume[] volumes = All<Volume>();
                Assert.AreEqual(1, volumes.Length, scene + ": one global volume");
                Assert.IsTrue(volumes[0].isGlobal);
                Assert.IsNotNull(volumes[0].sharedProfile);
                Assert.IsTrue(volumes[0].sharedProfile.Has<Bloom>(), scene + ": bloom");
                Assert.IsTrue(volumes[0].sharedProfile.Has<Vignette>(), scene + ": vignette");
                Assert.IsTrue(volumes[0].sharedProfile.Has<ColorAdjustments>(), scene + ": colour grading");

                Camera camera = Camera.main;
                Assert.IsNotNull(camera, scene);
                Assert.IsTrue(camera.GetUniversalAdditionalCameraData().renderPostProcessing, scene + ": the camera renders post-processing");
                Assert.IsNotNull(RenderSettings.skybox, scene);
                Assert.AreEqual("Echoes/GradientSky", RenderSettings.skybox.shader.name, scene);
            }

            EditorSceneManager.OpenScene(ValleyPath, OpenSceneMode.Single);
            ValleyLighting lighting = First<ValleyLighting>();
            Assert.IsNotNull(lighting.Volume, "ValleyLighting drives the volume");
            Assert.IsNotNull(lighting.Sun);
        }

        [Test]
        public void TheValley_HasCloudsFlowingWaterGlowingWindowsAndRiggedCharacters()
        {
            EditorSceneManager.OpenScene(ValleyPath, OpenSceneMode.Single);
            CloudDrift clouds = First<CloudDrift>();
            Assert.IsNotNull(clouds);
            Assert.GreaterOrEqual(clouds.transform.childCount, 5, "a sky with some clouds in it");
            Assert.GreaterOrEqual(All<WaterFlow>().Length, 2, "the spring and the stream flow");
            Assert.GreaterOrEqual(All<StageGlow>().Length, 6, "lit windows and lanterns");

            foreach (string prefab in new[] { "Hero", "Villager", "GardenKeeper", "WindmillRestorer" })
            {
                GameObject character = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Echoes/Prefabs/" + prefab + ".prefab");
                Assert.IsNotNull(character, prefab);
                CharacterAnimator animator = character.GetComponent<CharacterAnimator>();
                Assert.IsNotNull(animator, prefab + " animates");
                Assert.IsTrue(animator.HasLimbs, prefab + " has arms and legs to swing");
                Assert.GreaterOrEqual(character.GetComponentsInChildren<Renderer>().Length, 12, prefab + " is more than a few shapes");
            }
        }
    }
}
