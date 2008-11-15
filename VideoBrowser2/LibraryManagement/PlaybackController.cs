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
using System.Threading;

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
                //Trace.TraceInformation("Transport property changed: " + property);
                if (property == "Position")                {
                    try
                    {
                        var mce = AddInHost.Current.MediaCenterEnvironment.MediaExperience;
                        if (mce.Transport != null)
                        {
                            //Trace.TraceInformation("PlayState: " + mce.Transport.PlayState.ToString());
                            //Trace.TraceInformation("Position: " + mce.Transport.Position.ToString());
                            string title = null;
                            string currentItem = Path.GetFileNameWithoutExtension(currentPlaybackController.VideoFilename);
                            try
                            {
                                title = mce.MediaMetadata["Title"] as string;
                            }
                            catch (Exception e)
                            {
                                Trace.TraceInformation("Failed to get title on current media item!\n" + e.ToString());
                                return;
                            }

                            if (title == currentItem)
                            {
                                currentPlaybackController.folderItem.PlayState.Position = mce.Transport.Position;
                                if (mce.Transport.PlayState != Microsoft.MediaCenter.PlayState.Playing)
                                    currentPlaybackController.folderItem.PlayState.Save(); // force the save
                                //Trace.TraceInformation("Playstate position recorded.");
                            }
                            else
                                Trace.TraceInformation("Property " + property + " changed for " + title + " currentController was on " + currentItem);
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("Error trying in Transport_PropertyChanged.\n" + ex.ToString());
                    }
                }
            }
        }

        static PlaybackController currentPlaybackController;
        static MediaTransport currentTransport;

        static void ListenForChanges(PlaybackController playbackController)
        {
            // no resume support for DVDs yet 
            if (playbackController.folderItem.IsFolder && playbackController.folderItem.ContainsDvd)
            {
                return;
            }

            // No resume support for playlists
            // There is no way for us to figure out our position in the play list. 
            if (playbackController.folderItem.IsFolder && playbackController.folderItem.GetMovieList().Length > 1)
            {
                return;
            }
            try
            {
                if (currentTransport != AddInHost.Current.MediaCenterEnvironment.MediaExperience.Transport)
                {
                    if (currentTransport != null)
                        currentTransport.PropertyChanged -= new PropertyChangedEventHandler(Transport_PropertyChanged);
                    currentTransport = AddInHost.Current.MediaCenterEnvironment.MediaExperience.Transport;
                    currentTransport.PropertyChanged += new PropertyChangedEventHandler(Transport_PropertyChanged);
                }
            }
            catch (Exception e)
            {
                Trace.TraceInformation("Failed trying to hook Transport.PropertyChanged.\n" + e.ToString());
            }
            currentPlaybackController = playbackController;
        }

        static void currentTransport_PropertyChanged(IPropertyObject sender, string property)
        {
            throw new NotImplementedException();
        }
        
        private FolderItem folderItem;
        
        public PlaybackController(FolderItem folderItem)
        {
            this.folderItem = folderItem;

        }

        public void Resume()
        {
            if (folderItem == null)
            {
                return;
            }
            PlayFile(VideoFilename);

            var mce = AddInHost.Current.MediaCenterEnvironment;
            Trace.TraceInformation("Trying to play from position :" + folderItem.PlayState.Position.ToString());
            int i = 0;
            while ((i++ < 15) && (mce.MediaExperience.Transport.PlayState != Microsoft.MediaCenter.PlayState.Playing))
            {
                // settng the position only works once it is playing and on fast multicore machines we can get here too quick!
                Thread.Sleep(100);
            }
            mce.MediaExperience.Transport.Position = folderItem.PlayState.Position;
            folderItem.PlayState.LastPlayed = DateTime.Now;
            folderItem.PlayState.PlayCount = folderItem.PlayState.PlayCount + 1;
        } 

        public void Play()
        {
            if (folderItem == null)
            {
                return;
            }

            PlayFile(VideoFilename);
            folderItem.PlayState.LastPlayed = DateTime.Now;
            folderItem.PlayState.PlayCount = folderItem.PlayState.PlayCount + 1;

        }

        public bool CanResume
        {
            get
            {
                return folderItem.PlayState.Position.Milliseconds > 0;
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
                try
                {
                    bool isLocal = AddInHost.Current.MediaCenterEnvironment.Capabilities.ContainsKey("Console") &&
                             (bool)AddInHost.Current.MediaCenterEnvironment.Capabilities["Console"];
                    return !isLocal;
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error in RunningOnExtender.\n" + ex.ToString());
                    Application.ReportBrokenEnvironment();
                    throw;
                }
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

                // in case something else is playing try to save its position
                if (currentPlaybackController != null)
                {
                    Transport_PropertyChanged(null, "Position");
                    currentPlaybackController.folderItem.PlayState.Save(); // force the save
                }
                // Get access to Windows Media Center host.
                var mce = AddInHost.Current.MediaCenterEnvironment;
                try
                {
                    // Play the video in the Windows Media Center view port.
                    if (!mce.PlayMedia(MediaType.Video, filename, false))
                    {
                        Trace.TraceInformation("PlayMedia returned false");
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceInformation("Playing media failed.\n" + ex.ToString());
                    Application.ReportBrokenEnvironment();
                    return;
                }
                mce.MediaExperience.GoToFullScreen();
                ListenForChanges(this);

            }
            catch (Exception e)
            {
                // Failed to play the movie, log it
                //Trace.TraceInformation("Failed to load movie : " + e.ToString());
                Trace.TraceInformation("Error in PlayFilewithoutTranscode file: " + filename + "\n" + e.ToString());
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
                //Trace.TraceInformation("Failed to load movie : " + e.ToString());
                Trace.TraceInformation("Error playing file: " + filename + "\n" + e.ToString());
                Application.ReportBrokenEnvironment();
            }
        }    

    }
}
