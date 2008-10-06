using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.MediaCenter.Hosting;
using System.Diagnostics;
using Microsoft.MediaCenter.UI;
using Microsoft.MediaCenter;
using System.Runtime.InteropServices;
using System.Reflection;

namespace SamSoft.VideoBrowser.LibraryManagement
{
    /// <summary>
    /// Controls the playback for a folder item 
    /// </summary>
    public class PlaybackController
    {
       


        static void Transport_PropertyChanged(IPropertyObject sender, string property)
        {
            if (currentPlaybackController != null)
            {
                if (property == "Position")
                {
                    var mce = AddInHost.Current.MediaCenterEnvironment.MediaExperience;
                    string title = null; 
                    try
                    {
                        title = mce.MediaMetadata["Title"] as string;
                        /*
                        foreach (var key in mce.MediaMetadata)
                        {
                            var fi = mce.GetType().GetField("_legacyExperience", BindingFlags.NonPublic | BindingFlags.Instance);
                            object test = fi.GetValue(mce);
                            Trace.WriteLine(test.GetType().ToString());
                            Trace.WriteLine(test.GetType().Assembly.CodeBase);

                          

                           // if (key.Value != null)
                          //  {
                           //     Trace.WriteLine(key.Key + " : " + key.Value.ToString() + "  --- " + key.Value.GetType().ToString());
                           // }
                         
                        }
                         */
                    }
                    catch (Exception e)
                    {
                        Trace.WriteLine("Failed to get title!" + e.ToString());
                    }
                    if (title == Path.GetFileNameWithoutExtension(currentPlaybackController.folderItem.Filename))
                    {
                        currentPlaybackController.playState.Position = mce.Transport.Position;
                        Trace.WriteLine("Set the playlist.");
                    }
                }
            }
        }

        static PlaybackController currentPlaybackController;
        static PropertyChangedEventHandler transportChangedHandler;

        static void ListenForChanges(PlaybackController playbackController)
        {
            // no resume support for DVDs yet 
            if (playbackController.folderItem.IsFolder && playbackController.folderItem.ContainsDvd)
            {
                return;
            }

            // No resume support fot playlists
            // There is no way for us to figure out our position in the play list. 
            if (playbackController.folderItem.IsFolder && playbackController.folderItem.GetMovieList().Length > 1)
            {
                return;
            }

            // attempt to unhook previous handler
            if (transportChangedHandler == null)
            {
                try
                {
                    transportChangedHandler = new PropertyChangedEventHandler(Transport_PropertyChanged);
                    AddInHost.Current.MediaCenterEnvironment.MediaExperience.Transport.PropertyChanged += transportChangedHandler;
                }
                catch (Exception e)
                {
                    // Don't crash!
                    Trace.WriteLine("Failed to unhook previous handler. " + e.ToString());
                }
            }
            currentPlaybackController = playbackController;
        }
       

        private FolderItem folderItem;
        private PlayState playState;

        public PlaybackController(FolderItem folderItem)
        {
            this.folderItem = folderItem;
            this.playState = new PlayState(folderItem);

        }

        public void Resume()
        {
            if (folderItem == null)
            {
                return;
            }
            PlayFile(VideoFilename);

            var mce = AddInHost.Current.MediaCenterEnvironment;
            mce.MediaExperience.Transport.Position = playState.Position;
            Trace.WriteLine("Trying to play from position :" + playState.Position.ToString()); 
            
        } 

        public void Play()
        {
            if (folderItem == null)
            {
                return;
            }

            PlayFile(VideoFilename);
            playState.LastPlayed = DateTime.Now;
            playState.PlayCount = playState.PlayCount + 1;
 
        }

        public bool CanResume
        {
            get
            {
                return playState.Position.Milliseconds > 0;
            }
        }

        private string videoFilename;

