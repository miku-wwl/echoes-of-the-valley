using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace Echoes.Tests
{
    public class SaveTests
    {
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

        static GameSession PlayedSession()
        {
            GameSession session = new GameSession();
            session.RegisterNode("herb_a", ItemIds.Herb, 1, 40f);
            session.RegisterNode("ore_a", ItemIds.Ore, 1, 75f);
            session.SetExplorationStep(GameSession.ExplorationComplete);
            session.TryGather("herb_a");
            session.Tick(12.5f);
            session.Inventory.TryAdd(ItemIds.Flower, 2);
            session.Inventory.TryAdd(ItemIds.ChickenFeed, 1);
            session.TryFeedChicken();
            session.Tick(4f);
            return session;
        }

        static string Json(GameSession session) => SaveCodec.Serialize(SaveCodec.ToData(session));

        static string Json(SaveData data)
        {
            data.checksum = SaveCodec.ComputeChecksum(data);
            return SaveCodec.Serialize(data);
        }

        // ---- Round trips ----------------------------------------------------------------------------------------------

        [Test]
        public void FreshSession_RoundTripsThroughJson()
        {
            LoadResult result = SaveCodec.Parse(Json(new GameSession()));

            Assert.AreEqual(SaveStatus.Ok, result.Status, result.Message);
            Assert.AreEqual(GameSession.StartingCoins, result.Session.Coins);
            Assert.AreEqual(0, result.Session.Inventory.UsedSlots);
            Assert.AreEqual(ChickenPhase.Hungry, result.Session.Chicken);
            Assert.AreEqual(StreamQuestStage.NotStarted, result.Session.StreamQuest);
            Assert.AreEqual(ResonanceStage.SilentValley, result.Session.Resonance);
        }

        [Test]
        public void PlayedSession_RoundTripsEveryField()
        {
            GameSession original = PlayedSession();

            LoadResult result = SaveCodec.Parse(Json(original));

            Assert.AreEqual(SaveStatus.Ok, result.Status, result.Message);
            GameSession loaded = result.Session;
            Assert.AreEqual(original.Coins, loaded.Coins);
            Assert.AreEqual(original.ExplorationStep, loaded.ExplorationStep);
            Assert.AreEqual(1, loaded.Inventory.Count(ItemIds.Herb));
            Assert.AreEqual(2, loaded.Inventory.Count(ItemIds.Flower));
            Assert.AreEqual(0, loaded.Inventory.Count(ItemIds.ChickenFeed));
            Assert.AreEqual(ChickenPhase.Producing, loaded.Chicken);
            Assert.AreEqual(original.ChickenRemaining, loaded.ChickenRemaining, 0.002f);

            // node timers come back once the scene registers its nodes
            loaded.RegisterNode("herb_a", ItemIds.Herb, 1, 40f);
            loaded.RegisterNode("ore_a", ItemIds.Ore, 1, 75f);
            Assert.IsFalse(loaded.IsNodeReady("herb_a"));
            Assert.AreEqual(original.NodeRemaining("herb_a"), loaded.NodeRemaining("herb_a"), 0.002f);
            Assert.IsTrue(loaded.IsNodeReady("ore_a"));
        }

        [Test]
        public void CompletedRestoration_StaysCompleted_AndPaysNothingTwice()
        {
            GameSession session = new GameSession();
            session.SetExplorationStep(GameSession.ExplorationComplete);
            session.TryStartStreamQuest();
            session.Inventory.TryAdd(ItemIds.SpringTonic, 1);
            session.TryDeliverSpringTonic();
            int coins = session.Coins;

            GameSession loaded = SaveCodec.Parse(Json(session)).Session;

            Assert.IsNotNull(loaded);
            Assert.AreEqual(StreamQuestStage.Completed, loaded.StreamQuest);
            Assert.AreEqual(ResonanceStage.AwakeningStream, loaded.Resonance);
            Assert.AreEqual(coins, loaded.Coins);

            loaded.Inventory.TryAdd(ItemIds.SpringTonic, 1);
            Assert.AreEqual(Outcome.WrongState, loaded.TryDeliverSpringTonic(), "loading a finished quest must not allow a second reward");
            Assert.AreEqual(coins, loaded.Coins);
            Assert.AreEqual(1, loaded.Inventory.Count(ItemIds.SpringTonic));
        }

        [Test]
        public void NewGame_IsCleanEvenAfterALoad()
        {
            GameSession loaded = SaveCodec.Parse(Json(PlayedSession())).Session;
            Assert.AreNotEqual(0, loaded.Inventory.UsedSlots);

            GameSession fresh = new GameSession();
            Assert.AreEqual(0, fresh.Inventory.UsedSlots);
            Assert.AreEqual(GameSession.StartingCoins, fresh.Coins);
            Assert.AreEqual(0, fresh.ExplorationStep);
            Assert.AreEqual(ChickenPhase.Hungry, fresh.Chicken);
        }

        [Test]
        public void ProducingHenWithAnAlmostFinishedTimer_StillRoundTrips()
        {
            GameSession session = new GameSession();
            session.Inventory.TryAdd(ItemIds.ChickenFeed, 1);
            session.TryFeedChicken();
            session.Tick(GameSession.ChickenProductionSeconds - 0.0004f);

            LoadResult result = SaveCodec.Parse(Json(session));

            Assert.AreEqual(SaveStatus.Ok, result.Status, result.Message);
            Assert.AreEqual(ChickenPhase.Producing, result.Session.Chicken);
        }

        // ---- Bad input -----------------------------------------------------------------------------------------------

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("this is not json")]
        [TestCase("{{{{")]
        public void CorruptJson_IsRejected_AsCorrupt_WithoutThrowing(string text)
        {
            LoadResult result = SaveCodec.Parse(text);
            Assert.AreEqual(SaveStatus.Corrupt, result.Status, text);
            Assert.IsNull(result.Session);
        }

        [Test]
        public void TruncatedJson_IsNeverAccepted()
        {
            string valid = Json(PlayedSession());
            // cut anywhere before the closing lines; a payload that lost real content must never load
            for (int length = 1; length < valid.Length - 4; length += 7)
            {
                LoadResult result = SaveCodec.Parse(valid.Substring(0, length));
                Assert.AreNotEqual(SaveStatus.Ok, result.Status, "accepted a save cut off after " + length + " characters");
                Assert.IsNull(result.Session);
            }
        }

        [Test]
        public void MissingFields_AreRejected()
        {
            Assert.AreEqual(SaveStatus.Invalid, SaveCodec.Parse("{}").Status, "no version");
            Assert.AreEqual(SaveStatus.Invalid, SaveCodec.Parse("{\"version\":1}").Status, "everything else missing");

            string valid = Json(new GameSession());
            string noChecksum = Regex.Replace(valid, "\"checksum\"\\s*:\\s*\"[^\"]*\"", "\"unrelated\": 1");
            Assert.AreNotEqual(valid, noChecksum);
            Assert.AreEqual(SaveStatus.Invalid, SaveCodec.Parse(noChecksum).Status);

            string noCoins = Regex.Replace(valid, "\"coins\"\\s*:\\s*\\d+,?", string.Empty);
            Assert.AreNotEqual(valid, noCoins);
            Assert.AreEqual(SaveStatus.Invalid, SaveCodec.Parse(noCoins).Status);
        }

        [Test]
        public void UnsupportedVersion_IsRejected()
        {
            SaveData data = SaveCodec.ToData(new GameSession());
            data.version = SaveCodec.CurrentVersion + 1;

            LoadResult result = SaveCodec.Parse(Json(data));

            Assert.AreEqual(SaveStatus.Unsupported, result.Status);
            StringAssert.Contains("version", result.Message);

            data.version = 0;
            Assert.AreEqual(SaveStatus.Invalid, SaveCodec.Parse(Json(data)).Status);
        }

        [Test]
        public void HandEditedValues_FailTheIntegrityCheck()
        {
            string valid = Json(new GameSession());
            string tampered = Regex.Replace(valid, "\"coins\"\\s*:\\s*\\d+", "\"coins\": 9000");
            Assert.AreNotEqual(valid, tampered);

            LoadResult result = SaveCodec.Parse(tampered);

            Assert.AreEqual(SaveStatus.Corrupt, result.Status);
            Assert.IsNull(result.Session, "damaged data must never become a game");
        }

        [Test]
        public void InvalidQuantities_AreRejected_EvenWithAMatchingChecksum()
        {
            AssertInvalid(d => d.coins = GameSession.MaxCoins + 1, "coins above the cap");
            AssertInvalid(d => d.items.Add(new SavedItem { id = ItemIds.Herb, count = 0 }), "zero quantity");
            AssertInvalid(d => d.items.Add(new SavedItem { id = ItemIds.Herb, count = -2 }), "negative quantity");
            AssertInvalid(d => d.items.Add(new SavedItem { id = ItemIds.Herb, count = Inventory.DefaultStackLimit + 1 }), "over the stack limit");
            AssertInvalid(d => d.items.Add(new SavedItem { id = "mystery_item", count = 1 }), "unknown item");
            AssertInvalid(d =>
            {
                d.items.Add(new SavedItem { id = ItemIds.Herb, count = 1 });
                d.items.Add(new SavedItem { id = ItemIds.Herb, count = 1 });
            }, "the same item twice");
            AssertInvalid(d => d.nodes.Add(new SavedNode { id = "herb_a", remainingMs = -5 }), "negative node timer");
            AssertInvalid(d => d.nodes.Add(new SavedNode { id = "herb_a", remainingMs = SaveCodec.MaxNodeMs + 1 }), "node timer too long");
            AssertInvalid(d => d.nodes.Add(new SavedNode { id = string.Empty, remainingMs = 5 }), "node without id");
            AssertInvalid(d =>
            {
                d.nodes.Add(new SavedNode { id = "herb_a", remainingMs = 5 });
                d.nodes.Add(new SavedNode { id = "herb_a", remainingMs = 5 });
            }, "duplicate node");
            AssertInvalid(d => d.chickenPhase = 7, "unknown hen state");
            AssertInvalid(d => { d.chickenPhase = (int)ChickenPhase.Producing; d.chickenRemainingMs = 0; }, "producing with no timer");
            AssertInvalid(d => { d.chickenPhase = (int)ChickenPhase.Hungry; d.chickenRemainingMs = 5000; }, "timer without production");
            AssertInvalid(d => d.explorationStep = 9, "unknown exploration step");
            AssertInvalid(d => d.streamQuest = 5, "unknown quest state");
            AssertInvalid(d => d.resonanceStage = 4, "unknown stage");
            AssertInvalid(d => d.resonanceStage = (int)ResonanceStage.AwakeningStream, "stage 1 without the quest done");
            AssertInvalid(d => d.streamQuest = (int)StreamQuestStage.Active, "quest progress before finishing exploration");
        }

        static void AssertInvalid(Action<SaveData> corrupt, string because)
        {
            SaveData data = SaveCodec.ToData(new GameSession());
            corrupt(data);

            LoadResult result = SaveCodec.Parse(Json(data));

            Assert.AreNotEqual(SaveStatus.Ok, result.Status, because);
            Assert.IsNull(result.Session, because);
        }

        // ---- Files: temp file, verification, backup, recovery ------------------------------------------------------------

        [Test]
        public void MissingSave_IsReportedAsMissing()
        {
            LoadResult result = SaveSystem.Load();
            Assert.AreEqual(SaveStatus.Missing, result.Status);
            Assert.IsNull(result.Session);
        }

        [Test]
        public void Save_WritesTheMainFile_AndLeavesNoTempFile()
        {
            Assert.IsTrue(SaveSystem.Save(PlayedSession(), out string message), message);

            Assert.IsTrue(File.Exists(SaveSystem.MainPath));
            Assert.IsFalse(File.Exists(SaveSystem.TempPath), "the temp file is replaced, not left behind");
            Assert.AreEqual(SaveStatus.Ok, SaveSystem.Load().Status);
        }

        [Test]
        public void SecondSave_KeepsThePreviousGoodSaveAsBackup()
        {
            GameSession session = new GameSession();
            SaveSystem.Save(session, out _);
            session.TryAddCoins(7);
            SaveSystem.Save(session, out _);

            Assert.IsTrue(File.Exists(SaveSystem.BackupPath));
            Assert.AreEqual(GameSession.StartingCoins + 7, SaveSystem.Load().Session.Coins);
            Assert.AreEqual(GameSession.StartingCoins, SaveCodec.Parse(File.ReadAllText(SaveSystem.BackupPath)).Session.Coins, "the backup is the earlier save");
        }

        [Test]
        public void CorruptMainSave_FallsBackToTheBackup_AndSaysSo()
        {
            GameSession session = new GameSession();
            SaveSystem.Save(session, out _);
            session.TryAddCoins(7);
            SaveSystem.Save(session, out _);
            File.WriteAllText(SaveSystem.MainPath, "{ this is damaged");

            LoadResult result = SaveSystem.Load();

            Assert.AreEqual(SaveStatus.Ok, result.Status);
            Assert.IsTrue(result.UsedBackup);
            StringAssert.Contains("backup", result.Message);
            Assert.AreEqual(GameSession.StartingCoins, result.Session.Coins, "recovered state is the previous good save, not invented data");
        }

        [Test]
        public void CorruptMainAndBackup_ReportCorruption_AndNeverProduceASession()
        {
            GameSession session = new GameSession();
            SaveSystem.Save(session, out _);
            SaveSystem.Save(session, out _);
            File.WriteAllText(SaveSystem.MainPath, "garbage");
            File.WriteAllText(SaveSystem.BackupPath, "more garbage");

            LoadResult result = SaveSystem.Load();

            Assert.AreEqual(SaveStatus.Corrupt, result.Status);
            Assert.IsNull(result.Session);
        }

        [Test]
        public void SavingOverACorruptMainFile_DoesNotDestroyAGoodBackup()
        {
            GameSession session = new GameSession();
            SaveSystem.Save(session, out _);
            session.TryAddCoins(3);
            SaveSystem.Save(session, out _);   // main = 23 coins, backup = 20
            File.WriteAllText(SaveSystem.MainPath, "damaged");
            session.TryAddCoins(1);

            Assert.IsTrue(SaveSystem.Save(session, out string message), message);

            Assert.AreEqual(GameSession.StartingCoins + 4, SaveSystem.Load().Session.Coins);
            Assert.AreEqual(GameSession.StartingCoins, SaveCodec.Parse(File.ReadAllText(SaveSystem.BackupPath)).Session.Coins,
                "the damaged file must not replace the good backup");
        }

        [Test]
        public void FailedWrite_LeavesTheExistingSaveUntouched()
        {
            GameSession session = new GameSession();
            Assert.IsTrue(SaveSystem.Save(session, out _));
            string before = File.ReadAllText(SaveSystem.MainPath);

            Directory.CreateDirectory(SaveSystem.TempPath);   // a directory where the temp file belongs makes the write fail
            session.TryAddCoins(9);
            bool saved = SaveSystem.Save(session, out string message);
            Directory.Delete(SaveSystem.TempPath);

            Assert.IsFalse(saved);
            StringAssert.Contains("Could not save", message);
            Assert.AreEqual(before, File.ReadAllText(SaveSystem.MainPath));
            Assert.AreEqual(GameSession.StartingCoins, SaveSystem.Load().Session.Coins);
        }

        [Test]
        public void SavedFile_IsReadableJson_WithAnExplicitVersion()
        {
            SaveSystem.Save(new GameSession(), out _);
            string text = File.ReadAllText(SaveSystem.MainPath);

            StringAssert.Contains("\"version\": " + SaveCodec.CurrentVersion, text);
            StringAssert.Contains("\"items\"", text);
            StringAssert.Contains("\"checksum\"", text);
            Assert.DoesNotThrow(() => JsonUtility.FromJson<SaveData>(text));
        }
    }
}
