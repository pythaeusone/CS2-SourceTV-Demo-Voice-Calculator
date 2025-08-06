using CS2VoiceSegmenter;
using DemoFile;
using DemoFile.Game.Cs;
using System.Data;
using System.Resources;
using System.Windows.Forms;

namespace FaceitDemoVoiceCalc
{
    public partial class MainForm : Form
    {
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
        // Folder Path
        // ---------------------
        private string? _csDemoFolderPath = null;
        private string? _audioFolderPath = null;


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
        private string _mapName = "no Mapname!";
        private string _duration = "00:00:00";
        private string _hostName = "No Hostname";
        private bool _voicePlayerOpen = false;
        private const int _WINDOWHEIGHT = 435;
        private const int _VOICEPLAYERHEIGHT = 635;
        private List<AudioEntry> _audioEntries;


        // ------------------------------------------
        // Verson Numbers. of this project
        // ------------------------------------------
        private const string _GUIVERSIONNR = "v.1.0.0";


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
            this.Text = "Faceit Demo Voice Calculator " + _GUIVERSIONNR;
            groupBox_VoicePlayer.Text = "  Voice-Player  ";

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


        /// <summary>
        /// Sets the demo file on application startup cli if the provided file path is valid and points to a .dem file.
        /// </summary>
        /// <param name="filePath">Path to the demo file.</param>
        public void SetDemoFileOnStartup(string filePath)
        {
            if (!File.Exists(filePath) || Path.GetExtension(filePath).ToLower() != ".dem") return;

            PrepareStart(filePath);
        }


        /// <summary>
        /// Handles the DragEnter event to enable the copy effect when files are dragged over the input field.
        /// </summary>
        private void TB_demoFilePath_DragEnter(object sender, DragEventArgs e)
        {
            // Enable copy effect if file drop
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }

