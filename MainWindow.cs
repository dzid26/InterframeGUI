using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Deployment.Application;
using System.Threading;
using System.Media;

namespace InterframeGUI
{
    public partial class InterFrameGUI : Form
    {
        WorkingQueue currentWork;
        Templates templates;
        bool update=true;
        Logging logfile;

        public InterFrameGUI()
        {

            InitializeComponent();
            Settings32bitX264toolStripComboBox1.Items.Add(Properties.Resources.x264x32Path);
            Settings32bitX264toolStripComboBox1.Items.Add(Properties.Resources.x264x64Path);

            currentWork = new WorkingQueue(this); //replaces source with one with "this" guiconnection
            inputQueueGridView.DataSource = currentWork;
            
            logfile = new Logging();
            templates = new Templates();
            templatesGridView.DataSource = templates;
            templateSelectionComboBox.DataSource = templates;

            x264tuningcomboBox.SelectedIndexChanged -= x264tuningcomboBox_SelectedIndexChanged; //turns off combobox events
            List<String> tuning_presets = new List<string> { "film", "animation", "grain", "psnr", "ssim", "fastdecode" };
            x264tuningcomboBox.DataSource = tuning_presets;
            x264tuningcomboBox.SelectedIndexChanged += x264tuningcomboBox_SelectedIndexChanged; //turns it on again
            
            /*
            inputQueue.SelectedIndexChanged -= inputQueue_SelectedIndexChanged; //turns off list events
            List<String> startingMessage = new List<string> { "Add some video here !!" };
            inputQueue.DataSource = startingMessage;
            inputQueue.SelectedIndexChanged += inputQueue_SelectedIndexChanged; //turns it on again
            */

            
            openVideoDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Favorites);

            ApplicationDeployment ad;
            try
            {
                ad = ApplicationDeployment.CurrentDeployment;
                ad.CheckForUpdateCompleted += OnCheckForUpdateCompleted;
                ad.CheckForUpdateProgressChanged += OnCheckForUpdateProgressChanged;

                ad.CheckForUpdateAsync();
            }
            catch (Exception)
            {
                Text += " not installed";
                updateHideIndicators();
            }
            
        }

