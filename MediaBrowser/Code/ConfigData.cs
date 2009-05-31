using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Configuration;
using System.Reflection;
using System.Xml;

using MediaBrowser.Code.ShadowTypes;
using MediaBrowser.Library;
using MediaBrowser.LibraryManagement;
using System.Xml.Serialization;
using MediaBrowser.Library.Playables;
using MediaBrowser.Library.Configuration;

namespace MediaBrowser
{
    [Serializable]
    public class ConfigData
    {
        public bool EnableRootPage = false;
        public bool IsFirstRun = true;
        public string ImageByNameLocation = "";
        public Vector3 OverScanScaling = new Vector3() {X=1, Y=1, Z=1};
        public Inset OverScanPadding = new Inset();
        public bool EnableTraceLogging = false;
        public Size DefaultPosterSize = new Size() {Width=220, Height=220};
        public Size GridSpacing = new Size();
        public float MaximumAspectRatioDistortion = 0.2F;
        public bool EnableTranscode360 = true;
        public string ExtenderNativeTypes = ".dvr-ms,.wmv";
        public bool TransparentBackground = false;
        public bool DimUnselectedPosters = true;
        public bool EnableNestedMovieFolders = true;
        public bool EnableMoviePlaylists = true;
        public int PlaylistLimit = 2;
        public string InitialFolder = ApplicationPaths.AppInitialDirPath;
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
        public DateTime AssumeWatchedBefore = DateTime.Today.AddYears(-1);
        public bool InheritDefaultView = true;
        public string DefaultViewType = ViewType.Poster.ToString();
        public bool DefaultShowLabels = false;
        public bool DefaultVerticalScroll = false;
        public int BreadcrumbCountLimit = 2;
        public string SortRemoveCharacters = ",|&|-|{|}";
        public string SortReplaceCharacters = ".|+|%";
        public string SortReplaceWords = "the|a|an";
        public bool AllowInternetMetadataProviders = true;
        public bool EnableFileWatching = false;
        public int ThumbStripPosterWidth = 550;
        public bool RememberIndexing = false;
        public bool ShowIndexWarning = true;
        public double IndexWarningThreshold = 0.1;
        public string PreferredMetaDataLanguage = "en";
        public List<ExternalPlayer> ExternalPlayers = new List<ExternalPlayer>();
        public string Theme = "Default";
        public string FontTheme = "Default";
        public bool ShowClock = true;
        public bool EnableAdvancedCmds = false;
        public bool Advanced_EnableDelete = false;
        public bool UseAutoPlayForIso = false;
        public bool ShowBackdrop = true;
        public string InitialBreadcrumbName = "Media";

        public string UserSettingsPath = null;
        public string ViewTheme = "Default";
        public int AlphaBlending = 50;
        public bool ShowConfigButton = false;

        public bool EnableSyncViews = true;
        public string YahooWeatherFeed = "UKXX0085";
        public string YahooWeatherUnit = "c";

        public string PodcastHome = ApplicationPaths.DefaultPodcastPath;
        public bool HideFocusFrame = false;

        public class ExternalPlayer
        {
            public MediaType MediaType { get; set; }
            public string Command { get; set; }
            public string Args { get; set; }

            public override string ToString()
            {
                return MediaType.ToString();
            }
        }

        // for the serializer
        public ConfigData ()
	    {

	    }

        public ConfigData(string file)
        {
            this.file = file;
        }

        string file;

        public static ConfigData FromFile(string file)
        {
            if (!File.Exists(file))
            {
                ConfigData d = new ConfigData(file);
                d.Save();
            }
            XmlSerializer xs = new XmlSerializer(typeof(ConfigData));
            using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return (ConfigData)xs.Deserialize(fs);
            }
        }

        public void Save() {
            Save(file); 
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