        /// <summary>
        /// Handles the DragDrop event to validate and process dropped demo files.
        /// </summary>
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
                    PrepareStart(file);
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
        /// Prepares the application to read and process the given demo file.
        /// Async is for the CloseVoicePlayer function.
        /// </summary>
        /// <param name="demoPath">Path to the .dem file.</param>
        private async void PrepareStart(string demoPath)
        {
            // Clear All                    
            DisableAll();
            RemoveAudioFolder();
            _voicePlayerOpen = await CloseVoicePlayer();
            listBox_VoicePlayer.Items.Clear();
            dGv_VoicePlayer.Columns.Clear();

            // Default
            btn_MoveToCSFolder.Enabled = false;
            btn_CopyToClipboard.Enabled = false;
            extractorToolStripMenuItem.Enabled = false;

            tb_ConsoleCommand.ForeColor = Color.Black;
            tb_ConsoleCommand.Text = "Select one or more players you would like to hear in the demo...";

            lbl_progressBarText.Text = "";
            progressBar.Value = 0;

            // Show reading
            tb_demoFilePath.Text = demoPath;
            lbl_ReadInfo.ForeColor = Color.Red;
            lbl_ReadInfo.Text = "Read demo file";

            ReadDemoFile(demoPath);
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
                extractorToolStripMenuItem.Enabled = true;
                ResetAll();
            }
            else
            {
                lbl_ReadInfo.Text = "File loaded, audio stream may not be available!";
                tb_ConsoleCommand.ForeColor = Color.DarkRed;
                tb_ConsoleCommand.Text = "The loaded demo comes from competitive mode and has no audio...";

                btn_CopyToClipboard.Enabled = false;
                extractorToolStripMenuItem.Enabled = false;
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


        /// <summary>
        /// Removes the 'Audio' folder located in the application's current directory, including all subfolders and files.
        /// </summary>
        private void RemoveAudioFolder()
        {
            try
            {
                // Construct the path to the 'Audio' folder relative to the current directory
                string audioFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Audio");

                // Check if the folder exists before attempting to delete it
                if (Directory.Exists(audioFolderPath))
                {
                    // Delete the folder and all of its contents recursively
                    Directory.Delete(audioFolderPath, true);
                }
            }
            catch (Exception ex)
            {
                // Display an error message if folder deletion fails
                MessageBox.Show("Error deleting the folder: " + ex.Message);
            }
        }


        /// <summary>
        /// Animates the main form's height change with optional label repositioning.
        /// Temporarily disables movement, resizing, and control box interaction during the animation.
        /// </summary>
        /// <param name="targetHeight">The target height to animate to.</param>
        /// <param name="expand">If true, expands the form; if false, collapses it.</param>
        private async Task AnimateFormHeightAsync(int targetHeight, bool expand)
        {
            int step = 10; // Smaller step for smoother animation
            int distance = Math.Abs(this.Height - targetHeight);
            int steps = distance / step;
            int remainder = distance % step;
            int direction = expand ? 1 : -1;

            // Disable interactions during animation
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.Enabled = false;

            // Hide Elements during animation to avoid flickering or incorrect positions
            lbl_ReadInfo.Visible = false;
            lbl_byTeam.Visible = false;
            groupBox_VoicePlayer.Visible = false;
            groupBox_VoicePlayer.Enabled = false;

            for (int i = 0; i < steps; i++)
            {
                this.Height += step * direction;

                // Variable delay for smoother easing-like effect (starts fast, ends slow)
                int delay = (int)(4 + 8.0 * i / steps);
                await Task.Delay(delay);
            }

            if (remainder > 0)
            {
                this.Height += remainder * direction;
            }

            // Re-enable interactions after animation
            this.MinimizeBox = true;
            this.MaximizeBox = true;
            this.Enabled = true;
        }


        /// <summary>
        /// Animates the expansion of the main form to reveal the audio player.
        /// Updates the menu item text and tooltip accordingly.
        /// Restores label positions after animation.
        /// </summary>
        /// <returns>True if the expansion was successful.</returns>
        private async Task<bool> OpenVoicePlayer()
        {
            await AnimateFormHeightAsync(_VOICEPLAYERHEIGHT, expand: true);

            showAudioplayerToolStripMenuItem.Text = "Close the audio player";
            showAudioplayerToolStripMenuItem.ToolTipText = "Close the voice player.";

            // Reposition and show labels
            MoveLabels(lbl_ReadInfo, 12, 572);
            MoveLabels(lbl_byTeam, 643, 572);

            groupBox_VoicePlayer.Visible = true;
            groupBox_VoicePlayer.Enabled = true;

            return true;
        }


        /// <summary>
        /// Animates the collapse of the main form to hide the audio player.
        /// Updates the menu item text and tooltip accordingly.
        /// Restores label positions after animation.
        /// </summary>
        /// <returns>False to indicate the audio player is now closed.</returns>
        private async Task<bool> CloseVoicePlayer()
        {
            await AnimateFormHeightAsync(_WINDOWHEIGHT, expand: false);

            showAudioplayerToolStripMenuItem.Text = "Show the audio player";
            showAudioplayerToolStripMenuItem.ToolTipText = "Opens a voice player with more information.";

            // Reposition and show labels
            MoveLabels(lbl_ReadInfo, 12, 372);
            MoveLabels(lbl_byTeam, 643, 372);

            return false;
        }


        /// <summary>
        /// Repositions a label to the specified (x, y) coordinates
        /// and ensures both controlled labels are made visible again.
        /// </summary>
        /// <param name="label">The label to move.</param>
        /// <param name="x">New X coordinate.</param>
        /// <param name="y">New Y coordinate.</param>
        private void MoveLabels(Label label, int x = 0, int y = 0)
        {
            label.Location = new Point(x, y);

            // Ensure both labels are shown
            lbl_ReadInfo.Visible = true;
            lbl_byTeam.Visible = true;
        }


        /// <summary>
        /// Loads all player folders found under the demo's Audio directory.
        /// Updates the ListBox with player Steam IDs and their display names (if available).
        /// </summary>
        private void LoadPlayerFolders()
        {
            string? appPath = Application.StartupPath;
            string? demoPath = tb_demoFilePath.Text;
            string? demoName = demoPath != null ? Path.GetFileNameWithoutExtension(demoPath) : null;
            _audioFolderPath = demoName != null ? Path.Combine(appPath, "Audio", demoName) : null;

            if (!string.IsNullOrEmpty(_audioFolderPath) && Directory.Exists(_audioFolderPath))
            {
                var directories = Directory.GetDirectories(_audioFolderPath)
                                            .Select(Path.GetFileName)
                                            .Where(name => !string.IsNullOrWhiteSpace(name))
                                            .ToArray();

                var displayItems = directories
                    .Select(dir => new ListBoxItems
                    {
                        SteamId = dir,
                        DisplayName = _snapshot?.FirstOrDefault(p => p.PlayerSteamID?.ToString() == dir)?.PlayerName ?? dir
                    })
                    .ToList();

                listBox_VoicePlayer.Items.Clear();

                foreach (var item in displayItems)
                {
                    listBox_VoicePlayer.Items.Add(item);
                }

                listBox_VoicePlayer.Enabled = true;
            }
            else
            {
                listBox_VoicePlayer.Enabled = false;
                MessageBox.Show("The 'Audio' folder could not be found.\nTry running ‘Extract voice from demo’!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Configures the appearance and behavior of the DataGridView displaying voice entries.
        /// Disables sorting and ensures consistent styling.
        /// </summary>
        private void ConfigurePlayerDataGrid(DataGridView dgv)
        {
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.RowHeadersVisible = false;
            dgv.ClearSelection();
            dgv.EnableHeadersVisualStyles = false;

            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = dgv.ColumnHeadersDefaultCellStyle.BackColor;
            dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = dgv.ColumnHeadersDefaultCellStyle.ForeColor;

            foreach (DataGridViewColumn column in dgv.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
        }

        /// <summary>
        /// Loads all audio entries for the selected Steam ID.
        /// Populates the DataGridView with formatted round, time, and duration data.
        /// </summary>
        public void LoadVoiceData(string steamID)
        {
            dGv_VoicePlayer.Columns.Clear();
            dGv_VoicePlayer.Rows.Clear();

            dGv_VoicePlayer.Columns.Add("Round", "Round");
            dGv_VoicePlayer.Columns.Add("Time", "Time");
            dGv_VoicePlayer.Columns.Add("Duration", "Duration");

            _audioEntries = AudioReadHelper.GetAudioEntries(steamID, _audioFolderPath ?? "");

            var sorted = _audioEntries.OrderBy(x => x.Round).ToList();
            _audioEntries = sorted;

            foreach (var entry in sorted)
            {
                dGv_VoicePlayer.Rows.Add(
                    $"Round {entry.Round}",
                    entry.Time.ToString(@"hh\:mm\:ss"),
                    $"{entry.DurationSeconds:F1} s"
                );
            }

            ConfigurePlayerDataGrid(dGv_VoicePlayer);
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
            _ = VersionChecker.IsNewerVersionAvailable(_GUIVERSIONNR);
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

        /// <summary>
        /// Handles the click event for the "Extract Audios From Demo" menu item.
        /// Initiates asynchronous audio extraction from a .dem file while updating
        /// the progress bar and status label without freezing the UI.
        /// </summary>
        private async void extractAudiosFromDemoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            progressBar.Value = 0;
            extractAudiosFromDemoToolStripMenuItem.Enabled = false;

            var progress = new Progress<float>(value =>
            {
                // Update progress bar value in real-time
                progressBar.Value = Math.Min(100, (int)(value * 100));
            });

            lbl_progressBarText.Text = "Extract the voice audio files";

            // Run extraction in background thread to prevent UI freeze
            bool result = await Task.Run(() => AudioExtractor.ExtractAsync(tb_demoFilePath.Text, progress));

            extractAudiosFromDemoToolStripMenuItem.Enabled = true;

            if (result)
            {
                lbl_progressBarText.Text = "Extraction complete!";
                LoadPlayerFolders();
            }
            else
                lbl_progressBarText.Text = "Error during extraction.";

        }


        /// <summary>
        /// Handles the click event of the "Show Audio Player" menu item.
        /// Expands or collapses the main form based on the current state of the audio player.
        /// </summary>
        private async void showAudioplayerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Disable the extractor menu item during animation
            extractorToolStripMenuItem.Enabled = false;

            if (!_voicePlayerOpen)
            {
                _voicePlayerOpen = await OpenVoicePlayer(); // Expand the form                                                            
                this.Height = _VOICEPLAYERHEIGHT; // Fallback to ensure target height is correctly set
                LoadPlayerFolders();
            }
            else if (_voicePlayerOpen)
            {
                _voicePlayerOpen = await CloseVoicePlayer(); // Collapse the form                                                             
                this.Height = _WINDOWHEIGHT; // Fallback to ensure target height is correctly set
            }

            // Re-enable the extractor menu item
            extractorToolStripMenuItem.Enabled = true;
        }


        /// <summary>
        /// Triggered when the selected item in the ListBox changes.
        /// Attempts to locate and load voice data from the appropriate folder based on the selected Steam ID.
        /// </summary>
        private void listBox_VoicePlayer_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox_VoicePlayer.SelectedItem is ListBoxItems selectedItem)
            {
                string? demoPath = tb_demoFilePath.Text;
                string? demoName = demoPath != null ? Path.GetFileNameWithoutExtension(demoPath) : null;
                string folderPath = demoName != null
                    ? Path.Combine(Application.StartupPath, "Audio", demoName, selectedItem.SteamId)
                    : string.Empty;

                if (Directory.Exists(folderPath))
                {
                    LoadVoiceData(selectedItem.SteamId ?? "00");
                }
                else
                {
                    MessageBox.Show($"The folder '{selectedItem.SteamId}' could not be found in '{demoName}'.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }


        /// <summary>
        /// Handles double-click events on a DataGridView cell.
        /// Plays the audio entry associated with the clicked row.
        /// </summary>
        private void dGv_VoicePlayer_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < _audioEntries.Count)
            {
                var entry = _audioEntries[e.RowIndex];
                AudioReadHelper.PlayAudio(entry);
            }
        }
    }
}
