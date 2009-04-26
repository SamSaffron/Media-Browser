using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Entities;

namespace MediaBrowser.Library.Playables {
    public class PlayableCollection : PlayableItem {

        string filename;
        IEnumerable<Video> videos;
        string name;

        public PlayableCollection(string name, IEnumerable<Video> videos) {
            this.videos = videos;
            this.name = name;
        }

        public override void Prepare(bool resume) {
            var files = videos.Where(v => !v.ContainsRippedMedia).Select(v2 => v2.VideoFiles).SelectMany(i=>i);
            filename = CreateWPLPlaylist(name, files);
        }

        public override string Filename {
            get { return filename; }
        }

    }
}
