using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Echoes.EditorTools;
using Object = UnityEngine.Object;

namespace Echoes.Tests
{
    /// <summary>Phase 5.2-B2 revision checks. They measure assets and data; they cannot judge whether the art is beautiful.</summary>
    public class Phase52B2Tests
    {
        const string Folder = Phase52HeroBuilder.Folder;
        static GameObject Make() => Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Phase52HeroBuilder.PrefabPath));
        static AnimationClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder + "/Animations/" + name + ".anim");

        [Test] public void Hero_UsesFewPaintedToonMaterials_WithTexturesPassesAndABlinkTwin()
        {
            var root = Make();
            try
            {
                var skin = root.GetComponent<SkinnedMeshRenderer>(); Material[] materials = skin.sharedMaterials;
                Assert.AreEqual(8, materials.Length, "Few painted materials instead of many flat colours.");
                foreach (Material m in materials)
                {
                    Assert.IsNotNull(m); Assert.AreEqual("Echoes/ToonCharacter", m.shader.name, m.name); Assert.IsTrue(m.shader.isSupported, m.name);
                    var texture = m.GetTexture("_BaseMap") as Texture2D; Assert.IsNotNull(texture, m.name + " has no painted texture"); Assert.GreaterOrEqual(texture.width, 128);
                    Assert.GreaterOrEqual(m.FindPass("ForwardLit"), 0); Assert.GreaterOrEqual(m.FindPass("Outline"), 0); Assert.GreaterOrEqual(m.FindPass("ShadowCaster"), 0);
                }
                Material skinMaterial = materials.Single(m => m.name == "Hero_Skin");
                Assert.IsNotNull(skinMaterial.GetTexture("_BlinkMap")); Assert.AreEqual(1024, skinMaterial.GetTexture("_BaseMap").width);
                Assert.IsNotNull(root.GetComponent<CaretakerFace>()); Assert.IsNotNull(root.GetComponent<SkinnedCaretakerMotion>());
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test] public void Hero_MeshBudget_IsBoundedAndEverySubmeshIsUsed()
        {
            var root = Make();
            try
            {
                Mesh mesh = root.GetComponent<SkinnedMeshRenderer>().sharedMesh;
                int triangles = 0; for (int i = 0; i < mesh.subMeshCount; i++) { int count = (int)mesh.GetIndexCount(i) / 3; Assert.Greater(count, 100, "Empty submesh " + i); triangles += count; }
                Assert.That(triangles, Is.InRange(30000, 70000)); Assert.That(mesh.vertexCount, Is.InRange(20000, 60000));
                Assert.IsNotNull(mesh.uv); Assert.AreEqual(mesh.vertexCount, mesh.colors.Length, "Vertex AO and tint are baked into vertex colours."); Assert.AreEqual(mesh.vertexCount, mesh.normals.Length);
                var outline = new List<Vector2>(); mesh.GetUVs(2, outline); Assert.AreEqual(mesh.vertexCount, outline.Count, "Per-vertex outline scale.");
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test] public void WalkClip_PlantsTheStanceFoot_AtTheUnchangedControllerSpeed()
        {
            EditorSceneManager.OpenScene(Phase52PilotBuilder.Valley);
            float controllerSpeed = Object.FindFirstObjectByType<PlayerController>().MoveSpeed;
            var root = Make();
            var baked = new Mesh();
            try
            {
                var skin = root.GetComponent<SkinnedMeshRenderer>(); var motion = root.GetComponent<SkinnedCaretakerMotion>(); AnimationClip clip = Clip("Walk");
                Assert.AreEqual(4.5f, controllerSpeed, "The controller speed must stay at 4.5 m/s.");
                Assert.That(motion.StrideMetres / clip.length, Is.EqualTo(controllerSpeed).Within(.02f), "One cycle of the clip covers exactly the distance the controller travels in one cycle.");
                Vector3[] rest = skin.sharedMesh.vertices; int[] sole = Enumerable.Range(0, rest.Length).Where(i => rest[i].y < .03f).ToArray();
                Assert.Greater(sole.Length, 200);
                const int frames = 60; float dt = clip.length / frames; var track = new Vector3[frames + 1][]; float lowest = 9f;
                for (int k = 0; k <= frames; k++)
                {
                    clip.SampleAnimation(root, k * dt); skin.BakeMesh(baked, false); Vector3[] v = baked.vertices;
                    track[k] = sole.Select(i => v[i]).ToArray(); lowest = Mathf.Min(lowest, track[k].Min(p => p.y));
                }
                var speeds = new List<float>();
                for (int k = 0; k < frames; k++)
                    for (int j = 0; j < sole.Length; j++)
                    {
                        if (track[k][j].y > .015f || track[k + 1][j].y > .015f) continue;
                        Vector3 groundMove = track[k + 1][j] - track[k][j] + new Vector3(0, 0, controllerSpeed * dt); // character frame -> ground frame
                        speeds.Add(new Vector2(groundMove.x, groundMove.z).magnitude / dt);
                    }
                speeds.Sort(); Assert.Greater(speeds.Count, 300, "The feet must touch the ground during the cycle.");
                float median = speeds[speeds.Count / 2], p90 = speeds[(int)(speeds.Count * .9f)];
                Debug.Log("[P52B2 foot planting] lowest sole vertex=" + lowest + " m; contact samples=" + speeds.Count + "; ground speed of contact vertices median=" + median + " p90=" + p90 + " m/s (controller " + controllerSpeed + ")");
                Assert.Greater(lowest, -.015f, "No sole vertex may sink into the ground.");
                Assert.Less(median, .08f, "A planted foot must not slide at the controller speed."); Assert.Less(p90, .9f);
            }
            finally { Object.DestroyImmediate(baked); Object.DestroyImmediate(root); }
        }

        [Test] public void TurnClips_KeyEveryBone_SoLayeredBlendingNeverFallsBackToTheRestPose()
        {
            foreach (string name in new[] { "TurnLeft", "TurnRight" })
            {
                var bindings = AnimationUtility.GetCurveBindings(Clip(name));
                Assert.That(bindings.Select(b => b.path).Distinct().Count(), Is.GreaterThanOrEqualTo(40), name);
            }
        }

        [Test] public void Trees_AreSmallLeafClusters_HighAboveTheCameraCorridor()
        {
            for (int k = 0; k < 3; k++)
            {
                Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(Folder + "/Tree_" + k + ".asset"); Assert.IsNotNull(mesh);
                Vector3[] v = mesh.vertices; int[] t = mesh.GetTriangles(1); Assert.AreEqual(0, t.Length % 6); int cards = t.Length / 6;
                Assert.That(cards, Is.InRange(40, 200), "Tree_" + k);
                float lowest = 99, biggest = 0;
                for (int i = 0; i < t.Length; i += 6)
                {
                    biggest = Mathf.Max(biggest, Vector3.Distance(v[t[i]], v[t[i + 1]]), Vector3.Distance(v[t[i]], v[t[i + 5]]));
                    foreach (int idx in new[] { t[i], t[i + 1], t[i + 2], t[i + 5] }) lowest = Mathf.Min(lowest, v[idx].y);
                }
                Assert.LessOrEqual(biggest, Phase52EnvironmentBuilder.MaxCardSize + .05f, "No large flat leaf card (Tree_" + k + ").");
                Assert.GreaterOrEqual(lowest, 2.0f, "Leaves stay above the free-camera corridor (Tree_" + k + ").");
                Assert.That(mesh.GetTriangles(0).Length, Is.GreaterThan(100), "Real trunk and branches.");
            }
        }

        [Test] public void FoliageMaterials_UseTheDitherFadeShader_AndTheDriverIsInThePilotScene()
        {
            foreach (string name in new[] { "PilotLeaves", "PilotPlants" })
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/" + name + ".mat"); Assert.IsNotNull(m);
                Assert.AreEqual("Echoes/Foliage", m.shader.name); Assert.IsTrue(m.shader.isSupported); Assert.GreaterOrEqual(m.FindPass("ShadowCaster"), 0);
                Assert.That(m.GetFloat("_NearFadeEnd"), Is.GreaterThan(m.GetFloat("_NearFadeStart"))); Assert.IsNotNull(m.GetTexture("_BaseMap"));
            }
            var leaves = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/PilotLeaves.mat");
            Assert.That(leaves.GetFloat("_NearFadeStart"), Is.InRange(.5f, 1.5f)); Assert.That(leaves.GetFloat("_NearFadeEnd"), Is.GreaterThanOrEqualTo(3f)); Assert.AreEqual(1f, leaves.GetFloat("_OccluderStrength"));
            EditorSceneManager.OpenScene(Phase52PilotBuilder.Valley);
            Assert.IsNotNull(Object.FindFirstObjectByType<FoliageFadeDriver>(), "The sight-line fade needs its driver.");
            var pilot = Object.FindFirstObjectByType<Phase52Pilot>(); var routeTrees = pilot.Route.GetComponentsInChildren<MeshRenderer>().Where(r => r.name.EndsWith("_Pilot")).ToArray();
            Assert.GreaterOrEqual(routeTrees.Length, 6);
            foreach (var r in routeTrees) Assert.AreSame(leaves, r.sharedMaterials[1]);
        }

        // ---------------------------------------------------------------------------------------- terrain

        static float Weight(TerrainData data, float[,,] alpha, string[] names, Vector2 world, Vector3 origin, params string[] layers)
        {
            int x = Mathf.Clamp(Mathf.FloorToInt((world.x - origin.x) / data.size.x * data.alphamapWidth), 0, data.alphamapWidth - 1);
            int z = Mathf.Clamp(Mathf.FloorToInt((world.y - origin.z) / data.size.z * data.alphamapHeight), 0, data.alphamapHeight - 1);
            float sum = 0; foreach (string layer in layers) { int i = System.Array.IndexOf(names, layer); if (i >= 0) sum += alpha[z, x, i]; }
            return sum;
        }

        [Test] public void PilotTerrain_KeepsTheOriginalPathsAndPlaza_SumsToOne_AndIsNotUniform()
        {
            EditorSceneManager.OpenScene(Phase52PilotBuilder.Valley); var pilot = Object.FindFirstObjectByType<Phase52Pilot>();
            TerrainData b = pilot.BaselineTerrain, p = pilot.PilotTerrain; Vector3 origin = pilot.Terrain.transform.position;
            float[,,] ba = b.GetAlphamaps(0, 0, b.alphamapWidth, b.alphamapHeight), pa = p.GetAlphamaps(0, 0, p.alphamapWidth, p.alphamapHeight);
            string[] bn = b.terrainLayers.Select(l => l.name).ToArray(), pn = p.terrainLayers.Select(l => l.name).ToArray();
            Assert.Contains("RouteMeadow", pn); Assert.Contains("RouteDirt", pn);
            // the earlier pilot saved a terrain that was 100 % Grass everywhere
            int grass = System.Array.IndexOf(pn, "Grass"); double grassMean = 0; int layers = pa.GetLength(2);
            for (int z = 0; z < p.alphamapHeight; z++) for (int x = 0; x < p.alphamapWidth; x++) grassMean += pa[z, x, grass];
            grassMean /= p.alphamapWidth * p.alphamapHeight; Assert.Less(grassMean, .85, "The pilot terrain must not collapse to a single layer.");
            int off = 0, baselineHoles = 0;
            for (int z = 0; z < p.alphamapHeight; z++)
                for (int x = 0; x < p.alphamapWidth; x++) { float sum = 0; for (int i = 0; i < layers; i++) sum += pa[z, x, i]; if (Mathf.Abs(sum - 1f) > .02f) off++; }
            for (int z = 0; z < b.alphamapHeight; z++)
                for (int x = 0; x < b.alphamapWidth; x++) { float sum = 0; for (int i = 0; i < ba.GetLength(2); i++) sum += ba[z, x, i]; if (Mathf.Abs(sum - 1f) > .02f) baselineHoles++; }
            Debug.Log("[P52B2 terrain weights] texels whose weights do not sum to 1: pilot=" + off + " of " + p.alphamapWidth * p.alphamapHeight + " (the original RC2 terrain has " + baselineHoles + " such texels along its far southern edge)");
            Assert.AreEqual(0, off, "Every pilot texel has a full set of splat weights.");
            int baseline = 0, kept = 0;
            foreach (Vector2[] line in ValleyLayout.PathLines)
                for (int s = 0; s < line.Length - 1; s++)
                {
                    float length = Vector2.Distance(line[s], line[s + 1]);
                    for (float t = 0; t < length; t += .5f)
                    {
                        Vector2 world = Vector2.Lerp(line[s], line[s + 1], t / length);
                        if (Weight(b, ba, bn, world, origin, "Path", "WindmillTrail") <= .5f) continue;
                        baseline++; if (Weight(p, pa, pn, world, origin, "Path", "WindmillTrail", "RouteDirt") > .5f) kept++;
                    }
                }
            Debug.Log("[P52B2 path preservation] original path centreline samples that were dirt=" + baseline + ", still dirt in the pilot terrain=" + kept);
            Assert.Greater(baseline, 60); Assert.GreaterOrEqual(kept / (float)baseline, .95f, "Every original dirt path stays readable.");
            int routeSamples = 0, routeDirt = 0;
            foreach (Vector2[] route in Phase52EnvironmentBuilder.Routes)
                for (int s = 0; s < route.Length - 1; s++)
                {
                    float length = Vector2.Distance(route[s], route[s + 1]);
                    for (float t = 0; t < length; t += .5f)
                    {
                        Vector2 world = Vector2.Lerp(route[s], route[s + 1], t / length);
                        if (Weight(b, ba, bn, world, origin, "Stone") > .3f) continue;
                        routeSamples++; if (Weight(p, pa, pn, world, origin, "Path", "WindmillTrail", "RouteDirt") > .5f) routeDirt++;
                    }
                }
            Assert.GreaterOrEqual(routeDirt / (float)routeSamples, .9f, "The pilot route itself reads as a path.");
            double stoneBase = 0, stonePilot = 0;
            for (int z = 0; z < b.alphamapHeight; z++) for (int x = 0; x < b.alphamapWidth; x++) stoneBase += ba[z, x, System.Array.IndexOf(bn, "Stone")];
            for (int z = 0; z < p.alphamapHeight; z++) for (int x = 0; x < p.alphamapWidth; x++) stonePilot += pa[z, x, System.Array.IndexOf(pn, "Stone")];
            stoneBase *= (float)b.size.x * b.size.z / (b.alphamapWidth * b.alphamapHeight); stonePilot *= (float)p.size.x * p.size.z / (p.alphamapWidth * p.alphamapHeight);
            Assert.That(stonePilot, Is.EqualTo(stoneBase).Within(stoneBase * .05), "The stone plaza and cliffs stay.");
        }

        [Test] public void NoPilotAssetReferencesAMissingTextureShaderOrMesh()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { Folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid); var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                Assert.IsNotNull(m.shader, path); Assert.IsTrue(m.shader.isSupported, path + " uses " + m.shader.name); Assert.AreNotEqual("Hidden/InternalErrorShader", m.shader.name, path);
            }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Phase52HeroBuilder.PrefabPath); Assert.IsNotNull(prefab.GetComponent<SkinnedMeshRenderer>().sharedMesh);
            foreach (var m in prefab.GetComponent<SkinnedMeshRenderer>().sharedMaterials) Assert.IsNotNull(m);
            foreach (string stale in Phase52HeroBuilder.ObsoleteFirstPilotMaterials) Assert.IsNull(AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Hero_" + stale + ".mat"), "Flat first-pilot material " + stale + " is gone.");
        }
    }
}
