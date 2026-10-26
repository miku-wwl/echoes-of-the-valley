using System;
using System.Collections.Generic;

namespace Echoes
{
    public enum ChickenPhase { Hungry = 0, Producing = 1, EggReady = 2 }

    public enum StreamQuestStage { NotStarted = 0, Active = 1, Completed = 2 }

    /// <summary>Progress of the garden and windmill quests (the stream quest keeps its Phase 2 enum and values).</summary>
    public enum QuestStage { NotStarted = 0, Active = 1, Completed = 2 }

    /// <summary>
    /// The valley's single authoritative restoration value, saved as an integer: 0 silent, 1 stream restored,
    /// 2 garden blooming, 3 windmill turning. Only the quest deliveries below move it, one step at a time.
    /// </summary>
    public enum ResonanceStage { SilentValley = 0, AwakeningStream = 1, BloomingGarden = 2, RestoredValley = 3 }

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
        public const int GardenQuestReward = 30;
        public const int WindmillQuestReward = 40;
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
        public QuestStage GardenQuest { get; private set; } = QuestStage.NotStarted;
        public QuestStage WindmillQuest { get; private set; } = QuestStage.NotStarted;
        public ResonanceStage Resonance { get; private set; } = ResonanceStage.SilentValley;

        /// <summary>True once the closing scene has been shown. It is saved, so loading never replays it.</summary>
        public bool EndingSeen { get; private set; }

        public bool ExplorationDone => ExplorationStep >= ExplorationComplete;
        public int NodeCount => nodes.Count;

        /// <summary>Raised after any committed change.</summary>
        public event Action Changed;

        /// <summary>Short player-facing feedback ("+1 Moonleaf Herb").</summary>
        public event Action<string> Message;

        /// <summary>
        /// One-time story beats: "quest_started" / "quest_completed" (the stream), "garden_started" / "garden_completed",
        /// "windmill_started" / "windmill_completed" and "ending_seen". Each is raised exactly once per playthrough.
        /// </summary>
        public event Action<string> Milestone;

        /// <summary>A sound to play (see SoundCue). Raised after a commit, never by a loaded save.</summary>
        public event Action<SoundCue> Cue;

        public void Notify(string text) => Message?.Invoke(text);

        /// <summary>Raises a sound cue without touching any state (a refused action says "Denied", the Heart wakes).</summary>
        public void RaiseCue(SoundCue cue) => Cue?.Invoke(cue);

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
            RaiseCue(node.ItemId == ItemIds.Ore ? SoundCue.GatherOre : node.ItemId == ItemIds.Flower ? SoundCue.GatherFlower : SoundCue.GatherHerb);
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
            RaiseCue(SoundCue.Craft);
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
            RaiseCue(SoundCue.FeedHen);
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
            RaiseCue(SoundCue.CollectEgg);
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
            RaiseCue(SoundCue.QuestStart);
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
            RaiseCue(SoundCue.StreamRestored);
            Milestone?.Invoke("quest_completed");
            RaiseChanged();
            return Outcome.Ok;
        }

        // Quest 2 and 3 follow the same shape as the stream quest: start (needs the previous stage), then one delivery that
        // consumes exactly one item, pays once and moves ResonanceStage up one step. Holding the item before accepting the
        // quest is fine: the delivery only looks at the satchel.

        /// <summary>Restore the Garden: needs the stream restored (stage 1). Accepting it changes no item.</summary>
        public Outcome TryStartGardenQuest()
        {
            if (GardenQuest != QuestStage.NotStarted) return Outcome.WrongState;
            if (Resonance != ResonanceStage.AwakeningStream) return Outcome.Locked;

            GardenQuest = QuestStage.Active;
            Notify("New quest: Restore the Garden");
            RaiseCue(SoundCue.QuestStart);
            Milestone?.Invoke("garden_started");
            RaiseChanged();
            return Outcome.Ok;
        }

        /// <summary>Gives one of Clover's eggs to the Garden Keeper: stage 1 -> 2, one egg consumed, 30 coins once.</summary>
        public Outcome TryDeliverEgg()
        {
            if (GardenQuest == QuestStage.Completed) return Outcome.WrongState;
            if (GardenQuest != QuestStage.Active) return Outcome.Locked;

            Outcome result = Inventory.TryApplyQuiet(new[] { new ItemAmount(ItemIds.Egg, 1) }, null);
            if (result != Outcome.Ok) return result;

            Coins = Math.Min(MaxCoins, Coins + GardenQuestReward);
            GardenQuest = QuestStage.Completed;
            Resonance = ResonanceStage.BloomingGarden;
            Notify("The garden is blooming! +" + GardenQuestReward + " coins");
            RaiseCue(SoundCue.GardenRestored);
            Milestone?.Invoke("garden_completed");
            RaiseChanged();
            return Outcome.Ok;
        }

        /// <summary>Restore the Windmill: needs the garden restored (stage 2).</summary>
        public Outcome TryStartWindmillQuest()
        {
            if (WindmillQuest != QuestStage.NotStarted) return Outcome.WrongState;
            if (Resonance != ResonanceStage.BloomingGarden) return Outcome.Locked;

            WindmillQuest = QuestStage.Active;
            Notify("New quest: Restore the Windmill");
            RaiseCue(SoundCue.QuestStart);
            Milestone?.Invoke("windmill_started");
            RaiseChanged();
            return Outcome.Ok;
        }

        /// <summary>Hands Windmill Braces to the Windmill Restorer: stage 2 -> 3, one pair of braces consumed, 40 coins once.</summary>
        public Outcome TryDeliverWindmillBraces()
        {
            if (WindmillQuest == QuestStage.Completed) return Outcome.WrongState;
            if (WindmillQuest != QuestStage.Active) return Outcome.Locked;

            Outcome result = Inventory.TryApplyQuiet(new[] { new ItemAmount(ItemIds.WindmillBraces, 1) }, null);
            if (result != Outcome.Ok) return result;

            Coins = Math.Min(MaxCoins, Coins + WindmillQuestReward);
            WindmillQuest = QuestStage.Completed;
            Resonance = ResonanceStage.RestoredValley;
            Notify("The windmill turns again! +" + WindmillQuestReward + " coins");
            RaiseCue(SoundCue.WindmillRestored);
            Milestone?.Invoke("windmill_completed");
            RaiseChanged();
            return Outcome.Ok;
        }

        /// <summary>Records that the closing scene was shown: only once the valley is fully restored, and only once.</summary>
        public Outcome TryCompleteEnding()
        {
            if (EndingSeen) return Outcome.WrongState;
            if (Resonance != ResonanceStage.RestoredValley) return Outcome.Locked;

            EndingSeen = true;
            RaiseCue(SoundCue.EndingBegins);
            Milestone?.Invoke("ending_seen");
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
            int explorationStep, StreamQuestStage quest, QuestStage garden, QuestStage windmill, bool endingSeen,
            ResonanceStage resonance, IDictionary<string, float> nodeTimers)
        {
            Coins = coins;
            Inventory.Restore(items);
            Chicken = chicken;
            ChickenRemaining = chickenRemaining;
            ExplorationStep = explorationStep;
            StreamQuest = quest;
            GardenQuest = garden;
            WindmillQuest = windmill;
            EndingSeen = endingSeen;
            Resonance = resonance;
            pendingNodeTimers.Clear();
            foreach (KeyValuePair<string, float> timer in nodeTimers) pendingNodeTimers[timer.Key] = timer.Value;
        }

        void RaiseChanged() => Changed?.Invoke();
    }
}
