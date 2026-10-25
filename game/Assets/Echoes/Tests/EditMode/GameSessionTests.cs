using System.Collections.Generic;
using NUnit.Framework;

namespace Echoes.Tests
{
    /// <summary>Gathering, the hen and the stream quest, tested on the plain GameSession with no scene.</summary>
    public class GameSessionTests
    {
        static GameSession NewSession()
        {
            GameSession session = new GameSession();
            session.RegisterNode("herb_a", ItemIds.Herb, 1, 40f);
            session.RegisterNode("herb_b", ItemIds.Herb, 1, 40f);
            session.RegisterNode("flower_a", ItemIds.Flower, 1, 40f);
            session.RegisterNode("ore_a", ItemIds.Ore, 1, 75f);
            return session;
        }

        static void FinishExploration(GameSession session) => session.SetExplorationStep(GameSession.ExplorationComplete);

        static void GiveTonic(GameSession session) => Assert.AreEqual(Outcome.Ok, session.Inventory.TryAdd(ItemIds.SpringTonic, 1));

        // ---- Gathering ---------------------------------------------------------------------------------------------

        [Test]
        public void NewSession_StartsClean()
        {
            GameSession session = new GameSession();
            Assert.AreEqual(GameSession.StartingCoins, session.Coins);
            Assert.AreEqual(0, session.Inventory.UsedSlots);
            Assert.AreEqual(ChickenPhase.Hungry, session.Chicken);
            Assert.AreEqual(StreamQuestStage.NotStarted, session.StreamQuest);
            Assert.AreEqual(ResonanceStage.SilentValley, session.Resonance);
            Assert.AreEqual(0, session.ExplorationStep);
        }

        [Test]
        public void RegisterNode_RejectsBadDefinitions()
        {
            GameSession session = new GameSession();
            Assert.AreEqual(Outcome.InvalidAmount, session.RegisterNode("", ItemIds.Herb, 1, 10f));
            Assert.AreEqual(Outcome.InvalidAmount, session.RegisterNode("n", "mystery", 1, 10f));
            Assert.AreEqual(Outcome.InvalidAmount, session.RegisterNode("n", ItemIds.Herb, 0, 10f));
            Assert.AreEqual(Outcome.InvalidAmount, session.RegisterNode("n", ItemIds.Herb, 1, -1f));
            Assert.AreEqual(Outcome.InvalidAmount, session.RegisterNode("n", ItemIds.Herb, 1, float.NaN));
            Assert.AreEqual(0, session.NodeCount);
        }

        [Test]
        public void Gather_GrantsTheItem_AndStartsTheCooldown()
        {
            GameSession session = NewSession();

            Assert.AreEqual(Outcome.Ok, session.TryGather("herb_a"));

            Assert.AreEqual(1, session.Inventory.Count(ItemIds.Herb));
            Assert.IsFalse(session.IsNodeReady("herb_a"));
            Assert.AreEqual(40f, session.NodeRemaining("herb_a"), 0.001f);
            Assert.IsTrue(session.IsNodeReady("herb_b"), "other nodes are untouched");
        }

        [Test]
        public void Gather_DuringTheCooldown_IsRejected_AndGivesNothing()
        {
            GameSession session = NewSession();
            session.TryGather("herb_a");

            Assert.AreEqual(Outcome.NotReady, session.TryGather("herb_a"));
            Assert.AreEqual(Outcome.NotReady, session.TryGather("herb_a"), "mashing E does not stack extra items");
            Assert.AreEqual(1, session.Inventory.Count(ItemIds.Herb));
        }

        [Test]
        public void Nodes_Regenerate_AfterTheirCooldown()
        {
            GameSession session = NewSession();
            session.TryGather("herb_a");

            session.Tick(39f);
            Assert.AreEqual(Outcome.NotReady, session.TryGather("herb_a"));
            session.Tick(1.5f);
            Assert.IsTrue(session.IsNodeReady("herb_a"));
            Assert.AreEqual(Outcome.Ok, session.TryGather("herb_a"));
            Assert.AreEqual(2, session.Inventory.Count(ItemIds.Herb));
        }

