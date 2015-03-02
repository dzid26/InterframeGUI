using System;
using System.Collections.Generic;
using System.Text;

namespace InterframeGUI
{
    [Serializable]
    abstract public class Fileable
    {
        public string FilePath { get; set; }
        public Fileable() { }
        public Fileable(string path)
        {
            FilePath = path;
        }
        public string FilePathQuoted() { return (char)34 + FilePath + (char)34; }
    }
}
