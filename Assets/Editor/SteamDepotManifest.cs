using System;
using System.Globalization;
using System.IO;

namespace VelocityRivals.Editor
{
    /// <summary>Prepare local SteamPipe descriptors. Does not log in, upload or publish.</summary>
    public static class SteamDepotManifest
    {
        public static string Write(string executable, string appId, string depotId, string output, string version)
        {
            if (!uint.TryParse(appId, NumberStyles.None, CultureInfo.InvariantCulture, out uint app) || app == 0 ||
                !uint.TryParse(depotId, NumberStyles.None, CultureInfo.InvariantCulture, out uint depot) || depot == 0)
                throw new ArgumentException("Enter the numeric App ID and Windows depot ID from your own Steamworks app.");
            string exe = Path.GetFullPath(executable), content = Path.GetDirectoryName(exe), folder = Path.GetFullPath(output);
            if (!File.Exists(exe) || new FileInfo(exe).Length == 0 || !File.Exists(Path.Combine(content, "UnityPlayer.dll")) ||
                !Directory.Exists(Path.Combine(content, Path.GetFileNameWithoutExtension(exe) + "_Data")))
                throw new FileNotFoundException("Build the full Windows game before preparing its Steam depot.");
            if (folder.Equals(content, StringComparison.OrdinalIgnoreCase) ||
                folder.StartsWith(content + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Steam configuration and build logs must be outside the game folder.");
            Directory.CreateDirectory(folder);
            Directory.CreateDirectory(Path.Combine(folder, "logs"));
            string appFile = Path.Combine(folder, "app_build.vdf"), depotFile = Path.Combine(folder, "depot_build.vdf");
            if (File.Exists(appFile) || File.Exists(depotFile)) throw new IOException("Choose a fresh output folder to preserve prior depot settings.");
            File.WriteAllText(depotFile, "\"DepotBuildConfig\"\n{\n  \"DepotID\" \"" + depot + "\"\n" +
                "  \"FileMapping\"\n  {\n    \"LocalPath\" \"*\"\n    \"DepotPath\" \".\"\n    \"recursive\" \"1\"\n  }\n" +
                "  \"FileExclusion\" \"*_BackUpThisFolder_ButDontShipItWithYourGame/*\"\n}\n");
            File.WriteAllText(appFile, "\"AppBuild\"\n{\n  \"AppID\" \"" + app + "\"\n  \"Desc\" " + Quote("Velocity Rivals " + version) +
                "\n  \"ContentRoot\" " + Quote(content.Replace('\\', '/')) + "\n  \"BuildOutput\" " + Quote(Path.Combine(folder,"logs").Replace('\\','/')) +
                "\n  \"Depots\"\n  {\n    \"" + depot + "\" \"depot_build.vdf\"\n  }\n}\n");
            return appFile;
        }
        static string Quote(string value)
        {
            if (value.IndexOfAny(new[] {'\r','\n','\0'}) >= 0) throw new ArgumentException("VDF values must be single-line strings.");
            return "\"" + value.Replace("\\","\\\\").Replace("\"","\\\"") + "\"";
        }
    }
}
