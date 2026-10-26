using System.Collections.Generic;
using NUnit.Framework;

namespace Echoes.Tests
{
    /// <summary>The garden quest, the windmill quest, the ending and the invariants between them, on the plain GameSession.</summary>
    public class Phase3SessionTests
    {
        /// <summary>Like the Valley: renewable herb, blossom and ore nodes.</summary>
        static GameSession NewSession()
        {
            GameSession session = new GameSession();
            session.RegisterNode("herb_a", ItemIds.Herb, 1, 40f);
            session.RegisterNode("herb_b", ItemIds.Herb, 1, 40f);
            session.RegisterNode("flower_a", ItemIds.Flower, 1, 40f);
            session.RegisterNode("flower_b", ItemIds.Flower, 1, 40f);
            for (int i = 1; i <= 4; i++) session.RegisterNode("ore_" + i, ItemIds.Ore, 1, 75f);
            return session;
        }

        static GameSession AtStageOne()
        {
            GameSession session = NewSession();
            session.SetExplorationStep(GameSession.ExplorationComplete);
            Assert.AreEqual(Outcome.Ok, session.TryStartStreamQuest());
            Assert.AreEqual(Outcome.Ok, session.Inventory.TryAdd(ItemIds.SpringTonic, 1));
            Assert.AreEqual(Outcome.Ok, session.TryDeliverSpringTonic());
            return session;
        }

        static GameSession AtStageTwo()
        {
            GameSession session = AtStageOne();
            Assert.AreEqual(Outcome.Ok, session.TryStartGardenQuest());
            Assert.AreEqual(Outcome.Ok, session.Inventory.TryAdd(ItemIds.Egg, 1));
            Assert.AreEqual(Outcome.Ok, session.TryDeliverEgg());
            return session;
        }

        static GameSession AtStageThree()
        {
            GameSession session = AtStageTwo();
            Assert.AreEqual(Outcome.Ok, session.TryStartWindmillQuest());
            Assert.AreEqual(Outcome.Ok, session.Inventory.TryAdd(ItemIds.WindmillBraces, 1));
            Assert.AreEqual(Outcome.Ok, session.TryDeliverWindmillBraces());
            return session;
        }

        static void Gather(GameSession session, string id) => Assert.AreEqual(Outcome.Ok, session.TryGather(id), id);

        /// <summary>Everything a rejected action must leave alone.</summary>
        static string Snapshot(GameSession session) =>
            string.Join("|", session.Coins, (int)session.Resonance, (int)session.StreamQuest, (int)session.GardenQuest, (int)session.WindmillQuest,
                session.EndingSeen, session.Inventory.Count(ItemIds.Egg), session.Inventory.Count(ItemIds.WindmillBraces),
                session.Inventory.Count(ItemIds.SpringTonic), (int)session.Chicken);

        static void GiveEveryDeliverable(GameSession session)
        {
            session.Inventory.TryAdd(ItemIds.Egg, 1);
            session.Inventory.TryAdd(ItemIds.WindmillBraces, 1);
            session.Inventory.TryAdd(ItemIds.SpringTonic, 1);
        }

        // ---- Design numbers ------------------------------------------------------------------------------------------------

        [Test]
        public void ResonanceStages_KeepTheirSavedIntegerValues()
        {
            Assert.AreEqual(0, (int)ResonanceStage.SilentValley);
            Assert.AreEqual(1, (int)ResonanceStage.AwakeningStream);
            Assert.AreEqual(2, (int)ResonanceStage.BloomingGarden);
            Assert.AreEqual(3, (int)ResonanceStage.RestoredValley);
        }

        [Test]
        public void NewSession_HasNoLaterQuestProgress()
        {
            GameSession session = new GameSession();
            Assert.AreEqual(QuestStage.NotStarted, session.GardenQuest);
            Assert.AreEqual(QuestStage.NotStarted, session.WindmillQuest);
            Assert.IsFalse(session.EndingSeen);
        }

        // ---- Quest 2: the garden ------------------------------------------------------------------------------------------------

        [Test]
        public void GardenQuest_IsLockedUntilTheStreamIsRestored()
        {
            GameSession session = NewSession();
            session.SetExplorationStep(GameSession.ExplorationComplete);
            session.TryStartStreamQuest();   // active, not finished
            session.Inventory.TryAdd(ItemIds.Egg, 1);
            string before = Snapshot(session);
            List<string> milestones = new List<string>();
            session.Milestone += milestones.Add;

            Assert.AreEqual(Outcome.Locked, session.TryStartGardenQuest());
            Assert.AreEqual(Outcome.Locked, session.TryDeliverEgg());

            Assert.AreEqual(before, Snapshot(session));
            Assert.IsEmpty(milestones);
        }

