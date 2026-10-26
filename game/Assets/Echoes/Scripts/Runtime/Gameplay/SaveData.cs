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
    /// Version 1 (Phase 2) ends at resonanceStage; version 2 (Phase 3) adds the garden and windmill quests and the ending.
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
        public int resonanceStage = -1;      // 0..3 (a version 1 file only ever holds 0 or 1)

        // Version 2 fields. A version 1 file has none of them; they are never read from one.
        public int gardenQuest = -1;
        public int windmillQuest = -1;
        public int endingSeen = -1;          // 0 or 1

        public string checksum;              // null/empty = missing
    }

    public sealed class LoadResult
    {
        public LoadResult(SaveStatus status, GameSession session, string message, bool usedBackup = false, int sourceVersion = 0)
        {
            Status = status;
            Session = session;
            Message = message;
            UsedBackup = usedBackup;
            SourceVersion = sourceVersion;
        }

        public SaveStatus Status { get; }
        public GameSession Session { get; }
        public string Message { get; }
        public bool UsedBackup { get; }

        /// <summary>The file's own version number (0 when it could not be read). The session is always in the current model.</summary>
        public int SourceVersion { get; }

        public bool Ok => Status == SaveStatus.Ok;

        /// <summary>True when an older save was converted; the next successful save writes the current version.</summary>
        public bool Migrated => Ok && SourceVersion > 0 && SourceVersion < SaveCodec.CurrentVersion;
    }

    /// <summary>Converts a session to and from the save format, trusting nothing it reads.</summary>
    public static class SaveCodec
    {
        public const int CurrentVersion = 2;
        public const int OldestSupportedVersion = 1;
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
                resonanceStage = (int)session.Resonance,
                gardenQuest = (int)session.GardenQuest,
                windmillQuest = (int)session.WindmillQuest,
                endingSeen = session.EndingSeen ? 1 : 0
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
            if (data.version < OldestSupportedVersion || data.version > CurrentVersion)
                return new LoadResult(SaveStatus.Unsupported, null, "save version " + data.version + " is not supported (this game reads versions "
                    + OldestSupportedVersion + " to " + CurrentVersion + ")", false, data.version);

            // Version 1 is verified against the Version 1 field set only; the Version 2 fields are required from Version 2 on.
            bool hasV2 = data.version >= 2;
            if (data.items == null || data.nodes == null || data.coins < 0 || data.chickenPhase < 0 || data.chickenRemainingMs < 0
                || data.explorationStep < 0 || data.streamQuest < 0 || data.resonanceStage < 0 || string.IsNullOrEmpty(data.checksum)
                || (hasV2 && (data.gardenQuest < 0 || data.windmillQuest < 0 || data.endingSeen < 0)))
                return new LoadResult(SaveStatus.Invalid, null, "the save is missing required fields", false, data.version);

            if (!string.Equals(data.checksum, ComputeChecksum(data), StringComparison.Ordinal))
                return new LoadResult(SaveStatus.Corrupt, null, "the save failed its integrity check", false, data.version);

            // From here on the file is intact, so convert it to the current model: a Version 1 save has not started
            // the later quests and has not seen the ending.
            int garden = hasV2 ? data.gardenQuest : (int)QuestStage.NotStarted;
            int windmill = hasV2 ? data.windmillQuest : (int)QuestStage.NotStarted;
            int ending = hasV2 ? data.endingSeen : 0;

            if (data.coins > GameSession.MaxCoins) return Invalid(data, "coins out of range");
            if (data.chickenPhase > (int)ChickenPhase.EggReady) return Invalid(data, "unknown chicken state");
            if (data.chickenRemainingMs > MaxChickenMs) return Invalid(data, "chicken timer out of range");
            if (data.explorationStep > GameSession.ExplorationComplete) return Invalid(data, "unknown exploration step");
            if (data.streamQuest > (int)StreamQuestStage.Completed) return Invalid(data, "unknown quest state");
            if (garden > (int)QuestStage.Completed || windmill > (int)QuestStage.Completed) return Invalid(data, "unknown quest state");
            if (ending > 1) return Invalid(data, "unknown ending state");
            if (data.resonanceStage > (int)ResonanceStage.RestoredValley) return Invalid(data, "unknown resonance stage");
            if (data.version == 1 && data.resonanceStage > (int)ResonanceStage.AwakeningStream) return Invalid(data, "unknown resonance stage");

            ChickenPhase chicken = (ChickenPhase)data.chickenPhase;
            if (chicken == ChickenPhase.Producing && data.chickenRemainingMs <= 0) return Invalid(data, "producing hen with no timer");
            if (chicken != ChickenPhase.Producing && data.chickenRemainingMs != 0) return Invalid(data, "hen timer without production");

            StreamQuestStage quest = (StreamQuestStage)data.streamQuest;
            QuestStage gardenQuest = (QuestStage)garden;
            QuestStage windmillQuest = (QuestStage)windmill;
            ResonanceStage resonance = (ResonanceStage)data.resonanceStage;

            // ResonanceStage is derived from the quests, so the two have to agree. Each quest can only have progressed
            // once the one before it is completed.
            if (quest != StreamQuestStage.NotStarted && data.explorationStep != GameSession.ExplorationComplete) return Invalid(data, "quest progress without finishing exploration");
            if ((quest == StreamQuestStage.Completed) != (resonance >= ResonanceStage.AwakeningStream)) return Invalid(data, "quest and resonance stage disagree");
            if (gardenQuest != QuestStage.NotStarted && resonance < ResonanceStage.AwakeningStream) return Invalid(data, "garden quest before the stream was restored");
            if ((gardenQuest == QuestStage.Completed) != (resonance >= ResonanceStage.BloomingGarden)) return Invalid(data, "garden quest and resonance stage disagree");
            if (windmillQuest != QuestStage.NotStarted && resonance < ResonanceStage.BloomingGarden) return Invalid(data, "windmill quest before the garden was restored");
            if ((windmillQuest == QuestStage.Completed) != (resonance == ResonanceStage.RestoredValley)) return Invalid(data, "windmill quest and resonance stage disagree");
            if (ending == 1 && resonance != ResonanceStage.RestoredValley) return Invalid(data, "ending seen before the valley was restored");

            List<ItemAmount> items = new List<ItemAmount>();
            foreach (SavedItem item in data.items)
            {
                if (item == null || !ItemCatalog.IsKnown(item.id)) return Invalid(data, "unknown item");
                items.Add(new ItemAmount(item.id, item.count));
            }

            Dictionary<string, float> timers = new Dictionary<string, float>();
            foreach (SavedNode node in data.nodes)
            {
                if (node == null || string.IsNullOrEmpty(node.id)) return Invalid(data, "node without id");
                if (node.remainingMs < 0 || node.remainingMs > MaxNodeMs) return Invalid(data, "node timer out of range");
                if (timers.ContainsKey(node.id)) return Invalid(data, "duplicate node " + node.id);
                timers[node.id] = node.remainingMs / 1000f;
            }

            GameSession session = new GameSession();
            if (session.Inventory.Restore(items) != Outcome.Ok) return Invalid(data, "invalid item quantities");

            session.RestoreCore(data.coins, items, chicken, data.chickenRemainingMs / 1000f, data.explorationStep, quest,
                gardenQuest, windmillQuest, ending == 1, resonance, timers);
            return new LoadResult(SaveStatus.Ok, session, "ok", false, data.version);
        }

        static LoadResult Invalid(SaveData data, string reason) =>
            new LoadResult(SaveStatus.Invalid, null, "the save contains invalid data (" + reason + ")", false, data.version);

        static int ToMs(float seconds, int maxMs)
        {
            if (float.IsNaN(seconds) || seconds <= 0f) return 0;
            double ms = Math.Round(seconds * 1000.0);
            return (int)Math.Min(ms, maxMs);
        }

        /// <summary>
        /// FNV-1a over a canonical text form. It catches accidental damage and hand edits; it is not security.
        /// A Version 1 file is hashed over exactly the text Phase 2 produced, so every genuine Phase 2 save still verifies;
        /// Version 2 appends its own fields to that text.
        /// </summary>
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

            if (data.version >= 2)
                text.Append('|').Append(data.gardenQuest).Append('|').Append(data.windmillQuest).Append('|').Append(data.endingSeen);

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
