using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library.Logging;

namespace ITunesTrailers {
    public class Plugin : IPlugin {

        static readonly Guid TrailersGuid = new Guid("{828DCFEF-AEAF-44f2-B6A8-32AEAF27F3DA}");
        public static ILogger Logger; 

        public void Init(LibraryConfig config) {
            var trailers = new ITunesTrailerFolder();
            trailers.Name = "Trailers";
            trailers.Path = "";
            trailers.Id = TrailersGuid;
            Logger = config.Logger;
            config.RootFolder.AddVirtualChild(trailers);
        }

    }
}
