using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.EntityDiscovery;
using MediaBrowser.Code.ModelItems;
using MediaBrowser.Library.RemoteControl;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library.Factories;

namespace MediaBrowser.Library.Plugins {
    public class LibraryConfig {

        public LibraryConfig(
            AggregateFolder rootFolder, 
            List<IPlaybackController> playbackControllers, 
            List<MetadataProviderFactory> providers, 
            List<EntityResolver> entityResolvers,
            List<ImageResolver> imageResolvers,
            ILogger logger
            ) 
        {
            RootFolder = rootFolder;
            PlaybackControllers = playbackControllers;
            Providers = providers;
            EntityResolvers = entityResolvers;
            ImageResolvers = imageResolvers;
            Logger = logger;
        }

        public AggregateFolder RootFolder { get; private set; }
        public List<IPlaybackController> PlaybackControllers { get; private set; }
        public List<MetadataProviderFactory> Providers { get; private set; }
        public List<EntityResolver> EntityResolvers { get; private set; }
        public List<ImageResolver> ImageResolvers { get; private set; }
        public ILogger Logger { get; set; }
    }
}
