using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace Echoes.Tests
{
    /// <summary>
    /// Save format version 2 and compatibility with version 1. The two version 1 fixtures are real files written by the
    /// Phase 2 Windows player (smoke run, 2026-10-25), so they carry checksums produced by the original code. The legacy
    /// checksum below is an independent copy of that algorithm, so these tests do not grade the new code with itself.
    /// </summary>
    public class Phase3SaveTests
    {
        const string RealPhase2Save =
            "{\"version\":1,\"coins\":45,\"items\":[{\"id\":\"chicken_feed\",\"count\":2}],\"nodes\":[{\"id\":\"herb_6\",\"remainingMs\":0},{\"id\":\"herb_4\",\"remainingMs\":0},"
            + "{\"id\":\"ore_3\",\"remainingMs\":0},{\"id\":\"flower_2\",\"remainingMs\":34730},{\"id\":\"herb_2\",\"remainingMs\":34630},{\"id\":\"flower_4\",\"remainingMs\":0},"
            + "{\"id\":\"herb_5\",\"remainingMs\":0},{\"id\":\"flower_1\",\"remainingMs\":34680},{\"id\":\"herb_3\",\"remainingMs\":0},{\"id\":\"ore_2\",\"remainingMs\":0},"
            + "{\"id\":\"ore_1\",\"remainingMs\":0},{\"id\":\"flower_5\",\"remainingMs\":0},{\"id\":\"ore_4\",\"remainingMs\":0},{\"id\":\"herb_1\",\"remainingMs\":34547},"
            + "{\"id\":\"flower_3\",\"remainingMs\":0}],\"chickenPhase\":1,\"chickenRemainingMs\":9914,\"explorationStep\":3,\"streamQuest\":2,\"resonanceStage\":1,"
            + "\"checksum\":\"71c44e3d\"}";

        const string RealPhase2Backup =
            "{\"version\":1,\"coins\":45,\"items\":[{\"id\":\"chicken_feed\",\"count\":2}],\"nodes\":[{\"id\":\"herb_6\",\"remainingMs\":0},{\"id\":\"herb_4\",\"remainingMs\":0},"
            + "{\"id\":\"ore_3\",\"remainingMs\":0},{\"id\":\"flower_2\",\"remainingMs\":39767},{\"id\":\"herb_2\",\"remainingMs\":39667},{\"id\":\"flower_4\",\"remainingMs\":0},"
            + "{\"id\":\"herb_5\",\"remainingMs\":0},{\"id\":\"flower_1\",\"remainingMs\":39717},{\"id\":\"herb_3\",\"remainingMs\":0},{\"id\":\"ore_2\",\"remainingMs\":0},"
            + "{\"id\":\"ore_1\",\"remainingMs\":0},{\"id\":\"flower_5\",\"remainingMs\":0},{\"id\":\"ore_4\",\"remainingMs\":0},{\"id\":\"herb_1\",\"remainingMs\":39583},"
            + "{\"id\":\"flower_3\",\"remainingMs\":0}],\"chickenPhase\":1,\"chickenRemainingMs\":14950,\"explorationStep\":3,\"streamQuest\":2,\"resonanceStage\":1,"
            + "\"checksum\":\"40fe5362\"}";

        string folder;

        [SetUp]
        public void UseTemporaryFolder()
        {
            folder = Path.Combine(Path.GetTempPath(), "echoes-tests", Guid.NewGuid().ToString("N"));
            SaveSystem.UseFolder(folder);
        }

        [TearDown]
        public void RemoveTemporaryFolder()
        {
            SaveSystem.ClearFolderOverride();
            try
            {
                if (Directory.Exists(folder)) Directory.Delete(folder, true);
            }
            catch (IOException)
            {
                // the OS may still hold a handle for a moment; the folder is in the temp directory anyway
            }
        }

        // ---- An independent model of the Phase 2 file ---------------------------------------------------------------------

        [Serializable]
        sealed class LegacySave
        {
            public int version = 1;
            public int coins;
            public List<SavedItem> items = new List<SavedItem>();
            public List<SavedNode> nodes = new List<SavedNode>();
            public int chickenPhase;
            public int chickenRemainingMs;
            public int explorationStep;
            public int streamQuest;
            public int resonanceStage;
            public string checksum;
        }

        /// <summary>The Phase 2 algorithm, re-typed: FNV-1a over "echoes-save|version|coins|items|nodes|chicken|timer|exploration|quest|stage".</summary>
        static string LegacyChecksum(LegacySave data)
        {
            StringBuilder text = new StringBuilder("echoes-save|");
            text.Append(data.version).Append('|').Append(data.coins).Append('|');
            List<SavedItem> items = new List<SavedItem>(data.items);
            items.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
            foreach (SavedItem item in items) text.Append(item.id).Append(':').Append(item.count).Append(';');
            text.Append('|');
            List<SavedNode> nodes = new List<SavedNode>(data.nodes);
            nodes.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
            foreach (SavedNode node in nodes) text.Append(node.id).Append(':').Append(node.remainingMs).Append(';');
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

        static string LegacyJson(LegacySave data)
        {
            data.checksum = LegacyChecksum(data);
            return JsonUtility.ToJson(data);
        }

        static LegacySave EarlyGameSave()
        {
            LegacySave data = new LegacySave
            {
                coins = 31, explorationStep = 3, streamQuest = 1, resonanceStage = 0, chickenPhase = 0, chickenRemainingMs = 0
            };
            data.items.Add(new SavedItem { id = ItemIds.Herb, count = 2 });
            data.items.Add(new SavedItem { id = ItemIds.Egg, count = 1 });
            data.nodes.Add(new SavedNode { id = "herb_1", remainingMs = 12000 });
            return data;
        }

        // ---- Current-version sessions ---------------------------------------------------------------------------------------

        /// <summary>0 = stream quest active, 1..3 = that resonance stage reached, 4 = stage 3 with the ending seen.</summary>
        static GameSession SessionAt(int step)
        {
            GameSession session = new GameSession();
            session.RegisterNode("herb_1", ItemIds.Herb, 1, 40f);
            session.RegisterNode("ore_1", ItemIds.Ore, 1, 75f);
            session.SetExplorationStep(GameSession.ExplorationComplete);
            session.TryStartStreamQuest();
            if (step >= 1)
            {
                session.Inventory.TryAdd(ItemIds.SpringTonic, 1);
                session.TryDeliverSpringTonic();
            }
            if (step >= 2)
            {
                session.TryStartGardenQuest();
                session.Inventory.TryAdd(ItemIds.Egg, 1);
                session.TryDeliverEgg();
            }
            if (step >= 3)
            {
                session.TryStartWindmillQuest();
                session.Inventory.TryAdd(ItemIds.WindmillBraces, 1);
                session.TryDeliverWindmillBraces();
            }
            if (step >= 4) session.TryCompleteEnding();
            session.TryGather("herb_1");
            session.Tick(7.25f);
            session.Inventory.TryAdd(ItemIds.ChickenFeed, 1);
            session.TryFeedChicken();
            session.Tick(3f);
            return session;
        }

        static string Json(GameSession session) => SaveCodec.Serialize(SaveCodec.ToData(session));

        static string Json(SaveData data)
        {
            data.checksum = SaveCodec.ComputeChecksum(data);
            return SaveCodec.Serialize(data);
        }

        static void AssertSame(GameSession expected, GameSession actual)
        {
            Assert.AreEqual(expected.Coins, actual.Coins);
            Assert.AreEqual(expected.ExplorationStep, actual.ExplorationStep);
            Assert.AreEqual(expected.StreamQuest, actual.StreamQuest);
            Assert.AreEqual(expected.GardenQuest, actual.GardenQuest);
            Assert.AreEqual(expected.WindmillQuest, actual.WindmillQuest);
            Assert.AreEqual(expected.Resonance, actual.Resonance);
            Assert.AreEqual(expected.EndingSeen, actual.EndingSeen);
            Assert.AreEqual(expected.Chicken, actual.Chicken);
            Assert.AreEqual(expected.ChickenRemaining, actual.ChickenRemaining, 0.002f);
            foreach (ItemDef item in ItemCatalog.All)
                Assert.AreEqual(expected.Inventory.Count(item.Id), actual.Inventory.Count(item.Id), item.Id);

            Dictionary<string, float> before = Timers(expected);
            Dictionary<string, float> after = Timers(actual);
            CollectionAssert.AreEquivalent(before.Keys, after.Keys);
            foreach (KeyValuePair<string, float> timer in before) Assert.AreEqual(timer.Value, after[timer.Key], 0.002f, timer.Key);
        }

        /// <summary>Node timers by id, including those of nodes the scene has not registered yet (a loaded save holds them as pending).</summary>
        static Dictionary<string, float> Timers(GameSession session)
        {
            Dictionary<string, float> timers = new Dictionary<string, float>();
            foreach (KeyValuePair<string, float> timer in session.NodeTimers()) timers[timer.Key] = timer.Value;
            return timers;
        }

        // ---- Version 1 compatibility ------------------------------------------------------------------------------------------

        [Test]
        public void TheLegacyChecksum_ReproducesTheRealPlayerFiles_AndTheCurrentCodeAgrees()
        {
            foreach (KeyValuePair<string, string> file in new Dictionary<string, string> { { RealPhase2Save, "71c44e3d" }, { RealPhase2Backup, "40fe5362" } })
            {
                LegacySave legacy = JsonUtility.FromJson<LegacySave>(file.Key);
                Assert.AreEqual(file.Value, legacy.checksum, "fixture text");
                Assert.AreEqual(file.Value, LegacyChecksum(legacy), "the independent Phase 2 algorithm matches the real file");

                SaveData data = new SaveData();
                JsonUtility.FromJsonOverwrite(file.Key, data);
                Assert.AreEqual(file.Value, SaveCodec.ComputeChecksum(data), "the current code still hashes a version 1 file the way Phase 2 did");
            }
        }

        [Test]
        public void ARealPhase2Save_LoadsWithEverythingIntact_AndIsReportedAsMigrated()
        {
            LoadResult result = SaveCodec.Parse(RealPhase2Save);

            Assert.AreEqual(SaveStatus.Ok, result.Status, result.Message);
            Assert.AreEqual(1, result.SourceVersion);
            Assert.IsTrue(result.Migrated);
            GameSession session = result.Session;
            Assert.AreEqual(45, session.Coins);
            Assert.AreEqual(2, session.Inventory.Count(ItemIds.ChickenFeed));
            Assert.AreEqual(1, session.Inventory.UsedSlots);
            Assert.AreEqual(ChickenPhase.Producing, session.Chicken);
            Assert.AreEqual(9.914f, session.ChickenRemaining, 0.001f);
            Assert.AreEqual(GameSession.ExplorationComplete, session.ExplorationStep);
            Assert.AreEqual(StreamQuestStage.Completed, session.StreamQuest);
            Assert.AreEqual(ResonanceStage.AwakeningStream, session.Resonance);
            Dictionary<string, float> timers = Timers(session);
            Assert.AreEqual(15, timers.Count, "every node timer is kept");
            Assert.AreEqual(34.73f, timers["flower_2"], 0.001f);
            Assert.AreEqual(0f, timers["ore_1"], 0.001f);

            // Defaults for what Phase 2 did not know about
            Assert.AreEqual(QuestStage.NotStarted, session.GardenQuest);
            Assert.AreEqual(QuestStage.NotStarted, session.WindmillQuest);
            Assert.IsFalse(session.EndingSeen);
            Assert.AreEqual(Outcome.Ok, session.TryStartGardenQuest(), "the migrated game can go straight on to the garden quest");
        }

        [Test]
        public void ARealPhase2Backup_AlsoLoads()
        {
            LoadResult result = SaveCodec.Parse(RealPhase2Backup);
            Assert.AreEqual(SaveStatus.Ok, result.Status, result.Message);
            Assert.AreEqual(14.95f, result.Session.ChickenRemaining, 0.001f);
            Assert.AreEqual(ResonanceStage.AwakeningStream, result.Session.Resonance);
        }

        [Test]
        public void AnEarlyVersion1Save_KeepsItsProgress_AndStartsTheLaterQuestsFromScratch()
        {
            LoadResult result = SaveCodec.Parse(LegacyJson(EarlyGameSave()));

            Assert.AreEqual(SaveStatus.Ok, result.Status, result.Message);
            Assert.AreEqual(StreamQuestStage.Active, result.Session.StreamQuest);
            Assert.AreEqual(ResonanceStage.SilentValley, result.Session.Resonance);
            Assert.AreEqual(31, result.Session.Coins);
            Assert.AreEqual(2, result.Session.Inventory.Count(ItemIds.Herb));
            Assert.AreEqual(1, result.Session.Inventory.Count(ItemIds.Egg), "an egg carried since Phase 2 is still there for the garden quest");
            Assert.AreEqual(QuestStage.NotStarted, result.Session.GardenQuest);
            Assert.AreEqual(Outcome.Locked, result.Session.TryStartGardenQuest(), "the stream still has to be restored first");
        }

        [Test]
        public void AMigratedSave_IsWrittenAsVersionTwo_AndRoundTrips()
        {
            GameSession migrated = SaveCodec.Parse(RealPhase2Save).Session;

            SaveData data = SaveCodec.ToData(migrated);
            Assert.AreEqual(2, data.version);
            Assert.AreEqual(0, data.gardenQuest);
            Assert.AreEqual(0, data.windmillQuest);
            Assert.AreEqual(0, data.endingSeen);
            Assert.AreNotEqual("71c44e3d", data.checksum, "version 2 hashes its own field set");

            LoadResult again = SaveCodec.Parse(SaveCodec.Serialize(data));
            Assert.AreEqual(SaveStatus.Ok, again.Status, again.Message);
            Assert.AreEqual(2, again.SourceVersion);
            Assert.IsFalse(again.Migrated);
            AssertSame(migrated, again.Session);
        }

        [Test]
        public void Version1_IsVerifiedOnItsOwnFields_AnyEditFailsTheCheck()
        {
            string[] edits =
            {
                RealPhase2Save.Replace("\"coins\":45", "\"coins\":46"),
                RealPhase2Save.Replace("\"remainingMs\":34730", "\"remainingMs\":34731"),
                RealPhase2Save.Replace("\"chickenRemainingMs\":9914", "\"chickenRemainingMs\":9915"),
                RealPhase2Save.Replace("\"count\":2", "\"count\":3"),
                RealPhase2Save.Replace("\"streamQuest\":2", "\"streamQuest\":1")
            };
            foreach (string edited in edits)
            {
                Assert.AreNotEqual(RealPhase2Save, edited, "the edit applied");
                Assert.AreEqual(SaveStatus.Corrupt, SaveCodec.Parse(edited).Status, edited);
            }
        }

        [Test]
        public void Version1_NeverReadsVersionTwoFields_EvenIfSomeoneAddedThem()
        {
            string withExtras = RealPhase2Save.Replace("\"checksum\"", "\"gardenQuest\":2,\"windmillQuest\":2,\"endingSeen\":1,\"checksum\"");

            LoadResult result = SaveCodec.Parse(withExtras);

            Assert.AreEqual(SaveStatus.Ok, result.Status, "the version 1 checksum does not cover them: " + result.Message);
            Assert.AreEqual(QuestStage.NotStarted, result.Session.GardenQuest, "unverified extra fields must not grant progress");
            Assert.AreEqual(QuestStage.NotStarted, result.Session.WindmillQuest);
            Assert.IsFalse(result.Session.EndingSeen);
            Assert.AreEqual(ResonanceStage.AwakeningStream, result.Session.Resonance);
        }

        [Test]
        public void Version1_ClaimingAStageItCouldNeverHold_IsInvalid_EvenWithAValidChecksum()
        {
            LegacySave data = EarlyGameSave();
            data.streamQuest = 2;
            data.resonanceStage = 2;

            Assert.AreEqual(SaveStatus.Invalid, SaveCodec.Parse(LegacyJson(data)).Status);
        }

        // ---- Version 2 ---------------------------------------------------------------------------------------------------------

        [Test]
        public void Version2_RoundTripsEveryStage_AndTheEnding([Values(0, 1, 2, 3, 4)] int step)
        {
            GameSession original = SessionAt(step);

            LoadResult result = SaveCodec.Parse(Json(original));

            Assert.AreEqual(SaveStatus.Ok, result.Status, result.Message);
            AssertSame(original, result.Session);
            Assert.AreEqual(step >= 3 ? ResonanceStage.RestoredValley : (ResonanceStage)step, result.Session.Resonance);
            Assert.AreEqual(step >= 4, result.Session.EndingSeen);
        }

        [Test]
        public void AnActiveGardenQuest_AndAnActiveWindmillQuest_RoundTrip()
        {
            GameSession garden = SessionAt(1);
            garden.TryStartGardenQuest();
            LoadResult gardenResult = SaveCodec.Parse(Json(garden));
            Assert.AreEqual(QuestStage.Active, gardenResult.Session.GardenQuest);
            Assert.AreEqual(ResonanceStage.AwakeningStream, gardenResult.Session.Resonance);

            GameSession windmill = SessionAt(2);
            windmill.TryStartWindmillQuest();
            LoadResult windmillResult = SaveCodec.Parse(Json(windmill));
            Assert.AreEqual(QuestStage.Active, windmillResult.Session.WindmillQuest);
            Assert.AreEqual(ResonanceStage.BloomingGarden, windmillResult.Session.Resonance);
        }

        [Test]
        public void ALoadedEndingIsNotRecordedAgain_AndLoadingRaisesNoMilestone()
        {
            GameSession loaded = SaveCodec.Parse(Json(SessionAt(4))).Session;
            List<string> milestones = new List<string>();
            loaded.Milestone += milestones.Add;

            Assert.IsTrue(loaded.EndingSeen);
            Assert.AreEqual(Outcome.WrongState, loaded.TryCompleteEnding());
            Assert.AreEqual(Outcome.WrongState, loaded.TryDeliverEgg());
            Assert.AreEqual(Outcome.WrongState, loaded.TryDeliverWindmillBraces());
            Assert.IsEmpty(milestones);
        }

        [Test]
        public void Version2_RequiresItsNewFields()
        {
            foreach (Action<SaveData> remove in new Action<SaveData>[] { d => d.gardenQuest = -1, d => d.windmillQuest = -1, d => d.endingSeen = -1 })
            {
                SaveData data = SaveCodec.ToData(SessionAt(2));
                remove(data);
                Assert.AreEqual(SaveStatus.Invalid, SaveCodec.Parse(Json(data)).Status);
            }
        }

        [Test]
        public void Version2_ChecksumCoversTheNewFields()
        {
            foreach (Action<SaveData> edit in new Action<SaveData>[]
                     {
                         d => d.endingSeen = d.endingSeen == 1 ? 0 : 1,
                         d => d.gardenQuest = (d.gardenQuest + 1) % 3,
                         d => d.windmillQuest = (d.windmillQuest + 1) % 3
                     })
            {
                SaveData data = SaveCodec.ToData(SessionAt(3));
                edit(data);   // checksum left as it was
                Assert.AreEqual(SaveStatus.Corrupt, SaveCodec.Parse(SaveCodec.Serialize(data)).Status);
            }
        }

        [Test]
        public void Version2_InconsistentProgress_IsRejected_EvenWithAValidChecksum()
        {
            AssertRejected(1, d => d.gardenQuest = (int)QuestStage.Completed, "garden completed at stage 1");
            AssertRejected(0, d => d.gardenQuest = (int)QuestStage.Active, "garden active before the stream is restored");
            AssertRejected(2, d => d.gardenQuest = (int)QuestStage.Active, "garden still active at stage 2");
            AssertRejected(2, d => d.windmillQuest = (int)QuestStage.Completed, "windmill completed at stage 2");
            AssertRejected(1, d => d.windmillQuest = (int)QuestStage.Active, "windmill active at stage 1");
            AssertRejected(3, d => d.windmillQuest = (int)QuestStage.Active, "windmill still active at stage 3");
            AssertRejected(3, d => d.windmillQuest = (int)QuestStage.NotStarted, "stage 3 without the windmill quest done");
            AssertRejected(3, d => d.gardenQuest = (int)QuestStage.NotStarted, "stage 3 without the garden quest done");
            AssertRejected(2, d => d.endingSeen = 1, "ending seen at stage 2");
            AssertRejected(3, d => d.endingSeen = 2, "an ending value that is not 0 or 1");
            AssertRejected(2, d => d.resonanceStage = 4, "a stage past the last one");
            AssertRejected(2, d => d.gardenQuest = 3, "a quest value past the last one");
            AssertRejected(2, d => d.windmillQuest = 3, "a quest value past the last one");
        }

        static void AssertRejected(int step, Action<SaveData> corrupt, string because)
        {
            SaveData data = SaveCodec.ToData(SessionAt(step));
            corrupt(data);
            LoadResult result = SaveCodec.Parse(Json(data));   // the checksum is recomputed, so only validation can catch it
            Assert.AreEqual(SaveStatus.Invalid, result.Status, because + " (" + result.Message + ")");
            Assert.IsNull(result.Session);
        }

        [Test]
        public void VersionNumbers_AreValidated()
        {
            SaveData data = SaveCodec.ToData(SessionAt(2));

            data.version = 0;
            Assert.AreEqual(SaveStatus.Invalid, SaveCodec.Parse(Json(data)).Status, "no version");
            data.version = -1;
            Assert.AreEqual(SaveStatus.Invalid, SaveCodec.Parse(Json(data)).Status, "negative version");
            data.version = SaveCodec.CurrentVersion + 1;
            LoadResult future = SaveCodec.Parse(Json(data));
            Assert.AreEqual(SaveStatus.Unsupported, future.Status, "a newer game's file");
            StringAssert.Contains((SaveCodec.CurrentVersion + 1).ToString(), future.Message);
            Assert.AreEqual(2, SaveCodec.CurrentVersion);
            Assert.AreEqual(1, SaveCodec.OldestSupportedVersion);
        }

        // ---- On disk -----------------------------------------------------------------------------------------------------------

        [Test]
        public void SavingOverAVersion1File_KeepsItAsTheLoadableBackup()
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText(SaveSystem.MainPath, RealPhase2Save);
            Assert.IsTrue(SaveSystem.Load().Migrated, "the Phase 2 save loads as it is");

            GameSession played = SaveSystem.Load().Session;
            played.TryStartGardenQuest();
            Assert.IsTrue(SaveSystem.Save(played, out string message), message);

            LoadResult main = SaveSystem.Load();
            Assert.AreEqual(2, main.SourceVersion, "the next save writes version 2");
            Assert.AreEqual(QuestStage.Active, main.Session.GardenQuest);
            Assert.AreEqual(RealPhase2Save, File.ReadAllText(SaveSystem.BackupPath), "the old file is kept untouched as the backup");
            Assert.IsTrue(SaveCodec.Parse(File.ReadAllText(SaveSystem.BackupPath)).Ok);
        }

        [Test]
        public void ACorruptVersion2Main_FallsBackToTheVersion1Backup_AndSaysSo()
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText(SaveSystem.MainPath, "this is not a save");
            File.WriteAllText(SaveSystem.BackupPath, RealPhase2Save);

            LoadResult result = SaveSystem.Load();

            Assert.IsTrue(result.Ok);
            Assert.IsTrue(result.UsedBackup);
            Assert.IsTrue(result.Migrated);
            StringAssert.Contains("backup", result.Message);
            Assert.AreEqual(45, result.Session.Coins);
        }

        [Test]
        public void ACorruptMainAndABadBackup_NeverProduceASession()
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText(SaveSystem.MainPath, RealPhase2Save.Replace("\"coins\":45", "\"coins\":99"));
            File.WriteAllText(SaveSystem.BackupPath, RealPhase2Backup.Replace("\"coins\":45", "\"coins\":99"));

            LoadResult result = SaveSystem.Load();

            Assert.IsFalse(result.Ok);
            Assert.IsNull(result.Session);
        }

        [Test]
        public void AnUnreadableMainFile_IsSetAsideNotDeleted_WhenANewGameSaves()
        {
            Directory.CreateDirectory(folder);
            string future = "{ \"version\": 99, \"note\": \"from a newer game\" }";
            File.WriteAllText(SaveSystem.MainPath, future);
            Assert.IsTrue(SaveSystem.HasSaveFiles);
            Assert.AreEqual(SaveStatus.Unsupported, SaveSystem.Load().Status);

            Assert.IsTrue(SaveSystem.Save(new GameSession(), out string message), message);

            Assert.AreEqual(future, File.ReadAllText(SaveSystem.SetAsidePath), "the unreadable file is kept");
            Assert.IsTrue(SaveSystem.Load().Ok, "and the new save is the main file");
            Assert.IsFalse(File.Exists(SaveSystem.BackupPath), "an unreadable file is never promoted to the backup");
            Assert.IsFalse(File.Exists(SaveSystem.TempPath));
        }

        [Test]
        public void HasSaveFiles_ReflectsTheFolder()
        {
            Assert.IsFalse(SaveSystem.HasSaveFiles);
            Directory.CreateDirectory(folder);
            File.WriteAllText(SaveSystem.BackupPath, "x");
            Assert.IsTrue(SaveSystem.HasSaveFiles, "a lone backup counts");
        }
    }
}
