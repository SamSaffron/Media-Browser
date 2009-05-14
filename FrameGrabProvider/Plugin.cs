using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library;

namespace FrameGrabProvider {
    public class Plugin : IPlugin {

        public void Init(Kernel kernel) {


            kernel.MetadataProviderFactories.Add(new MetadataProviderFactory(typeof(FrameGrabProvider)));

            kernel.ImageResolvers.Add(path =>
            {
                if (path.ToLower().StartsWith("grab")) {
                    return new GrabImage(); 
                }
                return null;
            });
        }


        public string Name {
             get { return "Frame Grab provider"; }
        }

        public string Description {
            get { return "This plugin provides frame grabs for videos which contain no cover art."; }
        }

    }
}
