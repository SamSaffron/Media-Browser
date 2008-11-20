using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.MediaCenter;
using Microsoft.MediaCenter.Hosting;
using Microsoft.MediaCenter.UI;
using SamSoft.VideoBrowser.LibraryManagement;
using SamSoft.VideoBrowser.Util;
using System.Text;
using System;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.IO;
using Microsoft.MediaCenter.AddIn;
using System.Collections;


namespace SamSoft.VideoBrowser
{
    
    public class Application : ModelItem, IDisposable
    {


        /* Triple type support  http://mytv.senseitweb.com/blogs/mytv/archive/2007/11/22/implementing-jil-jump-in-list-in-your-mcml-application.aspx */
        private EditableText _JILtext;
        private Int32 _JILindex = new Int32();

        [MarkupVisible]
        internal EditableText JILtext
        {
            get { return _JILtext; }
            set
			{
				if (_JILtext != value)
				{
					_JILtext = value;
					base.FirePropertyChanged("JILtext");
				}
			}
        }

        [MarkupVisible]
        internal Int32 JILindex
        {
            get { return _JILindex; }
            set
			{
				_JILindex = value;
				base.FirePropertyChanged("JILindex");
			}
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

        public Config Config
        {
            get { return Config.Instance; }
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
			//count = (count+1) % found_letters.Count; 
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

        /**/

        public FolderItemListMCE FolderItems;
        private static Application singleApplicationInstance;
        //private static AddInHost host;
        private MyHistoryOrientedPageSession session;

        private static object syncObj = new object(); 

        private bool navigatingForward;
        /*
        public static AddInHost Host
        {
            get 
            {
                return host;
            }
        }*/

        public bool NavigatingForward
        {
            get { return navigatingForward; }
            set { navigatingForward = value; }
        }

        private static Dictionary<char, List<char>> letterMap = new Dictionary<char, List<char>>();
        static Application()
        {
            // init the letter map for JIL list 
            AddLetterMapping('2', "abc2");
            AddLetterMapping('3', "def3");
            AddLetterMapping('4', "ghi4");
            AddLetterMapping('5', "jkl5");
            AddLetterMapping('6', "mno6");
            AddLetterMapping('7', "pqrs7");
            AddLetterMapping('8', "tuv8");
            AddLetterMapping('9', "wxyz9");
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

        public Application()
            : this(null, null)
        {
            
        }

        System.Threading.Timer keepAliveTimer;

        public Application(MyHistoryOrientedPageSession session, Microsoft.MediaCenter.Hosting.AddInHost host)
        {
            
            //Debugger.Launch();
            //Thread.Sleep(20000);

            this.session = session;
            if (session != null)
            {
                this.session.Application = this;
            }
            //Application.host = host;
            singleApplicationInstance = this;

            _JILtext = new EditableText(this.Owner, "JIL");
            JILtext.Value = "";
            JILtext.Submitted += new EventHandler(JILtext_Activity);
            //keepAliveTimer = new System.Threading.Timer(new TimerCallback(KeepAlive), null, 10000, 60000);
            Trace.TraceInformation("Started");
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
                        hc.Dialog(message, "Video Browser", 1, 120, true);
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

        // Entry point for the app
        public void GoToMenu()
        {
            // We check config here instead of in the Updater class because the Config class 
            // CANNOT be instantiated outside of the application thread.
            if (Config.EnableUpdates)
            {
                Updater update = new Updater(this);
                ThreadPool.QueueUserWorkItem(new WaitCallback(update.checkUpdate));
            }

            var filename = Config.InitialFolder.ToLower();
            bool isVF = false;;
            if (Helper.IsVirtualFolder(filename))
            {
                NavigateToVirtualFolder(new VirtualFolder(filename), null);
                isVF = true;
            }

            if (filename == "myvideos" || !System.IO.Directory.Exists(filename))
            {
                filename = Helper.MyVideosPath;    
            }

            if (!isVF)
            {
                NavigateToPath(filename, null);
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


      
        public void ShowNowPlaying()
        {
            try
            {
                Microsoft.MediaCenter.Hosting.AddInHost.Current.ViewPorts.NowPlaying.Focus();
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error attempting to show and focus NowPlaying view port.\n" + ex.ToString());
            }
        }

        private void CacheData(object param)
        {
            FolderItemListMCE data = param as FolderItemListMCE;
            data.CacheMetadata();
        }
        
        private void Done(object param)
        {
            FolderItems.RefreshSortOrder();
        }

        public void NavigateToItems(List<IFolderItem> item, string breadcrumb)
        {
            FolderItems = new FolderItemListMCE(FolderItems, breadcrumb);
            FolderItems.Navigate(item);
            lock (syncObj)
            {
                OpenPage(FolderItems, null);
            }
        }

        public void NavigateToPath(string path, string breadcrumb)
        {
            FolderItems = new FolderItemListMCE(FolderItems, breadcrumb);
            FolderItems.Navigate(path);
            lock (syncObj)
            {
                Microsoft.MediaCenter.UI.Application.DeferredInvokeOnWorkerThread(CacheData, Done, FolderItems);
                OpenPage(FolderItems, path);
            }
        }

        private void NavigateToVirtualFolder(VirtualFolder virtualFolder, string breadcrumb)
        {
            FolderItems = new FolderItemListMCE(FolderItems, breadcrumb);
            FolderItems.Navigate(virtualFolder);
            lock (syncObj)
            {
                Microsoft.MediaCenter.UI.Application.DeferredInvokeOnWorkerThread(CacheData, Done, FolderItems);
                OpenPage(FolderItems, null);
            }
        }

        public void OpenPage(FolderItemListMCE items, string path)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties["Application"] = this;
            properties["FolderItems"] = items;
            properties["Model"] = new ListPage(items, path);

            if (session != null)
            {
                session.GoToPage("resx://SamSoft.VideoBrowser/SamSoft.VideoBrowser.Resources/Page", properties);
            }
            else
            {
                Debug.WriteLine("GoToMenu");
            }
        }

        public void Navigate(BaseFolderItem item)
        {
            FolderItem fi;

            // we need to upgrade
            if (item is CachedFolderItem)
            {
                fi = new FolderItem(item.Filename, item.IsFolder, item.Description);
                if (Helper.IsVirtualFolder(item.Filename))
                {
                    fi.VirtualFolder = new VirtualFolder(item.Filename);
                }
            }
            else
            {
                fi = item as FolderItem; 
            }

            if (item.IsMovie)
            {
                fi.EnsureMetadataLoaded();
                // go to details screen 
                Dictionary<string, object> properties = new Dictionary<string, object>();
                properties["Application"] = this;
                properties["FolderItem"] = fi;
                session.GoToPage("resx://SamSoft.VideoBrowser/SamSoft.VideoBrowser.Resources/MovieDetailsPage", properties);
                return;
            }

            if (fi.VirtualFolder != null || (fi.IsFolder && !fi.IsVideo))
            {
                if (fi.VirtualFolder != null)
                {
                    NavigateToVirtualFolder(fi.VirtualFolder, fi.Description);
                }
                else if (fi.Contents == null)
                {
                    NavigateToPath(fi.Filename, fi.Description);
                }
                else
                {
                    NavigateToItems(fi.Contents, fi.Description);
                }
            }
            else
            {
                // decide if we need to play or resume
                if (fi.CanResume)
                {
                    // Todo, activate the resume window
                    fi.Resume();
                }
                else
                {
                    fi.Play();
                }
                item.FirePropertyChanged_Public("HaveWatched");
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
            keepAliveTimer.Dispose();
            GC.SuppressFinalize(this);
        }

        #endregion

        
    }
}