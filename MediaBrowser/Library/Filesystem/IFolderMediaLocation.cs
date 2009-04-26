using System;
using System.Collections.Generic;
using System.Text;

namespace MediaBrowser.Library.Filesystem {
    public interface IFolderMediaLocation : IMediaLocation {
        IList<IMediaLocation> Children { get; }
    }
}
