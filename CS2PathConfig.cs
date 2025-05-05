using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows.Forms;

namespace FaceitDemoVoiceCalc
{
    /// <summary>
    /// Provides storage and retrieval of a single CS2 demo folder path
    /// in a JSON config file alongside the executable.
    /// </summary>
    internal static class CS2PathConfig
    {
        private const string ConfigFileName = "cs2config.json";

        // Full path to the config file next to the running .exe
        private static readonly string ConfigFilePath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);


        /// <summary>
        /// Represents the data structure stored in the JSON config file,
        /// containing a single CS2 demo folder path.
        /// </summary>
        private class CS2ConfigData
        {
            /// <summary>
            /// Gets or sets the stored CS2 demo folder path.
            /// </summary>
            [JsonPropertyName("path")]
            public string Path { get; set; }
        }


        /// <summary>
        /// Ensures that the JSON config file exists. If not, creates it
        /// with an empty "path" field.
        /// </summary>
        private static void EnsureConfigExists()
        {
            if (!File.Exists(ConfigFilePath))
            {
                // Create default config with empty path
                var defaultConfig = new CS2ConfigData { Path = string.Empty };
                File.WriteAllText(
                    ConfigFilePath,
                    JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions { WriteIndented = true })
                );
            }
        }


        /// <summary>
        /// Checks whether a non-empty path is already stored in the config.
        /// </summary>
        /// <returns>True if a path exists in config; otherwise, false.</returns>
        public static bool HasPath()
        {
            EnsureConfigExists();

            try
            {
                // Read and deserialize the config file
                var config = JsonSerializer.Deserialize<CS2ConfigData>(File.ReadAllText(ConfigFilePath));
                // Check for non-empty path
                return !string.IsNullOrWhiteSpace(config?.Path);
            }
            catch
            {
                // In case of any error, treat as no path set
                return false;
            }
        }


        /// <summary>
        /// Prompts the user with a folder-picker dialog to select the CS2 demo folder.
        /// Repeats the dialog if the selection is invalid. Saves the chosen path to config.
        /// </summary>
        /// <returns>
        /// The selected folder path, or null if the user cancels the dialog.
        /// </returns>
        public static string EnsurePathConfigured()
        {
            EnsureConfigExists();

            while (true)
            {
                // Create a folder picker dialog with an editable path textbox
                var dialog = new CommonOpenFileDialog
                {
                    IsFolderPicker = true,
                    Title = "Select the CS2 Demo folder",
                    InitialDirectory = AppDomain.CurrentDomain.BaseDirectory,
                    EnsurePathExists = true,
                    Multiselect = false
                };

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    string selectedPath = dialog.FileName;

                    // Verify the selected path is a directory
                    if (!Directory.Exists(selectedPath))
                    {
                        MessageBox.Show(
                            "The selected path is not a valid directory.",
                            "Invalid Path",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );
                        continue; // show dialog again
                    }

                    // Save the valid path to the config file
                    var config = new CS2ConfigData { Path = selectedPath };
                    File.WriteAllText(
                        ConfigFilePath,
                        JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true })
                    );

                    return selectedPath;
                }

                // User cancelled the dialog
                return null;
            }
        }


        /// <summary>
        /// Retrieves the stored CS2 demo folder path. If no path is stored,
        /// prompts the user to select one via EnsurePathConfigured().
        /// </summary>
        /// <returns>
        /// The stored or newly selected folder path, or null if not configured.
        /// </returns>
        public static string GetPath()
        {
            EnsureConfigExists();

            if (HasPath())
            {
                // Read and return the existing path from config
                var config = JsonSerializer.Deserialize<CS2ConfigData>(File.ReadAllText(ConfigFilePath));
                return config.Path;
            }

            // No path yet; prompt the user to configure one
            return EnsurePathConfigured();
        }
    }
}