        [Test]
        public void GardenQuest_StartsOnce_AtStageOne_AndChangesNothingElse()
        {
            GameSession session = AtStageOne();
            int coins = session.Coins;
            int slots = session.Inventory.UsedSlots;
            List<string> milestones = new List<string>();
            session.Milestone += milestones.Add;

            Assert.AreEqual(Outcome.Ok, session.TryStartGardenQuest());
            Assert.AreEqual(Outcome.WrongState, session.TryStartGardenQuest());

            Assert.AreEqual(QuestStage.Active, session.GardenQuest);
            Assert.AreEqual(ResonanceStage.AwakeningStream, session.Resonance, "accepting does not restore anything");
            Assert.AreEqual(coins, session.Coins, "and pays nothing");
            Assert.AreEqual(slots, session.Inventory.UsedSlots, "and takes nothing");
            CollectionAssert.AreEqual(new[] { "garden_started" }, milestones);
        }

        [Test]
        public void EggDelivery_NeedsTheQuestAndARealEgg_AndOtherwiseChangesNothing()
        {
            GameSession session = AtStageOne();
            session.Inventory.TryAdd(ItemIds.Egg, 2);
            string beforeAccepting = Snapshot(session);
            Assert.AreEqual(Outcome.Locked, session.TryDeliverEgg(), "the quest has not been accepted");
            Assert.AreEqual(beforeAccepting, Snapshot(session));

            session.Inventory.TryRemove(ItemIds.Egg, 2);
            session.TryStartGardenQuest();
            string beforeDelivering = Snapshot(session);
            Assert.AreEqual(Outcome.Insufficient, session.TryDeliverEgg(), "no egg in the satchel");
            Assert.AreEqual(beforeDelivering, Snapshot(session));
            Assert.AreEqual(QuestStage.Active, session.GardenQuest);
        }

        [Test]
        public void EggDelivery_ConsumesExactlyOneEgg_PaysOnce_AndMovesToStageTwo()
        {
            GameSession session = AtStageOne();
            session.TryStartGardenQuest();
            session.Inventory.TryAdd(ItemIds.Egg, 3);
            int coins = session.Coins;
            List<string> milestones = new List<string>();
            session.Milestone += milestones.Add;

            Assert.AreEqual(Outcome.Ok, session.TryDeliverEgg());

            Assert.AreEqual(2, session.Inventory.Count(ItemIds.Egg), "exactly one egg is consumed");
            Assert.AreEqual(coins + GameSession.GardenQuestReward, session.Coins);
            Assert.AreEqual(QuestStage.Completed, session.GardenQuest);
            Assert.AreEqual(ResonanceStage.BloomingGarden, session.Resonance);
            CollectionAssert.AreEqual(new[] { "garden_completed" }, milestones);
        }

        [Test]
        public void RapidRepeatedEggDeliveries_PayAndConsumeOnlyOnce()
        {
            GameSession session = AtStageOne();
            session.TryStartGardenQuest();
            session.Inventory.TryAdd(ItemIds.Egg, 5);
            int coins = session.Coins;
            int delivered = 0;
            List<string> milestones = new List<string>();
            session.Milestone += milestones.Add;

            for (int i = 0; i < 10; i++)
                if (session.TryDeliverEgg() == Outcome.Ok) delivered++;

            Assert.AreEqual(1, delivered);
            Assert.AreEqual(4, session.Inventory.Count(ItemIds.Egg));
            Assert.AreEqual(coins + GameSession.GardenQuestReward, session.Coins);
            Assert.AreEqual(1, milestones.Count);
        }

