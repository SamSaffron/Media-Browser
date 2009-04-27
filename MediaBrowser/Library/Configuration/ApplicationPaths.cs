using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace MediaBrowser.Library.Configuration {
    public static class ApplicationPaths {
        static Dictionary<string, string> pathMap;

        static string[,] tree = { 
                    { "AppConfigPath",       "app_data",         "MediaBrowser"  }, 
                    { "AppCachePath",        "AppConfigPath",    "Cache"         },
                    { "AppUserSettingsPath", "AppConfigPath",    "Cache"           },
                    { "AutoPlaylistPath",    "AppCachePath",     "autoPlaylists" }, 
                    { "AppImagePath",        "AppConfigPath",    "ImageCache"},
                    { "AppInitialDirPath",   "AppConfigPath",    "StartupFolder" },
                    { "AppPluginPath",       "AppConfigPath",    "Plugins" },
                    { "AppRSSPath",          "AppConfigPath",    "RSS"},
                    { "AppLogPath",          "AppConfigPath",    "Logs"}
            };


        static ApplicationPaths() {
            pathMap = new Dictionary<string, string>();
            pathMap["app_data"] = System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData);
            BuildTree();
        }

        static void BuildTree() {
            for (int i = 0; i <= tree.GetUpperBound(0); i++) {
                var e = Path.Combine(pathMap[tree[i, 1]], tree[i, 2]);
                if (!Directory.Exists(e)) {
                    Directory.CreateDirectory(e);
                }
                pathMap[tree[i, 0]] = e;
            }
        }


        public static void SetUserSettingsPath(string path) {
            Debug.Assert(Directory.Exists(path));

            pathMap["AppUserSettingsPath"] = path;
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

        public static string AppConfigPath {
            get {
                return pathMap["AppConfigPath"];
            }
        }

        public static string AppCachePath {
            get {
                return pathMap["AppCachePath"];
            }
        }


        public static string AutoPlaylistPath {
            get {
                return pathMap["AutoPlaylistPath"];
            }
        }

        public static string AppUserSettingsPath {
            get {
                return pathMap["AppUserSettingsPath"];
            }
        }


        public static string ConfigFile {
            get {
                var path = AppConfigPath;
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                return Path.Combine(path, "MediaBrowserXml.config");
            }
        }


        public static string AppRSSPath {
            get {
                return pathMap["AppRSSPath"];
            }
        }

    }
}
