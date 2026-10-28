using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Echoes.EditorTools
{
    /// <summary>
    /// Phase 5.2-B3 isolated 2.5D pilot. Copies the B2 pilot scenes (restored paths, canopy-fade foliage) into Art/Phase52B3/Scenes and
    /// swaps the presentation only: the caretaker becomes an illustrated sprite on the unchanged PlayerController root, the villagers get
    /// illustrated idle sprites on their unchanged NPC objects, a fixed three-quarter follow camera replaces the free orbit for play, and the
    /// dialogue box shows illustrated busts. Gameplay scripts, save data and the original scenes are untouched.
    /// </summary>
    public static class Phase52B3Builder
    {
        public const string Folder = "Assets/Echoes/Art/Phase52B3";
        public const string SceneFolder = Folder + "/Scenes";
        public const string Valley = SceneFolder + "/Valley.unity";
        public const string Menu = SceneFolder + "/MainMenu.unity";
        public const string CaretakerSheetPath = Folder + "/CaretakerSheet.asset";
        public const string ControlsText = "WASD  Move      Wheel  Zoom      E  Interact      I  Satchel      Esc  Pause";
        static readonly string[] Villagers = { "Mira", "Wren", "Alder" };

        [MenuItem("Echoes/Phase 5.2-B3/Generate 2.5D pilot")]
        public static void Generate()
        {
            AssetPaths.EnsureFolder(SceneFolder);
            Material sprite = SpriteMaterial("CaretakerSprite", Folder + "/Sprites/CaretakerAtlas.png");
            Material villagerSprite = SpriteMaterial("VillagerSprite", Folder + "/Sprites/VillagersAtlas.png");
            Material shadow = ShadowMaterial();
            SpriteSheet25D hero = HeroSheet(sprite.mainTexture as Texture2D);
            var villagerSheets = new Dictionary<string, SpriteSheet25D>();
            foreach (string name in Villagers) villagerSheets[name] = VillagerSheet(name, villagerSprite.mainTexture as Texture2D);
            var busts = new Dictionary<string, Sprite>();
            foreach (string name in new[] { "Caretaker", "Mira", "Wren", "Alder" }) busts[name] = Bust(name);

            // opening a scene unloads unreferenced in-memory assets, so everything is saved first and loaded again from disk afterwards
            AssetDatabase.SaveAssets();
            var scene = EditorSceneManager.OpenScene(Phase52PilotBuilder.Valley, OpenSceneMode.Single);
            if (!EditorSceneManager.SaveScene(scene, Valley, true)) throw new IOException("Could not copy the B2 pilot Valley.");
            scene = EditorSceneManager.OpenScene(Valley, OpenSceneMode.Single);
            sprite = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/CaretakerSprite.mat");
            villagerSprite = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/VillagerSprite.mat");
            shadow = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/ContactShadow.mat");
            hero = AssetDatabase.LoadAssetAtPath<SpriteSheet25D>(CaretakerSheetPath);
            foreach (string name in Villagers) villagerSheets[name] = AssetDatabase.LoadAssetAtPath<SpriteSheet25D>(Folder + "/VillagerSheet_" + name + ".asset");
            foreach (string name in new[] { "Caretaker", "Mira", "Wren", "Alder" }) busts[name] = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/Portraits/Bust_" + name + ".png");
            if (hero == null || sprite == null || villagerSprite == null || shadow == null || busts["Caretaker"] == null) throw new InvalidOperationException("B3 assets did not load back from disk.");

            var pilot = UnityEngine.Object.FindFirstObjectByType<Phase52Pilot>();
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            // the B2 skinned hero and the original rigid hero stay in the scene, disabled, as recoverable legacy visuals
            if (pilot.SkinnedVisual != null) { pilot.SkinnedVisual.SetActive(false); pilot.SkinnedVisual.name = "LegacySkinnedCaretaker_B2_Disabled"; }
            if (pilot.OriginalVisual != null) pilot.OriginalVisual.SetActive(false);
            var host = pilot.gameObject.AddComponent<Phase52B3Pilot>();

            var visual = new GameObject("CharacterVisual2D");
            visual.transform.SetParent(player.transform, false);
            var card = Card(visual.transform, "DirectionalSprite", sprite);
            var shadowGo = Shadow(visual.transform, shadow, 0.95f, 0.55f);
            var presenter = visual.AddComponent<CharacterVisual2D5D>();
            Wire.Set(presenter, "sheet", hero);
            Wire.Set(presenter, "player", player);
            Wire.Set(presenter, "card", card);
            Wire.Set(presenter, "contactShadow", shadowGo.transform);
            host.CharacterVisual = visual;
            host.LegacyVisuals = new[] { pilot.SkinnedVisual, pilot.OriginalVisual };

            var villagerVisuals = new List<GameObject>();
            var hiddenVillagerRenderers = new List<Renderer>();
            float phase = 0f;
            foreach (var npc in UnityEngine.Object.FindObjectsByType<NpcIdle>(FindObjectsSortMode.None))
            {
                string name = npc.gameObject.name;
                if (!villagerSheets.ContainsKey(name)) continue;
                foreach (Renderer r in npc.GetComponentsInChildren<Renderer>(true)) { hiddenVillagerRenderers.Add(r); r.enabled = false; }
                var go = new GameObject("VillagerVisual2D");
                go.transform.SetParent(npc.transform, false);
                var vCard = Card(go.transform, "IdleSprite", villagerSprite);
                Shadow(go.transform, shadow, 0.9f, 0.5f);
                var idle = go.AddComponent<NpcVisual2D5D>();
                Wire.Set(idle, "sheet", villagerSheets[name]);
                Wire.Set(idle, "card", vCard);
                Wire.Set(idle, "phaseSeconds", phase += 0.55f);
                villagerVisuals.Add(go);
            }
            host.VillagerVisuals = villagerVisuals.ToArray();
            host.HiddenVillagerRenderers = hiddenVillagerRenderers.ToArray();

            Camera camera = Camera.main;
            var legacyRig = camera.GetComponent<ThirdPersonCamera>();
            var follow = camera.gameObject.AddComponent<FixedFollowCamera25D>();
            Wire.Set(follow, "target", player.transform);
            Wire.Set(follow, "legacyRig", legacyRig);
            host.FollowCamera = follow;
            host.LegacyCamera = legacyRig;

            // dialogue busts: the caretaker listens on the left, the villager who speaks stands on the right
            var dialogue = UnityEngine.Object.FindFirstObjectByType<DialogueBox>();
            var dialoguePanel = dialogue.transform.Find("Dialogue");
            var portraits = dialogue.gameObject.AddComponent<DialoguePortraits25D>();
            Image left = BustImage(dialoguePanel.parent, "CaretakerBust_B3", busts["Caretaker"], new Vector2(-520f, 190f), BustHeight, dialoguePanel.GetSiblingIndex());
            Image right = BustImage(dialoguePanel.parent, "SpeakerBust_B3", busts["Mira"], new Vector2(520f, 190f), BustHeight, dialoguePanel.GetSiblingIndex());
            Wire.Set(portraits, "dialogue", dialogue);
            Wire.Set(portraits, "caretaker", left);
            Wire.Set(portraits, "speaker", right);
            Wire.Set(portraits, "mira", busts["Mira"]);
            Wire.Set(portraits, "wren", busts["Wren"]);
            Wire.Set(portraits, "alder", busts["Alder"]);
            host.DialogueBusts = new[] { left.gameObject, right.gameObject };

            // the satchel's Caretaker information shows the same character: the 01 illustration plus the in-game sprite at the same scale
            if (pilot.Portrait != null)
            {
                var note = pilot.Portrait.transform.Find("PilotPortraitNote");
                if (note != null) note.GetComponent<Text>().text = "Original AI illustration";
            }
            var controls = GameObject.Find("ControlsHint");
            if (controls != null) controls.GetComponent<Text>().text = ControlsText;   // the B3 camera has no mouse look

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            scene = EditorSceneManager.OpenScene(Phase52PilotBuilder.Menu, OpenSceneMode.Single);
            if (!EditorSceneManager.SaveScene(scene, Menu, true)) throw new IOException("Could not copy the B2 pilot menu.");
            AssetDatabase.SaveAssets();
            Debug.Log("[Echoes][P52 B3] 2.5D pilot generated: " + Valley + "; original and B2 scenes untouched.");
        }

        static Sprite25DQuad Card(Transform parent, string name, Material material)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = CardMeshAsset();
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return go.AddComponent<Sprite25DQuad>();
        }

        static GameObject Shadow(Transform parent, Material material, float width, float depth)
        {
            var go = new GameObject("ContactShadow", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = ShadowMeshAsset(width, depth);
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return go;
        }

        static Mesh CardMeshAsset()
        {
            string path = Folder + "/Sprite25DCard.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;
            mesh = new Mesh { name = "Sprite25DCard" };
            mesh.vertices = new[] { Vector3.zero, Vector3.right, Vector3.up, new Vector3(1, 1, 0) };
            mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            mesh.bounds = new Bounds(new Vector3(0f, 1.2f, 0f), new Vector3(5f, 5f, 5f));
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        static Mesh ShadowMeshAsset(float width, float depth)
        {
            string path = Folder + "/ContactShadow_" + Mathf.RoundToInt(width * 100) + "x" + Mathf.RoundToInt(depth * 100) + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;
            mesh = new Mesh { name = "ContactShadow" };
            float x = width / 2f, z = depth / 2f;
            mesh.vertices = new[] { new Vector3(-x, 0, -z), new Vector3(x, 0, -z), new Vector3(-x, 0, z), new Vector3(x, 0, z) };
            mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        static Material SpriteMaterial(string name, string texturePath)
        {
            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            if (importer == null) throw new FileNotFoundException("Run tools/art/phase52b3/make_hero_sprites.py and make_villager_sprites.py first: " + texturePath);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.mipmapFilter = TextureImporterMipFilter.KaiserFilter;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 2;
            importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;   // BC7 keeps the painted detail and a clean alpha edge
            importer.SaveAndReimport();
            string path = Folder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Echoes/Sprite25D")); AssetDatabase.CreateAsset(material, path); }
            material.shader = Shader.Find("Echoes/Sprite25D");
            material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            material.renderQueue = (int)RenderQueue.Transparent;
            // the sun shines from behind the fixed camera onto the facing cards: these weights keep the painted colours close to the
            // illustration in full sun and darken them by about half under a canopy shadow
            material.SetFloat("_AmbientWeight", 0.62f);
            material.SetFloat("_SunWeight", 0.78f);
            material.SetFloat("_ShadowFloor", 0.35f);
            material.SetFloat("_Exposure", 1f);
            material.SetFloat("_DepthPull", 0.45f);
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material ShadowMaterial()
        {
            string path = Folder + "/ContactShadow.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Echoes/ContactShadow")); AssetDatabase.CreateAsset(material, path); }
            material.shader = Shader.Find("Echoes/ContactShadow");
            material.renderQueue = (int)RenderQueue.Transparent - 20;
            EditorUtility.SetDirty(material);
            return material;
        }

        [Serializable] sealed class AtlasFrame { public string dir; public string villager; public string kind; public int index; public int x, y, w, h; }
        [Serializable] sealed class Direction { public float[] pivot; public int[] size; }
        [Serializable] sealed class HeroAtlas { public int[] atlas; public float pixelsPerMeter; public float strideMeters; public float idleFps; public AtlasFrame[] frames; }
        [Serializable] sealed class VillagerAtlas { public int[] atlas; public float idleFps; public AtlasFrame[] frames; }
        [Serializable] sealed class VillagerInfo { public float[] pivot; public float pixelsPerMeter; }

        static SpriteSheet25D HeroSheet(Texture2D atlas)
        {
            string json = File.ReadAllText(Folder + "/Sprites/CaretakerAtlas.json");
            var data = JsonUtility.FromJson<HeroAtlas>(json);
            var sheet = LoadOrCreate<SpriteSheet25D>(CaretakerSheetPath);
            sheet.atlas = atlas;
            sheet.pixelsPerMeter = data.pixelsPerMeter;
            sheet.strideMeters = data.strideMeters;
            sheet.idleFps = data.idleFps;
            var dirs = new List<SpriteSheet25D.Direction>();
            // E has its own frames: the side view without her left-only accessories, mirrored offline (see make_hero_sprites.py)
            foreach (string key in new[] { "S", "N", "W", "E" })
            {
                // the per-direction pivots live in a nested dictionary that JsonUtility cannot read, so they are parsed by key
                Vector2 pivot = ReadPivot(json, key);
                dirs.Add(new SpriteSheet25D.Direction { key = key, pivotPx = pivot, run = Frames(data, key, "run"), idle = Frames(data, key, "idle") });
            }
            sheet.directions = dirs.ToArray();
            EditorUtility.SetDirty(sheet);
            return sheet;
        }

        static SpriteSheet25D VillagerSheet(string name, Texture2D atlas)
        {
            string json = File.ReadAllText(Folder + "/Sprites/VillagersAtlas.json");
            var data = JsonUtility.FromJson<VillagerAtlas>(json);
            var info = JsonUtility.FromJson<VillagerInfo>(Section(json, "\"" + name + "\""));
            var sheet = LoadOrCreate<SpriteSheet25D>(Folder + "/VillagerSheet_" + name + ".asset");
            sheet.atlas = atlas;
            sheet.pixelsPerMeter = info.pixelsPerMeter;
            sheet.idleFps = data.idleFps;
            sheet.strideMeters = 1f;
            int count = 0;
            foreach (var f in data.frames) if (f.villager == name) count = Mathf.Max(count, f.index + 1);
            var ordered = new SpriteSheet25D.Frame[count];
            foreach (var f in data.frames) if (f.villager == name) ordered[f.index] = ToFrame(f, data.atlas);
            sheet.directions = new[] { new SpriteSheet25D.Direction { key = "S", pivotPx = new Vector2(info.pivot[0], info.pivot[1]), idle = ordered, run = ordered } };
            EditorUtility.SetDirty(sheet);
            return sheet;
        }

        static SpriteSheet25D.Frame[] Frames(HeroAtlas data, string dir, string kind)
        {
            var list = new List<AtlasFrame>();
            foreach (var f in data.frames) if (f.dir == dir && f.kind == kind) list.Add(f);
            list.Sort((a, b) => a.index.CompareTo(b.index));
            var result = new SpriteSheet25D.Frame[list.Count];
            for (int i = 0; i < list.Count; i++) result[i] = ToFrame(list[i], data.atlas);
            return result;
        }

        static SpriteSheet25D.Frame ToFrame(AtlasFrame f, int[] atlas) => new SpriteSheet25D.Frame
        {
            uv = new Vector4((float)f.x / atlas[0], (float)f.y / atlas[1], (float)f.w / atlas[0], (float)f.h / atlas[1]),
            sizePx = new Vector2(f.w, f.h),
        };

        static Vector2 ReadPivot(string json, string key)
        {
            var d = JsonUtility.FromJson<Direction>(Section(json, "\"" + key + "\""));
            return new Vector2(d.pivot[0], d.pivot[1]);
        }

        /// <summary>The JSON object that follows <paramref name="quotedKey"/> (first occurrence followed by a colon and a brace).</summary>
        static string Section(string json, string quotedKey)
        {
            int i = json.IndexOf(quotedKey + ": {", StringComparison.Ordinal);
            if (i < 0) throw new InvalidDataException("Missing " + quotedKey + " in atlas metadata");
            int start = json.IndexOf('{', i), depth = 0;
            for (int j = start; j < json.Length; j++)
            {
                if (json[j] == '{') depth++;
                else if (json[j] == '}' && --depth == 0) return json.Substring(start, j - start + 1);
            }
            throw new InvalidDataException("Unterminated " + quotedKey);
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static Sprite Bust(string name)
        {
            string path = Folder + "/Portraits/Bust_" + name + ".png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) throw new FileNotFoundException("Run tools/art/phase52b3/make_villager_sprites.py first: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        const float BustHeight = 440f;

        static Image BustImage(Transform parent, string name, Sprite sprite, Vector2 position, float height, int siblingIndex)
        {
            Vector2 size = new Vector2(height * sprite.rect.width / sprite.rect.height, height);
            var rect = UiKit.NewRect(name, parent).Place(UiKit.BottomCenter, UiKit.BottomCenter, position, size);
            rect.SetSiblingIndex(siblingIndex);   // behind the dialogue box, so the box overlaps the bust's fading bottom
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            rect.gameObject.SetActive(false);
            return image;
        }

        public static void BuildWindowsPilot()
        {
            string output = Argument("-echoesBuildPath");
            if (string.IsNullOrEmpty(output) || Directory.Exists(output)) throw new IOException("Use a fresh explicit -echoesBuildPath; existing builds are preserved.");
            Directory.CreateDirectory(output);
            string version = PlayerSettings.bundleVersion;
            try
            {
                PlayerSettings.bundleVersion = "0.5.2-pilot.3";
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { Menu, Valley }, locationPathName = Path.Combine(output, "EchoesOfTheValley.exe"), target = BuildTarget.StandaloneWindows64, targetGroup = BuildTargetGroup.Standalone, options = BuildOptions.None });
                var s = report.summary;
                Debug.Log("[Echoes][P52 B3] PILOT_BUILD " + s.result + " id=" + s.guid + " errors=" + s.totalErrors + " warnings=" + s.totalWarnings + " bytes=" + s.totalSize + "; owner approval PENDING, not a release candidate.");
                if (s.result != BuildResult.Succeeded) throw new Exception("B3 pilot build failed.");
            }
            finally { PlayerSettings.bundleVersion = version; }
        }

        static string Argument(string name) { string[] args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
    }
}
