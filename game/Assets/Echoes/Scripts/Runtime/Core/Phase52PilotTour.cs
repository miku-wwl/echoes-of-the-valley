using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// Opt-in actual-controller walking evidence. AUTOMATED analog input through PlayerController.InputOverride and ThirdPersonCamera.ApplyLook;
    /// no teleport during the measured route. The route follows the original dirt paths, passes NPCs, circles a canopy tree with the camera
    /// low and orbits the camera around the walking hero. Presentation evidence only; it is not a human play session.
    /// </summary>
    public sealed class Phase52PilotTour : MonoBehaviour
    {
        const float RouteSeconds = 40f;
        [Serializable] sealed class Sample { public float t, x, y, z, speed, phase, walkBlend, yaw, frameMs, cameraYaw, cameraPitch; public string leg; }
        [Serializable] sealed class CapturedFrame { public string file; public float t; }
        [Serializable] sealed class Record
        {
            public string input = "AUTOMATED PlayerController.InputOverride; original controller, gravity/collision and camera remain active";
            public bool teleportsDuringMovement = false;
            public float movementSeconds, travelledMetres;
            public int visitedWaypoints, width, height;
            public bool pass;
            public string canopyTree;
            public List<Sample> samples = new List<Sample>();
            public string videoCapture = "None";
            public List<CapturedFrame> capturedFrames = new List<CapturedFrame>();
        }
        PlayerController player;
        ThirdPersonCamera cameraRig;
        Camera tourCamera;
        string folder;
        bool finished;
        float launched;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Begin()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-echoes-pilot-tour") < 0) return;
            var root = new GameObject("AutomatedPhase52MovementEvidence"); DontDestroyOnLoad(root); root.AddComponent<Phase52PilotTour>();
        }
        void Start() { launched = Time.realtimeSinceStartup; StartCoroutine(Run()); }
        void Update()
        {
            if (!finished && Time.realtimeSinceStartup - launched > 140) { Debug.LogError("[Echoes][P52 TOUR] TIMEOUT; no movement acceptance."); Application.Quit(1); }
        }

        Vector3 Ground(Phase52Pilot pilot, Vector2 p) { var v = new Vector3(p.x, 0, p.y); v.y = pilot.Terrain.SampleHeight(v) + pilot.Terrain.transform.position.y + .02f; return v; }

        IEnumerator Run()
        {
            folder = Argument("-echoes-tour-output"); if (string.IsNullOrEmpty(folder)) throw new IOException("Explicit fresh tour output required.");
            Directory.CreateDirectory(folder); if (File.Exists(Path.Combine(folder, "tour.json"))) throw new IOException("Tour output already exists.");
            yield return null;
            var menu = FindFirstObjectByType<MainMenuController>(); if (menu != null) menu.PlayButton.onClick.Invoke();
            while ((player = FindFirstObjectByType<PlayerController>()) == null) yield return null;
            tourCamera = Camera.main; cameraRig = tourCamera.GetComponent<ThirdPersonCamera>(); QualitySettings.vSyncCount = 0; Application.targetFrameRate = 60;
            var pilot = FindFirstObjectByType<Phase52Pilot>();
            Vector3 start = Ground(pilot, new Vector2(10.4f, -11.0f));
            player.Teleport(start); cameraRig.Snap();
            yield return new WaitForSecondsRealtime(.5f);
            FindFirstObjectByType<HudController>().HideBanner();
            // The nearest pilot tree to the east path becomes the canopy detour: the hero circles it at 2.4 m.
            Transform tree = null; float best = 99f; Vector2 near = new Vector2(15, 4);
            foreach (Transform t in (pilot.Route != null ? pilot.Route.transform : transform))
            {
                if (!t.name.EndsWith("_Pilot")) continue; float d = Vector2.Distance(new Vector2(t.position.x, t.position.z), near);
                if (d < best && d > 3.5f && d < 14f) { best = d; tree = t; }
            }
            var points = new List<Vector2> { new Vector2(9.5f, -9.5f), new Vector2(7, -6), new Vector2(9, 1), new Vector2(13.2f, 3.4f), new Vector2(15, 4) };
            string canopy = "none";
            if (tree != null)
            {
                Vector2 c = new Vector2(tree.position.x, tree.position.z); canopy = tree.name + " at " + c;
                Vector2 toRoute = (near - c).normalized; if (toRoute.sqrMagnitude < .01f) toRoute = Vector2.right;
                float baseAngle = Mathf.Atan2(toRoute.y, toRoute.x);
                points.Add(c + toRoute * 3.6f);
                for (int i = 0; i < 9; i++) { float a = baseAngle + i * Mathf.PI / 4.5f; points.Add(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 2.6f); }
                points.Add(c + toRoute * 4.2f); points.Add(new Vector2(15, 4));
            }
            points.AddRange(new[] { new Vector2(9, 1), new Vector2(8, 8), new Vector2(0, 9), new Vector2(1.5f, 19), new Vector2(3.8f, 25.5f), new Vector2(1.5f, 19), new Vector2(0, 9), new Vector2(8, 8), new Vector2(9, 1), new Vector2(15, 4), new Vector2(9, 1) });
            File.WriteAllText(Path.Combine(folder, "ready.flag"), "Actual Windows player ready; movement begins after five seconds.\n");
            Debug.Log("[Echoes][P52 TOUR] READY; AUTOMATED_INPUT; " + RouteSeconds + "s route, no teleports during movement; canopy detour: " + canopy);
            yield return new WaitForSecondsRealtime(5f);
            int next = 0; float began = Time.realtimeSinceStartup; Vector3 last = player.transform.position; var record = new Record { width = Screen.width, height = Screen.height, canopyTree = canopy };
            bool captureFrames = Array.IndexOf(Environment.GetCommandLineArgs(), "-echoes-pilot-record-frames") >= 0;
            float nextFrame = 0;
            if (captureFrames) { Directory.CreateDirectory(Path.Combine(folder, "frames")); record.videoCapture = "Actual ScreenCapture.CaptureScreenshotAsTexture at WaitForEndOfFrame; original monotonic timestamps; no desktop capture or concept compositing"; }
            var motion = player.GetComponentInChildren<SkinnedCaretakerMotion>();
            float pitchTarget = 22f;
            while (Time.realtimeSinceStartup - began < RouteSeconds)
            {
                Vector2 current = new Vector2(player.transform.position.x, player.transform.position.z); Vector2 delta = points[next] - current;
                if (delta.magnitude < .7f) { next = (next + 1) % points.Count; record.visitedWaypoints++; delta = points[next] - current; }
                Vector3 wanted = new Vector3(delta.x, 0, delta.y).normalized;
                float age = Time.realtimeSinceStartup - began;
                // camera schedule: behind -> slow orbit around the walking hero -> low under the canopy -> behind again
                string leg = age < 8 ? "follow" : age < 17 ? "orbit" : age < 31 ? "canopy-low" : "follow-windmill";
                float viewOffset = age < 8 ? 0 : age < 17 ? Mathf.Lerp(0, 330, (age - 8) / 9f) : age < 31 ? 40 * Mathf.Sin((age - 17) * .45f) : 0;
                pitchTarget = age < 8 ? 22 : age < 17 ? Mathf.Lerp(22, 14, (age - 8) / 9f) : age < 31 ? 9 : Mathf.Lerp(9, 22, Mathf.Clamp01((age - 31) / 4f));
                float desiredYaw = Mathf.Atan2(wanted.x, wanted.z) * Mathf.Rad2Deg + viewOffset;
                float look = Mathf.Clamp(Mathf.DeltaAngle(cameraRig.Yaw, desiredYaw), -3.2f, 3.2f);
                float pitchLook = Mathf.Clamp(pitchTarget - cameraRig.Pitch, -1.2f, 1.2f);
                cameraRig.ApplyLook(new Vector2(look / .12f, -pitchLook / .12f));
                Vector3 forward = Vector3.ProjectOnPlane(tourCamera.transform.forward, Vector3.up).normalized, right = Vector3.ProjectOnPlane(tourCamera.transform.right, Vector3.up).normalized;
                player.InputOverride = new Vector2(Vector3.Dot(wanted, right), Vector3.Dot(wanted, forward));
                yield return null;
                Vector3 position = player.transform.position; float distance = Vector3.ProjectOnPlane(position - last, Vector3.up).magnitude;
                record.travelledMetres += distance; last = position;
                record.samples.Add(new Sample { t = Time.realtimeSinceStartup - began, x = position.x, y = position.y, z = position.z, speed = distance / Mathf.Max(.0001f, Time.deltaTime), phase = motion != null ? motion.WalkPhase : 0, walkBlend = motion != null ? motion.WalkBlend : 0, yaw = player.transform.eulerAngles.y, frameMs = Time.unscaledDeltaTime * 1000, cameraYaw = cameraRig.Yaw, cameraPitch = cameraRig.Pitch, leg = leg });
                if (captureFrames && Time.realtimeSinceStartup - began >= nextFrame)
                {
                    yield return new WaitForEndOfFrame();
                    float capturedAt = Time.realtimeSinceStartup - began;
                    Texture2D frame = ScreenCapture.CaptureScreenshotAsTexture();
                    string filename = "frame-" + record.capturedFrames.Count.ToString("D5") + ".jpg";
                    File.WriteAllBytes(Path.Combine(folder, "frames", filename), frame.EncodeToJPG(94)); Destroy(frame);
                    record.capturedFrames.Add(new CapturedFrame { file = filename, t = capturedAt }); nextFrame = capturedAt + 1f / 30f;
                }
            }
            player.InputOverride = Vector2.zero; record.movementSeconds = Time.realtimeSinceStartup - began;
            record.pass = record.travelledMetres > 60 && record.visitedWaypoints >= 8;
            File.WriteAllText(Path.Combine(folder, "tour.json"), JsonUtility.ToJson(record, true));
            Debug.Log("[Echoes][P52 TOUR] " + (record.pass ? "PASS" : "FAIL") + " distance=" + record.travelledMetres + "m visited=" + record.visitedWaypoints + " seconds=" + record.movementSeconds + "; automated, human QA PENDING.");
            yield return new WaitForSecondsRealtime(.5f);
            // Teleport only AFTER the recorded continuous route, for matched diagnostic stills.
            Vector3 foot = Ground(pilot, new Vector2(4.7f, 24f)); foot.y += .08f;
            player.Teleport(foot); player.transform.rotation = Quaternion.Euler(0, 180, 0); yield return new WaitForSecondsRealtime(.4f);
            cameraRig.enabled = false; FindFirstObjectByType<HudController>().HideBanner();
            Vector3 chest = foot + Vector3.up * .95f, head = foot + Vector3.up * 1.52f;
            yield return Shot("hero-front", foot + new Vector3(0, 1.10f, -2.6f), chest);
            yield return Shot("hero-side", foot + new Vector3(2.6f, 1.10f, 0), chest);
            yield return Shot("hero-rear", foot + new Vector3(0, 1.10f, 2.6f), chest);
            yield return Shot("hero-threeq", foot + new Vector3(-1.9f, 1.25f, -2.0f), chest);
            yield return Shot("hero-face", head + new Vector3(0.12f, 0.03f, -0.95f), head + new Vector3(0, -.01f, 0), 22f);
            yield return Shot("hero-face-threeq", head + new Vector3(-.55f, 0.05f, -.75f), head + new Vector3(0, -.01f, 0), 24f);
            yield return Shot("hero-near-camera", foot + new Vector3(.2f, 1.2f, 1.55f), chest);
            // Matched route stills (same hero, camera and place in the baseline and in the pilot): workshop approach, garden path, windmill approach.
            yield return RouteShot(pilot, "route-workshop", new Vector2(17.5f, 6.8f), 75f, 70f, 20f);
            yield return RouteShot(pilot, "route-garden", new Vector2(9.6f, -9.2f), 0f, 5f, 22f);
            yield return RouteShot(pilot, "route-windmill", new Vector2(3.2f, 24f), 10f, 8f, 20f);
            yield return RouteShot(pilot, "route-plaza-east", new Vector2(11.5f, 0.5f), 80f, 78f, 24f);
            finished = true; File.WriteAllText(Path.Combine(folder, "done.flag"), record.pass ? "PASS" : "FAIL"); yield return new WaitForSecondsRealtime(1f); Application.Quit(record.pass ? 0 : 1);
        }

        IEnumerator RouteShot(Phase52Pilot pilot, string name, Vector2 xz, float heroYaw, float cameraYaw, float pitch)
        {
            Vector3 at = Ground(pilot, xz); player.Teleport(at); player.transform.rotation = Quaternion.Euler(0, heroYaw, 0); yield return new WaitForSecondsRealtime(.3f);
            Quaternion rotation = Quaternion.Euler(pitch, cameraYaw, 0); Vector3 pivot = at + Vector3.up * 1.5f;
            yield return Shot(name, pivot + rotation * Vector3.back * 6f, pivot - Vector3.up * .2f);
        }

        IEnumerator Shot(string name, Vector3 at, Vector3 target, float fov = 0f)
        {
            float original = tourCamera.fieldOfView; if (fov > 0) tourCamera.fieldOfView = fov;
            tourCamera.transform.SetPositionAndRotation(at, Quaternion.LookRotation(target - at)); yield return new WaitForSecondsRealtime(.15f); yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, name + ".png")); yield return new WaitForSecondsRealtime(.25f); tourCamera.fieldOfView = original;
        }
        static string Argument(string name) { string[] args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
    }
}
