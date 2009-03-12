using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.MediaCenter;
using Microsoft.MediaCenter.Hosting;
using Microsoft.MediaCenter.UI;
using MediaBrowser.Util;
using System.Text;
using System;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.IO;
using Microsoft.MediaCenter.AddIn;
using System.Collections;
using MediaBrowser.Library;
using MediaBrowser.Library.Sources;
using MediaBrowser.LibraryManagement;


namespace MediaBrowser
{

    public class Application : ModelItem, IDisposable
    {

        public Config Config
        {
            get
            {
                return Config.Instance;
            }
        }

        public static Application CurrentInstance
        {
            get { return singleApplicationInstance; }
        }

        private static Application singleApplicationInstance;
        private MyHistoryOrientedPageSession session;
        private static object syncObj = new object();
        private bool navigatingForward;
        private PlaybackController playbackController = new PlaybackController();

        public bool NavigatingForward
        {
            get { return navigatingForward; }
            set { navigatingForward = value; }
        }

        private static Dictionary<char, List<char>> letterMap = new Dictionary<char, List<char>>();
        static Application()
        {
            
        }



        public Application()
            : this(null, null)
        {

        }

        
        public Application(MyHistoryOrientedPageSession session, Microsoft.MediaCenter.Hosting.AddInHost host)
        {

            this.session = session;
            if (session != null)
            {
                this.session.Application = this;
            }
            singleApplicationInstance = this;
            Debug.WriteLine("Started");
        }

        /// <summary>
        /// This is an oddity under TVPack, sometimes the MediaCenterEnvironemt and MediaExperience objects go bad and become
        /// disconnected from their host in the main application. Typically this is after 5 minutes of leaving the application idle (but noot always).
        /// What is odd is that using reflection under these circumstances seems to work - even though it is only doing the same as Reflector shoulds the real 
        /// methods do. As I said it's odd but this at least lets us get a warning on the screen before the application crashes out!
        /// </summary>
        /// <param name="message"></param>
        public static void DialogBoxViaReflection(string message)
        {
            MediaCenterEnvironment ev = Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment;
            FieldInfo fi = ev.GetType().GetField("_legacyAddInHost", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance);
            if (fi != null)
            {
                AddInHost2 ah2 = (AddInHost2)fi.GetValue(ev);
                if (ah2 != null)
                {
                    Type t = ah2.GetType();
                    PropertyInfo pi = t.GetProperty("HostControl", BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Public);
                    if (pi != null)
                    {
                        HostControl hc = (HostControl)pi.GetValue(ah2, null);
                        hc.Dialog(message, "Media Browser", 1, 120, true);
                    }
                }
            }
        }

        /// <summary>
        /// Unfortunately TVPack has some issues at the moment where the MedaCenterEnvironment stops working, we catch these errors and rport them then close.
        /// In the future this method and all references should be able to be removed, once MS fix the bugs
        /// </summary>
        internal static void ReportBrokenEnvironment()
        {
            Trace.TraceInformation("Application has broken MediaCenterEnvironment, possibly due to 5 minutes of idle while running under system with TVPack installed.\n Application will now close.");
            Trace.TraceInformation("Attempting to use reflection that sometimes works to show a dialog box");
            // for some reason using reflection still works
            Application.DialogBoxViaReflection("Application will now close due to broken MediaCenterEnvironment object, possibly due to 5 minutes of idle time and/or running with TVPack installed.");
            Microsoft.MediaCenter.Hosting.AddInHost.Current.ApplicationContext.CloseApplication();
        }

        public void FixRepeatRate(object scroller, int val)
        {
            try
            {
                PropertyInfo pi = scroller.GetType().GetProperty("View", BindingFlags.Public | BindingFlags.Instance);
                object view = pi.GetValue(scroller, null);
                pi = view.GetType().GetProperty("Control", BindingFlags.Public | BindingFlags.Instance);
                object control = pi.GetValue(view, null);

                pi = control.GetType().GetProperty("KeyRepeatThreshold", BindingFlags.NonPublic | BindingFlags.Instance);
                pi.SetValue(control, (UInt32)val, null);
            }
            catch
            {
                // thats it, I give up, Microsoft went and changed interfaces internally 
            }

        }

