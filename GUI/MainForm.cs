using CS2SourceTVDemoVoiceCalc.AudioExtract;
using CS2SourceTVDemoVoiceCalc.HelperClass;
using CS2SourceTVDemoVoiceCalc.UtilClass;
using DemoFile;
using DemoFile.Game.Cs;
using System.Data;
using System.Windows.Automation.Provider;

namespace CS2SourceTVDemoVoiceCalc.GUI
{
    /// <summary>
    /// Main window of the application. Controls the UI, demo parsing, and bitfield calculation.
    /// </summary>
    public partial class MainForm : Form
    {
        // ==== Fields ====

        // Parser and player snapshots
        private CsDemoParser? _demo = null;
        private PlayerSnapshot[]? _snapshot = null;

        // Bitfields for voice indices
        private int _teamAP1, _teamAP2, _teamAP3, _teamAP4, _teamAP5;
        private int _teamBP1, _teamBP2, _teamBP3, _teamBP4, _teamBP5;

        // Checkbox groups for both teams
        private readonly List<CheckBox> _teamACheckboxes = new();
        private readonly List<CheckBox> _teamBCheckboxes = new();

        // Tooltip for copy button
        private readonly ToolTip _copyToolTip = new ToolTip();

        // Recursion protection for "Select All"
        private bool _isSyncingSelectAll = false;

        // Paths for demo and audio folders
        private string? _csDemoFolderPath = null;
        private string? _audioFolderPath = null;

        // Hashes for file comparison
        private byte[]? _sourceHash = null;
        private byte[]? _destinationHash = null;

        // Static links to player profile pages
        private static string _steamProfileLink = "http://steamcommunity.com/profiles/";
        private static string _cswatchProfileLink = "https://cswatch.in/player/";
        private static string _csStatsProfileLink = "https://csstats.gg/player/";
        private static string _leetifyProfileLink = "https://leetify.com/app/profile/";

        // Other Globals
        private string _mapName = "no Mapname!";
        private string _duration = "00:00:00";
        private string _hostName = "No Hostname";
        private bool _voicePlayerOpen = false;
        private int _windowHeight = 400;
        private int _voicePlayerHeight = 585;
        private List<AudioEntry> _audioEntries;
        private float _fontScaleFactor = 1f;

        // GUI version
        private const string _GUIVERSIONNR = GlobalVersionInfo.GUI_VERSION;

        // ==== Constructor ====

        /// <summary>
        /// Initializes the main window and checkbox groups.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
            this.Text = "CS2 SourceTV Demo Voice Calculator v." + _GUIVERSIONNR;
            groupBox_VoicePlayer.Text = " Voice-Player\u00A0";
            InitializeCheckboxGroup();
            InitializeEventHandlers();
            AddShellContextMenu.ValidateShellIntegration();

            ApplyGuiScaling(); // <-- new method for responsive UI
        }

        /// <summary>
        /// Applies GUI scaling based on the current screen resolution.
        /// Only scales if the resolution is higher than the base resolution (1920x1080).
        /// </summary>
        private void ApplyGuiScaling()
        {
            // simulate 2560x1440
            //float baseWidth = 1440f;
            //float baseHeight = 810f;
            // or for 4K:
            //float baseWidth = 960f;
            //float baseHeight = 540f;


            // Define the base resolution for which the GUI was originally designed
            float baseWidth = 1920f;
            float baseHeight = 1080f;

            // Get the current screen resolution
            float screenWidth = Screen.PrimaryScreen.Bounds.Width;
            float screenHeight = Screen.PrimaryScreen.Bounds.Height;

            // Factor for font scaling based on resolution
            if (screenHeight >= 1440f && screenHeight < 2160f)
                _fontScaleFactor = 0.5f;
            else if (screenHeight >= 2160f)
                _fontScaleFactor = 0.25f;
            else
                _fontScaleFactor = 1f;


            // Skip scaling if screen resolution is less than or equal to base
            if (screenWidth <= baseWidth || screenHeight <= baseHeight)
                return;

            // Calculate scaling factors for width and height
            float scaleX = screenWidth / baseWidth;
            float scaleY = screenHeight / baseHeight;

            // Use the smaller factor to maintain aspect ratio (proportional scaling)
            float scale = Math.Min(scaleX, scaleY);

            // Scale all controls recursively
            ScaleControls(this, scale);

            // Scale form width and height
            this.Width = (int)(this.Width * scale);

            // Update stored form height values for animations or layout
            _windowHeight = (int)(this.Height * scale);
            _voicePlayerHeight = (int)(_voicePlayerHeight * scale);
            this.Height = _windowHeight;
        }

