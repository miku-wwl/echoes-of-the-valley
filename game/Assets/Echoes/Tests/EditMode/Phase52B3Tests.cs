using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Echoes.EditorTools;
using Object = UnityEngine.Object;

namespace Echoes.Tests
{
    /// <summary>Phase 5.2-B3 2.5D pilot: structure of the generated scene and the sprite data. Engineering properties only, never an art verdict.</summary>
    public class Phase52B3Tests
    {
        static SpriteSheet25D Hero => AssetDatabase.LoadAssetAtPath<SpriteSheet25D>(Phase52B3Builder.CaretakerSheetPath);

        [Test] public void B3Scene_KeepsOnePlayerRoot_AndTheSpriteIsAVisualOnlyChild()
        {
            EditorSceneManager.OpenScene(Phase52B3Builder.Valley);
            var players = Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(1, players.Length, "exactly one authoritative player");
            var player = players[0];
            Assert.IsNotNull(player.GetComponent<CharacterController>(), "the root keeps its collision");
            var presenter = player.GetComponentInChildren<CharacterVisual2D5D>(true);
            Assert.IsNotNull(presenter, "the 2.5D presenter sits under the player root");
            Assert.AreNotSame(player.gameObject, presenter.gameObject);
            Assert.IsEmpty(presenter.GetComponentsInChildren<Collider>(true), "the sprite never takes part in physics");
            Assert.IsEmpty(presenter.GetComponentsInChildren<Rigidbody>(true));
            Assert.IsEmpty(presenter.GetComponentsInChildren<CharacterController>(true));
            var b3 = Object.FindFirstObjectByType<Phase52B3Pilot>();
            Assert.IsNotNull(b3);
            var pilot = b3.GetComponent<Phase52Pilot>();
            Assert.IsNotNull(pilot.SkinnedVisual, "the B2 hero stays in the scene as a recoverable legacy visual");
            Assert.IsFalse(pilot.SkinnedVisual.activeSelf, "the B2 hero is disabled");
            Assert.IsFalse(pilot.OriginalVisual.activeSelf, "the original hero is disabled");
            Assert.AreSame(presenter.gameObject, b3.CharacterVisual);
        }

        [Test] public void CaretakerSheet_HasRunAndIdleForFourDirections_AndEHasItsOwnFrames()
        {
            var sheet = Hero;
            Assert.IsNotNull(sheet);
            Assert.IsNotNull(sheet.atlas);
            foreach (string key in new[] { "S", "N", "W", "E" })
            {
                var d = sheet.Get(key);
                Assert.IsNotNull(d, key);
                Assert.AreEqual(10, d.run.Length, key + " run frames");
                Assert.AreEqual(12, d.idle.Length, key + " idle frames");
                foreach (var f in d.run.Concat(d.idle))
                {
                    Assert.That(f.uv.x, Is.InRange(0f, 1f)); Assert.That(f.uv.y, Is.InRange(0f, 1f));
                    Assert.LessOrEqual(f.uv.x + f.uv.z, 1.0001f); Assert.LessOrEqual(f.uv.y + f.uv.w, 1.0001f);
                    Assert.That(d.pivotPx.x, Is.InRange(0f, f.sizePx.x), key + " pivot inside the frame");
                    Assert.That(d.pivotPx.y, Is.InRange(0f, f.sizePx.y * 0.2f), key + " ground pivot near the frame's bottom");
                    float metres = (f.sizePx.y - d.pivotPx.y) / sheet.pixelsPerMeter;
                    Assert.That(metres, Is.InRange(1.55f, 1.95f), key + " character height above the ground point");
                }
            }
            // E is pre-mirrored offline without her left-only accessories, so it is never flipped at runtime (no satchel/tassel side swap)
            var east = sheet.Resolve("E", out bool mirrored, out float offset);
            Assert.AreEqual("E", east.key);
            Assert.IsFalse(mirrored);
            Assert.AreEqual(0f, offset);
            Assert.AreNotEqual(sheet.Get("W").run[0].uv, east.run[0].uv, "E uses its own atlas frames");
            Assert.IsNull(sheet.Resolve("NE", out _, out _), "diagonals have no art of their own; the presenter maps them explicitly");
        }

