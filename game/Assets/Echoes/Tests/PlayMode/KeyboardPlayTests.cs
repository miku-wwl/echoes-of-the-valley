using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace Echoes.Tests
{
    /// <summary>Drives a virtual keyboard through the Input System so E, Esc and WASD are exercised end to end.</summary>
    public class KeyboardPlayTests : ValleyPlayTestBase
    {
        Keyboard keyboard;
        InputSettings.BackgroundBehavior backgroundBehavior;
        InputSettings.EditorInputBehaviorInPlayMode editorBehavior;

        [UnitySetUp]
        public IEnumerator AddVirtualKeyboard()
        {
            backgroundBehavior = InputSystem.settings.backgroundBehavior;
            editorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard = InputSystem.AddDevice<Keyboard>("EchoesTestKeyboard");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator RemoveVirtualKeyboard()
        {
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior = backgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode = editorBehavior;
            yield return null;
        }

        /// <summary>Holds a key for exactly one frame, then releases it.</summary>
        IEnumerator Tap(Key key)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
        }

        [UnityTest]
        public IEnumerator Escape_PausesAndResumes()
        {
            yield return Tap(Key.Escape);
            Assert.IsTrue(pause.IsPaused, "Esc pauses");
            Assert.AreEqual(0f, Time.timeScale);

            yield return Tap(Key.Escape);
            Assert.IsFalse(pause.IsPaused, "Esc resumes");
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator HoldingW_WalksTheCharacterForward()
        {
            yield return new WaitForSeconds(0.5f);
            Vector3 start = player.transform.position;

            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            yield return new WaitForSeconds(1f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;

            Vector3 moved = player.transform.position - start;
            moved.y = 0f;
            Assert.Greater(moved.magnitude, 2.5f, "W should move the player");
        }

        [UnityTest]
        public IEnumerator E_OpensDialogue_AdvancesOneLinePerPress_AndNeverReopensOnTheClosingPress()
        {
            yield return StandNextTo(npc);

            yield return Tap(Key.E);
            Assert.IsTrue(dialogue.IsOpen, "E opens the villager's dialogue");
            Assert.AreEqual(0, dialogue.LineIndex);

            yield return Tap(Key.E);
            Assert.AreEqual(1, dialogue.LineIndex, "one press advances exactly one line");

            yield return Tap(Key.E);
            Assert.AreEqual(2, dialogue.LineIndex);

            yield return Tap(Key.E);
            Assert.IsFalse(dialogue.IsOpen, "the last press closes it...");
            Assert.AreEqual(ObjectiveStep.InspectSpring, objective.Step);

            yield return null;
            Assert.IsFalse(dialogue.IsOpen, "...and does not reopen it");
        }

        [UnityTest]
        public IEnumerator I_TogglesTheSatchel_AndEscClosesItWithoutPausing()
        {
            yield return Tap(Key.I);
            Assert.IsTrue(inventoryPanel.IsOpen, "I opens the satchel");
            Assert.IsTrue(GameFlow.IsPanelOpen);

            yield return Tap(Key.Escape);
            Assert.IsFalse(inventoryPanel.IsOpen, "Esc closes the open panel");
            Assert.IsFalse(pause.IsPaused, "...and that same press must not also pause");
            Assert.AreEqual(1f, Time.timeScale);

            yield return Tap(Key.I);
            Assert.IsTrue(inventoryPanel.IsOpen);
            yield return Tap(Key.I);
            Assert.IsFalse(inventoryPanel.IsOpen, "I closes it again");

            yield return Tap(Key.Escape);
            Assert.IsTrue(pause.IsPaused, "with no panel open Esc pauses as before");
        }

        [UnityTest]
        public IEnumerator PressingE_GathersFromBushes_AndKeyOneCraftsTheTonicAtTheWorkshop()
        {
            yield return StandNextTo(Node("herb_1"));
            yield return Tap(Key.E);
            yield return StandNextTo(Node("herb_2"));
            yield return Tap(Key.E);
            yield return StandNextTo(Node("flower_1"));
            yield return Tap(Key.E);
            Assert.AreEqual(2, Session.Inventory.Count(ItemIds.Herb));
            Assert.AreEqual(1, Session.Inventory.Count(ItemIds.Flower));

            yield return StandNextTo(workshop);
            yield return Tap(Key.E);
            Assert.IsTrue(crafting.IsOpen, "E opens the workshop panel");

            yield return Tap(Key.Digit1);
            Assert.AreEqual(1, Session.Inventory.Count(ItemIds.SpringTonic), "key 1 crafts the first recipe");
            Assert.AreEqual(0, Session.Inventory.Count(ItemIds.Herb));

            yield return Tap(Key.Digit1);
            Assert.AreEqual(1, Session.Inventory.Count(ItemIds.SpringTonic), "a second press has nothing left to spend");

            yield return Tap(Key.Escape);
            Assert.IsFalse(crafting.IsOpen);
            Assert.IsFalse(pause.IsPaused);
        }
    }
}
