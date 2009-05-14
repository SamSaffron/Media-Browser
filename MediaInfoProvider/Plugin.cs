using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library;
using MediaBrowser.Library.Logging;

namespace MediaInfoProvider {
    public class Plugin : IPlugin {

        public void Init(Kernel kernel) {
            kernel.MetadataProviderFactories.Add(MetadataProviderFactory.Get<MediaInfoProvider>()); 
        }

        public string Name {
            get { return "MediaInfo Provider"; }
        }

        public string Description {
            get { return "This plugin provides rich information about your media using the MediaInfo project."; }
        }
    }
}
