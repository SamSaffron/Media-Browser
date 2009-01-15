using System;
using System.Collections.Generic;
using System.Text;
using MediaBrowser.LibraryManagement;
using System.IO;

namespace MediaBrowser.Library.Playables
{
    class PlayableVideoFile : PlayableItem
    {
        string file;
        public PlayableVideoFile(string file)
            : base()
        {
            this.file = file;
        }

        public override void Prepare(bool resume)
        {

        }

        public override string Filename
        {
            get { return file; }
        }

        public static bool CanPlay(string file)
        {
            return Helper.IsVideo(file);
        }
    }
}
