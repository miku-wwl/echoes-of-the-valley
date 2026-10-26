using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Echoes.Tests
{
    /// <summary>The generated Valley and MainMenu contain every Phase 3 object, wired, in their silent starting state, and the text fits its boxes.</summary>
    public class Phase3SceneTests
    {
        const string MainMenuPath = "Assets/Echoes/Scenes/MainMenu.unity";
        const string ValleyPath = "Assets/Echoes/Scenes/Valley.unity";

        static T First<T>() where T : Object => Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);

        static void OpenValley() => EditorSceneManager.OpenScene(ValleyPath, OpenSceneMode.Single);

        static void AssertWired(Object target, params string[] fields)
        {
            Assert.IsNotNull(target, "component missing");
            SerializedObject serialized = new SerializedObject(target);
            foreach (string field in fields)
            {
                SerializedProperty property = serialized.FindProperty(field);
                Assert.IsNotNull(property, target.GetType().Name + " has no field " + field);
                if (property.isArray)
                {
                    Assert.Greater(property.arraySize, 0, target.GetType().Name + "." + field + " is empty");
                    for (int i = 0; i < property.arraySize; i++)
                        Assert.IsNotNull(property.GetArrayElementAtIndex(i).objectReferenceValue, target.GetType().Name + "." + field + "[" + i + "]");
                }
                else
                {
                    Assert.IsNotNull(property.objectReferenceValue, target.GetType().Name + "." + field);
                }
            }
        }

        static Text FindText(string parent, string name) =>
            Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(t => t.name == name && t.transform.parent.name == parent);

        static float HeightOf(Text label, string content)
        {
            string previous = label.text;
            label.text = content;
            float height = label.preferredHeight;
            label.text = previous;
            return height;
        }

        // ---- Characters and places ---------------------------------------------------------------------------------------------

        [Test]
        public void Valley_HasExactlyThreeVillagers_OnTheSharedInteractionFramework()
        {
            OpenValley();
            NpcInteractable mira = First<NpcInteractable>();
            GardenKeeperInteractable wren = First<GardenKeeperInteractable>();
            WindmillRestorerInteractable alder = First<WindmillRestorerInteractable>();
            Assert.AreEqual(1, Object.FindObjectsByType<NpcInteractable>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(1, Object.FindObjectsByType<GardenKeeperInteractable>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(1, Object.FindObjectsByType<WindmillRestorerInteractable>(FindObjectsSortMode.None).Length);

            DialogueInteractable[] villagers = { mira, wren, alder };
            string[] names = { "Mira", "Wren", "Alder" };
            for (int i = 0; i < villagers.Length; i++)
            {
                DialogueInteractable villager = villagers[i];
                Assert.AreEqual(names[i], villager.name);
                Assert.IsTrue(villager.IsWired, villager.name + " needs dialogue and objective");
                Assert.Greater(villager.Range, 1f, villager.name);
                Assert.IsFalse(string.IsNullOrEmpty(villager.Prompt), villager.name);
                Assert.IsNotNull(villager.GetComponent<CapsuleCollider>(), villager.name + " blocks the player like the others");
                AssertWired(villager.GetComponent<NpcIdle>(), "animator");
                for (int j = i + 1; j < villagers.Length; j++)
                    Assert.Greater(Vector3.Distance(villager.transform.position, villagers[j].transform.position), 8f, villager.name + " vs " + villagers[j].name);
            }
            StringAssert.Contains("Wren", wren.Prompt);
            StringAssert.Contains("Alder", alder.Prompt);
            AssertWired(mira, "endingPanel");
        }

        [Test]
        public void ResonanceView_IsWiredForAllFourStages()
        {
            OpenValley();
            ResonanceView view = First<ResonanceView>();
            AssertWired(view, "lighting", "troubledStream", "clearStream", "pond", "springWater", "murkyWater", "clearWater", "bloomRoot", "pulse", "fountain",
                "gardenDormant", "gardenBloomRoot", "yardLifeRoot", "gardenMotes", "gardenPulse", "villagers",
                "windmill", "windmillDamaged", "windmillRepaired", "villageLifeRoot", "valleyMotes", "finalPulse");
            Assert.AreEqual(3, new SerializedObject(view).FindProperty("villagers").arraySize, "Mira, Wren and Alder");
        }

        [Test]
        public void Valley_StartsInTheSilentState_ForEveryStage()
        {
            OpenValley();
            ResonanceView view = First<ResonanceView>();
            Assert.IsTrue(view.TroubledStreamShown);
            Assert.IsFalse(view.ClearStreamShown);
            Assert.IsFalse(view.BloomShown);
            Assert.IsTrue(view.GardenDormantShown, "the garden starts withered");
            Assert.IsFalse(view.GardenBloomShown);
            Assert.IsFalse(view.YardLifeShown);
            Assert.IsTrue(view.WindmillDamagedShown, "the windmill starts broken");
            Assert.IsFalse(view.WindmillRepairedShown);
            Assert.IsFalse(view.VillageLifeShown);
            Assert.IsFalse(view.Windmill.Spinning);
            Assert.AreEqual(0f, view.Windmill.CurrentSpeed);
            Assert.AreEqual(ResonanceStage.SilentValley, view.ShownStage);
        }

        [Test]
        public void Windmill_HasARotor_BothSailSets_AndASolidTower()
        {
            OpenValley();
            WindmillRotator[] rotors = Object.FindObjectsByType<WindmillRotator>(FindObjectsSortMode.None);
            Assert.AreEqual(1, rotors.Length);
            Transform sails = rotors[0].Sails;
            Assert.IsNotNull(sails);
            Assert.IsTrue(sails.IsChildOf(rotors[0].transform));
            Assert.IsNotNull(sails.Find("IntactSails"), "whole sails");
            Assert.IsNotNull(sails.Find("BrokenSails"), "broken sails");
            Assert.AreEqual(4, sails.Find("IntactSails").childCount, "four arms");
            Assert.IsNotNull(rotors[0].transform.Find("Braces"), "the mended supports");
            Assert.IsNotNull(rotors[0].transform.Find("SupportsBroken"), "the broken supports");
            Assert.IsNotNull(rotors[0].transform.Find("TowerLower").GetComponent<MeshCollider>(), "the player cannot walk through the tower");
            Assert.IsFalse(rotors[0].gameObject.isStatic, "it is animated, so it must not be statically batched");
            Assert.IsFalse(sails.gameObject.isStatic);
        }

        [Test]
        public void TheGarden_HasItsBeds_AGate_AndBothStatesUnderOneRoot()
        {
            OpenValley();
            Transform garden = GameObject.Find("Garden").transform;
            Assert.IsNotNull(garden.Find("Dormant"));
            Assert.IsNotNull(garden.Find("Bloom"));
            Assert.IsNotNull(garden.Find("GateArch"));
            Assert.IsNotNull(garden.Find("Scarecrow"));
            for (int i = 0; i < 4; i++) Assert.IsNotNull(garden.Find("BedFrame" + i), "bed " + i);
            Assert.Greater(garden.Find("Bloom").childCount, 40, "plenty of flowers and bushes to pop up");
            Assert.IsFalse(garden.gameObject.isStatic);
            Assert.IsFalse(garden.Find("Bloom").gameObject.isStatic, "popped up one by one, so it is not static");
        }

        [Test]
        public void NewPlaces_StayClearOfResourceNodes_AndOfEachOther()
        {
            OpenValley();
            Vector3 garden = GameObject.Find("Garden").transform.position;
            Vector3 windmill = GameObject.Find("Windmill").transform.position;
            Vector3 henYard = GameObject.Find("HenYardFence").transform.position;

            foreach (ResourceNode node in Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None))
            {
                Assert.Greater(Flat(node.transform.position, garden), 6.5f, node.name + " is clear of the garden fence");
                Assert.Greater(Flat(node.transform.position, windmill), 8f, node.name + " is clear of the windmill");
            }
            Assert.Greater(Flat(garden, henYard), 9f, "garden and hen yard do not overlap");
            Assert.Greater(Flat(garden, windmill), 30f, "the two places are far apart, so the quests send you across the valley");
            Assert.Greater(Flat(windmill, Vector3.zero), 20f, "the windmill is not on the plaza");
        }

        static float Flat(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

        [Test]
        public void TheGround_IsFlatUnderTheGardenAndTheWindmill()
        {
            OpenValley();
            Terrain terrain = Terrain.activeTerrain;
            foreach (string place in new[] { "Garden", "Windmill" })
            {
                Vector3 center = GameObject.Find(place).transform.position;
                float radius = place == "Garden" ? 4.5f : 3.4f;
                float low = float.MaxValue;
                float high = float.MinValue;
                for (int i = 0; i < 16; i++)
                {
                    float angle = i * Mathf.PI / 8f;
                    float height = terrain.SampleHeight(center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius) + terrain.transform.position.y;
                    low = Mathf.Min(low, height);
                    high = Mathf.Max(high, height);
                }
                Assert.Less(high - low, 0.4f, place + " stands on level ground");
            }
        }

        [Test]
        public void TheNewPrefabs_ExistAndAreNotDuplicatedByTheGenerator()
        {
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Echoes/Prefabs/GardenKeeper.prefab"));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Echoes/Prefabs/WindmillRestorer.prefab"));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Echoes/Prefabs/Villager.prefab"));
        }

        // ---- The ending screen and the menu question ---------------------------------------------------------------------------------

        [Test]
        public void EndingScreen_IsWired_StartsHidden_AndOffersBothWaysOut()
        {
            OpenValley();
            EndingPanel ending = First<EndingPanel>();
            AssertWired(ending, "panel", "continueButton", "mainMenuButton", "orbitCamera");
            Assert.IsFalse(ending.Panel.activeSelf, "hidden until the closing conversation");
            Assert.IsNotNull(ending.ContinueButton);
            Assert.IsNotNull(ending.MainMenuButton);
            Assert.AreEqual("Continue Exploring", ending.ContinueButton.GetComponentInChildren<Text>().text);
            Assert.AreEqual("Main Menu", ending.MainMenuButton.GetComponentInChildren<Text>().text);

            string all = string.Join("\n", ending.Panel.GetComponentsInChildren<Text>(true).Select(t => t.text));
            StringAssert.Contains("Echoes of the Valley", all, "the game title");
            StringAssert.Contains("The valley has found its voice again.", all);
            StringAssert.Contains("Silence", all);
            StringAssert.Contains("Rest", all);
            StringAssert.Contains("thatgamejam #02", all, "credits");
            StringAssert.Contains("AI assistance", all, "the AI disclosure is part of the credits");
            StringAssert.Contains("Esc", all, "the controls are on the screen");
            Assert.AreEqual(1, Object.FindObjectsByType<EndingPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length);
        }

        [Test]
        public void MainMenu_HasTheNewGameQuestion_WiredAndHidden()
        {
            EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);
            MainMenuController menu = First<MainMenuController>();
            AssertWired(menu, "continueButton", "playButton", "quitButton", "confirmPanel", "confirmText", "confirmButton", "cancelButton");
            Assert.IsFalse(menu.ConfirmationOpen);
            Assert.IsNotNull(menu.ConfirmButton);
            Assert.IsNotNull(menu.CancelButton);
            Assert.AreEqual("Start New Game", menu.ConfirmButton.GetComponentInChildren<Text>().text);
            Assert.AreEqual("Cancel", menu.CancelButton.GetComponentInChildren<Text>().text);
        }

        // ---- Text must fit its boxes (labels truncate silently) ---------------------------------------------------------------------

        [Test]
        public void EveryObjectiveTheGameCanShow_FitsTheObjectivePanel_AndNamesTheNextStep()
        {
            OpenValley();
            Text heading = FindText("ObjectivePanel", "Heading");
            Text body = FindText("ObjectivePanel", "Body");

            List<QuestLine> lines = new List<QuestLine>();
            GameSession s = new GameSession();
            s.SetExplorationStep(GameSession.ExplorationComplete);
            void Note() => lines.Add(QuestLog.Current(s));

            Note();
            s.TryStartStreamQuest(); Note();
            s.Inventory.TryAdd(ItemIds.Herb, 1); Note();
            s.Inventory.TryAdd(ItemIds.Herb, 1); Note();
            s.Inventory.TryAdd(ItemIds.Flower, 1); Note();
            s.TryCraft(RecipeBook.SpringTonic); Note();
            s.TryDeliverSpringTonic(); Note();
            s.TryStartGardenQuest(); Note();
            s.Inventory.TryAdd(ItemIds.Flower, 1); Note();
            s.TryCraft(RecipeBook.ChickenFeed); Note();
            s.TryFeedChicken(); Note();
            s.Tick(GameSession.ChickenProductionSeconds); Note();
            s.TryCollectEgg(); Note();
            s.TryDeliverEgg(); Note();
            s.TryStartWindmillQuest(); Note();
            for (int i = 0; i < QuestLog.BracesOre; i++) { s.Inventory.TryAdd(ItemIds.Ore, 1); Note(); }
            s.TryCraft(RecipeBook.WindmillBraces); Note();
            s.TryDeliverWindmillBraces(); Note();
            s.TryCompleteEnding(); Note();

            string[] order =
            {
                QuestLog.StreamHeading, QuestLog.GardenHeading, QuestLog.WindmillHeading, QuestLog.ReturnHeading, QuestLog.EndedHeading
            };
            int previous = 0;
            foreach (QuestLine line in lines)
            {
                int index = System.Array.IndexOf(order, line.Heading);
                Assert.GreaterOrEqual(index, 0, "unknown heading " + line.Heading);
                Assert.GreaterOrEqual(index, previous, "the objective never goes back: " + line.Heading);
                previous = index;

                Assert.IsFalse(string.IsNullOrEmpty(line.Body));
                float bodyHeight = HeightOf(body, line.Body);
                Assert.Greater(bodyHeight, 0f, "the font measured something: " + line.Body);
                Assert.LessOrEqual(bodyHeight, body.rectTransform.rect.height + 0.5f, "does not fit the panel: " + line.Body);
                Assert.LessOrEqual(HeightOf(heading, line.Heading), heading.rectTransform.rect.height + 0.5f, line.Heading);
            }
            Assert.AreEqual(order.Length - 1, previous, "the game ends on the last objective");

            // It names who to talk to, what to bring and where
            string all = string.Join("\n", lines.Select(l => l.Body));
            foreach (string word in new[] { "Mira", "Wren", "Alder", "Dawn Blossom", "Chicken Feed", "workshop", "Echo Ore", "Windmill Braces", "Clover" })
                StringAssert.Contains(word, all);
        }

        [Test]
        public void EveryDialogueLine_FitsTheDialogueBox()
        {
            OpenValley();
            Text line = FindText("Dialogue", "Line");
            float limit = line.rectTransform.rect.height + 0.5f;
            List<string> lines = new List<string>();

            foreach (DialogueInteractable interactable in Object.FindObjectsByType<DialogueInteractable>(FindObjectsSortMode.None))
            {
                SerializedProperty property = new SerializedObject(interactable).GetIterator();
                while (property.NextVisible(true))
                {
                    if (property.propertyType != SerializedPropertyType.Generic || !property.isArray) continue;
                    for (int i = 0; i < property.arraySize; i++)
                    {
                        SerializedProperty element = property.GetArrayElementAtIndex(i);
                        if (element.propertyType == SerializedPropertyType.String) lines.Add(element.stringValue);
                    }
                }
            }
            Assert.Greater(lines.Count, 40, "found the written dialogue of every character");

            // The lines that depend on the satchel and the hen
            GameSession session = new GameSession();
            session.SetExplorationStep(GameSession.ExplorationComplete);
            foreach (ChickenPhase phase in new[] { ChickenPhase.Hungry, ChickenPhase.Producing, ChickenPhase.EggReady })
            {
                GameSession hen = new GameSession();
                if (phase != ChickenPhase.Hungry)
                {
                    hen.Inventory.TryAdd(ItemIds.ChickenFeed, 1);
                    hen.TryFeedChicken();
                    if (phase == ChickenPhase.EggReady) hen.Tick(GameSession.ChickenProductionSeconds);
                }
                lines.AddRange(GardenKeeperInteractable.Reminder(hen));
            }
            lines.AddRange(GardenKeeperInteractable.Reminder(session));   // nothing in the satchel yet: the longest one
            session.Inventory.TryAdd(ItemIds.Flower, 1);
            lines.AddRange(GardenKeeperInteractable.Reminder(session));
            session.Inventory.TryAdd(ItemIds.ChickenFeed, 1);
            lines.AddRange(GardenKeeperInteractable.Reminder(session));
            for (int ore = 0; ore <= 4; ore++)
            {
                GameSession miner = new GameSession();
                if (ore > 0) miner.Inventory.TryAdd(ItemIds.Ore, ore);
                lines.AddRange(WindmillRestorerInteractable.Reminder(miner));
            }

            foreach (string text in lines)
            {
                float height = HeightOf(line, text);
                Assert.Greater(height, 0f, "the font measured something: " + text);
                Assert.LessOrEqual(height, limit, "does not fit the dialogue box (" + height + " > " + limit + "): " + text);
            }
        }
    }
}
