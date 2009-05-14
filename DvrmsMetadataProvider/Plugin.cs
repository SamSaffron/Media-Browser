using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library;

namespace DvrmsMetadataProvider {
    public class Plugin : IPlugin {

        public void Init(Kernel kernel) {
            kernel.MetadataProviderFactories.Add(new MetadataProviderFactory(typeof(DvrmsMetadataProvider))); 
        }

        public string Name {
            get { return "DVR-MS metadata."; }
        }

        public string Description {
            get { return "This plugin provides metadata for DVR-MS files. (all your recorded tv shows start off as dvr-ms files)"; }
        }
    }
}
