using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Echoes.EditorTools
{
    internal struct WaterParts
    {
        public Transform Root;
        public Renderer Pond;
        public GameObject WideStream;
        public GameObject Trickle;
    }

    /// <summary>Generates the Valley scene from scratch on every run, so re-running replaces it instead of duplicating it.</summary>
    internal static class ValleySceneBuilder
    {
        /// <summary>Top offset of the banner: the objective panel ends at 164 reference pixels, the banner starts below it.</summary>
        public const float BannerTop = -190f;

        public static void Build(MaterialLibrary m, PrefabSet prefabs)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // The valley starts muted and cool; CompletionFeedback warms it up.
            Light sun = SceneKit.CreateSun(Quaternion.Euler(48f, 38f, 0f), new Color(0.86f, 0.90f, 1f), 1.0f);
            SceneKit.ConfigureEnvironment(m["SkyValley"], new Color(0.50f, 0.58f, 0.72f), new Color(0.46f, 0.48f, 0.54f),
                new Color(0.30f, 0.30f, 0.32f), new Color(0.62f, 0.68f, 0.76f), 0.007f);

            GameObject world = new GameObject("World");
            Terrain terrain = BuildTerrain(m, world.transform);

            HeartParts heart = Landmarks.Heart(world.transform, Ground(terrain, ValleyLayout.Heart), m);
            CottageParts cottage = Landmarks.Cottage(world.transform, Ground(terrain, ValleyLayout.Cottage), ValleyLayout.CottageYaw, m);
            SpringParts springParts = Landmarks.Spring(world.transform, Ground(terrain, ValleyLayout.Spring), 24f, m);
            Transform spring = springParts.Root;
            WaterParts water = BuildWater(world.transform, m);
            GameplayPieces.HenYardFence(world.transform, Ground(terrain, ValleyLayout.HenYard), m);
            Scatter(world.transform, terrain, prefabs);
            BuildBoundaries(world.transform);
            Pieces.MakeStatic(world);
            // Things that move, scale or swap materials at runtime must not be statically batched.
            Pieces.MakeDynamic(heart.CrystalPivot.gameObject);
            Pieces.MakeDynamic(water.Root.gameObject);
            Pieces.MakeDynamic(springParts.Water.gameObject);

            // Everything the player gathers, feeds or watches change lives outside the static scenery.
            Transform gameplay = new GameObject("Gameplay").transform;
            PlaceResourceNodes(gameplay, terrain, prefabs);
            PlaceHen(gameplay, terrain, prefabs);
            GameObject bloom = BuildBloom(gameplay, terrain, prefabs);

            // Phase 3: the garden, the windmill, the hen yard's decorations and the village's lanterns and flowers.
            // Each has a "before" and an "after" that ResonanceView switches between; none of it is static.
            GardenParts garden = Phase3Pieces.Garden(gameplay, Ground(terrain, ValleyLayout.Garden), m);
            WindmillParts windmill = Phase3Pieces.Windmill(gameplay, Ground(terrain, ValleyLayout.Windmill), ValleyLayout.WindmillYaw, m);
            GameObject yardLife = Phase3Pieces.YardLife(gameplay, Ground(terrain, ValleyLayout.HenYard), m);
            GameObject villageLife = BuildVillageLife(gameplay, terrain, prefabs, m);

            // ---- Player, camera ----
            Vector3 spawnPosition = Ground(terrain, ValleyLayout.Spawn) + Vector3.up * 0.1f;
            Transform spawn = new GameObject("SpawnPoint").transform;
            spawn.position = spawnPosition;

            GameObject player = new GameObject("Player");
            player.transform.position = spawnPosition;
            CharacterController controller = player.AddComponent<CharacterController>();
            controller.height = 1.6f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0f, 0.8f, 0f);
            controller.stepOffset = 0.3f;   // clears the 0.2 m and 0.1 m dais steps, but not the 0.4 m obelisk base
            controller.slopeLimit = 48f;
            controller.skinWidth = 0.04f;
            GameObject hero = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.Hero, player.transform);
            hero.name = "Visual";
            PlayerController playerController = player.AddComponent<PlayerController>();
            InteractionDetector detector = player.AddComponent<InteractionDetector>();

            Camera camera = SceneKit.CreateCamera(spawnPosition + new Vector3(0f, 4f, -6f), Quaternion.Euler(22f, 0f, 0f), 60f);
            ThirdPersonCamera rig = camera.gameObject.AddComponent<ThirdPersonCamera>();
            Wire.Set(rig, "target", player.transform);
            Wire.Set(playerController, "cameraTransform", camera.transform);
            Wire.Set(playerController, "characterAnimator", hero.GetComponent<CharacterAnimator>());
            Wire.Set(playerController, "respawnPoint", spawn);

            // ---- UI and game systems ----
            BuildHud(out HudController hud, out DialogueBox dialogue, out PauseController pause, out GameplayUi ui);
            EndingPanel ending = GameplayUiBuilder.BuildEnding(hud.GetComponent<Canvas>(), rig);
            SceneKit.CreateEventSystem();

            GameObject systems = new GameObject("GameSystems");
            systems.AddComponent<GameBootstrap>();
            ExplorationObjective objective = systems.AddComponent<ExplorationObjective>();
            CompletionFeedback feedback = systems.AddComponent<CompletionFeedback>();
            ValleyLighting lighting = systems.AddComponent<ValleyLighting>();
            GameSessionHost host = systems.AddComponent<GameSessionHost>();
            ResonanceView resonance = systems.AddComponent<ResonanceView>();
            Wire.Set(lighting, "sun", sun);
            Wire.Set(host, "objective", objective);
            Wire.Set(objective, "hud", hud);
            Wire.Set(objective, "feedback", feedback);
            Wire.Set(feedback, "sun", sun);
            Wire.Set(feedback, "lighting", lighting);
            Wire.Set(feedback, "sparkles", BuildSparkles(heart.Root));
            Wire.Set(feedback, "crystal", heart.Crystal);
            Wire.Set(feedback, "crystalPivot", heart.CrystalPivot);
            Wire.Set(detector, "hud", hud);

            // The world's answer to the restored stream (ResonanceStage 1)
            Wire.Set(resonance, "lighting", lighting);
            Wire.Set(resonance, "troubledStream", water.Trickle);
            Wire.Set(resonance, "clearStream", water.WideStream);
            Wire.Set(resonance, "pond", water.Pond);
            Wire.Set(resonance, "springWater", springParts.Water);
            Wire.Set(resonance, "murkyWater", m["WaterDull"]);
            Wire.Set(resonance, "clearWater", m["Water"]);
            Wire.Set(resonance, "bloomRoot", bloom);
            Wire.Set(resonance, "pulse", GameplayPieces.Pulse(spring));
            Wire.Set(resonance, "fountain", GameplayPieces.Fountain(spring));

            // The garden (stage 2) and the windmill (stage 3)
            Wire.Set(resonance, "gardenDormant", garden.Dormant);
            Wire.Set(resonance, "gardenBloomRoot", garden.Bloom);
            Wire.Set(resonance, "yardLifeRoot", yardLife);
            Wire.Set(resonance, "gardenMotes", Phase3Pieces.Motes(garden.Root, "GardenMotes", new Vector3(0f, 1.3f, 0f), new Vector3(9f, 1.6f, 7f),
                9f, 90, new Color(1f, 0.80f, 0.90f, 1f), new Color(1f, 0.95f, 0.62f, 1f), 0.16f));
            Wire.Set(resonance, "gardenPulse", GameplayPieces.RingPulse(garden.Root, "GardenPulse", new Vector3(0f, 0.4f, 0f), 8f, 2.2f, 0.35f, 110,
                new Color(1f, 0.82f, 0.72f, 1f)));

            WindmillRotator rotator = windmill.Root.gameObject.AddComponent<WindmillRotator>();
            Wire.Set(rotator, "sails", windmill.SailsPivot);
            Wire.Set(resonance, "windmill", rotator);
            Wire.Set(resonance, "windmillDamaged", windmill.Damaged);
            Wire.Set(resonance, "windmillRepaired", windmill.Repaired);
            Wire.Set(resonance, "villageLifeRoot", villageLife);
            Transform valleyMotes = Pieces.Group("ValleyMotesRoot", gameplay, Ground(terrain, ValleyLayout.Heart) + Vector3.up * 4f);
            Wire.Set(resonance, "valleyMotes", Phase3Pieces.Motes(valleyMotes, "ValleyMotes", Vector3.zero, new Vector3(80f, 8f, 80f),
                12f, 180, new Color(1f, 0.90f, 0.55f, 1f), new Color(1f, 1f, 0.90f, 1f), 0.22f));
            Wire.Set(resonance, "finalPulse", GameplayPieces.RingPulse(windmill.Root, "FinalPulse", new Vector3(0f, 0.5f, 0f), 16f, 3.6f, 0.8f, 260,
                new Color(1f, 0.88f, 0.55f, 1f)));

            // ---- Interactables: all of them use the same Interactable framework ----
            GameObject villager = PlaceVillager(prefabs.Villager, "Mira", ValleyLayout.Villager, Vector2.zero, terrain);
            NpcInteractable mira = villager.AddComponent<NpcInteractable>();
            Configure(mira, "Talk to Mira", 3f, "Mira", dialogue, objective);
            Wire.Set(mira, "endingPanel", ending);

            GameObject keeper = PlaceVillager(prefabs.GardenKeeper, "Wren", ValleyLayout.GardenKeeper, ValleyLayout.Garden, terrain);
            Configure(keeper.AddComponent<GardenKeeperInteractable>(), "Talk to Wren, the Garden Keeper", 3f, "Wren, the Garden Keeper", dialogue, objective);
            GameObject restorer = PlaceVillager(prefabs.WindmillRestorer, "Alder", ValleyLayout.WindmillRestorer, ValleyLayout.Windmill, terrain);
            Configure(restorer.AddComponent<WindmillRestorerInteractable>(), "Talk to Alder, the Windmill Restorer", 3f, "Alder, the Windmill Restorer", dialogue, objective);
            Wire.Set(resonance, "villagers", new[] { AddIdle(villager, 1, 0f), AddIdle(keeper, 2, 1.3f), AddIdle(restorer, 2, 2.6f) });
            WorkshopInteractable workshop = cottage.InteractionPoint.gameObject.AddComponent<WorkshopInteractable>();
            Configure(workshop, "Use the alchemy workshop", 3.5f, "Alchemy Workshop", dialogue, objective);
            Wire.Set(workshop, "craftingPanel", ui.Crafting);
            Configure(spring.gameObject.AddComponent<SpringInteractable>(), "Examine the ruined spring", 3.8f, "The Ruined Spring", dialogue, objective);
            Configure(heart.Root.gameObject.AddComponent<HeartInteractable>(), "Touch the Heart Stone", 4.5f, "The Heart of the Valley", dialogue, objective);

            if (!EditorSceneManager.SaveScene(scene, AssetPaths.ValleyScene))
                throw new System.InvalidOperationException("Could not save " + AssetPaths.ValleyScene);
            Debug.Log("[Echoes] Generated " + AssetPaths.ValleyScene);
        }

        /// <summary>A villager prefab standing on the ground at <paramref name="spot"/>, facing <paramref name="lookAt"/>.</summary>
        static GameObject PlaceVillager(GameObject prefab, string name, Vector2 spot, Vector2 lookAt, Terrain terrain)
        {
            GameObject npc = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            npc.name = name;
            npc.transform.position = Ground(terrain, spot);
            Vector2 toward = lookAt - spot;
            npc.transform.rotation = Quaternion.LookRotation(new Vector3(toward.x, 0f, toward.y));
            return npc;
        }

        static NpcIdle AddIdle(GameObject npc, int firstAwakeStage, float phase)
        {
            NpcIdle idle = npc.AddComponent<NpcIdle>();
            Wire.Set(idle, "animator", npc.GetComponent<CharacterAnimator>());
            Wire.Set(idle, "firstAwakeStage", firstAwakeStage);
            Wire.Set(idle, "phaseOffset", phase);
            return idle;
        }

        static void Configure(DialogueInteractable interactable, string prompt, float range, string speaker, DialogueBox dialogue, ExplorationObjective objective)
        {
            Wire.Set(interactable, "prompt", prompt);
            Wire.Set(interactable, "range", range);
            Wire.Set(interactable, "speaker", speaker);
            Wire.Set(interactable, "dialogue", dialogue);
            Wire.Set(interactable, "objective", objective);
        }

        static Vector3 Ground(Terrain terrain, Vector2 p)
        {
            float y = terrain.SampleHeight(new Vector3(p.x, 0f, p.y)) + terrain.transform.position.y;
            return new Vector3(p.x, y, p.y);
        }

        // ---- Terrain ----------------------------------------------------------------------------------------------

        static Terrain BuildTerrain(MaterialLibrary m, Transform parent)
        {
            AssetPaths.EnsureFolder(AssetPaths.Terrain);
            const int heightRes = 129;
            const int alphaRes = 128;
            float size = ValleyLayout.TerrainSize;

            TerrainLayer[] layers =
            {
                MakeLayer("Grass", new Color(0.50f, 0.74f, 0.36f)),
                MakeLayer("Meadow", new Color(0.66f, 0.78f, 0.40f)),
                MakeLayer("Path", new Color(0.84f, 0.74f, 0.54f)),
                MakeLayer("Stone", new Color(0.68f, 0.64f, 0.58f))
            };

            string dataPath = AssetPaths.Terrain + "/ValleyTerrainData.asset";
            TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(dataPath);
            if (data == null)
            {
                data = new TerrainData();
                AssetDatabase.CreateAsset(data, dataPath);
            }

            data.heightmapResolution = heightRes;
            data.size = new Vector3(size, ValleyLayout.TerrainMaxHeight, size);
            data.terrainLayers = layers;
            data.alphamapResolution = alphaRes;

            float[,] heights = new float[heightRes, heightRes];
            for (int zi = 0; zi < heightRes; zi++)
            {
                for (int xi = 0; xi < heightRes; xi++)
                {
                    float x = (xi / (float)(heightRes - 1) - 0.5f) * size;
                    float z = (zi / (float)(heightRes - 1) - 0.5f) * size;
                    heights[zi, xi] = Mathf.Clamp01((ValleyLayout.Height(x, z) + ValleyLayout.TerrainBaseOffset) / ValleyLayout.TerrainMaxHeight);
                }
            }
            data.SetHeights(0, 0, heights);

            float[,,] alphas = new float[alphaRes, alphaRes, layers.Length];
            float[] weights = new float[layers.Length];
            for (int zi = 0; zi < alphaRes; zi++)
            {
                for (int xi = 0; xi < alphaRes; xi++)
                {
                    float x = ((xi + 0.5f) / alphaRes - 0.5f) * size;
                    float z = ((zi + 0.5f) / alphaRes - 0.5f) * size;
                    ValleyLayout.Weights(x, z, weights);
                    for (int l = 0; l < layers.Length; l++) alphas[zi, xi, l] = weights[l];
                }
            }
            data.SetAlphamaps(0, 0, alphas);
            EditorUtility.SetDirty(data);

            GameObject go = Terrain.CreateTerrainGameObject(data);
            go.name = "Terrain";
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(-size * 0.5f, -ValleyLayout.TerrainBaseOffset, -size * 0.5f);
            Terrain terrain = go.GetComponent<Terrain>();
            terrain.materialTemplate = m["TerrainLit"];
            terrain.drawInstanced = true;
            terrain.heightmapPixelError = 6f;
            terrain.drawTreesAndFoliage = false;
            return terrain;
        }

        static TerrainLayer MakeLayer(string name, Color color)
        {
            string texturePath = AssetPaths.Terrain + "/" + name + "Color.asset";
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
            {
                texture = new Texture2D(4, 4, TextureFormat.RGB24, true, false);
                AssetDatabase.CreateAsset(texture, texturePath);
            }
            // No alpha channel on purpose: URP's terrain shader locks smoothness to 1 when the diffuse has alpha.
            texture.Reinitialize(4, 4, TextureFormat.RGB24, true);
            Color[] pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            texture.SetPixels(pixels);
            texture.Apply(true, false);
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Bilinear;
            EditorUtility.SetDirty(texture);

            string layerPath = AssetPaths.Terrain + "/" + name + ".terrainlayer";
            TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
            if (layer == null)
            {
                layer = new TerrainLayer();
                AssetDatabase.CreateAsset(layer, layerPath);
            }
            layer.diffuseTexture = texture;
            layer.tileSize = new Vector2(8f, 8f);
            layer.smoothness = 0f;
            layer.metallic = 0f;
            EditorUtility.SetDirty(layer);
            return layer;
        }

        // ---- Water -------------------------------------------------------------------------------------------------

        /// <summary>
        /// The pond and the stream in their silent-valley (stage 0) look: murky pond and a thin murky trickle. The wide, clear
        /// stream is built too but hidden; ResonanceView swaps them when the stream is restored.
        /// </summary>
        static WaterParts BuildWater(Transform parent, MaterialLibrary m)
        {
            Transform water = Pieces.Group("Water", parent, Vector3.zero);

            Vector3 pond = new Vector3(ValleyLayout.Pond.x, ValleyLayout.WaterLevel + 0.01f, ValleyLayout.Pond.y);
            GameObject pondSurface = Pieces.Prim(water, PrimitiveType.Cylinder, "Pond", pond,
                new Vector3(ValleyLayout.PondRadius * 2f, 0.01f, ValleyLayout.PondRadius * 2f), m["WaterDull"]);
            Pieces.NoShadows(pondSurface);

            for (int i = 0; i < 6; i++)
            {
                float angle = (i * 61f + 10f) * Mathf.Deg2Rad;
                float radius = 1.2f + 0.8f * (i % 3);
                Vector3 p = pond + new Vector3(Mathf.Cos(angle) * radius, 0.02f, Mathf.Sin(angle) * radius);
                Pieces.Prim(water, PrimitiveType.Cylinder, "LilyPad" + i, p, new Vector3(0.75f, 0.008f, 0.75f), m["LilyPad"]);
            }

            AssetPaths.EnsureFolder(AssetPaths.Meshes);
            GameObject wide = StreamObject(water, "Stream", BuildStreamMesh("StreamWater", ValleyLayout.StreamWidth), m["Water"]);
            wide.SetActive(false);
            GameObject trickle = StreamObject(water, "StreamTrickle", BuildStreamMesh("StreamTrickle", 0.8f), m["WaterDull"]);

            return new WaterParts { Root = water, Pond = pondSurface.GetComponent<Renderer>(), WideStream = wide, Trickle = trickle };
        }

        static GameObject StreamObject(Transform parent, string name, Mesh mesh, Material material)
        {
            GameObject stream = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            stream.transform.SetParent(parent, false);
            stream.GetComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = stream.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return stream;
        }

        static Mesh BuildStreamMesh(string assetName, float width)
        {
            string path = AssetPaths.Meshes + "/" + assetName + ".asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                mesh = new Mesh { name = assetName };
                AssetDatabase.CreateAsset(mesh, path);
            }
            mesh.Clear();

            List<Vector2> line = ValleyLayout.Stream;
            int count = line.Count;
            Vector3[] vertices = new Vector3[count * 2];
            Vector3[] normals = new Vector3[count * 2];
            Vector2[] uvs = new Vector2[count * 2];
            int[] triangles = new int[(count - 1) * 6];
            float distance = 0f;

            for (int i = 0; i < count; i++)
            {
                Vector2 previous = line[Mathf.Max(i - 1, 0)];
                Vector2 next = line[Mathf.Min(i + 1, count - 1)];
                Vector2 direction = (next - previous).normalized;
                Vector2 side = new Vector2(-direction.y, direction.x) * (width * 0.5f);
                Vector2 left = line[i] + side;
                Vector2 right = line[i] - side;
                if (i > 0) distance += Vector2.Distance(line[i], line[i - 1]);

                vertices[i * 2] = new Vector3(left.x, ValleyLayout.WaterLevel, left.y);
                vertices[i * 2 + 1] = new Vector3(right.x, ValleyLayout.WaterLevel, right.y);
                normals[i * 2] = Vector3.up;
                normals[i * 2 + 1] = Vector3.up;
                uvs[i * 2] = new Vector2(0f, distance * 0.25f);
                uvs[i * 2 + 1] = new Vector2(1f, distance * 0.25f);
            }

            for (int i = 0; i < count - 1; i++)
            {
                int a = i * 2, b = i * 2 + 1, c = i * 2 + 2, d = i * 2 + 3;
                triangles[i * 6] = a;
                triangles[i * 6 + 1] = c;
                triangles[i * 6 + 2] = b;
                triangles[i * 6 + 3] = b;
                triangles[i * 6 + 4] = c;
                triangles[i * 6 + 5] = d;
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        // ---- Scenery -----------------------------------------------------------------------------------------------

        static void Scatter(Transform parent, Terrain terrain, PrefabSet prefabs)
        {
            System.Random random = new System.Random(20261025);

            Transform trees = Pieces.Group("Trees", parent, Vector3.zero);
            GameObject[] treeVariants = { prefabs.TreeRound, prefabs.TreeTall, prefabs.TreeBlossom, prefabs.TreeRound, prefabs.TreeTall };
            List<Vector2> placed = new List<Vector2>();
            for (int attempt = 0; attempt < 6000 && placed.Count < 56; attempt++)
            {
                Vector2 p = RandomInRing(random, 15f, 48f);
                if (!ValleyLayout.IsFreeForScenery(p) || TooClose(placed, p, 5f)) continue;
                placed.Add(p);
                Spawn(treeVariants[placed.Count % treeVariants.Length], trees, Ground(terrain, p), random, 0.8f, 1.35f);
            }

            Transform rocks = Pieces.Group("Rocks", parent, Vector3.zero);
            List<Vector2> rockSpots = new List<Vector2>();
            for (int attempt = 0; attempt < 3000 && rockSpots.Count < 14; attempt++)
            {
                Vector2 p = RandomInRing(random, 14f, 48f);
                if (!ValleyLayout.IsFreeForScenery(p) || TooClose(placed, p, 3.5f) || TooClose(rockSpots, p, 6f)) continue;
                rockSpots.Add(p);
                Spawn(prefabs.Rock, rocks, Ground(terrain, p) + Vector3.down * 0.1f, random, 0.7f, 1.6f);
            }

            Transform flowers = Pieces.Group("Flowers", parent, Vector3.zero);
            List<Vector2> patches = new List<Vector2>();
            for (int attempt = 0; attempt < 4000 && patches.Count < 28; attempt++)
            {
                Vector2 p = RandomInRing(random, 10.5f, 42f);
                bool nearPath = ValleyLayout.DistanceToPaths(p) < 10f;
                if (!nearPath && random.NextDouble() > 0.2) continue;
                if (ValleyLayout.DistanceToPaths(p) < 2.3f || Vector2.Distance(p, ValleyLayout.Spawn) < 5f
                    || Vector2.Distance(p, ValleyLayout.Cottage) < 7.5f || Vector2.Distance(p, ValleyLayout.Villager) < 2.5f
                    || Vector2.Distance(p, ValleyLayout.Spring) < 4.5f || Vector2.Distance(p, ValleyLayout.Pond) < ValleyLayout.PondRadius + 1.5f
                    || ValleyLayout.DistanceToPolyline(p, ValleyLayout.Stream) < 3.2f || Mathf.Abs(p.x) > 46f || Mathf.Abs(p.y) > 46f
                    || ValleyLayout.NearGameplaySpot(p, 3f) || TooClose(placed, p, 2.2f) || TooClose(rockSpots, p, 2.2f) || TooClose(patches, p, 3.5f))
                    continue;
                patches.Add(p);
                Spawn(prefabs.FlowerPatches[patches.Count % prefabs.FlowerPatches.Length], flowers, Ground(terrain, p), random, 1.1f, 1.7f);
            }
            Debug.Log("[Echoes] Scenery: " + placed.Count + " trees, " + rockSpots.Count + " rocks, " + patches.Count + " flower patches");
        }

        static Vector2 RandomInRing(System.Random random, float inner, float outer)
        {
            float radius = Mathf.Lerp(inner, outer, Mathf.Sqrt((float)random.NextDouble()));
            float angle = (float)(random.NextDouble() * Mathf.PI * 2.0);
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        static bool TooClose(List<Vector2> others, Vector2 p, float distance)
        {
            foreach (Vector2 other in others)
                if ((other - p).sqrMagnitude < distance * distance) return true;
            return false;
        }

        static void Spawn(GameObject prefab, Transform parent, Vector3 position, System.Random random, float minScale, float maxScale)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.position = position;
            instance.transform.rotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
            instance.transform.localScale = Vector3.one * Mathf.Lerp(minScale, maxScale, (float)random.NextDouble());
        }

        /// <summary>Invisible walls keep the player inside the playable square, away from the terrain edge.</summary>
        static void BuildBoundaries(Transform parent)
        {
            float half = ValleyLayout.PlayableHalfSize;
            float span = half * 2f + 6f;
            Vector3[] centers =
            {
                new Vector3(0f, 20f, half + 1f), new Vector3(0f, 20f, -half - 1f),
                new Vector3(half + 1f, 20f, 0f), new Vector3(-half - 1f, 20f, 0f)
            };
            Vector3[] sizes = { new Vector3(span, 60f, 2f), new Vector3(span, 60f, 2f), new Vector3(2f, 60f, span), new Vector3(2f, 60f, span) };
            Transform group = Pieces.Group("Boundaries", parent, Vector3.zero);
            for (int i = 0; i < centers.Length; i++)
            {
                GameObject wall = new GameObject("Boundary" + i);
                wall.transform.SetParent(group, false);
                wall.transform.position = centers[i];
                wall.AddComponent<BoxCollider>().size = sizes[i];
            }
        }

        // ---- Gameplay objects --------------------------------------------------------------------------------------

        static void PlaceResourceNodes(Transform parent, Terrain terrain, PrefabSet prefabs)
        {
            Transform group = Pieces.Group("ResourceNodes", parent, Vector3.zero);
            for (int i = 0; i < ValleyLayout.HerbSpots.Length; i++)
                PlaceNode(group, terrain, prefabs.HerbNode, "herb_" + (i + 1), ItemIds.Herb, 40f, ValleyLayout.HerbSpots[i], i * 53f);
            for (int i = 0; i < ValleyLayout.FlowerSpots.Length; i++)
                PlaceNode(group, terrain, prefabs.FlowerNode, "flower_" + (i + 1), ItemIds.Flower, 40f, ValleyLayout.FlowerSpots[i], i * 71f);
            for (int i = 0; i < ValleyLayout.OreSpots.Length; i++)
                PlaceNode(group, terrain, prefabs.OreNode, "ore_" + (i + 1), ItemIds.Ore, 75f, ValleyLayout.OreSpots[i], i * 97f);
        }

        static void PlaceNode(Transform parent, Terrain terrain, GameObject prefab, string id, string itemId, float cooldown, Vector2 spot, float yaw)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = id;
            instance.transform.position = Ground(terrain, spot);
            instance.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            ResourceNode node = instance.AddComponent<ResourceNode>();
            Wire.Set(node, "prompt", "Gather");
            Wire.Set(node, "range", 2.4f);
            Wire.Set(node, "nodeId", id);
            Wire.Set(node, "itemId", itemId);
            Wire.Set(node, "yieldAmount", 1);
            Wire.Set(node, "cooldownSeconds", cooldown);
            Wire.Set(node, "readyVisual", instance.transform.Find("Ready").gameObject);
            Wire.Set(node, "depletedVisual", instance.transform.Find("Depleted").gameObject);
        }

        static ChickenInteractable PlaceHen(Transform parent, Terrain terrain, PrefabSet prefabs)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.Hen, parent);
            instance.name = "Clover";
            instance.transform.position = Ground(terrain, ValleyLayout.HenYard);
            instance.transform.rotation = Quaternion.LookRotation(new Vector3(-22f, 0f, 5f));

            ChickenInteractable chicken = instance.AddComponent<ChickenInteractable>();
            Wire.Set(chicken, "prompt", "Tend Clover");
            Wire.Set(chicken, "range", 3.6f);
            Wire.Set(chicken, "body", instance.transform.Find("Body"));
            Wire.Set(chicken, "hungryIcon", instance.transform.Find("HungryIcon").gameObject);
            Wire.Set(chicken, "producingIcon", instance.transform.Find("ProducingIcon").gameObject);
            Wire.Set(chicken, "eggVisual", instance.transform.Find("Egg").gameObject);
            return chicken;
        }

        /// <summary>Flower patches along both banks and saplings around the spring; hidden until the stream is restored.</summary>
        static GameObject BuildBloom(Transform parent, Terrain terrain, PrefabSet prefabs)
        {
            GameObject root = new GameObject("StreamBloom");
            root.transform.SetParent(parent, false);
            System.Random random = new System.Random(7);

            List<Vector2> line = ValleyLayout.Stream;
            int created = 0;
            for (int i = 2; i < line.Count - 2 && created < 18; i += 2)
            {
                Vector2 direction = (line[i + 1] - line[i - 1]).normalized;
                Vector2 side = new Vector2(-direction.y, direction.x);
                float offset = (created % 2 == 0 ? 1f : -1f) * (2.9f + (float)random.NextDouble() * 1.2f);
                GameObject patch = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.FlowerPatches[created % prefabs.FlowerPatches.Length], root.transform);
                patch.transform.position = Ground(terrain, line[i] + side * offset);
                patch.transform.rotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
                patch.transform.localScale = Vector3.one * (1.5f + (float)random.NextDouble() * 0.5f);
                created++;
            }

            foreach (float angle in new[] { 70f, 120f, 170f, 290f })
            {
                Vector2 spot = ValleyLayout.Spring + new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * 4.8f;
                GameObject sapling = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.TreeBlossom, root.transform);
                sapling.transform.position = Ground(terrain, spot);
                sapling.transform.rotation = Quaternion.Euler(0f, angle * 3f, 0f);
                sapling.transform.localScale = Vector3.one * 0.55f;
            }

            root.SetActive(false);
            return root;
        }

        /// <summary>
        /// What makes the village feel lived in once the windmill turns: lantern posts around the plaza, the garden and the
        /// windmill, extra flower patches and a few blossom saplings. Hidden until stage 3; ResonanceView pops the children up.
        /// </summary>
        static GameObject BuildVillageLife(Transform parent, Terrain terrain, PrefabSet prefabs, MaterialLibrary m)
        {
            GameObject root = new GameObject("VillageLife");
            root.transform.SetParent(parent, false);
            System.Random random = new System.Random(2026);

            Vector2[] lanternSpots =
            {
                Polar(10.8f, 45f), Polar(10.8f, 135f), Polar(10.8f, 225f), Polar(10.8f, 315f),
                ValleyLayout.Garden + new Vector2(-5.2f, 2.2f), ValleyLayout.Garden + new Vector2(-5.2f, -4.4f),
                ValleyLayout.Windmill + new Vector2(3.6f, -4.2f), ValleyLayout.Windmill + new Vector2(-5.8f, 0.8f)
            };
            for (int i = 0; i < lanternSpots.Length; i++)
            {
                Transform post = Pieces.Group("LanternPost" + i, root.transform, Ground(terrain, lanternSpots[i]), i * 70f);
                Pieces.Prim(post, PrimitiveType.Cylinder, "Pole", new Vector3(0f, 0.75f, 0f), new Vector3(0.09f, 0.75f, 0.09f), m["WoodDark"]);
                Pieces.Prim(post, PrimitiveType.Cube, "Arm", new Vector3(0.18f, 1.5f, 0f), new Vector3(0.4f, 0.05f, 0.05f), m["WoodDark"]);
                Pieces.Prim(post, PrimitiveType.Sphere, "Lantern", new Vector3(0.36f, 1.34f, 0f), new Vector3(0.3f, 0.38f, 0.3f), m["LanternGlow"]);
                Pieces.Prim(post, PrimitiveType.Cylinder, "Cap", new Vector3(0.36f, 1.56f, 0f), new Vector3(0.22f, 0.03f, 0.22f), m["Iron"]);
            }

            int patches = 0;
            for (int attempt = 0; attempt < 800 && patches < 18; attempt++)
            {
                float radius = Mathf.Lerp(11.5f, 27f, Mathf.Sqrt((float)random.NextDouble()));
                float angle = (float)(random.NextDouble() * Mathf.PI * 2.0);
                Vector2 p = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                if (ValleyLayout.DistanceToPaths(p) < 2.2f || ValleyLayout.NearGameplaySpot(p, 2.4f) || Vector2.Distance(p, ValleyLayout.Cottage) < 7f
                    || Vector2.Distance(p, ValleyLayout.Villager) < 2.5f || Vector2.Distance(p, ValleyLayout.Spring) < 5f
                    || Vector2.Distance(p, ValleyLayout.Pond) < ValleyLayout.PondRadius + 1.5f || Vector2.Distance(p, ValleyLayout.Spawn) < 5f
                    || ValleyLayout.DistanceToPolyline(p, ValleyLayout.Stream) < 3.4f)
                    continue;

                GameObject patch = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.FlowerPatches[patches % prefabs.FlowerPatches.Length], root.transform);
                patch.transform.position = Ground(terrain, p);
                patch.transform.rotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
                patch.transform.localScale = Vector3.one * (1.4f + (float)random.NextDouble() * 0.5f);
                patches++;
            }

            Vector2[] saplings =
            {
                ValleyLayout.Garden + new Vector2(6.8f, 4.2f), ValleyLayout.Garden + new Vector2(-1f, 6.6f),
                ValleyLayout.Windmill + new Vector2(6.5f, 0.5f), ValleyLayout.Windmill + new Vector2(-6.8f, -3.2f)
            };
            for (int i = 0; i < saplings.Length; i++)
            {
                GameObject sapling = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.TreeBlossom, root.transform);
                sapling.transform.position = Ground(terrain, saplings[i]);
                sapling.transform.rotation = Quaternion.Euler(0f, i * 83f, 0f);
                sapling.transform.localScale = Vector3.one * 0.6f;
            }

            root.SetActive(false);
            return root;
        }

        static Vector2 Polar(float radius, float degrees) =>
            new Vector2(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad)) * radius;

        static ParticleSystem BuildSparkles(Transform parent)
        {
            GameObject go = new GameObject("Sparkles");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            ParticleSystem system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.playOnAwake = false;
            main.duration = 5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.3f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.93f, 0.62f, 1f), new Color(0.62f, 1f, 0.96f, 1f));
            main.gravityModifier = -0.02f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 300;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 14f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 70) });

            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 18f;
            shape.radius = 3.6f;

            ParticleSystem.ColorOverLifetimeModule fade = system.colorOverLifetime;
            fade.enabled = true;
            fade.color = new ParticleSystem.MinMaxGradient(SceneKit.FadeInOut(Color.white, 1f));

            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = SceneKit.ParticleMaterial();
            return system;
        }

        // ---- HUD ---------------------------------------------------------------------------------------------------

        static void BuildHud(out HudController hud, out DialogueBox dialogue, out PauseController pause, out GameplayUi gameplayUi)
        {
            Canvas canvas = UiKit.CreateCanvas("HUD", 10);
            Transform root = canvas.transform;
            hud = canvas.gameObject.AddComponent<HudController>();
            dialogue = canvas.gameObject.AddComponent<DialogueBox>();
            pause = canvas.gameObject.AddComponent<PauseController>();

            // Objective (top-left)
            Image objectivePanel = UiKit.Panel(root, "ObjectivePanel", UiKit.PanelColor);
            objectivePanel.rectTransform.Place(UiKit.TopLeft, UiKit.TopLeft, new Vector2(32f, -32f), new Vector2(660f, 132f));
            Text heading = UiKit.Label(objectivePanel.transform, "Heading", ObjectiveTracker.Title, 30, UiKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            heading.rectTransform.Place(UiKit.TopLeft, UiKit.TopLeft, new Vector2(24f, -14f), new Vector2(612f, 40f));
            Text body = UiKit.Label(objectivePanel.transform, "Body", ObjectiveTracker.Describe(ObjectiveStep.MeetVillager), 26, UiKit.Cream, TextAnchor.UpperLeft);
            body.rectTransform.Place(UiKit.TopLeft, UiKit.TopLeft, new Vector2(24f, -60f), new Vector2(612f, 64f));

            // Controls hint (bottom-left)
            Text controls = UiKit.Label(root, "ControlsHint", "WASD  Move      Mouse  Look      E  Interact      Esc  Pause", 24,
                new Color(1f, 0.95f, 0.84f, 0.9f), TextAnchor.LowerLeft);
            controls.rectTransform.Place(UiKit.BottomLeft, UiKit.BottomLeft, new Vector2(32f, 24f), new Vector2(980f, 40f));

            // Interaction prompt (bottom-centre)
            Image promptPanel = UiKit.Panel(root, "InteractionPrompt", UiKit.PanelColor);
            promptPanel.rectTransform.Place(UiKit.BottomCenter, UiKit.BottomCenter, new Vector2(0f, 300f), new Vector2(640f, 70f));
            Text promptText = UiKit.Label(promptPanel.transform, "Text", "[E]  Interact", 34, UiKit.Cream, TextAnchor.MiddleCenter, FontStyle.Bold);
            promptText.rectTransform.Fill(8f);

            // Banner (top-centre), below the objective panel and the status panel so they never overlap
            Image bannerPanel = UiKit.Panel(root, "Banner", new Color(0.08f, 0.11f, 0.18f, 0.6f));
            bannerPanel.rectTransform.Place(UiKit.TopCenter, UiKit.TopCenter, new Vector2(0f, BannerTop), new Vector2(1300f, 168f));
            CanvasGroup bannerGroup = bannerPanel.gameObject.AddComponent<CanvasGroup>();
            bannerGroup.interactable = false;
            bannerGroup.blocksRaycasts = false;
            Text bannerTitle = UiKit.Label(bannerPanel.transform, "Title", "Objective Complete", 56, UiKit.Gold, TextAnchor.UpperCenter, FontStyle.Bold);
            bannerTitle.rectTransform.Place(UiKit.TopCenter, UiKit.TopCenter, new Vector2(0f, -14f), new Vector2(1240f, 72f));
            Text bannerSubtitle = UiKit.Label(bannerPanel.transform, "Subtitle", string.Empty, 30, UiKit.Cream, TextAnchor.UpperCenter);
            bannerSubtitle.rectTransform.Place(UiKit.TopCenter, UiKit.TopCenter, new Vector2(0f, -88f), new Vector2(1180f, 72f));

            // Dialogue (bottom-centre)
            Image dialoguePanel = UiKit.Panel(root, "Dialogue", new Color(0.08f, 0.11f, 0.18f, 0.86f));
            dialoguePanel.rectTransform.Place(UiKit.BottomCenter, UiKit.BottomCenter, new Vector2(0f, 76f), new Vector2(1300f, 224f));
            Text speaker = UiKit.Label(dialoguePanel.transform, "Speaker", string.Empty, 36, UiKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            speaker.rectTransform.Place(UiKit.TopLeft, UiKit.TopLeft, new Vector2(36f, -20f), new Vector2(900f, 46f));
            Text line = UiKit.Label(dialoguePanel.transform, "Line", string.Empty, 32, UiKit.Cream, TextAnchor.UpperLeft);
            line.rectTransform.Place(UiKit.TopLeft, UiKit.TopLeft, new Vector2(36f, -72f), new Vector2(1228f, 112f));
            Text hint = UiKit.Label(dialoguePanel.transform, "Hint", "E  Continue", 24, UiKit.Gold, TextAnchor.LowerRight);
            hint.rectTransform.Place(UiKit.BottomRight, UiKit.BottomRight, new Vector2(-36f, 14f), new Vector2(300f, 34f));

            // Gameplay UI (coins, hen status, toasts, satchel, alchemy panel) sits under the pause menu in draw order
            gameplayUi = GameplayUiBuilder.Build(canvas, hud);

            // Pause menu (full screen dimmer + centre panel)
            Image dim = UiKit.Panel(root, "PauseMenu", new Color(0.02f, 0.03f, 0.06f, 0.62f), false);
            dim.rectTransform.Fill();
            dim.raycastTarget = true;
            Image box = UiKit.Panel(dim.transform, "Panel", new Color(0.08f, 0.11f, 0.18f, 0.94f));
            box.rectTransform.Place(UiKit.Center, UiKit.Center, Vector2.zero, new Vector2(560f, 560f));
            Text pauseTitle = UiKit.Label(box.transform, "Title", "Paused", 72, UiKit.Gold, TextAnchor.UpperCenter, FontStyle.Bold);
            pauseTitle.rectTransform.Place(UiKit.TopCenter, UiKit.TopCenter, new Vector2(0f, -40f), new Vector2(500f, 90f));
            Button resume = UiKit.MakeButton(box.transform, "ResumeButton", "Resume", UiKit.Center, new Vector2(0f, 90f), new Vector2(400f, 76f));
            Button save = UiKit.MakeButton(box.transform, "SaveButton", "Save Game", UiKit.Center, new Vector2(0f, 0f), new Vector2(400f, 76f));
            Button mainMenu = UiKit.MakeButton(box.transform, "MainMenuButton", "Main Menu", UiKit.Center, new Vector2(0f, -90f), new Vector2(400f, 76f));
            Button quit = UiKit.MakeButton(box.transform, "QuitButton", "Quit", UiKit.Center, new Vector2(0f, -180f), new Vector2(400f, 76f));
            Text pauseHint = UiKit.Label(box.transform, "Hint", "Esc  Resume", 24, new Color(1f, 0.95f, 0.84f, 0.8f), TextAnchor.LowerCenter);
            pauseHint.rectTransform.Place(UiKit.BottomCenter, UiKit.BottomCenter, new Vector2(0f, 22f), new Vector2(400f, 34f));

            Wire.Set(hud, "objectiveHeading", heading);
            Wire.Set(hud, "objectiveBody", body);
            Wire.Set(hud, "promptRoot", promptPanel.gameObject);
            Wire.Set(hud, "promptText", promptText);
            Wire.Set(hud, "bannerGroup", bannerGroup);
            Wire.Set(hud, "bannerTitle", bannerTitle);
            Wire.Set(hud, "bannerSubtitle", bannerSubtitle);

            Wire.Set(dialogue, "panel", dialoguePanel.gameObject);
            Wire.Set(dialogue, "speakerText", speaker);
            Wire.Set(dialogue, "bodyText", line);
            Wire.Set(dialogue, "hintText", hint);

            Wire.Set(pause, "panel", dim.gameObject);
            Wire.Set(pause, "resumeButton", resume);
            Wire.Set(pause, "saveButton", save);
            Wire.Set(pause, "mainMenuButton", mainMenu);
            Wire.Set(pause, "quitButton", quit);

            dim.gameObject.SetActive(false);
            dialoguePanel.gameObject.SetActive(false);
            promptPanel.gameObject.SetActive(false);
        }
    }
}
