using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.MediaCenter.Hosting;
using MediaBrowser.LibraryManagement;
using Microsoft.MediaCenter;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace MediaBrowser.Library
{
    /// <summary>
    /// Encapsulates play back of different types of item. Builds playlists, mounts iso etc. where appropriate
    /// </summary>
    abstract class PlayableItem
    {
        static PlayableItem previousPlayable;

        static Transcoder transcoder;
        private string fileToPlay;
        protected PlayState PlayState { get; private set; }
        public PlayableItem()
        {

        }

        public abstract void Prepare(bool resume);
        public abstract string Filename { get; }

        public virtual void Play(PlayState playstate, bool resume)
        {
            try
            {
                this.PlayState = playstate;
                this.Prepare(resume);
                if (!RunningOnExtender || !Config.Instance.EnableTranscode360 || Helper.IsExtenderNativeVideo(this.Filename))
                    Play(this.Filename);
                else
                {
                    // if we are on an extender, we need to start up our transcoder
                    try
                    {
                        PlayFileWithTranscode(this.Filename);
                    }
                    catch
                    {
                        // in case t360 is not installed - we may get an assembly loading failure 
                        Play(this.Filename);
                    }
                }

                if (resume)
                {
                    // todo: currently this will not resume in a playlist beyond the first entry, 
                    // it resumes the first entry at the time for the entry we were on, but doesn't get the right entry
                    // this is because the position we record is only the position in the current file
                    // we need to record the current item as well and skip to that item when we start playing
                    var mce = AddInHost.Current.MediaCenterEnvironment;
                    Trace.TraceInformation("Trying to play from position :" + new TimeSpan(this.PlayState.PositionTicks).ToString());
                    int i = 0;
                    while ((i++ < 15) && (mce.MediaExperience.Transport.PlayState != Microsoft.MediaCenter.PlayState.Playing))
                    {
                        // settng the position only works once it is playing and on fast multicore machines we can get here too quick!
                        Thread.Sleep(100);
                    }
                    BeforeResume();
                    mce.MediaExperience.Transport.Position = new TimeSpan(this.PlayState.PositionTicks);
                }

            }
            catch (Exception ex)
            {
                Trace.TraceError("Failed to play " + this.Filename + "\n" + ex.ToString());
            }
        }

        protected virtual void BeforeResume() 
        {
        }

        private void PlayFileWithTranscode(string filename)
        {
            if (transcoder == null)
                transcoder = new Transcoder();

            string bufferpath = transcoder.BeginTranscode(this.Filename);

            // if bufferpath comes back null, that means the transcoder i) failed to start or ii) they
            // don't even have it installed
            if (bufferpath == null)
            {
                AddInHost.Current.MediaCenterEnvironment.Dialog("Could not start transcoding process", "Transcode Error", new object[] { DialogButtons.Ok }, 10, true, null, delegate(DialogResult dialogResult) { });
                return;
            }
            Play(bufferpath);
        }

        private Microsoft.MediaCenter.UI.PropertyChangedEventHandler eventHandler; 

        private void Play(string file)
        {
            if (previousPlayable != null)
            {
                // save previous position before playing new file
                previousPlayable.Transport_PropertyChanged(null, "Position"); 
            }

            this.fileToPlay = file;
            var mce = AddInHost.Current.MediaCenterEnvironment;
            try
            {
                if (!mce.PlayMedia(MediaType.Video, this.fileToPlay, false))
                    Trace.TraceInformation("PlayMedia returned false");
            }
            catch (Exception ex)
            {
                Trace.TraceError("Playing media failed.\n" + ex.ToString());
                Application.ReportBrokenEnvironment();
                return;
            }
            eventHandler = new Microsoft.MediaCenter.UI.PropertyChangedEventHandler(Transport_PropertyChanged);
            Application.CurrentInstance.ShowNowPlaying = true;
            mce.MediaExperience.GoToFullScreen();
            this.PlayState.LastPlayed = DateTime.Now;
            this.PlayState.PlayCount = this.PlayState.PlayCount + 1;
            AddInHost.Current.MediaCenterEnvironment.MediaExperience.Transport.PropertyChanged += eventHandler;
            previousPlayable = this;
        }

        public virtual bool UpdatePosition(string title, long positionTicks)
        {
            var currentTitle = Path.GetFileNameWithoutExtension(this.fileToPlay);
            if ((title == currentTitle) || title.EndsWith("(" + currentTitle + ")"))
            {
                this.PlayState.PositionTicks = positionTicks;
                return true; 
            }
            else
            {
                return false;
            }
        }

        void Transport_PropertyChanged(Microsoft.MediaCenter.UI.IPropertyObject sender, string property)
        {
            if (property == "Position")
            {
                try
                {
                    var mce = AddInHost.Current.MediaCenterEnvironment.MediaExperience;
                    if (mce.Transport != null)
                    {
                        string title = null;
                        try
                        {
                            /*
                            foreach (var x in mce.MediaMetadata)
                                try
                                {
                                    Debug.WriteLine("Key=" + x.Key);
                                    Debug.WriteLine("Value=" + x.Value == null ? "" : x.Value.ToString());
                                }
                                catch { }
                             */

                            title = mce.MediaMetadata["Title"] as string;
                        }
                        catch (Exception e)
                        {
                            Trace.TraceError("Failed to get title on current media item!\n" + e.ToString());
                            return;
                        }

                        Trace.WriteLine("Update Postion for : " + title);
                        if (!UpdatePosition(title, mce.Transport.Position.Ticks))
                            AddInHost.Current.MediaCenterEnvironment.MediaExperience.Transport.PropertyChanged -= eventHandler;
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error trying in Transport_PropertyChanged.\n" + ex.ToString());
                }
            }
            else if (property == "PlayState")
            {
                MediaTransport mt = sender as MediaTransport;
                if (mt != null)
                {
                    var ps = mt.PlayState;
                    Application.CurrentInstance.ShowNowPlaying = ((ps == Microsoft.MediaCenter.PlayState.Playing) || (ps == Microsoft.MediaCenter.PlayState.Paused));
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
    }






}
