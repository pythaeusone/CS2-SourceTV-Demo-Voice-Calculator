using Microsoft.VisualBasic;
using Microsoft.Win32;

namespace FaceitDemoVoiceCalc
{
    internal static class AddShellContextMenu
    {
        private static readonly string[] EXTENSIONS = new[] { ".dem" };
        private static readonly string EXE_PATH = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FaceitDemoVoiceCalc.exe"));
        private const string MENU_NAME = "Open with Faceit Demo Voice Calculator";

        public static void AddShellIntegration()
        {
            foreach (var ext in EXTENSIONS)
            {
                try
                {
                    string baseKeyPath = $"Software\\Classes\\SystemFileAssociations\\{ext}\\shell\\{MENU_NAME}";
                    string commandKeyPath = Path.Combine(baseKeyPath, "command");

                    using (var baseKey = Registry.CurrentUser.CreateSubKey(baseKeyPath))
                    {
                        baseKey?.SetValue(string.Empty, MENU_NAME);
                        baseKey?.SetValue("Icon", EXE_PATH);
                    }

                    using (var commandKey = Registry.CurrentUser.CreateSubKey(commandKeyPath))
                    {
                        commandKey?.SetValue(string.Empty, $"\"{EXE_PATH}\" \"%1\"");
                    }
                    MessageBox.Show($"Shell integration added for {ext}",
                            "Info for Shell-Context-Menu",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                            );
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error adding registry key for {ext}: {ex.Message}");
                }
            }
        }

        public static void RemoveShellIntegration()
        {
            foreach (var ext in EXTENSIONS)
            {
                try
                {
                    string baseKeyPath = $"Software\\Classes\\SystemFileAssociations\\{ext}\\shell\\{MENU_NAME}";
                    Registry.CurrentUser.DeleteSubKey(Path.Combine(baseKeyPath, "command"), false);
                    Registry.CurrentUser.DeleteSubKey(baseKeyPath, false);
                    MessageBox.Show($"Removed shell integration for {ext} file.",
                            "Info for Shell-Context-Menu",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                            );
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error removing registry key for {ext}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Verifies the existing shell context menu entry and replace it if it doesn't match the current executable path.
        /// </summary>
        public static void ValidateShellIntegration()
        {
            foreach (var ext in EXTENSIONS)
            {
                string baseKeyPath = $"Software\\Classes\\SystemFileAssociations\\{ext}\\shell\\{MENU_NAME}\\command";

                using (var commandKey = Registry.CurrentUser.OpenSubKey(baseKeyPath, writable: false))
                {
                    if (commandKey != null)
                    {
                        string? existingCommand = commandKey.GetValue(string.Empty) as string;
                        string expectedCommand = $"\"{EXE_PATH}\" \"%1\"";

                        if (!string.Equals(existingCommand, expectedCommand, StringComparison.OrdinalIgnoreCase))
                        {
                            // Mismatch detected: remove incorrect entry
                            MessageBox.Show($"An outdated shell entry for {ext} files has been found and will be replaced.",
                            "Info for Shell-Context-Menu",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                            );
                            AddShellIntegration();
                        }
                    }
                }
            }
        }
    }
}