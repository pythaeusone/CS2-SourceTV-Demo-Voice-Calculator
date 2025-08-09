using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Updater
{
    class Program
    {
        // Full path to the downloaded zip file (set after download)
        private static string _zipFilePath = null;
        // Directory where this updater executable is located (e.g. ...\\Updater_Old\\)
        private static string _currentDir = null;
        // Parent directory where the main application and Updater folder live (e.g. ...\\CS2VoiceTool\\)
        private static string _parentDir = null;

        /// <summary>
        /// Main entry point. Expects a version argument starting with "v.".
        /// Displays version info and starts the download and update process.
        /// </summary>
        static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("[ERROR] No parameters received!");
                Console.ReadLine();
                return;
            }

            string version = args[0];

            Console.WriteLine($"[INFO] Updater Version: {version} is loading...");

            if (version.StartsWith("v."))
            {
                // Set current directory and parent directory early so downloader and unpacker
                // know where to save files and where to copy the update to.
                _currentDir = Path.GetFullPath(AppContext.BaseDirectory);
                _parentDir = Directory.GetParent(Directory.GetParent(_currentDir)?.FullName ?? string.Empty)?.FullName;


                if (string.IsNullOrEmpty(_parentDir))
                {
                    Console.WriteLine("[ERROR] Could not determine parent directory.");
                    return;
                }

                await RunDownloadAndUpdate(version);
            }
            else
            {
                Console.WriteLine("[ERROR] Invalid parameter!");
            }
        }

        /// <summary>
        /// Coordinates the download, verification, unpacking, and running of the application.
        /// </summary>
        private static async Task RunDownloadAndUpdate(string version)
        {
            string apiPath = $"https://api.github.com/repos/pythaeusone/CS2-SourceTV-Demo-Voice-Calculator/releases/tags/{version}";

            bool downloadSuccess = await Downloader(apiPath);

            if (downloadSuccess)
            {
                Console.WriteLine("[INFO] Download and verification successful.");

                bool unpackSuccess = await UnpackZip();

                if (unpackSuccess)
                {
                    Console.WriteLine("[INFO] Unpacking successful.");

                    RunApplication();
                }
                else
                {
                    Console.WriteLine("[ERROR] Unpacking failed.");
                }
            }
            else
            {
                Console.WriteLine("[ERROR] Download or verification failed.");
            }
        }

        /// <summary>
        /// Starts the updated application with 'UpdateDone' parameter and exits this updater.
        /// </summary>
        private static void RunApplication()
        {
            string appName = "CS2SourceTVDemoVoiceCalc.exe";
            string runPath = Path.Combine(_parentDir, appName);
            string appStartParam = "UpdateDone";

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = runPath,
                    Arguments = appStartParam,
                    UseShellExecute = true
                };

                Process.Start(startInfo);
                Console.WriteLine("[INFO] Application started successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to start application: {ex.Message}");
            }

            Environment.Exit(0);
        }

        /// <summary>
        /// Unpacks the downloaded ZIP file (into a temporary folder), collapses any single-directory
        /// nesting, and copies the *contents* up one directory level (to _parentDir). This avoids
        /// creating nested package folders under Updater_Old and ensures the files end up in
        /// E:\Coding\CS2VoiceTool\ (the parent of Updater_Old).
        /// </summary>
        private static async Task<bool> UnpackZip()
        {
            try
            {
                if (string.IsNullOrEmpty(_currentDir) || string.IsNullOrEmpty(_parentDir))
                {
                    Console.WriteLine("[ERROR] Current or parent directory not set.");
                    return false;
                }

                if (string.IsNullOrEmpty(_zipFilePath) || !File.Exists(_zipFilePath))
                {
                    Console.WriteLine($"[ERROR] ZIP file '{_zipFilePath}' not found.");
                    return false;
                }

                string tempExtractPath = Path.Combine(_currentDir, "_temp_extract");

                // Clean up any existing temp folder
                if (Directory.Exists(tempExtractPath))
                    Directory.Delete(tempExtractPath, true);

                // Extract to the temp folder
                await Task.Run(() => ZipFile.ExtractToDirectory(_zipFilePath, tempExtractPath));

                // Determine the real source folder inside tempExtractPath.
                // Collapse nested single-folder hierarchies so we get to the actual package root.
                string sourceDir = tempExtractPath;
                while (true)
                {
                    // Get top-level directories and files
                    var topDirs = Directory.GetDirectories(sourceDir, "*", SearchOption.TopDirectoryOnly);
                    var topFiles = Directory.GetFiles(sourceDir, "*", SearchOption.TopDirectoryOnly);

                    // If there's exactly one directory and no files at this level, dive into it (collapse)
                    if (topFiles.Length == 0 && topDirs.Length == 1)
                    {
                        sourceDir = topDirs[0];
                        continue;
                    }

                    break;
                }

                Console.WriteLine($"[DEBUG] Using source folder: {sourceDir}");

                // Copy contents of sourceDir into parentDir (one level above currentDir)
                foreach (string dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
                {
                    string relativePath = Path.GetRelativePath(sourceDir, dirPath);
                    string targetDirPath = Path.Combine(_parentDir, relativePath);
                    Directory.CreateDirectory(targetDirPath);
                }

                foreach (string filePath in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
                {
                    string relativePath = Path.GetRelativePath(sourceDir, filePath);
                    string targetFilePath = Path.Combine(_parentDir, relativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(targetFilePath)!);

                    // Overwrite existing files in parent directory
                    File.Copy(filePath, targetFilePath, overwrite: true);
                    Console.WriteLine($"[INFO] Copied: {relativePath}");
                }

                // Cleanup
                try
                {
                    Directory.Delete(tempExtractPath, true);
                }
                catch { /* ignore cleanup errors */ }

                try
                {
                    File.Delete(_zipFilePath);
                }
                catch { /* ignore cleanup errors */ }

                Console.WriteLine($"[INFO] ZIP file unpacked and contents moved to '{_parentDir}'.");
                return true;
            }
            catch (Exception ex) when (
                ex is InvalidDataException ||
                ex is IOException ||
                ex is UnauthorizedAccessException)
            {
                Console.WriteLine($"[ERROR] Unpacking error: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Unexpected error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Downloads the release asset from GitHub, verifies SHA256 checksum, and saves the file
        /// to the updater's current directory. Sets _zipFilePath to the saved file path.
        /// </summary>
        private static async Task<bool> Downloader(string apiUrl)
        {
            // keep the same prefix detection as before
            string assetPrefix = "CS2-SourceTV-Demo-Voice-Calculator_";
            string downloadUrl = null;
            string assetName = null;

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "CSharpApp");
                var response = await client.GetAsync(apiUrl);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[ERROR] API error: {response.StatusCode}");
                    return false;
                }

                var json = await response.Content.ReadAsStringAsync();
                var jObject = JObject.Parse(json);
                var assets = jObject["assets"];

                // Find asset
                foreach (var asset in assets)
                {
                    string name = asset["name"].ToString();
                    if (name.StartsWith(assetPrefix))
                    {
                        assetName = name;
                        downloadUrl = asset["browser_download_url"].ToString();
                        break;
                    }
                }

                if (downloadUrl == null)
                {
                    Console.WriteLine("[ERROR] Asset not found!");
                    return false;
                }

                // Download file bytes
                var fileBytes = await client.GetByteArrayAsync(downloadUrl);

                // Calculate SHA256
                string calculatedSha256 = CalculateSha256(fileBytes);
                Console.WriteLine($"[INFO] Calculated SHA256: {calculatedSha256}");

                // Get expected SHA256 from GitHub
                string expectedSha256 = await GetExpectedSha256FromGitHub(client, jObject, assetName);

                if (!string.IsNullOrEmpty(expectedSha256))
                {
                    Console.WriteLine($"[INFO] Expected SHA256:   {expectedSha256}");

                    if (!VerifySha256(calculatedSha256, expectedSha256))
                    {
                        Console.WriteLine("[ERROR] SHA256 checksum does not match!");
                        return false;
                    }

                    Console.WriteLine("[INFO] SHA256 checksum verified.");
                }
                else
                {
                    Console.WriteLine("[WARN] No SHA256 checksum found to verify.");
                }

                // Save file into current directory
                if (string.IsNullOrEmpty(_currentDir))
                {
                    _currentDir = Path.GetFullPath(AppContext.BaseDirectory);
                }

                string savePath = Path.Combine(_currentDir, assetName);
                File.WriteAllBytes(savePath, fileBytes);

                _zipFilePath = savePath;
                Console.WriteLine($"[INFO] Download completed: {_zipFilePath}");

                return true;
            }
        }

        /// <summary>
        /// Calculates SHA256 hash of given byte array.
        /// </summary>
        private static string CalculateSha256(byte[] fileBytes)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(fileBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToUpper();
            }
        }

        /// <summary>
        /// Retrieves expected SHA256 checksum from GitHub release data.
        /// </summary>
        private static async Task<string> GetExpectedSha256FromGitHub(HttpClient client, JObject releaseData, string assetName)
        {
            var assets = releaseData["assets"];

            // Option 1: digest field in asset
            foreach (var asset in assets)
            {
                string name = asset["name"].ToString();

                if (name == assetName)
                {
                    var digest = asset["digest"]?.ToString();
                    if (!string.IsNullOrEmpty(digest))
                    {
                        if (digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
                        {
                            return digest.Substring(7).ToUpper();
                        }
                    }
                }
            }

            // Option 2: separate .sha256 file
            foreach (var asset in assets)
            {
                string name = asset["name"].ToString();

                if (name == assetName + ".sha256" ||
                    name == assetName + ".sha256.txt" ||
                    (name.StartsWith(assetName) && name.EndsWith(".sha256")))
                {
                    var sha256Url = asset["browser_download_url"].ToString();
                    var sha256Content = await client.GetStringAsync(sha256Url);
                    return ExtractSha256FromContent(sha256Content);
                }
            }

            // Option 3: SHA256 in release body text
            var releaseBody = releaseData["body"]?.ToString();
            if (!string.IsNullOrEmpty(releaseBody))
            {
                return ExtractSha256FromReleaseBody(releaseBody);
            }

            return null;
        }

        /// <summary>
        /// Extracts SHA256 hash from content string.
        /// </summary>
        private static string ExtractSha256FromContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return null;

            var parts = content.Trim().Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length > 0 && Regex.IsMatch(parts[0], @"^[a-fA-F0-9]{64}$"))
            {
                return parts[0].ToUpper();
            }

            return null;
        }

        /// <summary>
        /// Extracts SHA256 hash from release body text using regex patterns.
        /// </summary>
        private static string ExtractSha256FromReleaseBody(string releaseBody)
        {
            var patterns = new[]
            {
                @"SHA256:\s*([a-fA-F0-9]{64})",
                @"SHA-256:\s*([a-fA-F0-9]{64})",
                @"sha256:\s*([a-fA-F0-9]{64})",
                @"sha256:([a-fA-F0-9]{64})",
                @"checksum:\s*([a-fA-F0-9]{64})",
                @"`([a-fA-F0-9]{64})`"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(releaseBody, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return match.Groups[1].Value.ToUpper();
                }
            }

            return null;
        }

        /// <summary>
        /// Compares two SHA256 hashes for equality.
        /// </summary>
        private static bool VerifySha256(string calculated, string expected)
        {
            return string.Equals(calculated, expected, StringComparison.OrdinalIgnoreCase);
        }
    }
}
