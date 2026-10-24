using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VelocityRivals
{
    [Serializable] public sealed class RaceRecord
    {
        public string mode;
        public float bestSeconds;
        public int bestScore;
        public int finishes;
    }

    [Serializable] public sealed class PlayerProfile
    {
        public int schemaVersion = 1;
        public string playerName = "Rival";
        public string relayUrl = "";
        public int xp;
        public int wins;
        public int events;
        public int selectedVehicle;
        public int paint;
        public int wheels;
        public int trail;
        public int decal;
        public int spoiler;
        public int finish;
        public float masterVolume = 0.8f;
        public float musicVolume = 0.3f;
        public float effectsVolume = 0.75f;
        public int quality = 1;
        public int difficulty = 1;
        public float steeringSensitivity = 1;
        public List<RaceRecord> records = new List<RaceRecord>();
        public List<string> achievements = new List<string>();
        public int Level => 1 + xp / 500;
        public int UnlockedVehicles => Mathf.Clamp(1 + xp / 500, 1, 6);
    }

    /// <summary>Local JSON persistence. Keep platform storage adapters behind this boundary.</summary>
    public static class SaveStore
    {
        private static PlayerProfile current;
        public static string LastError { get; private set; }
        /// <summary>Inject a platform-specific directory, or an isolated test directory, before loading.</summary>
        public static string StorageDirectoryOverride { get; set; }
        private static string StorageDirectory => string.IsNullOrEmpty(StorageDirectoryOverride) ? Application.persistentDataPath : StorageDirectoryOverride;
        public static string FilePath => Path.Combine(StorageDirectory, "velocity-profile.json");
        public static PlayerProfile Current => current ?? (current = Load());
        public static void Reload() { current = null; }

        public static PlayerProfile Load()
        {
            LastError = null;
            foreach (string path in new[] { FilePath, FilePath + ".bak" })
            {
                if (!File.Exists(path)) continue;
                try
                {
                    PlayerProfile profile = JsonUtility.FromJson<PlayerProfile>(File.ReadAllText(path));
                    if (profile == null || profile.schemaVersion != 1 || profile.xp < 0)
                        throw new InvalidDataException("Unsupported or invalid player profile.");
                    profile.records ??= new List<RaceRecord>();
                    profile.achievements ??= new List<string>();
                    profile.selectedVehicle = Mathf.Clamp(profile.selectedVehicle, 0, profile.UnlockedVehicles - 1);
                    profile.paint = Mathf.Clamp(profile.paint, 0, 7);
                    profile.wheels = Mathf.Clamp(profile.wheels, 0, 2);
                    profile.trail = Mathf.Clamp(profile.trail, 0, 2);
                    profile.decal = Mathf.Clamp(profile.decal, 0, 2);
                    profile.spoiler = Mathf.Clamp(profile.spoiler, 0, 2);
                    profile.finish = Mathf.Clamp(profile.finish, 0, 2);
                    profile.quality = Mathf.Clamp(profile.quality, 0, 2);
                    profile.difficulty = Mathf.Clamp(profile.difficulty, 0, 2);
                    if (profile.steeringSensitivity < .5f || profile.steeringSensitivity > 1.5f || float.IsNaN(profile.steeringSensitivity)) profile.steeringSensitivity = 1;
                    profile.masterVolume = Mathf.Clamp01(profile.masterVolume);
                    profile.musicVolume = Mathf.Clamp01(profile.musicVolume);
                    profile.effectsVolume = Mathf.Clamp01(profile.effectsVolume);
                    if (path.EndsWith(".bak", StringComparison.Ordinal)) LastError = "Recovered the backup profile.";
                    return profile;
                }
                catch (Exception e) when (e is IOException || e is InvalidDataException || e is ArgumentException || e is UnauthorizedAccessException)
                {
                    LastError = "Could not read a profile: " + e.Message;
                    Debug.LogWarning(LastError);
                }
            }
            return new PlayerProfile();
        }

        public static bool Save()
        {
            try
            {
                Directory.CreateDirectory(StorageDirectory);
                string temp = FilePath + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(Current, true));
                // Replace is atomic on supported desktop filesystems; retain last good version.
                if (File.Exists(FilePath)) File.Replace(temp, FilePath, FilePath + ".bak");
                else File.Move(temp, FilePath);
                LastError = null;
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is NotSupportedException)
            {
                LastError = "Progress could not be saved: " + e.Message;
                Debug.LogWarning(LastError);
                return false;
            }
        }

        public static void RecordResult(string mode, bool won, float seconds, int score)
        {
            PlayerProfile profile = Current;
            profile.events++;
            if (won) profile.wins++;
            profile.xp += 150 + (won ? 150 : 0) + Mathf.Clamp(score / 20, 0, 200);
            RaceRecord record = profile.records.Find(r => r.mode == mode);
            if (record == null) { record = new RaceRecord { mode = mode }; profile.records.Add(record); }
            record.finishes++;
            record.bestScore = Mathf.Max(record.bestScore, score);
            if (seconds > 0 && !float.IsInfinity(seconds) && !float.IsNaN(seconds) && (record.bestSeconds <= 0 || seconds < record.bestSeconds))
                record.bestSeconds = seconds;
            Award("First finish", profile.events >= 1);
            Award("First victory", profile.wins >= 1);
            Award("Ten victories", profile.wins >= 10);
            Award("Full garage", profile.UnlockedVehicles == 6);
            Award("Stunt specialist", mode == "StuntRush" && score >= 2000);
            Save();
        }

        private static void Award(string name, bool condition)
        {
            if (condition && !Current.achievements.Contains(name)) Current.achievements.Add(name);
        }
    }
}
