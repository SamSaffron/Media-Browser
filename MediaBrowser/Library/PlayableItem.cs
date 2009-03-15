using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.MediaCenter.Hosting;
using MediaBrowser.LibraryManagement;
using Microsoft.MediaCenter;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.MediaCenter.UI;

namespace MediaBrowser.Library
{

    /// <summary>
    /// Encapsulates play back of different types of item. Builds playlists, mounts iso etc. where appropriate
    /// </summary>
    public abstract class PlayableItem
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

        public void Play(PlayState playstate, bool resume)
        {
            this.PlayState = playstate;
            this.Prepare(resume);
            this.PlayInternal(resume);
        }

        protected virtual void PlayInternal(bool resume)
        {
            try
            {
                
                if (!RunningOnExtender || !Config.Instance.EnableTranscode360 || Helper.IsExtenderNativeVideo(this.Filename))
                    PlayAndGoFullScreen(this.Filename);
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
                        PlayAndGoFullScreen(this.Filename);
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
            PlayAndGoFullScreen(bufferpath);
        }


        private void PlayAndGoFullScreen(string file)
        {
            this.fileToPlay = file;
            Play(file);
            Application.CurrentInstance.PlaybackController.GoToFullScreen();
            MarkWatched();

            Application.CurrentInstance.PlaybackController.OnProgress += new EventHandler<PlaybackState>(PlaybackController_OnProgress);
            previousPlayable = this;
            Application.CurrentInstance.ShowNowPlaying = true;
        }

        public virtual void Play(string file) {
            Application.CurrentInstance.PlaybackController.PlayVideo(file);
        }
        

        void PlaybackController_OnProgress(object sender, PlaybackState e) {
            if (!UpdatePosition(e.Title, e.Position)) {
                Application.CurrentInstance.PlaybackController.OnProgress -= new EventHandler<PlaybackState>(PlaybackController_OnProgress);
            }
        }

        protected void MarkWatched()
        {
            this.PlayState.LastPlayed = DateTime.Now;
            this.PlayState.PlayCount = this.PlayState.PlayCount + 1;
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

        protected static bool RunningOnExtender
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
