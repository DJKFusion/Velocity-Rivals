using System;
using System.IO;
using NUnit.Framework;

namespace VelocityRivals.Tests
{
    public sealed class ProgressAndSaveTests
    {
        [Test]
        public void FinishLineCannotSkipOrderedCheckpoints()
        {
            var race = new RaceProgress(4, 2);
            Assert.IsFalse(race.Cross(0, true));
            Assert.IsFalse(race.Cross(2, true));
            Assert.IsFalse(race.Cross(1, false));
            Assert.AreEqual(0, race.CompletedLaps);
            Assert.AreEqual(1, race.NextCheckpoint);
        }

        [Test]
        public void CompletesExactlyRequiredLapsThenStopsCounting()
        {
            var race = new RaceProgress(4, 2);
            for (int lap = 0; lap < 2; lap++)
            {
                foreach (int checkpoint in new[] { 1, 2, 3, 0 }) Assert.IsTrue(race.Cross(checkpoint, true));
                Assert.AreEqual(lap + 1, race.CompletedLaps);
                Assert.AreEqual(lap == 1, race.Complete);
            }
            Assert.IsFalse(race.Cross(1, true));
            Assert.AreEqual(2, race.CompletedLaps);
        }

        [TestCase(0, 1)] [TestCase(2, 1)] [TestCase(4, 0)]
        public void RejectsInvalidRaceConfiguration(int gates, int laps)
            => Assert.Throws<ArgumentOutOfRangeException>(() => new RaceProgress(gates, laps));

        string directory;
        string previousDirectory;
        [SetUp]
        public void IsolateProfile()
        {
            previousDirectory = SaveStore.StorageDirectoryOverride;
            directory = Path.Combine(Path.GetTempPath(), "VelocityRivalsTests-" + Guid.NewGuid());
            SaveStore.StorageDirectoryOverride = directory;
            SaveStore.Reload();
        }
        [TearDown]
        public void RestoreProfile()
        {
            SaveStore.StorageDirectoryOverride = previousDirectory;
            SaveStore.Reload();
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        [Test]
        public void CustomizationAndXpSurviveReload()
        {
            SaveStore.Current.paint = 3;
            SaveStore.Current.wheels = 2;
            SaveStore.Current.xp = 1100;
            Assert.IsTrue(SaveStore.Save());
            SaveStore.Reload();
            Assert.AreEqual(3, SaveStore.Current.paint);
            Assert.AreEqual(2, SaveStore.Current.wheels);
            Assert.AreEqual(3, SaveStore.Current.Level);
            Assert.AreEqual(3, SaveStore.Current.UnlockedVehicles);
        }

        [Test]
        public void ResultsKeepFastestRecordAndRewardProgress()
        {
            SaveStore.RecordResult("TimeAttack", true, 65, 100);
            SaveStore.RecordResult("TimeAttack", false, 72, 200);
            Assert.AreEqual(65, SaveStore.Current.records[0].bestSeconds);
            Assert.AreEqual(200, SaveStore.Current.records[0].bestScore);
            Assert.AreEqual(2, SaveStore.Current.events);
            Assert.AreEqual(1, SaveStore.Current.wins);
            Assert.Contains("First victory", SaveStore.Current.achievements);
            Assert.Greater(SaveStore.Current.xp, 0);
        }

        [Test]
        public void LegacyProfileKeepsUnlocksAndMigratesRecordsWithoutInventingTrackIdentity()
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(SaveStore.FilePath, "{\"schemaVersion\":1,\"xp\":1100,\"records\":[{\"mode\":\"TimeAttack\",\"bestSeconds\":65,\"finishes\":1}]}");
            SaveStore.Reload();
            Assert.AreEqual(2, SaveStore.Current.schemaVersion);
            Assert.AreEqual(3, SaveStore.Current.UnlockedVehicles);
            Assert.AreEqual(-1, SaveStore.Current.records[0].environment);
            Assert.AreEqual(-1, SaveStore.Current.records[0].vehicle);
            Assert.AreEqual(65, SaveStore.Current.records[0].bestSeconds);
            Assert.AreEqual(0, SaveStore.Current.career.NextEvent);
        }

        [Test]
        public void RecordsSeparateTracksAndCarsAndExcludeUnfinishedTimes()
        {
            SaveStore.RecordResult("TimeAttack", true, 100, 200, 0, 0);
            SaveStore.RecordResult("TimeAttack", true, 80, 300, 1, 0);
            SaveStore.RecordResult("TimeAttack", true, 90, 400, 0, 1);
            SaveStore.RecordResult("TimeAttack", false, 1, 0, 0, 0, false);
            Assert.AreEqual(3, SaveStore.Current.records.Count);
            Assert.AreEqual(100, SaveStore.Current.records[0].bestSeconds);
            Assert.AreEqual(1, SaveStore.Current.records[0].finishes);
            Assert.AreEqual(3, SaveStore.Current.wins);
        }

        [Test]
        public void CareerMedalsAndDisplayPreferencesSurviveReload()
        {
            SaveStore.Current.career.Record(0, 3);
            SaveStore.Current.career.Record(1, 1);
            SaveStore.Current.reducedEffects = true;
            SaveStore.Current.fullscreen = false;
            Assert.IsTrue(SaveStore.Save()); SaveStore.Reload();
            Assert.AreEqual(2, SaveStore.Current.career.NextEvent);
            Assert.AreEqual(4, SaveStore.Current.career.TotalMedals);
            Assert.IsTrue(SaveStore.Current.reducedEffects);
            Assert.IsFalse(SaveStore.Current.fullscreen);
        }

        [Test]
        public void UnsupportedSchemaFallsBackToFreshProfile()
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(SaveStore.FilePath, "{\"schemaVersion\":99,\"xp\":500}");
            SaveStore.Reload();
            Assert.AreEqual(0, SaveStore.Current.xp);
            Assert.IsNotEmpty(SaveStore.LastError);
        }

        [Test]
        public void BrokenPrimaryRecoversBackup()
        {
            SaveStore.Current.xp = 500;
            Assert.IsTrue(SaveStore.Save());
            SaveStore.Current.xp = 1000;
            Assert.IsTrue(SaveStore.Save());
            File.WriteAllText(SaveStore.FilePath, "broken JSON");
            SaveStore.Reload();
            Assert.AreEqual(500, SaveStore.Current.xp);
            Assert.IsNotEmpty(SaveStore.LastError);
        }
    }
}
