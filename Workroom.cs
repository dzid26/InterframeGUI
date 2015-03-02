using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Formatters.Soap;

namespace InterframeGUI
{
    [Serializable()]
    public class Item
    {
        public Item(string s)
        {
            TemplateName = s;

            interframe = new tasks.ParametersInterframeScript(Properties.Settings.Default.templatePath);
            x264 = new tasks.ParametersX264();
            mkvmerge = new tasks.ParametersMKVmerge();
        }
        
        public string TemplateName { get; set; }
        public string ActionState { get { return assignedVideo.State; } set { assignedVideo.State= value; } }
        public string ActionFilepath { get { return assignedVideo == null ? "" : assignedVideo.FilePath; } set { assignedVideo.FilePath = value; } }
        public string RunAfterJobProgramCommandLine { get; set; }
        public string RunAfterJobProgramArgumentLine { get; set; }

        public tasks.ParametersInterframeScript interframe;
        public tasks.ParametersX264 x264;
        public tasks.ParametersMKVmerge mkvmerge;

        public Action assignedVideo;


        public static Item DeepClone(Object obj)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                ms.Position = 0;

                return (Item)formatter.Deserialize(ms);
            }
        }


        //private void notifypropertychanged()
        //{
        //    if (propertychanged != null)
        //    {
        //        propertychanged(this, new propertychangedeventargs(null));
        //    }
        //}
        //public event propertychangedeventhandler propertychanged;
    }
    [Serializable()]
    public class Action : Fileable, INotifyPropertyChanged //for state changes to do
    {
        //public string FilePath { get; set; }
        public string State { get; set; }

        public Action(Item input, string path)
        {
            parameterItem = input;
            FilePath = path;
            State = "ready";
            tasks.VideoClip inputVideo = new tasks.VideoClip(FilePath);
            interframe = new tasks.AvisynthSript(inputVideo, ParameterItem.interframe);
            x264 = new tasks.X264(interframe, ParameterItem.x264); //would be good give here not a copy of interframesript object 
            mkvmerge = new tasks.MKVmerge(inputVideo, ParameterItem.mkvmerge, x264.outputVideo);
        }
        private void FeedTasksWithParameters()
        {
            interframe.parameters=ParameterItem.interframe;
            x264.parameters=ParameterItem.x264;
            mkvmerge.parameters=ParameterItem.mkvmerge;
        }
        public Item ParameterItem { get { return parameterItem; } set { parameterItem = value; FeedTasksWithParameters(); } }
        private Item parameterItem;
        public tasks.AvisynthSript interframe;
        public tasks.X264 x264;
        public tasks.MKVmerge mkvmerge;


        /*
        public void FillWithMkvInfo(string info) //it parses mkvInfo and gets needed video properties
        {
            MkvInfo = info;
            int b = MkvInfo.IndexOf(" fps for a video track");
            int a = MkvInfo.LastIndexOf("(", b) + 1;
            Fps = MkvInfo.Substring(a, b - a);
            a = MkvInfo.IndexOf("+ Pixel width: ") + 15;
            b = MkvInfo.IndexOf("\r", a);
            string s = MkvInfo.Substring(a, b - a);
            PixelWidth = int.Parse(s);
            a = MkvInfo.IndexOf("+ Pixel height: ", b) + 16;
            b = MkvInfo.IndexOf("\r", a);
            PixelHeight = int.Parse(MkvInfo.Substring(a, b - a));
            AspectRatio = (double)PixelWidth / PixelHeight;
        }
         * */ //mkvinfo


        public event PropertyChangedEventHandler PropertyChanged;
    }


    [Serializable()]
    public class WorkingQueue : BindingList<Item>, INotifyPropertyChanged
    {
        [NonSerialized]
        public Thread worker;
        [NonSerialized]
        ExternalTools tools;
        [NonSerialized]
        InterFrameGUI gui;
        public bool Running { get; set; }
        public WorkingQueue(InterFrameGUI connectionWithGUI)
        {
            tools = new ExternalTools(connectionWithGUI);
            gui = connectionWithGUI;
            Running=false;
            
            //Queue = new List<Job>();
            if (File.Exists("Queue.save"))
            {
                    Stream FileStream = File.OpenRead("Queue.save");
                    try
                    {
                        BinaryFormatter deserializer = new BinaryFormatter();
                        foreach (Item videoJobFromFile in ((BindingList<Item>)deserializer.Deserialize(FileStream)))
                        {
                            //videoJobFromFile.assignedVideo = new Action(videoJobFromFile,"dd");
                            this.Add(videoJobFromFile);
                        }
                        FileStream.Close();
                    }
                    catch (Exception)
                    {
                        FileStream.Close();
                        MessageBox.Show("Something went wrong with reading saved Queue. I'm going to delete Queue.save.");
                        File.Delete("Queue.save");
                    }
                    
                
            }
            
        }
        private void WorkOnQueue()
        {
            Running = true;
            int ActiveNo = 0;

            
            while (this.Count > ActiveNo)
            {
                Item transcodeJob = this[ActiveNo];
                transcodeJob.ActionState = "running";

                try
                {
                    gui.WriteLineToOutputLogBox("____________________________________________");
                    transcodeJob.assignedVideo.interframe.WriteSriptToFile();
                    gui.WriteLineToOutputLogBox("<x264 program> " + transcodeJob.assignedVideo.x264.GetAllArguments());
                    transcodeJob.assignedVideo.x264.AssignedProcess = tools.Run_x264(transcodeJob.assignedVideo.x264.GetAllArguments());
                    transcodeJob.assignedVideo.x264.AssignedProcess.WaitForExit();

                    if (transcodeJob.assignedVideo.x264.AssignedProcess.ExitCode == 0)
                    {
                        transcodeJob.ActionState = "muxing";
                        gui.WriteLineToOutputLogBox("<mkvmergeprogram program> " + transcodeJob.assignedVideo.mkvmerge.GetAllArguments());
                        transcodeJob.assignedVideo.mkvmerge.AssignedProcess = tools.Run_mkvmerge(transcodeJob.assignedVideo.mkvmerge.GetAllArguments());
                        transcodeJob.assignedVideo.mkvmerge.AssignedProcess.WaitForExit();
                    }
                    if (!(transcodeJob.assignedVideo.x264.AssignedProcess.ExitCode == 0 && transcodeJob.assignedVideo.mkvmerge.AssignedProcess.ExitCode != 0)) //don't delete if x264 was succesfull and mkvmerge not - because it would be a pity to lose all that time x264 worked
                    {
                        try
                        {
                            transcodeJob.ActionState = "deleting avs temp";
                            File.Delete(transcodeJob.assignedVideo.interframe.FilePath);
                            Thread.Sleep(300);
                            transcodeJob.ActionState = "deleting x264 temp";
                            File.Delete(transcodeJob.assignedVideo.x264.outputVideo.FilePath);
                            Thread.Sleep(300);
                        }
                        catch (Exception)
                        {
                            MessageBox.Show("Cannot delete temp video file of:" + transcodeJob.assignedVideo.x264.FilePath + " or .avs");
                        }
                    }

                    if (transcodeJob.assignedVideo.x264.AssignedProcess.ExitCode != 0)
                        transcodeJob.ActionState = "x264 failed";
                    else if (transcodeJob.assignedVideo.mkvmerge.AssignedProcess.ExitCode != 0)
                        transcodeJob.ActionState = "mkvmerge failed";
                    else
                    {
                        transcodeJob.ActionState = "Done";
                        try
                        {
                            if (transcodeJob.RunAfterJobProgramCommandLine != string.Empty)
                            {
                                transcodeJob.ActionState = "Done, external program launched";
                                System.Diagnostics.Process.Start(transcodeJob.RunAfterJobProgramCommandLine, transcodeJob.RunAfterJobProgramArgumentLine.Replace("%1", transcodeJob.assignedVideo.mkvmerge.FilePathQuoted()));
                            }
                        }
                        catch (Exception)
                        {
                            transcodeJob.ActionState = "Done, but external program failed";
                            MessageBox.Show("Execution of program after video conversion job failed");
                        }
                    }

                }
                catch (ThreadAbortException e)
                {
                    transcodeJob.ActionState = "aborted, deleting temps";
                    File.Delete(transcodeJob.assignedVideo.x264.outputVideo.FilePath);
                    File.Delete(transcodeJob.assignedVideo.interframe.FilePath);
                    Console.WriteLine("Exception message: {0}, temporary files should be deleted", e.Message);
                    transcodeJob.ActionState = "aborted";
                    Running = false;
                }
                catch (Exception e)
                {
                    transcodeJob.ActionState = "error!";
                    Running = false;
                    MessageBox.Show("Check settings in menu. For example Temp path. Error:" + e.Message);
                }
                ActiveNo = this.IndexOf(transcodeJob)+1;//even if queue order will be changed this will point to next movie job
            }
            Running = false;
            gui.StopGuiAction();
        }

        

        public void StartWork()
        {
            worker = new Thread(new ThreadStart(WorkOnQueue));
            worker.Start();
        }
        public void StopWork()
        {
            foreach (Item transcodeJob in this)
            {
                if (transcodeJob.ActionState == "running")
                {
                    transcodeJob.assignedVideo.x264.StopAssignedProcess();
                    transcodeJob.assignedVideo.mkvmerge.StopAssignedProcess();
                }
            }
            try
            {
                worker.Abort();
            }
            catch (Exception)
            {

            }
            
        }
        public void PauseWork()
        {
            foreach (Item transcodeJob in this)
            {
                if (transcodeJob.ActionState == "running")
                {
                    transcodeJob.assignedVideo.x264.SuspendAssignedProcess();
                    transcodeJob.assignedVideo.mkvmerge.SuspendAssignedProcess();
                }
            }
        }

        internal void Resume()
        {
            foreach (Item transcodeJob in this)
            {
                if (transcodeJob.ActionState == "running")
                {
                    transcodeJob.assignedVideo.x264.ResumeAssignedProcess();
                    transcodeJob.assignedVideo.mkvmerge.ResumeAssignedProcess();
                }
            }            
            
        }

        public void SaveToDisk()
        {
            Stream FileStream = File.Create("Queue.save");
            BinaryFormatter serializer = new BinaryFormatter();
            try
            {
                serializer.Serialize(FileStream, (BindingList<Item>)this);
            }
            catch (Exception e)
            {
                FileStream.Close();
                throw;
            }
            FileStream.Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;



        
    }


    
}
