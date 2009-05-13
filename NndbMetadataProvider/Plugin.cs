using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library;

namespace NndbMetadataProvider {
    class Plugin : IPlugin {

        internal static ILogger Logger { get; private set; }

        public void Init(LibraryConfig config) {
            Logger = config.Logger;

            config.Providers.Add(MetadataProviderFactory.Get<NndbPeopleProvider>());
        }

        public string Name {
            get { return "Nndb image provider"; }
        }

        public string Description {
            get { return "Downloads actor and director images from nndb.com"; }
        }
    }
}