        [Test]
        public void Gather_WithoutRoom_KeepsTheNodeReady_AndTakesNothing()
        {
            GameSession session = NewSession();
            session.Inventory.TryAdd(ItemIds.Herb, Inventory.DefaultStackLimit);

            Assert.AreEqual(Outcome.NoRoom, session.TryGather("herb_a"));

            Assert.AreEqual(Inventory.DefaultStackLimit, session.Inventory.Count(ItemIds.Herb));
            Assert.IsTrue(session.IsNodeReady("herb_a"), "a failed pick must not burn the node");
        }

        [Test]
        public void Gather_UnknownNode_IsRejected()
        {
            GameSession session = NewSession();
            Assert.AreEqual(Outcome.UnknownTarget, session.TryGather("herb_zzz"));
            Assert.AreEqual(Outcome.UnknownTarget, session.TryGather(null));
        }

        [Test]
        public void Gather_NotifiesOnce_WithTheItemName()
        {
            GameSession session = NewSession();
            List<string> messages = new List<string>();
            int changes = 0;
            session.Message += messages.Add;
            session.Changed += () => changes++;

            session.TryGather("flower_a");

            Assert.AreEqual(1, messages.Count);
            StringAssert.Contains("Dawn Blossom", messages[0]);
            Assert.AreEqual(1, changes, "inventory and node change under a single notification");
        }

        [Test]
        public void Tick_IgnoresNonsenseDeltas()
        {
            GameSession session = NewSession();
            session.TryGather("herb_a");
            session.Tick(-5f);
            session.Tick(0f);
            session.Tick(float.NaN);
            session.Tick(float.PositiveInfinity);
            Assert.AreEqual(40f, session.NodeRemaining("herb_a"), 0.001f);
        }

        // ---- The hen -----------------------------------------------------------------------------------------------

        [Test]
        public void Chicken_StartsHungry_AndCannotBeFedWithoutFeed()
        {
            GameSession session = NewSession();
            Assert.AreEqual(ChickenPhase.Hungry, session.Chicken);
            Assert.AreEqual(Outcome.Insufficient, session.TryFeedChicken());
            Assert.AreEqual(ChickenPhase.Hungry, session.Chicken);
        }

        [Test]
        public void Feeding_ConsumesExactlyOneFeed_AndStartsProduction()
        {
            GameSession session = NewSession();
            session.Inventory.TryAdd(ItemIds.ChickenFeed, 3);

            Assert.AreEqual(Outcome.Ok, session.TryFeedChicken());

            Assert.AreEqual(2, session.Inventory.Count(ItemIds.ChickenFeed));
            Assert.AreEqual(ChickenPhase.Producing, session.Chicken);
            Assert.AreEqual(GameSession.ChickenProductionSeconds, session.ChickenRemaining, 0.001f);
        }

        [Test]
        public void FeedingTwice_DoesNotConsumeASecondFeed()
        {
            GameSession session = NewSession();
            session.Inventory.TryAdd(ItemIds.ChickenFeed, 3);
            session.TryFeedChicken();

            Assert.AreEqual(Outcome.WrongState, session.TryFeedChicken());
            Assert.AreEqual(2, session.Inventory.Count(ItemIds.ChickenFeed));
        }

        [Test]
        public void Egg_IsReady_OnlyAfterTheProductionTime()
        {
            GameSession session = NewSession();
            session.Inventory.TryAdd(ItemIds.ChickenFeed, 1);
            session.TryFeedChicken();

            session.Tick(GameSession.ChickenProductionSeconds - 1f);
            Assert.AreEqual(ChickenPhase.Producing, session.Chicken);
            Assert.AreEqual(Outcome.WrongState, session.TryCollectEgg(), "too early");

            session.Tick(1.01f);
            Assert.AreEqual(ChickenPhase.EggReady, session.Chicken);
        }

