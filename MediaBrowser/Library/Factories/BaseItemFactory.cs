using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.EntityDiscovery;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Filesystem;
using MediaBrowser.Library.Interfaces;

namespace MediaBrowser.Library.Factories {
    public class BaseItemFactory : IBaseItemFactory {
        public static readonly BaseItemFactory Instance = new BaseItemFactory();

        private static ChainedEntityResolver resolver = new ChainedEntityResolver() { 
            new VodCastResolver(),
            new EpisodeResolver(), 
            new SeasonResolver(), 
            new SeriesResolver(), 
            new MovieResolver(
                    Config.Instance.EnableMoviePlaylists?Config.Instance.PlaylistLimit:1, 
                    Config.Instance.EnableNestedMovieFolders), 
            new FolderResolver(),
            
        };

        public BaseItem Create(IMediaLocation location) {
            BaseItem item = null;

            BaseItemFactoryBase factory;
            IEnumerable<InitializationParameter> setup;

            resolver.ResolveEntity(location, out factory, out setup);
            if (factory != null) {
                item = factory.CreateInstance(location, setup);
            }

            return item;
        }
    }
}
