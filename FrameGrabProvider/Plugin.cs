using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library;

namespace FrameGrabProvider {
    public class Plugin : IPlugin {

        public static ILogger Logger {get; private set;}

        public void Init(LibraryConfig config) {
            Logger = config.Logger;

            config.Providers.Add(new MetadataProviderFactory(typeof(FrameGrabProvider)));

            config.ImageResolvers.Add(path => {
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