        /// <summary>
        /// Recursively scales all child controls in a form, including font sizes and special cases like DataGridView and ListBox.
        /// </summary>
        /// <param name="parent">The parent control containing children to scale.</param>
        /// <param name="scale">The scaling factor based on screen resolution.</param>
        private void ScaleControls(Control parent, float scale)
        {
            foreach (Control control in parent.Controls)
            {
                // Scale position and size
                control.Left = (int)(control.Left * scale);
                control.Top = (int)(control.Top * scale);
                control.Width = (int)(control.Width * scale);
                control.Height = (int)(control.Height * scale);

                // Default font scaling
                control.Font = new Font(control.Font.FontFamily, control.Font.Size * scale, control.Font.Style);

                // Handle DataGridView separately
                if (control is DataGridView dgv)
                {
                    // Scale row height
                    dgv.RowTemplate.Height = (int)(dgv.RowTemplate.Height * scale);

                    // Apply smaller font specifically for dGv_VoicePlayer
                    if (control.Name == "dGv_VoicePlayer")
                    {
                        float fontScale = scale * _fontScaleFactor;
                        dgv.Font = new Font(dgv.Font.FontFamily, dgv.Font.Size * fontScale, dgv.Font.Style);
                    }
                }
                // Handle ListBox separately
                else if (control is ListBox lb)
                {
                    // Scale item height normally
                    lb.ItemHeight = (int)(lb.ItemHeight * scale);

                    // Adjust overall ListBox height to always show 5 visible items
                    int borderPadding = lb.Height - (lb.ItemHeight * 5);
                    lb.Height = (lb.ItemHeight * 5) + borderPadding;

                    // Scale font smaller than the global scale
                    float fontScale = scale * _fontScaleFactor;
                    lb.Font = new Font(lb.Font.FontFamily, lb.Font.Size * fontScale, lb.Font.Style);
                }

                // Recursively scale nested child controls
                if (control.Controls.Count > 0)
                {
                    ScaleControls(control, scale);
                }
            }
        }

        // ==== Demo Handling ====

        /// <summary>
        /// Sets the demo file on startup if the file exists and is a .dem file.
        /// </summary>
        public void SetDemoFileOnStartup(string filePath)
        {
            if (!File.Exists(filePath) || Path.GetExtension(filePath).ToLower() != ".dem") return;
            PrepareStart(filePath);
        }

        /// <summary>
        /// Prepares the UI and state for loading a new demo file.
        /// </summary>
        private async void PrepareStart(string demoPath)
        {
            DisableAll();
            RemoveAudioFolder();
            _voicePlayerOpen = await CloseVoicePlayer();
            listBox_VoicePlayer.Items.Clear();
            dGv_VoicePlayer.Columns.Clear();

            btn_MoveToCSFolder.Enabled = false;
            btn_CopyToClipboard.Enabled = false;
            extractorToolStripMenuItem.Enabled = false;

            tb_ConsoleCommand.ForeColor = Color.Black;
            tb_ConsoleCommand.Text = "Select one or more players you would like to hear in the demo...";

            toolStripStatusLabel_progressBarText.Text = "";
            toolStripProgressBar.Value = 0;

            tb_demoFilePath.Text = demoPath;
            toolStripStatusLabel_StatusText.ForeColor = Color.Red;
            toolStripStatusLabel_StatusText.Text = "Read demo file";

            ReadDemoFile(demoPath);
        }

