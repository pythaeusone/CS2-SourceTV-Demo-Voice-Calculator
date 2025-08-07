namespace CS2SourceTVDemoVoiceCalc.GUI
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            menuStrip1 = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            workInProgressToolStripMenuItem = new ToolStripMenuItem();
            settingsToolStripMenuItem = new ToolStripMenuItem();
            changeDemoFolderPathToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            addToShellContextMenuToolStripMenuItem = new ToolStripMenuItem();
            removeFromShellContextMenuToolStripMenuItem = new ToolStripMenuItem();
            extractorToolStripMenuItem = new ToolStripMenuItem();
            extractAudiosFromDemoToolStripMenuItem = new ToolStripMenuItem();
            showAudioplayerToolStripMenuItem = new ToolStripMenuItem();
            infoToolStripMenuItem = new ToolStripMenuItem();
            checkForUpdatesToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator2 = new ToolStripSeparator();
            smallGuideToolStripMenuItem = new ToolStripMenuItem();
            aboutToolStripMenuItem1 = new ToolStripMenuItem();
            lbl_demoFilePath = new Label();
            tb_demoFilePath = new TextBox();
            dGv_CT = new DataGridView();
            dGv_T = new DataGridView();
            lbl_TeamA = new Label();
            lbl_TeamB = new Label();
            lbl_VS = new Label();
            cb_TeamAP1 = new CheckBox();
            cb_TeamAP2 = new CheckBox();
            cb_TeamAP3 = new CheckBox();
            cb_TeamAP4 = new CheckBox();
            cb_TeamAP5 = new CheckBox();
            cb_TeamBP5 = new CheckBox();
            cb_TeamBP4 = new CheckBox();
            cb_TeamBP3 = new CheckBox();
            cb_TeamBP2 = new CheckBox();
            cb_TeamBP1 = new CheckBox();
            tb_ConsoleCommand = new TextBox();
            cb_AllTeamA = new CheckBox();
            cb_AllTeamB = new CheckBox();
            lbl_ConsolCommand = new Label();
            btn_CopyToClipboard = new Button();
            btn_MoveToCSFolder = new Button();
            lbl_MapName = new Label();
            lbl_PlayTime = new Label();
            groupBox_VoicePlayer = new GroupBox();
            listBox_VoicePlayer = new ListBox();
            dGv_VoicePlayer = new DataGridView();
            statusStrip = new StatusStrip();
            toolStripStatusLabel_StatusText = new ToolStripStatusLabel();
            toolStripStatusLabel_progressBarText = new ToolStripStatusLabel();
            toolStripProgressBar = new ToolStripProgressBar();
            toolStripStatusLabel_BY = new ToolStripStatusLabel();
            menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dGv_CT).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dGv_T).BeginInit();
            groupBox_VoicePlayer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dGv_VoicePlayer).BeginInit();
            statusStrip.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, settingsToolStripMenuItem, extractorToolStripMenuItem, infoToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.RightToLeft = RightToLeft.No;
            menuStrip1.Size = new Size(822, 24);
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { workInProgressToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(37, 20);
            fileToolStripMenuItem.Text = "File";
            // 
            // workInProgressToolStripMenuItem
            // 
            workInProgressToolStripMenuItem.Enabled = false;
            workInProgressToolStripMenuItem.Name = "workInProgressToolStripMenuItem";
            workInProgressToolStripMenuItem.Size = new Size(163, 22);
            workInProgressToolStripMenuItem.Text = "Work in progress";
            // 
            // settingsToolStripMenuItem
            // 
            settingsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { changeDemoFolderPathToolStripMenuItem, toolStripSeparator1, addToShellContextMenuToolStripMenuItem, removeFromShellContextMenuToolStripMenuItem });
            settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            settingsToolStripMenuItem.Size = new Size(61, 20);
            settingsToolStripMenuItem.Text = "Settings";
            // 
            // changeDemoFolderPathToolStripMenuItem
            // 
            changeDemoFolderPathToolStripMenuItem.Name = "changeDemoFolderPathToolStripMenuItem";
            changeDemoFolderPathToolStripMenuItem.Size = new Size(257, 22);
            changeDemoFolderPathToolStripMenuItem.Text = "Change Demo folder path";
            changeDemoFolderPathToolStripMenuItem.ToolTipText = "Changes the path to where the demo should be moved.";
            changeDemoFolderPathToolStripMenuItem.Click += changeDemoFolderPathToolStripMenuItem_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(254, 6);
            // 
            // addToShellContextMenuToolStripMenuItem
            // 
            addToShellContextMenuToolStripMenuItem.Name = "addToShellContextMenuToolStripMenuItem";
            addToShellContextMenuToolStripMenuItem.Size = new Size(257, 22);
            addToShellContextMenuToolStripMenuItem.Text = "Add to Shell-Context-Menu";
            addToShellContextMenuToolStripMenuItem.ToolTipText = "Adds the app to the Windows shell context menu.";
            addToShellContextMenuToolStripMenuItem.Click += addToShellContextMenuToolStripMenuItem_Click;
            // 
            // removeFromShellContextMenuToolStripMenuItem
            // 
            removeFromShellContextMenuToolStripMenuItem.Name = "removeFromShellContextMenuToolStripMenuItem";
            removeFromShellContextMenuToolStripMenuItem.Size = new Size(257, 22);
            removeFromShellContextMenuToolStripMenuItem.Text = "Remove from Shell-Context-Menu";
            removeFromShellContextMenuToolStripMenuItem.ToolTipText = "Removes the app from the Windows shell context menu.";
            removeFromShellContextMenuToolStripMenuItem.Click += removeFromShellContextMenuToolStripMenuItem_Click;
            // 
            // extractorToolStripMenuItem
            // 
            extractorToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { extractAudiosFromDemoToolStripMenuItem, showAudioplayerToolStripMenuItem });
            extractorToolStripMenuItem.Enabled = false;
            extractorToolStripMenuItem.Name = "extractorToolStripMenuItem";
            extractorToolStripMenuItem.Size = new Size(51, 20);
            extractorToolStripMenuItem.Text = "Audio";
            // 
            // extractAudiosFromDemoToolStripMenuItem
            // 
            extractAudiosFromDemoToolStripMenuItem.Name = "extractAudiosFromDemoToolStripMenuItem";
            extractAudiosFromDemoToolStripMenuItem.Size = new Size(204, 22);
            extractAudiosFromDemoToolStripMenuItem.Text = "Extract voice from demo";
            extractAudiosFromDemoToolStripMenuItem.ToolTipText = "If the demo has voice audio streams, these are extracted.";
            extractAudiosFromDemoToolStripMenuItem.Click += extractAudiosFromDemoToolStripMenuItem_Click;
            // 
            // showAudioplayerToolStripMenuItem
            // 
            showAudioplayerToolStripMenuItem.Name = "showAudioplayerToolStripMenuItem";
            showAudioplayerToolStripMenuItem.Size = new Size(204, 22);
            showAudioplayerToolStripMenuItem.Text = "Show the audio player";
            showAudioplayerToolStripMenuItem.ToolTipText = "Opens a voice player with more information.";
            showAudioplayerToolStripMenuItem.Click += showAudioplayerToolStripMenuItem_Click;
            // 
            // infoToolStripMenuItem
            // 
            infoToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { checkForUpdatesToolStripMenuItem, toolStripSeparator2, smallGuideToolStripMenuItem, aboutToolStripMenuItem1 });
            infoToolStripMenuItem.Name = "infoToolStripMenuItem";
            infoToolStripMenuItem.Size = new Size(40, 20);
            infoToolStripMenuItem.Text = "Info";
            // 
            // checkForUpdatesToolStripMenuItem
            // 
            checkForUpdatesToolStripMenuItem.Name = "checkForUpdatesToolStripMenuItem";
            checkForUpdatesToolStripMenuItem.Size = new Size(171, 22);
            checkForUpdatesToolStripMenuItem.Text = "Check for Updates";
            checkForUpdatesToolStripMenuItem.ToolTipText = "Checks whether a new version is available on GitHub.";
            checkForUpdatesToolStripMenuItem.Click += checkForUpdatesToolStripMenuItem_Click;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(168, 6);
            // 
            // smallGuideToolStripMenuItem
            // 
            smallGuideToolStripMenuItem.Name = "smallGuideToolStripMenuItem";
            smallGuideToolStripMenuItem.Size = new Size(171, 22);
            smallGuideToolStripMenuItem.Text = "Small Guide";
            smallGuideToolStripMenuItem.ToolTipText = "Opens a small guide.";
            smallGuideToolStripMenuItem.Click += smallGuideToolStripMenuItem_Click;
            // 
            // aboutToolStripMenuItem1
            // 
            aboutToolStripMenuItem1.Name = "aboutToolStripMenuItem1";
            aboutToolStripMenuItem1.Size = new Size(171, 22);
            aboutToolStripMenuItem1.Text = "About";
            aboutToolStripMenuItem1.ToolTipText = "About the developer and contributors.";
            aboutToolStripMenuItem1.Click += aboutToolStripMenuItem1_Click;
            // 
            // lbl_demoFilePath
            // 
            lbl_demoFilePath.AutoSize = true;
            lbl_demoFilePath.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lbl_demoFilePath.Location = new Point(12, 30);
            lbl_demoFilePath.Name = "lbl_demoFilePath";
            lbl_demoFilePath.Size = new Size(52, 15);
            lbl_demoFilePath.TabIndex = 1;
            lbl_demoFilePath.Text = "Filepath:";
            // 
            // tb_demoFilePath
            // 
            tb_demoFilePath.AllowDrop = true;
            tb_demoFilePath.Location = new Point(72, 27);
            tb_demoFilePath.Name = "tb_demoFilePath";
            tb_demoFilePath.ReadOnly = true;
            tb_demoFilePath.Size = new Size(632, 23);
            tb_demoFilePath.TabIndex = 2;
            tb_demoFilePath.Text = "drop demo File here ...";
            tb_demoFilePath.DragDrop += TB_demoFilePath_DragDrop;
            tb_demoFilePath.DragEnter += TB_demoFilePath_DragEnter;
            // 
            // dGv_CT
            // 
            dGv_CT.AllowUserToAddRows = false;
            dGv_CT.AllowUserToDeleteRows = false;
            dGv_CT.AllowUserToResizeColumns = false;
            dGv_CT.AllowUserToResizeRows = false;
            dGv_CT.BackgroundColor = SystemColors.Control;
            dGv_CT.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dGv_CT.Location = new Point(33, 95);
            dGv_CT.MultiSelect = false;
            dGv_CT.Name = "dGv_CT";
            dGv_CT.ReadOnly = true;
            dGv_CT.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dGv_CT.Size = new Size(325, 147);
            dGv_CT.TabIndex = 3;
            dGv_CT.MouseDown += dGv_CT_MouseDown;
            // 
            // dGv_T
            // 
            dGv_T.AllowUserToAddRows = false;
            dGv_T.AllowUserToDeleteRows = false;
            dGv_T.AllowUserToResizeColumns = false;
            dGv_T.AllowUserToResizeRows = false;
            dGv_T.BackgroundColor = SystemColors.Control;
            dGv_T.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dGv_T.Location = new Point(458, 95);
            dGv_T.MultiSelect = false;
            dGv_T.Name = "dGv_T";
            dGv_T.ReadOnly = true;
            dGv_T.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dGv_T.Size = new Size(325, 147);
            dGv_T.TabIndex = 4;
            dGv_T.MouseDown += dGv_T_MouseDown;
            // 
            // lbl_TeamA
            // 
            lbl_TeamA.Font = new Font("Segoe UI", 12F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, 0);
            lbl_TeamA.Location = new Point(33, 65);
            lbl_TeamA.Name = "lbl_TeamA";
            lbl_TeamA.Size = new Size(275, 21);
            lbl_TeamA.TabIndex = 5;
            lbl_TeamA.Text = "Team A";
            lbl_TeamA.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lbl_TeamB
            // 
            lbl_TeamB.Font = new Font("Segoe UI", 12F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, 0);
            lbl_TeamB.Location = new Point(508, 65);
            lbl_TeamB.Name = "lbl_TeamB";
            lbl_TeamB.Size = new Size(275, 21);
            lbl_TeamB.TabIndex = 6;
            lbl_TeamB.Text = "Team B";
            lbl_TeamB.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lbl_VS
            // 
            lbl_VS.Font = new Font("Comic Sans MS", 24F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, 0);
            lbl_VS.Location = new Point(364, 138);
            lbl_VS.Name = "lbl_VS";
            lbl_VS.Size = new Size(88, 48);
            lbl_VS.TabIndex = 7;
            lbl_VS.Text = "VS";
            lbl_VS.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // cb_TeamAP1
            // 
            cb_TeamAP1.AutoSize = true;
            cb_TeamAP1.Enabled = false;
            cb_TeamAP1.Location = new Point(12, 121);
            cb_TeamAP1.Name = "cb_TeamAP1";
            cb_TeamAP1.Size = new Size(15, 14);
            cb_TeamAP1.TabIndex = 9;
            cb_TeamAP1.UseVisualStyleBackColor = true;
            // 
            // cb_TeamAP2
            // 
            cb_TeamAP2.AutoSize = true;
            cb_TeamAP2.Enabled = false;
            cb_TeamAP2.Location = new Point(12, 146);
            cb_TeamAP2.Name = "cb_TeamAP2";
            cb_TeamAP2.Size = new Size(15, 14);
            cb_TeamAP2.TabIndex = 10;
            cb_TeamAP2.UseVisualStyleBackColor = true;
            // 
            // cb_TeamAP3
            // 
            cb_TeamAP3.AutoSize = true;
            cb_TeamAP3.Enabled = false;
            cb_TeamAP3.Location = new Point(12, 171);
            cb_TeamAP3.Name = "cb_TeamAP3";
            cb_TeamAP3.Size = new Size(15, 14);
            cb_TeamAP3.TabIndex = 11;
            cb_TeamAP3.UseVisualStyleBackColor = true;
            // 
            // cb_TeamAP4
            // 
            cb_TeamAP4.AutoSize = true;
            cb_TeamAP4.Enabled = false;
            cb_TeamAP4.Location = new Point(12, 196);
            cb_TeamAP4.Name = "cb_TeamAP4";
            cb_TeamAP4.Size = new Size(15, 14);
            cb_TeamAP4.TabIndex = 12;
            cb_TeamAP4.UseVisualStyleBackColor = true;
            // 
            // cb_TeamAP5
            // 
            cb_TeamAP5.AutoSize = true;
            cb_TeamAP5.Enabled = false;
            cb_TeamAP5.Location = new Point(12, 221);
            cb_TeamAP5.Name = "cb_TeamAP5";
            cb_TeamAP5.Size = new Size(15, 14);
            cb_TeamAP5.TabIndex = 13;
            cb_TeamAP5.UseVisualStyleBackColor = true;
            // 
            // cb_TeamBP5
            // 
            cb_TeamBP5.AutoSize = true;
            cb_TeamBP5.Enabled = false;
            cb_TeamBP5.Location = new Point(793, 221);
            cb_TeamBP5.Name = "cb_TeamBP5";
            cb_TeamBP5.Size = new Size(15, 14);
            cb_TeamBP5.TabIndex = 18;
            cb_TeamBP5.UseVisualStyleBackColor = true;
            // 
            // cb_TeamBP4
            // 
            cb_TeamBP4.AutoSize = true;
            cb_TeamBP4.Enabled = false;
            cb_TeamBP4.Location = new Point(793, 196);
            cb_TeamBP4.Name = "cb_TeamBP4";
            cb_TeamBP4.Size = new Size(15, 14);
            cb_TeamBP4.TabIndex = 17;
            cb_TeamBP4.UseVisualStyleBackColor = true;
            // 
            // cb_TeamBP3
            // 
            cb_TeamBP3.AutoSize = true;
            cb_TeamBP3.Enabled = false;
            cb_TeamBP3.Location = new Point(793, 171);
            cb_TeamBP3.Name = "cb_TeamBP3";
            cb_TeamBP3.Size = new Size(15, 14);
            cb_TeamBP3.TabIndex = 16;
            cb_TeamBP3.UseVisualStyleBackColor = true;
            // 
            // cb_TeamBP2
            // 
            cb_TeamBP2.AutoSize = true;
            cb_TeamBP2.Enabled = false;
            cb_TeamBP2.Location = new Point(793, 146);
            cb_TeamBP2.Name = "cb_TeamBP2";
            cb_TeamBP2.Size = new Size(15, 14);
            cb_TeamBP2.TabIndex = 15;
            cb_TeamBP2.UseVisualStyleBackColor = true;
            // 
            // cb_TeamBP1
            // 
            cb_TeamBP1.AutoSize = true;
            cb_TeamBP1.Enabled = false;
            cb_TeamBP1.Location = new Point(793, 121);
            cb_TeamBP1.Name = "cb_TeamBP1";
            cb_TeamBP1.Size = new Size(15, 14);
            cb_TeamBP1.TabIndex = 14;
            cb_TeamBP1.UseVisualStyleBackColor = true;
            // 
            // tb_ConsoleCommand
            // 
            tb_ConsoleCommand.BackColor = SystemColors.Window;
            tb_ConsoleCommand.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            tb_ConsoleCommand.Location = new Point(33, 291);
            tb_ConsoleCommand.Name = "tb_ConsoleCommand";
            tb_ConsoleCommand.ReadOnly = true;
            tb_ConsoleCommand.Size = new Size(624, 29);
            tb_ConsoleCommand.TabIndex = 20;
            tb_ConsoleCommand.Text = "Select one or more players you would like to hear in the demo...";
            tb_ConsoleCommand.TextAlign = HorizontalAlignment.Center;
            // 
            // cb_AllTeamA
            // 
            cb_AllTeamA.AutoSize = true;
            cb_AllTeamA.Enabled = false;
            cb_AllTeamA.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            cb_AllTeamA.Location = new Point(12, 248);
            cb_AllTeamA.Name = "cb_AllTeamA";
            cb_AllTeamA.Size = new Size(78, 19);
            cb_AllTeamA.TabIndex = 21;
            cb_AllTeamA.Text = "Select All";
            cb_AllTeamA.UseVisualStyleBackColor = true;
            cb_AllTeamA.CheckStateChanged += CB_AllTeamA_CheckStateChanged;
            // 
            // cb_AllTeamB
            // 
            cb_AllTeamB.AutoSize = true;
            cb_AllTeamB.Enabled = false;
            cb_AllTeamB.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            cb_AllTeamB.Location = new Point(730, 248);
            cb_AllTeamB.Name = "cb_AllTeamB";
            cb_AllTeamB.RightToLeft = RightToLeft.Yes;
            cb_AllTeamB.Size = new Size(78, 19);
            cb_AllTeamB.TabIndex = 22;
            cb_AllTeamB.Text = "Select All";
            cb_AllTeamB.UseVisualStyleBackColor = true;
            cb_AllTeamB.CheckStateChanged += CB_AllTeamB_CheckStateChanged;
            // 
            // lbl_ConsolCommand
            // 
            lbl_ConsolCommand.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lbl_ConsolCommand.Location = new Point(33, 274);
            lbl_ConsolCommand.Name = "lbl_ConsolCommand";
            lbl_ConsolCommand.Size = new Size(624, 16);
            lbl_ConsolCommand.TabIndex = 23;
            lbl_ConsolCommand.Text = "Copy the command below into your CS2 console after you have loaded this demo.";
            lbl_ConsolCommand.TextAlign = ContentAlignment.BottomCenter;
            // 
            // btn_CopyToClipboard
            // 
            btn_CopyToClipboard.Enabled = false;
            btn_CopyToClipboard.FlatStyle = FlatStyle.Flat;
            btn_CopyToClipboard.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btn_CopyToClipboard.Location = new Point(663, 293);
            btn_CopyToClipboard.Name = "btn_CopyToClipboard";
            btn_CopyToClipboard.Size = new Size(120, 25);
            btn_CopyToClipboard.TabIndex = 25;
            btn_CopyToClipboard.Text = "Copy Command";
            btn_CopyToClipboard.UseVisualStyleBackColor = true;
            // 
            // btn_MoveToCSFolder
            // 
            btn_MoveToCSFolder.Enabled = false;
            btn_MoveToCSFolder.FlatStyle = FlatStyle.Flat;
            btn_MoveToCSFolder.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btn_MoveToCSFolder.Location = new Point(710, 27);
            btn_MoveToCSFolder.Name = "btn_MoveToCSFolder";
            btn_MoveToCSFolder.Size = new Size(100, 23);
            btn_MoveToCSFolder.TabIndex = 26;
            btn_MoveToCSFolder.Text = "Move to CS2";
            btn_MoveToCSFolder.UseVisualStyleBackColor = true;
            // 
            // lbl_MapName
            // 
            lbl_MapName.Font = new Font("Segoe UI", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lbl_MapName.Location = new Point(310, 61);
            lbl_MapName.Name = "lbl_MapName";
            lbl_MapName.Size = new Size(193, 25);
            lbl_MapName.TabIndex = 27;
            lbl_MapName.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lbl_PlayTime
            // 
            lbl_PlayTime.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lbl_PlayTime.Location = new Point(364, 221);
            lbl_PlayTime.Name = "lbl_PlayTime";
            lbl_PlayTime.Size = new Size(88, 21);
            lbl_PlayTime.TabIndex = 28;
            lbl_PlayTime.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // groupBox_VoicePlayer
            // 
            groupBox_VoicePlayer.Controls.Add(listBox_VoicePlayer);
            groupBox_VoicePlayer.Controls.Add(dGv_VoicePlayer);
            groupBox_VoicePlayer.Enabled = false;
            groupBox_VoicePlayer.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            groupBox_VoicePlayer.Location = new Point(12, 340);
            groupBox_VoicePlayer.Name = "groupBox_VoicePlayer";
            groupBox_VoicePlayer.Size = new Size(798, 174);
            groupBox_VoicePlayer.TabIndex = 31;
            groupBox_VoicePlayer.TabStop = false;
            groupBox_VoicePlayer.Text = " Voice-Player ";
            groupBox_VoicePlayer.Visible = false;
            // 
            // listBox_VoicePlayer
            // 
            listBox_VoicePlayer.BorderStyle = BorderStyle.FixedSingle;
            listBox_VoicePlayer.FormattingEnabled = true;
            listBox_VoicePlayer.ItemHeight = 21;
            listBox_VoicePlayer.Location = new Point(15, 28);
            listBox_VoicePlayer.Name = "listBox_VoicePlayer";
            listBox_VoicePlayer.ScrollAlwaysVisible = true;
            listBox_VoicePlayer.Size = new Size(375, 128);
            listBox_VoicePlayer.Sorted = true;
            listBox_VoicePlayer.TabIndex = 6;
            listBox_VoicePlayer.SelectedIndexChanged += listBox_VoicePlayer_SelectedIndexChanged;
            // 
            // dGv_VoicePlayer
            // 
            dGv_VoicePlayer.AllowUserToAddRows = false;
            dGv_VoicePlayer.AllowUserToDeleteRows = false;
            dGv_VoicePlayer.AllowUserToResizeColumns = false;
            dGv_VoicePlayer.AllowUserToResizeRows = false;
            dGv_VoicePlayer.BackgroundColor = SystemColors.Control;
            dGv_VoicePlayer.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dGv_VoicePlayer.Location = new Point(406, 28);
            dGv_VoicePlayer.MultiSelect = false;
            dGv_VoicePlayer.Name = "dGv_VoicePlayer";
            dGv_VoicePlayer.ReadOnly = true;
            dGv_VoicePlayer.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dGv_VoicePlayer.Size = new Size(375, 128);
            dGv_VoicePlayer.TabIndex = 5;
            dGv_VoicePlayer.CellDoubleClick += dGv_VoicePlayer_CellDoubleClick;
            // 
            // statusStrip
            // 
            statusStrip.Items.AddRange(new ToolStripItem[] { toolStripStatusLabel_StatusText, toolStripStatusLabel_progressBarText, toolStripProgressBar, toolStripStatusLabel_BY });
            statusStrip.Location = new Point(0, 339);
            statusStrip.Name = "statusStrip";
            statusStrip.Size = new Size(822, 22);
            statusStrip.SizingGrip = false;
            statusStrip.TabIndex = 32;
            // 
            // toolStripStatusLabel_StatusText
            // 
            toolStripStatusLabel_StatusText.AutoSize = false;
            toolStripStatusLabel_StatusText.BorderSides = ToolStripStatusLabelBorderSides.Right;
            toolStripStatusLabel_StatusText.BorderStyle = Border3DStyle.Etched;
            toolStripStatusLabel_StatusText.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripStatusLabel_StatusText.Name = "toolStripStatusLabel_StatusText";
            toolStripStatusLabel_StatusText.Size = new Size(200, 17);
            toolStripStatusLabel_StatusText.Text = "Wait";
            toolStripStatusLabel_StatusText.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // toolStripStatusLabel_progressBarText
            // 
            toolStripStatusLabel_progressBarText.AutoSize = false;
            toolStripStatusLabel_progressBarText.Name = "toolStripStatusLabel_progressBarText";
            toolStripStatusLabel_progressBarText.Size = new Size(200, 17);
            // 
            // toolStripProgressBar
            // 
            toolStripProgressBar.AutoSize = false;
            toolStripProgressBar.ForeColor = Color.FromArgb(255, 128, 0);
            toolStripProgressBar.Name = "toolStripProgressBar";
            toolStripProgressBar.Size = new Size(200, 16);
            toolStripProgressBar.Step = 1;
            toolStripProgressBar.Style = ProgressBarStyle.Continuous;
            // 
            // toolStripStatusLabel_BY
            // 
            toolStripStatusLabel_BY.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripStatusLabel_BY.Enabled = false;
            toolStripStatusLabel_BY.Name = "toolStripStatusLabel_BY";
            toolStripStatusLabel_BY.Size = new Size(205, 17);
            toolStripStatusLabel_BY.Spring = true;
            toolStripStatusLabel_BY.Text = "by Pythaeus | Team Kinzo 2025";
            toolStripStatusLabel_BY.TextAlign = ContentAlignment.MiddleRight;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(822, 361);
            Controls.Add(statusStrip);
            Controls.Add(groupBox_VoicePlayer);
            Controls.Add(lbl_PlayTime);
            Controls.Add(lbl_MapName);
            Controls.Add(btn_MoveToCSFolder);
            Controls.Add(btn_CopyToClipboard);
            Controls.Add(lbl_ConsolCommand);
            Controls.Add(cb_AllTeamB);
            Controls.Add(cb_AllTeamA);
            Controls.Add(tb_ConsoleCommand);
            Controls.Add(cb_TeamBP5);
            Controls.Add(cb_TeamBP4);
            Controls.Add(cb_TeamBP3);
            Controls.Add(cb_TeamBP2);
            Controls.Add(cb_TeamBP1);
            Controls.Add(cb_TeamAP5);
            Controls.Add(cb_TeamAP4);
            Controls.Add(cb_TeamAP3);
            Controls.Add(cb_TeamAP2);
            Controls.Add(cb_TeamAP1);
            Controls.Add(lbl_VS);
            Controls.Add(lbl_TeamB);
            Controls.Add(lbl_TeamA);
            Controls.Add(dGv_T);
            Controls.Add(dGv_CT);
            Controls.Add(tb_demoFilePath);
            Controls.Add(lbl_demoFilePath);
            Controls.Add(menuStrip1);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = menuStrip1;
            MaximizeBox = false;
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dGv_CT).EndInit();
            ((System.ComponentModel.ISupportInitialize)dGv_T).EndInit();
            groupBox_VoicePlayer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dGv_VoicePlayer).EndInit();
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip menuStrip1;
        private Label lbl_demoFilePath;
        private TextBox tb_demoFilePath;
        private DataGridView dGv_CT;
        private DataGridView dGv_T;
        private Label lbl_TeamA;
        private Label lbl_TeamB;
        private Label lbl_VS;
        private CheckBox cb_TeamAP1;
        private CheckBox cb_TeamAP2;
        private CheckBox cb_TeamAP3;
        private CheckBox cb_TeamAP4;
        private CheckBox cb_TeamAP5;
        private CheckBox cb_TeamBP5;
        private CheckBox cb_TeamBP4;
        private CheckBox cb_TeamBP3;
        private CheckBox cb_TeamBP2;
        private CheckBox cb_TeamBP1;
        private TextBox tb_ConsoleCommand;
        private CheckBox cb_AllTeamA;
        private CheckBox cb_AllTeamB;
        private Label lbl_ConsolCommand;
        private Button btn_CopyToClipboard;
        private Button btn_MoveToCSFolder;
        private ToolStripMenuItem settingsToolStripMenuItem;
        private ToolStripMenuItem changeDemoFolderPathToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem addToShellContextMenuToolStripMenuItem;
        private ToolStripMenuItem removeFromShellContextMenuToolStripMenuItem;
        private ToolStripMenuItem infoToolStripMenuItem;
        private ToolStripMenuItem checkForUpdatesToolStripMenuItem;
        private ToolStripMenuItem aboutToolStripMenuItem1;
        private ToolStripSeparator toolStripSeparator2;
        private Label lbl_MapName;
        private Label lbl_PlayTime;
        private ToolStripMenuItem smallGuideToolStripMenuItem;
        private ToolStripMenuItem extractorToolStripMenuItem;
        private ToolStripMenuItem extractAudiosFromDemoToolStripMenuItem;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem workInProgressToolStripMenuItem;
        private ToolStripMenuItem showAudioplayerToolStripMenuItem;
        private GroupBox groupBox_VoicePlayer;
        private DataGridView dGv_VoicePlayer;
        private ListBox listBox_VoicePlayer;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel toolStripStatusLabel_StatusText;
        private ToolStripProgressBar toolStripProgressBar;
        private ToolStripStatusLabel toolStripStatusLabel_BY;
        private ToolStripStatusLabel toolStripStatusLabel_progressBarText;
    }
}