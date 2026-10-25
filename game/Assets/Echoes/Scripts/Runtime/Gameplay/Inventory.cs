using System;
using System.Collections.Generic;

namespace Echoes
{
    /// <summary>
    /// The satchel: stacks of known items in a limited number of slots (one slot per item type, up to a stack limit).
    /// Every change is one validated transaction that either applies completely or not at all. The default size
    /// (8 slots for 7 item types) means a full stack of one item can never block collecting another.
    /// </summary>
    public sealed class Inventory
    {
        public const int DefaultSlots = 8;
        public const int DefaultStackLimit = 9;

        readonly Dictionary<string, int> counts = new Dictionary<string, int>();

        public Inventory(int slotCapacity = DefaultSlots, int stackLimit = DefaultStackLimit)
        {
            if (slotCapacity < 1) throw new ArgumentOutOfRangeException(nameof(slotCapacity));
            if (stackLimit < 1) throw new ArgumentOutOfRangeException(nameof(stackLimit));
            SlotCapacity = slotCapacity;
            StackLimit = stackLimit;
        }

        public int SlotCapacity { get; }
        public int StackLimit { get; }
        public int UsedSlots => counts.Count;

        /// <summary>Raised once after a committed change made through the public API.</summary>
        public event Action Changed;

        public int Count(string id) => id != null && counts.TryGetValue(id, out int count) ? count : 0;

        public bool Has(string id, int quantity) => quantity > 0 && Count(id) >= quantity;

        /// <summary>Current stacks in catalog order.</summary>
        public IReadOnlyList<ItemAmount> Snapshot()
        {
            List<ItemAmount> list = new List<ItemAmount>(counts.Count);
            foreach (ItemDef def in ItemCatalog.All)
            {
                if (counts.TryGetValue(def.Id, out int count)) list.Add(new ItemAmount(def.Id, count));
            }
            return list;
        }

        public Outcome TryAdd(string id, int quantity) => TryApply(null, new[] { new ItemAmount(id, quantity) });

        public Outcome TryRemove(string id, int quantity) => TryApply(new[] { new ItemAmount(id, quantity) }, null);

        /// <summary>Would this transaction succeed? Changes nothing.</summary>
        public Outcome Check(IReadOnlyList<ItemAmount> remove, IReadOnlyList<ItemAmount> add) =>
            Simulate(new Dictionary<string, int>(counts), remove, add);

        /// <summary>Removes then adds in one atomic step: validate, commit once, notify.</summary>
        public Outcome TryApply(IReadOnlyList<ItemAmount> remove, IReadOnlyList<ItemAmount> add)
        {
            Outcome result = TryApplyQuiet(remove, add);
            if (result == Outcome.Ok) Changed?.Invoke();
            return result;
        }

        /// <summary>Same transaction without the notification, for callers (GameSession) that commit more state and notify once.</summary>
        internal Outcome TryApplyQuiet(IReadOnlyList<ItemAmount> remove, IReadOnlyList<ItemAmount> add)
        {
            Dictionary<string, int> work = new Dictionary<string, int>(counts);
            Outcome result = Simulate(work, remove, add);
            if (result != Outcome.Ok) return result;

            counts.Clear();
            foreach (KeyValuePair<string, int> pair in work) counts[pair.Key] = pair.Value;
            return Outcome.Ok;
        }

        /// <summary>Replaces the whole content from saved data after validating every entry. Changes nothing on failure.</summary>
        public Outcome Restore(IEnumerable<ItemAmount> saved)
        {
            Dictionary<string, int> work = new Dictionary<string, int>();
            foreach (ItemAmount item in saved)
            {
                if (!ItemCatalog.IsKnown(item.Id) || item.Count <= 0 || item.Count > StackLimit) return Outcome.InvalidAmount;
                if (work.ContainsKey(item.Id)) return Outcome.InvalidAmount;
                work[item.Id] = item.Count;
            }
            if (work.Count > SlotCapacity) return Outcome.NoRoom;

            counts.Clear();
            foreach (KeyValuePair<string, int> pair in work) counts[pair.Key] = pair.Value;
            Changed?.Invoke();
            return Outcome.Ok;
        }

        Outcome Simulate(Dictionary<string, int> work, IReadOnlyList<ItemAmount> remove, IReadOnlyList<ItemAmount> add)
        {
            if (remove != null)
            {
                foreach (ItemAmount item in remove)
                    if (!ItemCatalog.IsKnown(item.Id) || item.Count <= 0) return Outcome.InvalidAmount;
            }
            if (add != null)
            {
                foreach (ItemAmount item in add)
                    if (!ItemCatalog.IsKnown(item.Id) || item.Count <= 0) return Outcome.InvalidAmount;
            }

            if (remove != null)
            {
                foreach (ItemAmount item in remove)
                {
                    work.TryGetValue(item.Id, out int have);
                    if (have < item.Count) return Outcome.Insufficient;
                    if (have == item.Count) work.Remove(item.Id);
                    else work[item.Id] = have - item.Count;
                }
            }

            if (add != null)
            {
                foreach (ItemAmount item in add)
                {
                    work.TryGetValue(item.Id, out int have);
                    long next = (long)have + item.Count;
                    if (next > StackLimit) return Outcome.NoRoom;
                    work[item.Id] = (int)next;
                }
            }

            return work.Count > SlotCapacity ? Outcome.NoRoom : Outcome.Ok;
        }
    }
}
