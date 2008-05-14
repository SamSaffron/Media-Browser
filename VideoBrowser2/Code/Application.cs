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

                foreach (var item in data.folderItems)
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
                switch (previous_letter)
                {
                    case '1':
                        JILindex = 0 - CurrentIndex;
                        FindText = "";
                        break; 
                    case '2':
                        NavigateTo('a', counter);
                        break;
                    case '3':
                        NavigateTo('d', counter);
                        break;
                    case '4':
                        NavigateTo('g', counter);
                        break;
                    case '5':
                        NavigateTo('j', counter);
                        break;
                    case '6':
                        NavigateTo('m', counter);
                        break;
                    case '7':
                        NavigateTo('p', counter);
                        break;
                    case '8':
                        NavigateTo('t', counter);
                        break;
                    case '9':
                        NavigateTo('w', counter);
                        break; 

                    default:
                        break;
                }
            } 
            

        }

        void NavigateTo(char letter, int count)
        {
            List<char> letters = new List<char>();
            letters.Add(letter);
            letters.Add((char)(((byte)letter) + 1));
            letters.Add((char)(((byte)letter) + 2));
            if (letter == 'p' || letter == 'w')
            {
                letters.Add((char)(((byte)letter) + 3));
            }

            Dictionary<char, int> found_letters = new Dictionary<char,int>();

            int index = 0;

            // remove letters we do not have 
            foreach (var item in data.folderItems)
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

            count = count % found_letters.Count; 
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

        private FolderItemListMCE data;
        private static Application singleApplicationInstance;
        private AddInHost host;
        private MyHistoryOrientedPageSession session;

        private bool navigatingForward;

        public bool NavigatingForward
        {
            get { return navigatingForward; }
            set { navigatingForward = value; }
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
                if (item.IsFolder && !item.IsVideo)
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

                    try
                    {
                        MediaCenterEnvironment mce;
                        mce = host.MediaCenterEnvironment;    // Get access to Windows Media Center host.
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
            data.RefreshSortOrder();
        }

        public void NavigateToItems(List<IFolderItem> items)
        {
            data = new FolderItemListMCE();
            data.Navigate(items);
            OpenPage(data); 
        }

        public void NavigateToPath(string path)
        {    
            data = new FolderItemListMCE();
            data.Navigate(path);
            Microsoft.MediaCenter.UI.Application.DeferredInvokeOnWorkerThread(CacheData, Done, data);
            OpenPage(data); 
        }

        private void NavigateToVirtualFolder(VirtualFolder virtualFolder)
        {
            data = new FolderItemListMCE();
            data.Navigate(virtualFolder);
            Microsoft.MediaCenter.UI.Application.DeferredInvokeOnWorkerThread(CacheData, Done, data);
            OpenPage(data);
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