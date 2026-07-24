using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace Fam
{
    public class Recentfile
    {
        public string Filepath { get; set; }


        public string Name => Path.GetFileName(Filepath);

        public DateTime Lastmodifieddate => File.Exists(Filepath) ? File.GetLastWriteTime(Filepath) : new DateTime();

        public Recentfile(string filepath)
        {
            Filepath = filepath;
        }

    }
}
