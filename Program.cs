using System;
using System.Collections.Generic;
using System.Windows;
//using System.Windows.Forms;
using Utility.ModifyRegistry;
using System.IO;
using System.Runtime.InteropServices;
using System.Deployment;
using System.Reflection;
using System.Configuration;
using System.Deployment.Application;
using Microsoft.VisualBasic.ApplicationServices;

namespace InterframeGUI
{

    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
            SingleInstanceManager manager = new SingleInstanceManager();
            manager.Run(args);



        }
        
    }



    // Using VB bits to detect single instances and process accordingly:
    //  * OnStartup is fired when the first instance loads
    //  * OnStartupNextInstance is fired when the application is re-run again
    //    NOTE: it is redirected to this instance thanks to IsSingleInstance
    public class SingleInstanceManager : WindowsFormsApplicationBase
    {
        SingleInstanceApplication _application;

        public SingleInstanceManager()
        {
            this.IsSingleInstance = true;
        }


        protected override bool OnStartup(Microsoft.VisualBasic.ApplicationServices.StartupEventArgs eventArgs)
        {


            // First time _application is launched
            _application = new SingleInstanceApplication();
            _application.Run();
            return false;

        }

        protected override void OnStartupNextInstance(StartupNextInstanceEventArgs eventArgs)
        {
            // Subsequent launches
            base.OnStartupNextInstance(eventArgs);
            _application.Activate(eventArgs.CommandLine);
        }
    }

    public class SingleInstanceApplication : System.Windows.Application
    {
        InterFrameGUI window;
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

        protected override void OnStartup(System.Windows.StartupEventArgs e)
        {
            base.OnStartup(e);
            Settings config = new Settings();
            Console.OpenStandardOutput();
            
            // Create our MainWindow and show it
            window = new InterFrameGUI();
            window.Text += " " + GetRunningVersion();
            window.Show();
            
        }

        public void Activate(System.Collections.ObjectModel.ReadOnlyCollection<string> args)
        {
            // Reactivate application's main window
            window.Activate();
            string[] a = new string[args.Count];
            args.CopyTo(a, 0);
            window.addVideoFileToInputQueueGridView(a);
            window.transcodeStartCommand();


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