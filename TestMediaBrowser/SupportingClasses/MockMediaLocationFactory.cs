using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Interfaces;
using MediaBrowser.Library.Filesystem;

namespace TestMediaBrowser.SupportingClasses {

    public class MockMediaLocationFactory : IMediaLocationFactory {

        IMediaLocation location;

        public MockMediaLocationFactory(IMediaLocation location) {
            this.location = location;
        }

        public IMediaLocation Create(string path) {
            return location;
        }

        
    }
}
