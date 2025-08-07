namespace CS2SourceTVDemoVoiceCalc.GUI
{
    partial class About
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(About));
            richTextBoxAbout = new RichTextBox();
            SuspendLayout();
            // 
            // richTextBoxAbout
            // 
            richTextBoxAbout.BorderStyle = BorderStyle.None;
            richTextBoxAbout.Location = new Point(12, 12);
            richTextBoxAbout.Name = "richTextBoxAbout";
            richTextBoxAbout.ReadOnly = true;
            richTextBoxAbout.ShortcutsEnabled = false;
            richTextBoxAbout.Size = new Size(653, 444);
            richTextBoxAbout.TabIndex = 0;
            richTextBoxAbout.Text = resources.GetString("richTextBoxAbout.Text");
            // 
            // About
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(677, 468);
            Controls.Add(richTextBoxAbout);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "About";
            StartPosition = FormStartPosition.CenterParent;
            Text = "About";
            ResumeLayout(false);
        }

        #endregion

        private RichTextBox richTextBoxAbout;
    }
}