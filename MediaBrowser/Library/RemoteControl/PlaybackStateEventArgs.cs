using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaBrowser.Library.RemoteControl {
    public class PlaybackStateEventArgs : EventArgs {
        public string Title { get; set; }
        public long Position { get; set; }
    }
}
