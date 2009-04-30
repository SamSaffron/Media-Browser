using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.EntityDiscovery;
using MediaBrowser.Code.ModelItems;
using MediaBrowser.Library.RemoteControl;
using MediaBrowser.Library.Logging;

namespace MediaBrowser.Library.Plugins {
    public class LibraryConfig {

        public LibraryConfig(AggregateFolder rootFolder, List<IPlaybackController> playbackControllers, 
           List<MetadataProviderFactory> providers, List<EntityResolver> resolvers, ILogger logger) {
            RootFolder = rootFolder;
            PlaybackControllers = playbackControllers;
            Providers = providers;
            Resolvers = resolvers;
            Logger = logger;
        }

        public AggregateFolder RootFolder { get; private set; }
        public List<IPlaybackController> PlaybackControllers { get; private set; }
        public List<MetadataProviderFactory> Providers { get; private set; }
        public List<EntityResolver> Resolvers { get; private set; }
        public ILogger Logger { get; set; }
    }
}
