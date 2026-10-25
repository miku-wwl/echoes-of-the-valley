using System.Collections.Generic;
using NUnit.Framework;

namespace Echoes.Tests
{
    public class InventoryTests
    {
        static ItemAmount[] One(string id, int count) => new[] { new ItemAmount(id, count) };

        [Test]
        public void NewInventory_IsEmpty_WithDocumentedCapacity()
        {
            Inventory inventory = new Inventory();
            Assert.AreEqual(Inventory.DefaultSlots, inventory.SlotCapacity);
            Assert.AreEqual(Inventory.DefaultStackLimit, inventory.StackLimit);
            Assert.AreEqual(0, inventory.UsedSlots);
            Assert.AreEqual(0, inventory.Snapshot().Count);
            Assert.AreEqual(0, inventory.Count(ItemIds.Herb));
        }

        [Test]
        public void Add_StacksTheSameItem_AndUsesOneSlot()
        {
            Inventory inventory = new Inventory();
            Assert.AreEqual(Outcome.Ok, inventory.TryAdd(ItemIds.Herb, 2));
            Assert.AreEqual(Outcome.Ok, inventory.TryAdd(ItemIds.Herb, 3));
            Assert.AreEqual(5, inventory.Count(ItemIds.Herb));
            Assert.AreEqual(1, inventory.UsedSlots);

            inventory.TryAdd(ItemIds.Ore, 1);
            Assert.AreEqual(2, inventory.UsedSlots);
        }

        [Test]
        public void Remove_ReducesTheStack_AndFreesTheSlotWhenEmpty()
        {
            Inventory inventory = new Inventory();
            inventory.TryAdd(ItemIds.Flower, 3);

            Assert.AreEqual(Outcome.Ok, inventory.TryRemove(ItemIds.Flower, 2));
            Assert.AreEqual(1, inventory.Count(ItemIds.Flower));
            Assert.AreEqual(Outcome.Ok, inventory.TryRemove(ItemIds.Flower, 1));
            Assert.AreEqual(0, inventory.Count(ItemIds.Flower));
            Assert.AreEqual(0, inventory.UsedSlots, "an empty stack gives its slot back");
        }

        [Test]
        public void Remove_MoreThanOwned_IsRejected_AndChangesNothing()
        {
            Inventory inventory = new Inventory();
            inventory.TryAdd(ItemIds.Herb, 2);

            Assert.AreEqual(Outcome.Insufficient, inventory.TryRemove(ItemIds.Herb, 3));
            Assert.AreEqual(Outcome.Insufficient, inventory.TryRemove(ItemIds.Ore, 1));
            Assert.AreEqual(2, inventory.Count(ItemIds.Herb));
        }

        [Test]
        public void InvalidQuantitiesAndIds_AreRejected()
        {
            Inventory inventory = new Inventory();
            inventory.TryAdd(ItemIds.Herb, 1);

            Assert.AreEqual(Outcome.InvalidAmount, inventory.TryAdd(ItemIds.Herb, 0));
            Assert.AreEqual(Outcome.InvalidAmount, inventory.TryAdd(ItemIds.Herb, -3));
            Assert.AreEqual(Outcome.InvalidAmount, inventory.TryRemove(ItemIds.Herb, 0));
            Assert.AreEqual(Outcome.InvalidAmount, inventory.TryRemove(ItemIds.Herb, -1));
            Assert.AreEqual(Outcome.InvalidAmount, inventory.TryAdd("not_an_item", 1));
            Assert.AreEqual(Outcome.InvalidAmount, inventory.TryAdd(null, 1));
            Assert.AreEqual(Outcome.NoRoom, inventory.TryAdd(ItemIds.Herb, int.MaxValue), "overflow is a capacity problem, not a wrap-around");
            Assert.AreEqual(1, inventory.Count(ItemIds.Herb));
        }

        [Test]
        public void StackLimit_IsEnforced()
        {
            Inventory inventory = new Inventory(8, 5);
            Assert.AreEqual(Outcome.Ok, inventory.TryAdd(ItemIds.Ore, 5));
            Assert.AreEqual(Outcome.NoRoom, inventory.TryAdd(ItemIds.Ore, 1));
            Assert.AreEqual(Outcome.NoRoom, new Inventory(8, 5).TryAdd(ItemIds.Ore, 6));
            Assert.AreEqual(5, inventory.Count(ItemIds.Ore));
        }

        [Test]
        public void SlotCapacity_IsEnforced()
        {
            Inventory inventory = new Inventory(2, 9);
            Assert.AreEqual(Outcome.Ok, inventory.TryAdd(ItemIds.Herb, 1));
            Assert.AreEqual(Outcome.Ok, inventory.TryAdd(ItemIds.Flower, 1));
            Assert.AreEqual(Outcome.NoRoom, inventory.TryAdd(ItemIds.Ore, 1), "a third item type needs a third slot");
            Assert.AreEqual(Outcome.Ok, inventory.TryAdd(ItemIds.Herb, 4), "more of an item already held needs no new slot");
            Assert.AreEqual(2, inventory.UsedSlots);
        }

        [Test]
        public void Transaction_IsAtomic_WhenTheAdditionDoesNotFit()
        {
            Inventory inventory = new Inventory(8, 9);
            inventory.TryAdd(ItemIds.Flower, 2);
            inventory.TryAdd(ItemIds.ChickenFeed, 8);

            // removing a flower is fine, but 3 more feed would exceed the stack limit: neither half may apply
            Outcome result = inventory.TryApply(One(ItemIds.Flower, 1), One(ItemIds.ChickenFeed, 3));

            Assert.AreEqual(Outcome.NoRoom, result);
            Assert.AreEqual(2, inventory.Count(ItemIds.Flower));
            Assert.AreEqual(8, inventory.Count(ItemIds.ChickenFeed));
        }