        public void addVideoFileToInputQueueGridView(string[] fileNames)
        {
            inputQueueGridView.ClearSelection();
            Item videoJob = null;
            foreach (string path in fileNames)
            {
                try
                {
                    videoJob = Item.DeepClone(templateSelectionComboBox.SelectedItem);
                }
                catch (Exception)
                {
                    MessageBox.Show("Create some template");
                }
                videoJob.assignedVideo = new Action(videoJob, Path.GetFullPath(path));
                if (!videoJob.assignedVideo.interframe.PrepareSript())
                    WriteLineToOutputLogBox("Something went wrong with parsing template avisynth script");
                currentWork.Add(videoJob);
                inputQueueGridView.Rows[inputQueueGridView.Rows.Count - 1].Selected = true;
            }
        }
        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            addVideoFileToInputQueueGridView(openVideoDialog.FileNames);
            
        }
        private void inputQueueGridView_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }
        private void inputQueueGridView_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] fileNames = (string[])(e.Data.GetData(DataFormats.FileDrop));
                addVideoFileToInputQueueGridView(fileNames);
            }
        }


        private void OpenVideo_Click(object sender, EventArgs e)
        {
            openVideoDialog.ShowDialog();
        }

        private void SaveVideo_Click(object sender, EventArgs e)
        {
            if (inputQueueGridView.SelectedRows.Count == 1)
            {
                Item item = inputQueueGridView.CurrentRow.DataBoundItem as Item;

                openOutputFileDialog.InitialDirectory = Path.GetDirectoryName(item.assignedVideo.mkvmerge.FilePath);
                openOutputFileDialog.FileName = Path.GetFileName(item.assignedVideo.mkvmerge.FilePath);
                openOutputFileDialog.ShowDialog();
                
            }
            Refresh_controls_and_fields();
        }


        public void x264_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
                if (!e.Data.Contains("[info]"))
                {
                   WriteToOutputLogBox(e.Data);
                    
                }
                else
                    WriteLineToOutputLogBox(e.Data);
        }

        public void mkvmerge_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
                if (e.Data.Contains("Progress:"))
                    WriteToOutputLogBox(e.Data);
                else
                    WriteLineToOutputLogBox(e.Data);
        }

        delegate void SetTextCallback(string text);
        public void WriteLineToOutputLogBox(string message) //appending lines
        {

            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            try
            {
                if (this.outputLog.InvokeRequired)
                {
                    SetTextCallback d = new SetTextCallback(WriteLineToOutputLogBox);
                    this.Invoke(d, new object[] { message });
                }
                else
                {
                    this.outputLog.AppendText(message + Environment.NewLine);
                }
            }
            catch (Exception)
            {
            }
            try
            {
                logfile.WriteLine(message);
            }
            catch (Exception)
            {
            }
        }
        public void WriteToOutputLogBox(string message) //replacing line
        {

            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            try
            {
                if (LogBoxUpdatingCheckBox1.Checked)
                {
                    if (this.outputLog.InvokeRequired)
                    {
                        SetTextCallback d = new SetTextCallback(WriteToOutputLogBox);
                        this.Invoke(d, new object[] { message });
                    }
                    else
                    {
                        //string[] lines = outputLog.Lines;
                        //lines[outputLog.Lines.Length-1] = message;
                        //outputLog.Lines = lines;
                        //outputLog.SelectionStart= (outputLog.TextLength + 1);
                        //outputLog.ScrollToCaret();

                        string s = null;
                        try
                        {
                            s = outputLog.Text.Substring(0, outputLog.Text.LastIndexOf(Environment.NewLine) + Environment.NewLine.Length);
                        }
                        catch (Exception)
                        {
                        }
                        outputLog.Text = s + message;
                        outputLog.SelectionStart = (outputLog.TextLength + 1);
                        outputLog.SelectionStart = (outputLog.TextLength + 1);
                        outputLog.ScrollToCaret();
                    }
                }
                logfile.WriteLine(message);  //writing to file nevertheless LogBoxUpdatingCheckBox1
                logfile.Flush();
            }
            catch (Exception)
            {

            }
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void clearQueueButton_Click(object sender, EventArgs e)
        {
            currentWork.Clear();
        }

        private void removeQueueItemButton_Click(object sender, EventArgs e)
        {
            removeSelectedItems();
        }

        private void removeSelectedItems()
        {
            Item item;
            for (int i = inputQueueGridView.SelectedRows.Count; i > 0; i-- )
            {
                item = inputQueueGridView.SelectedRows[0].DataBoundItem as Item;
                if (item.ActionState != "running")
                    inputQueueGridView.Rows.Remove(inputQueueGridView.SelectedRows[0]);
                else
                    inputQueueGridView.SelectedRows[0].Selected = false;
            }
        }

        DataGridView CurrentDataGrid()
        {
            if (tabControl1.SelectedIndex == 0)
                return inputQueueGridView;
            else
                return templatesGridView;
        }

        private void avisynthScriptBox_TextChanged(object sender, EventArgs e)
        {
            if (CurrentDataGrid().SelectedRows.Count == 1)
            {
                Item item = CurrentDataGrid().CurrentRow.DataBoundItem as Item;
                if(CurrentDataGrid().Equals(templatesGridView))
                    item.interframe.TemplateScript = avisynthScriptBox.Text;
                else
                    item.assignedVideo.interframe.Script = avisynthScriptBox.Text;
            }
        }


        private void transcodeButton_Click(object sender, EventArgs e)
        {
            if (!currentWork.Running)
            {
                StartTranscodeGuiAction();
            }
        }

        public void transcodeStartCommand()
        {
            if (!currentWork.Running)
            {
                StartTranscodeGuiAction();
            }
        }

        private void AbortButton_Click(object sender, EventArgs e)
        {
            currentWork.StopWork();
            StopGuiAction();
        }

        public void abortCommand()
        {
            currentWork.StopWork();
            StopGuiAction();
        }
        public void StopGuiAction() //http://stackoverflow.com/questions/2367718/c-automating-the-invokerequired-code-pattern
        {
            Invoke((MethodInvoker)delegate
            {
                transcodeButton.Enabled = true;
                AbortButton.Enabled = false;
                PauseButton.Enabled = false;
                PauseButton.Text = "||";
                PauseButton.Checked = false;
                clearQueueButton.Enabled = true;
            }
            );
            
            //Refresh_controls_and_fields();
        }

        public void StartTranscodeGuiAction()
        {
            currentWork.StartWork();
                transcodeButton.Enabled = false;
                AbortButton.Enabled = true;
                PauseButton.Enabled = true;
                clearQueueButton.Enabled = false;
                //Refresh_controls_and_fields();
        }


        private void x264Preset_Scroll(object sender, EventArgs e)
        {
            foreach (DataGridViewRow QueueItem in CurrentDataGrid().SelectedRows)
                (QueueItem.DataBoundItem as Item).x264.presetInt = x264PresetTrackBar.Value;
            Refresh_controls_and_fields();
        }

        public void Refresh_controls_and_fields()
        {

            if ( CurrentDataGrid().SelectedRows.Count>0) //row with an arrow
            {
                Item inputItem = null;
                try
                {
                    inputItem = CurrentDataGrid().CurrentRow.DataBoundItem as Item;


                    if (CurrentDataGrid().SelectedRows.Count > 0)
                    {
                        x264PresetTrackBar.Value = inputItem.x264.presetInt;
                        x264PresetLabel.Text = inputItem.x264.preset;
                        x264crfNumericInput.Value = inputItem.x264.crf;
                        x264threadsNumericInput.Value = inputItem.x264.threads;
                        x264tuningcomboBox.SelectedItem = inputItem.x264.tune;
                        x264commandLine.Text = inputItem.x264.GetArguments();
                        newtemplateButton.Text = "Clone template";
                        RunAfterJobCommandTextBox.Enabled = true;
                        RunAfterJobCommandTextBox.Text = inputItem.RunAfterJobProgramCommandLine;
                        RunAfterJobArgumentsTextBox.Enabled = true;
                        RunAfterJobArgumentsTextBox.Text = inputItem.RunAfterJobProgramArgumentLine;
                        if (!currentWork.Running)
                            transcodeButton.Enabled = true;
                    }


                    if (CurrentDataGrid().SelectedRows.Count == 1)
                    {
                            
                        if (CurrentDataGrid().Equals(inputQueueGridView))
                        {
                            if (inputItem.ActionState == "running")
                            {
                                avisynthScriptBox.ReadOnly = true;
                            }
                            else
                            {
                                avisynthScriptBox.ReadOnly = false;
                                avisynthScriptBox.SelectionStart = avisynthScriptBox.Text.Length + 1;
                                avisynthScriptBox.ScrollToCaret();
                            }
                            avisynthScriptBox.Text = inputItem.assignedVideo.interframe.Script;
                            outputFileTextBox.Text = inputItem.assignedVideo.mkvmerge.FilePath;
                            outputFileTextBox.Enabled = true;
                            SaveVideoButton.Enabled = true;
                        }
                        else
                        {
                            avisynthScriptBox.Text = inputItem.interframe.TemplateScript;
                            outputFileTextBox.Text = "Here you will choose name for converted video";
                            outputFileTextBox.Enabled = false;
                            SaveVideoButton.Enabled = false;
                        }
                    }
                    else
                    {
                        avisynthScriptBox.ReadOnly = true;
                        outputFileTextBox.Enabled = false;
                        SaveVideoButton.Enabled = false;
                        RunAfterJobCommandTextBox.Enabled = false;
                        RunAfterJobArgumentsTextBox.Enabled = false;
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("A bug. Don't worry... my bad");
                }

            }
            else  //generaly not selected, but sometimes selected but without active current row
            {
                avisynthScriptBox.Text = (string.Empty);
                transcodeButton.Enabled = false;
                newtemplateButton.Text = "Create template";
                outputFileTextBox.Text = string.Empty;

            }
        }




        private void x264crfTextBox_ValueChanged(object sender, EventArgs e)
        {
            foreach (DataGridViewRow QueueItem in CurrentDataGrid().SelectedRows)
                (QueueItem.DataBoundItem as Item).x264.crf = x264crfNumericInput.Value;
            Refresh_controls_and_fields();
        }

        private void x264threadsNumericInput_ValueChanged(object sender, EventArgs e)
        {
            foreach (DataGridViewRow QueueItem in CurrentDataGrid().SelectedRows)
                (QueueItem.DataBoundItem as Item).x264.threads = (int) x264threadsNumericInput.Value;
            Refresh_controls_and_fields();
        }

        private void x264commandLine_TextChanged(object sender, EventArgs e)
        {

        }

        private void x264tuningcomboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            foreach (DataGridViewRow QueueItem in CurrentDataGrid().SelectedRows)
                (QueueItem.DataBoundItem as Item).x264.tune = (string)x264tuningcomboBox.SelectedItem;
            Refresh_controls_and_fields();
        }

        private void x264threadsLabel_Click(object sender, EventArgs e)
        {

        }

        private void InterframeGUI_FormClosed(object sender, FormClosedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void openOutputFileDialog_FileOk(object sender, CancelEventArgs e)
        {

            if (inputQueueGridView.SelectedRows.Count == 1)
            {
                Item item = inputQueueGridView.CurrentRow.DataBoundItem as Item;
                item.assignedVideo.mkvmerge.FilePath = openOutputFileDialog.FileName;
                outputFileTextBox.Text = openOutputFileDialog.FileName;
            }
        }

        private void outputFileTextBox_TextChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedIndex==0 && inputQueueGridView.SelectedRows.Count == 1)
            {
                Item item = inputQueueGridView.CurrentRow.DataBoundItem as Item;
                item.assignedVideo.mkvmerge.FilePath = Path.GetFullPath(outputFileTextBox.Text);
            }
        }



        
        private void OnCheckForUpdateProgressChanged(object sender, DeploymentProgressChangedEventArgs e)
        {
            WriteToOutputLogBox(String.Format("Downloading: {0}. {1:D}K of {2:D}K downloaded.", GetProgressString(e.State), e.BytesCompleted / 1024, e.BytesTotal / 1024));
            UpdateStripProgressBar1.Value = e.ProgressPercentage;
        }

        string GetProgressString(DeploymentProgressState state)
        {
            if (state == DeploymentProgressState.DownloadingApplicationFiles)
            {
                return "application files";
            }
            if (state == DeploymentProgressState.DownloadingApplicationInformation)
            {
                return "application manifest";
            }
            return "deployment manifest";
        }

        private void OnCheckForUpdateCompleted(object sender, CheckForUpdateCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show("ERROR: Could not retrieve new version of the application. Reason: \n" + e.Error.Message + "\nPlease report this error to the system administrator.");
                return;
            }
            if (e.Cancelled)
            {
                MessageBox.Show("The update was cancelled.");
            }

            // Ask the user if they would like to update the application now.
            if (e.UpdateAvailable)
            {
                if (!e.IsUpdateRequired)
                {
                    long updateSize = e.UpdateSizeBytes;
                    //DialogResult dr = MessageBox.Show(string.Format("An update ({0}K) is available. Would you like to update the application now?", updateSize / 1024), "Update Available", MessageBoxButtons.OKCancel);
                    updateStopStripStatusLabel1.Visible = true;
                    Thread.Sleep(5000);
                    updateStopStripStatusLabel1.Visible = false;
                    if (update)
                    {
                        BeginUpdate();
                    }
                }
                else
                {
                    MessageBox.Show("A mandatory update is available for your application. We will install the update now, after which we will save all of your in-progress data and restart your application.");
                    BeginUpdate();
                }
            }
            else
            {
                updateHideIndicators();
            }
        }

        private void BeginUpdate()
        {
            Text = "Downloading update...";
            ApplicationDeployment ad = ApplicationDeployment.CurrentDeployment;
            ad.UpdateCompleted += ad_UpdateCompleted;
            ad.UpdateProgressChanged += ad_UpdateProgressChanged;

            ad.UpdateAsync();
        }

        void ad_UpdateProgressChanged(object sender, DeploymentProgressChangedEventArgs e)
        {
            String progressText = String.Format("{0:D}K out of {1:D}K downloaded - {2:D}% complete", e.BytesCompleted / 1024, e.BytesTotal / 1024, e.ProgressPercentage);
            UpdateStripProgressBar1.Value = e.ProgressPercentage;
            WriteToOutputLogBox( progressText);
        }

        void ad_UpdateCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                MessageBox.Show("The update of the application's latest version was cancelled.");
                return;
            }
            if (e.Error != null)
            {
                MessageBox.Show("ERROR: Could not install the latest version of the application. Reason: \n" + e.Error.Message + "\nPlease report this error to the system administrator.");
                return;
            }
            UpdateInstalled_restarttoolStripStatusLabel1.Visible = true;
                updateHideIndicators();


        }
        void updateHideIndicators()
        {
            UpdateStripProgressBar1.Visible = false;
            updateLabel1.Visible = false;
            updateStopStripStatusLabel1.Visible = false;
        }
        private void clearOutputLogButton_Click(object sender, EventArgs e)
        {
            outputLog.Text = string.Empty;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }


        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            Refresh_controls_and_fields();
            //CurrentDataGrid().Focus();
        }

        private void newtemplateButton_Click(object sender, EventArgs e)
        {
            if (templates.Count > 0)
            {
                if (templatesGridView.SelectedRows.Count > 0)
                {
                    Item item = Item.DeepClone(templatesGridView.CurrentRow.DataBoundItem);
                    item.TemplateName = item.TemplateName + " copy";
                    templates.Add(item);
                }
                else
                {
                    Item item = Item.DeepClone(templates[templates.Count - 1]);
                    item.TemplateName = item.TemplateName + " copy";
                    templates.Add(item);
                }
            }
            else
            {
                Item template = new Item(null); // This is form with quotation marks ("C:\\path\\")
                template.TemplateName = "new template";
                templates.Add(template);
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            tabControl1.SelectedIndex = 1;
        }

        private void templateSelectionComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (CurrentDataGrid().Equals(inputQueueGridView))
            {
                foreach (DataGridViewRow QueueItem in CurrentDataGrid().SelectedRows)
                {
                    Action a = (QueueItem.DataBoundItem as Item).assignedVideo;
                    a.ParameterItem = Item.DeepClone(templateSelectionComboBox.SelectedItem);//a.parameterItem looks at new cloned Item with parameters
                    int selectedRow = currentWork.IndexOf(QueueItem.DataBoundItem as Item);
                    currentWork[selectedRow] = a.ParameterItem; //assigning cloned parameterItem to DataGridView datasource
                    currentWork[selectedRow].assignedVideo = a; //assigning back again to assignedVideo 'a'
                    if (!currentWork[selectedRow].assignedVideo.interframe.PrepareSript())
                        WriteLineToOutputLogBox("Something went wrong with parsing template avisynth script");
                }
                //CurrentDataGrid().DataSource = currentWork;
            }
            Refresh_controls_and_fields();
        }

        private void templatesGridView_UserDeletedRow(object sender, DataGridViewRowEventArgs e)
        {
            Refresh_controls_and_fields();
        }

        private void templatesGridView_SelectionChanged(object sender, EventArgs e)
        {
            Refresh_controls_and_fields();
        }

        private void inputQueueGridView_SelectionChanged(object sender, EventArgs e)
        {
            Refresh_controls_and_fields();
        }

        private void templatesGridView_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            if (templatesGridView.Rows.Count < 2)
            {
                SystemSounds.Beep.Play();
                e.Cancel = true;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            currentWork.SaveToDisk();
        }

        private void InterFrameGUI_FormClosing(object sender, FormClosingEventArgs e)
        {
            currentWork.StopWork();
            templates.SaveToDisk();  //Warning IDE by default doesn't catch exceptions in this event. Chech output console if exception occurs
            currentWork.SaveToDisk();
        }

        private void templatesGridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            templateSelectionComboBox.DataSource = null;
            templateSelectionComboBox.DataSource = templates;
            templateSelectionComboBox.DisplayMember = "TemplateName";
        }

        private void toolStripStatusLabel1_Click(object sender, EventArgs e)
        {
            update = false;
            updateHideIndicators();
        }

        private void toolStripStatusLabel1_Click_1(object sender, EventArgs e)
        {
            Application.Restart();
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(logfile.LogsPath);
            }
            catch (Exception)
            {
                MessageBox.Show("Directory not found");
            }
        }

        private void RunAfterJobButtonClick(object sender, EventArgs e)
        {
            //RunAfterJobOpenFileDialog.InitialDirectory(Environment.SpecialFolder.ProgramFiles);
            RunAfterJobOpenFileDialog.ShowDialog();
        }

        private void RunAfterJobOpenFileDialog_FileOk(object sender, CancelEventArgs e)
        {
            foreach (DataGridViewRow Item in CurrentDataGrid().SelectedRows)
                (Item.DataBoundItem as Item).RunAfterJobProgramCommandLine = RunAfterJobOpenFileDialog.FileName;
            Refresh_controls_and_fields();
        }

        private void RunAfterJobEmptyButtonClick(object sender, EventArgs e)
        {
            foreach (DataGridViewRow Item in CurrentDataGrid().SelectedRows)
            {
                (Item.DataBoundItem as Item).RunAfterJobProgramCommandLine = string.Empty;
                (Item.DataBoundItem as Item).RunAfterJobProgramArgumentLine = string.Empty;
            }
            Refresh_controls_and_fields();
        }

        private void RunAfterJobArgumentsTextBoxtextBox1_TextChanged(object sender, EventArgs e)
        {
            foreach (DataGridViewRow Item in CurrentDataGrid().SelectedRows)
                (Item.DataBoundItem as Item).RunAfterJobProgramArgumentLine = RunAfterJobArgumentsTextBox.Text;
        }

        private void inputQueueGridView_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (currentWork[e.RowIndex].ActionState == "running")
            {
                if (File.Exists(currentWork[e.RowIndex].assignedVideo.mkvmerge.FilePath))
                    System.Diagnostics.Process.Start("explorer.exe", "/select, " + currentWork[e.RowIndex].assignedVideo.x264.FilePathQuoted());
                else
                    System.Diagnostics.Process.Start("explorer.exe", (char)34 + Properties.Settings.Default.tempPath + (char)34);
            }
            else if (currentWork[e.RowIndex].ActionState.Contains("Done"))
            {
                if (File.Exists(currentWork[e.RowIndex].assignedVideo.mkvmerge.FilePath))
                    System.Diagnostics.Process.Start("explorer.exe", "/select, " + currentWork[e.RowIndex].assignedVideo.mkvmerge.FilePathQuoted());
                else
                    System.Diagnostics.Process.Start("explorer.exe", (char)34 + Path.GetDirectoryName(currentWork[e.RowIndex].assignedVideo.mkvmerge.FilePath) + (char)34);
            }else
                if (File.Exists(currentWork[e.RowIndex].assignedVideo.mkvmerge.FilePath))
                    System.Diagnostics.Process.Start("explorer.exe", "/select, " + currentWork[e.RowIndex].assignedVideo.FilePathQuoted());
                else
                    System.Diagnostics.Process.Start("explorer.exe", (char)34 + Path.GetDirectoryName(currentWork[e.RowIndex].assignedVideo.FilePath) + (char)34); 
            
        }

        private void playInputToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(currentWork[inputQueueGridView.CurrentRow.Index].assignedVideo.FilePathQuoted());
            }
            catch (Exception)
            {
                SystemSounds.Beep.Play();
            }
        }

        private void toolStripMenuPlayOutputItem1_Click(object sender, EventArgs e)
        {

            try
            {
                System.Diagnostics.Process.Start(currentWork[inputQueueGridView.CurrentRow.Index].assignedVideo.mkvmerge.FilePathQuoted());
            }
            catch (Exception)
            {
                SystemSounds.Beep.Play();
            }
        }

        private void inputVideoQueueContextMenuStrip_Opening(object sender, CancelEventArgs e)
        {
            if (inputQueueGridView.Rows.Count > 0)
            {
                ///////////////////////////////////////Context menu
                if (File.Exists(currentWork[inputQueueGridView.CurrentRow.Index].assignedVideo.FilePath))
                    playInputToolStripMenuItem.Enabled = true;
                else
                    playInputToolStripMenuItem.Enabled = false;
                if (File.Exists(currentWork[inputQueueGridView.CurrentRow.Index].assignedVideo.mkvmerge.FilePath))
                    playOutputToolStripMenuItem.Enabled = true;
                else
                    playOutputToolStripMenuItem.Enabled = false;
            }
            else
                e.Cancel=true;
        }

        private void locateInputOnDiskToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {

                if (File.Exists(currentWork[inputQueueGridView.CurrentRow.Index].assignedVideo.FilePath))
                    System.Diagnostics.Process.Start("explorer.exe", "/select, " + currentWork[inputQueueGridView.CurrentRow.Index].assignedVideo.FilePathQuoted());
                else
                    System.Diagnostics.Process.Start("explorer.exe", (char)34 + Path.GetDirectoryName(currentWork[inputQueueGridView.CurrentRow.Index].assignedVideo.FilePath) + (char)34);
            }
            catch (Exception)
            {
                SystemSounds.Beep.Play();
            }
        }


        private void locateOutputToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            try
            {

                if (File.Exists(currentWork[inputQueueGridView.CurrentRow.Index].assignedVideo.mkvmerge.FilePath))
                    System.Diagnostics.Process.Start("explorer.exe", "/select, " + currentWork[inputQueueGridView.CurrentRow.Index].assignedVideo.mkvmerge.FilePathQuoted());
                else
                    System.Diagnostics.Process.Start("explorer.exe", (char)34 + Path.GetDirectoryName(currentWork[inputQueueGridView.CurrentRow.Index].assignedVideo.mkvmerge.FilePath) + (char)34);
            }
            catch (Exception)
            {
                SystemSounds.Beep.Play();
            }
        }

        private void SettingsTempPathtoolStripTextBox1_TextChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.tempPath = SettingsTempPathtoolStripTextBox1.Text;
            Properties.Settings.Default.Save();
        }

        private void InterFrameGUI_Load(object sender, EventArgs e)
        {

        }

        private void Settings32bitX264toolStripComboBox1_TextChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.x264Path = Settings32bitX264toolStripComboBox1.Text;
            Properties.Settings.Default.Save();
        }

        public void PauseButton_CheckedChanged(object sender, EventArgs e)
        {
            if (!PauseButton.Checked)
            {
                currentWork.Resume();
                PauseButton.Text = "||";
            }
            else
            {
                currentWork.PauseWork();
                PauseButton.Text = "►";
            }
            //transcodeButton.Enabled = true;
            //AbortButton.Enabled = false;
        }

        private void inputQueueGridView_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            Item item = e.Row.DataBoundItem as Item;
            if (item.ActionState == "running")
                e.Cancel = true;
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            removeSelectedItems();
        }


        private void inputQueueGridView_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)  //right click
            {
                if (!inputQueueGridView.Rows[e.RowIndex].Selected)
                {
                    inputQueueGridView.CurrentCell = inputQueueGridView.Rows[e.RowIndex].Cells[0];
                }
                else
                {
                    DataGridViewSelectedRowCollection workaround = inputQueueGridView.SelectedRows;
                    inputQueueGridView.CurrentCell = inputQueueGridView.Rows[e.RowIndex].Cells[0];//stupidly it clear also selection so workaround is added
                    foreach (DataGridViewRow previouslySelectedRow in workaround)
                        inputQueueGridView.Rows[previouslySelectedRow.Index].Selected = true;


                }
                
            }
        }

        private void moveDownQueueInputItems_Click(object sender, EventArgs e)
        {

            int first = 0;
            int last = inputQueueGridView.Rows.Count - 1;

            int firstSelected = first;
            for (int i = first+1; i <= last+1 & i<inputQueueGridView.Rows.Count ; i++)
            {
                if (!inputQueueGridView.Rows[i - 1].Selected & inputQueueGridView.Rows[i].Selected)
                    firstSelected = i;

                if (!inputQueueGridView.Rows[i].Selected & inputQueueGridView.Rows[i-1].Selected)
                {
                    currentWork.Insert(firstSelected, inputQueueGridView.Rows[i].DataBoundItem as Item);
                    currentWork.RemoveAt(i + 1);
                    break;
                }
            } 

        }

        private void moveUpQueueInputItems_Click(object sender, EventArgs e)
        {

            int first = 0;
            int last = inputQueueGridView.Rows.Count-1;

            int lastSelected = last ;
            for (int i = last-1; i >=first-1 & i>=0 ; i--)
            {
                if (!inputQueueGridView.Rows[i + 1].Selected & inputQueueGridView.Rows[i].Selected)
                    lastSelected = i;

                if (!inputQueueGridView.Rows[i].Selected & inputQueueGridView.Rows[i+1].Selected)
                {
                    currentWork.Insert(lastSelected +1, inputQueueGridView.Rows[i].DataBoundItem as Item);
                    currentWork.RemoveAt(i);
                    break;
                }
            } 
        }




    }
}
