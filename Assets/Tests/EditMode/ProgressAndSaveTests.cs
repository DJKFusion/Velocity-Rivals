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
        public void OldProfileGetsWalletWithoutLosingCarsAndPurchasesPersist()
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(SaveStore.FilePath,"{\"schemaVersion\":2,\"xp\":1100,\"selectedVehicle\":2}");
            SaveStore.Reload();
            Assert.AreEqual(3,SaveStore.Current.UnlockedVehicles);
            Assert.AreEqual(2,SaveStore.Current.selectedVehicle);
            Assert.AreEqual(500,SaveStore.Current.garage.credits);
            SaveStore.Current.garage.Credit(1000);
            Assert.IsTrue(SaveStore.PurchaseCar());
            Assert.AreEqual(4,SaveStore.Current.UnlockedVehicles);
            Assert.IsTrue(SaveStore.PurchaseUpgrade(3,0));
            int wallet=SaveStore.Current.garage.credits;SaveStore.Reload();
            Assert.AreEqual(4,SaveStore.Current.UnlockedVehicles);
            Assert.AreEqual(3,SaveStore.Current.selectedVehicle);
            Assert.AreEqual(1,SaveStore.Current.garage.Level(3,0));
            Assert.AreEqual(wallet,SaveStore.Current.garage.credits);
        }

        [Test]
        public void FailedPurchaseSaveRestoresCreditsAndUpgrade()
        {
            int before=SaveStore.Current.garage.credits;
            Directory.CreateDirectory(directory);string blocked=Path.Combine(directory,"blocked");File.WriteAllText(blocked,"file, not a directory");
            SaveStore.StorageDirectoryOverride=blocked;
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Warning,new System.Text.RegularExpressions.Regex("Progress could not be saved:"));
            Assert.IsFalse(SaveStore.PurchaseUpgrade(0,0));
            Assert.AreEqual(before,SaveStore.Current.garage.credits);
            Assert.AreEqual(0,SaveStore.Current.garage.Level(0,0));
            SaveStore.StorageDirectoryOverride=directory;
        }

        [Test]
        public void EarlierChampionshipRulesKeepTheirRecordsWithoutReplacingNewTimes()
        {
            SaveStore.Current.records.Add(new RaceRecord { mode="Championship",environment=0,vehicle=0,layoutRevision=CircuitLayout.Revision,bestSeconds=100,finishes=1 });
            SaveStore.RecordResult("Championship",true,90,30,0,0);
            Assert.AreEqual(2,SaveStore.Current.records.Count);
            Assert.AreEqual(100,SaveStore.Current.records[0].bestSeconds);
            Assert.AreEqual(RaceCraft.RulesVersion,SaveStore.Current.records[1].raceRulesRevision);
            SaveStore.RecordResult("Elimination",true,135,3000,0,0);
            Assert.AreEqual(CircuitLayout.Revision,SaveStore.Current.records[2].layoutRevision);
            Assert.Contains("Last survivor",SaveStore.Current.achievements);
        }

        [Test]
        public void TunedRecordsStaySeparateButTimeAttackUsesStockIdentity()
        {
            SaveStore.RecordResult("Championship",true,100,30,0,0);
            SaveStore.Current.garage.upgrades[0]=2;
            SaveStore.RecordResult("Championship",true,90,30,0,0);
            Assert.AreEqual(2,SaveStore.Current.records.Count);
            SaveStore.RecordResult("TimeAttack",true,60,0,0,0);
            SaveStore.Current.garage.upgrades[0]=3;
            SaveStore.RecordResult("TimeAttack",true,55,0,0,0);
            Assert.AreEqual(3,SaveStore.Current.records.Count);
            Assert.AreEqual(0,SaveStore.Current.records[2].upgradeSignature);
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
        public void NewCircuitRecordsPreserveButDoNotCompeteWithOldLayoutTimes()
        {
            SaveStore.Current.records.Add(new RaceRecord { mode = "TimeAttack", environment = 0, vehicle = 0, bestSeconds = 10, layoutRevision = 0 });
            SaveStore.Current.xp = 1200;
            SaveStore.Current.career.Record(0, 3);
            SaveStore.RecordResult("TimeAttack", true, 150, 200, 0, 0);
            SaveStore.Reload();
            Assert.AreEqual(2, SaveStore.Current.records.Count);
            Assert.AreEqual(10, SaveStore.Current.records[0].bestSeconds);
            Assert.AreEqual(CircuitLayout.Revision, SaveStore.Current.records[1].layoutRevision);
            Assert.AreEqual(150, SaveStore.Current.records[1].bestSeconds);
            Assert.AreEqual(3, SaveStore.Current.career.medals[0]);
            Assert.GreaterOrEqual(SaveStore.Current.xp, 1200);
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
