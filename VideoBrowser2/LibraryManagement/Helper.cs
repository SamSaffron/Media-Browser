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


namespace SamSoft.VideoBrowser.LibraryManagement
{
    using Image = Microsoft.MediaCenter.UI.Image;
    using System.Drawing.Drawing2D;
    using System.Diagnostics;

    public static class Helper
    {
        private static MD5CryptoServiceProvider CryptoService = new MD5CryptoServiceProvider();

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
            PathMap["app_data"] = System.Environment.GetFolderPath(
                        System.Environment.SpecialFolder.CommonApplicationData
                    );
            
            string[,] tree = { 
                    { "AppConfigPath", "app_data", "VideoBrowser"}, 
                    { "AppPrefsPath", "AppConfigPath", "Prefs" },
                    { "AppPlayStatePath", "AppConfigPath", "PlayState" },
                    { "AppCachePath", "AppConfigPath", "Cache" },
                    { "AppPosterThumbPath", "AppCachePath", "PosterThumb" },
                    { "AutoPlaylistPath", "AppCachePath", "AutoPlaylists" }
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


        public static string AppPosterThumbPath
        {
            get
            {
                return PathMap["AppPosterThumbPath"];    
            }
        }

        public static string AppPrefsPath
        {
            get
            {
                return PathMap["AppPrefsPath"];
            }
        }

       
        public static string AppPlayStatePath
        {
            get
            {
                return PathMap["AppPlayStatePath"];
            }
        }

     
        public static string AutoPlaylistPath
        {
            get
            {
               return PathMap["AppPlayStatePath"];  
            }
        }

        // return null if no thumbnail found otherwise thumbnail name 
        public static string GetThumb(string path, bool isFolder)
        {
            if (isFolder)
            {
                string[] paths = new string[] { 
                System.IO.Path.Combine(path, "folder.jpg"), 
                System.IO.Path.Combine(path, "folder.jpeg") 
            };

                foreach (string thumb in paths)
                {
                    if (File.Exists(thumb))
                    {
                        return thumb;
                    }
                }
            }
            else
            {
                // TODO: Possibly allow for filename.jpg
            } 

            return null;
        }

        public static bool IsShortcut(string filename)
        {
            return System.IO.Path.GetExtension(filename).ToLower() == ".lnk";
        }

        public static Dictionary<string, bool> perceivedTypeCache = new Dictionary<string, bool>();

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
        public static bool isIso(string filename)
        {
            string extension = System.IO.Path.GetExtension(filename).ToLower();

            return extension == ".iso";
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
                case ".iso":
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
        }

        

        /*
        internal static void ResizeImage(string source, string destination, int width, int height)
        {
            try
            {
                System.Drawing.Image image = System.Drawing.Image.FromFile(source);
                System.Drawing.Image thumbnail = new Bitmap(width, height);
                Graphics graphic = Graphics.FromImage(thumbnail);

                graphic.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphic.SmoothingMode = SmoothingMode.HighQuality;
                graphic.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphic.CompositingQuality = CompositingQuality.HighQuality;

                graphic.DrawImage(image, 0, 0, width, height);

                thumbnail.Save(destination, ImageFormat.Png);

            }
            catch (Exception e)
            {
                Trace.WriteLine("Failed to resize image: " + e.ToString());
            }
 
        }*/

        static Image defImage = null;
        internal static Microsoft.MediaCenter.UI.Image GetMCMLThumb(string path, bool isVideo)
        {
            // Do we have a thumbnail path?
            if (!String.IsNullOrEmpty(path))
            {
                // yes, so lets say the string is not empty and construct the resource
                // to build the image from.
                try
                {
                    // This throws an exception is the file does not exist (and possibly
                    // if the file is not an image?)
                    return new Image("file://" + path);
                }
                catch (Exception)
                {
                    // If that failed, treat the rest of the function as if the path was empty.
                }
            }
            
            if (isVideo)
            {
                if (defImage==null)
                    lock(typeof(Helper))
                        if (defImage == null)
                           defImage = new Image("res://ehres!MOVIE.ICON.DEFAULT.PNG");
                return defImage;
                //resource = "res://ehres!MOVIE.ICON.DEFAULT.PNG";
            }
            else
                return null;
        }

        

        internal static Microsoft.MediaCenter.UI.Image GetMCMLBanner(string path)
        {
            if (!String.IsNullOrEmpty(path))
            {
                try
                {
                    return new Image("file://" + path);
                }
                catch (Exception)
                {
                    return null;
                }
            }
            return null;
        }

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


        internal static bool IsVirtualFolder(string filename)
        {
            return System.IO.Path.GetExtension(filename).ToLower() == ".vf"; 
        }

        public static string HashString(string str)
        {
            Byte[] originalBytes = ASCIIEncoding.Default.GetBytes(str);
            Byte[] encodedBytes = CryptoService.ComputeHash(originalBytes);
            return new Guid(encodedBytes).ToString("N");
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
    }
}
