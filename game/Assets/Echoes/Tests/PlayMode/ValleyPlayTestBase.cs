using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Echoes.Tests
{
    /// <summary>Loads a fresh Valley before every test and always leaves time and cursor state clean afterwards.</summary>
    public abstract class ValleyPlayTestBase
    {
        protected PlayerController player;
        protected ThirdPersonCamera rig;
        protected InteractionDetector detector;
        protected ExplorationObjective objective;
        protected PauseController pause;
        protected DialogueBox dialogue;
        protected HudController hud;
        protected NpcInteractable npc;
        protected WorkshopInteractable workshop;
        protected SpringInteractable spring;
        protected HeartInteractable heart;

        [UnitySetUp]
        public IEnumerator LoadValley()
        {
            yield return SceneManager.LoadSceneAsync(SceneNames.Valley);
            yield return null;
            yield return null;
            FindSceneObjects();
        }

        [UnityTearDown]
        public IEnumerator RestoreGlobalState()
        {
            GameFlow.EnterMenuMode();
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
            npc = Object.FindFirstObjectByType<NpcInteractable>();
            workshop = Object.FindFirstObjectByType<WorkshopInteractable>();
            spring = Object.FindFirstObjectByType<SpringInteractable>();
            heart = Object.FindFirstObjectByType<HeartInteractable>();
        }

        /// <summary>Puts the player 1.5 m south of an interactable (well inside its range) and lets one frame refresh the prompt.</summary>
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
    }
}
