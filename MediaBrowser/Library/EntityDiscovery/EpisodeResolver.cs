using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Filesystem;
using MediaBrowser.Library.Factories;
using MediaBrowser.LibraryManagement;
using MediaBrowser.Library.Providers.TVDB;

namespace MediaBrowser.Library.EntityDiscovery {
    public class EpisodeResolver : EntityResolver {
        
        public override void ResolveEntity(IMediaLocation location, 
            out BaseItemFactoryBase factory, 
            out IEnumerable<InitializationParameter> setup) 
        {
            factory = null;
            setup = null;

            if (!(location is IFolderMediaLocation) && Helper.IsVideo(location.Path) && TVUtils.IsEpisode(location.Path)) {
                factory = BaseItemFactory<Episode>.Instance; 
            }
        }
    }
}
