using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Security.Cryptography;
using Microsoft.Win32;
using System.Reflection;
using System.Drawing;
using System.Drawing.Imaging;


namespace MediaBrowser.LibraryManagement
{
    using System.Drawing.Drawing2D;
    using System.Diagnostics;
    using System.Text.RegularExpressions;
    using MediaBrowser.Util;

    public static class Helper
    {
        public const string MY_VIDEOS = "MyVideos";
        static readonly string[] isoExtensions = { "iso", "img" };

        public static Dictionary<string, bool> perceivedTypeCache = new Dictionary<string, bool>();
        static Dictionary<string, string> pathMap;

        static Helper()
        {
            pathMap = new Dictionary<string, string>();
            pathMap["app_data"] = System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData);

            string[,] tree = { 
                    { "AppConfigPath",      "app_data",         "MediaBrowser"  }, 
                    { "AppCachePath",       "AppConfigPath",    "Cache"         },
                    { "AutoPlaylistPath",   "AppCachePath",     "autoPlaylists" }, 
                    { "AppImagePath",       "AppCachePath",     "images"},
                    { "AppInitialDirPath",       "AppConfigPath",    "StartupFolder" },
                    { "AppPluginPath",       "AppConfigPath",    "Plugins" },
                    { "AppRSSPath",  "AppConfigPath",    "RSS"},
                    { "AppLogPath",  "AppConfigPath",    "Logs"}
            };

            for (int i = 0; i <= tree.GetUpperBound(0); i++)
            {
                var e = Path.Combine(pathMap[tree[i, 1]], tree[i, 2]);
                if (!Directory.Exists(e))
                {
                    Directory.CreateDirectory(e);
                }
                pathMap[tree[i, 0]] = e;
            }
        }


        public static string AppLogPath {
            get {
                return pathMap["AppLogPath"];
            }
        }

        public static string AppPluginPath {
            get {
                return pathMap["AppPluginPath"];
            }
        }

        public static string AppImagePath {
            get {
                return pathMap["AppImagePath"];
            }
        }

        public static string AppInitialDirPath {
            get {
                return pathMap["AppInitialDirPath"];
            }
        }

        public static string AppConfigPath
        {
            get
            {
                return pathMap["AppConfigPath"];
            }
        }

        public static string AppCachePath
        {
            get
            {
                return pathMap["AppCachePath"];
            }
        }


        public static string AutoPlaylistPath
        {
            get
            {
                return pathMap["AutoPlaylistPath"];
            }
        }

        public static string ConfigFile
        {
            get
            {
                var path = Helper.AppConfigPath;
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                return Path.Combine(path, "MediaBrowserXml.config");
            }
        }


        public static string AppRSSPath
        {
            get
            {
                return pathMap["AppRSSPath"];
            }
        }
        public static bool IsExtenderNativeVideo(string filename)
        {
            string extension = System.IO.Path.GetExtension(filename).ToLower();
            var extensions = Config.Instance.ExtenderNativeTypes.Split(',');
            foreach (var item in extensions)
            {
                if (item == extension)
                {
                    return true;
                }
            }
            return false;
        }

        // Check if this file is an Iso.  (This is not used to determine what files
        // are videos, etc.  It is more used to filter certain cases
        // that are handled differently for Isos).
        public static bool IsIso(string filename)
        {
            string extension = System.IO.Path.GetExtension(filename).ToLower();
            foreach (string e in isoExtensions)
                if (extension == "." + e)
                    return true;
            return false;
        }

        public static List<string> GetIsoFiles(string path)
        {
            List<string> files = new List<string>();
            foreach(string ext in isoExtensions)
                files.AddRange(Directory.GetFiles(path, "*." + ext));
            return files;
        }

        // I left the hardcoded list, cause the failure mode is better, at least it will show
        // videos if the codecs are not installed properly
        public static bool IsVideo(string filename)
        {
            string extension = System.IO.Path.GetExtension(filename).ToLower();

            switch (extension)
            {
                // special case so DVD files are never considered videos
                case ".vob":
                case ".bup":
                case ".ifo":
                    return false;
                case ".rmvb":
                case ".mov":
                case ".avi":
                case ".mpg":
                case ".mpeg":
                case ".wmv":
                case ".mp4":
                case ".mkv":
                case ".divx":
                case ".dvr-ms":
                case ".ogm":
                    return true;

                default:

                    bool isVideo;
                    lock (perceivedTypeCache)
                    {
                        if (perceivedTypeCache.TryGetValue(extension, out isVideo))
                        {
                            return isVideo;
                        }
                    }

                    string pt = null;
                    RegistryKey key = Registry.ClassesRoot;
                    key = key.OpenSubKey(extension);
                    if (key != null)
                    {
                        pt = key.GetValue("PerceivedType") as string;
                    }
                    if (pt == null) pt = "";
                    pt = pt.ToLower();

                    lock (perceivedTypeCache)
                    {
                        perceivedTypeCache[extension] = (pt == "video");
                    }

                    return perceivedTypeCache[extension];
            }
            
        }