        [Test]
        public void GardenQuest_WorksThroughTheRealHenCycle_AndTheHenStaysUsable()
        {
            GameSession session = AtStageOne();
            session.TryStartGardenQuest();
            Gather(session, "flower_a");
            Assert.AreEqual(Outcome.Ok, session.TryCraft(RecipeBook.ChickenFeed));
            Assert.AreEqual(Outcome.Ok, session.TryFeedChicken());
            session.Tick(GameSession.ChickenProductionSeconds - 0.1f);
            Assert.AreEqual(ChickenPhase.Producing, session.Chicken, "no egg yet");
            Assert.AreEqual(Outcome.Insufficient, session.TryDeliverEgg(), "so there is nothing to hand over");

            session.Tick(0.2f);
            Assert.AreEqual(ChickenPhase.EggReady, session.Chicken);
            Assert.AreEqual(Outcome.Ok, session.TryCollectEgg());
            Assert.AreEqual(Outcome.Ok, session.TryDeliverEgg());
            Assert.AreEqual(ResonanceStage.BloomingGarden, session.Resonance);

            Assert.AreEqual(ChickenPhase.Hungry, session.Chicken, "Clover is ready for more");
            Assert.AreEqual(Outcome.Ok, session.TryFeedChicken(), "the second feed of the three");
            session.Tick(GameSession.ChickenProductionSeconds + 1f);
            Assert.AreEqual(Outcome.Ok, session.TryCollectEgg());
            Assert.AreEqual(1, session.Inventory.Count(ItemIds.Egg), "she keeps laying after the quest");
        }

        [Test]
        public void AnEggCollectedBeforeTheQuest_CompletesItWithoutASoftlock()
        {
            GameSession session = AtStageOne();
            Gather(session, "flower_a");
            session.TryCraft(RecipeBook.ChickenFeed);
            session.TryFeedChicken();
            session.Tick(GameSession.ChickenProductionSeconds + 1f);
            Assert.AreEqual(Outcome.Ok, session.TryCollectEgg());
            Assert.AreEqual(1, session.Inventory.Count(ItemIds.Egg));

            Assert.AreEqual(Outcome.Ok, session.TryStartGardenQuest());
            Assert.AreEqual(1, session.Inventory.Count(ItemIds.Egg), "accepting does not consume it");
            Assert.AreEqual(Outcome.Ok, session.TryDeliverEgg());
            Assert.AreEqual(ResonanceStage.BloomingGarden, session.Resonance);
        }

        // ---- Quest 3: the windmill --------------------------------------------------------------------------------------------

        [Test]
        public void WindmillQuest_IsLockedUntilTheGardenBlooms()
        {
            foreach (GameSession session in new[] { NewSession(), AtStageOne() })
            {
                session.Inventory.TryAdd(ItemIds.WindmillBraces, 1);
                string before = Snapshot(session);

                Assert.AreEqual(Outcome.Locked, session.TryStartWindmillQuest());
                Assert.AreEqual(Outcome.Locked, session.TryDeliverWindmillBraces());

                Assert.AreEqual(before, Snapshot(session));
                Assert.AreEqual(QuestStage.NotStarted, session.WindmillQuest);
            }
        }

        [Test]
        public void WindmillQuest_StartsOnce_AtStageTwo()
        {
            GameSession session = AtStageTwo();
            List<string> milestones = new List<string>();
            session.Milestone += milestones.Add;

            Assert.AreEqual(Outcome.Ok, session.TryStartWindmillQuest());
            Assert.AreEqual(Outcome.WrongState, session.TryStartWindmillQuest());

            Assert.AreEqual(QuestStage.Active, session.WindmillQuest);
            Assert.AreEqual(ResonanceStage.BloomingGarden, session.Resonance);
            CollectionAssert.AreEqual(new[] { "windmill_started" }, milestones);
        }

        [Test]
        public void BracesDelivery_NeedsTheQuestAndRealBraces_AndOtherwiseChangesNothing()
        {
            GameSession session = AtStageTwo();
            session.Inventory.TryAdd(ItemIds.WindmillBraces, 1);
            string beforeAccepting = Snapshot(session);
            Assert.AreEqual(Outcome.Locked, session.TryDeliverWindmillBraces());
            Assert.AreEqual(beforeAccepting, Snapshot(session));

            session.Inventory.TryRemove(ItemIds.WindmillBraces, 1);
            session.TryStartWindmillQuest();
            string beforeDelivering = Snapshot(session);
            Assert.AreEqual(Outcome.Insufficient, session.TryDeliverWindmillBraces());
            Assert.AreEqual(beforeDelivering, Snapshot(session));
        }

