using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Echoes.EditorTools
{
    /// <summary>
    /// Read-only audit of the Phase 5.2 pilot: measures where the original dirt paths went, how large the foliage cards are and
    /// renders matched top-down terrain views. It writes only to the explicit output folder (never into Assets).
    /// </summary>
    public static class Phase52B2Diagnostics
    {
        // Pilot region in world metres (the corridor plus the windmill knoll).
        const float X0 = -12, X1 = 44, Z0 = -24, Z1 = 44;

        static string Output()
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-echoesDiagDir");
            string dir = i >= 0 && i + 1 < args.Length ? args[i + 1] : Path.GetFullPath(Path.Combine(Application.dataPath, "../../artifacts/visual-qa/phase-5.2-b2/diagnostics"));
            Directory.CreateDirectory(dir);
            return dir;
        }

        static bool DirtLike(string name)
        {
            string n = name.ToLowerInvariant();
            return n.Contains("path") || n.Contains("trail") || n.Contains("dirt") || n.Contains("sand");
        }

        /// <summary>Per-texel "dirt-like" weight of a terrain, summed over every path/trail/dirt layer.</summary>
        static float[,] Dirt(TerrainData data, out string[] layerNames)
        {
            float[,,] a = data.GetAlphamaps(0, 0, data.alphamapWidth, data.alphamapHeight);
            layerNames = data.terrainLayers.Select(l => l != null ? l.name : "(null)").ToArray();
            var dirt = new float[data.alphamapHeight, data.alphamapWidth];
            for (int layer = 0; layer < layerNames.Length; layer++)
            {
                if (!DirtLike(layerNames[layer])) continue;
                for (int z = 0; z < data.alphamapHeight; z++)
                    for (int x = 0; x < data.alphamapWidth; x++) dirt[z, x] += a[z, x, layer];
            }
            return dirt;
        }

        public static void TerrainAudit()
        {
            string dir = Output();
            var report = new List<string>();

            EditorSceneManager.OpenScene(AssetPaths.ValleyScene, OpenSceneMode.Single);
            Terrain baseTerrain = Terrain.activeTerrain;
            TerrainData baseData = baseTerrain.terrainData;
            Vector3 origin = baseTerrain.transform.position;
            float[,] baseDirt = Dirt(baseData, out string[] baseNames);
            report.Add("baseline terrain: size=" + baseData.size + " origin=" + origin + " alphamap=" + baseData.alphamapWidth + " layers=[" + string.Join(", ", baseNames) + "]");
            TopDown(dir + "/terrain-topdown-baseline.png");

            EditorSceneManager.OpenScene(Phase52PilotBuilder.Valley, OpenSceneMode.Single);
            var pilot = UnityEngine.Object.FindFirstObjectByType<Phase52Pilot>();
            float[,] pilotDirtFine = Dirt(pilot.PilotTerrain, out string[] pilotNames);
            report.Add("pilot terrain: alphamap=" + pilot.PilotTerrain.alphamapWidth + " layers=[" + string.Join(", ", pilotNames) + "]");
            var pilotDirt = new float[baseData.alphamapHeight, baseData.alphamapWidth];
            for (int zz = 0; zz < baseData.alphamapHeight; zz++)
                for (int xx = 0; xx < baseData.alphamapWidth; xx++)
                    pilotDirt[zz, xx] = Sample(pilotDirtFine, new Vector2(origin.x + (xx + .5f) / baseData.alphamapWidth * baseData.size.x, origin.z + (zz + .5f) / baseData.alphamapHeight * baseData.size.z), origin, pilot.PilotTerrain);

            int w = baseData.alphamapWidth, h = baseData.alphamapHeight;
            float texel = baseData.size.x / w;
            float area = texel * texel;
            double baseMass = 0, pilotMass = 0, lost = 0, gained = 0;
            int inRegion = 0;
            for (int z = 0; z < h; z++)
                for (int x = 0; x < w; x++)
                {
                    float wx = origin.x + (x + .5f) / w * baseData.size.x, wz = origin.z + (z + .5f) / h * baseData.size.z;
                    if (wx < X0 || wx > X1 || wz < Z0 || wz > Z1) continue;
                    inRegion++;
                    float b = baseDirt[z, x], p = pilotDirt[z, x];
                    baseMass += b; pilotMass += p;
                    lost += Mathf.Max(0, b - p); gained += Mathf.Max(0, p - b);
                }
            report.Add(string.Format("region x[{0},{1}] z[{2},{3}] texel={4:F3}m", X0, X1, Z0, Z1, texel));
            report.Add(string.Format("dirt area baseline={0:F1} m2, pilot={1:F1} m2, erased={2:F1} m2, newly painted={3:F1} m2, erased share of baseline={4:P0}", baseMass * area, pilotMass * area, lost * area, gained * area, lost / Math.Max(1e-6, baseMass)));

            // Does the new paint land where the old paths were, and where the new route centrelines are?
            int oldSamples = 0, oldKept = 0, routeSamples = 0, routeDirt = 0;
            foreach (Vector2[] line in ValleyLayout.PathLines)
                for (int s = 0; s < line.Length - 1; s++)
                {
                    float len = Vector2.Distance(line[s], line[s + 1]);
                    for (float t = 0; t < len; t += .5f)
                    {
                        Vector2 p = Vector2.Lerp(line[s], line[s + 1], t / len);
                        if (p.x < X0 || p.x > X1 || p.y < Z0 || p.y > Z1) continue;
                        if (Phase52EnvironmentBuilder.Distance(p) > 8f && Vector2.Distance(p, ValleyLayout.Windmill) > 12f) continue;
                        oldSamples++;
                        if (Sample(pilotDirtFine, p, origin, pilot.PilotTerrain) > .5f) oldKept++;
                    }
                }
            for (float x = X0; x < X1; x += .5f)
                for (float z = Z0; z < Z1; z += .5f)
                {
                    var p = new Vector2(x, z);
                    if (Phase52EnvironmentBuilder.Distance(p) > .25f) continue;
                    routeSamples++;
                    if (Sample(pilotDirtFine, p, origin, pilot.PilotTerrain) > .5f) routeDirt++;
                }
            report.Add("original path centreline samples inside the pilot: " + oldSamples + ", of which still dirt (>0.5) in the pilot terrain: " + oldKept);
            report.Add("new route centreline samples: " + routeSamples + ", of which dirt (>0.5): " + routeDirt);

            Export(dir + "/dirt-baseline.png", baseDirt, null, origin, baseData);
            Export(dir + "/dirt-pilot.png", pilotDirt, null, origin, baseData);
            Export(dir + "/dirt-difference.png", baseDirt, pilotDirt, origin, baseData);

            // Foliage cards in the three pilot tree meshes.
            for (int k = 0; k < 3; k++)
            {
                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(Phase52HeroBuilder.Folder + "/Tree_" + k + ".asset");
                if (mesh == null) continue;
                var v = mesh.vertices;
                int[] t = mesh.GetTriangles(1);
                float maxCard = 0, minCard = 99, low = 99, top = -99;
                for (int i = 0; i + 5 < t.Length; i += 6)
                {
                    float edge0 = Vector3.Distance(v[t[i]], v[t[i + 1]]), edge1 = Vector3.Distance(v[t[i]], v[t[i + 5]]);
                    maxCard = Mathf.Max(maxCard, edge0, edge1); minCard = Mathf.Min(minCard, edge0, edge1);
                    foreach (int idx in new[] { t[i], t[i + 1], t[i + 2], t[i + 5] }) { low = Mathf.Min(low, v[idx].y); top = Mathf.Max(top, v[idx].y); }
                }
                report.Add(string.Format("Tree_{0}: leaf cards={1} edge {2:F2}..{3:F2} m, canopy y {4:F2}..{5:F2} m, bounds {6}", k, t.Length / 6, minCard, maxCard, low, top, mesh.bounds.size));
            }
            var camera = UnityEngine.Object.FindFirstObjectByType<ThirdPersonCamera>();
            report.Add("camera: distance=" + camera.Distance + " pitch " + camera.MinPitch + ".." + camera.MaxPitch + "; FreeDistance sphere-casts against colliders only (leaf cards have none).");
            pilot.Apply(false);
            TopDown(dir + "/terrain-topdown-pilot.png");
            Debug.Log("[Echoes][P52 B2 DIAG]\n" + string.Join("\n", report));
            File.WriteAllLines(dir + "/terrain-foliage-audit.txt", report);
        }

        /// <summary>Inspects the saved terrain assets themselves: layer count, splat textures and what the pilot copy still holds.</summary>
        public static void PilotAssetAudit()
        {
            string dir = Output();
            var report = new List<string>();
            foreach (string path in new[] { "Assets/Echoes/Art/Terrain/ValleyTerrainData.asset", Phase52HeroBuilder.Folder + "/PilotTerrain.asset" })
            {
                var data = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
                if (data == null) { report.Add(path + ": missing"); continue; }
                var textures = data.alphamapTextures;
                report.Add(path + ": layers=" + data.terrainLayers.Length + " alphamapLayers=" + data.alphamapLayers + " alphamapResolution=" + data.alphamapResolution + " splatTextures=" + textures.Length);
                foreach (Texture2D texture in textures)
                {
                    string stats = "null";
                    if (texture != null)
                    {
                        Color[] pixels = texture.GetPixels();
                        Color sum = Color.black;
                        foreach (Color c in pixels) sum += c;
                        sum /= pixels.Length;
                        stats = texture.name + " " + texture.width + "x" + texture.height + " mean RGBA=" + sum.ToString("F3");
                    }
                    report.Add("  splat: " + stats);
                }
                var objects = AssetDatabase.LoadAllAssetsAtPath(path);
                report.Add("  sub-assets: " + string.Join(", ", objects.Select(o => o == null ? "null" : o.GetType().Name + ":" + o.name)));
                float[,,] a = data.GetAlphamaps(0, 0, data.alphamapWidth, data.alphamapHeight);
                int layers = a.GetLength(2);
                var total = new double[layers];
                for (int z = 0; z < data.alphamapHeight; z++) for (int x = 0; x < data.alphamapWidth; x++) for (int l = 0; l < layers; l++) total[l] += a[z, x, l];
                report.Add("  mean weight per layer: " + string.Join(", ", total.Select((t, i) => data.terrainLayers[i].name + "=" + (t / (data.alphamapWidth * data.alphamapHeight)).ToString("F3"))));
                int off = 0; float min = 9, max = 0; var examples = new List<string>();
                for (int z = 0; z < data.alphamapHeight; z++)
                    for (int x = 0; x < data.alphamapWidth; x++)
                    {
                        float sum = 0; for (int l = 0; l < layers; l++) sum += a[z, x, l];
                        min = Mathf.Min(min, sum); max = Mathf.Max(max, sum);
                        if (Mathf.Abs(sum - 1f) > .02f) { off++; if (examples.Count < 6) examples.Add("(" + x + "," + z + ") sum=" + sum.ToString("F3") + " [" + string.Join(" ", Enumerable.Range(0, layers).Select(l => a[z, x, l].ToString("F2"))) + "]"); }
                    }
                report.Add("  weight sum per texel: min=" + min.ToString("F3") + " max=" + max.ToString("F3") + " texels off by >0.02: " + off + " of " + data.alphamapWidth * data.alphamapHeight + (examples.Count > 0 ? "; e.g. " + string.Join("; ", examples) : ""));
            }
            EditorSceneManager.OpenScene(Phase52PilotBuilder.Valley, OpenSceneMode.Single);
            report.Add("--- pilot scene objects near the corridor ---");
            var pilotScene = UnityEngine.Object.FindFirstObjectByType<Phase52Pilot>();
            report.Add("scene terrainData=" + AssetDatabase.GetAssetPath(pilotScene.Terrain.terrainData) + "; Phase52Pilot.PilotTerrain=" + AssetDatabase.GetAssetPath(pilotScene.PilotTerrain) + "; baseline=" + AssetDatabase.GetAssetPath(pilotScene.BaselineTerrain));
            foreach (var capsule in UnityEngine.Object.FindObjectsByType<CapsuleCollider>(FindObjectsSortMode.None).OrderBy(c => c.transform.position.z))
                report.Add("capsule " + capsule.name + " pos=" + capsule.transform.position.ToString("F1") + " r=" + capsule.radius + " h=" + capsule.height);
            foreach (var node in UnityEngine.Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None)) report.Add("node " + node.name + " " + node.transform.position.ToString("F1"));
            foreach (var npc in UnityEngine.Object.FindObjectsByType<NpcInteractable>(FindObjectsSortMode.None)) report.Add("npc " + npc.name + " " + npc.transform.position.ToString("F1"));
            var camera = UnityEngine.Object.FindFirstObjectByType<ThirdPersonCamera>();
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            var controller = player.GetComponent<CharacterController>();
            report.Add("player root=" + player.transform.position.ToString("F2") + " controller h=" + controller.height + " r=" + controller.radius + " centre=" + controller.center + " camera=" + camera.transform.position.ToString("F2"));
            foreach (var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) report.Add("light " + light.name + " type=" + light.type + " intensity=" + light.intensity + " color=" + light.color + " shadows=" + light.shadows + " euler=" + light.transform.eulerAngles);
            report.Add("ambient mode=" + RenderSettings.ambientMode + " sky=" + RenderSettings.ambientSkyColor + " equator=" + RenderSettings.ambientEquatorColor + " ground=" + RenderSettings.ambientGroundColor + " fog=" + RenderSettings.fog + " fogColor=" + RenderSettings.fogColor);
            File.WriteAllLines(dir + "/pilot-asset-audit.txt", report);
            Debug.Log("[Echoes][P52 B2 DIAG]\n" + string.Join("\n", report));
        }

        static float Sample(float[,] map, Vector2 p, Vector3 origin, TerrainData data)
        {
            int x = Mathf.Clamp(Mathf.RoundToInt((p.x - origin.x) / data.size.x * data.alphamapWidth - .5f), 0, data.alphamapWidth - 1);
            int z = Mathf.Clamp(Mathf.RoundToInt((p.y - origin.z) / data.size.z * data.alphamapHeight - .5f), 0, data.alphamapHeight - 1);
            return map[z, x];
        }

        /// <summary>Greyscale = dirt weight; with a second map red = lost dirt, green = new dirt. Blue dots = original path lines, yellow dots = pilot routes.</summary>
        static void Export(string file, float[,] a, float[,] b, Vector3 origin, TerrainData data)
        {
            const int scale = 5;
            int w = Mathf.RoundToInt((X1 - X0) * data.alphamapWidth / data.size.x), h = Mathf.RoundToInt((Z1 - Z0) * data.alphamapHeight / data.size.z);
            int ox = Mathf.RoundToInt((X0 - origin.x) / data.size.x * data.alphamapWidth), oz = Mathf.RoundToInt((Z0 - origin.z) / data.size.z * data.alphamapHeight);
            var image = new Texture2D(w * scale, h * scale, TextureFormat.RGB24, false);
            for (int z = 0; z < h; z++)
                for (int x = 0; x < w; x++)
                {
                    int sx = Mathf.Clamp(ox + x, 0, data.alphamapWidth - 1), sz = Mathf.Clamp(oz + z, 0, data.alphamapHeight - 1);
                    Color c;
                    if (b == null) c = Color.white * a[sz, sx];
                    else { float d = b[sz, sx] - a[sz, sx]; c = new Color(Mathf.Max(0, -d), Mathf.Max(0, d), 0f) + Color.white * .12f * Mathf.Max(a[sz, sx], b[sz, sx]); }
                    for (int dy = 0; dy < scale; dy++) for (int dx = 0; dx < scale; dx++) image.SetPixel(x * scale + dx, z * scale + dy, c);
                }
            void Dot(Vector2 p, Color color)
            {
                int px = Mathf.RoundToInt((p.x - X0) / data.size.x * data.alphamapWidth * scale), pz = Mathf.RoundToInt((p.y - Z0) / data.size.z * data.alphamapHeight * scale);
                for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                    if (px + dx >= 0 && px + dx < image.width && pz + dy >= 0 && pz + dy < image.height) image.SetPixel(px + dx, pz + dy, color);
            }
            foreach (Vector2[] line in ValleyLayout.PathLines)
                for (int s = 0; s < line.Length - 1; s++)
                {
                    float len = Vector2.Distance(line[s], line[s + 1]);
                    for (float t = 0; t < len; t += .25f) Dot(Vector2.Lerp(line[s], line[s + 1], t / len), new Color(.2f, .5f, 1f));
                }
            for (float x = X0; x < X1; x += .2f) for (float z = Z0; z < Z1; z += .2f) if (Phase52EnvironmentBuilder.Distance(new Vector2(x, z)) < .1f) Dot(new Vector2(x, z), new Color(1f, .9f, .1f));
            image.Apply();
            File.WriteAllBytes(file, image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);
        }

        /// <summary>Terrain-only orthographic top view of the audited region, using whichever terrain data the open scene shows.</summary>
        static void TopDown(string file)
        {
            foreach (Renderer renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)) renderer.enabled = false;
            var go = new GameObject("DiagCamera");
            Camera camera = go.AddComponent<Camera>();
            camera.orthographic = true; camera.orthographicSize = (Z1 - Z0) / 2f; camera.aspect = (X1 - X0) / (Z1 - Z0);
            camera.transform.position = new Vector3((X0 + X1) / 2f, 60f, (Z0 + Z1) / 2f); camera.transform.rotation = Quaternion.Euler(90, 0, 0);
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.magenta; camera.nearClipPlane = .5f; camera.farClipPlane = 200f;
            const int width = 1120, height = 1360;
            var rt = RenderTexture.GetTemporary(width, height, 24); camera.targetTexture = rt;
            camera.Render(); camera.Render();
            RenderTexture.active = rt;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
            File.WriteAllBytes(file, image.EncodeToPNG());
            RenderTexture.active = null; camera.targetTexture = null; RenderTexture.ReleaseTemporary(rt);
            UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
