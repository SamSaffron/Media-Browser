using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library;
using MediaBrowser.Library.Logging;

namespace MediaInfoProvider {
    public class Plugin : IPlugin {

        public static ILogger Logger { get; private set; }

        public void Init(LibraryConfig config) {
            Logger = config.Logger;

            config.Providers.Add(MetadataProviderFactory.Get<MediaInfoProvider>()); 
        }

    }
}
