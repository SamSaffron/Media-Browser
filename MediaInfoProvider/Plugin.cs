using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library;
using MediaBrowser.Library.Logging;

namespace MediaInfoProvider {
    public class Plugin : BasePlugin {

        public override void Init(Kernel kernel) {
            kernel.MetadataProviderFactories.Add(MetadataProviderFactory.Get<MediaInfoProvider>()); 
        }

        public override string Name {
            get { return "MediaInfo Provider"; }
        }

        public override string Description {
            get { return "This plugin provides rich information about your media using the MediaInfo project."; }
        }
    }
}
