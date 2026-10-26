using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Echoes.Tests
{
    /// <summary>
    /// Phase 3 in the real Valley scene: the garden and windmill quests, the world's answer to stages 2 and 3, the windmill turning,
    /// the closing conversation and ending screen, and saving and loading at each stage. Every state is reached through the
    /// same interactions the player uses (teleporting next to a target instead of walking, and ticking the session clock the
    /// host would tick every frame while Clover lays).
    /// </summary>
    public class Phase3PlayTests : ValleyPlayTestBase
    {
        // ---- Quest 2: the garden ----------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator GardenKeeper_BeforeTheStream_ExplainsWhyTheGardenSleeps_AndStartsNothing()
        {
            yield return StandNextTo(keeper);
            StringAssert.Contains("Wren", detector.Current.Prompt);
            Assert.IsTrue(detector.TryInteract());
            StringAssert.Contains("asleep", dialogue.CurrentLine);
            while (dialogue.IsOpen) dialogue.Advance();

            Assert.AreEqual(QuestStage.NotStarted, Session.GardenQuest);
            Assert.AreEqual(ResonanceStage.SilentValley, Session.Resonance);
            Assert.IsTrue(resonanceView.GardenDormantShown);
            Assert.IsFalse(resonanceView.GardenBloomShown);
        }

        [UnityTest]
        public IEnumerator GardenQuest_ThroughRealInteractions_BlossomsTheGarden_AndTheWorldKeepsEverythingEarlier()
        {
            yield return RestoreTheStream();
            yield return new WaitForSeconds(3.8f);   // let the stream's transition finish first
            Assert.IsTrue(resonanceView.ClearStreamShown);
            Assert.IsTrue(resonanceView.GardenDormantShown, "the garden still sleeps at stage 1");
            Assert.AreEqual(QuestLog.GardenHeading, hud.ObjectiveHeading);

            // Wren offers the quest and the panel says what to do next
            yield return StandNextTo(keeper);
            InteractAndReadAll(keeper);
            yield return null;
            Assert.AreEqual(QuestStage.Active, Session.GardenQuest);
            Assert.AreEqual(ResonanceStage.AwakeningStream, Session.Resonance, "accepting changes nothing in the world");
            StringAssert.Contains("Dawn Blossom", hud.ObjectiveBody);

            // Without an egg Wren only explains
            yield return StandNextTo(keeper);
            InteractAndReadAll(keeper);
            Assert.AreEqual(QuestStage.Active, Session.GardenQuest);
            Assert.AreEqual(ResonanceStage.AwakeningStream, Session.Resonance);

            yield return GatherFrom("flower_2");
            yield return CraftAtWorkshop(1);
            yield return null;
            StringAssert.Contains("Feed Clover", hud.ObjectiveBody);
            yield return FeedCloverAndWaitForTheEgg();
            yield return null;
            StringAssert.Contains("egg from Clover's nest", hud.ObjectiveBody);
            Assert.IsTrue(detector.TryInteract());
            yield return null;
            StringAssert.Contains("Give Clover's egg", hud.ObjectiveBody);

            yield return StandNextTo(keeper);
            StringAssert.Contains("egg", hud.PromptText);
            int coins = Session.Coins;
            InteractAndReadAll(keeper);

            Assert.AreEqual(ResonanceStage.BloomingGarden, Session.Resonance);
            Assert.AreEqual(0, Session.Inventory.Count(ItemIds.Egg), "the egg is consumed");
            Assert.AreEqual(coins + GameSession.GardenQuestReward, Session.Coins);

            // The garden blooms and everything from stage 1 is still there
            yield return null;
            yield return null;
            Assert.AreEqual(ResonanceStage.BloomingGarden, resonanceView.ShownStage);
            Assert.IsTrue(resonanceView.GardenBloomShown);
            Assert.IsFalse(resonanceView.GardenDormantShown);
            Assert.IsTrue(resonanceView.YardLifeShown, "the hen yard comes alive");
            Assert.IsTrue(resonanceView.GardenMotesPlaying);
            Assert.IsTrue(resonanceView.ClearStreamShown && resonanceView.BloomShown && resonanceView.PondIsClear, "stage 1 stays");
            Assert.IsFalse(resonanceView.WindmillRepairedShown, "the windmill waits for stage 3");
            Assert.IsTrue(hud.IsBannerVisible);
            StringAssert.Contains("Garden", hud.BannerTitle);
            Assert.AreEqual(QuestLog.WindmillHeading, hud.ObjectiveHeading);
            StringAssert.Contains("Alder", hud.ObjectiveBody);

            yield return new WaitForSeconds(4f);
            Assert.IsFalse(resonanceView.IsTransitioning);
            Assert.AreEqual(1f, lighting.GardenBlend, 0.001f);
            Assert.AreEqual(1f, lighting.StreamBlend, 0.001f);
            Assert.AreEqual(0f, lighting.ValleyBlend, 0.001f);
            foreach (NpcIdle villager in resonanceView.Villagers)
                Assert.AreEqual(1, villager.Level, villager.name + " perks up once the garden blooms");

            // Nothing is paid or consumed twice, and Clover is still usable
            Session.Inventory.TryAdd(ItemIds.Egg, 1);
            coins = Session.Coins;
            Assert.AreEqual(Outcome.WrongState, Session.TryDeliverEgg());
            Assert.AreEqual(coins, Session.Coins);
            Assert.AreEqual(ChickenPhase.Hungry, Session.Chicken);
            Assert.AreEqual(2, Session.Inventory.Count(ItemIds.ChickenFeed));
            Assert.AreEqual(Outcome.Ok, Session.TryFeedChicken(), "Clover keeps working after the quest");
        }

        [UnityTest]
        public IEnumerator GardenQuest_WithAnEggAlreadyInTheSatchel_StillCompletes()
        {
            yield return RestoreTheStream();
            yield return GatherFrom("flower_2");
            yield return CraftAtWorkshop(1);
            yield return FeedCloverAndWaitForTheEgg();
            Assert.IsTrue(detector.TryInteract());
            Assert.AreEqual(1, Session.Inventory.Count(ItemIds.Egg), "the egg was collected before the quest began");

            yield return StandNextTo(keeper);
            Assert.IsTrue(detector.TryInteract());
            dialogue.Advance();
            dialogue.Advance();
            StringAssert.Contains("already", dialogue.CurrentLine, "Wren notices the egg you carry");
            while (dialogue.IsOpen) dialogue.Advance();
            Assert.AreEqual(QuestStage.Active, Session.GardenQuest);
            Assert.AreEqual(1, Session.Inventory.Count(ItemIds.Egg), "accepting does not take the egg");
            yield return null;
            StringAssert.Contains("Give Clover's egg", hud.ObjectiveBody);

            InteractAndReadAll(keeper);
            Assert.AreEqual(QuestStage.Completed, Session.GardenQuest);
            Assert.AreEqual(0, Session.Inventory.Count(ItemIds.Egg));
        }

        [UnityTest]
        public IEnumerator GardenWorld_PersistsAcrossSaveAndLoad_WithoutReplayingTheReward()
        {
            yield return RestoreTheGarden();
            int coins = Session.Coins;
            Assert.IsTrue(File.Exists(SaveSystem.MainPath), "completing a quest autosaves");

            yield return ContinueFromSave();

            Assert.IsTrue(host.LoadedFromSave);
            Assert.AreEqual(ResonanceStage.BloomingGarden, Session.Resonance);
            Assert.AreEqual(QuestStage.Completed, Session.GardenQuest);
            Assert.AreEqual(coins, Session.Coins, "loading pays nothing");
            Assert.AreEqual(0, Session.Inventory.Count(ItemIds.Egg));
            Assert.AreEqual(Outcome.WrongState, Session.TryDeliverEgg());

            Assert.AreEqual(ResonanceStage.BloomingGarden, resonanceView.ShownStage);
            Assert.IsFalse(resonanceView.IsTransitioning, "a loaded stage is shown at once");
            Assert.IsTrue(resonanceView.GardenBloomShown && resonanceView.YardLifeShown && !resonanceView.GardenDormantShown);
            Assert.IsTrue(resonanceView.ClearStreamShown && resonanceView.BloomShown);
            Assert.AreEqual(1f, lighting.GardenBlend, 0.001f);
            Assert.AreEqual(1f, lighting.StreamBlend, 0.001f);
            Assert.AreEqual(1f, lighting.HeartBlend, 0.001f);
            foreach (NpcIdle villager in resonanceView.Villagers) Assert.AreEqual(1, villager.Level);
            yield return null;
            Assert.AreEqual(QuestLog.WindmillHeading, hud.ObjectiveHeading);
        }

        [UnityTest]
        public IEnumerator UnfinishedGardenQuest_SurvivesSaveAndLoad()
        {
            yield return RestoreTheStream();
            yield return StandNextTo(keeper);
            InteractAndReadAll(keeper);
            yield return GatherFrom("flower_2");
            yield return CraftAtWorkshop(1);
            yield return FeedCloverAndWaitForTheEgg();
            Assert.IsTrue(pause.SaveGame());
            int feedBefore = Session.Inventory.Count(ItemIds.ChickenFeed);

            yield return ContinueFromSave();

            Assert.AreEqual(QuestStage.Active, Session.GardenQuest);
            Assert.AreEqual(ResonanceStage.AwakeningStream, Session.Resonance);
            Assert.AreEqual(ChickenPhase.EggReady, Session.Chicken, "the finished egg is still in the nest");
            Assert.AreEqual(feedBefore, Session.Inventory.Count(ItemIds.ChickenFeed));
            Assert.IsTrue(resonanceView.GardenDormantShown, "an unfinished quest changes nothing in the world");
            yield return null;
            StringAssert.Contains("egg from Clover's nest", hud.ObjectiveBody);

            yield return StandNextTo(hen);
            Assert.IsTrue(detector.TryInteract());
            yield return StandNextTo(keeper);
            InteractAndReadAll(keeper);
            Assert.AreEqual(QuestStage.Completed, Session.GardenQuest);
        }

        // ---- Quest 3: the windmill --------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator WindmillRestorer_BeforeTheGarden_AsksYouToWait_AndStartsNothing()
        {
            yield return RestoreTheStream();   // stage 1 only
            yield return StandNextTo(restorer);
            Assert.IsTrue(detector.TryInteract());
            StringAssert.Contains("silent", dialogue.CurrentLine);
            while (dialogue.IsOpen) dialogue.Advance();

            Assert.AreEqual(QuestStage.NotStarted, Session.WindmillQuest);
            Assert.AreEqual(Outcome.Locked, Session.TryStartWindmillQuest());
            Assert.IsTrue(resonanceView.WindmillDamagedShown);
            Assert.IsFalse(resonanceView.Windmill.Spinning);
        }

        [UnityTest]
        public IEnumerator WindmillQuest_ThroughRealInteractions_MendsAndTurnsTheWindmill()
        {
            yield return RestoreTheGarden();
            yield return new WaitForSeconds(4f);
            Assert.IsTrue(resonanceView.WindmillDamagedShown);
            Assert.IsFalse(resonanceView.Windmill.Spinning);
            float restAngle = resonanceView.Windmill.TotalDegrees;
            Assert.AreEqual(0f, restAngle, "the broken windmill does not turn");

            yield return StandNextTo(restorer);
            InteractAndReadAll(restorer);
            Assert.AreEqual(QuestStage.Active, Session.WindmillQuest);
            yield return null;
            StringAssert.Contains("Echo Ore 0/4", hud.ObjectiveBody);

            // Without braces Alder only explains
            yield return StandNextTo(restorer);
            InteractAndReadAll(restorer);
            Assert.AreEqual(ResonanceStage.BloomingGarden, Session.Resonance);

            yield return GatherFrom("ore_1");
            yield return null;
            StringAssert.Contains("Echo Ore 1/4", hud.ObjectiveBody);
            yield return GatherFrom("ore_2");
            yield return GatherFrom("ore_3");
            yield return GatherFrom("ore_4");
            yield return null;
            StringAssert.Contains("Mix Windmill Braces", hud.ObjectiveBody);
            yield return CraftAtWorkshop(2);
            yield return null;
            StringAssert.Contains("Give the Windmill Braces", hud.ObjectiveBody);

            yield return StandNextTo(restorer);
            StringAssert.Contains("Windmill Braces", hud.PromptText);
            int coins = Session.Coins;
            InteractAndReadAll(restorer);

            Assert.AreEqual(ResonanceStage.RestoredValley, Session.Resonance);
            Assert.AreEqual(0, Session.Inventory.Count(ItemIds.WindmillBraces), "exactly one set of braces is consumed");
            Assert.AreEqual(coins + GameSession.WindmillQuestReward, Session.Coins);

            // The windmill is mended and starts to turn; the stream and the garden are still there
            yield return null;
            yield return null;
            Assert.AreEqual(ResonanceStage.RestoredValley, resonanceView.ShownStage);
            Assert.IsTrue(resonanceView.WindmillRepairedShown);
            Assert.IsFalse(resonanceView.WindmillDamagedShown);
            Assert.IsTrue(resonanceView.VillageLifeShown);
            Assert.IsTrue(resonanceView.ValleyMotesPlaying);
            Assert.IsTrue(resonanceView.Windmill.Spinning);
            Assert.IsTrue(resonanceView.ClearStreamShown && resonanceView.BloomShown, "stage 1 stays");
            Assert.IsTrue(resonanceView.GardenBloomShown && resonanceView.YardLifeShown && !resonanceView.GardenDormantShown, "stage 2 stays");
            StringAssert.Contains("Windmill", hud.BannerTitle);
            Assert.AreEqual(QuestLog.ReturnHeading, hud.ObjectiveHeading);
            StringAssert.Contains("Mira", hud.ObjectiveBody);

            // It turns during normal play, faster as the sails wind up
            float before = resonanceView.Windmill.TotalDegrees;
            yield return new WaitForSeconds(1.5f);
            float after = resonanceView.Windmill.TotalDegrees;
            Assert.Greater(after - before, 10f, "the sails are turning");
            yield return new WaitForSeconds(4f);
            Assert.IsFalse(resonanceView.IsTransitioning);
            Assert.AreEqual(resonanceView.Windmill.FullSpeed, resonanceView.Windmill.CurrentSpeed, 0.01f);
            Assert.AreEqual(1f, lighting.ValleyBlend, 0.001f);
            Assert.AreEqual(1f, lighting.GardenBlend, 0.001f);
            Assert.AreEqual(1f, lighting.StreamBlend, 0.001f);
            foreach (NpcIdle villager in resonanceView.Villagers) Assert.AreEqual(2, villager.Level, "everyone is cheerful in the restored valley");

            // A second delivery changes nothing
            Session.Inventory.TryAdd(ItemIds.WindmillBraces, 1);
            coins = Session.Coins;
            Assert.AreEqual(Outcome.WrongState, Session.TryDeliverWindmillBraces());
            Assert.AreEqual(coins, Session.Coins);
            Assert.AreEqual(1, Session.Inventory.Count(ItemIds.WindmillBraces));
        }

        [UnityTest]
        public IEnumerator WindmillBraces_CraftedBeforeTheQuest_StillCompleteIt()
        {
            yield return RestoreTheGarden();
            yield return CraftWindmillBraces();   // before talking to Alder at all

            yield return StandNextTo(restorer);
            Assert.IsTrue(detector.TryInteract());
            dialogue.Advance();
            dialogue.Advance();
            StringAssert.Contains("already", dialogue.CurrentLine, "Alder notices the braces you carry");
            while (dialogue.IsOpen) dialogue.Advance();
            Assert.AreEqual(QuestStage.Active, Session.WindmillQuest);
            Assert.AreEqual(1, Session.Inventory.Count(ItemIds.WindmillBraces), "accepting does not take the braces");

            InteractAndReadAll(restorer);
            Assert.AreEqual(QuestStage.Completed, Session.WindmillQuest);
            Assert.AreEqual(ResonanceStage.RestoredValley, Session.Resonance);
        }

        [UnityTest]
        public IEnumerator WindmillWorld_PersistsAcrossSaveAndLoad_AndKeepsTurning()
        {
            yield return RestoreTheWindmill();
            int coins = Session.Coins;

            yield return ContinueFromSave();

            Assert.AreEqual(ResonanceStage.RestoredValley, Session.Resonance);
            Assert.AreEqual(QuestStage.Completed, Session.WindmillQuest);
            Assert.AreEqual(coins, Session.Coins, "loading pays nothing");
            Assert.AreEqual(Outcome.WrongState, Session.TryDeliverWindmillBraces());
            Assert.IsFalse(endingPanel.IsOpen, "the ending is only shown by Mira's closing conversation");

            Assert.AreEqual(ResonanceStage.RestoredValley, resonanceView.ShownStage);
            Assert.IsFalse(resonanceView.IsTransitioning);
            Assert.IsTrue(resonanceView.WindmillRepairedShown && !resonanceView.WindmillDamagedShown && resonanceView.VillageLifeShown);
            Assert.IsTrue(resonanceView.GardenBloomShown && resonanceView.ClearStreamShown);
            Assert.AreEqual(1f, lighting.ValleyBlend, 0.001f);
            Assert.AreEqual(resonanceView.Windmill.FullSpeed, resonanceView.Windmill.CurrentSpeed, 0.01f, "a loaded windmill is already at full speed");

            float before = resonanceView.Windmill.TotalDegrees;
            yield return new WaitForSeconds(0.5f);
            Assert.Greater(resonanceView.Windmill.TotalDegrees - before, 10f, "the loaded windmill turns");
            yield return null;
            Assert.AreEqual(QuestLog.ReturnHeading, hud.ObjectiveHeading);
        }

        [UnityTest]
        public IEnumerator Windmill_StandsStillWhilePaused_AndTurnsAgainOnResume()
        {
            yield return RestoreTheWindmill();
            yield return new WaitForSeconds(4f);

            pause.SetPaused(true);
            yield return null;
            float frozen = resonanceView.Windmill.TotalDegrees;
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.AreEqual(frozen, resonanceView.Windmill.TotalDegrees, 0.0001f, "paused time does not turn the sails");

            pause.SetPaused(false);
            yield return new WaitForSeconds(0.4f);
            Assert.Greater(resonanceView.Windmill.TotalDegrees, frozen + 5f);
        }

        // ---- The ending ----------------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator Mira_DoesNotStartTheEnding_BeforeTheWindmillIsMended()
        {
            yield return RestoreTheGarden();   // stage 2
            yield return StandNextTo(npc);
            InteractAndReadAll(npc);

            Assert.IsFalse(endingPanel.IsOpen);
            Assert.AreEqual(0, endingPanel.TimesShown);
            Assert.IsFalse(Session.EndingSeen);
            Assert.AreEqual(Outcome.Locked, Session.TryCompleteEnding());
        }

        [UnityTest]
        public IEnumerator ClosingConversation_ShowsTheEnding_AndContinueExploringReturnsTheValley()
        {
            yield return PlayToTheEnding();

            Assert.AreEqual(1, endingPanel.TimesShown);
            Assert.IsTrue(Session.EndingSeen, "the ending is recorded as seen when it begins");
            Assert.IsTrue(GameFlow.IsPanelOpen && !GameFlow.GameplayInputAllowed, "the world waits behind the screen");
            Assert.IsTrue(endingPanel.Panel.activeSelf);
            Assert.IsTrue(endingPanel.ContinueButton.interactable && endingPanel.MainMenuButton.interactable);
            StringAssert.Contains("voice", AllText(endingPanel.Panel));
            StringAssert.Contains("Echoes of the Valley", AllText(endingPanel.Panel));
            Assert.IsTrue(File.Exists(SaveSystem.MainPath), "the ending autosaves");
            Assert.IsTrue(SaveSystem.Load().Session.EndingSeen);

            // The restored valley is the backdrop: the camera drifts slowly while the screen is up
            float yaw = rig.Yaw;
            yield return new WaitForSeconds(0.6f);
            Assert.AreNotEqual(yaw, rig.Yaw, "the camera drifts around the valley behind the screen");

            endingPanel.ContinueButton.onClick.Invoke();
            yield return null;
            Assert.IsFalse(endingPanel.IsOpen);
            Assert.IsFalse(endingPanel.Panel.activeSelf);
            Assert.IsTrue(GameFlow.GameplayInputAllowed, "control comes back");
            Assert.AreEqual(0f, rig.AutoOrbitDegreesPerSecond);
            Assert.AreEqual(QuestLog.EndedHeading, hud.ObjectiveHeading);

            // Mira does not repeat it; the valley is still fully playable
            yield return StandNextTo(npc);
            InteractAndReadAll(npc);
            Assert.AreEqual(1, endingPanel.TimesShown);
            Assert.IsFalse(endingPanel.IsOpen);

            Vector3 before = player.transform.position;
            player.Simulate(Vector2.up, 0.5f);
            Assert.AreNotEqual(before, player.transform.position, "the player can walk again");
            yield return GatherFrom("herb_3");
            Assert.AreEqual(1, Session.Inventory.Count(ItemIds.Herb), "gathering still works after the ending");
        }

        [UnityTest]
        public IEnumerator Ending_IsRemembered_AndNeverReplaysOnContinue()
        {
            yield return PlayToTheEnding();
            endingPanel.ContinueButton.onClick.Invoke();
            int coins = Session.Coins;
            Assert.IsTrue(pause.SaveGame());

            yield return ContinueFromSave();

            Assert.IsTrue(Session.EndingSeen);
            Assert.AreEqual(ResonanceStage.RestoredValley, Session.Resonance);
            Assert.AreEqual(coins, Session.Coins);
            Assert.IsFalse(endingPanel.IsOpen, "reopening the game never forces the ending");
            Assert.AreEqual(0, endingPanel.TimesShown);
            Assert.IsTrue(GameFlow.GameplayInputAllowed);
            yield return null;
            Assert.AreEqual(QuestLog.EndedHeading, hud.ObjectiveHeading);

            yield return StandNextTo(npc);
            InteractAndReadAll(npc);
            Assert.IsFalse(endingPanel.IsOpen, "talking to Mira after the ending is just a conversation");
        }

        [UnityTest]
        public IEnumerator Ending_MainMenuButton_LeavesForTheTitle_AndContinueResumesTheEndedGame()
        {
            yield return PlayToTheEnding();

            endingPanel.MainMenuButton.onClick.Invoke();
            float waited = 0f;
            while (Object.FindFirstObjectByType<MainMenuController>() == null && waited < 20f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            MainMenuController menu = Object.FindFirstObjectByType<MainMenuController>();
            Assert.IsNotNull(menu, "the main menu loaded");
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsTrue(menu.ContinueButton.interactable, "the ending was saved, so Continue is available");

            menu.ContinueButton.onClick.Invoke();
            waited = 0f;
            while (Object.FindFirstObjectByType<PlayerController>() == null && waited < 20f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            yield return null;
            yield return null;
            FindSceneObjects();

            Assert.IsTrue(host.LoadedFromSave);
            Assert.IsTrue(Session.EndingSeen);
            Assert.IsFalse(endingPanel.IsOpen);
            Assert.AreEqual(ResonanceStage.RestoredValley, resonanceView.ShownStage);
        }

        // ---- After everything is restored -----------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator EverythingStaysGatherableCraftableAndFeedable_AfterAllThreeRestorations()
        {
            yield return PlayToTheEnding();
            endingPanel.ContinueButton.onClick.Invoke();

            // Time passes, every node has regrown, and the whole economy still works with nothing left to do
            Session.Tick(120f);
            foreach (string id in new[] { "herb_1", "herb_2", "flower_1", "ore_1", "ore_4" }) Assert.IsTrue(Node(id).IsReady, id + " regrows");

            yield return GatherFrom("herb_1");
            yield return GatherFrom("herb_2");
            yield return GatherFrom("flower_1");
            yield return CraftAtWorkshop(0);
            Assert.AreEqual(1, Session.Inventory.Count(ItemIds.SpringTonic), "a tonic can still be mixed");
            yield return GatherFrom("ore_1");
            Assert.AreEqual(1, Session.Inventory.Count(ItemIds.Ore));

            yield return GatherFrom("flower_3");
            yield return CraftAtWorkshop(1);
            yield return FeedCloverAndWaitForTheEgg();
            Assert.IsTrue(detector.TryInteract());
            Assert.AreEqual(1, Session.Inventory.Count(ItemIds.Egg), "Clover still lays after the story ends");

            // Nothing left over can reward or restore a second time
            int coins = Session.Coins;
            yield return StandNextTo(spring);
            InteractAndReadAll(spring);
            yield return StandNextTo(keeper);
            InteractAndReadAll(keeper);
            yield return StandNextTo(restorer);
            InteractAndReadAll(restorer);
            Assert.AreEqual(coins, Session.Coins);
            Assert.AreEqual(1, Session.Inventory.Count(ItemIds.SpringTonic));
            Assert.AreEqual(1, Session.Inventory.Count(ItemIds.Egg));
        }

        /// <summary>Every piece of text under a panel, for content checks.</summary>
        static string AllText(GameObject root)
        {
            System.Text.StringBuilder text = new System.Text.StringBuilder();
            foreach (UnityEngine.UI.Text label in root.GetComponentsInChildren<UnityEngine.UI.Text>(true)) text.AppendLine(label.text);
            return text.ToString();
        }
    }
}
