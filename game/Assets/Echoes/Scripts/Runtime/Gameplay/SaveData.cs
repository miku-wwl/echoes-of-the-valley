using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Echoes
{
    public enum SaveStatus { Ok, Missing, Corrupt, Unsupported, Invalid, WriteFailed }

    [Serializable]
    public sealed class SavedItem
    {
        public string id;
        public int count;
    }

    [Serializable]
    public sealed class SavedNode
    {
        public string id;
        public int remainingMs;
    }

    /// <summary>
    /// The save file. Missing JSON fields keep the sentinel defaults below (-1 / null / 0), which is how "field missing"
    /// is told apart from a real value. Timers are whole milliseconds so nothing depends on float formatting.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public int version;                  // 0 = missing
        public int coins = -1;
        public List<SavedItem> items;        // null = missing
        public List<SavedNode> nodes;        // null = missing
        public int chickenPhase = -1;
        public int chickenRemainingMs = -1;
        public int explorationStep = -1;
        public int streamQuest = -1;
        public int resonanceStage = -1;
        public string checksum;              // null/empty = missing
    }

    public sealed class LoadResult
    {
        public LoadResult(SaveStatus status, GameSession session, string message, bool usedBackup = false)
        {
            Status = status;
            Session = session;
            Message = message;
            UsedBackup = usedBackup;
        }

        public SaveStatus Status { get; }
        public GameSession Session { get; }
        public string Message { get; }
        public bool UsedBackup { get; }
        public bool Ok => Status == SaveStatus.Ok;
    }

    /// <summary>Converts a session to and from the save format, trusting nothing it reads.</summary>
    public static class SaveCodec
    {
        public const int CurrentVersion = 1;
        public const int MaxNodeMs = 3600 * 1000;
        public const int MaxChickenMs = 600 * 1000;

        public static SaveData ToData(GameSession session)
        {
            SaveData data = new SaveData
            {
                version = CurrentVersion,
                coins = session.Coins,
                items = new List<SavedItem>(),
                nodes = new List<SavedNode>(),
                chickenPhase = (int)session.Chicken,
                // A producing hen always saves at least 1 ms, so rounding can never produce "producing with no timer".
                chickenRemainingMs = session.Chicken == ChickenPhase.Producing ? Math.Max(1, ToMs(session.ChickenRemaining, MaxChickenMs)) : 0,
                explorationStep = session.ExplorationStep,
                streamQuest = (int)session.StreamQuest,
                resonanceStage = (int)session.Resonance
            };
            foreach (ItemAmount item in session.Inventory.Snapshot())
                data.items.Add(new SavedItem { id = item.Id, count = item.Count });
            foreach (KeyValuePair<string, float> timer in session.NodeTimers())
                data.nodes.Add(new SavedNode { id = timer.Key, remainingMs = ToMs(timer.Value, MaxNodeMs) });
            data.checksum = ComputeChecksum(data);
            return data;
        }

        public static string Serialize(SaveData data) => JsonUtility.ToJson(data, true);

        /// <summary>Parses and fully validates save text. Never throws; the status says what was wrong.</summary>
        public static LoadResult Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new LoadResult(SaveStatus.Corrupt, null, "the save file is empty");

            SaveData data = new SaveData();
            try
            {
                JsonUtility.FromJsonOverwrite(json, data);
            }
            catch (Exception exception)
            {
                return new LoadResult(SaveStatus.Corrupt, null, "the save is not valid JSON (" + exception.Message + ")");
            }
            return Validate(data);
        }

        public static LoadResult Validate(SaveData data)
        {
            if (data == null) return new LoadResult(SaveStatus.Corrupt, null, "no save data");
            if (data.version <= 0) return new LoadResult(SaveStatus.Invalid, null, "the save has no version");
            if (data.version != CurrentVersion)
                return new LoadResult(SaveStatus.Unsupported, null, "save version " + data.version + " is not supported (expected " + CurrentVersion + ")");

            if (data.items == null || data.nodes == null || data.coins < 0 || data.chickenPhase < 0 || data.chickenRemainingMs < 0
                || data.explorationStep < 0 || data.streamQuest < 0 || data.resonanceStage < 0 || string.IsNullOrEmpty(data.checksum))
                return new LoadResult(SaveStatus.Invalid, null, "the save is missing required fields");

            if (!string.Equals(data.checksum, ComputeChecksum(data), StringComparison.Ordinal))
                return new LoadResult(SaveStatus.Corrupt, null, "the save failed its integrity check");

            if (data.coins > GameSession.MaxCoins) return Invalid("coins out of range");
            if (data.chickenPhase > (int)ChickenPhase.EggReady) return Invalid("unknown chicken state");
            if (data.chickenRemainingMs > MaxChickenMs) return Invalid("chicken timer out of range");
            if (data.explorationStep > GameSession.ExplorationComplete) return Invalid("unknown exploration step");
            if (data.streamQuest > (int)StreamQuestStage.Completed) return Invalid("unknown quest state");
            if (data.resonanceStage > (int)ResonanceStage.AwakeningStream) return Invalid("unknown resonance stage");

            ChickenPhase chicken = (ChickenPhase)data.chickenPhase;
            if (chicken == ChickenPhase.Producing && data.chickenRemainingMs <= 0) return Invalid("producing hen with no timer");
            if (chicken != ChickenPhase.Producing && data.chickenRemainingMs != 0) return Invalid("hen timer without production");

            StreamQuestStage quest = (StreamQuestStage)data.streamQuest;
            ResonanceStage resonance = (ResonanceStage)data.resonanceStage;
            if (quest != StreamQuestStage.NotStarted && data.explorationStep != GameSession.ExplorationComplete) return Invalid("quest progress without finishing exploration");
            if ((quest == StreamQuestStage.Completed) != (resonance == ResonanceStage.AwakeningStream)) return Invalid("quest and resonance stage disagree");

            List<ItemAmount> items = new List<ItemAmount>();
            foreach (SavedItem item in data.items)
            {
                if (item == null || !ItemCatalog.IsKnown(item.id)) return Invalid("unknown item");
                items.Add(new ItemAmount(item.id, item.count));
            }

            Dictionary<string, float> timers = new Dictionary<string, float>();
            foreach (SavedNode node in data.nodes)
            {
                if (node == null || string.IsNullOrEmpty(node.id)) return Invalid("node without id");
                if (node.remainingMs < 0 || node.remainingMs > MaxNodeMs) return Invalid("node timer out of range");
                if (timers.ContainsKey(node.id)) return Invalid("duplicate node " + node.id);
                timers[node.id] = node.remainingMs / 1000f;
            }

            GameSession session = new GameSession();
            if (session.Inventory.Restore(items) != Outcome.Ok) return Invalid("invalid item quantities");

            session.RestoreCore(data.coins, items, chicken, data.chickenRemainingMs / 1000f, data.explorationStep, quest, resonance, timers);
            return new LoadResult(SaveStatus.Ok, session, "ok");
        }

        static LoadResult Invalid(string reason) => new LoadResult(SaveStatus.Invalid, null, "the save contains invalid data (" + reason + ")");

        static int ToMs(float seconds, int maxMs)
        {
            if (float.IsNaN(seconds) || seconds <= 0f) return 0;
            double ms = Math.Round(seconds * 1000.0);
            return (int)Math.Min(ms, maxMs);
        }

        /// <summary>FNV-1a over a canonical text form. It catches accidental damage and hand edits; it is not security.</summary>
        public static string ComputeChecksum(SaveData data)
        {
            StringBuilder text = new StringBuilder("echoes-save|");
            text.Append(data.version).Append('|').Append(data.coins).Append('|');

            List<SavedItem> items = new List<SavedItem>(data.items ?? new List<SavedItem>());
            items.Sort((a, b) => string.CompareOrdinal(a?.id, b?.id));
            foreach (SavedItem item in items)
                if (item != null) text.Append(item.id).Append(':').Append(item.count).Append(';');
            text.Append('|');

            List<SavedNode> nodes = new List<SavedNode>(data.nodes ?? new List<SavedNode>());
            nodes.Sort((a, b) => string.CompareOrdinal(a?.id, b?.id));
            foreach (SavedNode node in nodes)
                if (node != null) text.Append(node.id).Append(':').Append(node.remainingMs).Append(';');

            text.Append('|').Append(data.chickenPhase).Append('|').Append(data.chickenRemainingMs).Append('|')
                .Append(data.explorationStep).Append('|').Append(data.streamQuest).Append('|').Append(data.resonanceStage);

            uint hash = 2166136261;
            foreach (char c in text.ToString())
            {
                hash ^= c;
                hash *= 16777619;
            }
            return hash.ToString("x8", CultureInfo.InvariantCulture);
        }
    }
}