        [Test]
        public void ProductionTime_IsShortEnoughToPlay()
        {
            Assert.LessOrEqual(GameSession.ChickenProductionSeconds, 30f, "no multi-minute waits");
        }

        [Test]
        public void CollectingTheEgg_GivesOneEgg_AndCannotBeRepeated()
        {
            GameSession session = NewSession();
            session.Inventory.TryAdd(ItemIds.ChickenFeed, 1);
            session.TryFeedChicken();
            session.Tick(GameSession.ChickenProductionSeconds + 1f);

            Assert.AreEqual(Outcome.Ok, session.TryCollectEgg());
            Assert.AreEqual(1, session.Inventory.Count(ItemIds.Egg));
            Assert.AreEqual(ChickenPhase.Hungry, session.Chicken);

            Assert.AreEqual(Outcome.WrongState, session.TryCollectEgg(), "the egg is gone");
            Assert.AreEqual(1, session.Inventory.Count(ItemIds.Egg));
        }

        [Test]
        public void CollectingTheEgg_WithAFullEggStack_KeepsTheEggInTheNest()
        {
            GameSession session = NewSession();
            session.Inventory.TryAdd(ItemIds.Egg, Inventory.DefaultStackLimit);
            session.Inventory.TryAdd(ItemIds.ChickenFeed, 1);
            session.TryFeedChicken();
            session.Tick(GameSession.ChickenProductionSeconds + 1f);

            Assert.AreEqual(Outcome.NoRoom, session.TryCollectEgg());
            Assert.AreEqual(ChickenPhase.EggReady, session.Chicken, "the egg waits instead of vanishing");

            session.Inventory.TryRemove(ItemIds.Egg, 1);
            Assert.AreEqual(Outcome.Ok, session.TryCollectEgg());
        }

        [Test]
        public void Hen_CanAlwaysBeFedAgain_BecauseFeedIsCraftableFromRenewableBlossoms()
        {
            GameSession session = NewSession();
            session.TryGather("flower_a");
            Assert.AreEqual(Outcome.Ok, session.TryCraft(RecipeBook.ChickenFeed));
            Assert.AreEqual(Outcome.Ok, session.TryFeedChicken());
            session.Tick(GameSession.ChickenProductionSeconds + 1f);
            Assert.AreEqual(Outcome.Ok, session.TryCollectEgg());

            session.Tick(41f);   // the blossom grows back
            Assert.AreEqual(Outcome.Ok, session.TryGather("flower_a"));
        }

        // ---- The stream quest and ResonanceStage ----------------------------------------------------------------------

        [Test]
        public void QuestCannotStart_BeforeTheOpeningObjectiveIsDone()
        {
            GameSession session = NewSession();
            Assert.AreEqual(Outcome.Locked, session.TryStartStreamQuest());
            Assert.AreEqual(StreamQuestStage.NotStarted, session.StreamQuest);

            FinishExploration(session);
            Assert.AreEqual(Outcome.Ok, session.TryStartStreamQuest());
            Assert.AreEqual(StreamQuestStage.Active, session.StreamQuest);
            Assert.AreEqual(Outcome.WrongState, session.TryStartStreamQuest(), "a quest starts once");
        }

        [Test]
        public void ExplorationProgress_OnlyMovesForward()
        {
            GameSession session = NewSession();
            session.SetExplorationStep(2);
            session.SetExplorationStep(1);
            session.SetExplorationStep(-4);
            session.SetExplorationStep(9);
            Assert.AreEqual(2, session.ExplorationStep);
        }

        [Test]
        public void Restoration_IsRejected_WithoutAnActiveQuest()
        {
            GameSession session = NewSession();
            GiveTonic(session);
            Assert.AreEqual(Outcome.Locked, session.TryDeliverSpringTonic());

            FinishExploration(session);
            Assert.AreEqual(Outcome.Locked, session.TryDeliverSpringTonic(), "finishing the intro is not accepting the quest");
            Assert.AreEqual(ResonanceStage.SilentValley, session.Resonance);
            Assert.AreEqual(1, session.Inventory.Count(ItemIds.SpringTonic));
        }