        private static string myVideosPath = null;
        public static string MyVideosPath
        {
            get
            {
                if (myVideosPath == null)
                {
                    // Missing from System.Environment
                    int CSIDL_MYVIDEO = 0xe;

                    StringBuilder lpszPath = new StringBuilder(260);
                    MediaBrowser.Interop.ShellNativeMethods.SHGetFolderPath(IntPtr.Zero, CSIDL_MYVIDEO, IntPtr.Zero, 0, lpszPath);
                    myVideosPath = lpszPath.ToString();
                }
                return myVideosPath;

            }
        }

        public static bool IsDvd(String filename)
        {
            string extension = System.IO.Path.GetExtension(filename).ToLower();
            return extension == ".vob";
        }

        public static bool IsShortcut(string filename)
        {
            return System.IO.Path.GetExtension(filename).ToLower() == ".lnk";
        }

        internal static bool IsVirtualFolder(string filename)
        {
            return System.IO.Path.GetExtension(filename).ToLower() == ".vf";
        }

        public static string ResolveShortcut(string filename)
        {
            return MediaBrowser.Interop.ShortcutNativeMethods.ResolveShortcut(filename);
        }

        public static bool ContainsFile(string path, string filter)
        {
            if (Directory.Exists(path))
                return Directory.GetFiles(path, filter).Length > 0;
            else
                return false;
        }

        public static bool IsFolder(string path)
        {  
          return (Directory.Exists(path)); 
        }

        public static bool IsFolder(FileSystemInfo fsi)
        {
            return ((fsi.Attributes & FileAttributes.Directory) == FileAttributes.Directory);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="files">A pre obtained list of the files in the path folder if available, else null</param>
        /// <param name="folders">A pre obtained list of folders in the path folder if available, else null</param>
        /// <returns></returns>
        public static bool IsDvDFolder(string path,string[] files, string[] folders)
        {
            if (files == null)
                files = Directory.GetFiles(path);
            foreach (string f in files)
                if ((f.Length > 4) && (f.Substring(f.Length - 4).ToLower() == ".vob"))
                    return true;
            if (folders == null)
                folders = Directory.GetDirectories(path);
            foreach (string f in folders)
                if (f.ToUpper().EndsWith("VIDEO_TS"))
                    return true;
            return false;
        }

        public static bool IsBluRayFolder(string path, string[] folders)
        {
            if (folders == null)
                folders = Directory.GetDirectories(path);
            foreach (string f in folders)
                if (f.ToUpper().EndsWith("BDMV"))
                    return true;
            return false;
        }

        public static bool IsHDDVDFolder(string path,  string[] folders)
        {
            if (folders == null)
                folders = Directory.GetDirectories(path);
            foreach (string f in folders)
                if (f.ToUpper().EndsWith("HVDVD_TS"))
                    return true;
            return false; 
        }

        public static int IsoCount(string path, string[] files)
        {
            if (files == null)
            {
                if (Directory.Exists(path))
                {
                    return GetIsoFiles(path).Count;
                }
                else
                    return 0;
            }
            else
            {
                int i = 0;
                foreach (string f in files)
                    if (f.Length > 4)
                    {
                        string ext = f.Substring(f.Length - 4).ToLower();
                        foreach(string e in isoExtensions)
                            if (ext == "." + e)
                            {
                                i++;
                                break;
                            }
                    }
                return i;
            }  
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="files">A pre obtained list of the files in the path folder if available, else null</param>
        /// <param name="folders">A pre obtained list of folders in the path folder if available, else null</param>
        /// <returns></returns>
        public static bool ContainsNestedDvdOrIso(string path, string[] files, string[] folders)
        {
            if (files == null)
                files = Directory.GetFiles(path);
            if (IsoCount(path, files) > 0)
                return true;
            if (folders == null)
                folders = Directory.GetDirectories(path);
            if (IsDvDFolder(path, files, folders))
                return true;
            if (IsBluRayFolder(path,  folders))
                return true;
            if (IsHDDVDFolder(path, folders))
                return true;
            
            foreach (string f in folders)
            {
                if (ContainsNestedDvdOrIso(f, null,null))
                    return true;
            }
            return false;  
        }


        static Regex commentExpression = new Regex(@"(\[[^\]]*\])");
        public static string RemoveCommentsFromName(string name)
        {
            return name == null ? null : commentExpression.Replace(name, "");
        }

        internal static bool HasNoAutoPlaylistFile(string path, string[] files)
        {
            foreach (string file in files)
                if (file.ToLower().EndsWith("noautoplaylist"))
                    return true;
            return false;
        }

        internal static bool IsRoot(string path)
        {
            return (Config.Instance.InitialFolder==path) || (Config.Instance.InitialFolder == Helper.MY_VIDEOS && path == Helper.MyVideosPath);
        }

        private static readonly Regex alphaNumeric = new Regex("[^a-zA-Z0-9]");
        public static bool IsAlphaNumeric(string str)
        {
            return (!alphaNumeric.IsMatch(str));
        }
    }
}
