using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Logging;

namespace FilmTrailerPlugin {
    class Plugin : IPlugin {

        public static ILogger Logger;


        static readonly Guid TrailersGuid = new Guid("{B70517FE-9B66-44a7-838B-CC2A2B6FEC0C}");

        public void Init(LibraryConfig config) {
            var trailers = new FilmTrailerFolder();
            trailers.Name = "Trailers";
            trailers.Path = "";
            trailers.Id = TrailersGuid;
            Logger = config.Logger;
            config.RootFolder.AddVirtualChild(trailers);
        }

    }
}
