using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Utility.ModifyRegistry;
using System.IO;
using System.Runtime.InteropServices;
using System.Deployment;
using System.Reflection;
using System.Configuration;
using System.Deployment.Application;

namespace InterframeGUI
{
    
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Console.WriteLine();
            //  Invoke this sample with an arbitrary set of command line arguments.
            Console.WriteLine("CommandLine: {0}", Environment.CommandLine);

            Settings config = new Settings();
            Console.OpenStandardOutput();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Form toBeStarted = new InterFrameGUI();
            toBeStarted.Text += " " + GetRunningVersion();
            Application.Run(toBeStarted);

        }
        private static Version GetRunningVersion()
        {
            try
            {
                return ApplicationDeployment.CurrentDeployment.CurrentVersion;
            }
            catch
            {
                return Assembly.GetExecutingAssembly().GetName().Version;
            }
        }
    }
    public class MyUserSettings : ApplicationSettingsBase
    {
        [UserScopedSetting()]
        [DefaultSettingValue("white")]
        public string x264path { get; set; }
    }

    class Settings
    {

        ModifyRegistry myRegistry;
        String DataPath
        {
            get
            {
                if (ApplicationDeployment.IsNetworkDeployed)
                    return (ApplicationDeployment.CurrentDeployment.DataDirectory + "\\");
                else
                {
                    if(!Directory.Exists("Data"))
                        Directory.CreateDirectory("Data");
                    return "Data\\";
                }
            }
        }

        bool isTheFirstInstalation()
        {
            if (!File.Exists(DataPath + ".notTheFirstRun"))
            {
                File.Create(DataPath + ".notTheFirstRun");
                return true;
            }
            else
                return false;
        }

        public Settings()
        {

            if (isTheFirstInstalation())
            {
                Properties.Settings.Default.tempPath = Path.GetTempPath();

                if (Environment.Is64BitOperatingSystem)
                    Properties.Settings.Default.x264Path = Properties.Resources.x264x64Path;
                else
                    Properties.Settings.Default.x264Path = Properties.Resources.x264x32Path;
                //Properties.Settings.Default.Save();
            }
        }
    }
}