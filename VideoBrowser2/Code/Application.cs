using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.MediaCenter;
using Microsoft.MediaCenter.Hosting;
using Microsoft.MediaCenter.UI;
using SamSoft.VideoBrowser.LibraryManagement;
using System.Text;
using System;
using System.Reflection;
using System.Text.RegularExpressions;


namespace SamSoft.VideoBrowser
{
    public class MyHistoryOrientedPageSession : HistoryOrientedPageSession
    {

        private Application myApp;

        public Application Application
        {
            get { return myApp; }
            set { myApp = value; }
        }

        

        protected override void LoadPage(object target, string source, IDictionary<string, object> sourceData, IDictionary<string, object> uiProperties, bool navigateForward)
        {
            this.Application.NavigatingForward = navigateForward;
            if (!navigateForward)
            {
                if (uiProperties.ContainsKey("FolderItems"))
                {
                    this.Application.FolderItems = uiProperties["FolderItems"] as FolderItemListMCE;
                }
            }
            base.LoadPage(target, source, sourceData, uiProperties, navigateForward);
        }
    }

    public class Application : ModelItem
    {

        /* Triple type support  http://mytv.senseitweb.com/blogs/mytv/archive/2007/11/22/implementing-jil-jump-in-list-in-your-mcml-application.aspx */
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

