using CS2SourceTVDemoVoiceCalc.GUI;

namespace CS2SourceTVDemoVoiceCalc
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            MainForm mainForm = new MainForm();

            // If an argument starts with "UpdateDone", delete the old updater directory
            if (args.Length > 0 && args[0].StartsWith("UpdateDone"))
            {
                Directory.Delete("Updater_Old", true);
            }

            // If the first argument is an existing file with a ".dem" extension (case-insensitive)
            // pass the file path to the form so it can be processed on startup
            if (args.Length > 0 && File.Exists(args[0]) && Path.GetExtension(args[0]).Equals(".dem", StringComparison.OrdinalIgnoreCase))
            {
                // Passes the file to the form at startup
                mainForm.SetDemoFileOnStartup(args[0]);
            }

            Application.Run(mainForm);
        }
    }
}