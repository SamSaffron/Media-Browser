using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library;

namespace NndbMetadataProvider {
    class Plugin : IPlugin {



        public void Init(Kernel kernel) {
            kernel.MetadataProviderFactories.Add(MetadataProviderFactory.Get<NndbPeopleProvider>());
        }

        public string Name {
            get { return "Nndb image provider"; }
        }

        public string Description {
            get { return "Downloads actor and director images from nndb.com"; }
        }
    }
}
