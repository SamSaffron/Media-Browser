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

        #region triple type support
        /* Triple type support  http://mytv.senseitweb.com/blogs/mytv/archive/2007/11/22/implementing-jil-jump-in-list-in-your-mcml-application.aspx */
        /*
        private EditableText _JILtext;
        private Int32 _JILindex = new Int32();

        [MarkupVisible]
        internal EditableText JILtext
        {
            get { return _JILtext; }
            set { if (_JILtext != value) { _JILtext = value; base.FirePropertyChanged("JILtext"); } }
        }

        [MarkupVisible]
        internal Int32 JILindex
        {
            get { return _JILindex; }
            set { _JILindex = value; base.FirePropertyChanged("JILindex"); }
        }

        string _findText = ""; 

        [MarkupVisible]
        internal string FindText
        {
            get { return _findText; }
            set 
            { 
                _findText = value;
                base.FirePropertyChanged("FindText");
            }
        }

        

        public int CurrentIndex { get; set; }

        private void JILtext_Activity(object sender, EventArgs args)
        {
            if (JILtext.Value == string.Empty)
            {
                FindText = "";
                return;
            }

            Regex isKeyboard = new Regex("[a-z]"); 
            if (isKeyboard.Match(JILtext.Value.ToLower()).Success)
            {
                int index = 0;
                // handle a keyboard match 

                string match = JILtext.Value.ToLower(); 

                foreach (var item in FolderItems.folderItems)
                {
                    if (item.SortableDescription.StartsWith(match))
                    {
                        JILindex = index - CurrentIndex;
                        break; 
                    } 
                    index++; 
                }

                // not coming from remote
                FindText = JILtext.Value;
                return;
            } 

            char? previous_letter = null;
            int counter = 0; 

            foreach (var letter in JILtext.Value)
            {
                if (letter == previous_letter)
                {
                    counter++;
                }
                else
                {
                    counter = 0;
                    previous_letter = letter; 
                }
            }

            if (previous_letter != null)
            {
                if (previous_letter == '1')
                {
                    JILindex = 0 - CurrentIndex;
                    FindText = "";
                }
                else if (previous_letter > '1' && previous_letter <= '9')
                {
                    NavigateTo((char)previous_letter, counter);
                }
            }   
        }

        

        void NavigateTo(char letter, int count)
        {
            List<char> letters = letterMap[letter]; 

            Dictionary<char, int> found_letters = new Dictionary<char,int>();

            int index = 0;

            // remove letters we do not have 
            foreach (var item in FolderItems.folderItems)
            {
                char current_letter = item.SortableDescription[0];
                if (letters.Contains(current_letter))
                {
                    if (!found_letters.ContainsKey(current_letter))
                    {
                        found_letters[current_letter] = index;
                    }
                }

                index++;
            }

            if (found_letters.Count == 0)
            {
                return; 
            }

            // navigate to letters first then numbers
            count = (count+1) % found_letters.Count; 
            foreach (var item in found_letters)
	        {
		        if (count == 0) 
                {
                    JILindex = item.Value - CurrentIndex;
                    FindText = item.Key.ToString().ToUpper();
                }
                count--;
	        }
        } 
        private static void AddLetterMapping(char letter, string letters)
        {
            var list = new List<char>();
            foreach (char c in letters)
            {
                list.Add(c);
            }
            letterMap[letter] = list;
        }

        */
        #endregion

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

        /*
         * The below doesn't seem to work fully, stangely the PlayMediaByReflection works when the 
         * normal PlayMedia doesn't even though it's only doing what Reflector shows is normally happening anyway.
         * The problem is that there are other calls required as well and the app still tends to be a bit unstable 
         * so I've stopped this avenue of coding for now - hopefully MS will fix TVPack soon.
         * For now we just have a problem - when running under TVPack 5 minutes of idle will kill the app.
        public static void PlayMediaViaReflection(string media)
        {
            Trace.TraceInformation("Playing via relfection: " + media);
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
                        hc.PlayMedia(Microsoft.MediaCenter.Extensibility.MediaType.Video,media, false);
                        Trace.TraceInformation("Suceeded playing via reflection: " + media);
                    }
                }
            }
            
        }
         */
        /* I was hoping to be able to keep the relavent object alive by accessing them regularly to prevent the 
         * timeout - this had some success but only kept alive some of the objects that were required, so didn;t solve the full problem.
         * I really hope MS fix this TVPack issue soon.
         * 
        private void KeepAlive(object none)
        {
            // this is necessary as otherwise the AddinHost disappears after 5 minutes of inactivity
            // See remarks for IAddInEntryPoint.Launch Method in media center sdk
            // we need to do something that accesses the HostControl object
            try
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
                            IDictionary dict = hc.MediaContext;
                            Trace.TraceInformation("Keep alive found HostControl");
                            if (dict != null)
                            {
                                foreach (object key in dict.Keys)
                                {
                                    object o = dict[key];
                                    Trace.TraceInformation(key.ToString() + " : " + (o == null ? "null" : o.ToString()));
                                }
                            }
                            return;
                        }
                    }
                }
                Trace.TraceInformation("Keep alive failed, no host control");
            }
            catch (Exception e)
            {
                Trace.TraceError("Keep alive failed: \n" + e.ToString());
            }
        }

        */

        public void FixRepeatRate(object scroller, uint val)
        {
            try
            {
                PropertyInfo pi = scroller.GetType().GetProperty("View", BindingFlags.Public | BindingFlags.Instance);
                object view = pi.GetValue(scroller, null);
                pi = view.GetType().GetProperty("Control", BindingFlags.Public | BindingFlags.Instance);
                object control = pi.GetValue(view, null);

                pi = control.GetType().GetProperty("KeyRepeatThreshold", BindingFlags.NonPublic | BindingFlags.Instance);
                pi.SetValue(control, val, null);
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
                //if (host == null) return null;
                //return host.MediaCenterEnvironment;
            }
        }


        private string ResolveInitialFolder()
        {
            string start = Config.Instance.InitialFolder;
            //if (Helper.IsShortcut(start))
            //start = Helper.ResolveShortcut(start);
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
            set { if (this.showNowPlaying != value) { this.showNowPlaying = value; FirePropertyChanged("ShowNowPlaying"); } }
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
