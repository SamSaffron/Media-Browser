using System;
using System.Collections.Generic;
using System.Text;
using MediaBrowser.LibraryManagement;
using System.IO;
using MediaBrowser.Library.Entities;
using System.Linq;

namespace MediaBrowser.Library.Playables
{
    class PlayableVideoFile : PlayableItem
    {
        Video video;
        string path;
        public PlayableVideoFile(Video video)
            : base()
        {
            this.video = video;
            this.path = video.VideoFiles.First();
        }

        public override void Prepare(bool resume)
        {

        }

        public override string Filename
        {
            get { return path; }
        }

        public static bool CanPlay(Video video)
        {
            // can play DVDs and normal videos
            return video.VideoFiles.Count() == 1 && !video.ContainsRippedMedia;
        }
    }
}