        /// <summary>
        /// Create automatic play list or return the filename
        /// </summary>
        private string VideoFilename
        {
            get
            {
                // cache this
                if (videoFilename != null)
                {
                    return videoFilename;
                }

                videoFilename = folderItem.Filename;

                if (folderItem.IsFolder && !folderItem.ContainsDvd)
                {
                    string[] filenames = folderItem.GetMovieList();

                    if (filenames.Length > 1)
                    {
                        videoFilename = Path.Combine(Helper.AutoPlaylistPath, Path.GetFileNameWithoutExtension(folderItem.Filename) + ".wpl");

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
                        System.IO.File.WriteAllText(videoFilename, contents.ToString());
                    }
                    else
                    {
                        videoFilename = filenames[0];
                    }

                }
                return videoFilename;
            }

        }


        private void PlayFile(string filename)
        {
            // if we are on a mce host, we can just play the media
            if (!RunningOnExtender || !Config.Instance.EnableTranscode360 || Helper.IsExtenderNativeVideo(filename))
            {
                PlayFileWithoutTranscode(filename);
            }

            // if we are on an extender, we need to start up our transcoder
            else
            {
                try
                {
                    PlayFileWithTranscode(filename);
                }
                catch
                {
                    // in case t360 is not installed - we may get an assembly loading failure 
                    PlayFileWithoutTranscode(filename);
                }
            }
        }

        private static bool RunningOnExtender
        {
            get
            {
                bool isLocal = AddInHost.Current.MediaCenterEnvironment.Capabilities.ContainsKey("Console") &&
                         (bool)AddInHost.Current.MediaCenterEnvironment.Capabilities["Console"];
                return !isLocal;
            }
        }
        

        private void PlayFileWithoutTranscode(string filename)
        {
            try
            {
                if (Helper.isIso(filename))
                {
                    try
                    {
                        // Create the process start information.
                        Process process = new Process();
                        process.StartInfo.Arguments = "-mount 0,\"" + filename + "\"";
                        process.StartInfo.FileName = Config.Instance.DaemonToolsLocation;
                        process.StartInfo.ErrorDialog = false;
                        process.StartInfo.CreateNoWindow = true;

                        // We wait for exit to ensure the iso is completely loaded.
                        process.Start();
                        process.WaitForExit();

                        // Play the DVD video that was mounted.
                        filename = Config.Instance.DaemonToolsDrive + ":\\";
                    }
                    catch (Exception)
                    {
                        // Display the error in this case, they might wonder why it didn't work.
                        AddInHost.Current.MediaCenterEnvironment.Dialog("DaemonTools is not correctly configured.", "Could not load ISO", DialogButtons.Ok, 10,true);
                        throw (new Exception("Daemon tools is not configured correctly"));
                    }
                }

                // Get access to Windows Media Center host.
                var mce = AddInHost.Current.MediaCenterEnvironment;

                // Play the video in the Windows Media Center view port.
                mce.PlayMedia(MediaType.Video, filename, false);
                mce.MediaExperience.GoToFullScreen();
                ListenForChanges(this);

            }
            catch (Exception e)
            {
                // Failed to play the movie, log it
                Trace.WriteLine("Failed to load movie : " + e.ToString());
            }
        }

       

        Transcoder transcoder;
        private void PlayFileWithTranscode(string filename)
        {
            if (transcoder == null)
            {
                transcoder = new Transcoder();
            }

            string bufferpath = transcoder.BeginTranscode(filename);

            // if bufferpath comes back null, that means the transcoder i) failed to start or ii) they
            // don't even have it installed
            if (bufferpath == null)
            {
                AddInHost.Current.MediaCenterEnvironment.Dialog("Could not start transcoding process", "Transcode Error", new object[] { DialogButtons.Ok }, 10, true, null, delegate(DialogResult dialogResult) { });
                return;
            }

            try
            {
                // Get access to Windows Media Center host.
                var mce = AddInHost.Current.MediaCenterEnvironment;

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

    }
}