        [Test]
        public void BracesDelivery_ConsumesExactlyOneSet_PaysOnce_AndMovesToStageThree()
        {
            GameSession session = AtStageTwo();
            session.TryStartWindmillQuest();
            session.Inventory.TryAdd(ItemIds.WindmillBraces, 2);
            int coins = session.Coins;
            List<string> milestones = new List<string>();
            session.Milestone += milestones.Add;

            Assert.AreEqual(Outcome.Ok, session.TryDeliverWindmillBraces());
            Assert.AreEqual(Outcome.WrongState, session.TryDeliverWindmillBraces());

            Assert.AreEqual(1, session.Inventory.Count(ItemIds.WindmillBraces), "exactly one set is consumed");
            Assert.AreEqual(coins + GameSession.WindmillQuestReward, session.Coins);
            Assert.AreEqual(QuestStage.Completed, session.WindmillQuest);
            Assert.AreEqual(ResonanceStage.RestoredValley, session.Resonance);
            CollectionAssert.AreEqual(new[] { "windmill_completed" }, milestones);
        }

        [Test]
        public void BracesCraftedAtAnyEarlierPoint_StillCompleteTheQuest()
        {
            GameSession session = AtStageOne();
            for (int i = 1; i <= 4; i++) Gather(session, "ore_" + i);
            Assert.AreEqual(Outcome.Ok, session.TryCraft(RecipeBook.WindmillBraces), "crafted long before the quest exists");
            Assert.AreEqual(1, session.Inventory.Count(ItemIds.WindmillBraces));

            session.TryStartGardenQuest();
            session.Inventory.TryAdd(ItemIds.Egg, 1);
            session.TryDeliverEgg();
            Assert.AreEqual(Outcome.Ok, session.TryStartWindmillQuest());
            Assert.AreEqual(1, session.Inventory.Count(ItemIds.WindmillBraces), "accepting does not consume them");
            Assert.AreEqual(Outcome.Ok, session.TryDeliverWindmillBraces());
            Assert.AreEqual(ResonanceStage.RestoredValley, session.Resonance);
            Assert.AreEqual(0, session.Inventory.Count(ItemIds.WindmillBraces));
        }

        [Test]
        public void TheWindmillNeedsOnlyRenewableOre_EvenWithASingleNode()
        {
            GameSession session = new GameSession();
            session.RegisterNode("lonely_ore", ItemIds.Ore, 1, 75f);

            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(Outcome.Ok, session.TryGather("lonely_ore"), "ore " + (i + 1));
                Assert.AreEqual(Outcome.NotReady, session.TryGather("lonely_ore"), "it is regrowing, not gone");
                session.Tick(75f);
            }

            Assert.AreEqual(4, session.Inventory.Count(ItemIds.Ore));
            Assert.AreEqual(Outcome.Ok, session.TryCraft(RecipeBook.WindmillBraces));
            Assert.AreEqual(1, session.Inventory.Count(ItemIds.WindmillBraces));
        }

        // ---- The ending -------------------------------------------------------------------------------------------------------

        [Test]
        public void Ending_NeedsTheRestoredValley_AndIsRecordedOnce()
        {
            GameSession early = AtStageTwo();
            Assert.AreEqual(Outcome.Locked, early.TryCompleteEnding());
            Assert.IsFalse(early.EndingSeen);

            GameSession session = AtStageThree();
            List<string> milestones = new List<string>();
            session.Milestone += milestones.Add;
            Assert.IsFalse(session.EndingSeen, "restoring the windmill is not the ending");

            Assert.AreEqual(Outcome.Ok, session.TryCompleteEnding());
            Assert.AreEqual(Outcome.WrongState, session.TryCompleteEnding());

            Assert.IsTrue(session.EndingSeen);
            CollectionAssert.AreEqual(new[] { "ending_seen" }, milestones);
            Assert.AreEqual(ResonanceStage.RestoredValley, session.Resonance, "the ending changes no stage");
        }

        // ---- Invariants across the whole game ---------------------------------------------------------------------------------