        public static MediaCenterEnvironment MediaCenterEnvironment
        {
            get
            {
                return Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment;
            }
        }

        public PlaybackController PlaybackController {
            get {
                return playbackController;
            }
        }


        private string ResolveInitialFolder()
        {
            string start = Config.Instance.InitialFolder;
            if (start == Helper.MY_VIDEOS)
                start = Helper.MyVideosPath;
            return start;
        }

        private ItemSource InitialSource
        {
            get
            {
                var folder = ResolveInitialFolder();
                if (Helper.IsShortcut(folder))
                {
                    string f = Helper.ResolveShortcut(folder);
                    if (Helper.IsVirtualFolder(f))
                        return new VirtualFolderSource(f);
                    else
                        return new ShortcutSource(folder);
                }
                if (Helper.IsVirtualFolder(folder))
                    return new VirtualFolderSource(folder);
                else
                    return new FileSystemSource(folder);
            }
        }

        private bool CheckInitialSource()
        {
            string start = ResolveInitialFolder();
            if (Helper.IsShortcut(start))
                start = Helper.ResolveShortcut(start);
            if (Helper.IsVirtualFolder(start))
                return File.Exists(start);
            else
                return Directory.Exists(start);
        }

        public void Back()
        {
            session.BackPage();
        }

        public void FinishInitialConfig()
        {
            MediaCenterEnvironment ev = Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment;
            ev.Dialog("Initial configuration is complete, please restart Media Browser", "Restart", DialogButtons.Ok, 60, true);
            Microsoft.MediaCenter.Hosting.AddInHost.Current.ApplicationContext.CloseApplication();

        }

        public void DeleteMediaItem(Item Item)
        {
            // Setup variables
            MediaCenterEnvironment mce = Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment;
            var msg = "Are you sure you wish to delete this media item?";
            var caption = "Delete Confirmation";

            // Present dialog
            DialogResult dr = mce.Dialog(msg, caption, DialogButtons.No | DialogButtons.Yes, 0, true);

            if (dr == DialogResult.Yes)
            {
                // Perform itemtype and configuration checks
                if (Item.Source.ItemType == ItemType.Movie &&
                    (this.Config.Advanced_EnableDelete == true && this.Config.EnableAdvancedCmds == true)
                   )
                {
                    Item Parent = Item.PhysicalParent;
                    string path = Item.Source.Location;
                    try
                    {
                        if (Directory.Exists(path))
                        {
                            Directory.Delete(path, true);
                        }
                        else if (File.Exists(path))
                        {
                            File.Delete(path);
                        }
                    }
                    catch (IOException)
                    {
                        mce.Dialog("The selected media item cannot be deleted due to an invalid path. Or you may not have sufficient access rights to perform this command.", "Delete Failed", DialogButtons.Ok, 0, true);
                    }
                    catch (Exception)
                    {
                        mce.Dialog("The selected media item cannot be deleted due to an unknown error.", "Delete Failed", DialogButtons.Ok, 0, true);
                    }
                    Back(); // Back to the Parent Item; This parent still contains old data.

                    // These tricks are required in order to load the parent item with "fresh" data.
                    if (session.BackPage()) // Double Back to the GrandParent because history still has old parent.
                    {
                        Navigate(Parent);  // Navigate forward to Parent 
                    }
                    else // No GrandParent to go back to.
                    {
                        Navigate(Parent); // Navigate to the parent again - this will refresh the objects
                        session.BackPage(); // Now safe to go back to previous parent, and keep session history valid
                    }
                }
                else
                    mce.Dialog("The selected media item cannot be deleted due to its Item-Type or you have not enabled this feature in the configuration file.", "Delete Failed", DialogButtons.Ok, 0, true);
            }
        }

