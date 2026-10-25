using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Echoes.Tests
{
    /// <summary>Gives every test its own empty save folder so tests never read or overwrite the developer's real save.</summary>
    public static class TestSaveFolder
    {
        static string current;

        public static string Begin()
        {
            End();
            current = Path.Combine(Path.GetTempPath(), "echoes-playmode", Guid.NewGuid().ToString("N"));
            SaveSystem.UseFolder(current);
            GameLaunch.Mode = LaunchMode.None;
            return current;
        }

        public static void End()
        {
            SaveSystem.ClearFolderOverride();
            GameLaunch.Mode = LaunchMode.None;
            try
            {
                if (current != null && Directory.Exists(current)) Directory.Delete(current, true);
            }
            catch (IOException)
            {
                // a handle can linger briefly; the folder is under the temp directory
            }
            current = null;
        }
    }

    /// <summary>Loads a fresh Valley before every test and always leaves time, cursor and save state clean afterwards.</summary>
    public abstract class ValleyPlayTestBase
    {
        protected PlayerController player;
        protected ThirdPersonCamera rig;
        protected InteractionDetector detector;
        protected ExplorationObjective objective;
        protected PauseController pause;
        protected DialogueBox dialogue;
        protected HudController hud;
        protected GameplayHud gameplayHud;
        protected CraftingPanel crafting;
        protected InventoryPanel inventoryPanel;
        protected ChickenInteractable hen;
        protected ResonanceView resonanceView;
        protected ValleyLighting lighting;
        protected GameSessionHost host;
        protected NpcInteractable npc;
        protected WorkshopInteractable workshop;
        protected SpringInteractable spring;
        protected HeartInteractable heart;

        protected GameSession Session => GameSession.Current;

        [UnitySetUp]
        public IEnumerator LoadValley()
        {
            TestSaveFolder.Begin();
            yield return SceneManager.LoadSceneAsync(SceneNames.Valley);
            yield return null;
            yield return null;
            FindSceneObjects();
        }

        [UnityTearDown]
        public IEnumerator RestoreGlobalState()
        {
            GameFlow.EnterMenuMode();
            TestSaveFolder.End();
            yield return null;
        }

        protected void FindSceneObjects()
        {
            player = Object.FindFirstObjectByType<PlayerController>();
            rig = Object.FindFirstObjectByType<ThirdPersonCamera>();
            detector = Object.FindFirstObjectByType<InteractionDetector>();
            objective = Object.FindFirstObjectByType<ExplorationObjective>();
            pause = Object.FindFirstObjectByType<PauseController>();
            dialogue = Object.FindFirstObjectByType<DialogueBox>();
            hud = Object.FindFirstObjectByType<HudController>();
            gameplayHud = Object.FindFirstObjectByType<GameplayHud>();
            crafting = Object.FindFirstObjectByType<CraftingPanel>();
            inventoryPanel = Object.FindFirstObjectByType<InventoryPanel>();
            hen = Object.FindFirstObjectByType<ChickenInteractable>();
            resonanceView = Object.FindFirstObjectByType<ResonanceView>();
            lighting = Object.FindFirstObjectByType<ValleyLighting>();
            host = Object.FindFirstObjectByType<GameSessionHost>();
            npc = Object.FindFirstObjectByType<NpcInteractable>();
            workshop = Object.FindFirstObjectByType<WorkshopInteractable>();
            spring = Object.FindFirstObjectByType<SpringInteractable>();
            heart = Object.FindFirstObjectByType<HeartInteractable>();
        }

        protected ResourceNode Node(string id) =>
            Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None).First(n => n.NodeId == id);

        /// <summary>Puts the player 1.5 m south of an interactable (well inside its range) and lets two frames refresh the prompt.</summary>
        protected IEnumerator StandNextTo(Interactable target)
        {
            player.Teleport(target.Position + new Vector3(0f, 0.6f, -1.5f));
            yield return null;
            yield return null;
        }

        /// <summary>Opens the target's dialogue through the detector and reads it to the end.</summary>
        protected void InteractAndReadAll(Interactable target)
        {
            Assert.IsTrue(detector.TryInteract(), "interaction with " + target.name + " should succeed in range");
            Assert.IsTrue(dialogue.IsOpen, target.name + " should open the dialogue box");
            while (dialogue.IsOpen) dialogue.Advance();
        }

        /// <summary>Walks up to a resource node and presses E through the detector, exactly as the player would.</summary>
        protected IEnumerator GatherFrom(string nodeId)
        {
            ResourceNode node = Node(nodeId);
            yield return StandNextTo(node);
            Assert.AreSame(node, detector.Current, nodeId + " should be the offered interaction");
            Assert.IsTrue(detector.TryInteract());
            yield return null;
        }

        /// <summary>The Phase 1 chain: Mira, the spring, then the Heart.</summary>
        protected IEnumerator CompleteExploration()
        {
            yield return StandNextTo(npc);
            InteractAndReadAll(npc);
            yield return StandNextTo(spring);
            InteractAndReadAll(spring);
            yield return StandNextTo(heart);
            InteractAndReadAll(heart);
            Assert.IsTrue(objective.IsComplete);
        }

        /// <summary>Opens the workshop panel by interacting with it, crafts recipe <paramref name="index"/> with its button, and closes it.</summary>
        protected IEnumerator CraftAtWorkshop(int index)
        {
            yield return StandNextTo(workshop);
            Assert.IsTrue(detector.TryInteract());
            yield return null;
            Assert.IsTrue(crafting.IsOpen);
            crafting.CraftButton(index).onClick.Invoke();
            crafting.Close();
            yield return null;
        }

        /// <summary>
        /// The whole first quest through real interactions: exploration, Mira's offer, gathering 2 herbs and a blossom, mixing the
        /// tonic at the workshop and pouring it into the spring. Nothing is seeded or set directly.
        /// </summary>
        protected IEnumerator RestoreTheStream()
        {
            yield return CompleteExploration();
            yield return StandNextTo(npc);
            InteractAndReadAll(npc);
            Assert.AreEqual(StreamQuestStage.Active, Session.StreamQuest);

            yield return GatherFrom("herb_1");
            yield return GatherFrom("herb_2");
            yield return GatherFrom("flower_1");
            yield return CraftAtWorkshop(0);
            Assert.AreEqual(1, Session.Inventory.Count(ItemIds.SpringTonic));

            yield return StandNextTo(spring);
            InteractAndReadAll(spring);
            Assert.AreEqual(StreamQuestStage.Completed, Session.StreamQuest);
        }

        /// <summary>Reloads the Valley the way the Continue button does.</summary>
        protected IEnumerator ContinueFromSave()
        {
            GameLaunch.Mode = LaunchMode.Continue;
            yield return SceneManager.LoadSceneAsync(SceneNames.Valley);
            yield return null;
            yield return null;
            FindSceneObjects();
        }
    }
}