        [Test] public void AtlasFrames_DoNotOverlap()
        {
            var rects = Hero.directions.SelectMany(d => d.run.Concat(d.idle)).Select(f => new Rect(f.uv.x, f.uv.y, f.uv.z, f.uv.w)).ToList();
            Assert.AreEqual(4 * 22, rects.Count);
            for (int i = 0; i < rects.Count; i++)
                for (int j = i + 1; j < rects.Count; j++)
                {
                    Rect a = rects[i], b = rects[j];
                    bool overlap = a.xMin < b.xMax - 1e-5f && b.xMin < a.xMax - 1e-5f && a.yMin < b.yMax - 1e-5f && b.yMin < a.yMax - 1e-5f;
                    Assert.IsFalse(overlap, "frames " + i + " and " + j + " overlap");
                }
        }

        [Test] public void RunCycle_StrideMatchesTheUnchangedControllerSpeed()
        {
            EditorSceneManager.OpenScene(Phase52B3Builder.Valley);
            var player = Object.FindFirstObjectByType<PlayerController>();
            Assert.AreEqual(4.5f, player.MoveSpeed, 1e-4f, "the controller speed is unchanged");
            float stride = Hero.strideMeters;
            Assert.That(stride, Is.InRange(2.5f, 3.2f), "a jog stride for a 1.65 m character");
            float framesPerSecond = player.MoveSpeed / stride * 10f;
            Assert.That(framesPerSecond, Is.InRange(12f, 20f), "the run cycle plays at a readable sprite rate at full speed");
        }

        [Test] public void Atlas_TransparentTexelsCarryBledColour_SoFilteringLeavesNoDarkOrWhiteFringe()
        {
            string path = Phase52B3Builder.Folder + "/Sprites/CaretakerAtlas.png";
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.IsTrue(texture.LoadImage(File.ReadAllBytes(path)));
            Color32[] p = texture.GetPixels32();
            int w = texture.width, h = texture.height, edge = 0, black = 0;
            for (int y = 1; y < h - 1; y += 1)
                for (int x = 1; x < w - 1; x += 1)
                {
                    Color32 c = p[y * w + x];
                    if (c.a > 8) continue;
                    if (p[y * w + x + 1].a < 200 && p[y * w + x - 1].a < 200 && p[(y + 1) * w + x].a < 200 && p[(y - 1) * w + x].a < 200) continue;
                    edge++;
                    if (c.r + c.g + c.b < 6 || (c.r > 250 && c.g > 250 && c.b > 250)) black++;
                }
            Object.DestroyImmediate(texture);
            Assert.Greater(edge, 1000, "the atlas has sprite edges to check");
            Assert.Less(black / (float)edge, 0.01f, "transparent texels next to the sprite carry the sprite's colour");
        }

        [Test] public void FixedCamera_IsConstrained_AndTheLegacyOrbitStaysForTheEndingDrift()
        {
            EditorSceneManager.OpenScene(Phase52B3Builder.Valley);
            Camera camera = Camera.main;
            var follow = camera.GetComponent<FixedFollowCamera25D>();
            Assert.IsNotNull(follow);
            Assert.IsNotNull(camera.GetComponent<ThirdPersonCamera>(), "the orbit rig stays (ending drift, scripted shots)");
            Assert.AreSame(Object.FindFirstObjectByType<PlayerController>().transform, follow.Target);
            Assert.That(follow.Pitch, Is.InRange(25f, 45f));
            Assert.That(follow.MinDistance, Is.InRange(5f, 8f));
            Assert.That(follow.MaxDistance, Is.InRange(follow.MinDistance, 14f));
            Assert.That(follow.Distance, Is.InRange(follow.MinDistance, follow.MaxDistance));
        }

        [Test] public void Villagers_ShowIllustratedSprites_AndKeepTheirInteractionAndCollider()
        {
            EditorSceneManager.OpenScene(Phase52B3Builder.Valley);
            var visuals = Object.FindObjectsByType<NpcVisual2D5D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(3, visuals.Length);
            foreach (var v in visuals)
            {
                Assert.IsNotNull(v.Sheet);
                Assert.IsNotNull(v.GetComponentInParent<DialogueInteractable>(), v.name + " belongs to a villager who can still be spoken to");
                Assert.IsNotNull(v.transform.parent.GetComponent<CapsuleCollider>(), v.name + " villager keeps the collider");
                Assert.IsEmpty(v.GetComponentsInChildren<Collider>(true));
            }
        }

