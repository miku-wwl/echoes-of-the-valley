using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Echoes.EditorTools
{
    /// <summary>
    /// Only the selected garden / workshop / north-path corridor. Does not change heights, navigation, collision or the restoration groups.
    /// Three original tree silhouettes made of many small painted leaf clusters (never a card larger than ~1.2 m, canopy kept above
    /// 2 m), low ground cover, and a terrain copy that keeps the original dirt paths and re-skins them.
    /// </summary>
    internal static class Phase52EnvironmentBuilder
    {
        const string Folder = Phase52HeroBuilder.Folder;
        internal static readonly Vector2[][] Routes = { new[] { new Vector2(11, -13), new Vector2(9.5f, -9.5f), new Vector2(7, -6), new Vector2(9, 1), new Vector2(15, 4), new Vector2(21, 8) }, new[] { new Vector2(9, 1), new Vector2(8, 8), new Vector2(0, 9), new Vector2(1.5f, 19), new Vector2(4.5f, 28.5f) } };
        internal static float Distance(Vector2 p) => Routes.Min(line => ValleyLayout.DistanceToPolyline(p, line));
        internal static bool InPilot(Vector3 p) => Distance(new Vector2(p.x, p.z)) < 8 || Vector2.Distance(new Vector2(p.x, p.z), ValleyLayout.Windmill) < 12;
        /// <summary>Distance to the corridor the pilot repaints: the new routes, the original dirt paths and the windmill's trodden circle.</summary>
        internal static float CorridorDistance(Vector2 p) => Mathf.Min(Distance(p), ValleyLayout.DistanceToPaths(p), Mathf.Max(0, Vector2.Distance(p, ValleyLayout.Windmill) - 6f));

        // Cells of the 2x2 leaf / plant atlases (u0, v0); each cell spans 0.5.
        static readonly Vector2[] LeafCells = { new Vector2(0, .5f), new Vector2(.5f, .5f), new Vector2(0, 0), new Vector2(.5f, 0) };
        public const float MinCanopyHeight = 2.15f;
        public const float MaxCardSize = 1.25f;

        public static void Build(Phase52Pilot pilot)
        {
            Transform root = new GameObject("Phase52_RouteOnly").transform; pilot.Route = root.gameObject;
            Terrain terrain = pilot.Terrain;
            Material bark = AssetDatabase.LoadAssetAtPath<Material>("Assets/Echoes/Art/Pilot/Art_Bark.mat");
            Material leaves = FoliageMaterial("PilotLeaves", "LeafClusters", 0.8f, 3.6f, true);
            Material plantMaterial = FoliageMaterial("PilotPlants", "GroundPlants", 0.35f, 1.3f, false);
            Mesh[] trees = { TreeMesh(0), TreeMesh(1), TreeMesh(2) };
            Mesh[] tufts = Enumerable.Range(0, 4).Select(PlantMesh).ToArray();
            var old = new List<Renderer>(); int index = 0;
            // Preserve original trunk capsules; replace visible models at the same roots.
            foreach (var tree in UnityEngine.Object.FindObjectsByType<CapsuleCollider>(FindObjectsSortMode.None).OrderBy(c => c.transform.position.z).ToArray())
            {
                if (!tree.name.StartsWith("Tree_") || !InPilot(tree.transform.position)) continue;
                old.AddRange(tree.GetComponentsInChildren<Renderer>());
                AddTree(tree.transform.position, tree.transform.eulerAngles.y, index++, false);
            }
            foreach (var tree in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).ToArray())
            {
                if (!tree.name.StartsWith("Tree") || tree.GetComponent<Renderer>() != null || tree.GetComponent<Collider>() != null || !InPilot(tree.position)) continue;
                if (tree.GetComponentInParent<Collider>() != null) continue;
                var renderers = tree.GetComponentsInChildren<MeshRenderer>(); if (renderers.Length == 0) continue;
                old.AddRange(renderers); AddTree(tree.position, tree.eulerAngles.y, index++, false);
            }
            pilot.ReplacedTreeRenderers = old.Distinct().ToArray();
            foreach (var renderer in pilot.ReplacedTreeRenderers) renderer.enabled = false;
            // A few framing trees well away from the dirt, each with its own trunk collider so nothing walks or orbits through a trunk.
            foreach (Vector2 p in new[] { new Vector2(-6.2f, 17.5f), new Vector2(9.5f, 21.5f), new Vector2(14.5f, 11f), new Vector2(-3.5f, 3.5f), new Vector2(14.5f, -5.5f), new Vector2(-1.5f, 27f) })
                if (Distance(p) > 4.5f && !ValleyLayout.NearGameplaySpot(p, 1.7f)) AddTree(Ground(p), index * 47, index++, true);

            // Ground cover on the verges (never on the dirt), thinner near gameplay spots.
            var rng = new System.Random(5202); int plantCount = 0;
            foreach (Vector2[] route in Routes)
                for (int segment = 0; segment < route.Length - 1; segment++)
                {
                    Vector2 a = route[segment], b = route[segment + 1], normal = new Vector2(-(b - a).y, (b - a).x).normalized;
                    int count = Mathf.CeilToInt(Vector2.Distance(a, b) * 2.1f);
                    for (int i = 0; i < count; i++)
                    {
                        float t = (i + (float)rng.NextDouble() * .8f) / count; float sign = rng.Next(2) == 0 ? -1 : 1;
                        float lateral = 2.15f + (float)rng.NextDouble() * (rng.NextDouble() < .35 ? 1.0f : 4.2f);
                        Vector2 p = Vector2.Lerp(a, b, t) + normal * sign * lateral;
                        if (ValleyLayout.NearGameplaySpot(p, 1.5f) || Vector2.Distance(p, ValleyLayout.Cottage) < 4.4f || Vector2.Distance(p, ValleyLayout.Garden) < 4.4f || CorridorDistance(p) < 2.1f) continue;
                        if (ValleyLayout.DistanceToPaths(p) < 2.1f) continue;
                        int kind = lateral < 3.0f ? (rng.Next(3) == 0 ? 2 : 0) : rng.Next(4);
                        var go = new GameObject("Plant" + kind, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(root, false);
                        go.transform.position = Ground(p); go.transform.localScale = Vector3.one * (.75f + (float)rng.NextDouble() * .7f); go.transform.rotation = Quaternion.Euler(0, (float)rng.NextDouble() * 360, 0);
                        go.GetComponent<MeshFilter>().sharedMesh = tufts[kind]; var mr = go.GetComponent<MeshRenderer>(); mr.sharedMaterial = plantMaterial; mr.shadowCastingMode = ShadowCastingMode.Off; plantCount++;
                    }
                }
            pilot.PilotTerrain = PaintTerrain(pilot.BaselineTerrain, terrain.transform.position);
            terrain.terrainData = pilot.PilotTerrain; terrain.GetComponent<TerrainCollider>().terrainData = pilot.PilotTerrain;
            Debug.Log("[Echoes][P52B2] bounded route: " + index + " tree models, " + plantCount + " ground-cover clusters, " + pilot.ReplacedTreeRenderers.Length + " old renderers swapped; original colliders retained.");

            Vector3 Ground(Vector2 p) { Vector3 v = new Vector3(p.x, 0, p.y); v.y = terrain.SampleHeight(v) + terrain.transform.position.y; return v; }
            void AddTree(Vector3 position, float yaw, int n, bool addCollider)
            {
                var go = new GameObject(new[] { "Oak", "Aspen", "Willow" }[n % 3] + "_Pilot", typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(root, false);
                go.transform.position = position; go.transform.rotation = Quaternion.Euler(0, yaw + 17 * n, 0); go.transform.localScale = Vector3.one * (.88f + (n % 4) * .065f);
                go.GetComponent<MeshFilter>().sharedMesh = trees[n % 3]; var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterials = new[] { bark, leaves };
                if (addCollider) { var c = go.AddComponent<CapsuleCollider>(); c.radius = .17f; c.height = 2.4f; c.center = new Vector3(0, 1.2f, 0); }
                var lod = go.AddComponent<LODGroup>(); lod.SetLODs(new[] { new LOD(.02f, new Renderer[] { renderer }) }); lod.RecalculateBounds();
            }
        }

        // ------------------------------------------------------------------------------------------------ materials

        static Material FoliageMaterial(string name, string texture, float nearStart, float nearEnd, bool readable)
        {
            string texPath = Folder + "/Textures/" + texture + ".png";
            Phase52HeroBuilder.ConfigureTexture(texPath, true);
            var importer = (TextureImporter)AssetImporter.GetAtPath(texPath);
            if (importer.isReadable != readable) { importer.isReadable = readable; if (readable) importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport(); }
            string path = Folder + "/" + name + ".mat"; var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Echoes/Foliage"); if (shader == null) throw new FileNotFoundException("Missing shader Echoes/Foliage");
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
            material.shader = shader;
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath)); material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Cutoff", .45f); material.SetFloat("_NearFadeStart", nearStart); material.SetFloat("_NearFadeEnd", nearEnd);
            material.SetFloat("_Cull", 0); material.EnableKeyword("_ALPHATEST_ON"); material.renderQueue = 2450; material.SetOverrideTag("RenderType", "TransparentCutout");
            material.enableInstancing = true; EditorUtility.SetDirty(material); return material;
        }

        // ------------------------------------------------------------------------------------------------ trees

        sealed class Geo
        {
            public readonly List<Vector3> V = new List<Vector3>(), N = new List<Vector3>();
            public readonly List<Vector2> Uv = new List<Vector2>(), Sway = new List<Vector2>();
            public readonly List<Color> C = new List<Color>();
            public readonly List<int> Bark = new List<int>(), Leaf = new List<int>();
        }

        static Mesh TreeMesh(int kind)
        {
            var rng = new System.Random(8200 + kind); var g = new Geo();
            Func<double, double, float> R = (a, b) => (float)(a + rng.NextDouble() * (b - a));
            float height = kind == 1 ? 7.4f : kind == 2 ? 5.3f : 5.9f, canopyMin = kind == 2 ? MinCanopyHeight : 2.45f;
            Vector3 crown = new Vector3(0, kind == 1 ? 5.2f : 4.2f, 0);
            // trunk with a root flare
            Vector3[] trunk = kind == 1 ? new[] { new Vector3(0, -.05f, 0), new Vector3(.05f, 1.6f, .03f), new Vector3(-.04f, 3.4f, .06f), new Vector3(.05f, 5.4f, -.02f), new Vector3(0, 7.0f, .02f) }
                : new[] { new Vector3(0, -.05f, 0), new Vector3(.08f, 1.2f, .04f), new Vector3(-.05f, 2.4f, .08f), new Vector3(.05f, kind == 2 ? 2.9f : 3.4f, .02f) };
            float[] trunkR = kind == 1 ? new[] { .30f, .17f, .13f, .09f, .04f } : kind == 2 ? new[] { .46f, .26f, .19f, .14f } : new[] { .44f, .24f, .17f, .11f };
            Tube(g, trunk, trunkR, 9, 0.55f, 0.6f);
            var puffs = new List<(Vector3 c, float r, int cell, int cards, float s0, float s1, bool hang)>();
            if (kind == 1)
            {
                for (int i = 0; i < 9; i++) { float y = 3.1f + i * .5f; float a = i * 2.4f; float off = .30f + .30f * Mathf.Sin(i * 1.3f); puffs.Add((new Vector3(Mathf.Cos(a) * off, y, Mathf.Sin(a) * off), .78f - i * .012f, 1, 11, .52f, .82f, false)); }
                puffs.Add((new Vector3(0, 7.15f, 0), .7f, 1, 9, .5f, .72f, false));
            }
            else
            {
                int branches = kind == 2 ? 5 : 6;
                for (int i = 0; i < branches; i++)
                {
                    float a = i * 2.4f + R(-.3, .3), reach = kind == 2 ? R(1.7, 2.2) : R(1.5, 2.1), rise = kind == 2 ? R(.35, .8) : R(.8, 1.5);
                    float startY = kind == 2 ? R(2.3, 2.8) : R(2.5, 3.1);
                    Vector3 s = new Vector3(0, startY, 0), e = s + new Vector3(Mathf.Cos(a) * reach, rise, Mathf.Sin(a) * reach);
                    Tube(g, new[] { s, Vector3.Lerp(s, e, .5f) + Vector3.up * .22f, e }, new[] { .10f, .06f, .025f }, 6, 0.6f, 0.75f);
                    puffs.Add((e + Vector3.up * (kind == 2 ? .15f : .6f), kind == 2 ? 1.05f : 1.25f, kind == 2 ? 2 : (i % 3 == 2 ? 3 : 0), kind == 2 ? 11 : 14, kind == 2 ? .55f : .8f, kind == 2 ? 1.1f : 1.15f, kind == 2));
                }
                if (kind == 0) { puffs.Add((crown, 1.5f, 0, 16, .85f, 1.2f, false)); puffs.Add((crown + new Vector3(.9f, -.4f, .5f), 1.1f, 3, 12, .8f, 1.1f, false)); puffs.Add((crown + new Vector3(-.8f, -.5f, -.6f), 1.1f, 0, 12, .8f, 1.1f, false)); }
                else puffs.Add((new Vector3(0, 3.7f, 0), 1.2f, 2, 12, .55f, 1.1f, true));
            }
            float maxR = puffs.Max(p => new Vector2(p.c.x, p.c.z).magnitude + p.r);
            foreach (var puff in puffs)
                for (int i = 0; i < puff.cards; i++)
                {
                    Vector3 dir = new Vector3(R(-1, 1), R(-.55, 1), R(-1, 1)).normalized;
                    float radius = puff.r * Mathf.Pow(R(.25, 1), .7f);
                    Vector3 center = puff.c + dir * radius; float size = R(puff.s0, puff.s1);
                    float w = size * (puff.hang ? .5f : 1f), h = size * (puff.hang ? 1.1f : 1f);
                    center.y = Mathf.Max(center.y, canopyMin + (puff.hang ? h * .85f : size * .72f));
                    Vector3 outward = (center - crown).normalized; Vector3 normal = (outward * .7f + Vector3.up * .3f).normalized;
                    Vector3 cardNormal = (dir + Vector3.up * .25f + new Vector3(R(-.4, .4), 0, R(-.4, .4))).normalized;
                    Vector3 right = Vector3.Cross(Vector3.up, cardNormal); if (right.sqrMagnitude < .05f) right = Vector3.right; right.Normalize();
                    Vector3 up = Vector3.Cross(cardNormal, right).normalized; if (puff.hang) up = Vector3.up;
                    if (!puff.hang) { float roll = R(-.6, .6); right = Quaternion.AngleAxis(roll * Mathf.Rad2Deg, cardNormal) * right; up = Quaternion.AngleAxis(roll * Mathf.Rad2Deg, cardNormal) * up; }
                    float heightT = Mathf.Clamp01((center.y - canopyMin) / Mathf.Max(height - canopyMin, 1f));
                    float outer = Mathf.Clamp01(new Vector2(center.x, center.z).magnitude / maxR);
                    float shade = Mathf.Lerp(.70f, 1.10f, heightT) * R(.94, 1.06f);
                    var color = new Color(shade * R(.97, 1.03), shade, shade * R(.88, .98), Mathf.Lerp(.55f, 1f, Mathf.Max(outer, heightT)));
                    Card(g, center + (puff.hang ? Vector3.down * h * .35f : Vector3.zero), right * w * .5f, up * h * .5f, normal, LeafCells[puff.cell], .3f + .7f * heightT, color);
                }
            return Save("Tree_" + kind, g, false);
        }

        static void Tube(Geo g, Vector3[] points, float[] radii, int sides, float colorLow, float colorHigh)
        {
            int offset = g.V.Count;
            // sample a smooth path (Catmull-Rom) so trunks and branches curve
            int samples = Mathf.Max(6, points.Length * 3);
            for (int j = 0; j < samples; j++)
            {
                float t = j / (float)(samples - 1) * (points.Length - 1); int i = Mathf.Min((int)t, points.Length - 2); float f = t - i;
                Vector3 p0 = points[Mathf.Max(i - 1, 0)], p1 = points[i], p2 = points[i + 1], p3 = points[Mathf.Min(i + 2, points.Length - 1)];
                Vector3 pos = .5f * (2 * p1 + (p2 - p0) * f + (2 * p0 - 5 * p1 + 4 * p2 - p3) * f * f + (-p0 + 3 * p1 - 3 * p2 + p3) * f * f * f);
                Vector3 next = j < samples - 1 ? CatmullAt(points, (j + 1) / (float)(samples - 1) * (points.Length - 1)) : pos + (pos - CatmullAt(points, (j - 1) / (float)(samples - 1) * (points.Length - 1)));
                Vector3 tangent = (next - pos).normalized; if (tangent.sqrMagnitude < .01f) tangent = Vector3.up;
                Vector3 x = Vector3.Cross(tangent, Vector3.forward); if (x.sqrMagnitude < .05f) x = Vector3.right; x.Normalize(); Vector3 z = Vector3.Cross(tangent, x).normalized;
                float radius = Mathf.Lerp(radii[Mathf.Min(i, radii.Length - 1)], radii[Mathf.Min(i + 1, radii.Length - 1)], f);
                float ao = Mathf.Lerp(colorLow, colorHigh, Mathf.Clamp01(pos.y / 3f));
                for (int k = 0; k < sides; k++)
                {
                    float a = k * Mathf.PI * 2 / sides; Vector3 n = x * Mathf.Cos(a) + z * Mathf.Sin(a);
                    g.V.Add(pos + n * radius); g.N.Add(n); g.Uv.Add(new Vector2(k / (float)sides, pos.y * .5f)); g.Sway.Add(Vector2.zero); g.C.Add(new Color(ao, ao, ao, Mathf.Lerp(.65f, 1f, Mathf.Clamp01(pos.y / 2.5f))));
                }
            }
            for (int j = 0; j < samples - 1; j++)
                for (int k = 0; k < sides; k++)
                {
                    int a = offset + j * sides + k, b = offset + j * sides + (k + 1) % sides;
                    g.Bark.AddRange(new[] { a, a + sides, b, b, a + sides, b + sides });
                }
        }

        static Vector3 CatmullAt(Vector3[] points, float t)
        {
            int i = Mathf.Clamp((int)t, 0, points.Length - 2); float f = t - i;
            Vector3 p0 = points[Mathf.Max(i - 1, 0)], p1 = points[i], p2 = points[i + 1], p3 = points[Mathf.Min(i + 2, points.Length - 1)];
            return .5f * (2 * p1 + (p2 - p0) * f + (2 * p0 - 5 * p1 + 4 * p2 - p3) * f * f + (-p0 + 3 * p1 - 3 * p2 + p3) * f * f * f);
        }

        static void Card(Geo g, Vector3 center, Vector3 right, Vector3 up, Vector3 normal, Vector2 cell, float sway, Color color)
        {
            int o = g.V.Count; const float m = .012f;
            g.V.AddRange(new[] { center - right - up, center + right - up, center + right + up, center - right + up });
            for (int i = 0; i < 4; i++) { g.N.Add(normal); g.Sway.Add(new Vector2(sway, 0)); g.C.Add(color); }
            g.Uv.AddRange(new[] { cell + new Vector2(m, m), cell + new Vector2(.5f - m, m), cell + new Vector2(.5f - m, .5f - m), cell + new Vector2(m, .5f - m) });
            g.Leaf.AddRange(new[] { o, o + 1, o + 2, o, o + 2, o + 3 });
        }

        static Mesh PlantMesh(int kind)
        {
            var g = new Geo(); float[] widths = { .95f, 1.15f, .85f, .85f }, heights = { .78f, 1.0f, .5f, 1.25f };
            for (int i = 0; i < 3; i++)
            {
                float a = i * Mathf.PI / 3f + .3f; Vector3 right = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * widths[kind] * .5f; Vector3 up = Vector3.up * heights[kind];
                int o = g.V.Count; Vector3 b = Vector3.down * .03f;
                g.V.AddRange(new[] { b - right, b + right, b + right + up, b - right + up });
                for (int k = 0; k < 4; k++) { g.N.Add(Vector3.up); g.Sway.Add(new Vector2(k < 2 ? 0 : 1, 0)); g.C.Add(k < 2 ? new Color(.72f, .82f, .72f, .6f) : new Color(1.05f, 1.05f, .95f, 1f)); }
                Vector2 cell = LeafCells[kind]; const float m = .012f;
                g.Uv.AddRange(new[] { cell + new Vector2(m, m), cell + new Vector2(.5f - m, m), cell + new Vector2(.5f - m, .5f - m), cell + new Vector2(m, .5f - m) });
                g.Leaf.AddRange(new[] { o, o + 1, o + 2, o, o + 2, o + 3 });
            }
            return Save("Plant_" + kind, g, true);
        }

        static Mesh Save(string name, Geo g, bool leafOnly = false)
        {
            string path = Folder + "/" + name + ".asset"; var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null) { mesh = new Mesh(); AssetDatabase.CreateAsset(mesh, path); }
            mesh.Clear(); mesh.name = name; mesh.indexFormat = IndexFormat.UInt16;
            mesh.SetVertices(g.V); mesh.SetNormals(g.N); mesh.SetUVs(0, g.Uv); mesh.SetUVs(1, g.Sway); mesh.SetColors(g.C);
            if (leafOnly) { mesh.subMeshCount = 1; mesh.SetTriangles(g.Leaf, 0); }
            else { mesh.subMeshCount = 2; mesh.SetTriangles(g.Bark, 0); mesh.SetTriangles(g.Leaf, 1); }
            mesh.RecalculateBounds(); EditorUtility.SetDirty(mesh); return mesh;
        }

        // ------------------------------------------------------------------------------------------------ terrain

        /// <summary>
        /// Fresh copy of the original terrain data (AssetDatabase.CopyAsset keeps the splat textures; the earlier CopySerialized overwrite
        /// silently left the copy 100 % Grass, which erased the plaza, every path and the windmill trail). Heights are untouched; the original
        /// grass layers and the original path/trail layers are re-skinned to the new painted layers; stone stays stone.
        /// </summary>
        static TerrainData PaintTerrain(TerrainData original, Vector3 origin)
        {
            string path = Folder + "/PilotTerrain.asset"; string source = AssetDatabase.GetAssetPath(original);
            AssetDatabase.DeleteAsset(path);
            if (!AssetDatabase.CopyAsset(source, path)) throw new IOException("Could not copy terrain data " + source);
            TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
            int oldW = data.alphamapWidth, oldH = data.alphamapHeight;
            float[,,] old = data.GetAlphamaps(0, 0, oldW, oldH);
            var layers = data.terrainLayers.ToList();
            int iGrass = layers.FindIndex(l => l.name == "Grass"), iMeadow = layers.FindIndex(l => l.name == "Meadow"), iPath = layers.FindIndex(l => l.name == "Path"), iStone = layers.FindIndex(l => l.name == "Stone");
            int iTrail = layers.FindIndex(l => l.name == "WindmillTrail"), iPilotMeadow = layers.FindIndex(l => l.name == "PilotMeadow");
            if (iGrass < 0 || iMeadow < 0 || iPath < 0 || iStone < 0) throw new InvalidDataException("Unexpected baseline terrain layers.");
            int newMeadow = layers.Count; layers.Add(Layer("RouteMeadow", "RouteGrass", 4.6f));
            int newDirt = layers.Count; layers.Add(Layer("RouteDirt", "RouteDirt", 2.8f));
            data.terrainLayers = layers.ToArray(); data.alphamapResolution = 512;
            int W = data.alphamapWidth, H = data.alphamapHeight; var weights = new float[H, W, layers.Count];
            float Old(int layer, float u, float v)
            {
                if (layer < 0) return 0; float x = u * oldW - .5f, z = v * oldH - .5f; int x0 = Mathf.Clamp(Mathf.FloorToInt(x), 0, oldW - 1), z0 = Mathf.Clamp(Mathf.FloorToInt(z), 0, oldH - 1), x1 = Mathf.Min(x0 + 1, oldW - 1), z1 = Mathf.Min(z0 + 1, oldH - 1);
                float fx = Mathf.Clamp01(x - x0), fz = Mathf.Clamp01(z - z0);
                return Mathf.Lerp(Mathf.Lerp(old[z0, x0, layer], old[z0, x1, layer], fx), Mathf.Lerp(old[z1, x0, layer], old[z1, x1, layer], fx), fz);
            }
            for (int z = 0; z < H; z++)
                for (int x = 0; x < W; x++)
                {
                    float u = (x + .5f) / W, v = (z + .5f) / H; float wx = origin.x + u * data.size.x, wz = origin.z + v * data.size.z; var p = new Vector2(wx, wz);
                    float corridor = CorridorDistance(p); float mask = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(6f, 11f, corridor));
                    var o = new float[oldCount(layers)]; for (int i = 0; i < o.Length; i++) o[i] = Old(i, u, v);
                    if (mask <= 0.0001f) { for (int i = 0; i < o.Length; i++) weights[z, x, i] = o[i]; FillMissing(weights, z, x, layers.Count, iGrass); continue; }
                    float oldGrass = o[iGrass] + o[iMeadow] + (iPilotMeadow >= 0 ? o[iPilotMeadow] : 0), oldDirt = o[iPath] + (iTrail >= 0 ? o[iTrail] : 0), oldStone = o[iStone];
                    float noise = Mathf.PerlinNoise(wx * .9f + 30, wz * .9f + 17), noise2 = Mathf.PerlinNoise(wx * .35f + 8, wz * .35f + 51);
                    float route = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(1.15f + noise * .3f, 2.4f + noise * .35f, Distance(p)));
                    float dirt = Mathf.Max(oldDirt, route * (1 - oldStone));
                    dirt = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.30f + noise * .22f, .72f + noise * .22f + noise2 * .06f, dirt)); dirt = Mathf.Min(dirt, 1 - oldStone);
                    float grass = Mathf.Max(0, 1 - dirt - oldStone);
                    for (int i = 0; i < o.Length; i++) weights[z, x, i] = o[i] * (1 - mask);
                    weights[z, x, iStone] += mask * oldStone; weights[z, x, newMeadow] = mask * grass; weights[z, x, newDirt] = mask * dirt;
                    FillMissing(weights, z, x, layers.Count, iGrass);
                }
            data.SetAlphamaps(0, 0, weights); EditorUtility.SetDirty(data); AssetDatabase.SaveAssets();
            return data;
        }

        static int oldCount(List<TerrainLayer> layers) => layers.Count - 2;

        /// <summary>
        /// The original terrain has 156 zero-weight texels along its far southern edge (outside the playable area). The pilot copy gives every
        /// texel a full set of weights by letting Grass take whatever is missing, so no texel can render as an undefined splat.
        /// </summary>
        static void FillMissing(float[,,] weights, int z, int x, int layerCount, int grassIndex)
        {
            float sum = 0; for (int i = 0; i < layerCount; i++) sum += weights[z, x, i];
            if (sum < .99f) weights[z, x, grassIndex] += 1f - sum;
        }

        static TerrainLayer Layer(string name, string texture, float tile)
        {
            string texPath = Folder + "/Textures/" + texture + ".png";
            if (!File.Exists(texPath)) throw new FileNotFoundException("Missing terrain texture " + texPath + "; run tools/art/phase52b2/foliage_textures.py");
            AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(texPath);
            importer.sRGBTexture = true; importer.mipmapEnabled = true; importer.anisoLevel = 8; importer.wrapMode = TextureWrapMode.Repeat; importer.filterMode = FilterMode.Trilinear; importer.alphaSource = TextureImporterAlphaSource.None; importer.textureCompression = TextureImporterCompression.CompressedHQ; importer.SaveAndReimport();
            string p = Folder + "/" + name + ".terrainlayer"; var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(p);
            if (layer == null) { layer = new TerrainLayer(); AssetDatabase.CreateAsset(layer, p); }
            layer.diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath); layer.tileSize = Vector2.one * tile; layer.smoothness = 0; layer.metallic = 0; EditorUtility.SetDirty(layer);
            return layer;
        }
    }
}
