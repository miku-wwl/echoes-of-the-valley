using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Echoes.Tests
{
    public class GatheringPlayTests : ValleyPlayTestBase
    {
        [UnityTest]
        public IEnumerator InRange_Gathering_GrantsTheItem_AndTheNodeShowsItsRegrowingLook()
        {
            ResourceNode node = Node("herb_1");
            yield return StandNextTo(node);

            Assert.AreSame(node, detector.Current);
            StringAssert.Contains("Moonleaf Herb", hud.PromptText);
            Assert.IsTrue(node.ReadyVisualShown);

            Assert.IsTrue(detector.TryInteract());
            yield return null;

            Assert.AreEqual(1, Session.Inventory.Count(ItemIds.Herb));
            Assert.IsFalse(node.IsReady);
            Assert.IsFalse(node.ReadyVisualShown, "the bush is stripped");
            StringAssert.Contains("regrowing", node.Prompt);
            StringAssert.Contains("+1 Moonleaf Herb", gameplayHud.LatestToast);
        }

        [UnityTest]
        public IEnumerator OutOfRange_Gathering_IsRejected()
        {
            ResourceNode node = Node("herb_1");
            player.Teleport(node.Position + new Vector3(0f, 0.6f, -(node.Range + 1.2f)));
            yield return null;
            yield return null;

            Assert.IsFalse(node.IsInRange(player.transform.position));
            Assert.AreNotSame(node, detector.Current);
            Assert.IsFalse(detector.TryInteract());
            Assert.AreEqual(0, Session.Inventory.Count(ItemIds.Herb));
            Assert.IsTrue(node.IsReady);
        }

        [UnityTest]
        public IEnumerator PressingTwice_GathersOnce_UntilTheNodeRegrows()
        {
            ResourceNode node = Node("flower_1");
            yield return StandNextTo(node);

            Assert.IsTrue(detector.TryInteract());
            Assert.IsTrue(detector.TryInteract(), "the interaction is still offered");
            Assert.IsTrue(detector.TryInteract());
            Assert.AreEqual(1, Session.Inventory.Count(ItemIds.Flower), "only the first press gathered");

            Session.Tick(node.CooldownSeconds + 1f);
            yield return null;
            Assert.IsTrue(node.IsReady);
            Assert.IsTrue(node.ReadyVisualShown, "the blossoms are back");

            Assert.IsTrue(detector.TryInteract());
            Assert.AreEqual(2, Session.Inventory.Count(ItemIds.Flower));
        }

        [UnityTest]
        public IEnumerator FullStack_DoesNotConsumeTheNode_AndExplainsWhy()
        {
            Session.Inventory.TryAdd(ItemIds.Ore, Inventory.DefaultStackLimit);
            ResourceNode node = Node("ore_1");
            yield return StandNextTo(node);

            Assert.IsTrue(detector.TryInteract());
            yield return null;

            Assert.AreEqual(Inventory.DefaultStackLimit, Session.Inventory.Count(ItemIds.Ore));
            Assert.IsTrue(node.IsReady, "a refused pick must not burn the node");
            StringAssert.Contains("cannot hold", gameplayHud.LatestToast);
        }

        [UnityTest]
        public IEnumerator EveryNode_StartsReady_AndComesBack()
        {
            ResourceNode[] nodes = Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None);
            Assert.GreaterOrEqual(nodes.Length, 13);
            foreach (ResourceNode node in nodes)
            {
                Assert.IsTrue(node.IsReady, node.NodeId);
                Assert.AreEqual(Outcome.Ok, Session.TryGather(node.NodeId), node.NodeId);
                Session.Inventory.TryRemove(node.ItemId, 1);   // keep the bag from filling while testing every node
            }
            yield return null;
            foreach (ResourceNode node in nodes) Assert.IsFalse(node.ReadyVisualShown, node.NodeId);

            Session.Tick(200f);
            yield return null;
            foreach (ResourceNode node in nodes)
            {
                Assert.IsTrue(node.IsReady, node.NodeId + " must regrow");
                Assert.IsTrue(node.ReadyVisualShown, node.NodeId);
            }
        }
    }

    public class CraftingPlayTests : ValleyPlayTestBase
    {
        [UnityTest]
        public IEnumerator Workshop_OpensThePanel_AndTheButtonCraftsFromRealIngredients()
        {
            Session.Inventory.TryAdd(ItemIds.Herb, 2);
            Session.Inventory.TryAdd(ItemIds.Flower, 1);

            yield return StandNextTo(workshop);
            Assert.AreSame(workshop, detector.Current);
            Assert.IsTrue(detector.TryInteract());
            yield return null;

            Assert.IsTrue(crafting.IsOpen);
            Assert.IsTrue(GameFlow.IsPanelOpen);
            Assert.IsFalse(GameFlow.GameplayInputAllowed, "the world waits while the panel is open");
            Assert.IsFalse(dialogue.IsOpen);
            StringAssert.Contains("Spring Tonic", crafting.NameText(0));
            StringAssert.Contains("Moonleaf Herb 2/2", crafting.DetailText(0));
            Assert.IsTrue(crafting.CraftButton(0).interactable, "ingredients are present");
            Assert.IsFalse(crafting.CraftButton(2).interactable, "no ore for braces");

            crafting.CraftButton(0).onClick.Invoke();

            Assert.AreEqual(1, Session.Inventory.Count(ItemIds.SpringTonic));
            Assert.AreEqual(0, Session.Inventory.Count(ItemIds.Herb));
            Assert.AreEqual(0, Session.Inventory.Count(ItemIds.Flower));
            StringAssert.Contains("Crafted", crafting.StatusMessage);
            Assert.IsFalse(crafting.CraftButton(0).interactable, "the ingredients are spent");

            crafting.CloseButton.onClick.Invoke();
            Assert.IsFalse(crafting.IsOpen);
            Assert.IsTrue(GameFlow.GameplayInputAllowed);
        }

        [UnityTest]
        public IEnumerator MissingIngredients_ChangeNothing_AndTheStatusSaysSo()
        {
            Session.Inventory.TryAdd(ItemIds.Herb, 1);
            yield return StandNextTo(workshop);
            detector.TryInteract();
            yield return null;

            Assert.AreEqual(Outcome.Insufficient, crafting.Craft(0));

            Assert.AreEqual(1, Session.Inventory.Count(ItemIds.Herb));
            Assert.AreEqual(0, Session.Inventory.Count(ItemIds.SpringTonic));
            StringAssert.Contains("missing", crafting.StatusMessage);
        }

        [UnityTest]
        public IEnumerator RepeatedClicks_CraftOnlyAsOftenAsTheIngredientsAllow()
        {
            Session.Inventory.TryAdd(ItemIds.Herb, 2);
            Session.Inventory.TryAdd(ItemIds.Flower, 1);
            yield return StandNextTo(workshop);
            detector.TryInteract();
            yield return null;

            Assert.AreEqual(Outcome.Ok, crafting.Craft(0));
            Assert.AreEqual(Outcome.Insufficient, crafting.Craft(0));
            Assert.AreEqual(Outcome.Insufficient, crafting.Craft(0));

            Assert.AreEqual(1, Session.Inventory.Count(ItemIds.SpringTonic));
        }

        [UnityTest]
        public IEnumerator OutputWithoutRoom_KeepsTheIngredients()
        {
            Session.Inventory.TryAdd(ItemIds.Flower, 1);
            Session.Inventory.TryAdd(ItemIds.ChickenFeed, 8);
            yield return StandNextTo(workshop);
            detector.TryInteract();
            yield return null;

            Assert.AreEqual(Outcome.NoRoom, crafting.Craft(1));

            Assert.AreEqual(1, Session.Inventory.Count(ItemIds.Flower));
            Assert.AreEqual(8, Session.Inventory.Count(ItemIds.ChickenFeed));
            StringAssert.Contains("no room", crafting.StatusMessage);
        }

        [UnityTest]
        public IEnumerator Inventory_ListsTheRealStacks_SlotsAndCoins()
        {
            Session.Inventory.TryAdd(ItemIds.Herb, 2);
            Session.Inventory.TryAdd(ItemIds.Ore, 4);

            inventoryPanel.Open();
            yield return null;

            Assert.IsTrue(inventoryPanel.IsOpen);
            Assert.IsTrue(GameFlow.IsPanelOpen);
            Assert.AreEqual("Moonleaf Herb", inventoryPanel.RowName(0));
            Assert.AreEqual("x2 / 9", inventoryPanel.RowCountText(0));
            Assert.AreEqual("Echo Ore", inventoryPanel.RowName(1));
            Assert.AreEqual("x4 / 9", inventoryPanel.RowCountText(1));
            Assert.AreEqual("- empty -", inventoryPanel.RowName(2));
            StringAssert.Contains("Slots 2/8", inventoryPanel.Summary);
            StringAssert.Contains("Coins " + GameSession.StartingCoins, inventoryPanel.Summary);

            inventoryPanel.Close();
            Assert.IsFalse(GameFlow.IsPanelOpen);
        }

        [UnityTest]
        public IEnumerator OnlyOnePanelCanBeOpen_AndPanelsRefuseToOpenDuringADialogue()
        {
            inventoryPanel.Open();
            crafting.Open();
            Assert.IsTrue(inventoryPanel.IsOpen);
            Assert.IsFalse(crafting.IsOpen, "a second panel cannot stack on the first");
            inventoryPanel.Close();

            dialogue.Open("Test", new[] { "line" });
            inventoryPanel.Open();
            Assert.IsFalse(inventoryPanel.IsOpen);
            dialogue.Close();
            yield return null;
        }
    }

    public class ChickenPlayTests : ValleyPlayTestBase
    {
        [UnityTest]
        public IEnumerator Hen_FeedWaitCollect_ThroughRealInteractions()
        {
            Assert.AreEqual(ChickenPhase.Hungry, hen.ShownPhase);
            Assert.IsTrue(hen.HungryIconShown);
            StringAssert.Contains("hungry", gameplayHud.ChickenText);
            Session.Inventory.TryAdd(ItemIds.ChickenFeed, 2);

            yield return StandNextTo(hen);
            StringAssert.Contains("Feed Clover", hud.PromptText);
            Assert.IsTrue(detector.TryInteract());
            yield return null;

            Assert.AreEqual(ChickenPhase.Producing, Session.Chicken);
            Assert.AreEqual(1, Session.Inventory.Count(ItemIds.ChickenFeed));
            Assert.IsTrue(hen.ProducingIconShown);
            Assert.IsFalse(hen.HungryIconShown);
            StringAssert.Contains("laying", hud.PromptText);

            Assert.IsTrue(detector.TryInteract());
            Assert.AreEqual(1, Session.Inventory.Count(ItemIds.ChickenFeed), "pressing E again must not feed twice");

            Session.Tick(GameSession.ChickenProductionSeconds + 0.5f);
            yield return null;
            yield return null;
            Assert.AreEqual(ChickenPhase.EggReady, Session.Chicken);
            Assert.IsTrue(hen.EggShown);
            StringAssert.Contains("Collect", hud.PromptText);

            Assert.IsTrue(detector.TryInteract());
            Assert.AreEqual(1, Session.Inventory.Count(ItemIds.Egg));
            Assert.AreEqual(ChickenPhase.Hungry, Session.Chicken);
            yield return null;
            Assert.IsFalse(hen.EggShown);
            Assert.AreEqual(1, Session.Inventory.Count(ItemIds.Egg), "the egg cannot be collected twice");
        }

        [UnityTest]
        public IEnumerator HungryHen_WithoutFeed_ExplainsWhatIsNeeded_AndChangesNothing()
        {
            yield return StandNextTo(hen);
            StringAssert.Contains("needs Chicken Feed", hud.PromptText);

            Assert.IsTrue(detector.TryInteract());
            yield return null;

            Assert.AreEqual(ChickenPhase.Hungry, Session.Chicken);
            StringAssert.Contains("Chicken Feed", gameplayHud.LatestToast);
        }

        [UnityTest]
        public IEnumerator HenStatus_AppearsInTheHud()
        {
            Session.Inventory.TryAdd(ItemIds.ChickenFeed, 1);
            yield return StandNextTo(hen);
            StringAssert.Contains("hungry", gameplayHud.ChickenText);
            detector.TryInteract();
            yield return null;
            StringAssert.Contains("laying an egg", gameplayHud.ChickenText);
            Session.Tick(GameSession.ChickenProductionSeconds + 1f);
            yield return null;
            StringAssert.Contains("egg ready", gameplayHud.ChickenText);
        }
    }

    public class StreamQuestPlayTests : ValleyPlayTestBase
    {
        [UnityTest]
        public IEnumerator TheValley_StartsSilent()
        {
            yield return null;
            Assert.AreEqual(ResonanceStage.SilentValley, Session.Resonance);
            Assert.AreEqual(ResonanceStage.SilentValley, resonanceView.ShownStage);
            Assert.IsTrue(resonanceView.TroubledStreamShown);
            Assert.IsFalse(resonanceView.ClearStreamShown);
            Assert.IsFalse(resonanceView.BloomShown);
            Assert.IsFalse(resonanceView.PondIsClear);
            Assert.AreEqual(0f, lighting.StreamBlend);
            StringAssert.Contains("COINS", gameplayHud.CoinText.ToUpperInvariant());
        }

        [UnityTest]
        public IEnumerator Spring_CannotRestoreTheStream_WithoutTheQuestAndTheTonic()
        {
            // before the opening objective is finished the spring only shows its lore
            Session.Inventory.TryAdd(ItemIds.SpringTonic, 1);
            yield return StandNextTo(spring);
            InteractAndReadAll(spring);
            Assert.AreEqual(ResonanceStage.SilentValley, Session.Resonance);
            Assert.AreEqual(1, Session.Inventory.Count(ItemIds.SpringTonic), "a tonic is not spent by the lore visit");

            // with the quest active but no tonic, it explains what is needed and changes nothing
            Session.Inventory.TryRemove(ItemIds.SpringTonic, 1);
            yield return CompleteExploration();
            yield return StandNextTo(npc);
            InteractAndReadAll(npc);
            yield return StandNextTo(spring);
            InteractAndReadAll(spring);
            Assert.AreEqual(StreamQuestStage.Active, Session.StreamQuest);
            Assert.AreEqual(ResonanceStage.SilentValley, Session.Resonance);
            Assert.AreEqual(GameSession.StartingCoins, Session.Coins);
        }

        [UnityTest]
        public IEnumerator FullQuest_GatherCraftDeliver_RestoresTheStream_AndTheWorldVisiblyChanges()
        {
            yield return CompleteExploration();
            yield return StandNextTo(npc);
            InteractAndReadAll(npc);
            yield return null;
            Assert.AreEqual("Restore the Stream", hud.ObjectiveHeading);
            StringAssert.Contains("Gather Moonleaf Herb 0/2", hud.ObjectiveBody);

            yield return GatherFrom("herb_1");
            yield return GatherFrom("herb_2");
            yield return GatherFrom("flower_1");
            yield return null;
            StringAssert.Contains("Mix a Spring Tonic", hud.ObjectiveBody);

            yield return CraftAtWorkshop(0);
            yield return null;
            Assert.AreEqual(1, Session.Inventory.Count(ItemIds.SpringTonic));
            StringAssert.Contains("Pour the Spring Tonic", hud.ObjectiveBody);

            yield return StandNextTo(spring);
            StringAssert.Contains("Pour the Spring Tonic", hud.PromptText);
            int coinsBefore = Session.Coins;
            InteractAndReadAll(spring);

            Assert.AreEqual(StreamQuestStage.Completed, Session.StreamQuest);
            Assert.AreEqual(ResonanceStage.AwakeningStream, Session.Resonance);
            Assert.AreEqual(0, Session.Inventory.Count(ItemIds.SpringTonic), "the tonic is consumed");
            Assert.AreEqual(coinsBefore + GameSession.StreamQuestReward, Session.Coins);

            // The world answers: clear wide stream, filled basin, blooms and a warmer light, once the short transition has run.
            yield return null;
            yield return null;
            Assert.AreEqual(ResonanceStage.AwakeningStream, resonanceView.ShownStage);
            Assert.IsTrue(resonanceView.ClearStreamShown);
            Assert.IsFalse(resonanceView.TroubledStreamShown);
            Assert.IsTrue(resonanceView.BloomShown);
            Assert.IsTrue(resonanceView.PondIsClear);
            Assert.IsTrue(hud.IsBannerVisible);
            StringAssert.Contains("Stream", hud.BannerTitle);

            yield return new WaitForSeconds(4.3f);
            Assert.IsFalse(resonanceView.IsTransitioning);
            Assert.AreEqual(1f, lighting.StreamBlend, 0.001f);
            Assert.AreEqual(1f, lighting.HeartBlend, 0.001f, "the opening payoff finished too");
            Assert.Greater(lighting.Sun.intensity, 1.45f, "warm light: heart look plus the stream's golden-hour lift");
            // Phase 3: a finished stream quest hands the objective panel on to the next quest instead of saying "Complete".
            Assert.AreEqual(QuestLog.GardenHeading, hud.ObjectiveHeading);
            StringAssert.Contains("Wren", hud.ObjectiveBody);

            // Nothing can be delivered or rewarded twice.
            int coins = Session.Coins;
            Session.Inventory.TryAdd(ItemIds.SpringTonic, 1);
            yield return StandNextTo(spring);
            InteractAndReadAll(spring);
            Assert.AreEqual(coins, Session.Coins);
            Assert.AreEqual(1, Session.Inventory.Count(ItemIds.SpringTonic));
            Assert.AreEqual(Outcome.WrongState, Session.TryDeliverSpringTonic());
        }

        [UnityTest]
        public IEnumerator Mira_FollowsTheRealQuestState()
        {
            yield return CompleteExploration();
            yield return StandNextTo(npc);

            // offer -> reminder with live counts -> tonic reminder
            Assert.IsTrue(detector.TryInteract());
            dialogue.Advance();
            StringAssert.Contains("Spring Tonic", dialogue.CurrentLine);
            while (dialogue.IsOpen) dialogue.Advance();
            Assert.AreEqual(StreamQuestStage.Active, Session.StreamQuest);

            Session.Inventory.TryAdd(ItemIds.Herb, 1);
            Assert.IsTrue(detector.TryInteract());
            StringAssert.Contains("you have 1", dialogue.CurrentLine);
            dialogue.Close();

            Session.Inventory.TryAdd(ItemIds.SpringTonic, 1);
            Assert.IsTrue(detector.TryInteract());
            StringAssert.Contains("pour it in", dialogue.CurrentLine);
            dialogue.Close();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Lighting_ComposesTheHeartAndTheStream_InAnyOrder()
        {
            yield return null;
            float muted = lighting.Sun.intensity;

            lighting.SetStreamBlend(1f);
            yield return null;
            float streamOnly = lighting.Sun.intensity;

            lighting.SetHeartBlend(1f);
            yield return null;
            float both = lighting.Sun.intensity;

            lighting.SetStreamBlend(0f);
            yield return null;
            float heartOnly = lighting.Sun.intensity;

            Assert.Greater(streamOnly, muted, "the restored stream lifts the muted valley");
            Assert.Greater(heartOnly, streamOnly, "the Heart payoff is the bigger change");
            Assert.Greater(both, heartOnly, "and the stream still adds to it afterwards");
            Assert.AreEqual(both - heartOnly, streamOnly - muted, 0.001f, "the stream's contribution is the same either way (additive)");
        }
    }

    public class SaveLoadPlayTests : ValleyPlayTestBase
    {
        [UnityTest]
        public IEnumerator SaveFromThePauseMenu_ThenContinue_RestoresTheSessionAndTheScene()
        {
            yield return GatherFrom("herb_1");
            yield return GatherFrom("flower_1");
            yield return CraftAtWorkshop(1);   // Chicken Feed
            Assert.AreEqual(3, Session.Inventory.Count(ItemIds.ChickenFeed));
            yield return StandNextTo(hen);
            Assert.IsTrue(detector.TryInteract());
            Session.Tick(5f);

            int coins = Session.Coins;
            float henRemaining = Session.ChickenRemaining;
            float herbRemaining = Session.NodeRemaining("herb_1");
            Assert.IsTrue(pause.SaveGame());
            Assert.IsTrue(File.Exists(SaveSystem.MainPath));

            yield return ContinueFromSave();

            Assert.IsTrue(host.LoadedFromSave);
            Assert.AreEqual(1, Session.Inventory.Count(ItemIds.Herb));
            Assert.AreEqual(2, Session.Inventory.Count(ItemIds.ChickenFeed));
            Assert.AreEqual(0, Session.Inventory.Count(ItemIds.Flower));
            Assert.AreEqual(coins, Session.Coins);
            Assert.AreEqual(ChickenPhase.Producing, Session.Chicken);
            Assert.AreEqual(henRemaining, Session.ChickenRemaining, 0.6f);
            Assert.IsFalse(Node("herb_1").IsReady, "the gathered bush is still regrowing");
            Assert.AreEqual(herbRemaining, Session.NodeRemaining("herb_1"), 0.6f);
            Assert.IsFalse(Node("flower_1").IsReady);
            Assert.IsTrue(Node("herb_2").IsReady);
            Assert.IsFalse(Node("herb_1").ReadyVisualShown, "the scene shows the saved state");
            Assert.IsTrue(hen.ProducingIconShown);
            StringAssert.Contains("laying", gameplayHud.ChickenText);
            StringAssert.Contains("loaded", gameplayHud.LatestToast.ToLowerInvariant());
        }

        [UnityTest]
        public IEnumerator CompletedRestoration_PersistsAcrossSaveAndLoad_WithoutReplayingAnyReward()
        {
            yield return RestoreTheStream();
            int coins = Session.Coins;
            Assert.IsTrue(pause.SaveGame());

            yield return ContinueFromSave();

            Assert.IsTrue(host.LoadedFromSave);
            Assert.AreEqual(StreamQuestStage.Completed, Session.StreamQuest);
            Assert.AreEqual(ResonanceStage.AwakeningStream, Session.Resonance);
            Assert.AreEqual(coins, Session.Coins, "loading must not pay the quest reward again");
            Assert.AreEqual(0, Session.Inventory.Count(ItemIds.SpringTonic));

            // the restored valley is shown at once, with no transition playing
            Assert.AreEqual(ResonanceStage.AwakeningStream, resonanceView.ShownStage);
            Assert.IsFalse(resonanceView.IsTransitioning);
            Assert.IsTrue(resonanceView.ClearStreamShown);
            Assert.IsTrue(resonanceView.BloomShown);
            Assert.IsTrue(resonanceView.PondIsClear);
            Assert.AreEqual(1f, lighting.StreamBlend, 0.001f);

            // the opening payoff is restored too, and the quest panel shows the finished state
            Assert.IsTrue(objective.IsComplete);
            Assert.IsTrue(objective.Feedback.HasPlayed);
            Assert.AreEqual(1f, lighting.HeartBlend, 0.001f);
            Assert.AreEqual(0, objective.CompletionCount, "restoring is not completing");
            yield return null;
            Assert.AreEqual(QuestLog.GardenHeading, hud.ObjectiveHeading, "Phase 3: the panel moves on to the garden quest");
            StringAssert.Contains("Wren", hud.ObjectiveBody);

            Session.Inventory.TryAdd(ItemIds.SpringTonic, 1);
            Assert.AreEqual(Outcome.WrongState, Session.TryDeliverSpringTonic());
            Assert.AreEqual(coins, Session.Coins);
        }

        [UnityTest]
        public IEnumerator Continue_WithoutASave_FallsBackToANewGame_AndSaysSo()
        {
            Assert.IsFalse(File.Exists(SaveSystem.MainPath));

            yield return ContinueFromSave();

            Assert.IsFalse(host.LoadedFromSave);
            StringAssert.Contains("Could not load", host.StartupNotice);
            Assert.AreEqual(GameSession.StartingCoins, Session.Coins);
            Assert.AreEqual(0, Session.Inventory.UsedSlots);
        }

        [UnityTest]
        public IEnumerator Continue_WithACorruptSave_NeverLoadsIt()
        {
            Directory.CreateDirectory(SaveSystem.Folder);
            File.WriteAllText(SaveSystem.MainPath, "{ \"version\": 1, \"coins\": 99999 garbage");

            yield return ContinueFromSave();

            Assert.IsFalse(host.LoadedFromSave);
            Assert.AreEqual(GameSession.StartingCoins, Session.Coins, "damaged data must not become progress");
            StringAssert.Contains("Could not load", gameplayHud.LatestToast);
        }

        [UnityTest]
        public IEnumerator MilestonesAutosave_AndNewGameStartsCleanEvenWhenASaveExists()
        {
            yield return CompleteExploration();
            yield return StandNextTo(npc);
            InteractAndReadAll(npc);   // accepting the quest is a milestone
            Assert.IsTrue(File.Exists(SaveSystem.MainPath), "starting the quest autosaves");
            Assert.AreEqual(StreamQuestStage.Active, SaveSystem.Load().Session.StreamQuest);

            GameLaunch.Mode = LaunchMode.New;
            yield return SceneManager.LoadSceneAsync(SceneNames.Valley);
            yield return null;
            yield return null;
            FindSceneObjects();

            Assert.IsFalse(host.LoadedFromSave);
            Assert.AreEqual(StreamQuestStage.NotStarted, Session.StreamQuest);
            Assert.AreEqual(0, Session.ExplorationStep);
            Assert.IsFalse(objective.IsComplete);
        }
    }

    public class MainMenuSavePlayTests
    {
        [UnitySetUp]
        public IEnumerator LoadMainMenu()
        {
            TestSaveFolder.Begin();
            yield return SceneManager.LoadSceneAsync(SceneNames.MainMenu);
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator RestoreGlobalState()
        {
            GameFlow.EnterMenuMode();
            TestSaveFolder.End();
            yield return null;
        }

        static MainMenuController Menu => Object.FindFirstObjectByType<MainMenuController>();

        static IEnumerator WaitForValley()
        {
            float waited = 0f;
            while (Object.FindFirstObjectByType<PlayerController>() == null && waited < 20f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator WithoutASave_ContinueIsDisabled_AndTheMenuExplainsIt()
        {
            Assert.IsFalse(Menu.ContinueButton.interactable);
            StringAssert.Contains("No saved game", Menu.SaveMessage);
            Assert.IsTrue(Menu.PlayButton.interactable);
            yield return null;
        }

        [UnityTest]
        public IEnumerator WithAValidSave_ContinueLoadsTheSavedGame()
        {
            GameSession saved = new GameSession();
            saved.Inventory.TryAdd(ItemIds.Ore, 3);
            saved.TryAddCoins(5);
            Assert.IsTrue(SaveSystem.Save(saved, out string message), message);

            Menu.RefreshSaveState();
            Assert.IsTrue(Menu.ContinueButton.interactable);
            Menu.ContinueButton.onClick.Invoke();
            yield return WaitForValley();

            Assert.IsTrue(GameSessionHost.Instance.LoadedFromSave);
            Assert.AreEqual(3, GameSession.Current.Inventory.Count(ItemIds.Ore));
            Assert.AreEqual(GameSession.StartingCoins + 5, GameSession.Current.Coins);
        }

        [UnityTest]
        public IEnumerator WithACorruptSave_ContinueIsDisabled_AndSaysWhy()
        {
            Directory.CreateDirectory(SaveSystem.Folder);
            File.WriteAllText(SaveSystem.MainPath, "not a save");

            Menu.RefreshSaveState();

            Assert.IsFalse(Menu.ContinueButton.interactable);
            StringAssert.Contains("unusable", Menu.SaveMessage);
            yield return null;
        }

        [UnityTest]
        public IEnumerator WhenOnlyTheBackupIsGood_ContinueStillWorks_AndMentionsTheBackup()
        {
            GameSession saved = new GameSession();
            saved.TryAddCoins(7);
            SaveSystem.Save(saved, out _);
            SaveSystem.Save(saved, out _);
            File.WriteAllText(SaveSystem.MainPath, "damaged");

            Menu.RefreshSaveState();

            Assert.IsTrue(Menu.ContinueButton.interactable);
            StringAssert.Contains("backup", Menu.SaveMessage);
            yield return null;
        }

        // Phase 3 behavior change: with a save on disk, the New Game button first asks (Phase 2 started the game at once).
        [UnityTest]
        public IEnumerator NewGame_WithAnExistingSave_AsksFirst_AndOnlyConfirmingStartsClean()
        {
            GameSession saved = new GameSession();
            saved.Inventory.TryAdd(ItemIds.Ore, 3);
            SaveSystem.Save(saved, out _);
            string savedText = File.ReadAllText(SaveSystem.MainPath);

            Menu.RefreshSaveState();
            Menu.PlayButton.onClick.Invoke();
            yield return null;
            Assert.IsTrue(Menu.ConfirmationOpen, "a save exists, so New Game must ask first");
            StringAssert.Contains("saved game", Menu.ConfirmationMessage);
            Assert.IsNull(Object.FindFirstObjectByType<PlayerController>(), "asking must not start the game");
            Assert.AreEqual(savedText, File.ReadAllText(SaveSystem.MainPath), "asking must not touch the save");

            Menu.ConfirmButton.onClick.Invoke();
            yield return WaitForValley();

            Assert.IsFalse(GameSessionHost.Instance.LoadedFromSave);
            Assert.AreEqual(0, GameSession.Current.Inventory.UsedSlots);
            Assert.AreEqual(GameSession.StartingCoins, GameSession.Current.Coins);
            Assert.AreEqual(savedText, File.ReadAllText(SaveSystem.MainPath), "the old save stays on disk until the new game first saves");
        }

        [UnityTest]
        public IEnumerator NewGame_Cancel_KeepsTheTitleScreenAndTheSave()
        {
            GameSession saved = new GameSession();
            saved.TryAddCoins(9);
            SaveSystem.Save(saved, out _);

            Menu.RefreshSaveState();
            Menu.PlayButton.onClick.Invoke();
            yield return null;
            Assert.IsTrue(Menu.ConfirmationOpen);
            Assert.IsFalse(Menu.PlayButton.interactable, "the buttons behind the question are off while it is open");

            Menu.CancelButton.onClick.Invoke();
            yield return null;

            Assert.IsFalse(Menu.ConfirmationOpen);
            Assert.IsTrue(Menu.PlayButton.interactable);
            Assert.IsTrue(Menu.ContinueButton.interactable, "the save is still usable");
            Assert.IsNull(Object.FindFirstObjectByType<PlayerController>());
            Assert.IsTrue(File.Exists(SaveSystem.MainPath));
        }

        [UnityTest]
        public IEnumerator NewGame_WithAnUnreadableSave_AlsoAsks_AndKeepsTheFileAside()
        {
            Directory.CreateDirectory(SaveSystem.Folder);
            File.WriteAllText(SaveSystem.MainPath, "{ \"version\": 99 }");

            Menu.RefreshSaveState();
            Menu.PlayButton.onClick.Invoke();
            yield return null;
            Assert.IsTrue(Menu.ConfirmationOpen);
            StringAssert.Contains("cannot be loaded", Menu.ConfirmationMessage);

            Menu.ConfirmButton.onClick.Invoke();
            yield return WaitForValley();
            Assert.IsTrue(GameSessionHost.Instance.SaveNow());

            Assert.IsTrue(File.Exists(SaveSystem.SetAsidePath), "an unreadable save is set aside, never deleted");
            Assert.AreEqual("{ \"version\": 99 }", File.ReadAllText(SaveSystem.SetAsidePath));
            Assert.IsTrue(SaveSystem.Load().Ok, "the new game's own save is the main file now");
        }
    }
}
