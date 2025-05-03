using DemoFile;
using DemoFile.Game.Cs;
using System.Data;

namespace FaceitDemoVoiceCalc
{
    public partial class MainForm : Form
    {
        // ======================================
        // Nested data class for player snapshot
        // ======================================
        /// <summary>
        /// Holds basic player information captured at round start.
        /// </summary>
        private class PlayerSnapshot
        {
            public int UserId { get; set; }             // User-ID + 1 = spec_player ID
            public string? PlayerName { get; set; }     // PlayerName = Name of the Player in this Game.
            public int TeamNumber { get; set; }         // TeamNumber = 2 = T-Side, 3 = CT-Side
            public string? TeamName { get; set; }       // TeamName = TeamClanName from Faceit team_xxxxx
        }


        // =====================
        // Parser and snapshots
        // =====================
        private CsDemoParser? demo = null; // An object is created to process the read DemoStream and read out the desired information.
        private PlayerSnapshot[]? snapshot = null; // The information collected from the demo is saved in an array.


        // ============================================================================================
        // Bitfields for voice indices, the calculated bitfield numbers are stored in these variables
        // ============================================================================================
        private int teamAP1, teamAP2, teamAP3, teamAP4, teamAP5;
        private int teamBP1, teamBP2, teamBP3, teamBP4, teamBP5;


        // ====================================================================================================
        // Checkbox groups for easy toggling. All checkboxes are grouped into a list to keep the code cleaner.
        // =====================================================================================================
        private readonly List<CheckBox> teamACheckboxes = new();
        private readonly List<CheckBox> teamBCheckboxes = new();


        // =====================================================================
        // The shared instance used to display non-blocking copy notifications.
        // =====================================================================
        private readonly ToolTip _copyToolTip = new ToolTip();


        // -------------------------------------
        // Class-level field to prevent recursion
        // -------------------------------------
        private bool _isSyncingSelectAll = false;


        // =================
        // Form constructor
        // =================
        /// <summary>
        /// Initializes form components and sets up checkbox handlers.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
            InitializeCheckboxGroup();
            InitializeEventHandlers();
        }


        /// <summary>
        /// Opens a form of the given type, either modal or non-modal.
        /// A more modern way to open a Windows form, this is about HowTo and About.
        /// </summary>
        private void OpenForm<T>(bool modal = true)
            where T : Form, new()
        {
            var frm = new T();
            if (modal)
                frm.ShowDialog();
            else
                frm.Show();
        }

