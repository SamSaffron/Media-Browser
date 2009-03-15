using System;
using System.Collections.Generic;
using System.Text;
using MediaBrowser.LibraryManagement;

namespace MediaBrowser.Library.Playables
{
    class PlayableDvd : PlayableItem
    {
        string folder;

        public PlayableDvd(string folder)
            : base()
        {
            this.folder = folder;
        }

        public override void Prepare(bool resume)
        {
        }

        public override void Play(string file) {
            Application.CurrentInstance.PlaybackController.PlayDVD(file);
        }

        public override string Filename
        {
            get { return this.folder; }
        }

        public static bool CanPlay(string path)
        {
            if (Helper.IsDvd(path))
                return true;
            return (Helper.IsDvDFolder(path,null,null));
        }
    }
}
