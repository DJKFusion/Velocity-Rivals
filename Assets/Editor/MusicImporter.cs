using System.IO;
using UnityEditor;
using UnityEngine;

namespace VelocityRivals.Editor
{
    public sealed class MusicImporter : AssetPostprocessor
    {
        public const string MusicRoot = "Assets/Resources/Music";

        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(MusicRoot + "/", System.StringComparison.Ordinal)) return;
            var importer = (AudioImporter)assetImporter;
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.Streaming;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = .8f;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = false;
        }

        [MenuItem("Velocity Rivals/Music/Import Menu Track")]
        public static void ImportMenuTrack() => Import("Menu");

        [MenuItem("Velocity Rivals/Music/Import Race Track")]
        public static void ImportRaceTrack() => Import("Race");

        [MenuItem("Velocity Rivals/Music/Show Music Folder")]
        public static void ShowFolder()
        {
            CreateFolders();
            EditorUtility.RevealInFinder(Path.GetFullPath(MusicRoot));
        }

        private static void CreateFolders()
        {
            Directory.CreateDirectory(MusicRoot + "/Menu");
            Directory.CreateDirectory(MusicRoot + "/Race");
            AssetDatabase.Refresh();
        }

        private static void Import(string playlist)
        {
            string source = EditorUtility.OpenFilePanelWithFilters("Choose music for " + playlist, "",
                new[] { "Audio files", "wav,mp3,ogg,aif,aiff" });
            if (string.IsNullOrEmpty(source)) return;
            CreateFolders();
            string destination = AssetDatabase.GenerateUniqueAssetPath(MusicRoot + "/" + playlist + "/" + Path.GetFileName(source));
            File.Copy(source, destination, false);
            AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceSynchronousImport);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<AudioClip>(destination);
            Debug.Log("Music imported. Add this path (without extension) to MusicLibrary." + playlist + "Tracks: " + Path.ChangeExtension(destination.Substring("Assets/Resources/".Length), null));
        }
    }
}
