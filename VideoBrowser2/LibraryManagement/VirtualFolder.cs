using System;
using System.Collections.Generic;
using System.Text;
using System.IO; 

namespace SamSoft.VideoBrowser.LibraryManagement
{
    public class VirtualFolder
    {
        public List<string> Folders = new List<string>();
        public string ThumbPath;
        public string Path; 

        public VirtualFolder(string path)
        {
            Path = path;
            foreach (var line in File.ReadAllLines(path))
            {                        
                if (line.StartsWith("image:"))
                {
                    // TODO? test if its a file  
                    ThumbPath = line.Substring(6).Trim(); 
                }
                else if (line.StartsWith("folder:"))
                { 
                    // TODO? test if its a file
                    Folders.Add(line.Substring(7).Trim()); 
                }
            }
        }
    }
}
