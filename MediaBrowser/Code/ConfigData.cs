using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Configuration;
using System.Reflection;
using System.Xml;

using Microsoft.MediaCenter.UI;
using MediaBrowser.Library;
using MediaBrowser.LibraryManagement;
using System.Xml.Serialization;
using MediaBrowser.Library.Playables;

namespace MediaBrowser
{
    [Serializable]
    public class ConfigData
    {
        public bool IsFirstRun = true;
        public string ImageByNameLocation = "";
        public Vector3 OverScanScaling = new Vector3(1, 1, 1);
        public Inset OverScanPadding = new Inset(0, 0, 0, 0);
        public bool EnableTraceLogging = false;
        public Size DefaultPosterSize = new Size(220, 220);
        public Size GridSpacing = new Size(0, 0);
        public float MaximumAspectRatioDistortion = 0.05F;
        public bool EnableTranscode360 = true;
        public string ExtenderNativeTypes = ".dvr-ms,.wmv";
        public bool TransparentBackground = false;
        public bool DimUnselectedPosters = true;
        public bool EnableNestedMovieFolders = true;
        public bool EnableMoviePlaylists = true;
        public int PlaylistLimit = 2;
        public string InitialFolder = Helper.MY_VIDEOS;
        public bool EnableUpdates = true;
        public bool EnableBetas = false;
        public string DaemonToolsLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),"DAEMON Tools Lite\\daemon.exe");
        public string DaemonToolsDrive = "E";
        public bool EnableAlphanumericSorting = true;
        public bool EnableListViewTicks = false;
        public Colors ListViewWatchedColor = Colors.LightSkyBlue;
        public bool ShowUnwatchedCount = true;
        public bool ShowWatchedTickOnFolders = true;
        public bool ShowWatchTickInPosterView = true;
        public bool DefaultToFirstUnwatched = false;
        public bool AutoEnterSingleDirs = false; 
        public DateTime AssumeWatchedBefore = DateTime.Today.AddYears(-2).AddDays(1-DateTime.Today.Day);
        public bool InheritDefaultView = true;
        public string DefaultViewType = ViewTypes.Poster.ToString();
        public bool DefaultShowLabels = true;
        public bool DefaultVerticalScroll = false;
        public int BreadcrumbCountLimit = 2;
        public string SortRemoveCharacters = ",|&|-|{|}";
        public string SortReplaceCharacters = ".|+|%";
        public string SortReplaceWords = "the|a|an";
        public bool AllowInternetMetadataProviders = true;
        public bool EnableFileWatching = false;
        public int ThumbStripPosterWidth = 470;
        public bool RememberIndexing = false;
        public bool ShowIndexWarning = true;
        public double IndexWarningThreshold = 0.1;
        public string PreferredMetaDataLanguage = "en";
        public List<ExternalPlayer> ExternalPlayers = new List<ExternalPlayer>();
        public string Theme = "Default";
        public string FontTheme = "Default";
        public bool ShowClock = false;
        public bool EnableAdvancedCmds = false;
        public bool Advanced_EnableDelete = false;
        public bool UseAutoPlayForIso = false;
        public bool ShowBackdrop = true;
        public string InitialBreadcrumbName = "Media Library";

        public string CentralisedCache = null;
        public string ViewTheme = "Default";
        public bool MaintainPosterAspectRatio = true;
        public int AlphaBlending = 50;
        public bool ShowConfigButton = false;
        
        public class ExternalPlayer
        {
            public PlayableExternal.MediaTypes MediaType { get; set; }
            public string Command { get; set; }
            public string Args { get; set; }
        }

        public ConfigData()
        {
            
        }

        public static ConfigData FromFile(string file)
        {
            if (!File.Exists(file))
            {
                ConfigData d = new ConfigData();
                d.Save(file);
            }
            XmlSerializer xs = new XmlSerializer(typeof(ConfigData));
            using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return (ConfigData)xs.Deserialize(fs);
            }
        }

        /// <summary>
        /// Write current config to file
        /// </summary>
        public void Save(string file)
        {
            XmlSerializer xs = new XmlSerializer(typeof(ConfigData));
            using (FileStream fs = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                xs.Serialize(fs, this);
            }
        }
    }
}
