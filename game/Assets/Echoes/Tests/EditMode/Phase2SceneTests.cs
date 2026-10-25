using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Echoes.Tests
{
    /// <summary>Checks that the generated Valley and MainMenu contain every Phase 2 object and that their references are wired.</summary>
    public class Phase2SceneTests
    {
        const string MainMenuPath = "Assets/Echoes/Scenes/MainMenu.unity";
        const string ValleyPath = "Assets/Echoes/Scenes/Valley.unity";

        static T First<T>() where T : Object => Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);

        static void OpenValley() => EditorSceneManager.OpenScene(ValleyPath, OpenSceneMode.Single);

        /// <summary>Every named serialized reference on the component is set (array elements included).</summary>
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

        // ---- Resource nodes --------------------------------------------------------------------------------------------

        [Test]
        public void Valley_HasEnoughRenewableNodes_ForEveryRecipeAndTheQuest()
        {
            OpenValley();
            ResourceNode[] nodes = Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None);

            CollectionAssert.AllItemsAreUnique(nodes.Select(n => n.NodeId), "node ids are the keys of the save file");
            foreach (ResourceNode node in nodes)
            {
                Assert.IsFalse(string.IsNullOrEmpty(node.NodeId), node.name);
                Assert.IsTrue(ItemCatalog.IsKnown(node.ItemId), node.name + " item " + node.ItemId);
                Assert.Greater(node.YieldAmount, 0, node.name);
                Assert.That(node.CooldownSeconds, Is.InRange(1f, 180f), node.name + " must regrow in a playable time");
                Assert.IsTrue(node.HasVisuals, node.name + " needs ready and regrowing looks");
                Assert.IsTrue(node.ReadyVisualShown, node.name + " starts ready");
                Assert.Greater(node.Range, 1f, node.name);
            }

            int herbs = nodes.Count(n => n.ItemId == ItemIds.Herb);
            int flowers = nodes.Count(n => n.ItemId == ItemIds.Flower);
            int ore = nodes.Count(n => n.ItemId == ItemIds.Ore);
            Assert.GreaterOrEqual(herbs, 3, "2 herbs per tonic");
            Assert.GreaterOrEqual(flowers, 3, "a blossom per tonic and per batch of feed");
            Assert.GreaterOrEqual(ore, 4, "4 ore per set of braces");
        }

        [Test]
        public void ResourceNodes_AreSpreadOut_AndAwayFromTheSpawn()
        {
            OpenValley();
            ResourceNode[] nodes = Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None);
            Vector3 spawn = GameObject.Find("SpawnPoint").transform.position;

            foreach (ResourceNode node in nodes)
            {
                Assert.Greater(Vector3.Distance(node.transform.position, spawn), 8f, node.name + " should not be offered at the spawn");
                Assert.IsFalse(node.gameObject.isStatic, node.name + " toggles visuals, so it must not be statically batched");
            }
            for (int i = 0; i < nodes.Length; i++)
                for (int j = i + 1; j < nodes.Length; j++)
                    Assert.Greater(Vector3.Distance(nodes[i].transform.position, nodes[j].transform.position), 4f, nodes[i].name + " vs " + nodes[j].name);
        }

        [Test]
        public void Valley_ProvidesTheHen_WithItsStateFeedback()
        {
            OpenValley();
            ChickenInteractable[] hens = Object.FindObjectsByType<ChickenInteractable>(FindObjectsSortMode.None);
            Assert.AreEqual(1, hens.Length, "exactly one chicken");
            AssertWired(hens[0], "body", "hungryIcon", "producingIcon", "eggVisual");
            Assert.Greater(hens[0].Range, 2f);
        }

        // ---- Gameplay UI -------------------------------------------------------------------------------------------------

        [Test]
        public void Valley_GameplayPanels_AreWired_AndStartHidden()
        {
            OpenValley();
            AssertWired(First<GameplayHud>(), "hud", "coinText", "chickenText", "toastLines");
            AssertWired(First<InventoryPanel>(), "panel", "summaryText", "rowNames", "rowCounts", "rowIcons");
            AssertWired(First<CraftingPanel>(), "panel", "nameTexts", "detailTexts", "craftButtons", "closeButton", "statusText");

            InventoryPanel inventory = First<InventoryPanel>();
            Assert.AreEqual(Inventory.DefaultSlots, inventory.RowCount, "one row per slot");
            Assert.IsFalse(inventory.Panel.activeSelf);
            CraftingPanel crafting = First<CraftingPanel>();
            Assert.AreEqual(RecipeBook.All.Count, crafting.RecipeRows, "one row per recipe");
            Assert.IsFalse(crafting.Panel.activeSelf);
        }

        [Test]
        public void Workshop_OpensTheCraftingPanel()
        {
            OpenValley();
            WorkshopInteractable workshop = First<WorkshopInteractable>();
            Assert.IsNotNull(workshop.Panel);
            Assert.AreSame(First<CraftingPanel>(), workshop.Panel);
        }

        [Test]
        public void PauseMenu_HasAWorkingSaveButton()
        {
            OpenValley();
            PauseController pause = First<PauseController>();
            AssertWired(pause, "panel", "resumeButton", "saveButton", "mainMenuButton", "quitButton");
            Assert.IsFalse(pause.Panel.activeSelf);
        }

        [Test]
        public void MainMenu_OffersContinueNewGameAndQuit()
        {
            EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);
            MainMenuController menu = First<MainMenuController>();
            AssertWired(menu, "continueButton", "playButton", "quitButton", "saveStatusText");

            string[] captions = Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Select(t => t.text).ToArray();
            CollectionAssert.Contains(captions, "Continue");
            CollectionAssert.Contains(captions, "New Game");
            CollectionAssert.Contains(captions, "Quit");
        }

        [Test]
        public void ObjectiveBanner_DoesNotOverlapTheObjectiveOrStatusPanels()
        {
            OpenValley();
            Rect objective = ReferenceRect(Find("ObjectivePanel"));
            Rect status = ReferenceRect(Find("StatusPanel"));
            Rect banner = ReferenceRect(Find("Banner"));

            Assert.GreaterOrEqual(banner.yMin, objective.yMax, "banner starts below the objective panel (y measured downward)");
            Assert.GreaterOrEqual(banner.yMin, status.yMax, "banner starts below the status panel");
            Assert.IsFalse(banner.Overlaps(objective), "banner overlaps the objective panel");
            Assert.IsFalse(banner.Overlaps(status), "banner overlaps the status panel");
        }

        static RectTransform Find(string name)
        {
            RectTransform rect = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(r => r.name == name);
            Assert.IsNotNull(rect, name);
            return rect;
        }

        /// <summary>Rect in the 1920x1080 reference canvas with y measured downward from the top edge (all HUD panels use single-point anchors).</summary>
        static Rect ReferenceRect(RectTransform rect)
        {
            const float width = 1920f;
            const float height = 1080f;
            Vector2 size = rect.sizeDelta;
            float left = rect.anchorMin.x * width + rect.anchoredPosition.x - rect.pivot.x * size.x;
            float top = (1f - rect.anchorMin.y) * height - (rect.anchoredPosition.y + (1f - rect.pivot.y) * size.y);
            return new Rect(left, top, size.x, size.y);
        }

        // ---- Systems and world response --------------------------------------------------------------------------------------

        [Test]
        public void Valley_HasTheSessionHostAndTheResonanceSystems()
        {
            OpenValley();
            AssertWired(First<GameSessionHost>(), "objective");
            AssertWired(First<ValleyLighting>(), "sun");
            AssertWired(First<CompletionFeedback>(), "sun", "lighting", "sparkles", "crystal", "crystalPivot");
            AssertWired(First<ResonanceView>(), "lighting", "troubledStream", "clearStream", "pond", "springWater",
                "murkyWater", "clearWater", "bloomRoot", "pulse", "fountain");
        }

        [Test]
        public void Valley_IsSavedInTheSilentStage()
        {
            OpenValley();
            ResonanceView view = First<ResonanceView>();
            Assert.IsTrue(view.TroubledStreamShown, "stage 0 shows the thin murky trickle");
            Assert.IsFalse(view.ClearStreamShown);
            Assert.IsFalse(view.BloomShown, "no blooms before the stream is restored");
            Assert.IsFalse(view.PondIsClear);
            Assert.AreEqual(ResonanceStage.SilentValley, view.ShownStage);
        }

        [Test]
        public void ResonanceView_HasBloomsToGrow_AndWaterThatActuallyChanges()
        {
            OpenValley();
            SerializedObject view = new SerializedObject(First<ResonanceView>());
            GameObject bloom = (GameObject)view.FindProperty("bloomRoot").objectReferenceValue;
            Assert.GreaterOrEqual(bloom.transform.childCount, 8, "enough blooms to read as the valley coming to life");

            Material murky = (Material)view.FindProperty("murkyWater").objectReferenceValue;
            Material clear = (Material)view.FindProperty("clearWater").objectReferenceValue;
            Assert.AreNotSame(murky, clear);
            Assert.AreNotEqual(murky.GetColor("_BaseColor"), clear.GetColor("_BaseColor"), "the two water looks must differ");
        }

        [Test]
        public void MovingWaterAndBloomObjects_AreNotStaticallyBatched()
        {
            OpenValley();
            ResonanceView view = First<ResonanceView>();
            SerializedObject so = new SerializedObject(view);
            foreach (string field in new[] { "troubledStream", "clearStream", "bloomRoot" })
                Assert.IsFalse(((GameObject)so.FindProperty(field).objectReferenceValue).isStatic, field);
            Assert.IsFalse(((Renderer)so.FindProperty("pond").objectReferenceValue).gameObject.isStatic, "pond swaps material");
            Assert.IsFalse(((Renderer)so.FindProperty("springWater").objectReferenceValue).gameObject.isStatic, "spring water is scaled");
        }

        [Test]
        public void Interactables_DoNotCrowdTheSpawnOrEachOther()
        {
            OpenValley();
            Vector3 spawn = GameObject.Find("SpawnPoint").transform.position;
            List<Interactable> all = Object.FindObjectsByType<Interactable>(FindObjectsSortMode.None).ToList();

            foreach (Interactable interactable in all)
                Assert.IsFalse(interactable.IsInRange(spawn), interactable.name + " must not be in reach of the spawn point");
        }
    }
}
