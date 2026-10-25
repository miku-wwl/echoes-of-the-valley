using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Echoes.Tests
{
    /// <summary>Runs the real Valley scene in Play Mode and drives it through the same public entry points the input uses.</summary>
    public class ValleyPlayTests : ValleyPlayTestBase
    {
        [UnityTest]
        public IEnumerator Player_SettlesOnTheTerrainAndStaysThere()
        {
            yield return new WaitForSeconds(1f);

            Terrain terrain = Terrain.activeTerrain;
            float ground = terrain.SampleHeight(player.transform.position) + terrain.transform.position.y;
            Assert.IsTrue(player.IsGrounded, "gravity should settle the player on the ground");
            Assert.AreEqual(ground, player.transform.position.y, 0.3f);
        }

        [UnityTest]
        public IEnumerator Player_WalksAlongTheCameraDirection_AtWalkingSpeed()
        {
            yield return new WaitForSeconds(0.5f);
            Vector3 forward = Vector3.ProjectOnPlane(player.CameraTransform.forward, Vector3.up).normalized;
            Vector3 start = player.transform.position;
            float startTime = Time.time;

            player.InputOverride = Vector2.up;
            yield return new WaitForSeconds(1f);
            player.InputOverride = null;

            Vector3 delta = player.transform.position - start;
            delta.y = 0f;
            float elapsed = Time.time - startTime;
            Assert.Greater(delta.magnitude, 2.5f, "should cover real ground in a second");
            Assert.LessOrEqual(delta.magnitude, player.MoveSpeed * elapsed + 0.1f, "never faster than the walking speed");
            Assert.Greater(Vector3.Dot(delta.normalized, forward), 0.95f, "forward input follows the camera direction");
        }

        [UnityTest]
        public IEnumerator Player_TurnsSmoothlyTowardTheMovementDirection()
        {
            yield return null;
            Vector3 right = Vector3.ProjectOnPlane(player.CameraTransform.right, Vector3.up).normalized;

            player.Simulate(Vector2.right, 0.016f);
            float afterOneStep = Vector3.Angle(player.transform.forward, right);
            Assert.Greater(afterOneStep, 45f, "a single step must not snap the facing");
            Assert.Less(afterOneStep, 90f, "but the character does start turning");

            for (int i = 0; i < 60; i++) player.Simulate(Vector2.right, 0.016f);
            Assert.Less(Vector3.Angle(player.transform.forward, right), 5f, "and soon faces the way it walks");
        }

        [UnityTest]
        public IEnumerator Player_IsStoppedByTheHeartObelisk()
        {
            Vector3 center = heart.transform.position;
            player.Teleport(center + new Vector3(0f, 0.8f, -4.2f));
            rig.Snap();
            yield return null;

            player.InputOverride = Vector2.up;
            yield return new WaitForSeconds(3f);
            player.InputOverride = null;

            Vector3 offset = player.transform.position - center;
            offset.y = 0f;
            Assert.Less(offset.magnitude, 1.8f, "the player should have walked up to the obelisk");
            Assert.Greater(offset.magnitude, 0.9f, "but must not pass through it");
        }

        [UnityTest]
        public IEnumerator Player_RespawnsWhenItFallsOutOfTheWorld()
        {
            player.Teleport(new Vector3(0f, -40f, 0f));
            yield return null;
            yield return null;

            Assert.Greater(player.transform.position.y, -5f);
            Assert.Less(Vector3.Distance(player.transform.position, player.RespawnPoint.position), 1f);
        }

        [UnityTest]
        public IEnumerator Camera_KeepsItsDistanceAndTheVerticalAngleInRange()
        {
            yield return null;
            Vector3 pivot = player.transform.position + new Vector3(0f, 1.5f, 0f);
            Assert.AreEqual(rig.Distance, Vector3.Distance(rig.transform.position, pivot), 0.35f, "open ground: full follow distance");

            for (int i = 0; i < 10; i++) rig.ApplyLook(new Vector2(0f, 100000f));
            Assert.AreEqual(rig.MinPitch, rig.Pitch, 0.001f, "cannot pitch below the minimum");
            for (int i = 0; i < 20; i++) rig.ApplyLook(new Vector2(0f, -100000f));
            Assert.AreEqual(rig.MaxPitch, rig.Pitch, 0.001f, "cannot pitch above the maximum");

            float yawBefore = rig.Yaw;
            rig.ApplyLook(new Vector2(100000f, 0f));
            Assert.LessOrEqual(Mathf.Abs(Mathf.DeltaAngle(yawBefore, rig.Yaw)), 10f, "one frame of mouse movement cannot spin the camera");
        }

        [UnityTest]
        public IEnumerator Camera_StaysAboveTheGround_OnHillsAndNearTheRim()
        {
            Terrain terrain = Terrain.activeTerrain;
            for (int i = 0; i < 5; i++) rig.ApplyLook(new Vector2(0f, 100000f));

            Vector2[] spots = { new Vector2(0f, -32f), new Vector2(0f, 0f), new Vector2(34f, -20f), new Vector2(-36f, 30f), new Vector2(44f, 36f), new Vector2(-44f, -40f) };
            foreach (Vector2 spot in spots)
            {
                float ground = terrain.SampleHeight(new Vector3(spot.x, 0f, spot.y)) + terrain.transform.position.y;
                player.Teleport(new Vector3(spot.x, ground + 0.1f, spot.y));
                rig.Snap();
                yield return null;

                Vector3 position = rig.transform.position;
                float cameraGround = terrain.SampleHeight(position) + terrain.transform.position.y;
                Assert.GreaterOrEqual(position.y, cameraGround + 0.02f, "camera dipped below the terrain near " + spot);
            }
        }

        [UnityTest]
        public IEnumerator Interaction_IsOfferedAndAllowedOnlyInRange()
        {
            detector.Refresh();
            Assert.IsNull(detector.Current, "nothing is in reach at the spawn point");
            Assert.IsFalse(detector.TryInteract());
            Assert.IsFalse(hud.IsPromptVisible);

            yield return StandNextTo(npc);
            Assert.AreSame(npc, detector.Current);
            Assert.IsTrue(hud.IsPromptVisible);
            StringAssert.Contains("Mira", hud.PromptText);

            player.Teleport(npc.Position + new Vector3(0f, 0.6f, -(npc.Range + 0.5f)));
            yield return null;
            yield return null;
            Assert.IsNull(detector.Current, "just outside the range the target is dropped");
            Assert.IsFalse(hud.IsPromptVisible, "and the prompt disappears");
            Assert.IsFalse(detector.TryInteract());
            Assert.IsFalse(dialogue.IsOpen);
        }

        [UnityTest]
        public IEnumerator Dialogue_FreezesMovementUntilItCloses()
        {
            yield return StandNextTo(npc);
            Assert.IsTrue(detector.TryInteract());
            Assert.IsTrue(dialogue.IsOpen);
            Assert.IsFalse(GameFlow.GameplayInputAllowed);

            yield return new WaitForSeconds(0.3f);
            Vector3 before = player.transform.position;
            player.InputOverride = Vector2.up;
            yield return new WaitForSeconds(0.5f);
            player.InputOverride = null;
            Vector3 moved = player.transform.position - before;
            moved.y = 0f;
            Assert.Less(moved.magnitude, 0.05f, "no walking while a dialogue is open");

            while (dialogue.IsOpen) dialogue.Advance();
            Assert.IsTrue(GameFlow.GameplayInputAllowed);
        }

        [UnityTest]
        public IEnumerator Workshop_ExplainsThatAlchemyComesLater_WithoutChangingTheObjective()
        {
            yield return StandNextTo(workshop);
            Assert.AreSame(workshop, detector.Current);
            Assert.IsTrue(detector.TryInteract());

            string all = dialogue.CurrentLine;
            dialogue.Advance();
            all += " " + dialogue.CurrentLine;
            StringAssert.Contains("future phase", all);
            dialogue.Close();
            Assert.AreEqual(ObjectiveStep.MeetVillager, objective.Step);
        }

        [UnityTest]
        public IEnumerator Objective_FollowsTheExpectedOrder_CompletesOnce_AndANewGameResetsIt()
        {
            Assert.AreEqual(ObjectiveStep.MeetVillager, objective.Step);

            // Visiting the later stops first must not skip ahead.
            yield return StandNextTo(spring);
            InteractAndReadAll(spring);
            Assert.AreEqual(ObjectiveStep.MeetVillager, objective.Step, "spring before the villager changes nothing");
            yield return StandNextTo(heart);
            InteractAndReadAll(heart);
            Assert.AreEqual(ObjectiveStep.MeetVillager, objective.Step, "heart before the villager changes nothing");

            yield return StandNextTo(npc);
            InteractAndReadAll(npc);
            Assert.AreEqual(ObjectiveStep.InspectSpring, objective.Step);
            yield return StandNextTo(heart);
            InteractAndReadAll(heart);
            Assert.AreEqual(ObjectiveStep.InspectSpring, objective.Step, "heart before the spring changes nothing");

            yield return StandNextTo(spring);
            InteractAndReadAll(spring);
            Assert.AreEqual(ObjectiveStep.ReturnToHeart, objective.Step);
            Assert.IsFalse(objective.Feedback.HasPlayed);

            yield return StandNextTo(heart);
            InteractAndReadAll(heart);
            Assert.IsTrue(objective.IsComplete);
            Assert.AreEqual(1, objective.CompletionCount);
            Assert.IsTrue(objective.Feedback.HasPlayed);
            Assert.IsTrue(hud.IsBannerVisible);
            StringAssert.Contains("Complete", hud.BannerTitle);

            // Repeating interactions never completes (or rewards) a second time.
            yield return StandNextTo(heart);
            InteractAndReadAll(heart);
            yield return StandNextTo(spring);
            InteractAndReadAll(spring);
            Assert.AreEqual(1, objective.CompletionCount);

            // The world answers: warmer, brighter sun and a glowing crystal.
            float sunBefore = objective.Feedback.Sun.intensity;
            yield return new WaitForSeconds(4.6f);
            Assert.Greater(objective.Feedback.Sun.intensity, 1.25f);
            Assert.Greater(objective.Feedback.Sun.color.r, objective.Feedback.Sun.color.b, "warm light");
            Color glow = objective.Feedback.Crystal.sharedMaterial != null ? objective.Feedback.Crystal.material.GetColor("_EmissionColor") : Color.black;
            Assert.Greater(glow.g + glow.b, 1f, "crystal glows");
            Assert.IsTrue(objective.Feedback.Sparkles.isPlaying, "sparkles are emitting");
            Assert.Less(sunBefore, 1.35f);

            // Back to the menu and into a fresh Valley: the objective starts over.
            yield return SceneManager.LoadSceneAsync(SceneNames.MainMenu);
            yield return null;
            yield return SceneManager.LoadSceneAsync(SceneNames.Valley);
            yield return null;
            yield return null;
            FindSceneObjects();
            Assert.AreEqual(ObjectiveStep.MeetVillager, objective.Step);
            Assert.AreEqual(0, objective.CompletionCount);
            Assert.IsFalse(objective.Feedback.HasPlayed);
            Assert.Less(objective.Feedback.Sun.intensity, 1.1f, "the valley is muted again");
        }

        [UnityTest]
        public IEnumerator Pause_FreezesTimeAndMovement_AndResumeRestoresThem()
        {
            Assert.AreEqual(1f, Time.timeScale);

            pause.SetPaused(true);
            Assert.AreEqual(0f, Time.timeScale);
            Assert.IsTrue(pause.IsPaused);
            Assert.IsTrue(pause.Panel.activeSelf);
            Assert.IsTrue(Cursor.visible, "cursor is shown while paused");

            Vector3 before = player.transform.position;
            player.InputOverride = Vector2.up;
            yield return new WaitForSecondsRealtime(0.3f);
            player.InputOverride = null;
            Assert.AreEqual(0f, Vector3.Distance(before, player.transform.position), 0.001f, "nothing moves while paused");

            pause.SetPaused(false);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsFalse(pause.IsPaused);
            Assert.IsFalse(pause.Panel.activeSelf);
            Assert.IsFalse(Cursor.visible, "cursor is hidden again in gameplay");
        }

        [UnityTest]
        public IEnumerator Pause_ResumeButtonAndMainMenuButtonWork_AndTimeIsRunningInTheMenu()
        {
            pause.SetPaused(true);
            pause.ResumeButton.onClick.Invoke();
            Assert.IsFalse(pause.IsPaused);
            Assert.AreEqual(1f, Time.timeScale);

            pause.SetPaused(true);
            pause.MainMenuButton.onClick.Invoke();
            yield return null;
            yield return null;
            Assert.AreEqual(SceneNames.MainMenu, SceneManager.GetActiveScene().name);
            Assert.AreEqual(1f, Time.timeScale, "returning from a paused game restores the clock");
            Assert.IsNotNull(Object.FindFirstObjectByType<MainMenuController>());
            Assert.IsTrue(Cursor.visible);
        }
    }
}
