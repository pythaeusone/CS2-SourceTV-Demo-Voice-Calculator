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
            public int UserId { get; set; }
            public string? PlayerName { get; set; }
            public int TeamNumber { get; set; }
            public string? TeamName { get; set; }
        }

        // =====================
        // Parser and snapshots
        // =====================
        private CsDemoParser? demo = null;
        private PlayerSnapshot[]? snapshot = null;

        // =============================
        // Bitfields for voice indices
        // =============================
        private int teamAP1, teamAP2, teamAP3, teamAP4, teamAP5;
        private int teamBP1, teamBP2, teamBP3, teamBP4, teamBP5;

        // ===================================
        // Checkbox groups for easy toggling
        // ===================================
        private readonly List<CheckBox> teamACheckboxes = new();
        private readonly List<CheckBox> teamBCheckboxes = new();

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
            InitializeCheckboxHandlers();
        }

        /// <summary>
        /// Opens a form of the given type, either modal or non-modal.
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

        // ==================
        // Menu event handlers
        // ==================
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
                "Please only save files with the extension .dem.",
                "Invalid format",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        // =====================================
        // Demo file parsing and snapshot load
        // =====================================
        /// <summary>
        /// Reads the demo file asynchronously and captures player info at first round start.
        /// </summary>
        private async void readDemoFile(string demoPath)
        {
            demo = new CsDemoParser();
            snapshot = null;

            var tcs = new TaskCompletionSource<bool>();

            // Handler to capture player snapshots on round start
            void OnRoundStart(Source1RoundStartEvent e)
            {
                if (snapshot == null)
                {
                    snapshot = demo.Players
                        .Where(p => !string.IsNullOrWhiteSpace(p.PlayerName))
                        .Select(p => new PlayerSnapshot
                        {
                            UserId = p.PlayerInfo.Userid + 1,
                            PlayerName = p.PlayerName,
                            TeamNumber = (int)p.Team.TeamNum,
                            TeamName = p.Team.ClanTeamname
                        })
                        .ToArray();

                    demo.Source1GameEvents.RoundStart -= OnRoundStart;
                    tcs.TrySetResult(true);
                }
            }

            demo.Source1GameEvents.RoundStart += OnRoundStart;

            try
            {
                using var stream = new FileStream(
                    demoPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 4096 * 1024);

                var reader = DemoFileReader.Create(demo, stream);
                var readTask = reader.ReadAllAsync().AsTask();

                // Wait for either round start or end of file
                await Task.WhenAny(readTask, tcs.Task);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading the demo: {ex.Message}");
                return;
            }

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

            var ctPlayers = snapshot.Where(p => p.TeamNumber == 3).OrderBy(p => p.UserId).ToList();
            var tPlayers = snapshot.Where(p => p.TeamNumber == 2).OrderBy(p => p.UserId).ToList();

            dGv_CT.DataSource = CreateDataTable(ctPlayers, out var teamAName);
            dGv_T.DataSource = CreateDataTable(tPlayers, out var teamBName);

            lbl_TeamA.Text = teamAName;
            lbl_TeamB.Text = teamBName;

            ConfigureDataGrid(dGv_CT);
            ConfigureDataGrid(dGv_T);

            lbl_ReadInfo.ForeColor = Color.DarkGreen;
            lbl_ReadInfo.Text = "File loaded";

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

        // =====================================
        // Checkbox group initialization
        // =====================================
        /// <summary>
        /// Assigns checkboxes to their respective team lists for batch operations.
        /// </summary>
        private void InitializeCheckboxGroup()
        {
            teamACheckboxes.AddRange(new[] { cb_TeamAP1, cb_TeamAP2, cb_TeamAP3, cb_TeamAP4, cb_TeamAP5 });
            teamBCheckboxes.AddRange(new[] { cb_TeamBP1, cb_TeamBP2, cb_TeamBP3, cb_TeamBP4, cb_TeamBP5 });
        }

        /// <summary>
        /// Sets up individual checkbox change handlers to update voice bitfields.
        /// </summary>
        private void InitializeCheckboxHandlers()
        {
            // Team A (CT) checkboxes
            cb_TeamAP1.CheckedChanged += (s, e) => UpdateBitField((CheckBox)s, dGv_CT.Rows[0], ref teamAP1);
            cb_TeamAP2.CheckedChanged += (s, e) => UpdateBitField((CheckBox)s, dGv_CT.Rows[1], ref teamAP2);
            cb_TeamAP3.CheckedChanged += (s, e) => UpdateBitField((CheckBox)s, dGv_CT.Rows[2], ref teamAP3);
            cb_TeamAP4.CheckedChanged += (s, e) => UpdateBitField((CheckBox)s, dGv_CT.Rows[3], ref teamAP4);
            cb_TeamAP5.CheckedChanged += (s, e) => UpdateBitField((CheckBox)s, dGv_CT.Rows[4], ref teamAP5);

            // Team B (T) checkboxes
            cb_TeamBP1.CheckedChanged += (s, e) => UpdateBitField((CheckBox)s, dGv_T.Rows[0], ref teamBP1);
            cb_TeamBP2.CheckedChanged += (s, e) => UpdateBitField((CheckBox)s, dGv_T.Rows[1], ref teamBP2);
            cb_TeamBP3.CheckedChanged += (s, e) => UpdateBitField((CheckBox)s, dGv_T.Rows[2], ref teamBP3);
            cb_TeamBP4.CheckedChanged += (s, e) => UpdateBitField((CheckBox)s, dGv_T.Rows[3], ref teamBP4);
            cb_TeamBP5.CheckedChanged += (s, e) => UpdateBitField((CheckBox)s, dGv_T.Rows[4], ref teamBP5);
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
        // Bulk select handlers
        // =====================================
        private void cb_AllTeamA_CheckStateChanged(object sender, EventArgs e)
        {
            ToggleCheckboxes(teamACheckboxes, cb_AllTeamA.Checked);
        }

        private void cb_AllTeamB_CheckStateChanged(object sender, EventArgs e)
        {
            ToggleCheckboxes(teamBCheckboxes, cb_AllTeamB.Checked);
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