        [Test]
        public void EveryOutOfOrderAttempt_ChangesNothing()
        {
            // At each stage, every action that does not belong to it is rejected and leaves the whole state untouched,
            // even with every deliverable item in the satchel.
            GameSession stage0 = NewSession();
            stage0.SetExplorationStep(GameSession.ExplorationComplete);
            GiveEveryDeliverable(stage0);
            AssertAllRejected(stage0, "stage 0", gardenStart: true, egg: true, windmillStart: true, braces: true, ending: true);

            GameSession stage1 = AtStageOne();
            GiveEveryDeliverable(stage1);
            AssertAllRejected(stage1, "stage 1", gardenStart: false, egg: true, windmillStart: true, braces: true, ending: true);

            GameSession stage1Active = AtStageOne();
            stage1Active.TryStartGardenQuest();
            GiveEveryDeliverable(stage1Active);
            stage1Active.Inventory.TryRemove(ItemIds.Egg, 1);   // keep the egg out so the check below cannot complete the quest
            AssertAllRejected(stage1Active, "stage 1 with the garden quest active", gardenStart: true, egg: false, windmillStart: true, braces: true, ending: true);

            GameSession stage2 = AtStageTwo();
            GiveEveryDeliverable(stage2);
            AssertAllRejected(stage2, "stage 2", gardenStart: true, egg: true, windmillStart: false, braces: true, ending: true);

            GameSession stage3 = AtStageThree();
            GiveEveryDeliverable(stage3);
            Assert.AreEqual(Outcome.WrongState, stage3.TryStartGardenQuest());
            Assert.AreEqual(Outcome.WrongState, stage3.TryDeliverEgg());
            Assert.AreEqual(Outcome.WrongState, stage3.TryStartWindmillQuest());
            Assert.AreEqual(Outcome.WrongState, stage3.TryDeliverWindmillBraces());
            Assert.AreEqual(Outcome.WrongState, stage3.TryDeliverSpringTonic());
            Assert.AreEqual(Outcome.WrongState, stage3.TryStartStreamQuest());
        }

        static void AssertAllRejected(GameSession session, string label, bool gardenStart, bool egg, bool windmillStart, bool braces, bool ending)
        {
            string before = Snapshot(session);
            if (gardenStart) Assert.AreNotEqual(Outcome.Ok, session.TryStartGardenQuest(), label + ": start garden");
            if (egg) Assert.AreNotEqual(Outcome.Ok, session.TryDeliverEgg(), label + ": deliver egg");
            if (windmillStart) Assert.AreNotEqual(Outcome.Ok, session.TryStartWindmillQuest(), label + ": start windmill");
            if (braces) Assert.AreNotEqual(Outcome.Ok, session.TryDeliverWindmillBraces(), label + ": deliver braces");
            if (ending) Assert.AreNotEqual(Outcome.Ok, session.TryCompleteEnding(), label + ": ending");
            Assert.AreEqual(before, Snapshot(session), label + ": nothing changed");
        }

        [Test]
        public void FullPlaythrough_OnlyRenewableResourcesAndPublicActions_ReachesTheEnding_InOrder()
        {
            GameSession session = NewSession();
            List<ResonanceStage> stages = new List<ResonanceStage>();
            List<string> milestones = new List<string>();
            session.Changed += () => stages.Add(session.Resonance);
            session.Milestone += milestones.Add;

            session.SetExplorationStep(GameSession.ExplorationComplete);

            // The stream
            Assert.AreEqual(Outcome.Ok, session.TryStartStreamQuest());
            Gather(session, "herb_a");
            Gather(session, "herb_b");
            Gather(session, "flower_a");
            Assert.AreEqual(Outcome.Ok, session.TryCraft(RecipeBook.SpringTonic));
            Assert.AreEqual(Outcome.Ok, session.TryDeliverSpringTonic());

            // The garden
            Assert.AreEqual(Outcome.Ok, session.TryStartGardenQuest());
            Gather(session, "flower_b");
            Assert.AreEqual(Outcome.Ok, session.TryCraft(RecipeBook.ChickenFeed));
            Assert.AreEqual(Outcome.Ok, session.TryFeedChicken());
            session.Tick(GameSession.ChickenProductionSeconds);
            Assert.AreEqual(Outcome.Ok, session.TryCollectEgg());
            Assert.AreEqual(Outcome.Ok, session.TryDeliverEgg());

            // The windmill
            Assert.AreEqual(Outcome.Ok, session.TryStartWindmillQuest());
            for (int i = 1; i <= 4; i++) Gather(session, "ore_" + i);
            Assert.AreEqual(Outcome.Ok, session.TryCraft(RecipeBook.WindmillBraces));
            Assert.AreEqual(Outcome.Ok, session.TryDeliverWindmillBraces());

            // The ending
            Assert.AreEqual(Outcome.Ok, session.TryCompleteEnding());

            Assert.AreEqual(ResonanceStage.RestoredValley, session.Resonance);
            Assert.IsTrue(session.EndingSeen);
            Assert.AreEqual(GameSession.StartingCoins + GameSession.StreamQuestReward + GameSession.GardenQuestReward + GameSession.WindmillQuestReward, session.Coins);
            CollectionAssert.AreEqual(new[]
            {
                "quest_started", "quest_completed", "garden_started", "garden_completed", "windmill_started", "windmill_completed", "ending_seen"
            }, milestones);

            // The stage only ever moved up, one step at a time
            for (int i = 1; i < stages.Count; i++)
            {
                Assert.GreaterOrEqual((int)stages[i], (int)stages[i - 1], "the stage never goes back");
                Assert.LessOrEqual((int)stages[i] - (int)stages[i - 1], 1, "the stage never skips one");
            }

            // Everything used up except the two spare portions of feed
            Assert.AreEqual(1, session.Inventory.UsedSlots);
            Assert.AreEqual(2, session.Inventory.Count(ItemIds.ChickenFeed));
        }

