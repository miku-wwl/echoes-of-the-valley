using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Echoes
{
    /// <summary>
    /// Scripted end-to-end pass for the standalone player. It only runs when the player is started with -echoes-smoke and
    /// plays the WHOLE game through the same public entry points the UI and input use (teleports instead of walking, and
    /// button clicks instead of mouse clicks): New Game, the opening objective, the three restorations with their gathering,
    /// crafting, feeding and deliveries, saving and Continue, the closing conversation, the ending screen, Continue Exploring
    /// and back to the menu. It never writes quest or stage state itself; the world has to answer to real interactions.
    /// It writes screenshots to the folder after -echoes-shots, saves only to an isolated folder (-echoes-save-dir or a temp
    /// folder), logs "[Echoes][SMOKE] PASS|FAIL" and exits with code 0 or 1. It cannot replace a human playtest.
    /// </summary>
    public class SmokeTestRunner : MonoBehaviour
    {
        const string Tag = "[Echoes][SMOKE] ";
        const float TimeoutSeconds = 420f;

        string shotDirectory;
        string step = "start";
        bool finished;
        float startedAt;
        int shots;
        bool measuring;
        float statSeconds;
        int statFrames;
        float statWorst;

        PlayerController player;
        ThirdPersonCamera rig;
        InteractionDetector detector;
        ExplorationObjective objective;
        CompletionFeedback feedback;
        PauseController pause;
        DialogueBox dialogue;
        HudController hud;
        GameplayHud gameplayHud;
        CraftingPanel crafting;
        InventoryPanel inventoryPanel;
        ChickenInteractable hen;
        ResonanceView view;
        ValleyLighting lighting;
        GameSessionHost host;
        NpcInteractable mira;
        GardenKeeperInteractable wren;
        WindmillRestorerInteractable alder;
        WorkshopInteractable workshop;
        SpringInteractable spring;
        HeartInteractable heart;
        EndingPanel ending;

        static bool SmokeRequested => Array.IndexOf(Environment.GetCommandLineArgs(), "-echoes-smoke") >= 0;

        /// <summary>Runs before any scene loads, so even the main menu's first save check uses the isolated folder.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void IsolateSaves()
        {
            if (!SmokeRequested) return;
            string folder = ReadArgument("-echoes-save-dir") ?? Path.Combine(Application.temporaryCachePath, "echoes-smoke-save");
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
            Directory.CreateDirectory(folder);
            SaveSystem.UseFolder(folder);
            Debug.Log(Tag + "save folder: " + folder);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoStart()
        {
            if (!SmokeRequested) return;
            GameObject host = new GameObject("SmokeTestRunner");
            DontDestroyOnLoad(host);
            host.AddComponent<SmokeTestRunner>();
        }

        void Start()
        {
            shotDirectory = ReadArgument("-echoes-shots");
            startedAt = Time.realtimeSinceStartup;
            StartCoroutine(Run());
        }

        /// <summary>Runs the scenario. Nested routines run inside the same try/catch, so any failed requirement ends the run with FAIL.</summary>
        IEnumerator Run()
        {
            Stack<IEnumerator> stack = new Stack<IEnumerator>();
            stack.Push(Steps());
            while (stack.Count > 0)
            {
                object current = null;
                string failure = null;
                bool advanced = false;
                try
                {
                    if (Time.realtimeSinceStartup - startedAt > TimeoutSeconds)
                        throw new Exception("timed out after " + TimeoutSeconds + " s");
                    if (stack.Peek().MoveNext())
                    {
                        advanced = true;
                        current = stack.Peek().Current;
                    }
                    else
                    {
                        stack.Pop();
                    }
                }
                catch (Exception e)
                {
                    failure = e.Message;
                    Debug.LogError(Tag + e);
                }

                if (failure != null)
                {
                    Finish(false, step + ": " + failure);
                    yield break;
                }
                if (!advanced) continue;
                if (current is IEnumerator nested)
                {
                    stack.Push(nested);
                    continue;
                }
                yield return current;
            }
            Finish(true, "all steps passed in " + (Time.realtimeSinceStartup - startedAt).ToString("F0") + " s, " + shots + " screenshots");
        }

        IEnumerator Steps()
        {
            Log("started: " + Application.productName + " " + Application.version + ", scene=" + SceneManager.GetActiveScene().name
                + ", " + Screen.width + "x" + Screen.height + ", " + SystemInfo.graphicsDeviceType + " / " + SystemInfo.graphicsDeviceName);
            Require(SceneManager.GetActiveScene().name == SceneNames.MainMenu, "startup scene is " + SceneManager.GetActiveScene().name + ", not MainMenu");

            yield return Section("StartANewGame", StartANewGame());
            yield return Section("MoveAndUseTheWorkshop", MoveAndUseTheWorkshop());
            yield return Section("DiscoverTheHeart", DiscoverTheHeart());
            yield return Section("AttemptsOutOfOrder", AttemptsOutOfOrder());
            yield return Section("RestoreTheStream", RestoreTheStream());
            yield return Section("SaveAndContinue", SaveAndContinue());
            yield return Section("RestoreTheGarden", RestoreTheGarden());
            yield return Section("RestoreTheWindmill", RestoreTheWindmill());
            yield return Section("TheEnding", TheEnding());
            yield return Section("ContinueExploring", ContinueExploring());
            yield return Section("ReloadAfterTheEnding", ReloadAfterTheEnding());
        }

        /// <summary>Names the part of the scenario that is running, so a failure says where it happened.</summary>
        IEnumerator Section(string name, IEnumerator body)
        {
            step = name;
            Log("section " + name + " (" + (Time.realtimeSinceStartup - startedAt).ToString("F0") + " s)");
            yield return body;
        }

        /// <summary>The Editor's batch mode has no Game view, so WaitForEndOfFrame never returns there (the player waits for it properly).</summary>
        static object EndOfFrame() => Application.isEditor && Application.isBatchMode ? null : new WaitForEndOfFrame();

        // ---- 1. Main menu and the opening ------------------------------------------------------------------------------------

        IEnumerator StartANewGame()
        {
            MainMenuController menu = FindFirstObjectByType<MainMenuController>();
            Require(menu != null, "MainMenuController missing");
            yield return new WaitForSecondsRealtime(1.5f);
            yield return EndOfFrame();
            Capture("01-main-menu");
            Require(EventSystem.current != null, "no EventSystem in MainMenu");
            Require(Time.timeScale == 1f, "time scale is not 1 in MainMenu");
            Require(Cursor.visible, "cursor is hidden in MainMenu");
            Require(menu.ContinueButton != null && !menu.ContinueButton.interactable, "Continue is enabled although no save exists");
            Require(!SaveSystem.HasSaveFiles, "the isolated save folder is not empty");
            Log("menu save status: " + menu.SaveMessage);

            menu.PlayButton.onClick.Invoke();   // the real New Game button: with no save on disk it starts at once
            Require(!menu.ConfirmationOpen, "New Game asked a question although no save exists");

            yield return WaitForValley();
            yield return new WaitForSecondsRealtime(1.5f);
            FindWorld();
            GameSession session = GameSession.Current;
            Require(session != null && !host.LoadedFromSave, "New Game should start a fresh session");
            Require(session.Coins == GameSession.StartingCoins && session.Inventory.UsedSlots == 0, "fresh session is not clean");
            Require(session.Resonance == ResonanceStage.SilentValley && session.GardenQuest == QuestStage.NotStarted
                && session.WindmillQuest == QuestStage.NotStarted && !session.EndingSeen, "fresh session has later progress");
            // The first frames after a scene load can be slow, so give the character up to 5 s to settle on the ground.
            float groundedBy = Time.realtimeSinceStartup + 5f;
            while (!player.IsGrounded && Time.realtimeSinceStartup < groundedBy) yield return null;
            Terrain ground = Terrain.activeTerrain;
            Require(player.IsGrounded, "player is not grounded after spawn (y " + player.transform.position.y.ToString("F2") + ", ground "
                + (ground != null ? (ground.SampleHeight(player.transform.position) + ground.transform.position.y).ToString("F2") : "no terrain")
                + ", frame " + Time.frameCount + ", timeScale " + Time.timeScale + ", dt " + Time.unscaledDeltaTime.ToString("F3") + " s)");
            Require(!Cursor.visible, "cursor is visible in the Valley");
            Log("cursor lockState=" + Cursor.lockState + ", window focused=" + Application.isFocused + " (informational; the OS decides focus)");
            Require(objective.Step == ObjectiveStep.MeetVillager, "objective does not start at the first step");
            Require(view.GardenDormantShown && !view.GardenBloomShown && view.WindmillDamagedShown && !view.WindmillRepairedShown
                && !view.Windmill.Spinning && !view.VillageLifeShown, "the silent valley should show the withered garden and the broken windmill");
            yield return EndOfFrame();
            Capture("02-valley-start");
        }

        IEnumerator MoveAndUseTheWorkshop()
        {
            Vector3 before = player.transform.position;
            player.InputOverride = Vector2.up;
            yield return new WaitForSecondsRealtime(1f);
            player.InputOverride = null;
            Vector3 after = player.transform.position;
            float moved = Vector2.Distance(new Vector2(before.x, before.z), new Vector2(after.x, after.z));
            Log("moved " + moved.ToString("F2") + " m in 1 s");
            Require(moved > 2f, "player moved only " + moved.ToString("F2") + " m in 1 s");

            detector.Refresh();
            Require(detector.Current == null, "an interactable is offered at the spawn area");
            Require(!detector.TryInteract(), "interaction succeeded while out of range");
            Require(!hud.IsPromptVisible, "prompt visible while out of range");

            yield return StandBeside(workshop);
            Require(ReferenceEquals(detector.Current, workshop), "workshop not selected next to it");
            Require(hud.IsPromptVisible, "no prompt next to the workshop");
            Require(detector.TryInteract() && crafting.IsOpen, "workshop did not open the crafting panel");
            Require(!GameFlow.GameplayInputAllowed && GameFlow.IsPanelOpen, "an open panel must pause movement and interaction");
            crafting.Close();
            Require(GameFlow.GameplayInputAllowed, "closing the panel did not restore control");
            Require(objective.Step == ObjectiveStep.MeetVillager, "workshop changed the objective");
        }

        IEnumerator DiscoverTheHeart()
        {
            GameSession session = GameSession.Current;
            yield return StandBeside(mira);
            Require(ReferenceEquals(detector.Current, mira), "Mira not selected next to her");
            Require(detector.TryInteract() && dialogue.IsOpen, "Mira's interaction did not open dialogue");
            yield return EndOfFrame();
            Capture("03-dialogue");
            ReadAll();
            Require(objective.Step == ObjectiveStep.InspectSpring, "talking to Mira did not advance the objective");
            Require(session.StreamQuest == StreamQuestStage.NotStarted, "the stream quest must wait for the opening objective");

            yield return StandBeside(heart);
            Require(detector.TryInteract(), "heart interaction failed");
            ReadAll();
            Require(objective.Step == ObjectiveStep.InspectSpring, "heart advanced the objective out of order");

            yield return StandBeside(spring);
            Require(ReferenceEquals(detector.Current, spring), "spring not selected next to it");
            Require(detector.TryInteract(), "spring interaction failed");
            ReadAll();
            Require(objective.Step == ObjectiveStep.ReturnToHeart, "spring did not advance the objective");

            yield return StandBeside(heart);
            Require(detector.TryInteract(), "heart interaction failed on return");
            ReadAll();
            Require(objective.IsComplete && objective.CompletionCount == 1, "objective did not complete exactly once");
            Require(feedback.HasPlayed, "completion feedback did not start");
            Require(session.ExplorationDone, "the session did not record the finished opening objective");

            yield return MeasureFrames(5f, "the Heart effect");
            yield return EndOfFrame();
            Capture("04-complete");
            Require(Mathf.Approximately(lighting.HeartBlend, 1f), "Heart lighting did not finish");
        }

        /// <summary>Every later quest and the ending refuse to start early, in the real player, and change nothing.</summary>
        IEnumerator AttemptsOutOfOrder()
        {
            GameSession session = GameSession.Current;

            yield return StandBeside(wren);
            Require(detector.TryInteract() && dialogue.IsOpen, "Wren did not answer");
            Require(dialogue.CurrentLine.Contains("asleep"), "Wren should explain that the garden sleeps: " + dialogue.CurrentLine);
            ReadAll();
            yield return StandBeside(alder);
            Require(detector.TryInteract() && dialogue.IsOpen, "Alder did not answer");
            Require(dialogue.CurrentLine.Contains("silent"), "Alder should explain that the windmill is silent: " + dialogue.CurrentLine);
            ReadAll();

            Require(session.TryStartGardenQuest() == Outcome.Locked && session.TryDeliverEgg() == Outcome.Locked
                && session.TryStartWindmillQuest() == Outcome.Locked && session.TryDeliverWindmillBraces() == Outcome.Locked
                && session.TryCompleteEnding() == Outcome.Locked, "a later quest or the ending was not locked at stage 0");
            Require(session.GardenQuest == QuestStage.NotStarted && session.WindmillQuest == QuestStage.NotStarted
                && session.Resonance == ResonanceStage.SilentValley && !session.EndingSeen, "an early attempt changed the state");
        }

        // ---- 2. The stream (stage 0 -> 1) ----------------------------------------------------------------------------------------

        IEnumerator RestoreTheStream()
        {
            GameSession session = GameSession.Current;
            yield return StandBeside(mira);
            Require(detector.TryInteract() && dialogue.IsOpen, "Mira did not speak after the objective");
            ReadAll();
            Require(session.StreamQuest == StreamQuestStage.Active, "Mira did not start the stream quest");
            Require(File.Exists(SaveSystem.MainPath), "starting the quest should autosave");

            // The spring does nothing without the tonic.
            int coinsAtStart = session.Coins;
            yield return StandBeside(spring);
            Require(detector.TryInteract(), "spring interaction failed");
            ReadAll();
            Require(session.Resonance == ResonanceStage.SilentValley && session.Coins == coinsAtStart, "the spring restored the stream without a tonic");
            Require(!view.ClearStreamShown && view.TroubledStreamShown && !view.BloomShown, "the world changed without a tonic");

            // Gathering by interaction: 2 herbs and 2 blossoms (one for the tonic, one for the feed)
            foreach (string id in new[] { "herb_1", "herb_2", "flower_1", "flower_2" }) yield return Gather(id);
            Require(session.Inventory.Count(ItemIds.Herb) == 2 && session.Inventory.Count(ItemIds.Flower) == 2, "gathered counts are wrong");

            inventoryPanel.Toggle();   // the I key calls Toggle
            yield return null;
            yield return EndOfFrame();
            Capture("05-inventory");
            Require(inventoryPanel.IsOpen && GameFlow.IsPanelOpen, "inventory did not open");
            Require(inventoryPanel.RowName(0) == "Moonleaf Herb" && inventoryPanel.RowCountText(0).StartsWith("x2"), "inventory row 0 is wrong: " + inventoryPanel.RowName(0) + " " + inventoryPanel.RowCountText(0));
            Require(inventoryPanel.RowName(1) == "Dawn Blossom" && inventoryPanel.RowCountText(1).StartsWith("x2"), "inventory row 1 is wrong");
            inventoryPanel.Toggle();
            Require(!inventoryPanel.IsOpen && GameFlow.GameplayInputAllowed, "inventory did not close");

            // Alchemy: Spring Tonic and Chicken Feed through the panel's buttons
            yield return StandBeside(workshop);
            Require(detector.TryInteract() && crafting.IsOpen, "workshop panel did not open");
            yield return null;
            yield return EndOfFrame();
            Capture("06-crafting");
            Require(crafting.CraftButton(0).interactable && crafting.CraftButton(1).interactable, "craft buttons should be enabled with the ingredients");
            Require(!crafting.CraftButton(2).interactable, "braces should not be craftable without ore");
            crafting.CraftButton(0).onClick.Invoke();   // Spring Tonic
            crafting.CraftButton(1).onClick.Invoke();   // Chicken Feed
            Require(session.Inventory.Count(ItemIds.SpringTonic) == 1 && session.Inventory.Count(ItemIds.ChickenFeed) == 3, "crafting produced the wrong items");
            Require(session.Inventory.Count(ItemIds.Herb) == 0 && session.Inventory.Count(ItemIds.Flower) == 0, "crafting did not consume the ingredients exactly once");
            Require(crafting.Craft(0) == Outcome.Insufficient, "a repeated craft should be rejected");
            Require(session.Inventory.Count(ItemIds.SpringTonic) == 1, "a rejected craft changed the satchel");
            crafting.Close();

            // The hen: feed once. She keeps laying while the rest of the game goes on.
            yield return StandBeside(hen);
            Require(ReferenceEquals(detector.Current, hen), "hen not selected next to it");
            Require(detector.TryInteract(), "feeding failed");
            Require(session.Chicken == ChickenPhase.Producing && session.Inventory.Count(ItemIds.ChickenFeed) == 2, "feeding did not start production");
            Require(detector.TryInteract() && session.Inventory.Count(ItemIds.ChickenFeed) == 2, "the hen was fed twice");
            yield return null;
            Require(hen.ProducingIconShown && !hen.HungryIconShown, "the hen does not look busy");

            // Deliver the tonic: the stream is restored
            yield return StandBeside(spring);
            Require(hud.PromptText.Contains("Pour"), "the spring does not offer the delivery: " + hud.PromptText);
            int coinsBefore = session.Coins;
            Require(detector.TryInteract() && dialogue.IsOpen, "delivering the tonic failed");
            ReadAll();
            Require(session.StreamQuest == StreamQuestStage.Completed, "the quest did not complete");
            Require(session.Resonance == ResonanceStage.AwakeningStream, "ResonanceStage did not reach 1");
            Require(session.Inventory.Count(ItemIds.SpringTonic) == 0, "the tonic was not consumed");
            Require(session.Coins == coinsBefore + GameSession.StreamQuestReward, "the one-time reward was wrong");
            yield return null;
            yield return null;
            Require(view.ShownStage == ResonanceStage.AwakeningStream && view.ClearStreamShown && view.BloomShown && view.PondIsClear && !view.TroubledStreamShown,
                "the world did not change visibly");
            Require(view.GardenDormantShown && !view.GardenBloomShown, "stage 1 must leave the garden asleep");

            yield return MeasureFrames(5f, "the stream restoration");
            yield return EndOfFrame();
            Capture("07-stream-restored");
            Require(!view.IsTransitioning && Mathf.Approximately(lighting.StreamBlend, 1f), "the restoration transition did not finish");
            Require(session.TryDeliverSpringTonic() == Outcome.WrongState, "a second delivery was not rejected");
            Require(hud.ObjectiveHeading == QuestLog.GardenHeading && hud.ObjectiveBody.Contains("Wren"),
                "the objective panel does not point to the garden: " + hud.ObjectiveHeading + " / " + hud.ObjectiveBody);
        }

        // ---- 3. Save, Continue, and the state that must survive ----------------------------------------------------------------------

        IEnumerator SaveAndContinue()
        {
            GameSession session = GameSession.Current;
            pause.SetPaused(true);
            Require(Time.timeScale == 0f && pause.IsPaused, "pause did not freeze time");
            Require(Cursor.visible, "cursor not shown while paused");
            yield return null;
            yield return EndOfFrame();
            Capture("08-pause");
            Require(pause.SaveGame(), "saving from the pause menu failed");
            Require(File.Exists(SaveSystem.MainPath) && !File.Exists(SaveSystem.TempPath), "save files are wrong after saving");
            pause.SetPaused(false);
            Require(Time.timeScale == 1f && !pause.IsPaused, "resume did not restore time");
            Require(!Cursor.visible, "cursor not hidden again after resume");

            int coinsSaved = session.Coins;
            float henLeft = session.ChickenRemaining;
            float herbLeft = session.NodeRemaining("herb_1");
            int feedSaved = session.Inventory.Count(ItemIds.ChickenFeed);
            pause.GoToMainMenu();
            yield return WaitForMainMenu();
            MainMenuController menu = FindFirstObjectByType<MainMenuController>();
            Require(menu.ContinueButton.interactable, "Continue is disabled although a save exists: " + menu.SaveMessage);
            menu.ContinueButton.onClick.Invoke();
            yield return WaitForValley();
            yield return null;
            yield return null;
            FindWorld();

            GameSession loaded = GameSession.Current;
            Require(host.LoadedFromSave && !ReferenceEquals(loaded, session), "Continue did not load the saved session");
            Require(loaded.StreamQuest == StreamQuestStage.Completed && loaded.Resonance == ResonanceStage.AwakeningStream, "the restoration was not saved");
            Require(loaded.GardenQuest == QuestStage.NotStarted && loaded.WindmillQuest == QuestStage.NotStarted && !loaded.EndingSeen, "later progress appeared from nowhere");
            Require(loaded.Coins == coinsSaved, "coins changed across the save");
            Require(loaded.Inventory.Count(ItemIds.ChickenFeed) == feedSaved && loaded.Inventory.Count(ItemIds.Egg) == 0, "inventory changed across the save");
            Require(loaded.Chicken == ChickenPhase.Producing && Mathf.Abs(loaded.ChickenRemaining - henLeft) < 1.5f, "the hen's production was not saved");
            Require(!FindNode("herb_1").IsReady && Mathf.Abs(loaded.NodeRemaining("herb_1") - herbLeft) < 1.5f && FindNode("herb_3").IsReady, "node timers were not saved");
            Require(view.ShownStage == ResonanceStage.AwakeningStream && !view.IsTransitioning && view.ClearStreamShown && view.BloomShown && view.PondIsClear,
                "the restored world was not shown at once after loading");
            Require(Mathf.Approximately(lighting.StreamBlend, 1f) && Mathf.Approximately(lighting.HeartBlend, 1f), "the restored lighting was not applied");
            Require(objective.IsComplete && objective.CompletionCount == 0, "the opening objective was not restored quietly");
            Require(loaded.TryDeliverSpringTonic() == Outcome.WrongState && loaded.Coins == coinsSaved, "loading allowed the reward again");
            yield return EndOfFrame();
            Capture("09-after-reload");
        }

        // ---- 4. The garden (stage 1 -> 2) -------------------------------------------------------------------------------------------

        IEnumerator RestoreTheGarden()
        {
            GameSession session = GameSession.Current;
            yield return StandBeside(wren);
            Require(hud.PromptText.Contains("Wren"), "Wren is not offered: " + hud.PromptText);
            Require(detector.TryInteract() && dialogue.IsOpen, "Wren did not answer");
            ReadAll();
            Require(session.GardenQuest == QuestStage.Active, "Wren did not give the garden quest");
            Require(session.Resonance == ResonanceStage.AwakeningStream, "accepting the quest changed the world");
            yield return null;
            Require(hud.ObjectiveHeading == QuestLog.GardenHeading && hud.IsBannerVisible, "the quest start was not announced");

            // The garden still sleeps; Wren without an egg only explains
            yield return ShootGarden("10-garden-asleep");
            yield return StandBeside(wren);
            Require(detector.TryInteract(), "Wren did not answer the second time");
            ReadAll();
            Require(session.GardenQuest == QuestStage.Active && session.Resonance == ResonanceStage.AwakeningStream, "Wren took something that was not there");

            // Clover has been laying since before the reload: collect her egg once it is ready
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, session.ChickenRemaining) + 1f);
            Require(session.Chicken == ChickenPhase.EggReady, "the hen did not finish laying after the reload");
            yield return null;
            Require(hen.EggShown, "the egg is not visible in the nest");
            yield return StandBeside(hen);
            Require(detector.TryInteract(), "collecting the egg failed");
            Require(session.Inventory.Count(ItemIds.Egg) == 1 && session.Chicken == ChickenPhase.Hungry, "the egg was not collected");
            // (Another E press would now feed her, which is right: she is hungry again. Ask the session directly instead.)
            Require(session.TryCollectEgg() == Outcome.WrongState && session.Inventory.Count(ItemIds.Egg) == 1, "the egg was collected twice");
            yield return null;
            Require(hud.ObjectiveBody.Contains("Give Clover's egg"), "the objective does not say who gets the egg: " + hud.ObjectiveBody);

            // Deliver it
            yield return StandBeside(wren);
            Require(hud.PromptText.Contains("egg"), "Wren does not offer to take the egg: " + hud.PromptText);
            int coins = session.Coins;
            Require(detector.TryInteract() && dialogue.IsOpen, "delivering the egg failed");
            ReadAll();
            Require(session.GardenQuest == QuestStage.Completed && session.Resonance == ResonanceStage.BloomingGarden, "ResonanceStage did not reach 2");
            Require(session.Inventory.Count(ItemIds.Egg) == 0, "the egg was not consumed");
            Require(session.Coins == coins + GameSession.GardenQuestReward, "the one-time reward was wrong");
            yield return null;
            yield return null;
            Require(view.ShownStage == ResonanceStage.BloomingGarden && view.GardenBloomShown && !view.GardenDormantShown && view.YardLifeShown,
                "the garden did not visibly bloom");
            Require(view.ClearStreamShown && view.BloomShown, "stage 2 must keep the restored stream");
            Require(!view.WindmillRepairedShown && view.WindmillDamagedShown, "the windmill must wait for stage 3");

            yield return MeasureFrames(4f, "the garden restoration");
            Require(!view.IsTransitioning && Mathf.Approximately(lighting.GardenBlend, 1f) && Mathf.Approximately(lighting.StreamBlend, 1f), "the garden transition did not finish");
            Require(view.GardenMotesPlaying, "no pollen over the garden");
            foreach (NpcIdle villager in view.Villagers) Require(villager.Level == 1, villager.name + " did not perk up");
            yield return ShootGarden("11-garden-restored");
            Require(session.TryDeliverEgg() == Outcome.WrongState, "a second egg delivery was not rejected");
            Require(session.Coins == coins + GameSession.GardenQuestReward, "a rejected delivery paid");
            Require(hud.ObjectiveHeading == QuestLog.WindmillHeading && hud.ObjectiveBody.Contains("Alder"), "the objective panel does not point to the windmill: " + hud.ObjectiveBody);
            Require(session.Inventory.Count(ItemIds.ChickenFeed) == 2 && session.TryFeedChicken() == Outcome.Ok, "Clover is not usable after the quest");
        }

        // ---- 5. The windmill (stage 2 -> 3) -----------------------------------------------------------------------------------------

        IEnumerator RestoreTheWindmill()
        {
            GameSession session = GameSession.Current;
            Require(!view.Windmill.Spinning && Mathf.Approximately(view.Windmill.TotalDegrees, 0f), "the broken windmill is turning");
            yield return ShootWindmill("12-windmill-broken");

            yield return StandBeside(alder);
            Require(hud.PromptText.Contains("Alder"), "Alder is not offered: " + hud.PromptText);
            Require(detector.TryInteract() && dialogue.IsOpen, "Alder did not answer");
            ReadAll();
            Require(session.WindmillQuest == QuestStage.Active && session.Resonance == ResonanceStage.BloomingGarden, "Alder did not give the windmill quest");
            yield return null;
            Require(hud.ObjectiveBody.Contains("Echo Ore 0/4"), "the objective does not count the ore: " + hud.ObjectiveBody);

            // Alder without braces only explains
            yield return StandBeside(alder);
            Require(detector.TryInteract(), "Alder did not answer the second time");
            ReadAll();
            Require(session.WindmillQuest == QuestStage.Active && session.Resonance == ResonanceStage.BloomingGarden, "Alder took something that was not there");

            // Four ore from the four outcrops around the valley's edge, then the braces at the workshop
            for (int i = 1; i <= 4; i++) yield return Gather("ore_" + i);
            Require(session.Inventory.Count(ItemIds.Ore) == 4, "did not gather four ore");
            yield return null;
            Require(hud.ObjectiveBody.Contains("Mix Windmill Braces"), "the objective does not send the player to the workshop: " + hud.ObjectiveBody);
            yield return StandBeside(workshop);
            Require(detector.TryInteract() && crafting.IsOpen, "workshop panel did not open");
            Require(crafting.CraftButton(2).interactable, "braces should be craftable with four ore");
            crafting.CraftButton(2).onClick.Invoke();
            Require(session.Inventory.Count(ItemIds.WindmillBraces) == 1 && session.Inventory.Count(ItemIds.Ore) == 0, "crafting the braces used the wrong amounts");
            Require(crafting.Craft(2) == Outcome.Insufficient, "a repeated craft of the braces was not rejected");
            crafting.Close();

            // Deliver them
            yield return StandBeside(alder);
            Require(hud.PromptText.Contains("Windmill Braces"), "Alder does not offer to take the braces: " + hud.PromptText);
            int coins = session.Coins;
            Require(detector.TryInteract() && dialogue.IsOpen, "delivering the braces failed");
            ReadAll();
            Require(session.WindmillQuest == QuestStage.Completed && session.Resonance == ResonanceStage.RestoredValley, "ResonanceStage did not reach 3");
            Require(session.Inventory.Count(ItemIds.WindmillBraces) == 0, "the braces were not consumed");
            Require(session.Coins == coins + GameSession.WindmillQuestReward, "the one-time reward was wrong");
            yield return null;
            yield return null;
            Require(view.ShownStage == ResonanceStage.RestoredValley && view.WindmillRepairedShown && !view.WindmillDamagedShown && view.VillageLifeShown,
                "the windmill was not visibly mended");
            Require(view.ClearStreamShown && view.GardenBloomShown && view.YardLifeShown, "stage 3 must keep stages 1 and 2");
            Require(view.Windmill.Spinning, "the mended windmill is not turning");

            yield return MeasureFrames(5f, "the windmill restoration with every effect running (stage 3)");
            Require(!view.IsTransitioning, "the final transition did not finish");
            Require(Mathf.Approximately(lighting.ValleyBlend, 1f) && Mathf.Approximately(lighting.GardenBlend, 1f) && Mathf.Approximately(lighting.StreamBlend, 1f),
                "the golden-hour lighting did not settle");
            Require(Mathf.Approximately(view.Windmill.CurrentSpeed, view.Windmill.FullSpeed), "the sails never reached full speed");
            foreach (NpcIdle villager in view.Villagers) Require(villager.Level == 2, villager.name + " is not cheerful");

            // Two pictures with the sails in different positions, and the angles that prove the rotation
            float angleA = view.Windmill.TotalDegrees;
            yield return ShootWindmill("13a-windmill-turning");
            yield return new WaitForSecondsRealtime(1.2f);
            float angleB = view.Windmill.TotalDegrees;
            yield return ShootWindmill("13b-windmill-turning");
            Log("windmill sail angle " + angleA.ToString("F1") + " -> " + angleB.ToString("F1") + " degrees between the two pictures");
            Require(angleB - angleA > 20f, "the windmill is not turning during normal play (" + angleA + " -> " + angleB + ")");
            yield return ShootPlaza("14-village-restored");

            Require(session.TryDeliverWindmillBraces() == Outcome.WrongState, "a second braces delivery was not rejected");
            Require(session.Coins == coins + GameSession.WindmillQuestReward, "a rejected delivery paid");
            Require(hud.ObjectiveHeading == QuestLog.ReturnHeading && hud.ObjectiveBody.Contains("Mira"), "the objective panel does not send the player back to Mira: " + hud.ObjectiveBody);
            Require(!ending.IsOpen && !session.EndingSeen, "the ending started before the closing conversation");
        }

        // ---- 6. The ending -----------------------------------------------------------------------------------------------------------

        IEnumerator TheEnding()
        {
            GameSession session = GameSession.Current;
            yield return StandBeside(mira);
            Require(detector.TryInteract() && dialogue.IsOpen, "Mira did not start the closing conversation");
            Require(dialogue.CurrentLine.Contains("feel it"), "this is not Mira's closing conversation: " + dialogue.CurrentLine);
            int lines = 1;
            while (dialogue.IsOpen)
            {
                dialogue.Advance();
                lines++;
            }
            Log("closing conversation: " + (lines - 1) + " lines");
            Require(ending.IsOpen && session.EndingSeen && ending.TimesShown == 1, "the closing conversation did not open the ending");
            Require(GameFlow.IsPanelOpen && !GameFlow.GameplayInputAllowed && Cursor.visible, "the ending must wait for the player with a free cursor");
            Require(File.Exists(SaveSystem.MainPath) && SaveSystem.Load().Session.EndingSeen, "the ending was not saved when it began");
            Require(ending.ContinueButton.interactable && ending.MainMenuButton.interactable, "an ending button is disabled");

            float yawBefore = rig.Yaw;
            yield return new WaitForSecondsRealtime(1.5f);
            Require(Mathf.Abs(Mathf.DeltaAngle(yawBefore, rig.Yaw)) > 1f, "the camera does not drift around the valley behind the ending");
            yield return EndOfFrame();
            Capture("15-ending");
            Require(session.TryCompleteEnding() == Outcome.WrongState, "the ending could be recorded twice");
        }

        IEnumerator ContinueExploring()
        {
            GameSession session = GameSession.Current;
            ending.ContinueButton.onClick.Invoke();   // the real Continue Exploring button
            Require(!ending.IsOpen && GameFlow.GameplayInputAllowed && !Cursor.visible, "Continue Exploring did not give the valley back");
            Require(Mathf.Approximately(rig.AutoOrbitDegreesPerSecond, 0f), "the camera is still drifting");
            yield return null;
            Require(hud.ObjectiveHeading == QuestLog.EndedHeading, "the objective panel does not show the end: " + hud.ObjectiveHeading);

            // Walk from open ground (Mira stands right in front of the player after the closing conversation)
            player.Teleport(player.RespawnPoint.position + Vector3.up * 0.2f);
            yield return null;
            yield return null;
            Vector3 before = player.transform.position;
            player.InputOverride = Vector2.up;
            yield return new WaitForSecondsRealtime(0.7f);
            player.InputOverride = null;
            Require(Vector3.Distance(before, player.transform.position) > 1f, "the player cannot walk after the ending");

            yield return StandBeside(mira);
            Require(detector.TryInteract() && dialogue.IsOpen, "Mira did not talk after the ending");
            ReadAll();
            Require(!ending.IsOpen && ending.TimesShown == 1, "talking to Mira replayed the ending");

            int herbs = session.Inventory.Count(ItemIds.Herb);
            yield return Gather("herb_3");
            Require(session.Inventory.Count(ItemIds.Herb) == herbs + 1, "gathering stopped working after the ending");
            yield return EndOfFrame();
            Capture("16-continue-exploring");
        }

        IEnumerator ReloadAfterTheEnding()
        {
            GameSession session = GameSession.Current;
            int coinsSaved = session.Coins;
            Require(pause.SaveGame(), "saving after the ending failed");
            pause.GoToMainMenu();
            yield return WaitForMainMenu();
            yield return null;
            Require(Time.timeScale == 1f, "time scale not restored in MainMenu");
            Require(Cursor.visible, "cursor not shown in MainMenu");

            // With a save on disk, New Game asks first and Cancel leaves everything alone
            MainMenuController menu = FindFirstObjectByType<MainMenuController>();
            Require(menu.ContinueButton.interactable, "Continue is disabled although a save exists: " + menu.SaveMessage);
            string savedText = File.ReadAllText(SaveSystem.MainPath);
            menu.PlayButton.onClick.Invoke();
            yield return null;
            Require(menu.ConfirmationOpen, "New Game did not ask before it could replace a save");
            Require(FindFirstObjectByType<PlayerController>() == null, "New Game started although it was only asked");
            yield return EndOfFrame();
            Capture("17-new-game-question");
            menu.CancelButton.onClick.Invoke();
            yield return null;
            Require(!menu.ConfirmationOpen && menu.PlayButton.interactable && menu.ContinueButton.interactable, "Cancel did not restore the menu");
            Require(File.ReadAllText(SaveSystem.MainPath) == savedText, "asking about New Game changed the save");

            // Continue: the ended game comes back exactly as it was, with no ending screen and no second reward
            menu.ContinueButton.onClick.Invoke();
            yield return WaitForValley();
            yield return null;
            yield return null;
            FindWorld();
            GameSession loaded = GameSession.Current;
            Require(host.LoadedFromSave && !ReferenceEquals(loaded, session), "Continue did not load the saved session");
            Require(loaded.Resonance == ResonanceStage.RestoredValley && loaded.GardenQuest == QuestStage.Completed
                && loaded.WindmillQuest == QuestStage.Completed && loaded.EndingSeen, "the finished game was not saved");
            Require(loaded.Coins == coinsSaved, "coins changed across the save");
            Require(!ending.IsOpen && ending.TimesShown == 0 && GameFlow.GameplayInputAllowed, "loading forced the ending to play again");
            Require(view.ShownStage == ResonanceStage.RestoredValley && !view.IsTransitioning && view.WindmillRepairedShown && !view.WindmillDamagedShown
                && view.GardenBloomShown && view.ClearStreamShown && view.VillageLifeShown, "the finished world was not shown at once after loading");
            Require(Mathf.Approximately(lighting.ValleyBlend, 1f) && Mathf.Approximately(lighting.GardenBlend, 1f) && Mathf.Approximately(lighting.StreamBlend, 1f),
                "the golden-hour lighting was not applied at once");
            Require(Mathf.Approximately(view.Windmill.CurrentSpeed, view.Windmill.FullSpeed), "the loaded windmill is not at full speed");
            float angle = view.Windmill.TotalDegrees;
            yield return new WaitForSecondsRealtime(0.6f);
            Require(view.Windmill.TotalDegrees - angle > 10f, "the loaded windmill is not turning");
            Require(hud.ObjectiveHeading == QuestLog.EndedHeading, "the objective panel does not show the end after loading: " + hud.ObjectiveHeading);
            Require(loaded.TryDeliverEgg() == Outcome.WrongState && loaded.TryDeliverWindmillBraces() == Outcome.WrongState
                && loaded.TryCompleteEnding() == Outcome.WrongState && loaded.Coins == coinsSaved, "loading allowed a reward or the ending again");
            yield return ShootWindmill("18-after-reload-windmill");

            pause.GoToMainMenu();
            yield return WaitForMainMenu();
            yield return null;
            Require(Time.timeScale == 1f, "time scale not restored in MainMenu");
            Require(Cursor.visible, "cursor not shown in MainMenu");
        }

        // ---- Helpers ---------------------------------------------------------------------------------------------------------------

        void FindWorld()
        {
            player = FindFirstObjectByType<PlayerController>();
            rig = FindFirstObjectByType<ThirdPersonCamera>();
            detector = FindFirstObjectByType<InteractionDetector>();
            objective = FindFirstObjectByType<ExplorationObjective>();
            feedback = FindFirstObjectByType<CompletionFeedback>();
            pause = FindFirstObjectByType<PauseController>();
            dialogue = FindFirstObjectByType<DialogueBox>();
            hud = FindFirstObjectByType<HudController>();
            gameplayHud = FindFirstObjectByType<GameplayHud>();
            crafting = FindFirstObjectByType<CraftingPanel>();
            inventoryPanel = FindFirstObjectByType<InventoryPanel>();
            hen = FindFirstObjectByType<ChickenInteractable>();
            view = FindFirstObjectByType<ResonanceView>();
            lighting = FindFirstObjectByType<ValleyLighting>();
            host = FindFirstObjectByType<GameSessionHost>();
            mira = FindFirstObjectByType<NpcInteractable>();
            wren = FindFirstObjectByType<GardenKeeperInteractable>();
            alder = FindFirstObjectByType<WindmillRestorerInteractable>();
            workshop = FindFirstObjectByType<WorkshopInteractable>();
            spring = FindFirstObjectByType<SpringInteractable>();
            heart = FindFirstObjectByType<HeartInteractable>();
            ending = FindFirstObjectByType<EndingPanel>();
            Require(player && rig && detector && objective && feedback && pause && dialogue && hud && gameplayHud && crafting && inventoryPanel
                && hen && view && lighting && host && mira && wren && alder && workshop && spring && heart && ending, "a required Valley object is missing");
        }

        static ResourceNode FindNode(string id)
        {
            foreach (ResourceNode node in FindObjectsByType<ResourceNode>(FindObjectsSortMode.None))
                if (node.NodeId == id) return node;
            return null;
        }

        /// <summary>Teleports next to the node and presses E through the detector, checking the grant and the regrowth.</summary>
        IEnumerator Gather(string id)
        {
            GameSession session = GameSession.Current;
            ResourceNode node = FindNode(id);
            Require(node != null && node.IsReady, id + " missing or not ready");
            yield return StandBeside(node);
            Require(ReferenceEquals(detector.Current, node), id + " is not the offered interaction");
            int held = session.Inventory.Count(node.ItemId);
            Require(detector.TryInteract(), id + " interaction failed");
            Require(session.Inventory.Count(node.ItemId) == held + 1, id + " did not grant its item");
            Require(!node.IsReady, id + " should be regrowing");
            Require(detector.TryInteract() && session.Inventory.Count(node.ItemId) == held + 1, id + " granted a second item while regrowing");
            yield return null;
            Require(!node.ReadyVisualShown, id + " still looks ripe");
        }

        IEnumerator StandBeside(Interactable target)
        {
            player.Teleport(Beside(target));
            yield return null;
            yield return null;
        }

        void ReadAll()
        {
            while (dialogue.IsOpen) dialogue.Advance();
        }

        IEnumerator WaitForValley()
        {
            float waitStart = Time.realtimeSinceStartup;
            while (FindFirstObjectByType<PlayerController>() == null)
            {
                Require(Time.realtimeSinceStartup - waitStart < 30f, "Valley did not load within 30 s");
                yield return null;
            }
            yield return null;
        }

        IEnumerator WaitForMainMenu()
        {
            float waitStart = Time.realtimeSinceStartup;
            while (FindFirstObjectByType<MainMenuController>() == null)
            {
                Require(Time.realtimeSinceStartup - waitStart < 30f, "MainMenu did not load within 30 s");
                yield return null;
            }
            yield return null;
        }

        static Vector3 Beside(Interactable target) => target.Position + new Vector3(0f, 0.6f, -1.5f);

        // ---- Pictures: a fixed camera for the scenery (the player's camera is switched off for one frame and snapped back) ----------

        IEnumerator ShootGarden(string shotName)
        {
            Vector3 center = view.GardenPosition;
            yield return ShootFrom(shotName, center + new Vector3(-10f, 0f, -5f), 3.4f, center + new Vector3(0f, 0.6f, 0f));
        }

        IEnumerator ShootWindmill(string shotName)
        {
            Vector3 center = view.Windmill.transform.position;
            yield return ShootFrom(shotName, center + new Vector3(-3f, 0f, -18f), 3.6f, center + new Vector3(0f, 4.6f, 0f));
        }

        IEnumerator ShootPlaza(string shotName)
        {
            yield return ShootFrom(shotName, new Vector3(-12f, 0f, -12f), 3.6f, new Vector3(0f, 1.6f, 0f));
        }

        IEnumerator ShootFrom(string shotName, Vector3 position, float heightAboveGround, Vector3 lookAt)
        {
            Terrain terrain = Terrain.activeTerrain;
            position.y = terrain.SampleHeight(position) + terrain.transform.position.y + heightAboveGround;
            Camera camera = rig.GetComponent<Camera>();
            rig.enabled = false;
            camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(lookAt - position));
            yield return EndOfFrame();
            Capture(shotName);
            rig.enabled = true;
            rig.Snap();
        }

        // ---- Frame time, logging and screenshots -------------------------------------------------------------------------------------

        /// <summary>Measures frame time over a window with vSync off, then turns vSync back on. One data point, not a benchmark.</summary>
        IEnumerator MeasureFrames(float seconds, string what)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            statSeconds = 0f;
            statFrames = 0;
            statWorst = 0f;
            measuring = true;
            yield return new WaitForSecondsRealtime(seconds);
            measuring = false;
            if (statFrames > 0)
                Log("frame time during " + what + ", " + Screen.width + "x" + Screen.height + " windowed, vSync off: " + statFrames + " frames in "
                    + statSeconds.ToString("F1") + " s, average " + (statSeconds / statFrames * 1000f).ToString("F1") + " ms ("
                    + (statFrames / statSeconds).ToString("F0") + " FPS), worst frame " + (statWorst * 1000f).ToString("F1") + " ms");
            QualitySettings.vSyncCount = 1;
            yield return EndOfFrame();
        }

        void Update()
        {
            // A wait that never ends (a scene that does not load, a missing frame callback) must still end the run with FAIL.
            if (!finished && Time.realtimeSinceStartup - startedAt > TimeoutSeconds + 10f) Finish(false, step + ": watchdog, no progress for " + TimeoutSeconds + " s");
            if (!measuring) return;
            float frame = Time.unscaledDeltaTime;
            statSeconds += frame;
            statFrames++;
            statWorst = Mathf.Max(statWorst, frame);
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        static void Log(string message) => Debug.Log(Tag + message);

        void Finish(bool passed, string message)
        {
            if (finished) return;
            finished = true;
            Log((passed ? "PASS: " : "FAIL: ") + message);
#if UNITY_EDITOR
            // Rehearsal in the Editor (Echoes > DryRun task): Application.Quit does nothing there, so end the batch process.
            if (Application.isBatchMode) UnityEditor.EditorApplication.Exit(passed ? 0 : 1);
#endif
            Application.Quit(passed ? 0 : 1);
        }

        void Capture(string shotName)
        {
            if (string.IsNullOrEmpty(shotDirectory)) return;
            try
            {
                Directory.CreateDirectory(shotDirectory);
                Texture2D texture = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(shotDirectory, shotName + ".png"), texture.EncodeToPNG());
                Destroy(texture);
                shots++;
                Log("screenshot " + shotName + ".png");
            }
            catch (Exception e)
            {
                // The standalone player must be able to take every picture; a batch-mode Editor rehearsal has no Game view to read.
                if (!Application.isEditor) throw new Exception("screenshot " + shotName + " failed: " + e.Message);
                Log("screenshot " + shotName + " skipped in the Editor (" + e.Message + ")");
            }
        }

        static string ReadArgument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
    }
}
