using CS2SourceTVDemoVoiceCalc.HelperClass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CS2SourceTVDemoVoiceCalc.UtilClass
{
    /// <summary>
    /// Provides static helper methods to configure and attach context menus to DataGridView controls.
    /// Includes menu setups for player-related actions such as copying Steam IDs and opening profile links,
    /// as well as audio file operations like copying voice recordings to a designated folder.
    /// The methods handle all required event bindings and context-specific logic.
    /// </summary>
    public static class DGVContextMenu
    {
        /// <summary>
        /// Configures the context menu for a DataGridView with player actions.
        /// </summary>
        public static void ConfigureContextMenuMainGrid(
            DataGridView dgv,
            List<PlayerSnapshot> playerList,
            string steamProfileLink,
            string cswatchProfileLink,
            string leetifyProfileLink,
            string csStatsProfileLink)
        {
            // Context menu setup for player actions (copy SteamID, open profiles, etc.)
            var playerHeader = new ToolStripMenuItem("Player")
            {
                Enabled = false,
                Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold),
                Image = Properties.Resources.iconPlayer
            };

            var copySteamId = new ToolStripMenuItem("Copy SteamID64")
            {
                Image = Properties.Resources.iconSteam
            };

            var profileDefinitions = new (string Label, string UrlPrefix, Image Icon)[]
            {
                ("Open Steam Profile", steamProfileLink, Properties.Resources.iconSteam),
                ("Open cswatch.in Profile", cswatchProfileLink, Properties.Resources.iconCsWatch),
                ("Open leetify.com Profile", leetifyProfileLink, Properties.Resources.iconLeetify),
                ("Open csstats.gg Profile", csStatsProfileLink, Properties.Resources.iconCsStats)
            };

            var profileItems = profileDefinitions.Select(def => new ToolStripMenuItem(def.Label)
            {
                Image = def.Icon
            }).ToList();

            ContextMenuStrip cms = new ContextMenuStrip();
            cms.Items.Add(playerHeader);
            cms.Items.Add(new ToolStripSeparator());
            cms.Items.Add(copySteamId);
            cms.Items.Add(new ToolStripSeparator());
            cms.Items.AddRange(profileItems.ToArray());

            dgv.ContextMenuStrip = cms;

            copySteamId.Click += (s, e) =>
            {
                if (copySteamId.Tag is PlayerSnapshot p && p.PlayerSteamID.HasValue)
                {
                    Clipboard.SetText(p.PlayerSteamID.ToString() ?? "Error reading SteamID");
                    MessageBox.Show($"SteamID64 for {p.PlayerName} copied to clipboard.", "Info");
                }
            };

            foreach (var (menuItem, def) in profileItems.Zip(profileDefinitions))
            {
                menuItem.Click += (s, e) =>
                {
                    if (menuItem.Tag is PlayerSnapshot p && p.PlayerSteamID.HasValue)
                    {
                        string url = def.UrlPrefix + p.PlayerSteamID;
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = url,
                            UseShellExecute = true
                        });
                    }
                };
            }

            dgv.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    var hit = dgv.HitTest(e.X, e.Y);
                    if (hit.Type == DataGridViewHitTestType.Cell && hit.RowIndex >= 0)
                    {
                        dgv.ClearSelection();
                        dgv.Rows[hit.RowIndex].Selected = true;
                        dgv.CurrentCell = dgv.Rows[hit.RowIndex].Cells[hit.ColumnIndex];

                        string playerName = dgv.Rows[hit.RowIndex].Cells[1].Value?.ToString() ?? "";
                        var player = playerList.FirstOrDefault(p => p.PlayerName == playerName);

                        if (player != null)
                        {
                            playerHeader.Text = player.PlayerName;
                            copySteamId.Text = $"Copy SteamID64";
                            copySteamId.Tag = player;
                            profileItems.ForEach(i => i.Tag = player);
                        }
                        else
                        {
                            playerHeader.Text = "Unknown Player";
                            copySteamId.Text = "Player not found";
                            copySteamId.Tag = null;
                            profileItems.ForEach(i =>
                            {
                                i.Text = "-";
                                i.Tag = null;
                            });
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Configures a context menu for the provided DataGridView to allow copying voice files.
        /// When a user right-clicks on a row and selects the "Copy Voice-File" option, the selected audio file
        /// is copied to the "Saved-Voice-Files" directory with a formatted filename based on player name, round, and time.
        /// The destination folder is created automatically if it does not exist.
        /// </summary>
        /// <param name="dgv">The DataGridView to attach the context menu to.</param>
        public static void ConfigureContextMenuAudioFileCopy(
            DataGridView dgv,
            List<AudioEntry> audioEntries,
            string selectedPlayerVoicePlayer,
            string demoFileName)
        {
            // Create the context menu item for copying files
            var copyFileItem = new ToolStripMenuItem("Copy Voice-File")
            {
                Image = Properties.Resources.iconCopy // Optional: icon for the menu item
            };

            // Create and assign the context menu to the DataGridView
            ContextMenuStrip cms = new ContextMenuStrip();
            cms.Items.Add(copyFileItem);
            dgv.ContextMenuStrip = cms;

            // Action executed when the menu item is clicked
            copyFileItem.Click += (s, e) =>
            {
                if (copyFileItem.Tag is AudioEntry entry)
                {
                    try
                    {

                        // Get the processed file name without prefix before "_-_"
                        string getTrueName = GetTrueName(demoFileName);

                        // Create the destination folder path
                        string destinationFolder = Path.Combine("Saved-Voice-Files", getTrueName);

                        // Ensure the destination folder exists
                        Directory.CreateDirectory(destinationFolder);

                        // Build the new file name using player name, round, and time
                        string playerName = selectedPlayerVoicePlayer;
                        string round = "_-_Round " + entry.Round + "_-_";
                        string minute = "DemoTime " + entry.Time.ToString(@"hh\-mm\-ss");
                        string newFileName = playerName + round + minute + Path.GetExtension(entry.FilePath);

                        // Combine the destination folder and file name
                        string destFile = Path.Combine(destinationFolder, newFileName);

                        // Copy the file to the destination, overwrite if it exists
                        File.Copy(entry.FilePath, destFile, true);

                        MessageBox.Show($"File copied to: \\{destinationFolder}\\{newFileName}", "Success");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error during copying: {ex.Message}", "Error");
                    }
                }
            };

            // Detect right-clicks on the DataGridView rows
            dgv.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    var hit = dgv.HitTest(e.X, e.Y);
                    if (hit.Type == DataGridViewHitTestType.Cell && hit.RowIndex >= 0)
                    {
                        // Select the row that was right-clicked
                        dgv.ClearSelection();
                        dgv.Rows[hit.RowIndex].Selected = true;
                        dgv.CurrentCell = dgv.Rows[hit.RowIndex].Cells[hit.ColumnIndex];

                        // Attach the corresponding AudioEntry to the menu item for later use
                        if (hit.RowIndex < audioEntries.Count)
                        {
                            var entry = audioEntries[hit.RowIndex];
                            copyFileItem.Tag = entry;
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Extracts the file name without extension from the given path.
        /// If the file name contains "_-_", removes everything before and including it.
        /// Otherwise, returns the original file name without extension.
        /// </summary>
        /// <param name="filePath">The full file path to process.</param>
        /// <returns>The processed file name.</returns>
        static string GetTrueName(string filePath)
        {
            // Extract file name without extension
            string name = Path.GetFileNameWithoutExtension(filePath);

            // Find the position of the separator "_-_"
            int index = name.IndexOf("_-_");

            // If found, return the part after the separator; otherwise, return the original name
            return index >= 0 ? name[(index + 3)..] : name;
        }
    }
}