        // ---- Near the limits ---------------------------------------------------------------------------------------------------------

        [Test]
        public void ANearlyFullEggStack_StillDelivers_AndALayingHenNeverLosesAnEgg()
        {
            GameSession session = AtStageOne();
            session.TryStartGardenQuest();
            session.Inventory.TryAdd(ItemIds.Egg, session.Inventory.StackLimit);
            session.Inventory.TryAdd(ItemIds.ChickenFeed, 1);
            session.TryFeedChicken();
            session.Tick(GameSession.ChickenProductionSeconds + 1f);
            Assert.AreEqual(ChickenPhase.EggReady, session.Chicken);

            Assert.AreEqual(Outcome.NoRoom, session.TryCollectEgg(), "a full stack leaves the egg in the nest");
            Assert.AreEqual(ChickenPhase.EggReady, session.Chicken);
            Assert.AreEqual(session.Inventory.StackLimit, session.Inventory.Count(ItemIds.Egg));

            Assert.AreEqual(Outcome.Ok, session.TryDeliverEgg(), "delivering frees a place");
            Assert.AreEqual(session.Inventory.StackLimit - 1, session.Inventory.Count(ItemIds.Egg));
            Assert.AreEqual(Outcome.Ok, session.TryCollectEgg(), "and now the waiting egg fits");
            Assert.AreEqual(session.Inventory.StackLimit, session.Inventory.Count(ItemIds.Egg));
        }

        [Test]
        public void RewardsAtTheCoinCap_AreClamped_NeverLostAndNeverBlockProgress()
        {
            GameSession session = AtStageOne();
            session.TryStartGardenQuest();
            session.TryAddCoins(GameSession.MaxCoins - session.Coins - 5);
            Assert.AreEqual(GameSession.MaxCoins - 5, session.Coins);
            session.Inventory.TryAdd(ItemIds.Egg, 1);

            Assert.AreEqual(Outcome.Ok, session.TryDeliverEgg());

            Assert.AreEqual(GameSession.MaxCoins, session.Coins);
            Assert.AreEqual(ResonanceStage.BloomingGarden, session.Resonance);
        }

        [Test]
        public void AfterEveryRestoration_GatheringCraftingAndTheHenKeepWorking()
        {
            GameSession session = AtStageThree();
            session.TryCompleteEnding();

            Gather(session, "herb_a");
            Gather(session, "herb_b");
            Gather(session, "flower_a");
            Assert.AreEqual(Outcome.Ok, session.TryCraft(RecipeBook.SpringTonic));
            Assert.AreEqual(Outcome.NotReady, session.TryGather("herb_a"), "a node still regrows after the story ends");
            session.Tick(40f);
            Assert.AreEqual(Outcome.Ok, session.TryGather("herb_a"));

            Gather(session, "flower_b");
            Assert.AreEqual(Outcome.Ok, session.TryCraft(RecipeBook.ChickenFeed));
            Assert.AreEqual(Outcome.Ok, session.TryFeedChicken());
            session.Tick(GameSession.ChickenProductionSeconds);
            Assert.AreEqual(Outcome.Ok, session.TryCollectEgg());

            Assert.AreEqual(ResonanceStage.RestoredValley, session.Resonance, "nothing here moves the stage");
            Assert.AreEqual(Outcome.WrongState, session.TryDeliverEgg(), "and no leftover item pays a reward again");
        }
    }
}