        [Test]
        public void Transaction_IsAtomic_WhenARemovalIsShort()
        {
            Inventory inventory = new Inventory();
            inventory.TryAdd(ItemIds.Herb, 2);

            Outcome result = inventory.TryApply(
                new[] { new ItemAmount(ItemIds.Herb, 2), new ItemAmount(ItemIds.Flower, 1) }, One(ItemIds.SpringTonic, 1));

            Assert.AreEqual(Outcome.Insufficient, result);
            Assert.AreEqual(2, inventory.Count(ItemIds.Herb));
            Assert.AreEqual(0, inventory.Count(ItemIds.SpringTonic));
        }

        [Test]
        public void Transaction_CountsTheSlotFreedByARemoval()
        {
            Inventory inventory = new Inventory(2, 9);
            inventory.TryAdd(ItemIds.Herb, 1);
            inventory.TryAdd(ItemIds.Flower, 1);

            // the bag is full of types, but spending the only herb frees its slot for the result
            Assert.AreEqual(Outcome.Ok, inventory.TryApply(One(ItemIds.Herb, 1), One(ItemIds.SpringTonic, 1)));
            Assert.AreEqual(1, inventory.Count(ItemIds.SpringTonic));
            Assert.AreEqual(2, inventory.UsedSlots);
        }

        [Test]
        public void DuplicateEntriesInOneTransaction_AreSummed()
        {
            Inventory inventory = new Inventory();
            inventory.TryAdd(ItemIds.Ore, 3);

            ItemAmount[] twice = { new ItemAmount(ItemIds.Ore, 2), new ItemAmount(ItemIds.Ore, 2) };
            Assert.AreEqual(Outcome.Insufficient, inventory.TryApply(twice, null), "2 + 2 is more than the 3 owned");
            Assert.AreEqual(3, inventory.Count(ItemIds.Ore));
            Assert.AreEqual(Outcome.Ok, inventory.TryApply(null, twice));
            Assert.AreEqual(7, inventory.Count(ItemIds.Ore));
        }

        [Test]
        public void Changed_FiresOncePerCommit_AndNeverForAFailure()
        {
            Inventory inventory = new Inventory();
            int fired = 0;
            inventory.Changed += () => fired++;

            inventory.TryAdd(ItemIds.Herb, 2);
            Assert.AreEqual(1, fired);
            inventory.TryRemove(ItemIds.Herb, 5);
            inventory.TryAdd(ItemIds.Herb, 0);
            Assert.AreEqual(1, fired, "rejected transactions are silent");
            inventory.TryApply(One(ItemIds.Herb, 1), One(ItemIds.Flower, 1));
            Assert.AreEqual(2, fired, "a two-part transaction notifies once");
        }

        [Test]
        public void DefaultBag_CanAlwaysHoldOneOfEveryItemType()
        {
            // The design guarantee that makes hoarding harmless: slots >= item types.
            Assert.GreaterOrEqual(Inventory.DefaultSlots, ItemCatalog.All.Count);

            Inventory inventory = new Inventory();
            foreach (ItemDef def in ItemCatalog.All)
                Assert.AreEqual(Outcome.Ok, inventory.TryAdd(def.Id, Inventory.DefaultStackLimit), def.Id);
            Assert.AreEqual(ItemCatalog.All.Count, inventory.UsedSlots);
        }

        [Test]
        public void Snapshot_ListsStacksInCatalogOrder()
        {
            Inventory inventory = new Inventory();
            inventory.TryAdd(ItemIds.SpringTonic, 1);
            inventory.TryAdd(ItemIds.Herb, 2);

            IReadOnlyList<ItemAmount> stacks = inventory.Snapshot();
            Assert.AreEqual(2, stacks.Count);
            Assert.AreEqual(ItemIds.Herb, stacks[0].Id);
            Assert.AreEqual(ItemIds.SpringTonic, stacks[1].Id);
        }

        [Test]
        public void Restore_ReplacesContent_OnlyWhenEveryEntryIsValid()
        {
            Inventory inventory = new Inventory();
            inventory.TryAdd(ItemIds.Ore, 1);

            Assert.AreEqual(Outcome.Ok, inventory.Restore(new[] { new ItemAmount(ItemIds.Herb, 4) }));
            Assert.AreEqual(4, inventory.Count(ItemIds.Herb));
            Assert.AreEqual(0, inventory.Count(ItemIds.Ore));

            Assert.AreEqual(Outcome.InvalidAmount, inventory.Restore(new[] { new ItemAmount(ItemIds.Herb, 0) }));
            Assert.AreEqual(Outcome.InvalidAmount, inventory.Restore(new[] { new ItemAmount("mystery", 1) }));
            Assert.AreEqual(Outcome.InvalidAmount, inventory.Restore(new[] { new ItemAmount(ItemIds.Herb, 99) }));
            Assert.AreEqual(Outcome.InvalidAmount, inventory.Restore(new[] { new ItemAmount(ItemIds.Herb, 1), new ItemAmount(ItemIds.Herb, 1) }));
            Assert.AreEqual(4, inventory.Count(ItemIds.Herb), "a rejected restore keeps the old content");
        }
    }
}
