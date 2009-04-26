using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Entities;

namespace FilmTrailerPlugin {
    public class FilmTrailer : Movie {
        public override bool RefreshMetadata(MediaBrowser.Library.Metadata.MetadataRefreshOptions options) {
            // do nothing metadata is assigned externally.
            return false;
        }
    }
}
