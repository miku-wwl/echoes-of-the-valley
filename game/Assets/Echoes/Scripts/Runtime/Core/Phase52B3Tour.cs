using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// Opt-in Phase 5.2-B3 evidence run ("-echoes-b3-tour" with "-echoes-tour-output folder"). AUTOMATED input through
    /// PlayerController.InputOverride on the real controller under the fixed 2.5D camera; no teleport during the recorded route. The route
    /// stands idle, zooms, walks the four diagonals, stops at Mira and talks to her, walks past the workshop, circles under the willow canopy,
    /// gathers a Dawn Blossom, climbs to the windmill, turns back and ends beside Wren at the garden. Afterwards (teleports allowed, labelled
    /// in the stills' names) it takes the stills of the visual gate. Presentation evidence only; not a human play session.
    /// </summary>
    public sealed class Phase52B3Tour : MonoBehaviour
    {
        [Serializable] sealed class Sample { public float t, x, y, z, speed, runPhase, cameraYaw, cameraPitch, cameraDistance, frameMs; public string step, dir, shown; public int frame; public bool running, mirrored; }
        [Serializable] sealed class Event { public float t; public string what; }
        [Serializable] sealed class CapturedFrame { public string file; public float t; }
        [Serializable] sealed class Record
        {
            public string input = "AUTOMATED PlayerController.InputOverride and FixedFollowCamera25D.SetZoom; the real controller, gravity, collision, interaction detector and dialogue";
            public bool teleportsDuringRoute;
            public float routeSeconds, travelledMetres;
            public int width, height;
            public bool pass;
            public string camera = "";
            public string presentation = "";
            public List<Sample> samples = new List<Sample>();
            public List<Event> events = new List<Event>();
            public List<string> failures = new List<string>();
            public string videoCapture = "None";
            public List<CapturedFrame> capturedFrames = new List<CapturedFrame>();
            public List<string> stills = new List<string>();
        }

        PlayerController player;
        FixedFollowCamera25D follow;
        CharacterVisual2D5D presenter;
        InteractionDetector detector;
        DialogueBox dialogue;
        Camera view;
        Phase52Pilot pilot;
        Record record;
        string folder, step = "setup";
        bool recording, captureFrames, finished;
        float began, launched, nextFrame, defaultZoom = 8f;
        Vector3 last;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Begin()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-echoes-b3-tour") < 0) return;
            var root = new GameObject("AutomatedPhase52B3Evidence");
            DontDestroyOnLoad(root);
            root.AddComponent<Phase52B3Tour>();
        }

        void Start()
        {
            launched = Time.realtimeSinceStartup;
            StartCoroutine(Run());
        }

        void Update()
        {
            if (!finished && Time.realtimeSinceStartup - launched > 260f) { Debug.LogError("[Echoes][P52 B3 TOUR] TIMEOUT"); Application.Quit(1); }
        }

        void LateUpdate()
        {
            if (!recording) return;
            Vector3 p = player.transform.position;
            float d = Vector3.ProjectOnPlane(p - last, Vector3.up).magnitude;
            last = p;
            record.travelledMetres += d;
            var s = new Sample
            {
                t = Time.realtimeSinceStartup - began, x = p.x, y = p.y, z = p.z, speed = d / Mathf.Max(0.0001f, Time.deltaTime), frameMs = Time.unscaledDeltaTime * 1000f, step = step,
                cameraYaw = follow != null ? follow.Yaw : 0f, cameraPitch = follow != null ? follow.Pitch : 0f, cameraDistance = Vector3.Distance(view.transform.position, p),
            };
            if (presenter != null && presenter.isActiveAndEnabled)
            {
                s.dir = presenter.Direction; s.shown = presenter.ShownDirection; s.frame = presenter.FrameIndex; s.running = presenter.Running; s.runPhase = presenter.RunPhase; s.mirrored = presenter.Mirrored;
            }
            record.samples.Add(s);
        }

        IEnumerator CaptureLoop()
        {
            Directory.CreateDirectory(Path.Combine(folder, "frames"));
            while (recording)
            {
                yield return new WaitForEndOfFrame();
                if (!recording) break;
                float now = Time.realtimeSinceStartup - began;
                if (now < nextFrame) continue;
                Texture2D frame = ScreenCapture.CaptureScreenshotAsTexture();
                string file = "frame-" + record.capturedFrames.Count.ToString("D5") + ".jpg";
                File.WriteAllBytes(Path.Combine(folder, "frames", file), frame.EncodeToJPG(94));
                Destroy(frame);
                record.capturedFrames.Add(new CapturedFrame { file = file, t = now });
                nextFrame = now + 1f / 30f;
            }
        }

        Vector3 Ground(Vector2 p)
        {
            var v = new Vector3(p.x, 0f, p.y);
            Terrain t = pilot != null ? pilot.Terrain : Terrain.activeTerrain;
            v.y = t.SampleHeight(v) + t.transform.position.y + 0.05f;
            return v;
        }

        void Mark(string what)
        {
            record.events.Add(new Event { t = Time.realtimeSinceStartup - began, what = what });
            Debug.Log("[Echoes][P52 B3 TOUR] " + what);
        }

        IEnumerator Run()
        {
            folder = Argument("-echoes-tour-output");
            if (string.IsNullOrEmpty(folder)) throw new IOException("Explicit fresh tour output required.");
            Directory.CreateDirectory(folder);
            if (File.Exists(Path.Combine(folder, "tour.json"))) throw new IOException("Tour output already exists.");
            yield return null;
            var menu = FindFirstObjectByType<MainMenuController>();
            if (menu != null) menu.PlayButton.onClick.Invoke();
            while ((player = FindFirstObjectByType<PlayerController>()) == null) yield return null;
            yield return null;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            view = Camera.main;
            follow = FindFirstObjectByType<FixedFollowCamera25D>();
            if (follow != null) defaultZoom = follow.Distance;
            presenter = player.GetComponentInChildren<CharacterVisual2D5D>(true);
            detector = FindFirstObjectByType<InteractionDetector>();
            dialogue = FindFirstObjectByType<DialogueBox>();
            pilot = FindFirstObjectByType<Phase52Pilot>();
            var b3 = FindFirstObjectByType<Phase52B3Pilot>();
            record = new Record { width = Screen.width, height = Screen.height };
            record.presentation = b3 == null ? "no B3 pilot in scene" : b3.LegacyHeroMode ? "B2 skinned 3D hero under the B3 camera (comparison mode)" : "B3 illustrated 2.5D sprite";
            record.camera = follow == null ? "none" : "FixedFollowCamera25D yaw=" + follow.Yaw + " pitch=" + follow.Pitch + " distance=" + follow.Distance + " (zoom " + follow.MinDistance + "-" + follow.MaxDistance + ") fov=" + view.fieldOfView;
            captureFrames = Array.IndexOf(Environment.GetCommandLineArgs(), "-echoes-b3-record-frames") >= 0;
            if (captureFrames) record.videoCapture = "Actual ScreenCapture.CaptureScreenshotAsTexture at WaitForEndOfFrame; original monotonic timestamps; no desktop capture or concept compositing";

            player.Teleport(Ground(new Vector2(11.0f, -1.6f)));
            player.transform.rotation = Quaternion.Euler(0f, (follow != null ? follow.Yaw : 0f) + 180f, 0f);
            if (follow != null) follow.Snap();
            yield return new WaitForSecondsRealtime(0.8f);
            FindFirstObjectByType<HudController>().HideBanner();
            File.WriteAllText(Path.Combine(folder, "ready.flag"), "Actual Windows player ready; the recorded route begins after three seconds.\n");
            Debug.Log("[Echoes][P52 B3 TOUR] READY; AUTOMATED_INPUT; " + record.presentation + "; " + record.camera);
            yield return new WaitForSecondsRealtime(3f);

            began = Time.realtimeSinceStartup;
            last = player.transform.position;
            recording = true;
            if (captureFrames) StartCoroutine(CaptureLoop());
            yield return Route();
            player.InputOverride = Vector2.zero;
            record.routeSeconds = Time.realtimeSinceStartup - began;
            yield return new WaitForSecondsRealtime(0.1f);
            recording = false;
            record.pass = record.failures.Count == 0 && record.travelledMetres > 80f;
            Debug.Log("[Echoes][P52 B3 TOUR] ROUTE " + (record.pass ? "PASS" : "FAIL") + " metres=" + record.travelledMetres.ToString("F1") + " seconds=" + record.routeSeconds.ToString("F1") + " failures=" + string.Join(" | ", record.failures));
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Stills();
            File.WriteAllText(Path.Combine(folder, "tour.json"), JsonUtility.ToJson(record, true));
            finished = true;
            File.WriteAllText(Path.Combine(folder, "done.flag"), record.pass ? "PASS" : "FAIL");
            yield return new WaitForSecondsRealtime(1f);
            Application.Quit(record.pass ? 0 : 1);
        }

        IEnumerator Route()
        {
            step = "idle"; Mark("idle start");
            yield return new WaitForSecondsRealtime(3.0f);
            step = "zoom";
            if (follow != null) { follow.SetZoom(follow.MinDistance); Mark("zoom in to " + follow.MinDistance); yield return new WaitForSecondsRealtime(2.2f); follow.SetZoom(defaultZoom); Mark("zoom back"); yield return new WaitForSecondsRealtime(1.2f); }
            step = "diagonals"; Mark("four diagonals (W+D, W+A, S+A, S+D)");
            foreach (Vector2 input in new[] { new Vector2(0.7071f, 0.7071f), new Vector2(-0.7071f, 0.7071f), new Vector2(-0.7071f, -0.7071f), new Vector2(0.7071f, -0.7071f) })
            {
                player.InputOverride = input;
                yield return new WaitForSecondsRealtime(0.7f);
            }
            yield return Stop("stop after diagonals", 0.9f);
            step = "to-mira";
            yield return Walk("walk to Mira", new Vector2(12.9f, 3.6f));
            yield return Stop("stop at Mira", 0.7f);
            yield return Talk("talk to Mira", FindFirstObjectByType<NpcInteractable>());
            step = "to-workshop";
            yield return Walk("walk to the workshop", new Vector2(16.5f, 5.0f), new Vector2(20.6f, 7.2f));
            yield return Stop("stop at the workshop door", 1.2f);
            step = "under-willow";
            Vector2 willow = new Vector2(14.5f, 11.0f);
            var circle = new List<Vector2> { new Vector2(18.2f, 9.6f) };
            for (int i = 0; i <= 8; i++) { float a = -0.35f + i * Mathf.PI / 4.2f; circle.Add(willow + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 2.3f); }
            yield return Walk("circle under the willow canopy", circle.ToArray());
            step = "gather";
            ResourceNode flower = Nearest(new Vector2(10f, 10.5f));
            yield return Walk("walk to the Dawn Blossom", new Vector2(flower.Position.x + 1.1f, flower.Position.z - 0.6f));
            yield return Stop("stop at the Dawn Blossom", 0.5f);
            yield return Gather(flower);
            step = "to-windmill";
            yield return Walk("walk to the windmill", new Vector2(6.0f, 11.5f), new Vector2(1.4f, 14.0f), new Vector2(1.6f, 19.0f), new Vector2(3.0f, 24.6f));
            yield return Stop("stop below the windmill", 1.2f);
            step = "turn-back";
            yield return Walk("turn round and walk back", new Vector2(1.6f, 19.0f), new Vector2(0.8f, 11.5f), new Vector2(5.0f, 4.0f), new Vector2(7.2f, -5.6f), new Vector2(9.4f, -9.4f), new Vector2(10.2f, -12.6f));
            yield return Stop("stop beside Wren at the garden", 2.5f);
            step = "back-to-plaza";
            yield return Walk("walk back up the garden path", new Vector2(9.4f, -9.4f), new Vector2(7.2f, -5.6f), new Vector2(4.0f, -4.4f));
            yield return Stop("stop on the plaza", 2.5f);
        }

        IEnumerator Walk(string label, params Vector2[] points)
        {
            Mark(label);
            foreach (Vector2 target in points)
            {
                float started = Time.realtimeSinceStartup;
                while (true)
                {
                    Vector3 p = player.transform.position;
                    Vector2 delta = target - new Vector2(p.x, p.z);
                    if (delta.magnitude < 0.45f) break;
                    if (Time.realtimeSinceStartup - started > 9f) { record.failures.Add(label + ": did not reach " + target + " (stuck at " + p.ToString("F1") + ")"); break; }
                    Vector3 wanted = new Vector3(delta.x, 0f, delta.y).normalized;
                    Vector3 forward = Vector3.ProjectOnPlane(view.transform.forward, Vector3.up).normalized, right = Vector3.ProjectOnPlane(view.transform.right, Vector3.up).normalized;
                    player.InputOverride = new Vector2(Vector3.Dot(wanted, right), Vector3.Dot(wanted, forward));
                    yield return null;
                }
            }
        }

        IEnumerator Stop(string label, float seconds)
        {
            Mark(label);
            step = "stop";
            player.InputOverride = Vector2.zero;
            yield return new WaitForSecondsRealtime(seconds);
        }

        IEnumerator Talk(string label, Interactable who)
        {
            step = "dialogue";
            float wait = 0f;
            while (!ReferenceEquals(detector.Current, who) && wait < 1f) { wait += Time.unscaledDeltaTime; yield return null; }
            if (!ReferenceEquals(detector.Current, who) || !detector.TryInteract() || !dialogue.IsOpen) { record.failures.Add(label + ": interaction did not open the dialogue"); yield break; }
            Mark(label + ": dialogue open (" + dialogue.Speaker + ")");
            int shown = 0;
            while (dialogue.IsOpen)
            {
                yield return new WaitForSecondsRealtime(shown < 2 ? 2.4f : 0.05f);
                shown++;
                dialogue.Advance();
            }
            Mark(label + ": dialogue closed after " + shown + " lines");
            yield return new WaitForSecondsRealtime(0.4f);
        }

        IEnumerator Gather(ResourceNode node)
        {
            step = "gather";
            var session = GameSession.Current;
            int before = session.Inventory.Count(node.ItemId);
            float wait = 0f;
            while (!ReferenceEquals(detector.Current, node) && wait < 1f) { wait += Time.unscaledDeltaTime; yield return null; }
            bool ok = ReferenceEquals(detector.Current, node) && detector.TryInteract();
            int after = session.Inventory.Count(node.ItemId);
            if (!ok || after != before + 1) record.failures.Add("gather " + node.NodeId + ": interaction " + ok + ", count " + before + " -> " + after);
            Mark("gathered " + node.NodeId + " (" + before + " -> " + after + ")");
            yield return new WaitForSecondsRealtime(1.0f);
        }

        static ResourceNode Nearest(Vector2 xz)
        {
            ResourceNode best = null;
            float bestDistance = float.MaxValue;
            foreach (ResourceNode n in FindObjectsByType<ResourceNode>(FindObjectsSortMode.None))
            {
                float d = Vector2.Distance(new Vector2(n.Position.x, n.Position.z), xz);
                if (n.IsReady && d < bestDistance) { bestDistance = d; best = n; }
            }
            return best;
        }

        // ---- stills (after the recorded route; teleports set them up) ---------------------------------------------------------------

        IEnumerator Stills()
        {
            step = "stills";
            float cy = follow != null ? follow.Yaw : 0f;
            HudController hud = FindFirstObjectByType<HudController>();
            Vector3 spot = Ground(new Vector2(11.0f, -1.6f));
            var views = new (string name, float yaw)[] { ("hero-front-S", cy + 180f), ("hero-side-W", cy - 90f), ("hero-side-E", cy + 90f), ("hero-rear-N", cy), ("hero-diagonal-SE", cy + 135f), ("hero-diagonal-NW", cy - 45f) };
            foreach (var v in views)
            {
                yield return Place(spot, v.yaw, follow != null ? follow.MinDistance : 7f);
                hud.HideBanner();
                yield return Shot(v.name + "-closest-zoom");
            }
            foreach (var v in new[] { ("hero-front-S", cy + 180f), ("hero-side-W", cy - 90f), ("hero-rear-N", cy) })
            {
                yield return Place(spot, v.Item2, defaultZoom);
                yield return Shot(v.Item1 + "-default-zoom");
            }
            var mira = FindFirstObjectByType<NpcInteractable>();
            Vector3 camRight = Vector3.ProjectOnPlane(view.transform.right, Vector3.up).normalized;
            Vector3 beside = mira.transform.position + camRight * 1.5f;
            yield return Place(Ground(new Vector2(beside.x, beside.z)), Yaw(mira.transform.position - beside), defaultZoom);
            yield return Shot("beside-npc-mira");
            yield return Place(Ground(new Vector2(21.0f, 7.0f)), 90f, defaultZoom);
            yield return Shot("location-workshop");
            yield return Place(Ground(new Vector2(4.7f, 24.0f)), cy + 180f, defaultZoom);
            yield return Shot("location-windmill");
            yield return Place(Ground(new Vector2(10.4f, -11.4f)), 150f, defaultZoom);
            yield return Shot("location-garden");
            yield return Place(Ground(new Vector2(1.5f, 17.0f)), cy, defaultZoom);
            yield return Shot("location-north-path");
            yield return Place(Ground(new Vector2(15.4f, 10.2f)), cy + 120f, defaultZoom);
            yield return Shot("location-under-willow");
            yield return Place(Ground(new Vector2(1.5f, -6.5f)), cy + 180f, defaultZoom);
            yield return Shot("location-plaza");
            // dialogue and satchel: the same caretaker in the illustrated busts and the satchel's information panel
            Vector3 front = mira.Position + new Vector3(0f, 0.3f, -1.6f);
            yield return Place(Ground(new Vector2(front.x, front.z)), 0f, defaultZoom);
            if (ReferenceEquals(detector.Current, mira) && detector.TryInteract() && dialogue.IsOpen)
            {
                yield return new WaitForSecondsRealtime(0.4f);
                yield return Shot("dialogue-mira");
                while (dialogue.IsOpen) dialogue.Advance();
            }
            else record.failures.Add("stills: Mira's dialogue did not open");
            var wren = FindFirstObjectByType<GardenKeeperInteractable>();
            front = wren.Position + new Vector3(0f, 0.3f, -1.6f);
            yield return Place(Ground(new Vector2(front.x, front.z)), 0f, defaultZoom);
            if (ReferenceEquals(detector.Current, wren) && detector.TryInteract() && dialogue.IsOpen)
            {
                yield return new WaitForSecondsRealtime(0.4f);
                yield return Shot("dialogue-wren");
                while (dialogue.IsOpen) dialogue.Advance();
            }
            else record.failures.Add("stills: Wren's dialogue did not open");
            var satchel = FindFirstObjectByType<InventoryPanel>();
            yield return Place(spot, cy + 180f, defaultZoom);
            satchel.Open();
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shot("inventory-portrait");
            satchel.Close();
        }

        static float Yaw(Vector3 direction) => Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

        IEnumerator Place(Vector3 at, float yaw, float zoom)
        {
            player.Teleport(at);
            player.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (follow != null) { follow.SetZoom(zoom); follow.Snap(); }
            yield return new WaitForSecondsRealtime(0.6f);
            if (follow != null) follow.Snap();
            yield return null;
        }

        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, name + ".png"));
            record.stills.Add(name);
            yield return new WaitForSecondsRealtime(0.3f);
        }

        static string Argument(string name) { string[] args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
    }
}
