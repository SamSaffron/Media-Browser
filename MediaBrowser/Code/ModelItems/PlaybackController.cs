using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.MediaCenter.UI;
using System.Threading;
using Microsoft.MediaCenter.Hosting;
using System.Diagnostics;
using Microsoft.MediaCenter;


namespace MediaBrowser {

    public class PlaybackState : EventArgs
    {
        public string Title {get; set;}
        public long Position { get; set; }
    }

    public class PlaybackController : ModelItem {

        EventHandler<PlaybackState> progressHandler;

        // dont allow multicast events 
        public event EventHandler<PlaybackState> OnProgress { 
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

        public PlaybackController() {
            PlayState = PlayState.Undefined;
            Thread t = new Thread(GovernatorThreadProc);
            t.IsBackground = true;
            t.Start();
        }

        public void PlayVideo(string path) {
            try {
                if (!AddInHost.Current.MediaCenterEnvironment.PlayMedia(Microsoft.MediaCenter.MediaType.Video, path, false))
                    Trace.TraceInformation("PlayMedia returned false");
            } catch (Exception ex) {
                Trace.TraceError("Playing media failed.\n" + ex.ToString());
                Application.ReportBrokenEnvironment();
                return;
            }
        }

        public void GoToFullScreen() {
            try {
                AddInHost.Current.MediaCenterEnvironment.MediaExperience.GoToFullScreen();
            } catch (Exception e) {
                // dont crash the UI thread
                Trace.WriteLine("FAIL: " + e.Message);
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
            while (true) {
                Thread.Sleep(ForceRefreshMillisecs);
                try {
                    var transport = MediaTransport;
                    if (transport != null) {
                        if (transport.PlayState != PlayState) {
                            ReAttach();
                        }
                        UpdateStatus();
                    }
                } 
                catch (Exception e) { 
                    // dont crash the background thread 
                    Trace.WriteLine("FAIL: something is wrong with media experience!" + e.Message.ToString());
                }
            }
        }

        private MediaExperience MediaExperience {
            get {
                return AddInHost.Current.MediaCenterEnvironment.MediaExperience;
            }
        }

        private MediaTransport MediaTransport {
            get {
                try {
                    var experience = AddInHost.Current.MediaCenterEnvironment.MediaExperience;
                    if (experience != null) {
                        return experience.Transport;
                    }
                } catch (InvalidOperationException) { 
                    // well if we are inactive we are not allowed to get media experience ...
                }
                return null;
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
                    Trace.TraceError("Failed to get title on current media item!\n" + e.ToString());
                }

                if (title != null && progressHandler != null && (this.title != title || this.position != position)) {
                    progressHandler(this, new PlaybackState() {Position = position, Title = title});
                    this.title = title;
                    this.position = position;
                }
            }

            if (state != PlayState) {
                PlayState = state;
                Microsoft.MediaCenter.UI.Application.DeferredInvoke((object o) => PlayStateChanged());
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

        internal void Pause() {
            var transport = MediaTransport;
            if (transport != null) {
                transport.PlayRate = 1;
            }
        }
    }
}
