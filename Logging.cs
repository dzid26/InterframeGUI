using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Deployment.Application;

namespace InterframeGUI
{
    
    public class Logging
    {

        const string LOGS_FOLDER = "Logs\\";
        private string logsPath;
        public string LogsPath;

        private StreamWriter  writer;

        public Logging()
        {
            LogsPath = LOGS_FOLDER;
            try
            {
                LogsPath = Path.Combine(ApplicationDeployment.CurrentDeployment.DataDirectory, LOGS_FOLDER);
            }
            catch (Exception)
            {
            }

            int maxLogFiles = 10;
            if (!Directory.Exists(LogsPath))
                Directory.CreateDirectory(LogsPath);
            string[] filelogsInDirectory = Directory.GetFiles(LogsPath);
            Array.Sort(filelogsInDirectory);
            Array.Reverse(filelogsInDirectory);
            for (int i = maxLogFiles; i < filelogsInDirectory.GetLength(0); i++)
            {
                File.Delete(filelogsInDirectory[i]);
            }
                writer = new StreamWriter(Path.Combine(LogsPath, DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss") + ".log"), true);


        }
        public void WriteLine(string line)
        {
            writer.WriteLine(line);
        }
        public void Flush()
        {
            writer.Flush();
        }
    }
}
