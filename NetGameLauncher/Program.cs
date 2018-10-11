using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace NetGameLauncher
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Thread.Sleep(500);
            if (File.Exists(Application.ExecutablePath + ".old"))
                File.Delete(Application.ExecutablePath + ".old");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FormLauncher());
        }
    }
}