        public Config Config
        {
            get
            {
                return Config.Instance;
                
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
                    if (item.Description.ToLower().StartsWith(match))
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
                char current_letter = item.Description.ToLower()[0];
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

        /**/

        public FolderItemListMCE FolderItems;
        private static Application singleApplicationInstance;
        private AddInHost host;
        private MyHistoryOrientedPageSession session;
        private Transcoder transcoder; 

        private bool navigatingForward;

        public bool NavigatingForward
        {
            get { return navigatingForward; }
            set { navigatingForward = value; }
        }

        private static Dictionary<char, List<char>> letterMap = new Dictionary<char, List<char>>();
        static Application()
        {
            // init the letter map for JIL list 
            AddLetterMapping('2',"abc2");
            AddLetterMapping('3',"def3");
            AddLetterMapping('4',"ghi4");
            AddLetterMapping('5',"jkl5");
            AddLetterMapping('6',"mno6");
            AddLetterMapping('7',"pqrs7");
            AddLetterMapping('8', "tuv8");
            AddLetterMapping('9',"wxyz9");
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

        public Application(MyHistoryOrientedPageSession session, AddInHost host)
        {

           // Debugger.Break();

            this.session = session;
            if (session != null)
            {
                this.session.Application = this;
            }
            this.host = host;
            singleApplicationInstance = this;

            _JILtext = new EditableText(this.Owner, "JIL");
            JILtext.Value = "";
            JILtext.Submitted += new EventHandler(JILtext_Activity);

            
        }

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

        public MediaCenterEnvironment MediaCenterEnvironment
        {
            get
            {
                if (host == null) return null;
                return host.MediaCenterEnvironment;
            }
        }

        public void GoToMenu()
        {
    
            transcoder = new Transcoder();
            NavigateToPath(Helper.MyVideosPath); 
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


        public void PlayMovie(FolderItem item)
        {
            if (item != null)
            {
                if (item.VirtualFolder!=null || (item.IsFolder && !item.IsVideo))
                {
                    if (item.VirtualFolder != null)
                    {
                        NavigateToVirtualFolder(item.VirtualFolder); 
                    }
                    else if (item.Contents == null)
                    {
                        NavigateToPath(item.Filename);
                    }
                    else
                    {
                        NavigateToItems(item.Contents);
                    }
                }
                else
                {
                    string filename = item.Filename;

                    if (item.IsFolder && !item.ContainsDvd)
                    {
                        string[] filenames = item.GetMovieList();

                        if (filenames.Length > 1)
                        {
                            filename = System.IO.Path.GetTempFileName();
                            filename += ".wpl";

                            // create a .wpl file and play it 
                            StringBuilder contents = new StringBuilder(@"
<?wpl version=""1.0""?>
<smil>
    <body>
        <seq>
");
                            foreach (string file in filenames)
                            {
                                contents.Append(@"<media src=""");
                                contents.Append(file);
                                contents.AppendLine(@"""/>");
                            }
                            contents.Append(@"
</seq>
    </body>
</smil>
");
                            System.IO.File.WriteAllText(filename, contents.ToString());
                        }
                        else
                        {
                            filename = filenames[0];
                        }

                    }

                    // Check to see if we are running on an extender... must have Full Trust permissions
                    Microsoft.MediaCenter.Hosting.AddInHost myHost = Microsoft.MediaCenter.Hosting.AddInHost.Current;

                    bool isLocal = myHost.MediaCenterEnvironment.Capabilities.ContainsKey("Console") &&
                             (bool)myHost.MediaCenterEnvironment.Capabilities["Console"];

                    // if we are on a mce host, we can just play the media
                    if (isLocal || !Config.Instance.EnableTranscode360 || Helper.IsExtenderNativeVideo(filename))
                    {
                        PlayFileWithoutTranscode(filename, host);
                    }

                    // if we are on an extender, we need to start up our transcoder
                    else
                    {
                        try
                        {
                            PlayFileWithTranscode(filename, host);
                        }
                        catch
                        {
                            // in case t360 is not installed - we may get an assembly loading failure 
                            PlayFileWithoutTranscode(filename, host);
                        }
                    }
                }
            }
        }

        private void PlayFileWithoutTranscode(string filename, Microsoft.MediaCenter.Hosting.AddInHost host)
        {
            try
            {
                // Get access to Windows Media Center host.
                MediaCenterEnvironment mce;
                mce = host.MediaCenterEnvironment;

                // Play the video in the Windows Media Center view port.
                mce.PlayMedia(MediaType.Video, filename, false);
                mce.MediaExperience.GoToFullScreen();
            }
            catch (Exception e)
            {
                // Failed to play the movie, log it
                Trace.WriteLine("Failed to load movie : " + e.ToString());
            }
        }

        private void PlayFileWithTranscode(string filename, Microsoft.MediaCenter.Hosting.AddInHost host)
        {
            string bufferpath = transcoder.BeginTranscode(filename);
            
            // if bufferpath comes back null, that means the transcoder i) failed to start or ii) they
            // don't even have it installed
            if (bufferpath == null)
            {
                MediaCenterEnvironment.Dialog("Could not start transcoding process", "Transcode Error", new object[] { DialogButtons.Ok }, 10, true, null, delegate(DialogResult dialogResult) { });
                return;
            }

            try
            {
                // Get access to Windows Media Center host.
                MediaCenterEnvironment mce;
                mce = host.MediaCenterEnvironment;

                // Play the video in the Windows Media Center view port.
                mce.PlayMedia(MediaType.Video, bufferpath, false);
                mce.MediaExperience.GoToFullScreen();
            }
            catch (Exception e)
            {
                // Failed to play the movie, log it
                Trace.WriteLine("Failed to load movie : " + e.ToString());
            }
        }    

        public void ShowNowPlaying()
        {
            host.ViewPorts.NowPlaying.Focus();
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

        public void NavigateToItems(List<IFolderItem> items)
        {
            FolderItems = new FolderItemListMCE();
            FolderItems.Navigate(items);
            OpenPage(FolderItems); 
        }

        public void NavigateToPath(string path)
        {    
            FolderItems = new FolderItemListMCE();
            FolderItems.Navigate(path);
            Microsoft.MediaCenter.UI.Application.DeferredInvokeOnWorkerThread(CacheData, Done, FolderItems);
            OpenPage(FolderItems); 
        }

        private void NavigateToVirtualFolder(VirtualFolder virtualFolder)
        {
            FolderItems = new FolderItemListMCE();
            FolderItems.Navigate(virtualFolder);
            Microsoft.MediaCenter.UI.Application.DeferredInvokeOnWorkerThread(CacheData, Done, FolderItems);
            OpenPage(FolderItems);
        }

        public void OpenPage(FolderItemListMCE items)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties["Application"] = this;
            properties["FolderItems"] = items;
            properties["Model"] = new ListPage(items);

            if (session != null)
            {
                session.GoToPage("resx://SamSoft.VideoBrowser/SamSoft.VideoBrowser.Resources/Page", properties);
            }
            else
            {
                Debug.WriteLine("GoToMenu");
            }
        }

        public void Navigate(IFolderItem item)
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

            PlayMovie(fi);
        }

    }
}