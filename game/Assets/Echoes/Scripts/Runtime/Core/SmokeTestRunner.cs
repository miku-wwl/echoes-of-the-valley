using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Echoes
{
    /// <summary>
    /// Scripted end-to-end pass for the standalone player. It only runs when the player is started with -echoes-smoke and
    /// drives the same public entry points the UI and input use (teleports and button clicks instead of keystrokes), so every
    /// transaction goes through the real GameSession rules: gathering, crafting, feeding, delivering, saving and loading.
    /// It writes screenshots to the folder after -echoes-shots, saves only to an isolated folder (-echoes-save-dir or a temp
    /// folder), logs "[Echoes][SMOKE] PASS|FAIL" and exits with code 0 or 1. It cannot replace a human playtest.
    /// </summary>
    public class SmokeTestRunner : MonoBehaviour
    {
        const string Tag = "[Echoes][SMOKE] ";
        const float TimeoutSeconds = 240f;

        string shotDirectory;
        float startedAt;
        bool measuring;
        float statSeconds;
        int statFrames;
        float statWorst;

        PlayerController player;
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
        NpcInteractable npc;
        WorkshopInteractable workshop;
        SpringInteractable spring;
        HeartInteractable heart;

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

        IEnumerator Run()
        {
            IEnumerator steps = Steps();
            while (true)
            {
                object current = null;
                string failure = null;
                bool finished = false;
                try
                {
                    if (Time.realtimeSinceStartup - startedAt > TimeoutSeconds)
                        throw new Exception("timed out after " + TimeoutSeconds + " s");
                    finished = !steps.MoveNext();
                    if (!finished) current = steps.Current;
                }
                catch (Exception e)
                {
                    failure = e.Message;
                }

                if (failure != null)
                {
                    Finish(false, failure);
                    yield break;
                }
                if (finished) break;
                yield return current;
            }
            Finish(true, "all steps passed");
        }

        IEnumerator Steps()
        {
            Log("started: " + Application.productName + " " + Application.version + ", scene=" + SceneManager.GetActiveScene().name
                + ", " + Screen.width + "x" + Screen.height + ", " + SystemInfo.graphicsDeviceType + " / " + SystemInfo.graphicsDeviceName);
            Require(SceneManager.GetActiveScene().name == SceneNames.MainMenu, "startup scene is " + SceneManager.GetActiveScene().name + ", not MainMenu");

            // ---- 1. Main menu: no save yet, so Continue is off and New Game starts clean ----
            MainMenuController menu = FindFirstObjectByType<MainMenuController>();
            Require(menu != null, "MainMenuController missing");
            yield return new WaitForSecondsRealtime(1.5f);
            yield return new WaitForEndOfFrame();
            Capture("01-main-menu");
            Require(EventSystem.current != null, "no EventSystem in MainMenu");
            Require(Time.timeScale == 1f, "time scale is not 1 in MainMenu");
            Require(Cursor.visible, "cursor is hidden in MainMenu");
            Require(menu.ContinueButton != null && !menu.ContinueButton.interactable, "Continue is enabled although no save exists");
            Log("menu save status: " + menu.SaveMessage);
            menu.NewGame();

            // ---- 2. Valley ----
            yield return WaitForValley();
            yield return new WaitForSecondsRealtime(1.5f);
            FindWorld();
            GameSession session = GameSession.Current;
            Require(session != null && !host.LoadedFromSave, "New Game should start a fresh session");
            Require(session.Coins == GameSession.StartingCoins && session.Inventory.UsedSlots == 0, "fresh session is not clean");
            Require(player.IsGrounded, "player is not grounded after spawn");
            Require(!Cursor.visible, "cursor is visible in the Valley");
            Log("cursor lockState=" + Cursor.lockState + ", window focused=" + Application.isFocused + " (informational; the OS decides focus)");
            Require(objective.Step == ObjectiveStep.MeetVillager, "objective does not start at the first step");
            yield return new WaitForEndOfFrame();
            Capture("02-valley-start");

            // ---- Movement and interaction range ----
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

            // ---- Workshop: opens the real crafting panel, and is not part of the opening objective ----
            player.Teleport(Beside(workshop));
            yield return null;
            yield return null;
            Require(ReferenceEquals(detector.Current, workshop), "workshop not selected next to it");
            Require(hud.IsPromptVisible, "no prompt next to the workshop");
            Require(detector.TryInteract() && crafting.IsOpen, "workshop did not open the crafting panel");
            Require(!GameFlow.GameplayInputAllowed && GameFlow.IsPanelOpen, "an open panel must pause movement and interaction");
            crafting.Close();
            Require(GameFlow.GameplayInputAllowed, "closing the panel did not restore control");
            Require(objective.Step == ObjectiveStep.MeetVillager, "workshop changed the objective");

            // ---- Opening objective (Phase 1): villager, spring, Heart ----
            player.Teleport(Beside(npc));
            yield return null;
            yield return null;
            Require(ReferenceEquals(detector.Current, npc), "villager not selected next to her");
            Require(detector.TryInteract() && dialogue.IsOpen, "villager interaction did not open dialogue");
            yield return new WaitForEndOfFrame();
            Capture("03-dialogue");
            while (dialogue.IsOpen) dialogue.Advance();
            Require(objective.Step == ObjectiveStep.InspectSpring, "talking to the villager did not advance the objective");
            Require(session.StreamQuest == StreamQuestStage.NotStarted, "the stream quest must wait for the opening objective");

            player.Teleport(Beside(heart));
            yield return null;
            yield return null;
            Require(detector.TryInteract(), "heart interaction failed");
            while (dialogue.IsOpen) dialogue.Advance();
            Require(objective.Step == ObjectiveStep.InspectSpring, "heart advanced the objective out of order");

            player.Teleport(Beside(spring));
            yield return null;
            yield return null;
            Require(ReferenceEquals(detector.Current, spring), "spring not selected next to it");
            Require(detector.TryInteract(), "spring interaction failed");
            while (dialogue.IsOpen) dialogue.Advance();
            Require(objective.Step == ObjectiveStep.ReturnToHeart, "spring did not advance the objective");

            player.Teleport(Beside(heart));
            yield return null;
            yield return null;
            Require(detector.TryInteract(), "heart interaction failed on return");
            while (dialogue.IsOpen) dialogue.Advance();
            Require(objective.IsComplete && objective.CompletionCount == 1, "objective did not complete exactly once");
            Require(feedback.HasPlayed, "completion feedback did not start");
            Require(session.ExplorationDone, "the session did not record the finished opening objective");

            // Measure frame time while the Heart effect plays, uncapped (vSync off). One data point, not a benchmark.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            ResetFrameStats();
            measuring = true;
            yield return new WaitForSecondsRealtime(5f);
            measuring = false;
            LogFrameStats("frame time during the Heart effect, " + Screen.width + "x" + Screen.height + " windowed, vSync off");
            QualitySettings.vSyncCount = 1;
            yield return new WaitForEndOfFrame();
            Capture("04-complete");
            Require(Mathf.Approximately(lighting.HeartBlend, 1f), "Heart lighting did not finish");

            // ---- Phase 2: Mira gives the stream quest ----
            player.Teleport(Beside(npc));
            yield return null;
            yield return null;
            Require(detector.TryInteract() && dialogue.IsOpen, "Mira did not speak after the objective");
            while (dialogue.IsOpen) dialogue.Advance();
            Require(session.StreamQuest == StreamQuestStage.Active, "Mira did not start the stream quest");
            Require(File.Exists(SaveSystem.MainPath), "starting the quest should autosave");

            // The spring does nothing without the tonic.
            int coinsAtStart = session.Coins;
            player.Teleport(Beside(spring));
            yield return null;
            yield return null;
            Require(detector.TryInteract(), "spring interaction failed");
            while (dialogue.IsOpen) dialogue.Advance();
            Require(session.Resonance == ResonanceStage.SilentValley && session.Coins == coinsAtStart, "the spring restored the stream without a tonic");
            Require(session.StreamQuest == StreamQuestStage.Active, "the quest state changed without a tonic");
            Require(!view.ClearStreamShown && view.TroubledStreamShown && !view.BloomShown, "the world changed without a tonic");

            // ---- Gathering by interaction: 2 herbs and 2 blossoms (one for the tonic, one for feed) ----
            foreach (string id in new[] { "herb_1", "herb_2", "flower_1", "flower_2" })
            {
                ResourceNode node = FindNode(id);
                Require(node != null && node.IsReady, id + " missing or not ready");
                player.Teleport(Beside(node));
                yield return null;
                yield return null;
                Require(ReferenceEquals(detector.Current, node), id + " is not the offered interaction");
                int held = session.Inventory.Count(node.ItemId);
                Require(detector.TryInteract(), id + " interaction failed");
                Require(session.Inventory.Count(node.ItemId) == held + 1, id + " did not grant its item");
                Require(!node.IsReady, id + " should be regrowing");
                Require(detector.TryInteract() && session.Inventory.Count(node.ItemId) == held + 1, id + " granted a second item while regrowing");
                yield return null;
                Require(!node.ReadyVisualShown, id + " still looks ripe");
            }
            Require(session.Inventory.Count(ItemIds.Herb) == 2 && session.Inventory.Count(ItemIds.Flower) == 2, "gathered counts are wrong");

            // ---- Satchel (the I key calls Toggle) ----
            inventoryPanel.Toggle();
            yield return null;
            yield return new WaitForEndOfFrame();
            Capture("05-inventory");
            Require(inventoryPanel.IsOpen && GameFlow.IsPanelOpen, "inventory did not open");
            Require(inventoryPanel.RowName(0) == "Moonleaf Herb" && inventoryPanel.RowCountText(0).StartsWith("x2"), "inventory row 0 is wrong: " + inventoryPanel.RowName(0) + " " + inventoryPanel.RowCountText(0));
            Require(inventoryPanel.RowName(1) == "Dawn Blossom" && inventoryPanel.RowCountText(1).StartsWith("x2"), "inventory row 1 is wrong");
            inventoryPanel.Toggle();
            Require(!inventoryPanel.IsOpen && GameFlow.GameplayInputAllowed, "inventory did not close");

            // ---- Alchemy: Spring Tonic and Chicken Feed through the panel's buttons ----
            player.Teleport(Beside(workshop));
            yield return null;
            yield return null;
            Require(detector.TryInteract() && crafting.IsOpen, "workshop panel did not open");
            yield return null;
            yield return new WaitForEndOfFrame();
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

            // ---- The hen: feed once ----
            player.Teleport(Beside(hen));
            yield return null;
            yield return null;
            Require(ReferenceEquals(detector.Current, hen), "hen not selected next to it");
            Require(detector.TryInteract(), "feeding failed");
            Require(session.Chicken == ChickenPhase.Producing && session.Inventory.Count(ItemIds.ChickenFeed) == 2, "feeding did not start production");
            Require(detector.TryInteract() && session.Inventory.Count(ItemIds.ChickenFeed) == 2, "the hen was fed twice");
            yield return null;
            Require(hen.ProducingIconShown && !hen.HungryIconShown, "the hen does not look busy");

            // ---- Deliver the tonic: the stream is restored ----
            player.Teleport(Beside(spring));
            yield return null;
            yield return null;
            Require(hud.PromptText.Contains("Pour"), "the spring does not offer the delivery: " + hud.PromptText);
            int coinsBefore = session.Coins;
            Require(detector.TryInteract() && dialogue.IsOpen, "delivering the tonic failed");
            while (dialogue.IsOpen) dialogue.Advance();
            Require(session.StreamQuest == StreamQuestStage.Completed, "the quest did not complete");
            Require(session.Resonance == ResonanceStage.AwakeningStream, "ResonanceStage did not reach 1");
            Require(session.Inventory.Count(ItemIds.SpringTonic) == 0, "the tonic was not consumed");
            Require(session.Coins == coinsBefore + GameSession.StreamQuestReward, "the one-time reward was wrong");
            yield return null;
            yield return null;
            Require(view.ShownStage == ResonanceStage.AwakeningStream && view.ClearStreamShown && view.BloomShown && view.PondIsClear && !view.TroubledStreamShown,
                "the world did not change visibly");

            ResetFrameStats();
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            measuring = true;
            yield return new WaitForSecondsRealtime(5f);
            measuring = false;
            LogFrameStats("frame time during the stream restoration, " + Screen.width + "x" + Screen.height + " windowed, vSync off");
            QualitySettings.vSyncCount = 1;
            yield return new WaitForEndOfFrame();
            Capture("07-stream-restored");
            Require(!view.IsTransitioning && Mathf.Approximately(lighting.StreamBlend, 1f), "the restoration transition did not finish");
            Require(session.TryDeliverSpringTonic() == Outcome.WrongState, "a second delivery was not rejected");
            Require(hud.ObjectiveBody.StartsWith("Complete"), "the quest panel does not show the finished quest: " + hud.ObjectiveBody);

            // ---- Save from the pause menu, pause/resume checks, then reload through Continue ----
            pause.SetPaused(true);
            Require(Time.timeScale == 0f && pause.IsPaused, "pause did not freeze time");
            Require(Cursor.visible, "cursor not shown while paused");
            yield return null;
            yield return new WaitForEndOfFrame();
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
            menu = FindFirstObjectByType<MainMenuController>();
            Require(menu.ContinueButton.interactable, "Continue is disabled although a save exists: " + menu.SaveMessage);
            menu.Continue();
            yield return WaitForValley();
            yield return null;
            yield return null;
            FindWorld();

            GameSession loaded = GameSession.Current;
            Require(host.LoadedFromSave && !ReferenceEquals(loaded, session), "Continue did not load the saved session");
            Require(loaded.StreamQuest == StreamQuestStage.Completed && loaded.Resonance == ResonanceStage.AwakeningStream, "the restoration was not saved");
            Require(loaded.Coins == coinsSaved, "coins changed across the save");
            Require(loaded.Inventory.Count(ItemIds.ChickenFeed) == feedSaved && loaded.Inventory.Count(ItemIds.Egg) == 0, "inventory changed across the save");
            Require(loaded.Chicken == ChickenPhase.Producing && Mathf.Abs(loaded.ChickenRemaining - henLeft) < 1.5f, "the hen's production was not saved");
            Require(!FindNode("herb_1").IsReady && Mathf.Abs(loaded.NodeRemaining("herb_1") - herbLeft) < 1.5f && FindNode("herb_3").IsReady, "node timers were not saved");
            Require(view.ShownStage == ResonanceStage.AwakeningStream && !view.IsTransitioning && view.ClearStreamShown && view.BloomShown && view.PondIsClear,
                "the restored world was not shown at once after loading");
            Require(Mathf.Approximately(lighting.StreamBlend, 1f) && Mathf.Approximately(lighting.HeartBlend, 1f), "the restored lighting was not applied");
            Require(objective.IsComplete && objective.CompletionCount == 0, "the opening objective was not restored quietly");
            Require(loaded.TryDeliverSpringTonic() == Outcome.WrongState && loaded.Coins == coinsSaved, "loading allowed the reward again");
            yield return new WaitForEndOfFrame();
            Capture("09-after-reload");

            // ---- The egg, after the reload: production survived and finishes ----
            yield return new WaitForSecondsRealtime(loaded.ChickenRemaining + 1f);
            Require(loaded.Chicken == ChickenPhase.EggReady, "the hen did not finish laying after the reload");
            yield return null;
            Require(hen.EggShown, "the egg is not visible in the nest");
            player.Teleport(Beside(hen));
            yield return null;
            yield return null;
            Require(detector.TryInteract(), "collecting the egg failed");
            Require(loaded.Inventory.Count(ItemIds.Egg) == 1 && loaded.Chicken == ChickenPhase.Hungry, "the egg was not collected");
            Require(!detector.TryInteract() || loaded.Inventory.Count(ItemIds.Egg) == 1, "the egg was collected twice");

            // ---- Back to the main menu ----
            pause.GoToMainMenu();
            yield return WaitForMainMenu();
            yield return null;
            Require(Time.timeScale == 1f, "time scale not restored in MainMenu");
            Require(Cursor.visible, "cursor not shown in MainMenu");
        }

        // ---- Helpers ---------------------------------------------------------------------------------------------------

        void FindWorld()
        {
            player = FindFirstObjectByType<PlayerController>();
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
            npc = FindFirstObjectByType<NpcInteractable>();
            workshop = FindFirstObjectByType<WorkshopInteractable>();
            spring = FindFirstObjectByType<SpringInteractable>();
            heart = FindFirstObjectByType<HeartInteractable>();
            Require(player && detector && objective && feedback && pause && dialogue && hud && gameplayHud && crafting && inventoryPanel
                && hen && view && lighting && host && npc && workshop && spring && heart, "a required Valley object is missing");
        }

        static ResourceNode FindNode(string id)
        {
            foreach (ResourceNode node in FindObjectsByType<ResourceNode>(FindObjectsSortMode.None))
                if (node.NodeId == id) return node;
            return null;
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

        static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        static void Log(string message) => Debug.Log(Tag + message);

        static void Finish(bool passed, string message)
        {
            Log((passed ? "PASS: " : "FAIL: ") + message);
            Application.Quit(passed ? 0 : 1);
        }

        void Capture(string shotName)
        {
            if (string.IsNullOrEmpty(shotDirectory)) return;
            Directory.CreateDirectory(shotDirectory);
            Texture2D texture = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(shotDirectory, shotName + ".png"), texture.EncodeToPNG());
            Destroy(texture);
            Log("screenshot " + shotName + ".png");
        }

        static string ReadArgument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }

        void Update()
        {
            if (!measuring) return;
            float frame = Time.unscaledDeltaTime;
            statSeconds += frame;
            statFrames++;
            statWorst = Mathf.Max(statWorst, frame);
        }

        void ResetFrameStats()
        {
            statSeconds = 0f;
            statFrames = 0;
            statWorst = 0f;
        }

        void LogFrameStats(string label)
        {
            if (statFrames == 0) return;
            Log(label + ": " + statFrames + " frames in " + statSeconds.ToString("F1") + " s, average "
                + (statSeconds / statFrames * 1000f).ToString("F1") + " ms (" + (statFrames / statSeconds).ToString("F0")
                + " FPS), worst frame " + (statWorst * 1000f).ToString("F1") + " ms");
        }
    }
}
