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
        private CsDemoParser? _demo = null; // An object is created to process the read DemoStream and read out the desired information.
        private PlayerSnapshot[]? _snapshot = null; // The information collected from the demo is saved in an array.


        // ============================================================================================
        // Bitfields for voice indices, the calculated bitfield numbers are stored in these variables
        // ============================================================================================
        private int _teamAP1, _teamAP2, _teamAP3, _teamAP4, _teamAP5;
        private int _teamBP1, _teamBP2, _teamBP3, _teamBP4, _teamBP5;


        // ====================================================================================================
        // Checkbox groups for easy toggling. All checkboxes are grouped into a list to keep the code cleaner.
        // =====================================================================================================
        private readonly List<CheckBox> _teamACheckboxes = new();
        private readonly List<CheckBox> _teamBCheckboxes = new();


        // =====================================================================
        // The shared instance used to display non-blocking copy notifications.
        // =====================================================================
        private readonly ToolTip _copyToolTip = new ToolTip();


        // -------------------------------------
        // Class-level field to prevent recursion
        // -------------------------------------
        private bool _isSyncingSelectAll = false;


        // ---------------------
        // CS2 Demo Folder Path
        // ---------------------
        private string? _csDemoFolderPath = null;


        // ------------------------------------------
        // The hash values before and after the move
        // ------------------------------------------
        private byte[]? _sourceHash = null;
        private byte[]? _destinationHash = null;


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
        private void HowToUseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Show usage instructions
            OpenForm<HowTo>();
        }

        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Show application information
            OpenForm<About>();
        }


        // ====================================
        // Drag & drop for demo file selection
        // ====================================
        private void TB_demoFilePath_DragEnter(object sender, DragEventArgs e)
        {
            // Enable copy effect if file drop
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }

        private void TB_demoFilePath_DragDrop(object sender, DragEventArgs e)
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

                    ReadDemoFile2(file);
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
        private async void ReadDemoFile(string demoPath)
        {
            // Initialize parser and clear previous snapshot
            _demo = new CsDemoParser();
            _snapshot = null;

            // TaskCompletionSource to signal when we've got enough players
            var tcs = new TaskCompletionSource<bool>();
            bool roundFallbackDone = false;

            // Listen for fully-connected players and collect them.
            _demo.Source1GameEvents.PlayerConnectFull += (Source1PlayerConnectFullEvent e) =>
            {
                // Build a distinct list of current players using PlayerInfo.Name
                var list = _demo.Players
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
                    _snapshot = list;
                    tcs.TrySetResult(true);
                }
            };

            // Fallback at first RoundStart: capture any players known so far by PlayerName
            _demo.Source1GameEvents.RoundStart += (Source1RoundStartEvent e) =>
            {
                if (roundFallbackDone)
                    return; // only run fallback once

                roundFallbackDone = true;

                var list = _demo.Players
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
                    _snapshot = list;
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

                var reader = DemoFileReader.Create(_demo, stream);
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
                _demo.Source1GameEvents.PlayerConnectFull -= null!;
                _demo.Source1GameEvents.RoundStart -= null!;
            }

            // Final fallback: if still fewer than 10, grab everyone present in demo.Players
            if (_snapshot == null || _snapshot.Length < 10)
            {
                _snapshot = _demo.Players
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
            LoadCTTDataGrid();
        }

        /// <summary>
        /// Reads a CS2 demo file and collects players until both teams (team 2 and 3) have 5 players each.
        /// This function is for testing the read function of Pro Matches
        /// </summary>
        /// <param name="demoPath">Pfad zur .dem-Datei.</param>
        private async void ReadDemoFile2(string demoPath)
        {
            // Initialisierung
            _demo = new CsDemoParser();
            _snapshot = null;
            var tcs = new TaskCompletionSource<bool>();
            bool done = false;
            const int neededPerTeam = 5;
            var collected = new List<PlayerSnapshot>();

            // RoundStart event for player registration
            _demo.Source1GameEvents.RoundStart += (Source1RoundStartEvent e) =>
            {
                if (done) return;

                foreach (var p in _demo.Players)
                {
                    if (string.IsNullOrWhiteSpace(p.PlayerName))
                        continue;

                    int team = (int)p.Team.TeamNum;
                    if (team != 2 && team != 3)
                        continue;

                    // Check whether the player has already been recorded (UserId +1 for 1-based index)
                    if (collected.Any(x => x.UserId == p.PlayerInfo.Userid + 1))
                        continue;

                    // Add players if team is not yet full
                    if (collected.Count(x => x.TeamNumber == team) < neededPerTeam)
                    {
                        collected.Add(new PlayerSnapshot
                        {
                            UserId = p.PlayerInfo.Userid + 1,
                            PlayerName = p.PlayerName!,
                            TeamNumber = team,
                            TeamName = p.Team.ClanTeamname
                        });
                    }
                }

                // Check whether both teams are complete
                bool teamsComplete = collected.Count(x => x.TeamNumber == 2) >= neededPerTeam &&
                                     collected.Count(x => x.TeamNumber == 3) >= neededPerTeam;
                if (teamsComplete)
                {
                    done = true;
                    _snapshot = collected.ToArray();
                    tcs.TrySetResult(true);
                }
            };

            try
            {
                // Read demo
                using var stream = new FileStream(demoPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096 * 1024);
                var reader = DemoFileReader.Create(_demo, stream);
                var readTask = reader.ReadAllAsync().AsTask();

                // Wait for teams or the end of the demo
                await Task.WhenAny(readTask, tcs.Task);
                await readTask; // Process remaining events
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error when reading the demo: {ex.Message}");
                return;
            }
            finally
            {
                _demo.Source1GameEvents.RoundStart -= null!;
            }

            // Update UI
            LoadCTTDataGrid();
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
        private void LoadCTTDataGrid()
        {
            if (_snapshot == null)
            {
                MessageBox.Show("Error reading demo file!");
                return;
            }

            var ctPlayers = _snapshot.Where(p => p.TeamNumber == 3).OrderBy(p => p.UserId).ToList(); // Get CT Data from the Snapshot.
            var tPlayers = _snapshot.Where(p => p.TeamNumber == 2).OrderBy(p => p.UserId).ToList();  // Get  T Data from the Snapshot.

            dGv_CT.DataSource = CreateDataTable(ctPlayers, out var teamAName);
            dGv_T.DataSource = CreateDataTable(tPlayers, out var teamBName);

            lbl_TeamA.Text = teamAName;
            lbl_TeamB.Text = teamBName;

            ConfigureDataGrid(dGv_CT);
            ConfigureDataGrid(dGv_T);

            lbl_ReadInfo.ForeColor = Color.DarkGreen;
            lbl_ReadInfo.Text = "File loaded";
            btn_CopyToClipboard.Enabled = true;
            btn_MoveToCSFolder.Enabled = true;

            ResetAll();
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
            _teamACheckboxes.AddRange(new[] { cb_TeamAP1, cb_TeamAP2, cb_TeamAP3, cb_TeamAP4, cb_TeamAP5 });
            _teamBCheckboxes.AddRange(new[] { cb_TeamBP1, cb_TeamBP2, cb_TeamBP3, cb_TeamBP4, cb_TeamBP5 });
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
            for (int i = 0; i < _teamACheckboxes.Count; i++)
            {
                int index = i; // capture for closure
                _teamACheckboxes[i].CheckedChanged += (s, e) =>
                {
                    // 1) Update the bitfield for this player
                    UpdateBitField(
                        (CheckBox)s,
                        dGv_CT.Rows[index],
                        ref GetTeamAFieldByIndex(index)
                    );
                    // 2) Synchronize the "Select All Team A" checkbox
                    SyncSelectAllCheckbox(_teamACheckboxes, cb_AllTeamA);
                };
            }

            // Team B (T) individual checkboxes
            for (int i = 0; i < _teamBCheckboxes.Count; i++)
            {
                int index = i;
                _teamBCheckboxes[i].CheckedChanged += (s, e) =>
                {
                    UpdateBitField(
                        (CheckBox)s,
                        dGv_T.Rows[index],
                        ref GetTeamBFieldByIndex(index)
                    );
                    SyncSelectAllCheckbox(_teamBCheckboxes, cb_AllTeamB);
                };
            }

            // Bulk-select handlers (named methods allow unsubscription if needed)
            cb_AllTeamA.CheckStateChanged += CB_AllTeamA_CheckStateChanged;
            cb_AllTeamB.CheckStateChanged += CB_AllTeamB_CheckStateChanged;

            // Copy-to-Clipboard button
            btn_CopyToClipboard.Click += (s, e) =>
            {
                Clipboard.SetText(tb_ConsoleCommand.Text);
                ShowCopyTooltip();
                this.ActiveControl = null; // Remove focus to avoid accidental re-triggers
            };

            // Move to CS2 Folder button
            btn_MoveToCSFolder.Click += (s, e) =>
            {
                MoveToCSFolder();
            };
        }


        /// <summary>
        /// Named handler for "Select All Team A" – toggles all Team A checkboxes.
        /// Guarded by _isSyncingSelectAll to avoid loops when SyncSelectAllCheckbox sets Checked.
        /// </summary>
        private void CB_AllTeamA_CheckStateChanged(object sender, EventArgs e)
        {
            if (_isSyncingSelectAll) return;
            ToggleCheckboxes(_teamACheckboxes, cb_AllTeamA.Checked);
        }


        /// <summary>
        /// Named handler for "Select All Team B" – toggles all Team B checkboxes.
        /// Guarded by _isSyncingSelectAll to avoid loops when SyncSelectAllCheckbox sets Checked.
        /// </summary>
        private void CB_AllTeamB_CheckStateChanged(object sender, EventArgs e)
        {
            if (_isSyncingSelectAll) return;
            ToggleCheckboxes(_teamBCheckboxes, cb_AllTeamB.Checked);
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
                case 0: return ref _teamAP1;
                case 1: return ref _teamAP2;
                case 2: return ref _teamAP3;
                case 3: return ref _teamAP4;
                case 4: return ref _teamAP5;
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
                case 0: return ref _teamBP1;
                case 1: return ref _teamBP2;
                case 2: return ref _teamBP3;
                case 3: return ref _teamBP4;
                case 4: return ref _teamBP5;
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
        private void ResetAll()
        {
            ResetCheckboxes(_teamACheckboxes);
            ResetCheckboxes(_teamBCheckboxes);
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
                ? GetPlayBitField(row.Cells[0].Value)
                : 0;

            ChangeConsoleCommand();
        }


        /// <summary>
        /// Computes the bitfield for a given spec ID (must be 4..13).
        /// </summary>
        private int GetPlayBitField(object cellValue)
        {
            if (cellValue == null || !int.TryParse(cellValue.ToString(), out int specPlayerId))
            {
                throw new ArgumentException("Invalid spec ID in the cell.");
            }

            if (specPlayerId < 1 || specPlayerId > 13)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(specPlayerId),
                    "Spec-ID must be between 1 and 13.");
            }

            return 1 << (specPlayerId - 1);
        }


        /// <summary>
        /// Updates the console command textbox based on current combined bitfields.
        /// </summary>
        public void ChangeConsoleCommand()
        {
            int voiceBitField = _teamAP1 + _teamAP2 + _teamAP3 + _teamAP4 + _teamAP5
                               + _teamBP1 + _teamBP2 + _teamBP3 + _teamBP4 + _teamBP5;

            tb_ConsoleCommand.Text =
                $"tv_listen_voice_indices {voiceBitField}; tv_listen_voice_indices_h {voiceBitField}";
        }


        /// <summary>
        /// Ensures a CS2 demo folder is configured, then moves and renames
        /// the file specified in the textbox into that folder. If successful,
        /// updates the textbox and reloads the demo file.
        /// </summary>
        private void MoveToCSFolder()
        {
            string? movedFullFilePath = null;
            string? sourceFile = null;
            _sourceHash = null; _destinationHash = null;

            // Ensure we have a valid demo folder path (prompt if necessary)
            if (string.IsNullOrWhiteSpace(_csDemoFolderPath) || !Directory.Exists(_csDemoFolderPath))
            {
                // Fetches the path from the config
                _csDemoFolderPath = CS2PathConfig.GetPath();

                // If the path is invalid or does not exist
                if (!Directory.Exists(_csDemoFolderPath))
                {
                    // Calls the path config function then
                    CS2PathConfig.EnsurePathConfigured();
                    // Fetches the path from the config again
                    _csDemoFolderPath = CS2PathConfig.GetPath();
                }
            }

            // If folder exists, attempt move/rename
            if (!string.IsNullOrWhiteSpace(_csDemoFolderPath) && Directory.Exists(_csDemoFolderPath))
            {
                sourceFile = tb_demoFilePath.Text;

                // Only proceed if user provided a non-empty path
                if (!string.IsNullOrWhiteSpace(sourceFile))
                {
                    try
                    {
                        // Get Hash from Source
                        _sourceHash = sourceFile.ComputeFileHash();
                        // Move and rename; returns new full path or null
                        movedFullFilePath = sourceFile.MoveAndRenameFile(_csDemoFolderPath);
                    }
                    catch
                    {
                        // Optional: log exception
                        movedFullFilePath = null;
                    }
                }
            }

            // If move succeeded, update UI and reload demo
            if (!string.IsNullOrWhiteSpace(movedFullFilePath))
            {
                // Get destination Hash
                _destinationHash = movedFullFilePath.ComputeFileHash();
                // Hash should not be null
                if (_sourceHash != null && _destinationHash != null)
                {

                    if (_sourceHash.HashesAreEqual(_destinationHash)) // Hash are the same, move was ok 
                    {
                        tb_demoFilePath.Text = movedFullFilePath;
                        ReadDemoFile(movedFullFilePath);
                        MessageBox.Show("File was moved successfully.",
                            "All fine",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                            );
                    }
                    else // File move has gone wrong
                    {
                        MessageBox.Show(
                        "Error while moving the file!",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                        );
                    }
                }
            }
            else
            {
                // Operation was canceled or failed without exception
                MessageBox.Show(
                    "File move was canceled or failed.",
                    "Operation Canceled",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
        }

    }
}