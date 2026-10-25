using System;
using System.Collections.Generic;

namespace Echoes
{
    public enum ChickenPhase { Hungry = 0, Producing = 1, EggReady = 2 }

    public enum StreamQuestStage { NotStarted = 0, Active = 1, Completed = 2 }

    /// <summary>The valley's single authoritative restoration value. Phase 2 implements 0 -> 1 only.</summary>
    public enum ResonanceStage { SilentValley = 0, AwakeningStream = 1 }

    public sealed class ResourceNodeState
    {
        public ResourceNodeState(string id)
        {
            Id = id;
        }

        public string Id { get; }
        public string ItemId { get; internal set; }
        public int Yield { get; internal set; }
        public float Cooldown { get; internal set; }
        public float Remaining { get; internal set; }
        public bool IsReady => Remaining <= 0f;
    }

    /// <summary>
    /// The one owner of gameplay state: satchel, coins, resource nodes, the hen, quest progress and ResonanceStage.
    /// It knows nothing about scenes or visuals. Every action validates first, then commits once, then notifies.
    /// Timers are session seconds advanced by Tick, so menus never advance them and saves store remaining seconds.
    /// </summary>
    public sealed class GameSession
    {
        public const int StartingCoins = 20;
        public const int MaxCoins = 9999;
        public const int StreamQuestReward = 25;
        public const float ChickenProductionSeconds = 15f;
        public const string ChickenName = "Clover";
        public const int ExplorationComplete = 3;   // ObjectiveStep.Complete

        readonly Dictionary<string, ResourceNodeState> nodes = new Dictionary<string, ResourceNodeState>();
        readonly Dictionary<string, float> pendingNodeTimers = new Dictionary<string, float>();

        public GameSession()
        {
            Inventory = new Inventory();
            Inventory.Changed += RaiseChanged;
            Coins = StartingCoins;
        }

        /// <summary>The session the running scene plays. MainMenu replaces it for New Game / Continue.</summary>
        public static GameSession Current { get; set; }

        public Inventory Inventory { get; }
        public int Coins { get; private set; }
        public ChickenPhase Chicken { get; private set; } = ChickenPhase.Hungry;
        public float ChickenRemaining { get; private set; }
        public int ExplorationStep { get; private set; }
        public StreamQuestStage StreamQuest { get; private set; } = StreamQuestStage.NotStarted;
        public ResonanceStage Resonance { get; private set; } = ResonanceStage.SilentValley;
        public bool ExplorationDone => ExplorationStep >= ExplorationComplete;
        public int NodeCount => nodes.Count;

        /// <summary>Raised after any committed change.</summary>
        public event Action Changed;

        /// <summary>Short player-facing feedback ("+1 Moonleaf Herb").</summary>
        public event Action<string> Message;

        /// <summary>One-time story beats: "quest_started", "quest_completed".</summary>
        public event Action<string> Milestone;

        public void Notify(string text) => Message?.Invoke(text);

        // ---- Resource nodes ----------------------------------------------------------------------------------------

        public Outcome RegisterNode(string id, string itemId, int yield, float cooldown)
        {
            if (string.IsNullOrEmpty(id) || !ItemCatalog.IsKnown(itemId) || yield <= 0
                || float.IsNaN(cooldown) || float.IsInfinity(cooldown) || cooldown < 0f)
                return Outcome.InvalidAmount;

            if (!nodes.TryGetValue(id, out ResourceNodeState node))
            {
                node = new ResourceNodeState(id);
                nodes[id] = node;
            }
            node.ItemId = itemId;
            node.Yield = yield;
            node.Cooldown = cooldown;
            if (pendingNodeTimers.TryGetValue(id, out float saved))
            {
                node.Remaining = Math.Min(Math.Max(saved, 0f), cooldown);
                pendingNodeTimers.Remove(id);
            }
            return Outcome.Ok;
        }

        public bool IsNodeReady(string id) => id != null && nodes.TryGetValue(id, out ResourceNodeState node) && node.IsReady;

        public float NodeRemaining(string id) => id != null && nodes.TryGetValue(id, out ResourceNodeState node) ? node.Remaining : 0f;

        public Outcome TryGather(string id)
        {
            if (id == null || !nodes.TryGetValue(id, out ResourceNodeState node)) return Outcome.UnknownTarget;
            if (!node.IsReady) return Outcome.NotReady;

            Outcome added = Inventory.TryApplyQuiet(null, new[] { new ItemAmount(node.ItemId, node.Yield) });
            if (added != Outcome.Ok) return added;

            node.Remaining = node.Cooldown;
            Notify("+" + node.Yield + " " + ItemCatalog.NameOf(node.ItemId));
            RaiseChanged();
            return Outcome.Ok;
        }

        // ---- Alchemy -----------------------------------------------------------------------------------------------

        public Outcome TryCraft(string recipeId)
        {
            Recipe recipe = RecipeBook.Get(recipeId);
            if (recipe == null) return Outcome.UnknownTarget;

            Outcome result = Inventory.TryApplyQuiet(recipe.Inputs, new[] { recipe.Output });
            if (result != Outcome.Ok) return result;

            Notify("Crafted " + ItemCatalog.NameOf(recipe.Output.Id) + (recipe.Output.Count > 1 ? " x" + recipe.Output.Count : ""));
            RaiseChanged();
            return Outcome.Ok;
        }

        // ---- The hen -----------------------------------------------------------------------------------------------

