using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Echoes.Tests
{
    /// <summary>
    /// Phase 4 in the running game: the soundscape follows the restoration stage (and is heard at once from a save, without
    /// replaying a sound), every cue comes from a real change exactly once, footsteps, interface sounds, volume, pausing and the
    /// ending music, plus the stage grading, drifting clouds, flowing water and glowing windows. Sound is checked at the
    /// AudioDirector (what it was asked to play and at what volume), because a test machine may have no sound device.
    /// </summary>
    public class Phase4PlayTests : ValleyPlayTestBase
    {
        static readonly string[] Quiet = { "harp", "water", "marimba", "pad", "lead", "chimes", "birds", "sails" };

        AudioDirector director;
        Keyboard keyboard;
        InputSettings.BackgroundBehavior backgroundBehavior;
        InputSettings.EditorInputBehaviorInPlayMode editorBehavior;

        [UnitySetUp]
        public IEnumerator FindTheDirectorAndAddAKeyboard()
        {
            director = Object.FindFirstObjectByType<AudioDirector>();
            backgroundBehavior = InputSystem.settings.backgroundBehavior;
            editorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard = InputSystem.AddDevice<Keyboard>("EchoesPhase4Keyboard");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator RemoveTheKeyboard()
        {
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior = backgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode = editorBehavior;
            GameSettings.Reload();
            yield return null;
        }

        /// <summary>Moves the session to a stage by its own rules (quest accepted, item in the satchel, item delivered).</summary>
        void ReachStage(ResonanceStage stage)
        {
            Session.SetExplorationStep(GameSession.ExplorationComplete);
            if (stage >= ResonanceStage.AwakeningStream)
            {
                Assert.AreEqual(Outcome.Ok, Session.TryStartStreamQuest());
                Session.Inventory.TryAdd(ItemIds.SpringTonic, 1);
                Assert.AreEqual(Outcome.Ok, Session.TryDeliverSpringTonic());
            }
            if (stage >= ResonanceStage.BloomingGarden)
            {
                Assert.AreEqual(Outcome.Ok, Session.TryStartGardenQuest());
                Session.Inventory.TryAdd(ItemIds.Egg, 1);
                Assert.AreEqual(Outcome.Ok, Session.TryDeliverEgg());
            }
            if (stage >= ResonanceStage.RestoredValley)
            {
                Assert.AreEqual(Outcome.Ok, Session.TryStartWindmillQuest());
                Session.Inventory.TryAdd(ItemIds.WindmillBraces, 1);
                Assert.AreEqual(Outcome.Ok, Session.TryDeliverWindmillBraces());
            }
        }

        // ---- The soundscape follows the stage -----------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator TheValleyStartsSparse_EveryLayerRunning_OnlyBellsAndWindHeard()
        {
            yield return new WaitForSeconds(0.3f);
            Assert.IsNotNull(director, "the valley has an audio director");
            Assert.IsTrue(director.FollowsStage);
            Assert.AreEqual(ResonanceStage.SilentValley, director.ShownStage);
            Assert.AreEqual(10, director.LayerCount);
            for (int i = 0; i < director.LayerCount; i++)
            {
                string layer = director.LayerName(i);
                Assert.IsNotNull(director.LayerClip(i), layer + " has a clip");
                Assert.IsNotNull(director.LayerSource(i), layer + " has a source");
                Assert.IsTrue(director.LayerSource(i).loop, layer + " loops");
                Assert.IsTrue(director.LayerStarted(layer), layer + " was started with the others");
            }

            Assert.Greater(director.LayerTarget("bells"), 0.2f);
            Assert.Greater(director.LayerTarget("wind"), 0.2f);
            foreach (string layer in Quiet) Assert.AreEqual(0f, director.LayerTarget(layer), layer + " is silent in the silent valley");
            Assert.AreEqual(director.LayerTarget("bells"), director.LayerVolume("bells"), 0.001f, "a new game is heard at once");
            Assert.AreEqual(0, director.TotalCues, "nothing has happened yet, so nothing has sounded");
        }

        [UnityTest]
        public IEnumerator EachRestoration_AddsItsLayers_AndTheStreamFadesInSmoothly()
        {
            Session.SetExplorationStep(GameSession.ExplorationComplete);
            Assert.AreEqual(Outcome.Ok, Session.TryStartStreamQuest());
            Assert.AreEqual(1, director.CueCount(SoundCue.QuestStart));
            Session.Inventory.TryAdd(ItemIds.SpringTonic, 1);
            Assert.AreEqual(Outcome.Ok, Session.TryDeliverSpringTonic());
            Assert.AreEqual(1, director.CueCount(SoundCue.StreamRestored), "the stream's sound comes with the commit");
            yield return null;

            Assert.AreEqual(ResonanceStage.AwakeningStream, director.ShownStage);
            Assert.Greater(director.LayerTarget("harp"), 0.1f, "the harp joins");
            Assert.Greater(director.LayerTarget("water"), 0.1f, "and the water");
            Assert.AreEqual(0f, director.LayerTarget("marimba"));
            Assert.AreEqual(0f, director.LayerTarget("lead"));
            Assert.Less(director.LayerVolume("harp"), director.LayerTarget("harp") * 0.5f, "it fades in rather than jumping");
            yield return new WaitForSeconds(7f);
            Assert.AreEqual(director.LayerTarget("harp"), director.LayerVolume("harp"), 0.02f, "and arrives");

            Assert.AreEqual(Outcome.Ok, Session.TryStartGardenQuest());
            Session.Inventory.TryAdd(ItemIds.Egg, 1);
            Assert.AreEqual(Outcome.Ok, Session.TryDeliverEgg());
            yield return null;
            Assert.AreEqual(ResonanceStage.BloomingGarden, director.ShownStage);
            foreach (string layer in new[] { "marimba", "pad", "chimes", "birds" }) Assert.Greater(director.LayerTarget(layer), 0.05f, layer + " joins the garden");
            Assert.AreEqual(0f, director.LayerTarget("lead"), "the melody waits for the golden hour");
            Assert.AreEqual(1, director.CueCount(SoundCue.GardenRestored));

            Assert.AreEqual(Outcome.Ok, Session.TryStartWindmillQuest());
            Session.Inventory.TryAdd(ItemIds.WindmillBraces, 1);
            Assert.AreEqual(Outcome.Ok, Session.TryDeliverWindmillBraces());
            yield return null;
            Assert.AreEqual(ResonanceStage.RestoredValley, director.ShownStage);
            Assert.Greater(director.LayerTarget("lead"), 0.05f, "the melody");
            Assert.Greater(director.LayerTarget("sails"), 0.05f, "the sails turn");
            Assert.Greater(director.LayerTarget("pad"), director.StageVolume("pad", ResonanceStage.BloomingGarden) * GameSettings.MusicVolume, "a fuller pad");
            Assert.AreEqual(1, director.CueCount(SoundCue.WindmillRestored));

            Assert.AreNotEqual(Outcome.Ok, Session.TryDeliverWindmillBraces(), "a second delivery is refused");
            Assert.AreEqual(1, director.CueCount(SoundCue.WindmillRestored), "...and silent");
            Assert.AreEqual(1, director.CueCount(SoundCue.StreamRestored));
            Assert.AreEqual(3, director.CueCount(SoundCue.QuestStart), "one sound per accepted quest");
        }

        [UnityTest]
        public IEnumerator ALoadedGame_IsHeardAtOnce_AndReplaysNoSound()
        {
            ReachStage(ResonanceStage.RestoredValley);
            Assert.IsTrue(host.SaveNow());
            yield return ContinueFromSave();
            director = Object.FindFirstObjectByType<AudioDirector>();
            yield return null;
            yield return null;

            Assert.AreEqual(ResonanceStage.RestoredValley, director.ShownStage);
            for (int i = 0; i < director.LayerCount; i++)
            {
                string layer = director.LayerName(i);
                Assert.AreEqual(director.LayerTarget(layer), director.LayerVolume(layer), 0.001f, layer + " is heard at once from a save");
            }
            Assert.Greater(director.LayerVolume("lead"), 0.1f);
            Assert.AreEqual(0, director.TotalCues, "loading a save replays no sound");
            Assert.IsFalse(director.EndingMusicPlaying);
        }

        // ---- One-shot sounds come from real changes ----------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator Cues_FollowRealChanges_OncePerCommit_AndRejectionsAreSilent()
        {
            yield return GatherFrom("herb_1");
            Assert.AreEqual(1, director.CueCount(SoundCue.GatherHerb));
            Assert.AreEqual(0, director.CueCount(SoundCue.Denied));

            Node("herb_1").Interact(detector);   // pressing E again while it regrows
            Assert.AreEqual(1, director.CueCount(SoundCue.GatherHerb), "no second gather sound");
            Assert.AreEqual(1, director.CueCount(SoundCue.Denied), "the polite refusal");
            Assert.AreNotEqual(Outcome.Ok, Session.TryGather("herb_1"));
            Assert.AreEqual(1, director.CueCount(SoundCue.GatherHerb));

            Assert.AreNotEqual(Outcome.Ok, Session.TryCraft(RecipeBook.SpringTonic), "no ingredients");
            Assert.AreEqual(0, director.CueCount(SoundCue.Craft));
            Session.Inventory.TryAdd(ItemIds.Herb, 2);
            Session.Inventory.TryAdd(ItemIds.Flower, 1);
            Assert.AreEqual(Outcome.Ok, Session.TryCraft(RecipeBook.SpringTonic));
            Assert.AreEqual(1, director.CueCount(SoundCue.Craft));

            Session.Inventory.TryAdd(ItemIds.ChickenFeed, 1);
            Assert.AreEqual(Outcome.Ok, Session.TryFeedChicken());
            Assert.AreEqual(1, director.CueCount(SoundCue.FeedHen));
            Assert.AreNotEqual(Outcome.Ok, Session.TryFeedChicken(), "she is not hungry");
            Assert.AreEqual(1, director.CueCount(SoundCue.FeedHen));
            Session.Tick(GameSession.ChickenProductionSeconds + 0.5f);
            Assert.AreEqual(Outcome.Ok, Session.TryCollectEgg());
            Assert.AreEqual(1, director.CueCount(SoundCue.CollectEgg));
            Assert.AreNotEqual(Outcome.Ok, Session.TryCollectEgg(), "no second egg");
            Assert.AreEqual(1, director.CueCount(SoundCue.CollectEgg));

            yield return StandNextTo(npc);
            Assert.IsTrue(detector.TryInteract());
            dialogue.Advance();
            Assert.AreEqual(1, director.CueCount(SoundCue.DialogueTick), "a soft tick per line");
        }

        [UnityTest]
        public IEnumerator Panels_AndButtons_HaveTheirSounds_AndButtonsSwellUnderThePointer()
        {
            inventoryPanel.Open();
            Assert.AreEqual(1, director.CueCount(SoundCue.PanelOpen));
            inventoryPanel.Close();
            Assert.AreEqual(1, director.CueCount(SoundCue.PanelClose));

            pause.SetPaused(true);
            yield return null;
            Button save = pause.SaveButton;
            UiButtonFx fx = save.GetComponent<UiButtonFx>();
            Assert.IsNotNull(fx, "every button has the hover and click feel");

            int hovers = director.CueCount(SoundCue.UiHover);
            ExecuteEvents.Execute(save.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler);
            Assert.AreEqual(hovers + 1, director.CueCount(SoundCue.UiHover));
            Assert.IsTrue(fx.Highlighted);
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.Greater(fx.CurrentScale, 1.03f, "it swells under the pointer even while the game is paused");

            ExecuteEvents.Execute(save.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerExitHandler);
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.AreEqual(1f, fx.CurrentScale, 0.001f);

            int clicks = director.CueCount(SoundCue.UiClick);
            save.onClick.Invoke();
            Assert.AreEqual(clicks + 1, director.CueCount(SoundCue.UiClick));
        }

        [UnityTest]
        public IEnumerator Footsteps_PlayWhenWalking_ButNotWhenTeleported()
        {
            FootstepAudio footsteps = player.GetComponent<FootstepAudio>();
            Assert.IsNotNull(footsteps);
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(0, footsteps.Steps, "standing still is silent");

            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            yield return new WaitForSeconds(2.5f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            Assert.GreaterOrEqual(footsteps.Steps, 3, "walking makes steps");
            Assert.AreEqual(footsteps.Steps, director.CueCount(SoundCue.Footstep), "every step is one footstep cue");

            int walked = footsteps.Steps;
            yield return StandNextTo(spring);
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(walked, footsteps.Steps, "a teleport is not a walk");
        }

        // ---- Volume and ducking --------------------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator Volume_ScalesTheMix_AndTheSlidersChangeAndKeepIt()
        {
            float bells = director.StageVolume("bells", ResonanceStage.SilentValley);
            float wind = director.StageVolume("wind", ResonanceStage.SilentValley);
            yield return null;
            Assert.AreEqual(bells * GameSettings.MusicVolume, director.LayerTarget("bells"), 0.001f);

            GameSettings.MusicVolume = 0.5f;
            GameSettings.SoundVolume = 0.3f;
            yield return null;
            Assert.AreEqual(bells * 0.5f, director.LayerTarget("bells"), 0.001f, "music volume scales the music layers");
            Assert.AreEqual(wind * 0.3f, director.LayerTarget("wind"), 0.001f, "sound volume scales the ambience");
            GameSettings.MusicVolume = 0f;
            yield return null;
            Assert.AreEqual(0f, director.LayerTarget("bells"));

            pause.SetPaused(true);
            yield return null;
            VolumeControl control = pause.Panel.GetComponentInChildren<VolumeControl>(true);
            Assert.IsNotNull(control, "the pause menu has the volume sliders");
            Assert.AreEqual(0f, control.MusicSlider.value, 0.001f, "the slider shows the stored value");
            control.MusicSlider.value = 0.35f;
            Assert.AreEqual(0.35f, GameSettings.MusicVolume, 1e-4f, "dragging changes the volume at once");
            yield return new WaitForSecondsRealtime(0.9f);
            GameSettings.Reload();
            Assert.AreEqual(0.35f, GameSettings.MusicVolume, 1e-4f, "and it was written to the settings file");
        }

        [UnityTest]
        public IEnumerator Pausing_DucksTheMix_AndResumingRestoresIt()
        {
            yield return null;
            float normal = director.LayerTarget("bells");
            Assert.Greater(normal, 0.1f);

            pause.SetPaused(true);
            yield return null;
            Assert.AreEqual(normal * 0.45f, director.LayerTarget("bells"), 0.002f);

            pause.SetPaused(false);
            yield return null;
            Assert.AreEqual(normal, director.LayerTarget("bells"), 0.002f);
        }

        // ---- The ending ---------------------------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator TheEnding_PlaysItsOwnPiece_StepsTheBedsAside_AndClearsTheBanner()
        {
            yield return RestoreTheWindmill();
            hud.ShowBanner("The windmill turns again!", "A banner that would sit over the ending title", 60f);
            yield return new WaitForSeconds(0.6f);
            Assert.IsTrue(hud.IsBannerVisible, "a banner is showing");

            yield return StandNextTo(npc);
            InteractAndReadAll(npc);
            Assert.IsTrue(endingPanel.IsOpen);
            Assert.IsFalse(hud.IsBannerVisible, "the banner no longer covers the ending title");
            yield return null;

            Assert.AreEqual(1, director.CueCount(SoundCue.EndingBegins));
            Assert.IsTrue(director.EndingMusicPlaying, "the ending piece is playing");
            Assert.IsNotNull(director.EndingClip);
            Assert.Greater(director.EndingClip.length, 30f);
            float lead = director.StageVolume("lead", ResonanceStage.RestoredValley) * GameSettings.MusicVolume;
            Assert.AreEqual(lead * 0.15f, director.LayerTarget("lead"), 0.002f, "the music beds step aside for the ending piece");
            Assert.AreEqual(ResonanceStage.RestoredValley, resonanceView.ShownStage, "the ending shows the fully restored valley");
            Assert.AreEqual(ResonanceStage.RestoredValley, director.ShownStage);

            Assert.AreNotEqual(Outcome.Ok, Session.TryCompleteEnding(), "the ending happens once");
            Assert.AreEqual(1, director.CueCount(SoundCue.EndingBegins));

            endingPanel.ContinueExploring();
            yield return null;
            Assert.AreEqual(ResonanceStage.RestoredValley, director.ShownStage, "the valley stays restored after Continue Exploring");
        }

        // ---- The look -----------------------------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator TheGrading_GetsWarmerAndBrighterWithEachStage_AndTheSilentValleyIsNotDark()
        {
            yield return null;
            Assert.IsTrue(lighting.HasGradientSky);
            lighting.ApplyNow(0f, 0f, 0f, 0f);
            Assert.GreaterOrEqual(lighting.Sun.intensity, 0.9f, "the silent valley is muted but not dark");
            Color sky = RenderSettings.ambientSkyColor;
            Assert.Greater(0.2126f * sky.r + 0.7152f * sky.g + 0.0722f * sky.b, 0.3f, "its ambient light is readable");

            float[] saturation = new float[5];
            float[] bloom = new float[5];
            float[] warmth = new float[5];
            float[] sunForwardY = new float[5];
            float[][] blends = { new[] { 0f, 0f, 0f, 0f }, new[] { 1f, 0f, 0f, 0f }, new[] { 1f, 1f, 0f, 0f }, new[] { 1f, 1f, 1f, 0f }, new[] { 1f, 1f, 1f, 1f } };
            for (int i = 0; i < blends.Length; i++)
            {
                lighting.ApplyNow(blends[i][0], blends[i][1], blends[i][2], blends[i][3]);
                saturation[i] = lighting.GradedSaturation;
                bloom[i] = lighting.GradedBloom;
                warmth[i] = lighting.SkyHorizon.r - lighting.SkyHorizon.b;
                sunForwardY[i] = lighting.Sun.transform.forward.y;
            }

            for (int i = 1; i < blends.Length; i++)
            {
                Assert.Greater(saturation[i], saturation[i - 1], "colour deepens: step " + i);
                Assert.Greater(bloom[i], bloom[i - 1], "the glow grows: step " + i);
            }
            for (int i = 2; i < blends.Length; i++) Assert.Greater(warmth[i], warmth[i - 1], "the horizon warms: step " + i);
            Assert.Greater(sunForwardY[4], sunForwardY[3] + 0.1f, "the golden hour: the sun sinks towards the horizon");
            Assert.Greater(lighting.GradedFilter.r - lighting.GradedFilter.b, 0.1f, "a golden grade at the end");
        }

        [UnityTest]
        public IEnumerator Clouds_Drift_WaterFlows_AndWindowsGlowBrighterOnceRestored()
        {
            CloudDrift clouds = Object.FindFirstObjectByType<CloudDrift>();
            WaterFlow[] flows = Object.FindObjectsByType<WaterFlow>(FindObjectsSortMode.None);
            Vector2[] offsets = flows.Select(f => f.Offset).ToArray();
            float travelled = clouds.Travelled;
            yield return new WaitForSeconds(1.2f);
            Assert.Greater(clouds.Travelled, travelled, "the clouds drift");
            Assert.IsTrue(flows.Select((f, i) => f.Offset != offsets[i]).Any(moved => moved), "some water flows even in the silent valley");

            StageGlow[] glows = Object.FindObjectsByType<StageGlow>(FindObjectsSortMode.None).Where(g => g.GoalForStage(ResonanceStage.RestoredValley) > 0f).ToArray();
            Assert.GreaterOrEqual(glows.Length, 6);
            int brighter = glows.Count(g => g.GoalForStage(ResonanceStage.RestoredValley) > g.GoalForStage(ResonanceStage.SilentValley) * 2f);
            Assert.GreaterOrEqual(brighter, 4, "windows and lanterns glow brighter as the valley is restored");
        }

        [UnityTest]
        public IEnumerator TheHero_SwingsLimbsWhileWalking_AndBlinksWhileStanding()
        {
            CharacterAnimator animator = player.Animator;
            Assert.IsTrue(animator.HasLimbs);
            bool blinked = false;
            float until = Time.time + 6.5f;
            while (Time.time < until)
            {
                blinked |= animator.IsBlinking;
                yield return null;
            }
            Assert.IsTrue(blinked, "the hero blinks every few seconds");
            Assert.AreEqual(0f, animator.Speed01, 0.05f, "and stands still");

            float phase = animator.StepPhase;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            float fastest = 0f;
            until = Time.time + 1.5f;
            while (Time.time < until)
            {
                fastest = Mathf.Max(fastest, animator.Speed01);
                yield return null;
            }
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            Assert.Greater(fastest, 0.3f, "walking speeds up the animation");
            Assert.AreNotEqual(phase, animator.StepPhase, "the legs and arms swing");
        }
    }

    /// <summary>The title screen's sound, its credits and its volume controls.</summary>
    public class Phase4MenuPlayTests
    {
        AudioDirector director;
        MainMenuController menu;

        [UnitySetUp]
        public IEnumerator LoadMainMenu()
        {
            TestSaveFolder.Begin();
            yield return SceneManager.LoadSceneAsync(SceneNames.MainMenu);
            yield return null;
            yield return null;
            director = Object.FindFirstObjectByType<AudioDirector>();
            menu = Object.FindFirstObjectByType<MainMenuController>();
        }

        [UnityTearDown]
        public IEnumerator RestoreGlobalState()
        {
            GameFlow.EnterMenuMode();
            GameSettings.Reload();
            TestSaveFolder.End();
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheTitleScreen_PlaysItsWelcomeMix_WhateverTheSave()
        {
            Assert.IsNotNull(director);
            Assert.IsFalse(director.FollowsStage);
            Assert.AreEqual(5, director.LayerCount);
            for (int i = 0; i < director.LayerCount; i++)
            {
                Assert.IsTrue(director.LayerStarted(director.LayerName(i)), director.LayerName(i));
                Assert.Greater(director.LayerTarget(director.LayerName(i)), 0.02f, director.LayerName(i) + " is audible on the title screen");
            }
            Assert.AreEqual(0f, director.LayerTarget("lead"), "the tune is saved for the golden hour");
            Assert.IsFalse(director.EndingMusicPlaying);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Credits_OpenAndClose_ShowTheTruth_AndKeepTheMenuUsable()
        {
            Assert.IsFalse(menu.CreditsOpen);
            int clicks = director.CueCount(SoundCue.UiClick);
            menu.CreditsButton.onClick.Invoke();
            Assert.AreEqual(clicks + 1, director.CueCount(SoundCue.UiClick), "the button clicks");
            Assert.IsTrue(menu.CreditsOpen);
            Assert.AreEqual(GameCredits.Text, menu.CreditsLabel.text);
            Assert.IsFalse(menu.PlayButton.interactable, "the menu behind the credits is inert");
            Assert.IsFalse(menu.QuitButton.interactable);
            Assert.AreEqual("v" + Application.version, menu.VersionLabel);

            menu.CreditsCloseButton.onClick.Invoke();
            Assert.IsFalse(menu.CreditsOpen);
            Assert.IsTrue(menu.PlayButton.interactable, "and usable again");
            Assert.IsTrue(menu.QuitButton.interactable);
            Assert.IsTrue(menu.CreditsButton.interactable);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheTitleVolumeSliders_ChangeTheMixAtOnce()
        {
            VolumeControl control = Object.FindFirstObjectByType<VolumeControl>();
            Assert.IsNotNull(control);
            float bells = director.StageVolume("bells", ResonanceStage.SilentValley);
            control.MusicSlider.value = 0.25f;
            yield return null;
            Assert.AreEqual(bells * 0.25f, director.LayerTarget("bells"), 0.001f);
            control.SoundSlider.value = 0f;
            Assert.AreEqual(0f, GameSettings.SoundVolume, 1e-4f);
            yield return null;
            Assert.AreEqual(0f, director.LayerTarget("wind"), 0.001f);
        }
    }
}
