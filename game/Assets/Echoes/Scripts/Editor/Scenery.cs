using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Echoes.EditorTools
{
    /// <summary>
    /// Small environmental details that make the valley feel looked after: tufts of grass, reeds along the stream and the pond,
    /// stepping stones beside the paths and a little dock. The grass and reeds are tiny custom meshes (a few triangles per blade,
    /// opaque, no alpha textures), so hundreds of them cost almost nothing; everything is static and batched.
    /// </summary>
    internal static class Scenery
    {
        public static void Build(Transform world, Terrain terrain, MaterialLibrary m)
        {
            AssetPaths.EnsureFolder(AssetPaths.Meshes);
            Transform group = Pieces.Group("Details", world, Vector3.zero);
            Mesh tuftA = BladeCluster("GrassTuftA", 6, 0.36f, 0.11f, 1);
            Mesh tuftB = BladeCluster("GrassTuftB", 5, 0.27f, 0.09f, 2);
            Mesh reeds = BladeCluster("ReedCluster", 7, 1.30f, 0.20f, 3);

            ScatterGrass(group, terrain, m, tuftA, tuftB);
            PlaceReeds(group, terrain, m, reeds);
            PlaceStones(group, terrain, m);
            BuildDock(group, terrain, m);
        }

        static Vector3 Ground(Terrain terrain, Vector2 p)
        {
            float y = terrain.SampleHeight(new Vector3(p.x, 0f, p.y)) + terrain.transform.position.y;
            return new Vector3(p.x, y, p.y);
        }

        /// <summary>True where decoration may stand without hiding a path, a building, the water or anything the player uses.</summary>
        static bool Allowed(Vector2 p, float pathClearance)
        {
            if (Mathf.Abs(p.x) > 47f || Mathf.Abs(p.y) > 47f) return false;
            if (p.magnitude < ValleyLayout.PlazaRadius + 1f) return false;
            if (ValleyLayout.DistanceToPaths(p) < pathClearance) return false;
            if (Vector2.Distance(p, ValleyLayout.Spawn) < 4f || Vector2.Distance(p, ValleyLayout.Cottage) < 6.5f) return false;
            if (Vector2.Distance(p, ValleyLayout.Pond) < ValleyLayout.PondRadius + 0.6f) return false;
            if (ValleyLayout.DistanceToPolyline(p, ValleyLayout.Stream) < 2.0f) return false;
            if (Vector2.Distance(p, ValleyLayout.Spring) < 3.5f) return false;
            return !ValleyLayout.NearGameplaySpot(p, 1.2f);
        }

        // ---- Grass ------------------------------------------------------------------------------------------------------------

        static void ScatterGrass(Transform parent, Terrain terrain, MaterialLibrary m, Mesh a, Mesh b)
        {
            Transform group = Pieces.Group("GrassTufts", parent, Vector3.zero);
            System.Random random = new System.Random(4242);
            int placed = 0;
            for (int attempt = 0; attempt < 6000 && placed < 520; attempt++)
            {
                float radius = Mathf.Lerp(7f, 46f, Mathf.Sqrt((float)random.NextDouble()));
                float angle = (float)(random.NextDouble() * Mathf.PI * 2.0);
                Vector2 p = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                if (!Allowed(p, 1.3f)) continue;

                bool light = random.NextDouble() < 0.4;
                Instance(group, "Tuft" + placed, random.NextDouble() < 0.55 ? a : b, m[light ? "GrassBladeLight" : "GrassBlade"],
                    Ground(terrain, p), (float)random.NextDouble() * 360f, 0.8f + (float)random.NextDouble() * 0.8f);
                placed++;
            }
            Debug.Log("[Echoes] Scenery: " + placed + " grass tufts");
        }

        static void PlaceReeds(Transform parent, Terrain terrain, MaterialLibrary m, Mesh reeds)
        {
            Transform group = Pieces.Group("Reeds", parent, Vector3.zero);
            System.Random random = new System.Random(77);
            List<Vector2> line = ValleyLayout.Stream;
            int placed = 0;

            // Along both banks of the stream, leaving the spring end open
            for (int i = 3; i < line.Count - 1; i += 2)
            {
                Vector2 direction = (line[Mathf.Min(i + 1, line.Count - 1)] - line[i - 1]).normalized;
                Vector2 side = new Vector2(-direction.y, direction.x);
                for (int bank = -1; bank <= 1; bank += 2)
                {
                    if (random.NextDouble() < 0.25) continue;
                    Vector2 p = line[i] + side * (bank * (ValleyLayout.StreamWidth * 0.5f + 0.55f + (float)random.NextDouble() * 0.8f));
                    Instance(group, "Reed" + placed++, reeds, m["Reed"], Ground(terrain, p), (float)random.NextDouble() * 360f, 0.75f + (float)random.NextDouble() * 0.5f);
                    if (random.NextDouble() < 0.5)
                        Cattail(group, terrain, m, p + new Vector2(0.25f, 0.2f), random);
                }
            }

            // Around the pond, except where the dock is (east)
            for (int i = 0; i < 16; i++)
            {
                float angle = i * 22.5f;
                if (angle < 25f || angle > 335f) continue;
                float rad = angle * Mathf.Deg2Rad;
                float radius = ValleyLayout.PondRadius + 0.2f + (float)random.NextDouble() * 0.7f;
                Vector2 p = ValleyLayout.Pond + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
                if (random.NextDouble() < 0.2) continue;
                Instance(group, "Reed" + placed++, reeds, m["Reed"], Ground(terrain, p), (float)random.NextDouble() * 360f, 0.7f + (float)random.NextDouble() * 0.5f);
                if (random.NextDouble() < 0.45) Cattail(group, terrain, m, p + new Vector2(-0.2f, 0.3f), random);
            }
            Debug.Log("[Echoes] Scenery: " + placed + " reed clusters");
        }

        static void Cattail(Transform parent, Terrain terrain, MaterialLibrary m, Vector2 p, System.Random random)
        {
            Vector3 foot = Ground(terrain, p);
            float height = 1.0f + (float)random.NextDouble() * 0.35f;
            Transform tail = Pieces.Group("Cattail", parent, foot, (float)random.NextDouble() * 360f);
            Pieces.Prim(tail, PrimitiveType.Cylinder, "Stalk", new Vector3(0f, height * 0.5f, 0f), new Vector3(0.025f, height * 0.5f, 0.025f), m["Stem"]);
            Pieces.Prim(tail, PrimitiveType.Capsule, "Head", new Vector3(0f, height + 0.02f, 0f), new Vector3(0.075f, 0.13f, 0.075f), m["WoodDark"]);
            Pieces.NoShadows(tail.gameObject);
        }

        // ---- Stones and the dock -------------------------------------------------------------------------------------------------

        static void PlaceStones(Transform parent, Terrain terrain, MaterialLibrary m)
        {
            Transform group = Pieces.Group("PathStones", parent, Vector3.zero);
            System.Random random = new System.Random(911);
            int placed = 0;
            foreach (Vector2[] line in ValleyLayout.PathLines)
            {
                float travelled = 0f;
                float next = 1.2f;
                bool left = false;
                for (int i = 0; i < line.Length - 1; i++)
                {
                    Vector2 a = line[i];
                    Vector2 b = line[i + 1];
                    float length = Vector2.Distance(a, b);
                    Vector2 direction = (b - a) / length;
                    Vector2 side = new Vector2(-direction.y, direction.x);
                    while (next <= travelled + length)
                    {
                        Vector2 p = a + direction * (next - travelled) + side * ((left ? 1f : -1f) * (1.55f + (float)random.NextDouble() * 0.5f));
                        left = !left;
                        next += 1.9f + (float)random.NextDouble() * 1.4f;
                        if (p.magnitude < ValleyLayout.PlazaRadius + 0.8f || ValleyLayout.NearGameplaySpot(p, 1.2f)) continue;
                        if (Vector2.Distance(p, ValleyLayout.Cottage) < 6f || ValleyLayout.DistanceToPolyline(p, ValleyLayout.Stream) < 2.5f) continue;

                        Vector3 at = Ground(terrain, p) + Vector3.down * 0.05f;
                        float size = 0.30f + (float)random.NextDouble() * 0.22f;
                        GameObject stone = Pieces.Prim(group, PrimitiveType.Sphere, "Stone" + placed++, at, new Vector3(size, size * 0.5f, size * 0.8f),
                            m[random.NextDouble() < 0.3 ? "StoneMoss" : "Stone"], Col.None, new Vector3(0f, (float)random.NextDouble() * 180f, 0f));
                        stone.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                    }
                    travelled += length;
                }
            }
            Debug.Log("[Echoes] Scenery: " + placed + " path stones");
        }

        /// <summary>A short wooden dock on the pond's east shore (the reeds leave that side open).</summary>
        static void BuildDock(Transform parent, Terrain terrain, MaterialLibrary m)
        {
            Vector2 shore = ValleyLayout.Pond + new Vector2(ValleyLayout.PondRadius - 0.5f, 0f);
            Transform dock = Pieces.Group("Dock", parent, new Vector3(shore.x, ValleyLayout.WaterLevel + 0.30f, shore.y));
            for (int i = 0; i < 9; i++)
                Pieces.Prim(dock, PrimitiveType.Cube, "Plank" + i, new Vector3(-i * 0.5f, 0f, 0f), new Vector3(0.44f, 0.07f, 1.7f), m[i % 2 == 0 ? "Wood" : "WoodDark"]);
            for (int side = -1; side <= 1; side += 2)
            {
                Pieces.Prim(dock, PrimitiveType.Cylinder, "PostNear" + side, new Vector3(0.1f, -0.1f, side * 0.8f), new Vector3(0.14f, 0.55f, 0.14f), m["WoodDark"]);
                Pieces.Prim(dock, PrimitiveType.Cylinder, "PostFar" + side, new Vector3(-4.1f, -0.1f, side * 0.8f), new Vector3(0.14f, 0.55f, 0.14f), m["WoodDark"]);
            }
            Pieces.Prim(dock, PrimitiveType.Cylinder, "Lantern", new Vector3(-4.1f, 0.62f, 0.8f), new Vector3(0.04f, 0.35f, 0.04f), m["WoodDark"]);
            Pieces.Prim(dock, PrimitiveType.Sphere, "LanternGlass", new Vector3(-4.1f, 1.02f, 0.8f), Vector3.one * 0.2f, m["LanternLamp"]);
            Pieces.NoShadows(dock.gameObject);
        }

        // ---- Meshes -------------------------------------------------------------------------------------------------------------

        static GameObject Instance(Transform parent, string name, Mesh mesh, Material material, Vector3 position, float yaw, float scale)
        {
            GameObject go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            go.transform.localScale = Vector3.one * scale;
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        /// <summary>
        /// A tuft: several thin tapering blades leaning outwards, each a single triangle drawn from both sides (so no two-sided
        /// shader is needed). Normals point up, which makes the grass light softly from above whichever way a blade faces.
        /// </summary>
        static Mesh BladeCluster(string assetName, int blades, float height, float spread, int seed)
        {
            string path = AssetPaths.Meshes + "/" + assetName + ".asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                mesh = new Mesh { name = assetName };
                AssetDatabase.CreateAsset(mesh, path);
            }
            mesh.Clear();

            System.Random random = new System.Random(seed);
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            for (int i = 0; i < blades; i++)
            {
                float angle = (i * 360f / blades + (float)random.NextDouble() * 35f) * Mathf.Deg2Rad;
                Vector3 outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 baseCenter = outward * ((float)random.NextDouble() * spread);
                Vector3 tangent = new Vector3(-outward.z, 0f, outward.x) * (0.026f + height * 0.03f);
                float h = height * (0.7f + (float)random.NextDouble() * 0.55f);
                Vector3 tip = baseCenter + outward * (0.05f + (float)random.NextDouble() * 0.16f * height * 2f) + Vector3.up * h;

                int start = vertices.Count;
                vertices.Add(baseCenter - tangent);
                vertices.Add(baseCenter + tangent);
                vertices.Add(tip);
                // front and back
                triangles.AddRange(new[] { start, start + 2, start + 1, start, start + 1, start + 2 });
            }
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            Vector3[] normals = new Vector3[vertices.Count];
            for (int i = 0; i < normals.Length; i++) normals[i] = Vector3.up;
            mesh.normals = normals;
            mesh.uv = new Vector2[vertices.Count];
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            return mesh;
        }
    }
}