        [Test]
        public void Restoration_IsRejected_WithoutTheRequiredItem()
        {
            GameSession session = NewSession();
            FinishExploration(session);
            session.TryStartStreamQuest();
            session.Inventory.TryAdd(ItemIds.Herb, 2);   // the ingredients are not the tonic

            Assert.AreEqual(Outcome.Insufficient, session.TryDeliverSpringTonic());

            Assert.AreEqual(StreamQuestStage.Active, session.StreamQuest);
            Assert.AreEqual(ResonanceStage.SilentValley, session.Resonance);
            Assert.AreEqual(GameSession.StartingCoins, session.Coins);
        }

        [Test]
        public void Restoration_ConsumesExactlyOneTonic_PaysOnce_AndMovesToStageOne()
        {
            GameSession session = NewSession();
            FinishExploration(session);
            session.TryStartStreamQuest();
            session.Inventory.TryAdd(ItemIds.SpringTonic, 2);

            Assert.AreEqual(Outcome.Ok, session.TryDeliverSpringTonic());

            Assert.AreEqual(1, session.Inventory.Count(ItemIds.SpringTonic), "exactly one tonic is consumed");
            Assert.AreEqual(GameSession.StartingCoins + GameSession.StreamQuestReward, session.Coins);
            Assert.AreEqual(StreamQuestStage.Completed, session.StreamQuest);
            Assert.AreEqual(ResonanceStage.AwakeningStream, session.Resonance);
        }

        [Test]
        public void Restoration_CannotRepeat_NoSecondRewardOrConsumption()
        {
            GameSession session = NewSession();
            FinishExploration(session);
            session.TryStartStreamQuest();
            session.Inventory.TryAdd(ItemIds.SpringTonic, 2);
            session.TryDeliverSpringTonic();
            int coins = session.Coins;

            Assert.AreEqual(Outcome.WrongState, session.TryDeliverSpringTonic());
            Assert.AreEqual(Outcome.WrongState, session.TryDeliverSpringTonic());

            Assert.AreEqual(coins, session.Coins);
            Assert.AreEqual(1, session.Inventory.Count(ItemIds.SpringTonic));
            Assert.AreEqual(ResonanceStage.AwakeningStream, session.Resonance);
        }

        [Test]
        public void Restoration_FiresItsMilestonesOnce()
        {
            GameSession session = NewSession();
            List<string> milestones = new List<string>();
            session.Milestone += milestones.Add;

            FinishExploration(session);
            session.TryStartStreamQuest();
            GiveTonic(session);
            session.TryDeliverSpringTonic();
            session.TryDeliverSpringTonic();
            session.TryStartStreamQuest();

            CollectionAssert.AreEqual(new[] { "quest_started", "quest_completed" }, milestones);
        }

        [Test]
        public void Coins_AreValidated()
        {
            GameSession session = new GameSession();
            Assert.AreEqual(Outcome.InvalidAmount, session.TryAddCoins(0));
            Assert.AreEqual(Outcome.InvalidAmount, session.TryAddCoins(-5));
            Assert.AreEqual(Outcome.InvalidAmount, session.TrySpendCoins(0));
            Assert.AreEqual(Outcome.Insufficient, session.TrySpendCoins(GameSession.StartingCoins + 1));
            Assert.AreEqual(Outcome.NoRoom, session.TryAddCoins(GameSession.MaxCoins));
            Assert.AreEqual(GameSession.StartingCoins, session.Coins);

            Assert.AreEqual(Outcome.Ok, session.TrySpendCoins(5));
            Assert.AreEqual(Outcome.Ok, session.TryAddCoins(10));
            Assert.AreEqual(GameSession.StartingCoins + 5, session.Coins);
        }

