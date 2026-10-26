using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Echoes.EditorTools
{
    /// <summary>
    /// Renders the scene cameras to PNG files without entering Play Mode, for quick visual review in batch mode.
    /// Unity.exe -batchmode -quit -projectPath game -executeMethod Echoes.EditorTools.PreviewTools.CaptureAll -echoesShots dir
    /// Overlay UI is not part of these shots (the player smoke run captures the full screen).
    /// </summary>
    public static class PreviewTools
    {
        const int Width = 1600;
        const int Height = 900;

        [MenuItem("Echoes/Capture Preview Shots")]
        public static void CaptureAll()
        {
            string directory = ReadArgument("-echoesShots")
                ?? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "artifacts", "logs", "preview-shots"));
            Directory.CreateDirectory(directory);
            ShaderUtil.allowAsyncCompilation = false;   // otherwise the first stills show placeholder colours while shaders compile

            EditorSceneManager.OpenScene(AssetPaths.ValleyScene, OpenSceneMode.Single);
            Camera camera = Camera.main;
            // The very first render in a fresh editor still shows placeholder colours for some materials; render once and discard it.
            Shot(camera, directory, "warmup", new Vector3(0f, 3.4f, -38f), new Vector3(0f, 2f, -20f));
            File.Delete(Path.Combine(directory, "warmup.png"));
            Shot(camera, directory, "valley-1-spawn", new Vector3(0f, 3.4f, -38f), new Vector3(0f, 2f, -20f));
            Shot(camera, directory, "valley-2-heart", new Vector3(-6f, 3.5f, -13f), new Vector3(0f, 2.2f, 0f));
            Shot(camera, directory, "valley-3-cottage", new Vector3(11f, 3.4f, 0f), new Vector3(24f, 2.4f, 8f));
            Shot(camera, directory, "valley-4-spring-pond", new Vector3(-8f, 4.5f, 6f), new Vector3(-23f, 0.5f, 10f));
            Shot(camera, directory, "valley-5-overview", new Vector3(0f, 40f, -52f), new Vector3(0f, 0f, 2f));
            Shot(camera, directory, "valley-6-villager", new Vector3(10f, 2.2f, 2f), new Vector3(14.2f, 1.2f, 5.4f));
            Shot(camera, directory, "valley-7-hen-yard", At(15f, -11f, 3.2f), At(22f, -5f, 0.6f));
            Shot(camera, directory, "valley-8-herbs", At(-14f, 4f, 2.4f), At(-19.5f, 8.5f, 0.3f));
            Shot(camera, directory, "valley-9-flowers-ore", At(30f, 12f, 3f), At(35f, 18f, 0.6f));

            // The four characters, seen from the front
            Shot(camera, directory, "char-1-caretaker", At(0f, -29.2f, 1.45f), At(0f, -32f, 0.95f));
            Shot(camera, directory, "char-2-mira", Front(ValleyLayout.Villager, new Vector2(-14.2f, -5.4f), 2.5f, 1.45f), At(ValleyLayout.Villager.x, ValleyLayout.Villager.y, 0.95f));
            Shot(camera, directory, "char-3-wren", Front(ValleyLayout.GardenKeeper, ValleyLayout.Garden - ValleyLayout.GardenKeeper, 2.5f, 1.45f), At(ValleyLayout.GardenKeeper.x, ValleyLayout.GardenKeeper.y, 0.95f));
            Shot(camera, directory, "char-4-alder", Front(ValleyLayout.WindmillRestorer, ValleyLayout.Windmill - ValleyLayout.WindmillRestorer, 2.6f, 1.5f), At(ValleyLayout.WindmillRestorer.x, ValleyLayout.WindmillRestorer.y, 0.95f));

            // Phase 3 places in their silent state (stage 0): the withered garden, the broken windmill, the new villagers
            Vector2 garden = ValleyLayout.Garden;
            Vector2 mill = ValleyLayout.Windmill;
            Shot(camera, directory, "valley-12-garden-dormant", At(garden.x - 10f, garden.y - 5f, 3.4f), At(garden.x, garden.y, 0.6f));
            Shot(camera, directory, "valley-13-windmill-broken", At(mill.x - 3f, mill.y - 18f, 3.6f), At(mill.x, mill.y, 4.6f));
            Shot(camera, directory, "valley-14-windmill-broken-close", At(mill.x - 7f, mill.y - 9f, 3.2f), At(mill.x, mill.y, 6.4f));
            Shot(camera, directory, "valley-15-keeper-slumped", At(ValleyLayout.GardenKeeper.x + 2f, ValleyLayout.GardenKeeper.y - 2.6f, 1.8f),
                At(ValleyLayout.GardenKeeper.x, ValleyLayout.GardenKeeper.y, 1.0f));
            Shot(camera, directory, "valley-16-restorer", At(ValleyLayout.WindmillRestorer.x - 2.6f, ValleyLayout.WindmillRestorer.y - 2.4f, 1.8f),
                At(ValleyLayout.WindmillRestorer.x, ValleyLayout.WindmillRestorer.y, 1.0f));

            ShowStage(1);
            Shot(camera, directory, "valley-10-stage1-spring", new Vector3(-8f, 4.5f, 6f), new Vector3(-23f, 0.5f, 10f));
            Shot(camera, directory, "valley-11-stage1-stream", At(-16f, -2f, 5f), At(-27f, -4f, 0f));

            Shot(camera, directory, "valley-7b-hen-yard", At(27.5f, -10f, 2.2f), At(22f, -5f, 0.5f));

            // Stage 2: the garden in bloom and the lively hen yard
            ShowStage(2);
            Shot(camera, directory, "valley-17-stage2-garden", At(garden.x - 10f, garden.y - 5f, 3.4f), At(garden.x, garden.y, 0.6f));
            Shot(camera, directory, "valley-18-stage2-garden-close", At(garden.x - 6.5f, garden.y - 1.5f, 1.8f), At(garden.x + 1f, garden.y, 0.5f));
            Shot(camera, directory, "valley-19-stage2-hen-yard", At(27.5f, -10f, 2.4f), At(22f, -5f, 0.8f));

            // Stage 3: the mended windmill (the turning is runtime-only), lanterns and flowers through the village
            ShowStage(3);
            Shot(camera, directory, "valley-20-stage3-windmill", At(mill.x - 3f, mill.y - 18f, 3.6f), At(mill.x, mill.y, 4.6f));
            Shot(camera, directory, "valley-21-stage3-windmill-close", At(mill.x - 7f, mill.y - 9f, 3.2f), At(mill.x, mill.y, 6.4f));
            Shot(camera, directory, "valley-22-stage3-plaza", At(-12f, -12f, 3.6f), At(0f, 0f, 1.6f));
            Shot(camera, directory, "valley-23-stage3-overview", new Vector3(0f, 44f, -50f), new Vector3(0f, 0f, 4f));

            CaptureValleyUi(directory);

            EditorSceneManager.OpenScene(AssetPaths.MainMenuScene, OpenSceneMode.Single);
            Shot(Camera.main, directory, "menu-backdrop", Camera.main.transform.position, new Vector3(0f, 4f, 22f));
            Canvas menuCanvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            ShowCanvasInCamera(menuCanvas, Camera.main);
            Shot(Camera.main, directory, "ui-main-menu", Camera.main.transform.position, new Vector3(0f, 4f, 22f), 1280, 720);
            menuCanvas.transform.Find("ConfirmPanel").gameObject.SetActive(true);
            SetText(menuCanvas.transform, "ConfirmPanel/Box/Message",
                "You have a saved game. Starting a new game replaces it the first time the new game saves. Continue keeps it until then.");
            Shot(Camera.main, directory, "ui-main-menu-confirm", Camera.main.transform.position, new Vector3(0f, 4f, 22f), 1280, 720);
            menuCanvas.transform.Find("ConfirmPanel").gameObject.SetActive(false);
            menuCanvas.transform.Find("CreditsPanel").gameObject.SetActive(true);
            Shot(Camera.main, directory, "ui-main-menu-credits", Camera.main.transform.position, new Vector3(0f, 4f, 22f), 1280, 720);
            Debug.Log("[Echoes] Preview shots written to " + directory);
        }

        /// <summary>HUD, satchel, workshop and pause screens at 1280x720, rendered from the generated hierarchy with sample text.</summary>
        static void CaptureValleyUi(string directory)
        {
            EditorSceneManager.OpenScene(AssetPaths.ValleyScene, OpenSceneMode.Single);
            Camera camera = Camera.main;
            Canvas canvas = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).First(c => c.name == "HUD");
            ShowCanvasInCamera(canvas, camera);
            Vector3 position = At(0f, -36f, 3.4f);
            Vector3 lookAt = At(0f, -18f, 2f);
            Transform root = canvas.transform;

            SetText(root, "ObjectivePanel/Heading", "Restore the Stream");
            SetText(root, "ObjectivePanel/Body", "Gather Moonleaf Herb 1/2 and Dawn Blossom 0/1.");
            SetText(root, "StatusPanel/Coins", "Coins  45");
            SetText(root, "StatusPanel/Hen", "Clover: laying an egg (12 s)");
            SetText(root, "Toast0", "+1 Moonleaf Herb");
            SetText(root, "Toast1", "Crafted Spring Tonic");
            SetText(root, "Toast2", "Clover munches the feed happily.");
            SetText(root, "InteractionPrompt/Text", "[E]  Gather Moonleaf Herb");
            SetText(root, "Banner/Title", "New Quest");
            SetText(root, "Banner/Subtitle", "Restore the Stream");
            root.Find("Banner").GetComponent<CanvasGroup>().alpha = 1f;
            root.Find("InteractionPrompt").gameObject.SetActive(true);
            Shot(camera, directory, "ui-hud", position, lookAt, 1280, 720);

            root.Find("Banner").GetComponent<CanvasGroup>().alpha = 0f;
            root.Find("InteractionPrompt").gameObject.SetActive(false);
            SetText(root, "Dialogue/NamePlate/Speaker", "Mira");
            root.Find("Dialogue/NamePlate").GetComponent<Image>().color = DialogueBox.SpeakerColor("Mira");
            SetText(root, "Dialogue/Line", "Gather two Moonleaf Herbs and a Dawn Blossom from the meadows, then mix them at the alchemy workshop, my cottage with the cauldron.");
            root.Find("Dialogue").gameObject.SetActive(true);
            Shot(camera, directory, "ui-dialogue", position, lookAt, 1280, 720);
            root.Find("Dialogue").gameObject.SetActive(false);

            Transform inventory = root.Find("InventoryPanel");
            SetText(inventory, "Summary", "Slots 4/8     Coins 45");
            string[] names = { "Moonleaf Herb", "Dawn Blossom", "Chicken Feed", "Spring Tonic" };
            string[] counts = { "x2 / 9", "x1 / 9", "x3 / 9", "x1 / 9" };
            Color[] tints = { new Color(0.30f, 0.78f, 0.66f), new Color(1f, 0.60f, 0.38f), new Color(0.95f, 0.82f, 0.45f), new Color(0.45f, 0.85f, 0.95f) };
            for (int i = 0; i < Inventory.DefaultSlots; i++)
            {
                bool filled = i < names.Length;
                SetText(inventory, "Name" + i, filled ? names[i] : "- empty -");
                SetText(inventory, "Count" + i, filled ? counts[i] : string.Empty);
                inventory.Find("Icon" + i).GetComponent<Image>().color = filled ? tints[i] : new Color(1f, 1f, 1f, 0.12f);
            }
            inventory.gameObject.SetActive(true);
            Shot(camera, directory, "ui-inventory", position, lookAt, 1280, 720);
            inventory.gameObject.SetActive(false);

            Transform crafting = root.Find("CraftingPanel");
            SetText(crafting, "RecipeName0", "[1]  Spring Tonic");
            SetText(crafting, "RecipeDetail0", "<color=#7fe39a>Moonleaf Herb 2/2</color>   <color=#7fe39a>Dawn Blossom 1/1</color>\nWakes sleeping water. Pour it into the ruined spring.  Makes Spring Tonic.");
            SetText(crafting, "RecipeName1", "[2]  Chicken Feed");
            SetText(crafting, "RecipeDetail1", "<color=#ff8f8f>Dawn Blossom 0/1</color>\nOne blossom makes three portions for Clover.  Makes 3 x Chicken Feed.");
            SetText(crafting, "RecipeName2", "[3]  Windmill Braces");
            SetText(crafting, "RecipeDetail2", "<color=#ff8f8f>Echo Ore 1/4</color>\nRinging ore struts. They mend the old windmill.  Makes Windmill Braces.");
            SetText(crafting, "Status", "Crafted Spring Tonic.");
            crafting.gameObject.SetActive(true);
            Shot(camera, directory, "ui-crafting", position, lookAt, 1280, 720);
            crafting.gameObject.SetActive(false);

            root.Find("PauseMenu").gameObject.SetActive(true);
            Shot(camera, directory, "ui-pause", position, lookAt, 1280, 720);
            root.Find("PauseMenu").gameObject.SetActive(false);

            // Phase 3 text: the longest objective, prompt, banner and dialogue speaker, so any overflow shows up
            SetText(root, "ObjectivePanel/Heading", QuestLog.GardenHeading);
            SetText(root, "ObjectivePanel/Body", "Gather a Dawn Blossom, mix Chicken Feed at the workshop, then feed Clover.");
            SetText(root, "InteractionPrompt/Text", "[E]  Talk to Alder, the Windmill Restorer");
            SetText(root, "Banner/Title", "The Garden Blooms");
            SetText(root, "Banner/Subtitle", "Colour and warmth spread through the village.");
            root.Find("Banner").GetComponent<CanvasGroup>().alpha = 1f;
            root.Find("InteractionPrompt").gameObject.SetActive(true);
            Shot(camera, directory, "ui-hud-garden", position, lookAt, 1280, 720);
            root.Find("Banner").GetComponent<CanvasGroup>().alpha = 0f;
            root.Find("InteractionPrompt").gameObject.SetActive(false);

            SetText(root, "Dialogue/NamePlate/Speaker", "Alder, the Windmill Restorer");
            root.Find("Dialogue/NamePlate").GetComponent<Image>().color = DialogueBox.SpeakerColor("Alder, the Windmill Restorer");
            SetText(root, "Dialogue/Line", "I need Windmill Braces to mend the supports. Gather 4 Echo Ore from the pale outcrops around the valley's edge (you have 0), mix Windmill Braces at the workshop and bring them here.");
            root.Find("Dialogue").gameObject.SetActive(true);
            Shot(camera, directory, "ui-dialogue-alder", position, lookAt, 1280, 720);
            root.Find("Dialogue").gameObject.SetActive(false);

            root.Find("EndingPanel").gameObject.SetActive(true);
            Shot(camera, directory, "ui-ending", position, lookAt, 1280, 720);
        }

        static void ShowCanvasInCamera(Canvas canvas, Camera camera)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
        }

        static void SetText(Transform root, string path, string value)
        {
            Transform target = root.Find(path);
            if (target == null) throw new InvalidOperationException("Preview: no UI element '" + path + "' under " + root.name);
            target.GetComponent<Text>().text = value;
        }

        /// <summary>A camera position in front of a character that faces <paramref name="facing"/>, at the given distance and height above the ground.</summary>
        static Vector3 Front(Vector2 spot, Vector2 facing, float distance, float above)
        {
            Vector2 p = spot + facing.normalized * distance;
            return At(p.x, p.y, above);
        }

        static Vector3 At(float x, float z, float above)
        {
            Terrain terrain = Terrain.activeTerrain;
            return new Vector3(x, terrain.SampleHeight(new Vector3(x, 0f, z)) + terrain.transform.position.y + above, z);
        }

        /// <summary>
        /// Edit-mode stand-in for ResonanceView (the real one runs in Play Mode): shows the objects each stage switches on, so the
        /// still shots show every stage's world. Lighting shifts, the pulse and the windmill's turning only exist at run time.
        /// </summary>
        static void ShowStage(int stage)
        {
            ResonanceView view = UnityEngine.Object.FindFirstObjectByType<ResonanceView>();
            SerializedObject so = new SerializedObject(view);
            bool stream = stage >= 1;
            bool garden = stage >= 2;
            bool valley = stage >= 3;

            SetActive(so, "troubledStream", !stream);
            SetActive(so, "clearStream", stream);
            SetActive(so, "bloomRoot", stream);
            Material water = (Material)so.FindProperty(stream ? "clearWater" : "murkyWater").objectReferenceValue;
            ((Renderer)so.FindProperty("pond").objectReferenceValue).sharedMaterial = water;
            Renderer springWater = (Renderer)so.FindProperty("springWater").objectReferenceValue;
            springWater.sharedMaterial = water;
            springWater.transform.localScale = so.FindProperty(stream ? "springWaterFull" : "springWaterDry").vector3Value;

            SetActive(so, "gardenDormant", !garden);
            SetActive(so, "gardenBloomRoot", garden);
            SetActive(so, "yardLifeRoot", garden);
            SetActive(so, "villageLifeRoot", valley);
            SerializedProperty damaged = so.FindProperty("windmillDamaged");
            SerializedProperty repaired = so.FindProperty("windmillRepaired");
            for (int i = 0; i < damaged.arraySize; i++) ((GameObject)damaged.GetArrayElementAtIndex(i).objectReferenceValue).SetActive(!valley);
            for (int i = 0; i < repaired.arraySize; i++) ((GameObject)repaired.GetArrayElementAtIndex(i).objectReferenceValue).SetActive(valley);

            // Looping effects that belong to the stage, and the stage's real mood (the same code the game runs)
            SetSystemActive(so, "fountain", stream);
            SetSystemActive(so, "gardenMotes", garden);
            SetSystemActive(so, "valleyMotes", valley);
            UnityEngine.Object.FindFirstObjectByType<ValleyLighting>().ApplyNow(stream ? 1f : 0f, stream ? 1f : 0f, garden ? 1f : 0f, valley ? 1f : 0f);
        }

        static void SetSystemActive(SerializedObject owner, string field, bool active) =>
            ((ParticleSystem)owner.FindProperty(field).objectReferenceValue).gameObject.SetActive(active);

        /// <summary>Looping particle systems are advanced a few seconds so the stills show them (Play Mode is not running).</summary>
        static void SimulateLoopingParticles()
        {
            foreach (ParticleSystem system in UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
                if (system.gameObject.activeInHierarchy && system.main.loop) system.Simulate(4f, false, true);
        }

        static void SetActive(SerializedObject owner, string field, bool active) =>
            ((GameObject)owner.FindProperty(field).objectReferenceValue).SetActive(active);

        static void Shot(Camera camera, string directory, string name, Vector3 position, Vector3 lookAt, int width = Width, int height = Height)
        {
            camera.transform.position = position;
            camera.transform.LookAt(lookAt);
            SimulateLoopingParticles();

            RenderTexture target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            camera.Render();

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            File.WriteAllBytes(Path.Combine(directory, name + ".png"), image.EncodeToPNG());

            RenderTexture.active = previous;
            camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(image);
        }

        static string ReadArgument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
    }
}
