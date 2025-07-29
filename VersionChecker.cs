using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FaceitDemoVoiceCalc
{
    public static class VersionChecker
    {
        private const string GITHUB_API_RELEASES_URL = "https://api.github.com/repos/pythaeusone/Faceit-Demo-Voice-Calculator/releases";
        private const string RELEASE_PAGE_BASE_URL = "https://github.com/pythaeusone/Faceit-Demo-Voice-Calculator/releases/tag/";

        /// <summary>
        /// Asynchronously checks GitHub for newer releases based on version tag names.
        /// If a newer version is found, the user will be asked whether to open the release page.
        /// </summary>
        /// <param name="versionNr">The current version of the application (e.g., "v.0.9.4b").</param>
        /// <returns>True if a newer version is available (regardless of user choice), otherwise false.</returns>
        public static async Task<bool> IsNewerVersionAvailable(string versionNr)
        {
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "VersionCheckerApp");

            try
            {
                var response = await client.GetStringAsync(GITHUB_API_RELEASES_URL);
                var releases = JsonDocument.Parse(response).RootElement;

                string latestTag = null;

                foreach (var release in releases.EnumerateArray())
                {
                    if (release.TryGetProperty("tag_name", out var tagNameElement))
                    {
                        string tag = tagNameElement.GetString();
                        if (latestTag == null || CompareVersions(tag, latestTag) > 0)
                        {
                            latestTag = tag;
                        }
                    }
                }

                if (latestTag != null && CompareVersions(latestTag, versionNr) > 0)
                {
                    var result = MessageBox.Show(
                        $"New version available: {latestTag}\n\nDo you want to open the release page?",
                        "Update Available",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information);

                    if (result == DialogResult.Yes)
                    {
                        string url = RELEASE_PAGE_BASE_URL + latestTag;
                        try
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = url,
                                UseShellExecute = true
                            });
                        }
                        catch (Exception openEx)
                        {
                            MessageBox.Show($"Could not open the release page: {openEx.Message}");
                        }
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error checking version: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Compares two version strings in the format v.X.X.X[a|b|...] (e.g., v.0.9.4b).
        /// </summary>
        private static int CompareVersions(string v1, string v2)
        {
            var regex = new Regex(@"v\.(\d+)\.(\d+)\.(\d+)([a-z]?)", RegexOptions.IgnoreCase);

            var m1 = regex.Match(v1);
            var m2 = regex.Match(v2);

            if (!m1.Success || !m2.Success)
                return 0;

            for (int i = 1; i <= 3; i++)
            {
                int num1 = int.Parse(m1.Groups[i].Value);
                int num2 = int.Parse(m2.Groups[i].Value);
                if (num1 != num2)
                    return num1.CompareTo(num2);
            }

            string suffix1 = m1.Groups[4].Value;
            string suffix2 = m2.Groups[4].Value;

            return string.Compare(suffix1, suffix2, StringComparison.OrdinalIgnoreCase);
        }
    }
}