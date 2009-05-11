using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library;

namespace DvrmsMetadataProvider {
    public class Plugin : IPlugin {

        public void Init(LibraryConfig config) {
            config.Providers.Add(new MetadataProviderFactory(typeof(DvrmsMetadataProvider))); 
        }

    }
}
