using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SamSoft.VideoBrowser.LibraryManagement
{
    /// <summary>
    /// Special folder (smart folders, movie trailer, online content) 
    /// </summary>
    public class SpecialFolderItem : FolderItem
    {

         public SpecialFolderItem(string filename, bool isFolder)
            : this(filename, isFolder, System.IO.Path.GetFileName(filename))
        { 
        }

        public SpecialFolderItem(string filename, bool isFolder, string description)
             : base(filename, isFolder, description)
        {
            
        }
    }
}
