using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

namespace Echoes.Tests
{
    /// <summary>Phase 5.2-B3: the 2.5D presenter and the fixed camera on the real player, controller and interaction systems.</summary>
    public class Phase52B3PlayTests : ValleyPlayTestBase
    {
#if UNITY_EDITOR
        const string Sheet = "Assets/Echoes/Art/Phase52B3/CaretakerSheet.asset";
        const string Material = "Assets/Echoes/Art/Phase52B3/CaretakerSprite.mat";

        CharacterVisual2D5D AddPresenter()
        {
            player.Animator.gameObject.SetActive(false);
            var visual = new GameObject("CharacterVisual2D");
            visual.transform.SetParent(player.transform, false);
            var card = new GameObject("DirectionalSprite", typeof(MeshFilter), typeof(MeshRenderer));
            card.transform.SetParent(visual.transform, false);
            card.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Material);
            var quad = card.AddComponent<Sprite25DQuad>();
            visual.SetActive(false);
            var presenter = visual.AddComponent<CharacterVisual2D5D>();
            Set(presenter, "sheet", AssetDatabase.LoadAssetAtPath<SpriteSheet25D>(Sheet));
            Set(presenter, "player", player);
            Set(presenter, "card", quad);
            visual.SetActive(true);
            return presenter;
        }

        FixedFollowCamera25D AddCamera()
        {
            var follow = rig.gameObject.AddComponent<FixedFollowCamera25D>();
            Set(follow, "target", player.transform);
            Set(follow, "legacyRig", rig);
            follow.Snap();
            return follow;
        }

