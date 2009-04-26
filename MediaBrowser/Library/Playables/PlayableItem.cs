using System;
using MediaBrowser.LibraryManagement;
using System.Diagnostics;
using System.IO;
using MediaBrowser.Library.Entities;
using System.Text;
using System.Collections.Generic;
using System.Xml;
using MediaBrowser.Code.ModelItems;
using MediaBrowser.Library.RemoteControl;

namespace MediaBrowser.Library
{

    /// <summary>
    /// Encapsulates play back of different types of item. Builds playlists, mounts iso etc. where appropriate
    /// </summary>
    public abstract class PlayableItem
    {

        IPlaybackController playbackController = Application.CurrentInstance.PlaybackController;
        public IPlaybackController PlaybackController {
            get {
                return playbackController;
            }
            set
            {
            	playbackController = value;
            }
        }

        static Transcoder transcoder;
        private string fileToPlay;
        protected PlaybackStatus PlayState { get; private set; }
        public PlayableItem()
        {

        }

        public abstract void Prepare(bool resume);
        public abstract string Filename { get; }

        public void Play(PlaybackStatus playstate, bool resume)
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

                if (resume) {
                    PlaybackController.Seek(PlayState.PositionTicks);
                }
                 

            }
            catch (Exception ex)
            {
                Application.Logger.ReportException("Failed to play " + this.Filename,  ex);
            }
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
                Application.DisplayDialog("Could not start transcoding process", "Transcode Error");
                return;
            }
            PlayAndGoFullScreen(bufferpath);
        }


        private void PlayAndGoFullScreen(string file)
        {
            this.fileToPlay = file;
            Play(file);
            PlaybackController.GoToFullScreen();
            MarkWatched();

            PlaybackController.OnProgress += new EventHandler<PlaybackStateEventArgs>(PlaybackController_OnProgress);
        }

        public virtual void Play(string file) {
            PlaybackController.PlayVideo(file);
        }
        

        void PlaybackController_OnProgress(object sender, PlaybackStateEventArgs e) {
            if (!UpdatePosition(e.Title, e.Position)) {
                PlaybackController.OnProgress -= new EventHandler<PlaybackStateEventArgs>(PlaybackController_OnProgress);
            }
        }

        protected void MarkWatched()
        {
            if (PlayState != null) {
                PlayState.LastPlayed = DateTime.Now;
                PlayState.PlayCount = PlayState.PlayCount + 1;
                PlayState.Save();
            }
        }

        public virtual bool UpdatePosition(string title, long positionTicks)
        {
            if (PlayState == null) {
                return false;
            }

            var currentTitle = Path.GetFileNameWithoutExtension(this.fileToPlay);
            if ((title == currentTitle) || title.EndsWith("(" + currentTitle + ")"))
            {
                PlayState.PositionTicks = positionTicks;
                PlayState.Save();
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
                return Application.RunningOnExtender;
            }
        }


        protected static string CreateWPLPlaylist(string name, IEnumerable<string> videoFiles) {
            var playListFile = Path.Combine(Helper.AutoPlaylistPath, name + ".wpl");


            StringWriter writer = new StringWriter();
            XmlTextWriter xml = new XmlTextWriter(writer);

            xml.Indentation = 2;
            xml.IndentChar = ' ';

            xml.WriteStartElement("smil");
            xml.WriteStartElement("body");
            xml.WriteStartElement("seq");

            foreach (string file in videoFiles) {
                xml.WriteStartElement("media");
                xml.WriteAttributeString("src", file);
                xml.WriteEndElement();
            }

            xml.WriteEndElement();
            xml.WriteEndElement();
            xml.WriteEndElement();

            System.IO.File.WriteAllText(playListFile, @"<?wpl version=""1.0""?>" + writer.ToString());

            return playListFile;
        }
    }

}
