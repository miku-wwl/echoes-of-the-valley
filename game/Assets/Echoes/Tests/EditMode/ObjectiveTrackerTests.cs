using NUnit.Framework;

namespace Echoes.Tests
{
    public class ObjectiveTrackerTests
    {
        [Test]
        public void StartsAtTheFirstStep()
        {
            ObjectiveTracker tracker = new ObjectiveTracker();
            Assert.AreEqual(ObjectiveStep.MeetVillager, tracker.Step);
            Assert.IsFalse(tracker.IsComplete);
        }

        [Test]
        public void AdvancesInOrderUntilComplete()
        {
            ObjectiveTracker tracker = new ObjectiveTracker();

            Assert.IsTrue(tracker.TryMarkVillagerMet());
            Assert.AreEqual(ObjectiveStep.InspectSpring, tracker.Step);
            Assert.IsTrue(tracker.TryMarkSpringInspected());
            Assert.AreEqual(ObjectiveStep.ReturnToHeart, tracker.Step);
            Assert.IsTrue(tracker.TryMarkHeartReached());
            Assert.AreEqual(ObjectiveStep.Complete, tracker.Step);
            Assert.IsTrue(tracker.IsComplete);
        }

        [Test]
        public void OutOfOrderMarksAreRejectedWithoutChangingState()
        {
            ObjectiveTracker tracker = new ObjectiveTracker();

            Assert.IsFalse(tracker.TryMarkSpringInspected());
            Assert.IsFalse(tracker.TryMarkHeartReached());
            Assert.AreEqual(ObjectiveStep.MeetVillager, tracker.Step);

            tracker.TryMarkVillagerMet();
            Assert.IsFalse(tracker.TryMarkHeartReached());
            Assert.AreEqual(ObjectiveStep.InspectSpring, tracker.Step);
        }

        [Test]
        public void RepeatedMarksNeverAdvanceTwice()
        {
            ObjectiveTracker tracker = new ObjectiveTracker();
            Assert.IsTrue(tracker.TryMarkVillagerMet());
            Assert.IsFalse(tracker.TryMarkVillagerMet());
            Assert.AreEqual(ObjectiveStep.InspectSpring, tracker.Step);
        }

        [Test]
        public void CompleteIsTerminal()
        {
            ObjectiveTracker tracker = new ObjectiveTracker();
            tracker.TryMarkVillagerMet();
            tracker.TryMarkSpringInspected();
            tracker.TryMarkHeartReached();

            Assert.IsFalse(tracker.TryMarkVillagerMet());
            Assert.IsFalse(tracker.TryMarkSpringInspected());
            Assert.IsFalse(tracker.TryMarkHeartReached());
            Assert.AreEqual(ObjectiveStep.Complete, tracker.Step);
        }

        [Test]
        public void ResetStartsANewGame()
        {
            ObjectiveTracker tracker = new ObjectiveTracker();
            tracker.TryMarkVillagerMet();
            tracker.TryMarkSpringInspected();

            tracker.Reset();

            Assert.AreEqual(ObjectiveStep.MeetVillager, tracker.Step);
            Assert.IsTrue(tracker.TryMarkVillagerMet());
        }

        [Test]
        public void EveryStepHasPlayerFacingText()
        {
            foreach (ObjectiveStep step in System.Enum.GetValues(typeof(ObjectiveStep)))
                Assert.IsFalse(string.IsNullOrWhiteSpace(ObjectiveTracker.Describe(step)), step.ToString());
        }
    }
}
