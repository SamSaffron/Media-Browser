using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Security.Cryptography;
using Microsoft.MediaCenter.UI;
using Microsoft.Win32;
using System.Reflection;
using System.Drawing;
using System.Drawing.Imaging;


namespace MediaBrowser.LibraryManagement
{
    using Image = Microsoft.MediaCenter.UI.Image;
    using System.Drawing.Drawing2D;
    using System.Diagnostics;
    using System.Text.RegularExpressions;
    using MediaBrowser.Util;

    public static class Helper
    {
        public static Dictionary<string, bool> perceivedTypeCache = new Dictionary<string, bool>();
        //private static MD5CryptoServiceProvider CryptoService = new MD5CryptoServiceProvider();

        #region Signitures imported from http://pinvoke.net

        [DllImport("shfolder.dll", CharSet = CharSet.Auto)]
        internal static extern int SHGetFolderPath(IntPtr hwndOwner, int nFolder, IntPtr hToken, int dwFlags, StringBuilder lpszPath);

        [Flags()]
        enum SLGP_FLAGS
        {
            /// <summary>Retrieves the standard short (8.3 format) file name</summary>
            SLGP_SHORTPATH = 0x1,
            /// <summary>Retrieves the Universal Naming Convention (UNC) path name of the file</summary>
            SLGP_UNCPRIORITY = 0x2,
            /// <summary>Retrieves the raw path name. A raw path is something that might not exist and may include environment variables that need to be expanded</summary>
            SLGP_RAWPATH = 0x4
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        struct WIN32_FIND_DATAW
        {
            public uint dwFileAttributes;
            public long ftCreationTime;
            public long ftLastAccessTime;
            public long ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }

        [Flags()]

        enum SLR_FLAGS
        {
            /// <summary>
            /// Do not display a dialog box if the link cannot be resolved. When SLR_NO_UI is set,
            /// the high-order word of fFlags can be set to a time-out value that specifies the
            /// maximum amount of time to be spent resolving the link. The function returns if the
            /// link cannot be resolved within the time-out duration. If the high-order word is set
            /// to zero, the time-out duration will be set to the default value of 3,000 milliseconds
            /// (3 seconds). To specify a value, set the high word of fFlags to the desired time-out
            /// duration, in milliseconds.
            /// </summary>
            SLR_NO_UI = 0x1,
            /// <summary>Obsolete and no longer used</summary>
            SLR_ANY_MATCH = 0x2,
            /// <summary>If the link object has changed, update its path and list of identifiers.
            /// If SLR_UPDATE is set, you do not need to call IPersistFile::IsDirty to determine
            /// whether or not the link object has changed.</summary>
            SLR_UPDATE = 0x4,
            /// <summary>Do not update the link information</summary>
            SLR_NOUPDATE = 0x8,
            /// <summary>Do not execute the search heuristics</summary>
            SLR_NOSEARCH = 0x10,
            /// <summary>Do not use distributed link tracking</summary>
            SLR_NOTRACK = 0x20,
            /// <summary>Disable distributed link tracking. By default, distributed link tracking tracks
            /// removable media across multiple devices based on the volume name. It also uses the
            /// Universal Naming Convention (UNC) path to track remote file systems whose drive letter
            /// has changed. Setting SLR_NOLINKINFO disables both types of tracking.</summary>
            SLR_NOLINKINFO = 0x40,
            /// <summary>Call the Microsoft Windows Installer</summary>
            SLR_INVOKE_MSI = 0x80
        }