        public Outcome TryFeedChicken()
        {
            if (Chicken != ChickenPhase.Hungry) return Outcome.WrongState;

            Outcome result = Inventory.TryApplyQuiet(new[] { new ItemAmount(ItemIds.ChickenFeed, 1) }, null);
            if (result != Outcome.Ok) return result;

            Chicken = ChickenPhase.Producing;
            ChickenRemaining = ChickenProductionSeconds;
            Notify(ChickenName + " munches the feed happily.");
            RaiseChanged();
            return Outcome.Ok;
        }

        public Outcome TryCollectEgg()
        {
            if (Chicken != ChickenPhase.EggReady) return Outcome.WrongState;

            Outcome result = Inventory.TryApplyQuiet(null, new[] { new ItemAmount(ItemIds.Egg, 1) });
            if (result != Outcome.Ok) return result;

            Chicken = ChickenPhase.Hungry;
            ChickenRemaining = 0f;
            Notify("+1 " + ItemCatalog.NameOf(ItemIds.Egg));
            RaiseChanged();
            return Outcome.Ok;
        }

        // ---- Coins -------------------------------------------------------------------------------------------------

        public Outcome TryAddCoins(int amount)
        {
            if (amount <= 0) return Outcome.InvalidAmount;
            if ((long)Coins + amount > MaxCoins) return Outcome.NoRoom;
            Coins += amount;
            RaiseChanged();
            return Outcome.Ok;
        }

        public Outcome TrySpendCoins(int amount)
        {
            if (amount <= 0) return Outcome.InvalidAmount;
            if (Coins < amount) return Outcome.Insufficient;
            Coins -= amount;
            RaiseChanged();
            return Outcome.Ok;
        }

        // ---- Quest and resonance -----------------------------------------------------------------------------------

        /// <summary>Records exploration progress (0..3). It only ever moves forward.</summary>
        public void SetExplorationStep(int step)
        {
            if (step < 0 || step > ExplorationComplete || step <= ExplorationStep) return;
            ExplorationStep = step;
            RaiseChanged();
        }

        public Outcome TryStartStreamQuest()
        {
            if (!ExplorationDone) return Outcome.Locked;
            if (StreamQuest != StreamQuestStage.NotStarted) return Outcome.WrongState;

            StreamQuest = StreamQuestStage.Active;
            Notify("New quest: Restore the Stream");
            Milestone?.Invoke("quest_started");
            RaiseChanged();
            return Outcome.Ok;
        }

        /// <summary>
        /// Pours a Spring Tonic into the ruined spring. Needs the quest to be active and a tonic in the satchel;
        /// consumes exactly one tonic, pays the one-time reward and moves ResonanceStage from 0 to 1.
        /// </summary>
        public Outcome TryDeliverSpringTonic()
        {
            if (StreamQuest == StreamQuestStage.Completed) return Outcome.WrongState;
            if (StreamQuest != StreamQuestStage.Active) return Outcome.Locked;

            Outcome result = Inventory.TryApplyQuiet(new[] { new ItemAmount(ItemIds.SpringTonic, 1) }, null);
            if (result != Outcome.Ok) return result;

            Coins = Math.Min(MaxCoins, Coins + StreamQuestReward);
            StreamQuest = StreamQuestStage.Completed;
            Resonance = ResonanceStage.AwakeningStream;
            Notify("The stream is restored! +" + StreamQuestReward + " coins");
            Milestone?.Invoke("quest_completed");
            RaiseChanged();
            return Outcome.Ok;
        }

        // ---- Time --------------------------------------------------------------------------------------------------

        public void Tick(float deltaSeconds)
        {
            if (!(deltaSeconds > 0f) || float.IsInfinity(deltaSeconds)) return;

            bool changed = false;
            foreach (ResourceNodeState node in nodes.Values)
            {
                if (node.Remaining <= 0f) continue;
                node.Remaining = Math.Max(0f, node.Remaining - deltaSeconds);
                if (node.Remaining <= 0f) changed = true;
            }

            if (Chicken == ChickenPhase.Producing)
            {
                ChickenRemaining -= deltaSeconds;
                if (ChickenRemaining <= 0f)
                {
                    ChickenRemaining = 0f;
                    Chicken = ChickenPhase.EggReady;
                    Notify(ChickenName + " laid an egg!");
                    changed = true;
                }
            }

            if (changed) RaiseChanged();
        }

        // ---- Save support ------------------------------------------------------------------------------------------

        /// <summary>Node timers for saving: registered nodes plus loaded timers whose nodes have not registered yet.</summary>
        public IEnumerable<KeyValuePair<string, float>> NodeTimers()
        {
            foreach (ResourceNodeState node in nodes.Values)
                yield return new KeyValuePair<string, float>(node.Id, node.Remaining);
            foreach (KeyValuePair<string, float> pending in pendingNodeTimers)
                if (!nodes.ContainsKey(pending.Key)) yield return pending;
        }

        /// <summary>Applies already-validated saved values (SaveCodec checks every one before calling this).</summary>
        internal void RestoreCore(int coins, IEnumerable<ItemAmount> items, ChickenPhase chicken, float chickenRemaining,
            int explorationStep, StreamQuestStage quest, ResonanceStage resonance, IDictionary<string, float> nodeTimers)
        {
            Coins = coins;
            Inventory.Restore(items);
            Chicken = chicken;
            ChickenRemaining = chickenRemaining;
            ExplorationStep = explorationStep;
            StreamQuest = quest;
            Resonance = resonance;
            pendingNodeTimers.Clear();
            foreach (KeyValuePair<string, float> timer in nodeTimers) pendingNodeTimers[timer.Key] = timer.Value;
        }

        void RaiseChanged() => Changed?.Invoke();
    }
}
