using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Filesystem;

namespace TestMediaBrowser.SupportingClasses {
    class MockMediaLocation : IMediaLocation {

        public MockMediaLocation() { }

        public MockMediaLocation(string path) {
            this.Path = path;
        }

        public IFolderMediaLocation Parent {
            get;
            set;
        }

        public string Path {
            get;
            set;
        }

        public string Name {
            get {
                return Path.Split('\\').Last();
            }
        }

        public string Contents { get; set; }

        public DateTime DateModified {
            get;
            set;
        }

        public DateTime DateCreated {
            get;
            set;
        }

    }
}
