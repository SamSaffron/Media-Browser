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
using MediaBrowser.Attributes;
using Microsoft.MediaCenter;

namespace MediaBrowser
{
   
    public class Config : IModelItem
    {
        private ConfigData data;

        public bool IsFirstRun
        {
            get { return this.data.IsFirstRun; }
            set { if (this.data.IsFirstRun != value) { this.data.IsFirstRun = value; Save(); FirePropertyChanged("HasBeenConfigured"); } }
        }

        [Comment("Dim all unselected posters in poster and thumbstrib views")]
        public bool DimUnselectedPosters
        {
            get { return this.data.DimUnselectedPosters; }
            set { if (this.data.DimUnselectedPosters != value) { this.data.DimUnselectedPosters = value; Save(); FirePropertyChanged("DimUnselectedPosters"); } }
        }


        [Comment(@"Location of images to match to items by name.
            Can be used to provide images for indexing folders - genres, actors, directors etc.
            Should contain folders to match 
            item names each with banner, folder, backdrop images in jpg or png format. The folder 
            name needs to match the name returned by the source of the item (e.g. the folder/filename 
            without extension or name of the indexing folder) this is not necessarily the 
            metadata name displayed. Where names contain characters that are illegal in filenames 
            they should just be removed.")]
        public string ImageByNameLocation
        {
            get { return this.data.ImageByNameLocation; }
            set { if (this.data.ImageByNameLocation!= value) { this.data.ImageByNameLocation = value; Save(); FirePropertyChanged("ImageByNameLocation"); } }
        }

        [Comment(@"Enables you to scan the display to cope with overscan issue, parameter should be of the for x,y,z scaling factors")]
        public Vector3 OverScanScaling
        {
            get { return this.data.OverScanScaling; }
            set { if (this.data.OverScanScaling != value) { this.data.OverScanScaling = value; Save(); FirePropertyChanged("OverScanScaling"); } }
        }
        [Comment("Defines padding to apply round the edge of the screen to cope with overscan issues")]
        public Inset OverScanPadding
        {
            get { return this.data.OverScanPadding; }
            set { if (this.data.OverScanPadding != value) { this.data.OverScanPadding = value; Save(); FirePropertyChanged("OverScanPadding"); } }
        }
        [Comment(@"Enables the writing of trace log files in a production environment to assist with problem solving")]
        public bool EnableTraceLogging
        {
            get { return this.data.EnableTraceLogging; }
            set { if (this.data.EnableTraceLogging != value) { this.data.EnableTraceLogging = value; Save(); FirePropertyChanged("EnableTraceLogging"); } }
        }
        [Comment(@"The default size of posters before change are made to the view settings")]
        public Size DefaultPosterSize
        {
            get { return this.data.DefaultPosterSize; }
            set { if (this.data.DefaultPosterSize != value) { this.data.DefaultPosterSize = value; Save(); FirePropertyChanged("DefaultPosterSize"); } }
        }

        public int DefaultPosterSizeCfg
        {
            get { return this.DefaultPosterSize.Width; }
            set { this.DefaultPosterSize = new Size(value, value); }
        }

        [Comment("Controls the space between items in the poster and thumb strip views")]
        public Size GridSpacing
        {
            get { return this.data.GridSpacing; }
            set { if (this.data.GridSpacing != value) { this.data.GridSpacing = value; Save(); FirePropertyChanged("GridSpacing"); } }
        }

        public int GridSpacingCfg
        {
            get { return GridSpacing.Width; }
            set { this.GridSpacing = new Size(value, value); }
        }

        public int ThumbStripPosterWidth
        {
            get { return this.data.ThumbStripPosterWidth; }
            set { if (this.data.ThumbStripPosterWidth != value) { this.data.ThumbStripPosterWidth = value; Save(); FirePropertyChanged("ThumbStripPosterWidth"); } }
        }

        public bool RememberIndexing
        {
            get { return this.data.RememberIndexing; }
            set { if (this.data.RememberIndexing != value) { this.data.RememberIndexing = value; Save(); FirePropertyChanged("RememberIndexing"); } }
        }

        public bool ShowIndexWarning
        {
            get { return this.data.ShowIndexWarning; }
            set { if (this.data.ShowIndexWarning != value) { this.data.ShowIndexWarning = value; Save(); FirePropertyChanged("ShowIndexWarning"); } }
        }

        public double IndexWarningThreshold
        {
            get { return this.data.IndexWarningThreshold; }
            set { if (this.data.IndexWarningThreshold != value) { this.data.IndexWarningThreshold = value; Save(); FirePropertyChanged("IndexWarningThreshold"); } }
        }

        [Comment(@"Controls the maximum difference between the actual aspect ration of a poster image and the thumbnails being displayed to allow the application to stretch the image non-proportionally.
            x = Abs( (image width/ image height) - (display width / display height) )
            if x is less than the configured value the imae will be stretched non-proportionally to fit the display size")]
        public float MaximumAspectRatioDistortion
        {
            get { return this.data.MaximumAspectRatioDistortion; }
            set { if (this.data.MaximumAspectRatioDistortion != value) { this.data.MaximumAspectRatioDistortion = value; Save(); FirePropertyChanged("MaximumAspectRatioDistortion"); } }
        }
        [Comment(@"Enable transcode 360 support on extenders")]
        public bool EnableTranscode360
        {
            get { return this.data.EnableTranscode360; }
            set { if (this.data.EnableTranscode360 != value) { this.data.EnableTranscode360 = value; Save(); FirePropertyChanged("EnableTranscode360"); } }
        }
        [Comment(@"A lower case comma delimited list of types the extender supports natively. Example: .dvr-ms,.wmv")]
        public string ExtenderNativeTypes
        {
            get { return this.data.ExtenderNativeTypes; }
            set { if (this.data.ExtenderNativeTypes != value) { this.data.ExtenderNativeTypes = value; Save(); FirePropertyChanged("ExtenderNativeTypes"); } }
        }
        [Comment("TransparentBackground [Default Value - False]\n\tTrue: Enables transparent background.\n\tFalse: Use default Video Browser background.")]
        public bool TransparentBackground
        {
            get { return this.data.TransparentBackground; }
            set { if (this.data.TransparentBackground != value) { this.data.TransparentBackground = value; Save(); FirePropertyChanged("TransparentBackground"); } }
        }
        [Comment("Example. If set to true the following will be treated as a movie and an automatic playlist will be created.\n\tIndiana Jones / Disc 1 / a.avi\n\tIndiana Jones / Disc 2 / b.avi")]
        public bool EnableNestedMovieFolders
        {
            get { return this.data.EnableNestedMovieFolders; }
            set { if (this.data.EnableNestedMovieFolders != value) { this.data.EnableNestedMovieFolders = value; Save(); FirePropertyChanged("EnableNestedMovieFolders"); } }
        }
        [Comment("Example. If set to true the following will be treated as a movie and an automatic playlist will be created.\n\tIndiana Jones / a.avi\n\tIndiana Jones / b.avi (This only works for 2 videos (no more))\n**Setting this to false will override EnableNestedMovieFolders if that is enabled.**")]
        public bool EnableMoviePlaylists
        {
            get { return this.data.EnableMoviePlaylists; }
            set { if (this.data.EnableMoviePlaylists != value) { this.data.EnableMoviePlaylists = value; Save(); FirePropertyChanged("EnableMoviePlaylists"); } }
        }
        [Comment("Limit to the number of video files that willbe assumed to be a single movie and create a playlist for")]
        public int PlaylistLimit
        {
            get { return this.data.PlaylistLimit; }
            set { if (this.data.PlaylistLimit != value) { this.data.PlaylistLimit = value; Save(); FirePropertyChanged("PlaylistLimit"); } }
        }
        [Comment("The starting folder for video browser. By default its set to MyVideos.\nCan be set to a folder for example c:\\ or a virtual folder for example c:\\folder.vf")]
        public string InitialFolder
        {
            get { return this.data.InitialFolder; }
            set { if (this.data.InitialFolder != value) { this.data.InitialFolder = value; Save(); FirePropertyChanged("InitialFolder"); } }
        }
        [Comment(@"Flag for auto-updates.  True will auto-update, false will not.")]
        public bool EnableUpdates
        {
            get { return this.data.EnableUpdates; }
            set { if (this.data.EnableUpdates != value) { this.data.EnableUpdates = value; Save(); FirePropertyChanged("EnableUpdates"); } }
        }
        [Comment(@"Flag for beta updates.  True will prompt you to update to beta versions.")]
        public bool EnableBetas
        {
            get { return this.data.EnableBetas; }
            set { if (this.data.EnableBetas != value) { this.data.EnableBetas = value; Save(); FirePropertyChanged("EnableBetas"); } }
        }
        [Comment(@"Set the location of the Daemon Tools binary..")]
        public string DaemonToolsLocation
        {
            get { return this.data.DaemonToolsLocation; }
            set { if (this.data.DaemonToolsLocation != value) { this.data.DaemonToolsLocation = value; Save(); FirePropertyChanged("DaemonToolsLocation"); } }
        }
        [Comment(@"The drive letter of the Daemon Tools virtual drive.")]
        public string DaemonToolsDrive
        {
            get { return this.data.DaemonToolsDrive; }
            set { if (this.data.DaemonToolsDrive != value) { this.data.DaemonToolsDrive = value; Save(); FirePropertyChanged("DaemonToolsDrive"); } }
        }
        [Comment("Flag for alphanumeric sorting.  True will use alphanumeric sorting, false will use alphabetic sorting.\nNote that the sorting algorithm is case insensitive.")]
        public bool EnableAlphanumericSorting
        {
            get { return this.data.EnableAlphanumericSorting; }
            set { if (this.data.EnableAlphanumericSorting != value) { this.data.EnableAlphanumericSorting = value; Save(); FirePropertyChanged("EnableAlphanumericSorting"); } }
        }
        [Comment(@"Enables the showing of tick in the list view for files that have been watched")]
        public bool EnableListViewTicks
        {
            get { return this.data.EnableListViewTicks; }
            set { if (this.data.EnableListViewTicks != value) { this.data.EnableListViewTicks = value; Save(); FirePropertyChanged("EnableListViewTicks"); } }
        }
        [Comment(@"Enables the showing of watched shows in a different color in the list view (Transparent disables it)")]
        public Colors ListViewWatchedColor
        {
            get { return this.data.ListViewWatchedColor; }
            set { if (this.data.ListViewWatchedColor != value) { this.data.ListViewWatchedColor = value; Save(); FirePropertyChanged("ListViewWatchedColor"); FirePropertyChanged("ListViewWatchedColorMcml"); } }
        }
        public Color ListViewWatchedColorMcml
        {
            get { return new Color(this.ListViewWatchedColor); }
        }

        public bool ShowUnwatchedCount
        {
            get { return this.data.ShowUnwatchedCount; }
            set { if (this.data.ShowUnwatchedCount != value) { this.data.ShowUnwatchedCount = value; Save(); FirePropertyChanged("ShowUnwatchedCount"); } }
        }

        public bool ShowWatchedTickOnFolders
        {
            get { return this.data.ShowWatchedTickOnFolders; }
            set { if (this.data.ShowWatchedTickOnFolders != value) { this.data.ShowWatchedTickOnFolders = value; Save(); FirePropertyChanged("ShowWatchedTickOnFolders"); } }
        }

        public bool ShowWatchTickInPosterView
        {
            get { return this.data.ShowWatchTickInPosterView; }
            set { if (this.data.ShowWatchTickInPosterView != value) { this.data.ShowWatchTickInPosterView = value; Save(); FirePropertyChanged("ShowWatchTickInPosterView"); } }
        }


        [Comment("Enables the views to default to the first unwatched item in a folder of movies or tv shows")]
        public bool DefaultToFirstUnwatched
        {
            get { return this.data.DefaultToFirstUnwatched; }
            set { if (this.data.DefaultToFirstUnwatched != value) { this.data.DefaultToFirstUnwatched = value; Save(); FirePropertyChanged("DefaultToFirstUnwatched"); } }
        }
        [Comment("When navigating, if only a single folder exists, enter it.")]
        public bool AutoEnterSingleDirs
        {
            get { return this.data.AutoEnterSingleDirs; }
            set { if (this.data.AutoEnterSingleDirs != value) { this.data.AutoEnterSingleDirs = value; Save(); FirePropertyChanged("AutoEnterSingleDirs"); } }
        }
        [Comment(@"Indicates that files with a date stamp before this date should be assumed to have been watched for the purpose of ticking them off.")]
        public DateTime AssumeWatchedBefore
        {
            get { return this.data.AssumeWatchedBefore; }
            set { if (this.data.AssumeWatchedBefore != value) { this.data.AssumeWatchedBefore = value; Save(); FirePropertyChanged("AssumeWatchedBefore"); FirePropertyChanged("AssumeWatchedBeforeStr"); } }
        }

        public string AssumeWatchedBeforeStr
        {
            get { return this.AssumeWatchedBefore.ToString("MMM yyyy"); }
        }

        public void IncrementAssumeWatched()
        {
            this.AssumeWatchedBefore = this.AssumeWatchedBefore.AddMonths(1);
        }

        public void DecrementAssumeWatched()
        {
            this.AssumeWatchedBefore = this.AssumeWatchedBefore.AddMonths(-1);
        }

        public bool InheritDefaultView
        {
            get { return this.data.InheritDefaultView; }
            set { if (this.data.InheritDefaultView != value) { this.data.InheritDefaultView = value; Save(); FirePropertyChanged("InheritDefaultView"); } }
        }

        [Comment("Changes the default view index for folders that have not yet been visited.\n\t[Detail|Poster|Thumb]")]
        public ViewTypes DefaultViewType
        {
            get 
            {
                try
                {
                    return (ViewTypes)Enum.Parse(typeof(ViewTypes), this.data.DefaultViewType);
                }
                catch
                {
                    return ViewTypes.Poster;
                }
            }
            set { if (this.data.DefaultViewType != value.ToString()) { this.data.DefaultViewType = value.ToString(); Save(); FirePropertyChanged("DefaultViewType"); } }
        }
        [Comment("Specifies whether the default Poster and Thumb views show labels")]
        public bool DefaultShowLabels
        {
            get { return this.data.DefaultShowLabels; }
            set { if (this.data.DefaultShowLabels != value) { this.data.DefaultShowLabels = value; Save(); FirePropertyChanged("DefaultShowLabels"); } }
        }
        [Comment("Specifies is the default for the Poster view is vertical scrolling")]
        public bool DefaultVerticalScroll
        {
            get { return this.data.DefaultVerticalScroll; }
            set { if (this.data.DefaultVerticalScroll != value) { this.data.DefaultVerticalScroll = value; Save(); FirePropertyChanged("DefaultVerticalScroll"); } }
        }
        [Comment(@"Limits the number of levels shown by the breadcrumbs.")]
        public int BreadcrumbCountLimit
        {
            get { return this.data.BreadcrumbCountLimit; }
            set { if (this.data.BreadcrumbCountLimit != value) { this.data.BreadcrumbCountLimit = value; Save(); FirePropertyChanged("BreadcrumbCountLimit"); } }
        }
        public bool AllowInternetMetadataProviders
        {
            get { return this.data.AllowInternetMetadataProviders; }
            set { if (this.data.AllowInternetMetadataProviders != value) { this.data.AllowInternetMetadataProviders = value; Save(); FirePropertyChanged("AllowInternetMetadataProviders"); } }
        }
        
        public bool EnableFileWatching
        {
            get { return this.data.EnableFileWatching; }
            set { if (this.data.EnableFileWatching != value) { this.data.EnableFileWatching = value; Save(); FirePropertyChanged("EnableFileWatching"); } }
        }

        internal List<ConfigData.ExternalPlayer> ExternalPlayers
        {
            get { return this.data.ExternalPlayers; }
            //set { if (this.data.ExternalPlayers != value) { this.data.ExternalPlayers = value; Save(); FirePropertyChanged("ExternalPlayers"); } }
        }

        public bool UseAutoPlayForIso
        {
            get { return this.data.UseAutoPlayForIso; }
            set { if (this.data.UseAutoPlayForIso != value) { this.data.UseAutoPlayForIso = value; Save(); FirePropertyChanged("UseAutoPlayForIso"); } }
        }

        [Comment("List of characters to remove from titles for alphanumeric sorting.  Separate each character with a '|'.\nThis allows titles like '10,000.BC.2008.720p.BluRay.DTS.x264-hV.mkv' to be properly sorted.")]
        public string SortRemoveCharacters
        {
            get { return this.data.SortRemoveCharacters; }
            set { if (this.data.SortRemoveCharacters != value) { this.data.SortRemoveCharacters = value; Save(); FirePropertyChanged("SortRemoveCharacters"); } }
        }
        [Comment("List of characters to replace with a ' ' in titles for alphanumeric sorting.  Separate each character with a '|'.\nThis allows titles like 'Iron.Man.REPACK.720p.BluRay.x264-SEPTiC.mkv' to be properly sorted.")]
        public string SortReplaceCharacters
        {
            get { return this.data.SortReplaceCharacters; }
            set { if (this.data.SortReplaceCharacters != value) { this.data.SortReplaceCharacters = value; Save(); FirePropertyChanged("SortReplaceCharacters"); } }
        }
        [Comment(@"List of words to remove from alphanumeric sorting.  Separate each word with a '|'.  Note that the
        algorithm appends a ' ' to the end of each word during the search which means words found at the end
        of each title will not be removed.  This is generally not an issue since most people will only want
        articles removed and articles are rarely found at the end of media titles.  This, combined with SortReplaceCharacters,
        allows titles like 'The.Adventures.Of.Baron.Munchausen.1988.720p.BluRay.x264-SiNNERS.mkv' to be properly sorted.")]
        public string SortReplaceWords
        {
            get { return this.data.SortReplaceWords; }
            set { if (this.data.SortReplaceWords != value) { this.data.SortReplaceWords = value; Save(); FirePropertyChanged("SortReplaceWords"); } }
        }

        public string ViewTheme
        {
            get { return this.data.ViewTheme; }
            set { if (this.data.ViewTheme != value) { this.data.ViewTheme = value; Save(); FirePropertyChanged("ViewTheme"); } }
        }

        public string PreferredMetaDataLanguage
        {
            get { return this.data.PreferredMetaDataLanguage; }
            set { if (this.data.PreferredMetaDataLanguage != value) { this.data.PreferredMetaDataLanguage = value; Save(); FirePropertyChanged("PreferredMetaDataLanguage"); } }
        }

        public string CentralisedCache
        {
            get { return this.data.CentralisedCache; }
            set { if (this.data.CentralisedCache != value) { this.data.CentralisedCache = value; Save(); FirePropertyChanged("CentralisedCache"); } }
        }

        public string Theme
        {
            get { return this.data.Theme; }
            set { if (this.data.Theme != value) { this.data.Theme = value; Save(); FirePropertyChanged("Theme"); } }
        }

        public string FontTheme
        {
            get { return this.data.FontTheme; }
            set { if (this.data.FontTheme != value) { this.data.FontTheme = value; Save(); FirePropertyChanged("FontTheme"); } }
        }

        [Comment(@"Enable clock onscreen.")]
        public bool ShowClock
        {
            get { return this.data.ShowClock; }
            set { if (this.data.ShowClock != value) { this.data.ShowClock = value; Save(); FirePropertyChanged("ShowClock"); } }
        }

        [Comment(@"Enable more advanced commands.")]
        public bool EnableAdvancedCmds
        {
            get { return this.data.EnableAdvancedCmds; }
            set { if (this.data.EnableAdvancedCmds != value) { this.data.EnableAdvancedCmds = value; Save(); FirePropertyChanged("EnableAdvancedCmds"); } }
        }

        [Comment(@"Advanced Command: Enable Delete")]
        public bool Advanced_EnableDelete
        {
            get { return this.data.Advanced_EnableDelete; }
            set { if (this.data.Advanced_EnableDelete != value) { this.data.Advanced_EnableDelete = value; Save(); FirePropertyChanged("Advanced_EnableDelete"); } }
        }

        [Comment(@"Show backdrop on main views.")]
        public bool ShowBackdrop
        {
            get { return this.data.ShowBackdrop; }
            set { if (this.data.ShowBackdrop != value) { this.data.ShowBackdrop = value; Save(); FirePropertyChanged("ShowBackdrop"); } }
        }


        [Comment(@"The name displayed in the top right when you first navigate into your library")]
        public string InitialBreadcrumbName {
            get { return this.data.InitialBreadcrumbName; }
            set {
                    if (this.data.InitialBreadcrumbName != value) {
                        this.data.InitialBreadcrumbName = value;
                        Save();
                        FirePropertyChanged("InitialBreadcrumbName");
                    }
            }
        }
        public bool MaintainPosterAspectRatio
        {
            get { return this.data.MaintainPosterAspectRatio; }
            set { if (this.data.MaintainPosterAspectRatio != value) { this.data.MaintainPosterAspectRatio = value; Save(); FirePropertyChanged("MaintainPosterAspectRatio"); } }
        }
    
        public bool ShowConfigButton
        {
            get { return this.data.ShowConfigButton; }
            set { if (this.data.ShowConfigButton != value) { this.data.ShowConfigButton = value; Save(); FirePropertyChanged("ShowConfigButton"); } }
        }
        public int AlphaBlending
        {
            get { return this.data.AlphaBlending; }
            set { if (this.data.AlphaBlending != value) { this.data.AlphaBlending = value; Save(); FirePropertyChanged("AlphaBlending"); } }
        }
        /* End of app specific settings*/

        private string[] _SortRemoveCharactersArray = null;
        public string[] SortRemoveCharactersArray
        {
            get
            {
                _SortRemoveCharactersArray = _SortRemoveCharactersArray ?? SortRemoveCharacters.Split('|');
                return _SortRemoveCharactersArray;
            }
        }

        private string[] _SortReplaceCharactersArray = null;
        public string[] SortReplaceCharactersArray
        {
            get
            {
                _SortReplaceCharactersArray = _SortReplaceCharactersArray ?? SortReplaceCharacters.Split('|');
                return _SortReplaceCharactersArray;
            }
        }

        private string[] _SortReplaceWordsArray = null;
        public string[] SortReplaceWordsArray
        {
            get
            {
                _SortReplaceWordsArray = _SortReplaceWordsArray ?? SortReplaceWords.Split('|');
                return _SortReplaceWordsArray;
            }
        }

        
        private static object _syncobj = new object();
        private static Config _instance = null;
        public static Config Instance
        {
            get
            {
                // ensure we are initialized. 
                Config.Initialize(); 
                return _instance;
            }
        }

        private Config()
        {
            
        }

        private void Save()
        {
            lock(this)
                this.data.Save(Helper.ConfigFile);
        }

        public void Reset()
        {
            lock (this)
            {
                this.data = new ConfigData();
                Save();
            }
        }

        private string GetComment(MemberInfo field)
        {
            string comment = "";
            var attribs = field.GetCustomAttributes(typeof(CommentAttribute), false);
            if (attribs != null && attribs.Length > 0)
            {
                comment = ((CommentAttribute)attribs[0]).Comment;
            }
            return comment;
        }

        private bool Load()
        {
            try
            {
                this.data = ConfigData.FromFile(Helper.ConfigFile);
                return true;
            }
            catch (Exception ex)
            {
                MediaCenterEnvironment ev = Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment;
                DialogResult r = ev.Dialog(ex.Message + "\nReset to default?", "Error in configuration file", DialogButtons.Yes | DialogButtons.No, 600, true);
                if (r == DialogResult.Yes)
                {
                    this.data = new ConfigData();
                    Save();
                    return true;
                }
                else
                    return false;
            }
        }
        internal static bool Initialize()
        {
            if (_instance == null)
            {
                lock (_syncobj)
                {
                    if (_instance == null)
                    {
                        _instance = new Config();
                        return _instance.Load();
                    }
                }
            }
            return true;
        }

        #region IModelItem Members

        public string Description {
            get {
                throw new NotImplementedException();
            }
            set {
                throw new NotImplementedException();
            }
        }

        public bool Selected {
            get {
                throw new NotImplementedException();
            }
            set {
                throw new NotImplementedException();
            }
        }

        public Guid UniqueId {
            get {
                throw new NotImplementedException();
            }
            set {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region IPropertyObject Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region IModelItemOwner Members

        protected void FirePropertyChanged(string property) {
            if (PropertyChanged != null) {
                PropertyChanged(this, property);
            }
        }

        List<ModelItem> items = new List<ModelItem>(); 

        public void RegisterObject(ModelItem modelItem) {
            items.Add(modelItem);
        }

        public void UnregisterObject(ModelItem modelItem) {
            if (items.Exists((i) => i == modelItem)) {
                // TODO : Invoke on the UI thread
                modelItem.Dispose();
            }
        }

        #endregion
    }
}
