using System.Diagnostics;

namespace CS2SourceTVDemoVoiceCalc.GUI
{
    public partial class About : Form
    {
        public About()
        {
            InitializeComponent();
            richTextBoxAbout.LinkClicked += RichTextBoxAbout_LinkClicked;

        }

        private void RichTextBoxAbout_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            try
            {
                // .NET Core / .NET 5+ need UseShellExecute = true
                var psi = new ProcessStartInfo
                {
                    FileName = e.LinkText,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Link could not be opened:\n{ex.Message}",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    }
}
