using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.IO.IsolatedStorage;
using System.Threading;

namespace SamSoft.VideoBrowser.LibraryManagement
{

    /// <summary>
    /// Extends IList to support a to dictionary method 
    /// </summary>
    public static class IListExtension
    {
        public static Dictionary<Guid, FolderItem> ToDictionary(IList<FolderItem> list)
        {
            if (list == null)
            {
                return null;
            } 

            Dictionary<Guid, FolderItem> dict = new Dictionary<Guid, FolderItem>();
            foreach (FolderItem item in list)
            {
                dict.Add(item.Hash, item); 
            }

            return dict;

        }
    } 

    internal delegate void FolderChangedDelegate(IList<FolderItem> newFolderDetails); 

    /// <summary>
    /// 
    /// </summary>
    class Scanner
    {

        static readonly Scanner instance = new Scanner();

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static Scanner()
        {
        }


        public static Scanner Instance
        {
            get
            {
                return instance;
            }
        }

        private List<FolderItem> GetRealFolderData(string path)
        {

            List<FolderItem> rval = new List<FolderItem>();

            try
            {
                foreach (string filename in Directory.GetDirectories(path))
                {
                    rval.Add(new FolderItem(filename, true, false));
                }


                foreach (string filename in Directory.GetFiles(path))
                {
                    if (Helper.IsShortcut(filename))
                    {
                        FolderItem fi = new FolderItem(Helper.ResolveShortcut(filename), true, System.IO.Path.GetFileNameWithoutExtension(filename), false);
                        fi.Path = path;
                        rval.Add(fi);
                    }

                    if (Helper.IsVideo(filename))
                    {
                        rval.Add(new FolderItem(filename, false, false));
                    }
                }
            }
            catch (DirectoryNotFoundException)
            {
                Trace.TraceInformation("Missing Dir: (Bad shortcut)" + path);
            }

            return rval;
        }
    }
}
