using DemoFile;
using DemoFile.Game.Cs;
using System.Data;
using System.Resources;

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
            public ulong? PlayerSteamID { get; set; }   // Get the SteamID64 number

            public override string ToString()
            {
                return PlayerName ?? "Unknown Player";
            }
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


        // ------------------------------------------
        // Static Links part to Steam Profiles etc.
        // ------------------------------------------
        private static string _steamProfileLink = "http://steamcommunity.com/profiles/";
        private static string _cswatchProfileLink = "https://cswatch.in/player/";
        private static string _csStatsProfileLink = "https://csstats.gg/player/";
        private static string _leetifyProfileLink = "https://leetify.com/app/profile/";


        // ------------------------------------------
        // Other Global Strings
        // ------------------------------------------
        string _mapName = "no Mapname!";
        string _duration = "00:00:00";
        string _hostName = "No Hostname";


        // ------------------------------------------
        // Verson Nr. of this project
        // ------------------------------------------
        private const string _VERSIONNR = "v.0.9.9";


        // =================
        // Form constructor
        // =================
        /// <summary>
        /// Initializes form components and sets up checkbox handlers.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();

            // Costum
            this.Text = "Faceit Demo Voice Calculator " + _VERSIONNR;
            InitializeCheckboxGroup();
            InitializeEventHandlers();
            AddShellContextMenu.ValidateShellIntegration();
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




        // ====================================
        // When the program is called via CLI
        // ====================================
        public void SetDemoFileOnStartup(string filePath)
        {
            if (!File.Exists(filePath) || Path.GetExtension(filePath).ToLower() != ".dem") return;

            tb_demoFilePath.Text = filePath;
            lbl_ReadInfo.ForeColor = Color.Red;
            lbl_ReadInfo.Text = "Read demo file";

            // The same logic as drag & drop
            DisableAll();
            btn_MoveToCSFolder.Enabled = false;
            btn_CopyToClipboard.Enabled = false;
            tb_ConsoleCommand.ForeColor = Color.Black;
            tb_ConsoleCommand.Text = "Select one or more players you would like to hear in the demo...";

            ReadDemoFile(filePath);
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
                    // Clear All
                    btn_MoveToCSFolder.Enabled = false;
                    btn_CopyToClipboard.Enabled = false;
                    DisableAll();
                    tb_ConsoleCommand.ForeColor = Color.Black;
                    tb_ConsoleCommand.Text = "Select one or more players you would like to hear in the demo...";

                    // Show reading
                    tb_demoFilePath.Text = file;
                    lbl_ReadInfo.ForeColor = Color.Red;
                    lbl_ReadInfo.Text = "Read demo file";

                    ReadDemoFile(file);
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
        /// Reads a CS2 demo file and collects players until both teams (team 2 and 3) have 5 players each.
        /// This function is for testing the read function of Pro Matches
        /// </summary>
        /// <param name="demoPath">Pfad zur .dem-Datei.</param>
        private async void ReadDemoFile(string demoPath)
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

                    // Check whether the player has already been recorded
                    if (collected.Any(x => x.UserId == (int)p.EntityIndex.Value))
                        continue;

                    // Add players if team is not yet full
                    if (collected.Count(x => x.TeamNumber == team) < neededPerTeam)
                    {
                        collected.Add(new PlayerSnapshot
                        {
                            UserId = (int)p.EntityIndex.Value,
                            PlayerName = p.PlayerName!,
                            TeamNumber = team,
                            TeamName = p.Team.ClanTeamname,
                            PlayerSteamID = p.PlayerInfo.Steamid
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
                    _mapName = _demo.ServerInfo?.MapName ?? "no Mapname!";
                    _duration = TicksToTimeString(_demo.TickCount.Value);
                    _hostName = _demo.ServerInfo?.HostName ?? "No Hostname";
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
            dgv.EnableHeadersVisualStyles = false; // Prevents Windows default marking
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = dgv.ColumnHeadersDefaultCellStyle.BackColor;
            dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = dgv.ColumnHeadersDefaultCellStyle.ForeColor;

            var specIdColumn = dgv.Columns["Spec ID"];
            specIdColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            specIdColumn.Width++;

            dgv.Columns["Players"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            // Disable sorting for all columns
            foreach (DataGridViewColumn column in dgv.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }
        }


        /// <summary>
        /// Configures a context menu for the given DataGridView, allowing SteamID actions
        /// and links to external profile pages. Menu items include icons and are updated
        /// dynamically based on the right-clicked row.
        /// </summary>
        private void ConfigureContextMenu(DataGridView dgv, List<PlayerSnapshot> playerList)
        {
            // Header label (disabled menu item) for player name
            var playerHeader = new ToolStripMenuItem("Player")
            {
                Enabled = false, // Acts as a label only
                Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold),
                Image = Properties.Resources.iconPlayer
            };

            // Menu item for copying SteamID64
            var copySteamId = new ToolStripMenuItem("Copy SteamID64")
            {
                Image = Properties.Resources.iconSteam
            };

            // Define multiple profile links with their label, URL base, and associated icon from resources
            var profileDefinitions = new (string Label, string UrlPrefix, Image Icon)[]
            {
                ("Open Steam Profile", _steamProfileLink, Properties.Resources.iconSteam),
                ("Open cswatch.in Profile", _cswatchProfileLink, Properties.Resources.iconCsWatch),
                ("Open leetify.com Profile", _leetifyProfileLink, Properties.Resources.iconLeetify),
                ("Open csstats.gg Profile", _csStatsProfileLink, Properties.Resources.iconCsStats)
            };

            // Create menu items from profile definitions
            var profileItems = profileDefinitions.Select(def => new ToolStripMenuItem(def.Label)
            {
                Image = def.Icon
            }).ToList();

            // Add items to the context menu
            ContextMenuStrip cms = new ContextMenuStrip();
            cms.Items.Add(playerHeader);           // Header
            cms.Items.Add(new ToolStripSeparator()); // Separator above Copy
            cms.Items.Add(copySteamId);            // Copy item
            cms.Items.Add(new ToolStripSeparator()); // Separator below Copy
            cms.Items.AddRange(profileItems.ToArray()); // All profile links

            dgv.ContextMenuStrip = cms;

            // Copy SteamID64 to clipboard
            copySteamId.Click += (s, e) =>
            {
                if (copySteamId.Tag is PlayerSnapshot p && p.PlayerSteamID.HasValue)
                {
                    Clipboard.SetText(p.PlayerSteamID.ToString() ?? "Error reading SteamID");
                    MessageBox.Show($"SteamID64 for {p.PlayerName} copied to clipboard.", "Info");
                }
            };

            // Handle profile link clicks
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

            // Right-click handling
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
            lbl_MapName.Text = _mapName;
            lbl_PlayTime.Text = _duration;

            ConfigureDataGrid(dGv_CT);
            ConfigureDataGrid(dGv_T);
            ConfigureContextMenu(dGv_CT, ctPlayers);
            ConfigureContextMenu(dGv_T, tPlayers);

            lbl_ReadInfo.ForeColor = Color.DarkGreen;

            // If the demo comes from a SourceTV server, it will also have audio.
            if (_hostName.Contains("SourceTV"))
            {
                lbl_ReadInfo.Text = "File loaded with audio stream";
                btn_CopyToClipboard.Enabled = true;
                ResetAll();
            }
            else
            {
                lbl_ReadInfo.Text = "File loaded, audio stream may not be available!";
                tb_ConsoleCommand.ForeColor = Color.DarkRed;
                tb_ConsoleCommand.Text = "The loaded demo comes from competitive mode and has no audio...";

                btn_CopyToClipboard.Enabled = false;
                DisableAll();
            }

            btn_MoveToCSFolder.Enabled = true;
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
        }


        /// <summary>
        /// Disable all checkboxes and console prompt after loading a new file.
        /// </summary>
        private void DisableAll()
        {
            DisableCheckboxes(_teamACheckboxes);
            DisableCheckboxes(_teamBCheckboxes);
            DisableCheckboxes(new List<CheckBox> { cb_AllTeamA, cb_AllTeamB });


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


        /// <summary>
        /// Generic method to clear and disable checkboxes in the provided list.
        /// </summary>
        private void DisableCheckboxes(List<CheckBox> checkboxes)
        {
            foreach (var cb in checkboxes)
            {
                cb.Enabled = false;
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

            if (specPlayerId < 1 || specPlayerId > 20)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(specPlayerId),
                    "Spec-ID must be between 1 and 20.");
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


        /// <summary>
        /// Converts a tick count into a formatted time string (hh:mm:ss) based on the provided tick rate.
        /// </summary>
        /// <param name="ticks">The total number of ticks from the demo.</param>
        /// <param name="tickRate">The tick rate of the server (default is 64 ticks per second).</param>
        /// <returns>A time string representing the duration in hh:mm:ss format.</returns>
        /// <exception cref="ArgumentException">Thrown if the tick rate is zero or negative.</exception>
        public static string TicksToTimeString(int ticks, double tickRate = 64.0)
        {
            if (tickRate <= 0)
                throw new ArgumentException("Tick rate must be greater than 0.");

            // Convert total ticks to seconds
            double totalSeconds = ticks / tickRate;

            // Create a TimeSpan from the total seconds
            TimeSpan duration = TimeSpan.FromSeconds(totalSeconds);

            // Return the formatted duration string
            return duration.ToString(@"hh\:mm\:ss");
        }


        /// <summary>
        /// Handles the MouseDown event for the CT DataGridView.
        /// Clears the selection and current cell from the T DataGridView when the CT DataGridView is clicked.
        /// Ensures only one DataGridView has an active selection/focus at a time.
        /// </summary>
        private void dGv_CT_MouseDown(object sender, MouseEventArgs e)
        {
            dGv_T.ClearSelection();
            dGv_T.CurrentCell = null;
        }

        /// <summary>
        /// Handles the MouseDown event for the T DataGridView.
        /// Clears the selection and current cell from the CT DataGridView when the T DataGridView is clicked.
        /// Ensures only one DataGridView has an active selection/focus at a time.
        /// </summary>
        private void dGv_T_MouseDown(object sender, MouseEventArgs e)
        {
            dGv_CT.ClearSelection();
            dGv_CT.CurrentCell = null;
        }


        // =======================
        // Menubar event handlers
        // =======================

        /// <summary>
        /// Opens the 'Small Guide' window showing usage instructions for the application.
        /// </summary>
        private void smallGuideToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Show usage instructions
            OpenForm<HowTo>();
        }


        /// <summary>
        /// Adds the application to the Windows shell context menu for quick access.
        /// </summary>
        private void addToShellContextMenuToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddShellContextMenu.AddShellIntegration();
        }


        /// <summary>
        /// Removes the application from the Windows shell context menu.
        /// </summary>
        private void removeFromShellContextMenuToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddShellContextMenu.RemoveShellIntegration();
        }


        /// <summary>
        /// Opens the 'About' window displaying information about the application.
        /// </summary>
        private void aboutToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            // Show application information
            OpenForm<About>();
        }


        /// <summary>
        /// Checks if a newer version of the application is available via GitHub releases.
        /// </summary>
        private void checkForUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _ = VersionChecker.IsNewerVersionAvailable(_VERSIONNR);
        }


        /// <summary>
        /// Ensures a CS2 demo folder is configured
        /// the file specified in the textbox into that folder. If successful,
        /// updates the textbox and reloads the demo file.
        /// </summary>
        private void changeDemoFolderPathToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Calls the path config function then
            CS2PathConfig.EnsurePathConfigured();
            // Fetches the path from the config again
            _csDemoFolderPath = CS2PathConfig.GetPath();
        }

        private async void extractAudiosFromDemoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            progressBar.Value = 0;

            var progress = new Progress<float>(value =>
            {
                progressBar.Value = (int)(value * 100);
            });

            bool result = await AudioExtractor.ExtractAsync(tb_demoFilePath.Text, progress);

            if (result)
                MessageBox.Show("Extraktion abgeschlossen!");
            else
                MessageBox.Show("Fehler beim Extrahieren.");
        }
    }
}