        // Entry point for the app
        public void GoToMenu()
        {
            try
            {
                if (Config.IsFirstRun)
                {
                    OpenConfiguration(false);
                    MediaCenterEnvironment ev = Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment;
                    ev.Dialog("As this is the first time you have run Media Browser please setup the inital configuration", "Configure", DialogButtons.Ok, 60, true);
                }
                else
                {
                    // We check config here instead of in the Updater class because the Config class 
                    // CANNOT be instantiated outside of the application thread.
                    if (Config.EnableUpdates)
                    {
                        Updater update = new Updater(this);
                        ThreadPool.QueueUserWorkItem(new WaitCallback(update.checkUpdate));
                    }
                    if (!CheckInitialSource())
                    {
                        MediaCenterEnvironment ev = Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment;
                        ev.Dialog("Initial folder: " + Config.Instance.InitialFolder + " cannot be found, please check configuration.", "Error", DialogButtons.Ok, 60, true);
                        Config.IsFirstRun = true;
                        OpenConfiguration(false);
                        return;
                    }

                    ItemSource initial = this.InitialSource;
                    Item item = ItemCache.Instance.Retrieve(initial.UniqueName);
                    if (item == null)
                    {
                        MediaCenterEnvironment ev = Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment;
                        ev.Dialog("Media Browser will now perform an initial scan of your configured folders, you may continue to use Media Browser but it may take a few moments for all your content to appear.", "Initial Cache Population", DialogButtons.Ok, 60, true);
                        item = initial.ConstructItem();
                        ItemCache.Instance.SaveSource(item.Source);
                    }
                    OpenPage(item);
                }
            }
            catch (Exception e)
            {
                Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment.Dialog("Media Browser encountered a critical error and had to shut down: " + e.ToString() + " " + e.StackTrace.ToString(), "Critical Error", DialogButtons.Ok, 60, true);
                Microsoft.MediaCenter.Hosting.AddInHost.Current.ApplicationContext.CloseApplication();
            }
        }

        private Boolean PlayStartupAnimation = true;

        public Boolean CanPlayStartup()
        {
            if (PlayStartupAnimation)
            {
                PlayStartupAnimation = false;
                return true;
            }
            else
            {
                return false;
            }
        }



        private bool showNowPlaying = true;
        public bool ShowNowPlaying
        {
            get { return this.showNowPlaying; }
            set { //if (this.showNowPlaying != value) { this.showNowPlaying = value; FirePropertyChanged("ShowNowPlaying"); } 
            }
        }


        public string BreadCrumbs
        {
            get
            {
                return session.Breadcrumbs;
            }
        }

        public void ClearCache()
        {
            MediaCenterEnvironment ev = Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment;
            DialogResult r = ev.Dialog("Are you sure you wish to clear the cache?\nThis will erase all cached and downloaded information and images.", "Clear Cache", DialogButtons.Yes | DialogButtons.No, 60, true);
            if (r == DialogResult.Yes)
            {
                bool ok = ItemCache.Instance.ClearEntireCache();
                if (!ok)
                {
                    ev.Dialog("An error occured during the clearing of the cache, you may wish to manually clear it from " + Helper.AppCachePath + " before restarting Media Browser", "Error", DialogButtons.Ok, 60, true);
                }
                else
                {
                    ev.Dialog("Please restart Media Browser", "Cache Cleared", DialogButtons.Ok, 60, true);
                }
                Microsoft.MediaCenter.Hosting.AddInHost.Current.ApplicationContext.CloseApplication();
            }
        }

