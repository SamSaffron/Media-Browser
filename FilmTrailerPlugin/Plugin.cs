using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library;

namespace FilmTrailerPlugin {
    class Plugin : IPlugin {

        static readonly Guid TrailersGuid = new Guid("{B70517FE-9B66-44a7-838B-CC2A2B6FEC0C}");

        public void Init(Kernel kernel) {
            var trailers = new FilmTrailerFolder();
            trailers.Name = "Trailers";
            trailers.Path = "";
            trailers.Id = TrailersGuid;

            kernel.RootFolder.AddVirtualChild(trailers);
        }

        public string Name {
            get { return "Film Trailers"; }
        }

        public string Description {
            get { return "Film Trailers powered by filmtrailer.com"; }
        }

    }
}
