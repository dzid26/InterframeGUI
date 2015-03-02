using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using System.Runtime.Serialization.Formatters.Soap;
using System.Deployment.Application;

namespace InterframeGUI
{
    public class Templates : BindingList<Item>
    {
        const string TEMPLATE_FOLDER = "Templates\\";
        private string templatesPath;
        public string TemplatesPath
        {
            get { return templatesPath; }
            private set { templatesPath = value; }
        }

        public Templates()
        {
            TemplatesPath = TEMPLATE_FOLDER;
            try
            {
                TemplatesPath = Path.Combine(ApplicationDeployment.CurrentDeployment.DataDirectory, TEMPLATE_FOLDER);
            }
            catch (Exception)
            {
            }

            if (Directory.Exists(TemplatesPath))
            {
                foreach (string FileNameOfTemplate in Directory.GetFiles(TemplatesPath))
                {
                    if (File.Exists(FileNameOfTemplate))
                    {
                        Stream FileStream = File.OpenRead(FileNameOfTemplate);
                        try
                        {

                            SoapFormatter deserializer = new SoapFormatter();
                            this.Add((Item)deserializer.Deserialize(FileStream));
                            FileStream.Close();
                        }
                        catch (Exception)
                        {
                            FileStream.Close();
                            DialogResult o = MessageBox.Show("Something went wrong with reading saved template. This probably my fault. You can go back to previous version of InterframeGUI to read those templates. You can also delete them I use default once. Delete?", Path.GetFileNameWithoutExtension(FileNameOfTemplate), MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                            if (o == DialogResult.Yes)
                                File.Delete(FileNameOfTemplate);
                            else if (o == DialogResult.Cancel)
                                Environment.Exit(1);
                            else
                                File.Move(FileNameOfTemplate, Path.ChangeExtension(FileNameOfTemplate, "dontDelete"));

                        }
                        
                    }

                }
            }
        }


        public void SaveToDisk()
        {

            if (!Directory.Exists(TemplatesPath))
                Directory.CreateDirectory(TemplatesPath);
            else
                foreach (string filePath in Directory.GetFiles(TemplatesPath))
                    if (filePath.Contains("dontDelete"))
                        File.Move(filePath, Path.ChangeExtension(filePath, "xml"));
                    else
                        File.Delete(filePath);

            foreach (Item templateItem in this)
            {
                string filename = Path.Combine(TemplatesPath, Path.ChangeExtension(templateItem.TemplateName, "xml"));
                if (File.Exists(filename))
                    filename = filename.Replace(".xml", " copy .xml");
                Stream FileStream = File.Create(filename);
                SoapFormatter serializer = new SoapFormatter();
                serializer.Serialize(FileStream, templateItem);
                FileStream.Close();
            }
        }
    }
}
