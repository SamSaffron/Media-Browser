using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library;

namespace NndbMetadataProvider {
    class Plugin : BasePlugin {

        public override void Init(Kernel kernel) {
            kernel.MetadataProviderFactories.Add(MetadataProviderFactory.Get<NndbPeopleProvider>());
        }

        public override string Name {
            get { return "Nndb image provider"; }
        }

        public override string Description {
            get { return "Downloads actor and director images from nndb.com"; }
        }
    }
}
