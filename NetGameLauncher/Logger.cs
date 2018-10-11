using System;
using System.IO;

namespace NetGameLauncher
{
    public static class Logger
    {
        private static FileStream logStream = File.Open("launcher.log", FileMode.OpenOrCreate);
        private static StreamWriter logWriter = new StreamWriter((Stream)Logger.logStream);

        public static void Log(string message)
        {
            logWriter.WriteLine(DateTime.Now.ToString("[dd.MM.yyyy hh:mm:ss.ffff]") + " " + message);
        }

        public static void Log(Exception ex)
        {
            logWriter.WriteLine(DateTime.Now.ToString("[dd.MM.yyyy hh:mm:ss.ffff]") + String.Format(" An exception of type: {0} occured", ex.GetType()));
            foreach (string s in ex.StackTrace.Split('\n'))
            {
                logWriter.WriteLine(String.Empty.PadLeft(26, ' ') + s);
            }
            logWriter.WriteLine(String.Empty.PadLeft(26, ' ') + ex.Message);
        }
    }
}
