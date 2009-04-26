using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaBrowser.Library.Entities {
    public class VodcastVideo : Video {

        public override bool RefreshMetadata(MediaBrowser.Library.Metadata.MetadataRefreshOptions options) {
            // do nothing
            // Metadata is assigned outside the provider framework
            return false;
        }
    }
}
