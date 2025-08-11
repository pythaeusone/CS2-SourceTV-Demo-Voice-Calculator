using System;
using System.IO;

namespace CS2SourceTVDemoVoiceCalc.UtilClass
{
    using System;
    using System.IO;

    /// <summary>
    /// A static helper class for managing the main application folder inside the
    /// user's local application data directory. This class only handles creating
    /// the root folder, creating subfolders under it, and returning their paths.
    /// </summary>
    public static class LocalAppDataFolder
    {
        /// <summary>
        /// The root folder name inside the local application data directory.
        /// </summary>
        private static readonly string RootFolderName = "CS2SourceTVDemoVoiceCalculator-Data";

        /// <summary>
        /// The full directory path to the application's root folder in the local application data.
        /// </summary>
        public static readonly string RootFolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            RootFolderName
        );

        /// <summary>
        /// Ensures that the root application directory exists. If it does not, it will be created.
        /// </summary>
        public static void EnsureRootDirectoryExists()
        {
            if (!Directory.Exists(RootFolderPath))
            {
                Directory.CreateDirectory(RootFolderPath);
            }
        }

        /// <summary>
        /// Ensures that a specific subdirectory under the root application folder exists.
        /// If it does not, it will be created.
        /// </summary>
        /// <param name="subFolderName">The name of the subdirectory to ensure exists.</param>
        /// <returns>The full path to the subdirectory.</returns>
        public static string EnsureSubDirectoryExists(string subFolderName)
        {
            string subDirPath = Path.Combine(RootFolderPath, subFolderName);
            if (!Directory.Exists(subDirPath))
            {
                Directory.CreateDirectory(subDirPath);
            }
            return subDirPath;
        }

        /// <summary>
        /// Gets the full path to a subdirectory under the root application folder without creating it.
        /// </summary>
        /// <param name="subFolderName">The name of the subdirectory.</param>
        /// <returns>The full path to the subdirectory.</returns>
        public static string GetSubDirectoryPath(string subFolderName)
        {
            return Path.Combine(RootFolderPath, subFolderName);
        }

        /// <summary>
        /// Gets the full path to a file in the root application folder.
        /// </summary>
        /// <param name="fileName">The name of the file (including extension).</param>
        /// <returns>The full path to the file inside the root folder.</returns>
        public static string GetFilePath(string fileName)
        {
            return Path.Combine(RootFolderPath, fileName);
        }
    }
}
