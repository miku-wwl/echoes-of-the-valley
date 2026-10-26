using System.Collections.Generic;
using UnityEngine;

namespace Echoes
{
    /// <summary>Result of a gameplay transaction. Anything other than Ok changed nothing.</summary>
    public enum Outcome
    {
        Ok = 0,
        InvalidAmount,   // zero/negative quantity, unknown item id, malformed request
        Insufficient,    // not enough of an item or coins
        NoRoom,          // the satchel has no slot or stack space for the result
        NotReady,        // a timer is still running (node regrowing)
        WrongState,      // not allowed in the current state (already fed, quest already done...)
        Locked,          // a prerequisite is missing (quest not unlocked yet)
        UnknownTarget    // node or recipe id is not registered
    }

    /// <summary>Player-facing wording for failed transactions.</summary>
    public static class OutcomeText
    {
        public static string Describe(Outcome outcome)
        {
            switch (outcome)
            {
                case Outcome.Ok: return "Done.";
                case Outcome.InvalidAmount: return "That does not work.";
                case Outcome.Insufficient: return "You do not have enough.";
                case Outcome.NoRoom: return "Your satchel has no room for that.";
                case Outcome.NotReady: return "It is still regrowing.";
                case Outcome.WrongState: return "Nothing to do there right now.";
                case Outcome.Locked: return "It is not time for that yet.";
                default: return "Nothing happens.";
            }
        }
    }

    public readonly struct ItemAmount
    {
        public readonly string Id;
        public readonly int Count;

        public ItemAmount(string id, int count)
        {
            Id = id;
            Count = count;
        }

        public override string ToString() => Count + "x " + Id;
    }

    /// <summary>Stable item ids. Never rename these: they are written to save files.</summary>
    public static class ItemIds
    {
        public const string Herb = "herb";
        public const string Flower = "flower";
        public const string Ore = "ore";
        public const string ChickenFeed = "chicken_feed";
        public const string Egg = "egg";
        public const string SpringTonic = "spring_tonic";
        public const string WindmillBraces = "windmill_braces";
    }

    public sealed class ItemDef
    {
        public ItemDef(string id, string name, string description, Color tint)
        {
            Id = id;
            Name = name;
            Description = description;
            Tint = tint;
        }

        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public Color Tint { get; }
    }

    public static class ItemCatalog
    {
        static readonly ItemDef[] all =
        {
            new ItemDef(ItemIds.Herb, "Moonleaf Herb", "A soft blue-green leaf that hums when picked.", new Color(0.30f, 0.78f, 0.66f)),
            new ItemDef(ItemIds.Flower, "Dawn Blossom", "Petals that open at first light. Good for tonics and feed.", new Color(1.00f, 0.60f, 0.38f)),
            new ItemDef(ItemIds.Ore, "Echo Ore", "A pale stone that rings like a bell when struck.", new Color(0.55f, 0.80f, 0.95f)),
            new ItemDef(ItemIds.ChickenFeed, "Chicken Feed", "Sweet seed mash. Clover loves it.", new Color(0.95f, 0.82f, 0.45f)),
            new ItemDef(ItemIds.Egg, "Hen's Egg", "Warm and fresh. The Garden Keeper wants one.", new Color(0.98f, 0.95f, 0.88f)),
            new ItemDef(ItemIds.SpringTonic, "Spring Tonic", "Bottled dew and moonleaf. It wakes sleeping water.", new Color(0.45f, 0.85f, 0.95f)),
            new ItemDef(ItemIds.WindmillBraces, "Windmill Braces", "Ringing ore struts to hold up the old windmill.", new Color(0.70f, 0.74f, 0.82f))
        };

        static readonly Dictionary<string, ItemDef> byId = Index();

        public static IReadOnlyList<ItemDef> All => all;

        public static bool IsKnown(string id) => !string.IsNullOrEmpty(id) && byId.ContainsKey(id);

        public static ItemDef Get(string id) => id != null && byId.TryGetValue(id, out ItemDef def) ? def : null;

        public static string NameOf(string id) => Get(id)?.Name ?? id ?? "?";

        static Dictionary<string, ItemDef> Index()
        {
            Dictionary<string, ItemDef> map = new Dictionary<string, ItemDef>();
            foreach (ItemDef def in all) map[def.Id] = def;
            return map;
        }
    }
}
