using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CS2SourceTVDemoVoiceCalc.UtilClass
{
    internal static class VersionChecker
    {
        // GitHub API endpoint that lists all releases for your repository
        private const string GITHUB_API_RELEASES_URL = "https://api.github.com/repos/pythaeusone/Faceit-Demo-Voice-Calculator/releases";

        // Base URL for a specific release tag on GitHub
        private const string RELEASE_PAGE_BASE_URL = "https://github.com/pythaeusone/Faceit-Demo-Voice-Calculator/releases/tag/";

        /// <summary>
        /// Asynchronously checks GitHub for newer releases based on version tag names.
        /// If a newer version is found, the user will be asked whether to open the release page.
        /// </summary>
        /// <param name="versionNr">The current version of the application (e.g., "v.0.9.4b").</param>
        /// <returns>True if a newer version is available, otherwise false.</returns>
        public static async Task<bool> IsNewerVersionAvailable(string versionNr, bool fromAutoCheck = false)
        {
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "VersionCheckerApp"); // Required by GitHub API

            try
            {
                // Request all releases from GitHub
                var response = await client.GetStringAsync(GITHUB_API_RELEASES_URL);
                var releases = JsonDocument.Parse(response).RootElement;

                string latestTag = null;

                // Find the highest version tag from the list of releases
                foreach (var release in releases.EnumerateArray())
                {
                    if (release.TryGetProperty("tag_name", out var tagNameElement))
                    {
                        string tag = tagNameElement.GetString();

                        // Update latestTag if this tag is newer
                        if (latestTag == null || CompareVersions(tag, latestTag) > 0)
                        {
                            latestTag = tag;
                        }
                    }
                }

                var versionTag = "v." + versionNr; // v. counts as version Nr. xD

                // If a newer version than the current one was found
                if (latestTag != null && CompareVersions(latestTag, versionTag) > 0)
                {
                    var result = MessageBox.Show(
                        $"New version available: {latestTag}\n\nWould you like to start the updater?",
                        "Update Available",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information);

                    if (result == DialogResult.Yes)
                    {
                        string appName = "Updater.exe";
                        string runPath = Path.Combine("Updater_Old", appName);

                        // Rename Updater Directory to Updater_Old
                        if (!RenameDirectory("Updater", "Updater_Old"))
                        {
                            MessageBox.Show("Error loading the updater");
                            return false;
                        }

                        MessageBox.Show("The CS2 SourceTV Demo Voice Calculator is now closing for the update process.");

                        try
                        {
                            ProcessStartInfo startInfo = new ProcessStartInfo
                            {
                                FileName = runPath,
                                Arguments = latestTag,
                                UseShellExecute = true
                            };

                            Process.Start(startInfo);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"[ERROR] Failed to start Updater: {ex.Message}");
                        }

                        Environment.Exit(0);
                    }

                    return true;
                }
                else
                {
                    if (!fromAutoCheck)
                    {
                        // Inform the user that their version is up to date
                        MessageBox.Show(
                            "You are using the latest version.",
                            "No Updates",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }

                }
            }
            catch (Exception ex)
            {
                // Handle errors such as network issues or invalid response
                MessageBox.Show($"Error checking version: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Renames a directory if source exists and destination doesn't
        /// </summary>
        /// <param name="source">Source directory path</param>
        /// <param name="newName">New directory name (not full path)</param>
        /// <returns>True if successful, false otherwise</returns>
        private static bool RenameDirectory(string source, string newName)
        {
            try
            {
                // Validate source exists
                if (!Directory.Exists(source))
                {
                    MessageBox.Show($"Source directory not found: {source}",
                                    "Rename Error",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error);
                    return false;
                }

                // Create destination path
                string parent = Directory.GetParent(source)?.FullName;
                string destination = Path.Combine(parent, newName);

                // Validate destination doesn't exist
                if (Directory.Exists(destination))
                {
                    MessageBox.Show($"Target directory already exists: {destination}",
                                    "Rename Error",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error);
                    return false;
                }

                // Execute rename
                Directory.Move(source, destination);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error renaming directory:\n{ex.Message}",
                                "Rename Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Compares two version strings in the format v.X.X.X[a|b|...] (e.g., v.0.9.4b).
        /// </summary>
        /// <param name="v1">First version string to compare.</param>
        /// <param name="v2">Second version string to compare.</param>
        /// <returns>
        /// -1 if v1 is less than v2, 0 if equal, 1 if v1 is greater than v2.
        /// </returns>
        private static int CompareVersions(string v1, string v2)
        {
            // Regex pattern to extract version numbers and optional suffix letter
            var regex = new Regex(@"v\.(\d+)\.(\d+)\.(\d+)([a-z]?)", RegexOptions.IgnoreCase);

            var m1 = regex.Match(v1);
            var m2 = regex.Match(v2);

            if (!m1.Success || !m2.Success)
                return 0; // Could not parse version strings

            // Compare major, minor, and patch versions
            for (int i = 1; i <= 3; i++)
            {
                int num1 = int.Parse(m1.Groups[i].Value);
                int num2 = int.Parse(m2.Groups[i].Value);
                if (num1 != num2)
                    return num1.CompareTo(num2);
            }

            // Compare optional suffixes ('a', 'b', etc.)
            string suffix1 = m1.Groups[4].Value;
            string suffix2 = m2.Groups[4].Value;

            return string.Compare(suffix1, suffix2, StringComparison.OrdinalIgnoreCase);
        }
    }
}