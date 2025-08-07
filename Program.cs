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

            if (args.Length > 0 && File.Exists(args[0]) && Path.GetExtension(args[0]).Equals(".dem", StringComparison.OrdinalIgnoreCase))
            {
                // Passes the file to the form at startup
                mainForm.SetDemoFileOnStartup(args[0]);
            }

            Application.Run(mainForm);
        }
    }
}