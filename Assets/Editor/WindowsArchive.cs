using System;
using System.IO;
using System.IO.Compression;

namespace VelocityRivals.Editor
{
    /// <summary>Archive the whole successful player build, never only the executable.</summary>
    public static class WindowsArchive
    {
        public static void Create(string executable, string archive)
        {
            string fullExecutable = Path.GetFullPath(executable);
            string folder = Path.GetDirectoryName(fullExecutable);
            string target = Path.GetFullPath(archive);
            string prefix = folder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (target.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("The archive must be outside the player folder.", nameof(archive));
            if (!File.Exists(fullExecutable) || new FileInfo(fullExecutable).Length == 0)
                throw new FileNotFoundException("A successful Windows executable is required.", fullExecutable);
            string player = Path.Combine(folder, "UnityPlayer.dll");
            if (!File.Exists(player) || new FileInfo(player).Length == 0)
                throw new FileNotFoundException("UnityPlayer.dll is missing from the build.", player);
            string data = Path.Combine(folder, Path.GetFileNameWithoutExtension(executable) + "_Data");
            if (!Directory.Exists(data) || Directory.GetFiles(data, "*", SearchOption.AllDirectories).Length == 0)
                throw new DirectoryNotFoundException("The game Data folder is missing or empty.");
            if (File.Exists(target)) throw new IOException("Choose a new archive name to preserve the existing package.");
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            string temporary = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                ZipFile.CreateFromDirectory(folder, temporary, CompressionLevel.Optimal, false);
                using (var zip = ZipFile.OpenRead(temporary))
                {
                    if (zip.GetEntry(Path.GetFileName(fullExecutable)) == null || zip.GetEntry("UnityPlayer.dll") == null)
                        throw new InvalidDataException("Package verification failed.");
                }
                File.Move(temporary, target);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}