        static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [UnityTest] public IEnumerator Walking_AdvancesTheRunCycleByDistanceOverStride_AndStoppingReturnsToIdle()
        {
            var presenter = AddPresenter();
            AddCamera();
            yield return null;
            yield return null;
            Assert.IsFalse(presenter.Running, "standing still shows the idle loop");
            float stride = presenter.Sheet.strideMeters;
            float phase0 = presenter.RunPhase, travelled = 0f;
            Vector3 last = player.transform.position;
            player.InputOverride = Vector2.up;
            float until = Time.time + 0.9f;
            while (Time.time < until)
            {
                yield return null;
                travelled += Vector3.ProjectOnPlane(player.transform.position - last, Vector3.up).magnitude;
                last = player.transform.position;
            }
            Assert.IsTrue(presenter.Running, "moving shows the run cycle");
            Assert.Greater(travelled, 2f);
            float expected = Mathf.Repeat(phase0 + travelled / stride, 1f);
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(presenter.RunPhase * 360f, expected * 360f)), Is.LessThan(20f), "the cycle advances by distance / stride (feet do not skate)");
            player.InputOverride = Vector2.zero;
            yield return new WaitForSeconds(0.45f);
            Assert.IsFalse(presenter.Running, "stopping returns to idle");
            player.InputOverride = null;
        }

        [UnityTest] public IEnumerator Direction_FollowsTheFacingRelativeToTheCamera_WithTheDiagonalsOnTheSideViews()
        {
            var presenter = AddPresenter();
            var follow = AddCamera();
            yield return null;
            Camera view = rig.GetComponent<Camera>();
            float cy = follow.Yaw;
            // every diagonal shows a side view whatever the previous view was; E has its own (pre-mirrored) frames, so nothing flips at runtime
            var cases = new (float yaw, string dir, bool mirrored)[] { (cy, "N", false), (cy + 180f, "S", false), (cy + 90f, "E", false), (cy - 90f, "W", false), (cy + 45f, "E", false), (cy - 135f, "W", false), (cy + 20f, "N", false),
                (cy + 135f, "E", false), (cy - 45f, "W", false), (cy + 180f, "S", false), (cy + 135f, "E", false), (cy, "N", false), (cy - 45f, "W", false) };
            foreach (var c in cases)
            {
                player.transform.rotation = Quaternion.Euler(0f, c.yaw, 0f);
                for (int i = 0; i < 12; i++) presenter.Step(1f / 60f, view);   // past the half-turn hold
                Assert.AreEqual(c.dir, presenter.Direction, "facing " + (c.yaw - cy) + " degrees from the camera");
                Assert.AreEqual(c.mirrored, presenter.Mirrored, "facing " + (c.yaw - cy));
            }
        }

        [UnityTest] public IEnumerator Teleport_DoesNotAdvanceTheRunCycle()
        {
            var presenter = AddPresenter();
            yield return null;
            yield return null;
            float phase = presenter.RunPhase;
            player.Teleport(player.transform.position + Vector3.right * 6f);
            yield return null;
            yield return null;
            Assert.AreEqual(phase, presenter.RunPhase, 1e-4f);
            Assert.IsFalse(presenter.Running);
        }

        [UnityTest] public IEnumerator FixedCamera_FollowsWithAConstantYaw_IgnoresTheOrbit_AndYieldsToScriptedShots()
        {
            AddPresenter();
            var follow = AddCamera();
            Transform cam = rig.transform;
            yield return null;
            Quaternion rotation = cam.rotation;
            rig.ApplyLook(new Vector2(400f, 0f));   // what a mouse sweep does to the orbit rig
            player.InputOverride = new Vector2(1f, 0f);
            yield return new WaitForSeconds(0.8f);
            player.InputOverride = null;
            yield return new WaitForSeconds(0.6f);
            Assert.Less(Quaternion.Angle(rotation, cam.rotation), 0.5f, "no free orbit: the view direction never changes in play");
            float d = Vector3.Distance(cam.position, player.transform.position + Vector3.up * 0.9f);
            Assert.That(d, Is.InRange(follow.MinDistance - 0.3f, follow.MaxDistance + 0.6f), "the camera keeps its follow distance");
            rig.enabled = false;
            cam.SetPositionAndRotation(new Vector3(0f, 50f, 0f), Quaternion.Euler(90f, 0f, 0f));
            yield return null;
            Assert.AreEqual(50f, cam.position.y, 1e-3f, "a scripted shot owns the camera while the orbit rig is off");
            rig.enabled = true;
        }

        [UnityTest] public IEnumerator Interaction_StillWorks_WithTheSpritePresentation()
        {
            AddPresenter();
            AddCamera();
            yield return StandNextTo(npc);
            Assert.AreSame(npc, detector.Current, "Mira is offered");
            InteractAndReadAll(npc);
            yield return GatherFrom("herb_1");
            Assert.AreEqual(1, Session.Inventory.Count(ItemIds.Herb));
        }

        [UnityTest] public IEnumerator DialoguePortraits_ShowTheCaretakerAndTheVillagerWhoSpeaks()
        {
            var root = new GameObject("BustTest").AddComponent<DialoguePortraits25D>();
            var left = new GameObject("Left").AddComponent<Image>();
            var right = new GameObject("Right").AddComponent<Image>();
            left.transform.SetParent(root.transform);
            right.transform.SetParent(root.transform);
            var mira = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
            Set(root, "dialogue", dialogue);
            Set(root, "caretaker", left);
            Set(root, "speaker", right);
            Set(root, "mira", mira);
            left.gameObject.SetActive(false);
            right.gameObject.SetActive(false);
            dialogue.Open("Mira", new[] { "Hello." });
            yield return null;
            Assert.IsTrue(root.CaretakerShown);
            Assert.AreSame(mira, root.SpeakerShown);
            dialogue.Close();
            dialogue.Open("The Ruined Spring", new[] { "Water." });
            yield return null;
            Assert.IsTrue(root.CaretakerShown);
            Assert.IsNull(root.SpeakerShown, "places show only the caretaker");
            dialogue.Close();
            yield return null;
            Assert.IsFalse(root.CaretakerShown);
        }

        [UnityTest] public IEnumerator B3PilotScene_ShowsOnlyTheSprite_TalksAndSaves_AndContinueRestoresTheQuest()
        {
            TestSaveFolder.Begin();
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Echoes/Art/Phase52B3/Scenes/Valley.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            FindSceneObjects();
            var b3 = Object.FindFirstObjectByType<Phase52B3Pilot>();
            Assert.IsNotNull(b3);
            Assert.IsTrue(b3.CharacterVisual.activeInHierarchy, "the illustrated caretaker is shown");
            var pilot = b3.GetComponent<Phase52Pilot>();
            Assert.IsFalse(pilot.SkinnedVisual.activeInHierarchy, "the B2 hero is hidden");
            Assert.IsFalse(pilot.OriginalVisual.activeInHierarchy, "the original hero is hidden");
            Vector3 start = player.transform.position;
            player.InputOverride = Vector2.up;
            yield return new WaitForSeconds(0.6f);
            player.InputOverride = null;
            Assert.Greater(Vector3.Distance(start, player.transform.position), 1f, "the player walks in the B3 scene");
            Assert.AreEqual(ObjectiveStep.MeetVillager, objective.Step, "a new game starts by meeting Mira");
            yield return StandNextTo(npc);
            InteractAndReadAll(npc);
            ObjectiveStep step = objective.Step;
            Assert.AreNotEqual(ObjectiveStep.MeetVillager, step, "talking to Mira in the B3 scene advances the quest");
            Assert.IsTrue(host.SaveNow(), "the B3 scene saves like the game");
            GameLaunch.Mode = LaunchMode.Continue;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Echoes/Art/Phase52B3/Scenes/Valley.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            FindSceneObjects();
            Assert.AreEqual(step, objective.Step, "Continue restores the same objective step in the B3 scene");
        }
#endif
    }
}
