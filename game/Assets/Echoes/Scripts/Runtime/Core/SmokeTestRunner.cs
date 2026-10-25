using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Echoes
{
    /// <summary>
    /// Scripted end-to-end pass for the standalone player. It only runs when the player is started with
    /// -echoes-smoke, drives the same public entry points the UI and input use (not real keystrokes),
    /// optionally saves screenshots to the folder after -echoes-shots, logs "[Echoes][SMOKE] PASS|FAIL"
    /// and exits with code 0 or 1. It cannot replace a human playtest.
    /// </summary>
    public class SmokeTestRunner : MonoBehaviour
    {
        const string Tag = "[Echoes][SMOKE] ";
        const float TimeoutSeconds = 150f;

        string shotDirectory;
        float startedAt;
        bool measuring;
        float statSeconds;
        int statFrames;
        float statWorst;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoStart()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-echoes-smoke") < 0) return;
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

            // ---- Main menu ----
            MainMenuController menu = FindFirstObjectByType<MainMenuController>();
            Require(menu != null, "MainMenuController missing");
            yield return new WaitForSecondsRealtime(1.5f);
            yield return new WaitForEndOfFrame();
            Capture("01-main-menu");
            Require(EventSystem.current != null, "no EventSystem in MainMenu");
            Require(Time.timeScale == 1f, "time scale is not 1 in MainMenu");
            Require(Cursor.visible, "cursor is hidden in MainMenu");
            menu.Play();

            // ---- Valley ----
            float waitStart = Time.realtimeSinceStartup;
            while (FindFirstObjectByType<PlayerController>() == null)
            {
                Require(Time.realtimeSinceStartup - waitStart < 30f, "Valley did not load within 30 s");
                yield return null;
            }
            yield return new WaitForSecondsRealtime(1.5f);

            PlayerController player = FindFirstObjectByType<PlayerController>();
            InteractionDetector detector = FindFirstObjectByType<InteractionDetector>();
            ExplorationObjective objective = FindFirstObjectByType<ExplorationObjective>();
            CompletionFeedback feedback = FindFirstObjectByType<CompletionFeedback>();
            PauseController pause = FindFirstObjectByType<PauseController>();
            DialogueBox dialogue = FindFirstObjectByType<DialogueBox>();
            HudController hud = FindFirstObjectByType<HudController>();
            NpcInteractable npc = FindFirstObjectByType<NpcInteractable>();
            WorkshopInteractable workshop = FindFirstObjectByType<WorkshopInteractable>();
            SpringInteractable spring = FindFirstObjectByType<SpringInteractable>();
            HeartInteractable heart = FindFirstObjectByType<HeartInteractable>();
            Require(player && detector && objective && feedback && pause && dialogue && hud && npc && workshop && spring && heart,
                "a required Valley object is missing");
            Require(player.IsGrounded, "player is not grounded after spawn");
            Require(!Cursor.visible, "cursor is visible in the Valley");
            Log("cursor lockState=" + Cursor.lockState + ", window focused=" + Application.isFocused + " (informational; the OS decides focus)");
            Require(objective.Step == ObjectiveStep.MeetVillager, "objective does not start at the first step");
            yield return new WaitForEndOfFrame();
            Capture("02-valley-start");

            // ---- Movement ----
            Vector3 before = player.transform.position;
            player.InputOverride = Vector2.up;
            yield return new WaitForSecondsRealtime(1f);
            player.InputOverride = null;
            Vector3 after = player.transform.position;
            float moved = Vector2.Distance(new Vector2(before.x, before.z), new Vector2(after.x, after.z));
            Log("moved " + moved.ToString("F2") + " m in 1 s");
            Require(moved > 2f, "player moved only " + moved.ToString("F2") + " m in 1 s");

            // ---- Interaction distance ----
            detector.Refresh();
            Require(detector.Current == null, "an interactable is offered at the spawn area");
            Require(!detector.TryInteract(), "interaction succeeded while out of range");
            Require(!hud.IsPromptVisible, "prompt visible while out of range");

            // ---- Workshop (must not advance the objective) ----
            player.Teleport(Beside(workshop));
            yield return null;
            yield return null;
            Require(ReferenceEquals(detector.Current, workshop), "workshop not selected next to it");
            Require(hud.IsPromptVisible, "no prompt next to the workshop");
            Require(detector.TryInteract() && dialogue.IsOpen, "workshop interaction did not open dialogue");
            dialogue.Advance();
            Require(dialogue.CurrentLine.Contains("future phase"), "workshop does not explain that alchemy comes later");
            dialogue.Close();
            Require(objective.Step == ObjectiveStep.MeetVillager, "workshop changed the objective");

            // ---- Villager ----
            player.Teleport(Beside(npc));
            yield return null;
            yield return null;
            Require(ReferenceEquals(detector.Current, npc), "villager not selected next to her");
            Require(detector.TryInteract() && dialogue.IsOpen, "villager interaction did not open dialogue");
            yield return new WaitForEndOfFrame();
            Capture("03-dialogue");
            while (dialogue.IsOpen) dialogue.Advance();
            Require(objective.Step == ObjectiveStep.InspectSpring, "talking to the villager did not advance the objective");

            // ---- Heart too early must not complete anything ----
            player.Teleport(Beside(heart));
            yield return null;
            yield return null;
            Require(detector.TryInteract(), "heart interaction failed");
            while (dialogue.IsOpen) dialogue.Advance();
            Require(objective.Step == ObjectiveStep.InspectSpring, "heart advanced the objective out of order");

            // ---- Spring, then the Heart ----
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

            // Measure frame time while the completion effect plays, uncapped (vSync off). One data point, not a benchmark.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            ResetFrameStats();
            measuring = true;
            yield return new WaitForSecondsRealtime(5f);
            measuring = false;
            LogFrameStats("frame time during the completion effect, " + Screen.width + "x" + Screen.height + " windowed, vSync off");
            QualitySettings.vSyncCount = 1;
            yield return new WaitForEndOfFrame();
            Capture("04-complete");

            Require(detector.TryInteract(), "heart interaction failed after completion");
            while (dialogue.IsOpen) dialogue.Advance();
            Require(objective.CompletionCount == 1, "completion repeated");

            // ---- Pause and resume ----
            pause.SetPaused(true);
            Require(Time.timeScale == 0f && pause.IsPaused, "pause did not freeze time");
            Require(Cursor.visible, "cursor not shown while paused");
            yield return null;
            yield return new WaitForEndOfFrame();
            Capture("05-pause");
            pause.SetPaused(false);
            Require(Time.timeScale == 1f && !pause.IsPaused, "resume did not restore time");
            Require(!Cursor.visible, "cursor not hidden again after resume");

            // ---- Back to the main menu from a paused game ----
            pause.SetPaused(true);
            pause.GoToMainMenu();
            waitStart = Time.realtimeSinceStartup;
            while (FindFirstObjectByType<MainMenuController>() == null)
            {
                Require(Time.realtimeSinceStartup - waitStart < 30f, "MainMenu did not load");
                yield return null;
            }
            yield return null;
            Require(Time.timeScale == 1f, "time scale not restored in MainMenu");
            Require(Cursor.visible, "cursor not shown in MainMenu");
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
    }
}
