using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Metadata;

namespace ITunesTrailers {
    public class ITunesTrailer : Movie {
        public override bool RefreshMetadata(MetadataRefreshOptions options) {
            // do nothing, metadata is assigned external to the provider framework
            return false;
        }
    }
}