        public void ResetConfig()
        {
            MediaCenterEnvironment ev = Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment;
            DialogResult r = ev.Dialog("Are you sure you wish to reset all configuration to defaults?", "Reset Configuration", DialogButtons.Yes | DialogButtons.No, 60, true);
            if (r == DialogResult.Yes)
            {
                Config.Instance.Reset();
                ev.Dialog("Please restart Media Browser", "Configuration Reset", DialogButtons.Ok, 60, true);
                Microsoft.MediaCenter.Hosting.AddInHost.Current.ApplicationContext.CloseApplication();
            }
        }

        public void OpenConfiguration(bool showFullOptions)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties["Application"] = this;
            properties["ShowFull"] = showFullOptions;

            if (session != null)
            {
                session.GoToPage("resx://MediaBrowser/MediaBrowser.Resources/ConfigPage", properties);
            }
            else
            {
                Trace.TraceError("Session is null in OpenPage");
            }
        }

        private void OpenPage(Item item)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties["Application"] = this;
            properties["Item"] = item;

            if (session != null)
            {
                item.NavigatingInto();
                session.GoToPage("resx://MediaBrowser/MediaBrowser.Resources/Page", properties);
            }
            else
            {
                Trace.TraceError("Session is null in OpenPage");
            }
        }

        public void NavigateToActor(Actor actor, Item currentMovie)
        {
            NavigateToParentItem(currentMovie,
                item => (item.Metadata.Actors.Find(a => a.Name == actor.Name) != null),
                IndexType.Actor,
                actor.Name
            );
        }

        

        public void NavigateToGenre(string genre, Item currentMovie)
        {
            NavigateToParentItem(currentMovie,
                item => item.Metadata.Genres.Contains(genre),
                IndexType.Genre,
                genre
            );
        }

        public void NavigateToParentItem(Item childItem, Predicate<Item> finder, IndexType indexType, string sourceName)
        {
            Item i = childItem.PhysicalParent;

            List<Item> movies = new List<Item>();
            foreach (var item in i.UnsortedChildren)
            {
                if (item.Source.ItemType != ItemType.Movie) continue;
                if (item.Metadata == null) continue;

                if (finder(item)) movies.Add(item); 
            }

            IndexingSource source = new IndexingSource(sourceName, movies, IndexType.Genre);
            Navigate(source.ConstructItem());
        }

        public void Navigate(Item item)
        {
            item.Source.ValidateItemType();

            if (item.Source.ItemType == ItemType.Movie)
            {
                item.EnsureMetadataLoaded();
                if (item.Metadata.HasDataForDetailPage)
                {
                    // go to details screen 
                    Dictionary<string, object> properties = new Dictionary<string, object>();
                    properties["Application"] = this;
                    properties["Item"] = item;
                    session.GoToPage("resx://MediaBrowser/MediaBrowser.Resources/MovieDetailsPage", properties);
                    return;
                }
            }

            if (!item.Source.IsPlayable)
            {
                if (!Config.Instance.RememberIndexing)
                    item.DisplayPrefs.IndexBy = IndexType.None;
                if (Config.Instance.AutoEnterSingleDirs && (item.Children.Count == 1))
                    Navigate(item.Children[0]);
                else
                    OpenPage(item);
            }
            else
            {
                 item.Resume();
            }
        }

        public void Play(Item item)
        {
            if (item.Source.IsPlayable)
            {
                item.Play();
            }
        }

        public void Resume(Item item)
        {
            if (item.Source.IsPlayable)
            {
                item.Resume();
            }
        }

        public DialogResult displayDialog(string message, string caption, DialogButtons buttons, int timeout)
        {
            // We won't be able to take this during a page transition.  This is good!
            // Conversly, no new pages can be navigated while this is present.
            lock (syncObj)
            {
                DialogResult result = MediaCenterEnvironment.Dialog(message, caption, buttons, timeout, true);
                return result;
            }
        }

        public string AppVersion
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
        }


        #region IDisposable Members

        ~Application()
        {
            Dispose();
        }

        void IDisposable.Dispose()
        {
            //keepAliveTimer.Dispose();
            GC.SuppressFinalize(this);
        }

        #endregion


    }
}