        // =======================
        // Menubar event handlers
        // =======================
        private void howToUseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Show usage instructions
            OpenForm<HowTo>();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Show application information
            OpenForm<About>();
        }


        // ====================================
        // Drag & drop for demo file selection
        // ====================================
        private void tb_demoFilePath_DragEnter(object sender, DragEventArgs e)
        {
            // Enable copy effect if file drop
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }

        private void tb_demoFilePath_DragDrop(object sender, DragEventArgs e)
        {
            // Ensure dropped data contains file paths
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            foreach (string file in files)
            {
                // Accept only .dem files (case-insensitive)
                if (Path.GetExtension(file)
                        .Equals(".dem", StringComparison.OrdinalIgnoreCase))
                {
                    tb_demoFilePath.Text = file;
                    lbl_ReadInfo.ForeColor = Color.Red;
                    lbl_ReadInfo.Text = "Read demo file";

                    readDemoFile(file);
                    return;
                }
            }

            // Show warning for invalid format
            MessageBox.Show(
                "Please only drop files with the extension .dem.",
                "Invalid format",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }


        /// <summary>
        /// Reads a CS1 demo file and captures up to 10 players into the snapshot array.
        /// Uses PlayerConnectFull events to collect players as they fully connect; if fewer
        /// than 10 are collected by the first round start, falls back to capture all players
        /// known at that point. Finally ensures that any late joins (e.g. after knife rounds)
        /// are included by waiting for the complete demo read, and performs a last-resort
        /// pass over demo.Players before loading the UI grid.
        /// </summary>
        /// <param name="demoPath">Path to the .dem file to read.</param>
        private async void readDemoFile(string demoPath)
        {
            // Initialize parser and clear previous snapshot
            demo = new CsDemoParser();
            snapshot = null;

            // TaskCompletionSource to signal when we've got enough players
            var tcs = new TaskCompletionSource<bool>();
            bool roundFallbackDone = false;

            // Listen for fully-connected players and collect them.
            demo.Source1GameEvents.PlayerConnectFull += (Source1PlayerConnectFullEvent e) =>
            {
                // Build a distinct list of current players using PlayerInfo.Name
                var list = demo.Players
                    .Where(p => !string.IsNullOrWhiteSpace(p.PlayerInfo.Name))
                    .Select(p => new PlayerSnapshot
                    {
                        UserId = p.PlayerInfo.Userid + 1,
                        PlayerName = p.PlayerInfo.Name!,
                        TeamNumber = (int)p.Team.TeamNum,
                        TeamName = p.Team.ClanTeamname
                    })
                    .DistinctBy(ps => ps.UserId)
                    .ToArray();

                // Once we have 10 or more unique players, set snapshot and signal completion
                if (list.Length >= 10)
                {
                    snapshot = list;
                    tcs.TrySetResult(true);
                }
            };

            // Fallback at first RoundStart: capture any players known so far by PlayerName
            demo.Source1GameEvents.RoundStart += (Source1RoundStartEvent e) =>
            {
                if (roundFallbackDone)
                    return; // only run fallback once

                roundFallbackDone = true;

                var list = demo.Players
                    .Where(p => !string.IsNullOrWhiteSpace(p.PlayerName))
                    .Select(p => new PlayerSnapshot
                    {
                        UserId = p.PlayerInfo.Userid + 1,
                        PlayerName = p.PlayerName!,
                        TeamNumber = (int)p.Team.TeamNum,
                        TeamName = p.Team.ClanTeamname
                    })
                    .DistinctBy(ps => ps.UserId)
                    .ToArray();

                // If any players were found at this stage, treat that as a completion
                if (list.Length > 0)
                {
                    snapshot = list;
                    tcs.TrySetResult(true);
                }
            };

            try
            {
                // Open demo file stream with a large buffer for speed
                using var stream = new FileStream(
                    demoPath,
                    FileMode.Open, FileAccess.Read, FileShare.Read,
                    bufferSize: 4096 * 1024);

                var reader = DemoFileReader.Create(demo, stream);
                var readTask = reader.ReadAllAsync().AsTask();

                // Wait until either:
                // - tcs.Task signals we've got enough players,
                // - or the demo file has been fully read
                await Task.WhenAny(readTask, tcs.Task);

                // Ensure the read completes so late join events are processed
                await readTask;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading the demo: {ex.Message}");
                return;
            }
            finally
            {
                // Detach event handlers to prevent memory leaks or duplicate handling
                demo.Source1GameEvents.PlayerConnectFull -= null!;
                demo.Source1GameEvents.RoundStart -= null!;
            }

            // Final fallback: if still fewer than 10, grab everyone present in demo.Players
            if (snapshot == null || snapshot.Length < 10)
            {
                snapshot = demo.Players
                    .Where(p => p.PlayerInfo != null)
                    .Select(p => new PlayerSnapshot
                    {
                        UserId = p.PlayerInfo.Userid + 1,
                        PlayerName = !string.IsNullOrWhiteSpace(p.PlayerName)
                                        ? p.PlayerName!
                                        : p.PlayerInfo.Name ?? "Unknown",
                        TeamNumber = (int)p.Team.TeamNum,
                        TeamName = p.Team.ClanTeamname
                    })
                    .DistinctBy(ps => ps.UserId)  // ensure unique entries
                    .OrderBy(ps => ps.UserId)     // sort by Spec ID
                    .ToArray();
            }

            // Populate the grid using the captured snapshot
            loadCTTDataGrid();
        }


        // =====================================
        // DataGrid configuration and loading
        // =====================================
        /// <summary>
        /// Applies visual settings to the provided DataGridView.
        /// </summary>
        private void ConfigureDataGrid(DataGridView dgv)
        {
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            dgv.RowHeadersVisible = false;
            dgv.ClearSelection();

            var specIdColumn = dgv.Columns["Spec ID"];
            specIdColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            specIdColumn.Width++;

            dgv.Columns["Players"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }


        /// <summary>
        /// Populates the CT and T grids with player data from the snapshot.
        /// </summary>
        private void loadCTTDataGrid()
        {
            if (snapshot == null)
            {
                MessageBox.Show("Error reading demo file!");
                return;
            }

            var ctPlayers = snapshot.Where(p => p.TeamNumber == 3).OrderBy(p => p.UserId).ToList(); // Get CT Data from the Snapshot.
            var tPlayers = snapshot.Where(p => p.TeamNumber == 2).OrderBy(p => p.UserId).ToList();  // Get  T Data from the Snapshot.

            dGv_CT.DataSource = CreateDataTable(ctPlayers, out var teamAName);
            dGv_T.DataSource = CreateDataTable(tPlayers, out var teamBName);

            lbl_TeamA.Text = teamAName;
            lbl_TeamB.Text = teamBName;

            ConfigureDataGrid(dGv_CT);
            ConfigureDataGrid(dGv_T);

            lbl_ReadInfo.ForeColor = Color.DarkGreen;
            lbl_ReadInfo.Text = "File loaded";
            btn_CopyToClipboard.Enabled = true;

            resetAll();
        }


        /// <summary>
        /// Builds a DataTable for a list of players and returns the team name.
        /// </summary>
        private DataTable CreateDataTable(List<PlayerSnapshot> players, out string teamName)
        {
            var table = new DataTable();
            table.Columns.Add("Spec ID", typeof(int));
            table.Columns.Add("Players", typeof(string));

            teamName = players.FirstOrDefault()?.TeamName ?? "Unknown";

            foreach (var p in players)
            {
                table.Rows.Add(p.UserId, p.PlayerName);
            }

            return table;
        }


        /// <summary>
        /// Assigns checkboxes to their respective team lists for batch operations.
        /// </summary>
        private void InitializeCheckboxGroup()
        {
            teamACheckboxes.AddRange(new[] { cb_TeamAP1, cb_TeamAP2, cb_TeamAP3, cb_TeamAP4, cb_TeamAP5 });
            teamBCheckboxes.AddRange(new[] { cb_TeamBP1, cb_TeamBP2, cb_TeamBP3, cb_TeamBP4, cb_TeamBP5 });
        }


        /// <summary>
        /// Registers all UI event handlers:
        /// – Links each individual team checkbox to UpdateBitField and SyncSelectAllCheckbox.
        /// – Links each "Select All" checkbox to bulk ToggleCheckboxes via named handlers.
        /// – Links the Copy button to copy the console command to the clipboard and show a tooltip.
        /// </summary>
        private void InitializeEventHandlers()
        {
            // Team A (CT) individual checkboxes
            for (int i = 0; i < teamACheckboxes.Count; i++)
            {
                int index = i; // capture for closure
                teamACheckboxes[i].CheckedChanged += (s, e) =>
                {
                    // 1) Update the bitfield for this player
                    UpdateBitField(
                        (CheckBox)s,
                        dGv_CT.Rows[index],
                        ref GetTeamAFieldByIndex(index)
                    );
                    // 2) Synchronize the "Select All Team A" checkbox
                    SyncSelectAllCheckbox(teamACheckboxes, cb_AllTeamA);
                };
            }

            // Team B (T) individual checkboxes
            for (int i = 0; i < teamBCheckboxes.Count; i++)
            {
                int index = i;
                teamBCheckboxes[i].CheckedChanged += (s, e) =>
                {
                    UpdateBitField(
                        (CheckBox)s,
                        dGv_T.Rows[index],
                        ref GetTeamBFieldByIndex(index)
                    );
                    SyncSelectAllCheckbox(teamBCheckboxes, cb_AllTeamB);
                };
            }

            // Bulk-select handlers (named methods allow unsubscription if needed)
            cb_AllTeamA.CheckStateChanged += cb_AllTeamA_CheckStateChanged;
            cb_AllTeamB.CheckStateChanged += cb_AllTeamB_CheckStateChanged;

            // Copy-to-Clipboard button
            btn_CopyToClipboard.Click += (s, e) =>
            {
                Clipboard.SetText(tb_ConsoleCommand.Text);
                ShowCopyTooltip();
                this.ActiveControl = null; // Remove focus to avoid accidental re-triggers
            };
        }


        /// <summary>
        /// Named handler for "Select All Team A" – toggles all Team A checkboxes.
        /// Guarded by _isSyncingSelectAll to avoid loops when SyncSelectAllCheckbox sets Checked.
        /// </summary>
        private void cb_AllTeamA_CheckStateChanged(object sender, EventArgs e)
        {
            if (_isSyncingSelectAll) return;
            ToggleCheckboxes(teamACheckboxes, cb_AllTeamA.Checked);
        }


        /// <summary>
        /// Named handler for "Select All Team B" – toggles all Team B checkboxes.
        /// Guarded by _isSyncingSelectAll to avoid loops when SyncSelectAllCheckbox sets Checked.
        /// </summary>
        private void cb_AllTeamB_CheckStateChanged(object sender, EventArgs e)
        {
            if (_isSyncingSelectAll) return;
            ToggleCheckboxes(teamBCheckboxes, cb_AllTeamB.Checked);
        }


        /// <summary>
        /// Synchronizes the given "Select All" checkbox based on its group's states.
        /// Prevents recursive toggles via the _isSyncingSelectAll flag.
        /// </summary>
        /// <param name="groupCheckboxes">
        /// The list of individual checkboxes in this group.
        /// </param>
        /// <param name="selectAllCheckbox">
        /// The "Select All" checkbox to update.
        /// </param>
        private void SyncSelectAllCheckbox(List<CheckBox> groupCheckboxes, CheckBox selectAllCheckbox)
        {
            _isSyncingSelectAll = true;
            // If every individual checkbox is checked, mark "Select All" as checked, else uncheck.
            selectAllCheckbox.Checked = groupCheckboxes.All(cb => cb.Checked);
            _isSyncingSelectAll = false;
        }


        /// <summary>
        /// Returns a reference to the Team A bitfield variable by zero-based index.
        /// </summary>
        /// <param name="index">Index into teamACheckboxes (0..4).</param>
        /// <returns>Reference to teamAP1…teamAP5.</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// If index is not in 0..4.
        /// </exception>
        private ref int GetTeamAFieldByIndex(int index)
        {
            switch (index)
            {
                case 0: return ref teamAP1;
                case 1: return ref teamAP2;
                case 2: return ref teamAP3;
                case 3: return ref teamAP4;
                case 4: return ref teamAP5;
                default: throw new IndexOutOfRangeException(nameof(index));
            }
        }


        /// <summary>
        /// Returns a reference to the Team B bitfield variable by zero-based index.
        /// </summary>
        /// <param name="index">Index into teamBCheckboxes (0..4).</param>
        /// <returns>Reference to teamBP1…teamBP5.</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// If index is not in 0..4.
        /// </exception>
        private ref int GetTeamBFieldByIndex(int index)
        {
            switch (index)
            {
                case 0: return ref teamBP1;
                case 1: return ref teamBP2;
                case 2: return ref teamBP3;
                case 3: return ref teamBP4;
                case 4: return ref teamBP5;
                default: throw new IndexOutOfRangeException(nameof(index));
            }
        }


        // =====================================
        // Checkbox toggle and reset methods
        // =====================================
        /// <summary>
        /// Toggles all checkboxes in the given list to the specified state.
        /// </summary>
        private void ToggleCheckboxes(List<CheckBox> checkboxes, bool isChecked)
        {
            foreach (var cb in checkboxes)
                cb.Checked = isChecked;
        }


        /// <summary>
        /// Displays a transient “Copied!” tooltip above the copy button.
        /// Configures and shows the <see cref="_copyToolTip"/> with zero delay and a 1 second auto-pop duration.
        /// </summary>
        private void ShowCopyTooltip()
        {
            _copyToolTip.AutomaticDelay = 0;
            _copyToolTip.AutoPopDelay = 1000;
            _copyToolTip.InitialDelay = 0;
            _copyToolTip.ReshowDelay = 0;
            _copyToolTip.ShowAlways = true;

            // Show tooltip over the TextBox
            var offset = new Point(btn_CopyToClipboard.Width / 2, -btn_CopyToClipboard.Height / 2);
            _copyToolTip.Show("Copied!", btn_CopyToClipboard, offset, 1000);
        }


        /// <summary>
        /// Resets all checkboxes and console prompt after loading a new file.
        /// </summary>
        private void resetAll()
        {
            ResetCheckboxes(teamACheckboxes);
            ResetCheckboxes(teamBCheckboxes);
            ResetCheckboxes(new List<CheckBox> { cb_AllTeamA, cb_AllTeamB });

            tb_ConsoleCommand.Text =
                "select one or more players you would like to hear in the demo ..";
        }


        /// <summary>
        /// Generic method to clear and enable checkboxes in the provided list.
        /// </summary>
        private void ResetCheckboxes(List<CheckBox> checkboxes)
        {
            foreach (var cb in checkboxes)
            {
                cb.Enabled = true;
                cb.Checked = false;
            }
        }


        // =====================================
        // Bitfield calculation
        // =====================================
        /// <summary>
        /// Updates the specified bitfield entry based on the checkbox state and row data.
        /// </summary>
        private void UpdateBitField(CheckBox checkbox, DataGridViewRow row, ref int field)
        {
            if (row?.Cells[0].Value == null)
            {
                field = 0;
                return;
            }

            field = checkbox.Checked
                ? getPlayBitField(row.Cells[0].Value)
                : 0;

            changeConsoleCommand();
        }


        /// <summary>
        /// Computes the bitfield for a given spec ID (must be 4..13).
        /// </summary>
        private int getPlayBitField(object cellValue)
        {
            if (cellValue == null || !int.TryParse(cellValue.ToString(), out int specPlayerId))
            {
                throw new ArgumentException("Invalid spec ID in the cell.");
            }

            if (specPlayerId < 4 || specPlayerId > 13)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(specPlayerId),
                    "Spec-ID must be between 4 and 13.");
            }

            return 1 << (specPlayerId - 1);
        }


        /// <summary>
        /// Updates the console command textbox based on current combined bitfields.
        /// </summary>
        public void changeConsoleCommand()
        {
            int voiceBitField = teamAP1 + teamAP2 + teamAP3 + teamAP4 + teamAP5
                               + teamBP1 + teamBP2 + teamBP3 + teamBP4 + teamBP5;

            tb_ConsoleCommand.Text =
                $"tv_listen_voice_indices {voiceBitField}; tv_listen_voice_indices_h {voiceBitField}";
        }
    }
}