        /// <summary>
        /// Reads the demo file and extracts player snapshots and metadata.
        /// </summary>
        private async void ReadDemoFile(string demoPath)
        {
            _demo = new CsDemoParser();
            _snapshot = null;
            var tcs = new TaskCompletionSource<bool>();
            bool done = false;
            const int neededPerTeam = 5;
            var collected = new List<PlayerSnapshot>();

            // Collect player data on round start
            _demo.Source1GameEvents.RoundStart += (Source1RoundStartEvent e) =>
            {
                if (done) return;
                foreach (var p in _demo.Players)
                {
                    if (string.IsNullOrWhiteSpace(p.PlayerName)) continue;
                    int team = (int)p.Team.TeamNum;
                    if (team != 2 && team != 3) continue;
                    if (collected.Any(x => x.UserId == (int)p.EntityIndex.Value)) continue;
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
                using var stream = new FileStream(demoPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096 * 1024);
                var reader = DemoFileReader.Create(_demo, stream);
                var readTask = reader.ReadAllAsync().AsTask();
                await Task.WhenAny(readTask, tcs.Task);
                await readTask;
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

            LoadCTTDataGrid();
        }

        // ==== DataGrid and Context Menu ====

        /// <summary>
        /// Configures the appearance and behavior of a DataGridView.
        /// </summary>
        private void ConfigureDataGrid(DataGridView dgv)
        {
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            dgv.RowHeadersVisible = false;
            dgv.ClearSelection();
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = dgv.ColumnHeadersDefaultCellStyle.BackColor;
            dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = dgv.ColumnHeadersDefaultCellStyle.ForeColor;

            var specIdColumn = dgv.Columns["Spec ID"];
            specIdColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            specIdColumn.Width++;

            dgv.Columns["Players"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            foreach (DataGridViewColumn column in dgv.Columns)
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
        }

        /// <summary>
        /// Configures the context menu for a DataGridView with player actions.
        /// </summary>
        private void ConfigureContextMenu(DataGridView dgv, List<PlayerSnapshot> playerList)
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
                ("Open Steam Profile", _steamProfileLink, Properties.Resources.iconSteam),
                ("Open cswatch.in Profile", _cswatchProfileLink, Properties.Resources.iconCsWatch),
                ("Open leetify.com Profile", _leetifyProfileLink, Properties.Resources.iconLeetify),
                ("Open csstats.gg Profile", _csStatsProfileLink, Properties.Resources.iconCsStats)
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
        /// Loads player data into the CT and T DataGrids.
        /// </summary>
        private void LoadCTTDataGrid()
        {
            if (_snapshot == null)
            {
                MessageBox.Show("Error reading demo file!");
                return;
            }

            var ctPlayers = _snapshot.Where(p => p.TeamNumber == 3).OrderBy(p => p.UserId).ToList();
            var tPlayers = _snapshot.Where(p => p.TeamNumber == 2).OrderBy(p => p.UserId).ToList();

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

            toolStripStatusLabel_StatusText.ForeColor = Color.DarkGreen;

            if (_hostName.Contains("SourceTV"))
            {
                toolStripStatusLabel_StatusText.Text = "File loaded with audio stream";
                btn_CopyToClipboard.Enabled = true;
                extractorToolStripMenuItem.Enabled = true;
                ResetAll();
            }
            else
            {
                toolStripStatusLabel_StatusText.Text = "File loaded, no audio stream!";
                tb_ConsoleCommand.ForeColor = Color.DarkRed;
                tb_ConsoleCommand.Text = "The loaded demo comes from competitive mode and has no audio...";

                btn_CopyToClipboard.Enabled = false;
                extractorToolStripMenuItem.Enabled = false;
                DisableAll();
            }

            btn_MoveToCSFolder.Enabled = true;
        }

        /// <summary>
        /// Creates a DataTable from a list of players for DataGridView binding.
        /// </summary>
        private DataTable CreateDataTable(List<PlayerSnapshot> players, out string teamName)
        {
            var table = new DataTable();
            table.Columns.Add("Spec ID", typeof(int));
            table.Columns.Add("Players", typeof(string));

            teamName = players.FirstOrDefault()?.TeamName ?? "Unknown";

            foreach (var p in players)
                table.Rows.Add(p.UserId, p.PlayerName);

            return table;
        }

        // ==== Checkbox Handling ====

        /// <summary>
        /// Initializes the checkbox groups for both teams.
        /// </summary>
        private void InitializeCheckboxGroup()
        {
            _teamACheckboxes.AddRange(new[] { cb_TeamAP1, cb_TeamAP2, cb_TeamAP3, cb_TeamAP4, cb_TeamAP5 });
            _teamBCheckboxes.AddRange(new[] { cb_TeamBP1, cb_TeamBP2, cb_TeamBP3, cb_TeamBP4, cb_TeamBP5 });
        }

        /// <summary>
        /// Sets up event handlers for checkboxes and buttons.
        /// </summary>
        private void InitializeEventHandlers()
        {
            for (int i = 0; i < _teamACheckboxes.Count; i++)
            {
                int index = i;
                _teamACheckboxes[i].CheckedChanged += (s, e) =>
                {
                    UpdateBitField((CheckBox)s, dGv_CT.Rows[index], ref GetTeamAFieldByIndex(index));
                    SyncSelectAllCheckbox(_teamACheckboxes, cb_AllTeamA);
                };
            }
            for (int i = 0; i < _teamBCheckboxes.Count; i++)
            {
                int index = i;
                _teamBCheckboxes[i].CheckedChanged += (s, e) =>
                {
                    UpdateBitField((CheckBox)s, dGv_T.Rows[index], ref GetTeamBFieldByIndex(index));
                    SyncSelectAllCheckbox(_teamBCheckboxes, cb_AllTeamB);
                };
            }
            cb_AllTeamA.CheckStateChanged += CB_AllTeamA_CheckStateChanged;
            cb_AllTeamB.CheckStateChanged += CB_AllTeamB_CheckStateChanged;

            btn_CopyToClipboard.Click += (s, e) =>
            {
                Clipboard.SetText(tb_ConsoleCommand.Text);
                ShowCopyTooltip();
                this.ActiveControl = null;
            };
            btn_MoveToCSFolder.Click += (s, e) => { MoveToCSFolder(); };
        }

        /// <summary>
        /// Handles the "Select All" checkbox for Team A.
        /// </summary>
        private void CB_AllTeamA_CheckStateChanged(object sender, EventArgs e)
        {
            if (_isSyncingSelectAll) return;
            ToggleCheckboxes(_teamACheckboxes, cb_AllTeamA.Checked);
        }

        /// <summary>
        /// Handles the "Select All" checkbox for Team B.
        /// </summary>
        private void CB_AllTeamB_CheckStateChanged(object sender, EventArgs e)
        {
            if (_isSyncingSelectAll) return;
            ToggleCheckboxes(_teamBCheckboxes, cb_AllTeamB.Checked);
        }

        /// <summary>
        /// Synchronizes the "Select All" checkbox state with the group.
        /// </summary>
        private void SyncSelectAllCheckbox(List<CheckBox> groupCheckboxes, CheckBox selectAllCheckbox)
        {
            _isSyncingSelectAll = true;
            selectAllCheckbox.Checked = groupCheckboxes.All(cb => cb.Checked);
            _isSyncingSelectAll = false;
        }

        /// <summary>
        /// Returns a reference to the bitfield for Team A by index.
        /// </summary>
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
        /// Returns a reference to the bitfield for Team B by index.
        /// </summary>
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

        /// <summary>
        /// Toggles all checkboxes in a group.
        /// </summary>
        private void ToggleCheckboxes(List<CheckBox> checkboxes, bool isChecked)
        {
            foreach (var cb in checkboxes)
                cb.Checked = isChecked;
        }

        /// <summary>
        /// Shows a tooltip when copying to clipboard.
        /// </summary>
        private void ShowCopyTooltip()
        {
            _copyToolTip.AutomaticDelay = 0;
            _copyToolTip.AutoPopDelay = 1000;
            _copyToolTip.InitialDelay = 0;
            _copyToolTip.ReshowDelay = 0;
            _copyToolTip.ShowAlways = true;
            var offset = new Point(btn_CopyToClipboard.Width / 2, -btn_CopyToClipboard.Height / 2);
            _copyToolTip.Show("Copied!", btn_CopyToClipboard, offset, 1000);
        }

        /// <summary>
        /// Resets all checkboxes to default state.
        /// </summary>
        private void ResetAll()
        {
            ResetCheckboxes(_teamACheckboxes);
            ResetCheckboxes(_teamBCheckboxes);
            ResetCheckboxes(new List<CheckBox> { cb_AllTeamA, cb_AllTeamB });
        }

        /// <summary>
        /// Disables all checkboxes.
        /// </summary>
        private void DisableAll()
        {
            DisableCheckboxes(_teamACheckboxes);
            DisableCheckboxes(_teamBCheckboxes);
            DisableCheckboxes(new List<CheckBox> { cb_AllTeamA, cb_AllTeamB });
        }

        /// <summary>
        /// Resets a list of checkboxes.
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
        /// Disables a list of checkboxes.
        /// </summary>
        private void DisableCheckboxes(List<CheckBox> checkboxes)
        {
            foreach (var cb in checkboxes)
            {
                cb.Enabled = false;
                cb.Checked = false;
            }
        }

        // ==== Bitfield Calculation ====

        /// <summary>
        /// Updates the bitfield for a player based on checkbox state.
        /// </summary>
        private void UpdateBitField(CheckBox checkbox, DataGridViewRow row, ref int field)
        {
            if (row?.Cells[0].Value == null)
            {
                field = 0;
                return;
            }
            field = checkbox.Checked ? GetPlayBitField(row.Cells[0].Value) : 0;
            ChangeConsoleCommand();
        }

        /// <summary>
        /// Calculates the bitfield value for a given player spec ID.
        /// </summary>
        private int GetPlayBitField(object cellValue)
        {
            if (cellValue == null || !int.TryParse(cellValue.ToString(), out int specPlayerId))
                throw new ArgumentException("Invalid spec ID in the cell.");
            if (specPlayerId < 1 || specPlayerId > 20)
                throw new ArgumentOutOfRangeException(nameof(specPlayerId), "Spec-ID must be between 1 and 20.");
            return 1 << (specPlayerId - 1);
        }

        /// <summary>
        /// Updates the console command textbox with the current bitfield.
        /// </summary>
        public void ChangeConsoleCommand()
        {
            int voiceBitField = _teamAP1 + _teamAP2 + _teamAP3 + _teamAP4 + _teamAP5
                + _teamBP1 + _teamBP2 + _teamBP3 + _teamBP4 + _teamBP5;
            tb_ConsoleCommand.Text =
                $"tv_listen_voice_indices {voiceBitField}; tv_listen_voice_indices_h {voiceBitField}";
        }

        // ==== File and Folder Operations ====

        /// <summary>
        /// Moves the demo file to the CS folder and verifies the file hash.
        /// </summary>
        private void MoveToCSFolder()
        {
            string? movedFullFilePath = null;
            string? sourceFile = null;
            _sourceHash = null; _destinationHash = null;

            if (string.IsNullOrWhiteSpace(_csDemoFolderPath) || !Directory.Exists(_csDemoFolderPath))
            {
                _csDemoFolderPath = CS2PathConfig.GetPath();
                if (!Directory.Exists(_csDemoFolderPath))
                {
                    CS2PathConfig.EnsurePathConfigured();
                    _csDemoFolderPath = CS2PathConfig.GetPath();
                }
            }

            if (!string.IsNullOrWhiteSpace(_csDemoFolderPath) && Directory.Exists(_csDemoFolderPath))
            {
                sourceFile = tb_demoFilePath.Text;
                if (!string.IsNullOrWhiteSpace(sourceFile))
                {
                    try
                    {
                        _sourceHash = sourceFile.ComputeFileHash();
                        movedFullFilePath = sourceFile.MoveAndRenameFile(_csDemoFolderPath);
                    }
                    catch
                    {
                        movedFullFilePath = null;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(movedFullFilePath))
            {
                _destinationHash = movedFullFilePath.ComputeFileHash();
                if (_sourceHash != null && _destinationHash != null)
                {
                    if (_sourceHash.HashesAreEqual(_destinationHash))
                    {
                        tb_demoFilePath.Text = movedFullFilePath;
                        ReadDemoFile(movedFullFilePath);
                        MessageBox.Show("File was moved successfully.", "All fine", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Error while moving the file!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("File move was canceled or failed.", "Operation Canceled", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Removes the Audio folder if it exists.
        /// </summary>
        private void RemoveAudioFolder()
        {
            try
            {
                string audioFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Audio");
                if (Directory.Exists(audioFolderPath))
                {
                    Directory.Delete(audioFolderPath, true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error deleting the folder: " + ex.Message);
            }
        }

        // ==== Helper Functions ====

        /// <summary>
        /// Converts demo ticks to a time string (hh:mm:ss).
        /// </summary>
        public static string TicksToTimeString(int ticks, double tickRate = 64.0)
        {
            if (tickRate <= 0)
                throw new ArgumentException("Tick rate must be greater than 0.");
            double totalSeconds = ticks / tickRate;
            TimeSpan duration = TimeSpan.FromSeconds(totalSeconds);
            return duration.ToString(@"hh\:mm\:ss");
        }

        // ==== UI Animations ====

        /// <summary>
        /// Animates the form height for expanding or collapsing the audio player.
        /// </summary>
        private async Task AnimateFormHeightAsync(int targetHeight, bool expand)
        {
            int step = 10;
            int distance = Math.Abs(this.Height - targetHeight);
            int steps = distance / step;
            int remainder = distance % step;
            int direction = expand ? 1 : -1;

            this.MinimizeBox = false;
            this.Enabled = false;

            groupBox_VoicePlayer.Visible = false;
            groupBox_VoicePlayer.Enabled = false;

            for (int i = 0; i < steps; i++)
            {
                this.Height += step * direction;
                int delay = (int)(4 + 8.0 * i / steps);
                await Task.Delay(delay);
            }
            if (remainder > 0)
            {
                this.Height += remainder * direction;
            }

            this.MinimizeBox = true;
            this.Enabled = true;
        }

        /// <summary>
        /// Expands the form to show the voice player.
        /// </summary>
        private async Task<bool> OpenVoicePlayer()
        {
            await AnimateFormHeightAsync(_voicePlayerHeight, expand: true);
            showAudioplayerToolStripMenuItem.Text = "Close the audio player";
            showAudioplayerToolStripMenuItem.ToolTipText = "Close the voice player.";
            groupBox_VoicePlayer.Visible = true;
            groupBox_VoicePlayer.Enabled = true;
            return true;
        }

        /// <summary>
        /// Collapses the form to hide the voice player.
        /// </summary>
        private async Task<bool> CloseVoicePlayer()
        {
            await AnimateFormHeightAsync(_windowHeight, expand: false);
            showAudioplayerToolStripMenuItem.Text = "Show the audio player";
            showAudioplayerToolStripMenuItem.ToolTipText = "Opens a voice player with more information.";
            return false;
        }

        // ==== Audio Player ====

        /// <summary>
        /// Loads available player folders for the audio player.
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
                    listBox_VoicePlayer.Items.Add(item);

                listBox_VoicePlayer.Enabled = true;
                dGv_VoicePlayer.ClearSelection();
            }
            else
            {
                listBox_VoicePlayer.Enabled = false;
                MessageBox.Show("The 'Audio' folder could not be found.\nTry running ‘Extract voice from demo’!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Configures the DataGridView for displaying voice data.
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
        /// Loads voice data for a specific player by SteamID.
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

        // ==== Event Handlers for Menus and UI ====

        /// <summary>
        /// Adds the shell context menu integration.
        /// </summary>
        private void addToShellContextMenuToolStripMenuItem_Click(object sender, EventArgs e) => AddShellContextMenu.AddShellIntegration();

        /// <summary>
        /// Removes the shell context menu integration.
        /// </summary>
        private void removeFromShellContextMenuToolStripMenuItem_Click(object sender, EventArgs e) => AddShellContextMenu.RemoveShellIntegration();

        /// <summary>
        /// Opens the About form.
        /// </summary>
        private void aboutToolStripMenuItem1_Click(object sender, EventArgs e) => OpenForm<About>();

        /// <summary>
        /// Checks for updates.
        /// </summary>
        private void checkForUpdatesToolStripMenuItem_Click(object sender, EventArgs e) => _ = VersionChecker.IsNewerVersionAvailable(_GUIVERSIONNR);

        /// <summary>
        /// Changes the demo folder path.
        /// </summary>
        private void changeDemoFolderPathToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CS2PathConfig.EnsurePathConfigured();
            _csDemoFolderPath = CS2PathConfig.GetPath();
        }

        /// <summary>
        /// Extracts audio from the demo file.
        /// </summary>
        private async void extractAudiosFromDemoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStripProgressBar.Value = 0;
            extractAudiosFromDemoToolStripMenuItem.Enabled = false;

            var progress = new Progress<float>(value =>
            {
                toolStripProgressBar.Value = Math.Min(100, (int)(value * 100));
            });

            toolStripStatusLabel_progressBarText.Text = "Extract the voice audio files";
            bool result = await Task.Run(() => AudioExtractor.ExtractAsync(tb_demoFilePath.Text, progress));
            extractAudiosFromDemoToolStripMenuItem.Enabled = true;

            if (result)
            {
                toolStripStatusLabel_progressBarText.Text = "Extraction complete!";
                LoadPlayerFolders();
            }
            else
                toolStripStatusLabel_progressBarText.Text = "Error during extraction.";
        }

        /// <summary>
        /// Shows or hides the audio player.
        /// </summary>
        private async void showAudioplayerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            extractorToolStripMenuItem.Enabled = false;
            if (!_voicePlayerOpen)
            {
                _voicePlayerOpen = await OpenVoicePlayer();
                this.Height = _voicePlayerHeight;
                LoadPlayerFolders();
            }
            else if (_voicePlayerOpen)
            {
                _voicePlayerOpen = await CloseVoicePlayer();
                this.Height = _windowHeight;
            }
            extractorToolStripMenuItem.Enabled = true;
        }

        /// <summary>
        /// Handles selection change in the voice player list box.
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
        /// Handles double click on a voice entry to play audio.
        /// </summary>
        private void dGv_VoicePlayer_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < _audioEntries.Count)
            {
                var entry = _audioEntries[e.RowIndex];
                AudioReadHelper.PlayAudio(entry);
            }
        }

        // ==== Drag & Drop ====

        /// <summary>
        /// Handles drag enter event for the demo file path textbox.
        /// </summary>
        private void TB_demoFilePath_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }

        /// <summary>
        /// Handles drag drop event for the demo file path textbox.
        /// </summary>
        private void TB_demoFilePath_DragDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            foreach (string file in files)
            {
                if (Path.GetExtension(file).Equals(".dem", StringComparison.OrdinalIgnoreCase))
                {
                    PrepareStart(file);
                    return;
                }
            }
            MessageBox.Show(
                "Please only drop files with the extension .dem.",
                "Invalid format",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        // ==== DataGridView MouseDown ====

        /// <summary>
        /// Handles mouse down event for the CT DataGridView.
        /// </summary>
        private void dGv_CT_MouseDown(object sender, MouseEventArgs e)
        {
            dGv_T.ClearSelection();
            dGv_T.CurrentCell = null;
        }

        /// <summary>
        /// Handles mouse down event for the T DataGridView.
        /// </summary>
        private void dGv_T_MouseDown(object sender, MouseEventArgs e)
        {
            dGv_CT.ClearSelection();
            dGv_CT.CurrentCell = null;
        }

        // ==== Helper Functions for Form Opening ====

        /// <summary>
        /// Opens a new form of the specified type, optionally modal.
        /// </summary>
        private void OpenForm<T>(bool modal = true) where T : Form, new()
        {
            var frm = new T();
            if (modal)
                frm.ShowDialog();
            else
                frm.Show();
        }
    }
}