        [Test] public void DialogueBusts_AndSatchelPortrait_ShowTheSameOriginalCaretaker()
        {
            EditorSceneManager.OpenScene(Phase52B3Builder.Valley);
            var portraits = Object.FindFirstObjectByType<DialoguePortraits25D>();
            Assert.IsNotNull(portraits);
            var so = new SerializedObject(portraits);
            string bust = AssetDatabase.GetAssetPath(((Image)so.FindProperty("caretaker").objectReferenceValue).sprite);
            Assert.AreEqual(Phase52B3Builder.Folder + "/Portraits/Bust_Caretaker.png", bust);
            foreach (string villager in new[] { "mira", "wren", "alder" }) Assert.IsNotNull(so.FindProperty(villager).objectReferenceValue, villager + " bust");
            var pilot = Object.FindFirstObjectByType<Phase52Pilot>();
            Image satchel = pilot.Portrait.GetComponentsInChildren<Image>(true).First(i => i.name == "OriginalCaretakerPortrait");
            Assert.AreEqual(Phase52HeroBuilder.Folder + "/CaretakerPortrait.png", AssetDatabase.GetAssetPath(satchel.sprite), "the satchel shows the owner's 01 illustration");
        }

        [Test] public void Sprites_RecordTheUnmodifiedOwnerSuppliedSources()
        {
            var expected = new (string file, string sha)[]
            {
                ("01-heroine-portrait.png", "10e28cc332aa62a38872fc02449300bf1ca236b8ef77367d8a382be551f6a569"),
                ("02-npc-lineup.png", "77ef2bdfbbdf24a08e93f60f2cbcc3c7091ab967510943c4c5be3dabf1ce83d3"),
                ("05-heroine-turnaround-reference.png", "435d1465b9a5fd00053bc62fb76241428c08f61a235a95c7bfd55f24ab815041"),
            };
            // the atlases carry the hashes of the exact illustrations they were made from
            string atlases = File.ReadAllText(Phase52B3Builder.Folder + "/Sprites/CaretakerAtlas.json") + File.ReadAllText(Phase52B3Builder.Folder + "/Sprites/VillagersAtlas.json");
            foreach (var e in expected) StringAssert.Contains("\"" + e.file + "\": \"" + e.sha + "\"", atlases, e.file + " is recorded as a sprite source");
            using (var sha = SHA256.Create())
            {
                string Hash(string path) => string.Concat(sha.ComputeHash(File.ReadAllBytes(path)).Select(b => b.ToString("x2")));
                // the satchel shows 01 itself, unmodified
                Assert.AreEqual(expected[0].sha, Hash(Phase52HeroBuilder.Folder + "/CaretakerPortrait.png"));
                // in a full checkout the source files sit next to the pipeline; the isolated test copy holds only the Unity project
                string sources = Path.GetFullPath(Path.Combine(Application.dataPath, "../../tools/art/phase52b3/source"));
                if (Directory.Exists(sources))
                    foreach (var e in expected) Assert.AreEqual(e.sha, Hash(Path.Combine(sources, e.file)), e.file);
            }
        }

        [Test] public void OriginalAndB2Scenes_ContainNoB3Presentation()
        {
            foreach (string scene in new[] { AssetPaths.ValleyScene, Phase52PilotBuilder.Valley })
            {
                EditorSceneManager.OpenScene(scene);
                Assert.IsNull(Object.FindFirstObjectByType<Phase52B3Pilot>(FindObjectsInactive.Include), scene);
                Assert.IsNull(Object.FindFirstObjectByType<CharacterVisual2D5D>(FindObjectsInactive.Include), scene);
                Assert.IsNull(Object.FindFirstObjectByType<FixedFollowCamera25D>(FindObjectsInactive.Include), scene);
            }
        }
    }
}