        /// <summary>The IShellLink interface allows Shell links to be created, modified, and resolved</summary>
        [ComImport(), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("000214F9-0000-0000-C000-000000000046")]
        interface IShellLinkW
        {
            /// <summary>Retrieves the path and file name of a Shell link object</summary>
            void GetPath([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out WIN32_FIND_DATAW pfd, SLGP_FLAGS fFlags);
            /// <summary>Retrieves the list of item identifiers for a Shell link object</summary>
            void GetIDList(out IntPtr ppidl);
            /// <summary>Sets the pointer to an item identifier list (PIDL) for a Shell link object.</summary>
            void SetIDList(IntPtr pidl);
            /// <summary>Retrieves the description string for a Shell link object</summary>
            void GetDescription([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
            /// <summary>Sets the description for a Shell link object. The description can be any application-defined string</summary>
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            /// <summary>Retrieves the name of the working directory for a Shell link object</summary>
            void GetWorkingDirectory([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
            /// <summary>Sets the name of the working directory for a Shell link object</summary>
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            /// <summary>Retrieves the command-line arguments associated with a Shell link object</summary>
            void GetArguments([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
            /// <summary>Sets the command-line arguments for a Shell link object</summary>
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            /// <summary>Retrieves the hot key for a Shell link object</summary>
            void GetHotkey(out short pwHotkey);
            /// <summary>Sets a hot key for a Shell link object</summary>
            void SetHotkey(short wHotkey);
            /// <summary>Retrieves the show command for a Shell link object</summary>
            void GetShowCmd(out int piShowCmd);
            /// <summary>Sets the show command for a Shell link object. The show command sets the initial show state of the window.</summary>
            void SetShowCmd(int iShowCmd);
            /// <summary>Retrieves the location (path and index) of the icon for a Shell link object</summary>
            void GetIconLocation([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath,
                int cchIconPath, out int piIcon);
            /// <summary>Sets the location (path and index) of the icon for a Shell link object</summary>
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            /// <summary>Sets the relative path to the Shell link object</summary>
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
            /// <summary>Attempts to find the target of a Shell link, even if it has been moved or renamed</summary>
            void Resolve(IntPtr hwnd, SLR_FLAGS fFlags);
            /// <summary>Sets the path and file name of a Shell link object</summary>
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);

        }

        [ComImport, Guid("0000010c-0000-0000-c000-000000000046"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IPersist
        {
            [PreserveSig]
            void GetClassID(out Guid pClassID);
        }


        [ComImport, Guid("0000010b-0000-0000-C000-000000000046"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IPersistFile : IPersist
        {
            new void GetClassID(out Guid pClassID);
            [PreserveSig]
            int IsDirty();

            [PreserveSig]
            void Load([In, MarshalAs(UnmanagedType.LPWStr)]
            string pszFileName, uint dwMode);

            [PreserveSig]
            void Save([In, MarshalAs(UnmanagedType.LPWStr)] string pszFileName,
                [In, MarshalAs(UnmanagedType.Bool)] bool fRemember);

            [PreserveSig]
            void SaveCompleted([In, MarshalAs(UnmanagedType.LPWStr)] string pszFileName);

            [PreserveSig]
            void GetCurFile([In, MarshalAs(UnmanagedType.LPWStr)] string ppszFileName);
        }

        const uint STGM_READ = 0;
        const int MAX_PATH = 260;

        // CLSID_ShellLink from ShlGuid.h 
        [
            ComImport(),
            Guid("00021401-0000-0000-C000-000000000046")
        ]
        public class ShellLink
        {
        }

        #endregion

        static Dictionary<string, string> PathMap;

        static Helper()
        {
            PathMap = new Dictionary<string, string>();
            PathMap["app_data"] = System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData);

            string[,] tree = { 
                    { "AppConfigPath",      "app_data",         "MediaBrowser"  }, 
                    //{ "AppPrefsPath",       "AppConfigPath",    "Prefs"         },
                    //{ "AppPlayStatePath",   "AppConfigPath",    "PlayState"     },
                    { "AppCachePath",       "AppConfigPath",    "Cache"         },
                    //{ "AppPosterThumbPath", "AppCachePath",     "PosterThumb"   },
                    { "AutoPlaylistPath",   "AppCachePath",     "autoPlaylists" }
            };

            for (int i = 0; i <= tree.GetUpperBound(0); i++)
            {
                var e = Path.Combine(PathMap[tree[i, 1]], tree[i, 2]);
                if (!Directory.Exists(e))
                {
                    Directory.CreateDirectory(e);
                }
                PathMap[tree[i, 0]] = e;
            }
        }

        public static string AppDataPath
        {
            get
            {
                return PathMap["AppConfigPath"];
            }
        }

        public static string AppConfigPath
        {
            get
            {
                return PathMap["AppConfigPath"];
            }
        }

        public static string AppCachePath
        {
            get
            {
                return PathMap["AppCachePath"];
            }
        }


        public static string AutoPlaylistPath
        {
            get
            {
                return PathMap["AutoPlaylistPath"];
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
            return extension == ".iso";
        }

        // I left the hardcoded list, cause the failure mode is better, at least it will show
        // videos if the codecs are not installed properly
        public static bool IsVideo(string filename)
        {
            //using (new Profiler(filename))
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
                    //case ".iso": // these are not directly playable and need to be handled differently
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
        }


        public const string MY_VIDEOS = "MyVideos";

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
                    SHGetFolderPath(IntPtr.Zero, CSIDL_MYVIDEO, IntPtr.Zero, 0, lpszPath);
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

        /*
        public static bool IsImage(string filename)
        {
            string extension = System.IO.Path.GetExtension(filename).ToLower();

            switch (extension)
            {
                case ".jpg":
                case ".gif":
                case ".jpeg":
                case ".png":
                case ".bmp":
                    return true;

                default:
                    return false;
            }
        }*/


        /*
        internal static string GetRandomNames(List<IFolderItem> items, int maxLength)
        {
            var itemsCopy = items.ToArray();
            Shuffle(itemsCopy);
            var isFirst = true;
            var len = 0;
            var count = 0; 
            
            for (int i = 0; i < itemsCopy.Length ; i++)
            {
                if (len + itemsCopy[i].Description.Length + 3 < maxLength)
                {
                    if (!isFirst)
                    {
                        len += 2; 
                    }
                    len += itemsCopy[i].Description.Length;
                    isFirst = false;
                    count++;
                }
                else
                {
                    break; 
                }
            }

            isFirst = true;

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                if (!isFirst)
                {
                    sb.Append(", ");
                }
                sb.Append(itemsCopy[i].Description);
                isFirst = false;
            }
            sb.Append(".");

            return sb.ToString();
        }
        
        public static void Shuffle<T>(T[] array)
        {
            Random rng = new Random();
            int n = array.Length;
            while (n-- > 0)
            {
                int k = rng.Next(n + 1);  // 0 <= k <= n (!)
                T temp = array[n];
                array[n] = array[k];
                array[k] = temp;
            }
        }
        */
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
            ShellLink link = new ShellLink();
            ((IPersistFile)link).Load(filename, STGM_READ);
            // TODO: if I can get hold of the hwnd call resolve first. This handles moved and renamed files.  
            // ((IShellLinkW)link).Resolve(hwnd, 0) 
            StringBuilder sb = new StringBuilder(MAX_PATH);
            WIN32_FIND_DATAW data = new WIN32_FIND_DATAW();
            ((IShellLinkW)link).GetPath(sb, sb.Capacity, out data, 0);
            return sb.ToString();
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
            //using (new Profiler(path))
            {
                return (Directory.Exists(path)); // && ((new FileInfo(path).Attributes & FileAttributes.Directory) == FileAttributes.Directory));
            }
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
            //using (new Profiler(path))
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
                /*
                if (ContainsFile(path, "*.vob"))
                    return true;
                else
                    return (ContainsFile(Path.Combine(path, "VIDEO_TS"), "*.vob"));
                 */
            }
        }

        public static int IsoCount(string path, string[] files)
        {
            //using (new Profiler(path))
            {
                if (files == null)
                {
                    if (Directory.Exists(path))
                        return Directory.GetFiles(path, "*.iso").Length;
                    else
                        return 0;
                }
                else
                {
                    int i = 0;
                    foreach (string f in files)
                        if ((f.Length > 4) && (f.Substring(f.Length - 4).ToLower() == ".iso"))
                            i++;
                    return i;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="files">A pre obtained list of the files in the path folder if available, else null</param>
        /// <param name="folders">A pre obtained list of folders in the path folder if available, else null</param>
        /// <returns></returns>
        public static bool ContainsSingleMovie(string path, string[] files, string[] folders)
        {
            //using (new Profiler(path))
            {
                int i = 0;
                int limit = Config.Instance.PlaylistLimit;
                if (!Config.Instance.EnableMoviePlaylists)
                    limit = 1;
                foreach (string file in EnumerateVideoFiles(path, files,folders, Config.Instance.EnableNestedMovieFolders))
                {
                    i++;
                    if (i > limit)
                        return false;
                }
                if (Helper.ContainsNestedDvdOrIso(path,files, folders))
                    return false;
                return (i > 0);
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
            //using (new Profiler(path))
            {
                //if (!Directory.Exists(path))
                //return false;
                if (files == null)
                    files = Directory.GetFiles(path);
                if (IsoCount(path, files) > 0)
                    return true;
                if (folders == null)
                    folders = Directory.GetDirectories(path);
                if (IsDvDFolder(path, files, folders))
                    return true;
                
                foreach (string f in folders)
                {
                    if (ContainsNestedDvdOrIso(f, null,null))
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="files">A pre obtained list of the files in the path folder if available, else null</param>
        /// <param name="folders">A pre obtained list of folders in the path folder if available, else null</param>
        /// <param name="recursive"></param>
        /// <returns></returns>
        public static IEnumerable<string> EnumerateVideoFiles(string path, string[] files,string[] folders, bool recursive)
        {
            //if (Directory.Exists(path))
            {
                List<string> nestedFolders = new List<string>();

                if (files==null)
                    files = Directory.GetFiles(path);                
                foreach (string file in files)
                {
                    if (Helper.IsVideo(file))
                        yield return file;

                    if (recursive)
                    {
                        if (Helper.IsShortcut(file))
                        {
                            var resolvedFolder = Helper.ResolveShortcut(file);
                            if (Helper.IsFolder(resolvedFolder))
                            {
                                nestedFolders.Add(resolvedFolder);
                            }
                        }
                    }
                }
                if (recursive)
                {
                    if (folders == null)
                        folders = Directory.GetDirectories(path);
                    nestedFolders.AddRange(folders);
                    foreach (string folder in nestedFolders)
                        foreach (string s in EnumerateVideoFiles(folder, null,null, recursive))
                            yield return s;
                }
            }
        }


        /// <summary>
        /// Used to detect paths that represent episodes, need to make sure they don't also
        /// match movie titles like "2001 A Space..."
        /// Currently we limit the numbers here to 2 digits to try and avoid this
        /// </summary>
        /// <remarks>
        /// The order here is important, if the order is changed some of the later
        /// ones might incorrectly match things that higher ones would have caught.
        /// The most restrictive expressions should appear first
        /// </remarks>
        private static readonly Regex[] episodeExpressions = new Regex[] {
                         new Regex(@".*\\[s|S]?(?<seasonnumber>\d{1,2})[x|X](?<epnumber>\d{1,2})[^\\]*$"),   // 01x02 blah.avi S01x01 balh.avi
                        new Regex(@".*\\[s|S](?<seasonnumber>\d{1,2})x?[e|E](?<epnumber>\d{1,2})[^\\]*$"), // S01E02 blah.avi, S01xE01 blah.avi
                        new Regex(@".*\\(?<seriesname>[^\\]*)[s|S]?(?<seasonnumber>\d{1,2})[x|X](?<epnumber>\d{1,2})[^\\]*$"),   // 01x02 blah.avi S01x01 balh.avi
                        new Regex(@".*\\(?<seriesname>[^\\]*)[s|S](?<seasonnumber>\d{1,2})[x|X|\.]?[e|E](?<epnumber>\d{1,2})[^\\]*$") // S01E02 blah.avi, S01xE01 blah.avi
        };
        /// <summary>
        /// To avoid the following matching moview they are only valid when contained in a folder which has been matched as a being season
        /// </summary>
        private static readonly Regex[] episodeExpressionsInASeasonFolder = new Regex[] {
                        new Regex(@".*\\(?<epnumber>\d{1,2})\s?-\s?[^\\]*$"), // 01 - blah.avi, 01-blah.avi
                        new Regex(@".*\\(?<epnumber>\d{1,2})[^\d\\]+[^\\]*$"), // 01.avi, 01.blah.avi "01 - 22 blah.avi" 
                        new Regex(@".*\\(?<seasonnumber>\d)(?<epnumber>\d{1,2})[^\d\\]+[^\\]*$") // 01.avi, 01.blah.avi

        };

        public static bool IsEpisode(string fullPath)
        {
            //using (new Profiler(fullPath))
            {
                bool isInSeason = IsSeasonFolder(Path.GetDirectoryName(fullPath));
                if (isInSeason)
                    return true;
                else if (EpisodeNumberFromFile(fullPath, isInSeason) != null)
                    return true;
                return false;
            }
        }


        /// <summary>
        /// Takes the full path and filename
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static string EpisodeNumberFromFile(string fullPath)
        {
            return EpisodeNumberFromFile(fullPath, IsSeasonFolder(Path.GetDirectoryName(fullPath)));
        }

        private static string EpisodeNumberFromFile(string fullPath, bool isInSeason)
        {
            if (! (IsVideo(fullPath) || IsIso(fullPath)))
                return null;
            string fl = fullPath.ToLower();
            foreach (Regex r in episodeExpressions)
            {
                Match m = r.Match(fl);
                if (m.Success)
                    return m.Groups["epnumber"].Value;
            }
            if (isInSeason)
            {
                foreach (Regex r in episodeExpressionsInASeasonFolder)
                {
                    Match m = r.Match(fl);
                    if (m.Success)
                        return m.Groups["epnumber"].Value;
                }
            }
            

            return null;
        }

        private static readonly Regex[] seasonPathExpressions = new Regex[] {
                        new Regex(@".+\\[s|S]eason\s?(?<seasonnumber>\d{1,2})$"),
                        new Regex(@".+\\[s|S]æson\s?(?<seasonnumber>\d{1,2})$"),
                        new Regex(@".+\\[t|T]emporada\s?(?<seasonnumber>\d{1,2})$"),
                        new Regex(@".+\\[s|S]aison\s?(?<seasonnumber>\d{1,2})$"),
                        new Regex(@".+\\[s|S](?<seasonnumber>\d{1,2})$")
        };

        public static bool IsSeasonFolder(string path)
        {
            //using (new Profiler(path))
            {
                foreach (Regex r in seasonPathExpressions)
                    if (r.IsMatch(path))
                        return true;
                return false;
            }
        }

        /// <summary>
        /// Takes the single folder name not the full path
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        public static string SeasonNumberFromFolderName(string fullpath)
        {

            foreach (Regex r in seasonPathExpressions)
            {
                Match m = r.Match(fullpath);
                if (m.Success)
                    return m.Groups["seasonnumber"].Value;
            }
            return null;
        }

        public static string SeasonNumberFromEpisodeFile(string fullPath)
        {
            string fl = fullPath.ToLower();
            foreach (Regex r in episodeExpressions)
            {
                Match m = r.Match(fl);
                if (m.Success)
                {
                    Group g = m.Groups["seasonnumber"];
                    if (g != null)
                        return g.Value;
                    else
                        return null;
                }
            }
            return null;
        }

        public static bool IsSeriesFolder(string path,string[] files, string[] folders)
        {
            //using (new Profiler(path))
            {
                //if (!Directory.Exists(path))
                //return false;
                if (IsSeasonFolder(path))
                    return false;
                //string[] folders = Directory.GetDirectories(path);
                int i = 0;

                foreach (string folder in folders)
                {
                    if (IsSeasonFolder(folder))
                        return true; // we have found at least one season folder
                    else
                        i++;
                    if (i >= 3)
                        return false; // a folder with more than 3 non-season folders in will not becounted as a series
                }

                if (files==null)
                    files = Directory.GetFiles(path);
                foreach (string file in files)
                {
                    if (EpisodeNumberFromFile(file, false) != null)
                        return true;
                }
                return false;
            }
        }

        static Regex commentExpression = new Regex(@"(\[[^\]]*\])");
        public static string RemoveCommentsFromName(string name)
        {
            return name == null ? null : commentExpression.Replace(name, "");
        }

        internal static bool HasNoAutoPlaylistFile(string path, string[] files)
        {
            //using (new Profiler(path))
            {
                foreach (string file in files)
                    if (file.ToLower().EndsWith("noautoplaylist"))
                        return true;
                return false;
            }
            //string file = Path.Combine(path, "noautoplaylist");
            //return File.Exists(file);
        }

        internal static bool IsRoot(string path)
        {
            return (Config.Instance.InitialFolder==path) || (Config.Instance.InitialFolder == Helper.MY_VIDEOS && path == Helper.MyVideosPath);
        }
    }
}
