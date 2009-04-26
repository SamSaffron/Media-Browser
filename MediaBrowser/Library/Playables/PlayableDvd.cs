using System;
using System.Collections.Generic;
using System.Text;
using MediaBrowser.LibraryManagement;
using MediaBrowser.Library.Entities;

namespace MediaBrowser.Library.Playables
{
    class PlayableDvd : PlayableItem
    {
        Video video;

        public PlayableDvd(Video video)
            : base()
        {
            this.video = video;
        }

        public override void Prepare(bool resume)
        {
        }

        public override void Play(string file) {
            Application.CurrentInstance.PlaybackController.PlayDVD(file);
        }

        public override string Filename
        {
            get { return video.Path; }
        }

        public static bool CanPlay(Video video)
        {
            return video.MediaType == MediaType.DVD;
        }
    }
}
