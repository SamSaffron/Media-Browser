using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.MediaCenter.UI;
using System.Threading;
using Microsoft.MediaCenter.Hosting;
using System.Diagnostics;
using Microsoft.MediaCenter;
using MediaBrowser.Code.ModelItems;
using MediaBrowser.Library.RemoteControl;
using MediaBrowser.Util;


namespace MediaBrowser {

    public class PlaybackController : BaseModelItem, IPlaybackController {

        EventHandler<PlaybackStateEventArgs> progressHandler;

        // dont allow multicast events 
        public event EventHandler<PlaybackStateEventArgs> OnProgress { 
            add
            {
                progressHandler = value;
            } 
            remove
            {
                if (progressHandler == value)
                {
                    progressHandler = null;
                }
            }
        }

        // Default controller can play everything
        public bool CanPlay(string filename) {
            return true;
        }

        // commands are not routed in this way ... 
        public void ProcessCommand(RemoteCommand command) { 
            // dont do anything (only plugins need to handle this)
        }

        public PlaybackController() {
            PlayState = PlayState.Undefined;
            Thread t = new Thread(GovernatorThreadProc);
            t.IsBackground = true;
            t.Start();
        }

        bool lastWasDVD = true;
        public void PlayDVD(string path) {
            PlayPath(path);
            lastWasDVD = true;
        }

        public void PlayVideo(string path) {
            if (lastWasDVD) mediaTransport = null;
            PlayPath(path);
            lastWasDVD = false;
        }

        public void Seek(long position) {
            var mce = AddInHost.Current.MediaCenterEnvironment;
            Application.Logger.ReportInfo("Trying to seek position :" + new TimeSpan(position).ToString());
            int i = 0;
            while ((i++ < 15) && (mce.MediaExperience.Transport.PlayState != Microsoft.MediaCenter.PlayState.Playing)) {
                // settng the position only works once it is playing and on fast multicore machines we can get here too quick!
                Thread.Sleep(100);
            }
            mce.MediaExperience.Transport.Position = new TimeSpan(position);
        }


        private static void PlayPath(string path) {
            try {
                if (!AddInHost.Current.MediaCenterEnvironment.PlayMedia(Microsoft.MediaCenter.MediaType.Video, path, false)) {
                    Application.Logger.ReportInfo("PlayMedia returned false");
                }
            } catch (Exception ex) {
                Application.Logger.ReportException("Playing media failed.", ex);
                Application.ReportBrokenEnvironment();
                return;
            }
        }

        public void GoToFullScreen() {
            try {
                using (new Profiler("Time to go to Full Screen"))
                {
                    AddInHost.Current.MediaCenterEnvironment.MediaExperience.GoToFullScreen();
                }
            } catch (Exception e) {
                // dont crash the UI thread
                Application.Logger.ReportException("Failed to go to full screen", e);
                AddInHost.Current.MediaCenterEnvironment.Dialog("We can not maximize the window for some reason! " + e.Message, "", Microsoft.MediaCenter.DialogButtons.Ok, 0, true);
            }
        }

        #region Playback status


        public bool IsPlaying {
            get { return PlayState == PlayState.Playing; }
        }

        public bool IsStopped {
            get { return PlayState == PlayState.Stopped; }
        }

        public bool IsPaused {
            get { return PlayState == PlayState.Paused; }
        }

        public PlayState PlayState { get; private set; }

        #endregion
        const int ForceRefreshMillisecs = 5000;
        private void GovernatorThreadProc()
        {
            try {
                while (true) {
                    Thread.Sleep(ForceRefreshMillisecs);
                    Microsoft.MediaCenter.UI.Application.DeferredInvoke( _ => AttachAndUpdateStatus());
                }
            }
            catch(Exception e)
            {
                Application.Logger.ReportException("Governator thread proc died!", e); 
            }
        }

        private void AttachAndUpdateStatus() {
            try {
                var transport = MediaTransport;
                if (transport != null) {
                    if (transport.PlayState != PlayState) {
                        ReAttach();
                    }
                    UpdateStatus();
                }
            } catch (Exception e) {
                // dont crash the background thread 
                Application.Logger.ReportException("FAIL: something is wrong with media experience!", e);
                mediaTransport = null;
            }
        }

        private MediaExperience MediaExperience {
            get {
                return AddInHost.Current.MediaCenterEnvironment.MediaExperience;
            }
        }

        private MediaTransport mediaTransport;
        private MediaTransport MediaTransport {
            get {
                if (mediaTransport != null) return mediaTransport;
                try {
                    var experience = AddInHost.Current.MediaCenterEnvironment.MediaExperience;
                    if (experience != null) {
                        mediaTransport = experience.Transport;
                    }
                } catch (InvalidOperationException e) { 
                    // well if we are inactive we are not allowed to get media experience ...
                    Application.Logger.ReportException("EXCEPTION : ", e);
                }
                return mediaTransport;
            }
        }

        void ReAttach() {
            var transport = MediaTransport;
            if (transport != null) {
                transport.PropertyChanged -= new PropertyChangedEventHandler(TransportPropertyChanged);
                transport.PropertyChanged += new PropertyChangedEventHandler(TransportPropertyChanged);  
            }
        }

        DateTime lastCall = DateTime.Now;

        void TransportPropertyChanged(IPropertyObject sender, string property) {
            // protect against really agressive calls
            var diff = (DateTime.Now - lastCall).TotalMilliseconds;
            if (diff < 1000 && diff >= 0) {
                return;
            }
            lastCall = DateTime.Now;
            UpdateStatus();
        }


        long position;
        string title;
        private void UpdateStatus() {
            var transport = MediaTransport;
            PlayState state = PlayState.Undefined;
            if (transport != null) {
                state = transport.PlayState;
                long position = transport.Position.Ticks;
                string title = null;
                try {
                    title = MediaExperience.MediaMetadata["Title"] as string;
                } catch (Exception e) {
                    Application.Logger.ReportException("Failed to get title on current media item!", e);
                }

                if (title != null && progressHandler != null && (this.title != title || this.position != position)) {
                    progressHandler(this, new PlaybackStateEventArgs() {Position = position, Title = title});
                    this.title = title;
                    this.position = position;
                }
            }

            if (state != PlayState) {
                PlayState = state;
                Microsoft.MediaCenter.UI.Application.DeferredInvoke( _ => PlayStateChanged());
                Application.CurrentInstance.ShowNowPlaying = (
                    (state == Microsoft.MediaCenter.PlayState.Playing) || 
                    (state == Microsoft.MediaCenter.PlayState.Paused));
            }
        }

        private void PlayStateChanged() {
            FirePropertyChanged("PlayState");
            FirePropertyChanged("IsPlaying");
            FirePropertyChanged("IsStopped");
            FirePropertyChanged("IsPaused");
        }

        public void Pause() {
            var transport = MediaTransport;
            if (transport != null) {
                transport.PlayRate = 1;
            }
        }

     
    }
}
