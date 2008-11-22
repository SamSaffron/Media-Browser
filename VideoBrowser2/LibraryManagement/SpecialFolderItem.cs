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

         public SpecialFolderItem(string filename, bool isFolder, bool useBanners)
            : this(filename, isFolder, System.IO.Path.GetFileName(filename), useBanners)
        { 
        }

        public SpecialFolderItem(string filename, bool isFolder, string description, bool useBanners)
             : base(filename, isFolder, description, useBanners)
        {
            
        }
    }
}
