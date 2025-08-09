using Microsoft.WindowsAPICodePack.Dialogs;

namespace CS2SourceTVDemoVoiceCalc.UtilClass
{
    /// <summary>
    /// Handles selection and storage of the CS2 demo folder path using the JsonClass utility.
    /// </summary>
    internal static class CS2PathPicker
    {
        private const string PathKey = "CS2DemoPath";

        /// <summary>
        /// Checks whether a non-empty path is already stored in the JSON config.
        /// </summary>
        public static bool HasPath()
        {
            if (!JsonClass.KeyExists(PathKey))
                return false;

            var storedPath = JsonClass.ReadJson<string>(PathKey);
            return !string.IsNullOrWhiteSpace(storedPath);
        }

        /// <summary>
        /// Prompts the user with a folder-picker dialog to select the CS2 demo folder.
        /// Saves the chosen path to config.
        /// </summary>
        public static string EnsurePathConfigured()
        {
            while (true)
            {
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

                    if (!Directory.Exists(selectedPath))
                    {
                        MessageBox.Show(
                            "The selected path is not a valid directory.",
                            "Invalid Path",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );
                        continue;
                    }

                    JsonClass.WriteJson(PathKey, selectedPath);
                    return selectedPath;
                }

                return null; // User cancelled
            }
        }

        /// <summary>
        /// Retrieves the stored CS2 demo folder path. If no path is stored,
        /// prompts the user to select one.
        /// </summary>
        public static string GetPath()
        {
            if (HasPath())
            {
                return JsonClass.ReadJson<string>(PathKey);
            }

            return EnsurePathConfigured();
        }
    }
}