        // ---- No softlocks ------------------------------------------------------------------------------------------------

        [Test]
        public void Quest_CanStillBeFinished_AfterEveryNodeHasBeenEmptied()
        {
            GameSession session = NewSession();
            FinishExploration(session);
            session.TryStartStreamQuest();

            // Pick everything, throw away the haul, and the quest must still be reachable once the nodes regrow.
            foreach (string id in new[] { "herb_a", "herb_b", "flower_a", "ore_a" }) session.TryGather(id);
            session.Inventory.TryRemove(ItemIds.Herb, 2);
            session.Inventory.TryRemove(ItemIds.Flower, 1);
            Assert.AreEqual(Outcome.NotReady, session.TryGather("herb_a"));

            session.Tick(41f);
            Assert.AreEqual(Outcome.Ok, session.TryGather("herb_a"));
            Assert.AreEqual(Outcome.Ok, session.TryGather("herb_b"));
            Assert.AreEqual(Outcome.Ok, session.TryGather("flower_a"));
            Assert.AreEqual(Outcome.Ok, session.TryCraft(RecipeBook.SpringTonic));
            Assert.AreEqual(Outcome.Ok, session.TryDeliverSpringTonic());
            Assert.AreEqual(ResonanceStage.AwakeningStream, session.Resonance);
        }

        [Test]
        public void Quest_CanBeFinished_EvenWithABagStuffedWithEverythingElse()
        {
            GameSession session = NewSession();
            FinishExploration(session);
            session.TryStartStreamQuest();

            foreach (string id in new[] { ItemIds.Ore, ItemIds.ChickenFeed, ItemIds.Egg, ItemIds.WindmillBraces })
                session.Inventory.TryAdd(id, Inventory.DefaultStackLimit);

            // herbs, a blossom and the tonic itself all still have a slot
            Assert.AreEqual(Outcome.Ok, session.TryGather("herb_a"));
            Assert.AreEqual(Outcome.Ok, session.TryGather("herb_b"));
            Assert.AreEqual(Outcome.Ok, session.TryGather("flower_a"));
            Assert.AreEqual(Outcome.Ok, session.TryCraft(RecipeBook.SpringTonic));
            Assert.AreEqual(Outcome.Ok, session.TryDeliverSpringTonic());
        }

        [Test]
        public void Quest_CanBeFinished_WithEveryOtherItemTypeAlreadyInTheBag()
        {
            GameSession session = new GameSession();
            FinishExploration(session);
            session.TryStartStreamQuest();
            session.RegisterNode("herb_a", ItemIds.Herb, 1, 40f);
            session.RegisterNode("herb_b", ItemIds.Herb, 1, 40f);
            session.RegisterNode("flower_a", ItemIds.Flower, 1, 40f);

            // the four other item types take four of the eight slots; herb, blossom and tonic fit in the rest
            foreach (string id in new[] { ItemIds.Ore, ItemIds.ChickenFeed, ItemIds.Egg, ItemIds.WindmillBraces })
                session.Inventory.TryAdd(id, 1);
            session.TryGather("herb_a");
            session.TryGather("herb_b");
            session.TryGather("flower_a");

            Assert.AreEqual(Outcome.Ok, session.TryCraft(RecipeBook.SpringTonic));
            Assert.AreEqual(Outcome.Ok, session.TryDeliverSpringTonic());
        }

        [Test]
        public void EveryRequiredIngredient_IsOfferedByRenewableNodes()
        {
            // The scene generator places 6 herb, 5 blossom and 4 ore nodes; the model must make every one of them renewable.
            GameSession session = new GameSession();
            session.RegisterNode("ore_a", ItemIds.Ore, 1, 75f);
            session.TryGather("ore_a");
            Assert.Greater(session.NodeRemaining("ore_a"), 0f);
            session.Tick(75f);
            Assert.IsTrue(session.IsNodeReady("ore_a"), "even the slowest node comes back");
        }
    }
}
