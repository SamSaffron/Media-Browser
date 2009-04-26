using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaBrowser.Library.Entities {
    public class Media : BaseItem{
        PlaybackStatus PlayState {get; set; }
    }
}
