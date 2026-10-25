using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Echoes.Tests
{
    public class InteractionRangeTests
    {
        sealed class Stub : IInteractable
        {
            public string Prompt => "stub";
            public Vector3 Position { get; set; }
            public float Range { get; set; } = 2f;
            public bool IsInRange(Vector3 from) => Interactable.IsWithinRange(from, Position, Range);
            public void Interact(InteractionDetector interactor) { }
        }

        [Test]
        public void InsideRangeIsAccepted_OutsideIsRejected()
        {
            Assert.IsTrue(Interactable.IsWithinRange(Vector3.zero, new Vector3(1.9f, 0f, 0f), 2f));
            Assert.IsTrue(Interactable.IsWithinRange(Vector3.zero, new Vector3(2f, 0f, 0f), 2f), "exactly at range counts as in range");
            Assert.IsFalse(Interactable.IsWithinRange(Vector3.zero, new Vector3(2.01f, 0f, 0f), 2f));
            Assert.IsFalse(Interactable.IsWithinRange(Vector3.zero, new Vector3(1.5f, 0f, 1.5f), 2f), "diagonal distance is 2.12");
        }

        [Test]
        public void HeightDifferenceBeyondToleranceBlocksInteraction()
        {
            Assert.IsTrue(Interactable.IsWithinRange(Vector3.zero, new Vector3(0f, Interactable.VerticalTolerance, 0f), 2f));
            Assert.IsFalse(Interactable.IsWithinRange(Vector3.zero, new Vector3(0f, Interactable.VerticalTolerance + 0.1f, 0f), 2f));
        }

        [Test]
        public void NearestInRangeCandidateWins()
        {
            Stub far = new Stub { Position = new Vector3(1.8f, 0f, 0f) };
            Stub near = new Stub { Position = new Vector3(0.5f, 0f, 0f) };
            IInteractable chosen = InteractionDetector.SelectBest(new List<IInteractable> { far, near }, Vector3.zero);
            Assert.AreSame(near, chosen);
        }

        [Test]
        public void NothingInRangeSelectsNothing()
        {
            Stub tooFar = new Stub { Position = new Vector3(5f, 0f, 0f) };
            Assert.IsNull(InteractionDetector.SelectBest(new List<IInteractable> { tooFar }, Vector3.zero));
            Assert.IsNull(InteractionDetector.SelectBest(new List<IInteractable>(), Vector3.zero));
        }

        [Test]
        public void EachCandidateUsesItsOwnRange()
        {
            Stub shortReach = new Stub { Position = new Vector3(2f, 0f, 0f), Range = 1f };
            Stub longReach = new Stub { Position = new Vector3(3f, 0f, 0f), Range = 4f };
            IInteractable chosen = InteractionDetector.SelectBest(new List<IInteractable> { shortReach, longReach }, Vector3.zero);
            Assert.AreSame(longReach, chosen);
        }

        [Test]
        public void EqualDistancePrefersTheEarlierEntry()
        {
            Stub first = new Stub { Position = new Vector3(1f, 0f, 0f) };
            Stub second = new Stub { Position = new Vector3(-1f, 0f, 0f) };
            Assert.AreSame(first, InteractionDetector.SelectBest(new List<IInteractable> { first, second }, Vector3.zero));
            Assert.AreSame(second, InteractionDetector.SelectBest(new List<IInteractable> { second, first }, Vector3.zero));
        }
    }
}
